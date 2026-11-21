using System.Collections.Immutable;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.Sqlite;
using Microsoft.Data.Sqlite;

namespace ExpressionBesoin.Infrastructure.Fiches;

/// <summary>
/// Adaptateur SQLite d'<see cref="IDepotFiches"/> (DEB-Q12, DEB-Q21, DEB-Q22, DEB-Q26) : coquille sans décision, le
/// format vient de <see cref="CodecFiche"/>, le SQL de <see cref="SchemaFiches"/>. Une connexion par appel (réservoir de
/// Microsoft.Data.Sqlite). Appels SYNCHRONES rendus en <see cref="ValueTask"/> déjà terminée : l'API asynchrone de
/// Microsoft.Data.Sqlite n'est qu'une enveloppe synchrone, l'éviter épargne une machine à états par appel. Une erreur
/// (fichier, contrainte, contenu illisible) lève donc dès l'appel ; c'est une panne (DEB-Q22), jamais un refus.
/// Libérations écrites en <c>try</c> / <c>finally</c> : aucune branche cachée à couvrir.
/// </summary>
public sealed class DepotFichesSqlite(OptionsSqlite options) : IDepotFiches
{
    private readonly ConnexionsSqlite _connexions = new(options);

    /// <summary>
    /// Met la base au schéma courant (<see cref="SchemaFiches.Planifier"/>) : base neuve créée (mode WAL, tables, index,
    /// version), base au schéma 1 migrée (DEB-Q76), en une transaction ; sans effet sur une base au schéma courant ;
    /// lève pour une base d'une autre version. Appelé une fois, à la composition (lot F1).
    /// </summary>
    public void Initialiser()
    {
        var connexion = _connexions.Ouvrir();
        try
        {
            var commande = connexion.CreateCommand();
            commande.CommandText = SchemaFiches.LectureVersion;
            if (SchemaFiches.Planifier((long)commande.ExecuteScalar()!) is { } plan)
            {
                if (plan.PoserWal)
                {
                    commande.CommandText = SchemaFiches.ModeWal;
                    commande.ExecuteNonQuery();
                }

                var transaction = connexion.BeginTransaction(deferred: false);
                try
                {
                    commande.Transaction = transaction;
                    commande.CommandText = plan.Script;
                    commande.ExecuteNonQuery();
                    transaction.Commit();
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }
        finally
        {
            connexion.Dispose();
        }
    }

    public ValueTask<FicheBesoin?> Lire(Guid id, CancellationToken annulation)
    {
        annulation.ThrowIfCancellationRequested();
        var connexion = _connexions.Ouvrir();
        try
        {
            var commande = connexion.CreateCommand();
            commande.CommandText = SchemaFiches.Lecture;
            commande.Parameters.Add("$id", SqliteType.Blob).Value = id;
            return new(commande.ExecuteScalar() is string contenu ? CodecFiche.Decoder(contenu) : null);
        }
        finally
        {
            connexion.Dispose();
        }
    }

    public ValueTask<ListeImmuable<Echange>> LireHistorique(Guid id, CancellationToken annulation)
    {
        annulation.ThrowIfCancellationRequested();
        var connexion = _connexions.Ouvrir();
        try
        {
            var commande = connexion.CreateCommand();
            commande.CommandText = SchemaFiches.Historique;
            commande.Parameters.Add("$id", SqliteType.Blob).Value = id;
            var echanges = new List<Echange>();
            var lecteur = commande.ExecuteReader();
            try
            {
                while (lecteur.Read())
                {
                    echanges.Add(new Echange((AuteurEchange)lecteur.GetInt32(0), lecteur.GetString(1), Instant(lecteur.GetInt64(2))));
                }
            }
            finally
            {
                lecteur.Dispose();
            }

            return new(ListeImmuable<Echange>.De(echanges));
        }
        finally
        {
            connexion.Dispose();
        }
    }

    public ValueTask<ListeImmuable<Transition>> LireTransitions(Guid id, CancellationToken annulation)
    {
        annulation.ThrowIfCancellationRequested();
        var connexion = _connexions.Ouvrir();
        try
        {
            var commande = connexion.CreateCommand();
            commande.CommandText = SchemaFiches.LectureTransitions;
            commande.Parameters.Add("$id", SqliteType.Blob).Value = id;
            var transitions = new List<Transition>();
            var lecteur = commande.ExecuteReader();
            try
            {
                while (lecteur.Read())
                {
                    transitions.Add(new Transition(
                        lecteur.IsDBNull(0) ? null : (Statut)lecteur.GetInt32(0),
                        (Statut)lecteur.GetInt32(1),
                        lecteur.GetString(2),
                        Instant(lecteur.GetInt64(3))));
                }
            }
            finally
            {
                lecteur.Dispose();
            }

            return new(ListeImmuable<Transition>.De(transitions));
        }
        finally
        {
            connexion.Dispose();
        }
    }

    public ValueTask Enregistrer(
        FicheBesoin fiche,
        ListeImmuable<Echange> echangesAjoutes,
        Transition? transition,
        CancellationToken annulation)
    {
        annulation.ThrowIfCancellationRequested();
        var contenu = CodecFiche.Encoder(fiche);
        var resume = ResumeFiche.De(fiche);
        var connexion = _connexions.Ouvrir();
        try
        {
            var transaction = connexion.BeginTransaction(deferred: false);
            try
            {
                EcrireFiche(connexion, transaction, resume, contenu);
                AjouterEchanges(connexion, transaction, fiche.Id, echangesAjoutes);
                if (transition is not null)
                {
                    AjouterTransition(connexion, transaction, fiche.Id, transition);
                }

                transaction.Commit();
            }
            finally
            {
                transaction.Dispose();
            }
        }
        finally
        {
            connexion.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<ImmutableArray<ResumeFiche>> Lister(CancellationToken annulation)
    {
        annulation.ThrowIfCancellationRequested();
        var connexion = _connexions.Ouvrir();
        try
        {
            var commande = connexion.CreateCommand();
            commande.CommandText = SchemaFiches.Liste;
            var resumes = ImmutableArray.CreateBuilder<ResumeFiche>();
            var lecteur = commande.ExecuteReader();
            try
            {
                while (lecteur.Read())
                {
                    resumes.Add(new ResumeFiche(
                        lecteur.GetGuid(0),
                        TexteOuNull(lecteur, 1),
                        TexteOuNull(lecteur, 2),
                        lecteur.GetString(3),
                        (Statut)lecteur.GetInt32(4),
                        Instant(lecteur.GetInt64(5)),
                        lecteur.IsDBNull(6) ? null : Instant(lecteur.GetInt64(6))));
                }
            }
            finally
            {
                lecteur.Dispose();
            }

            return new(resumes.ToImmutable());
        }
        finally
        {
            connexion.Dispose();
        }
    }

    public ValueTask<string?> DernierNumero(CancellationToken annulation)
    {
        annulation.ThrowIfCancellationRequested();
        var connexion = _connexions.Ouvrir();
        try
        {
            var commande = connexion.CreateCommand();
            commande.CommandText = SchemaFiches.DernierNumero;
            return new(commande.ExecuteScalar() as string);
        }
        finally
        {
            connexion.Dispose();
        }
    }

    private static void EcrireFiche(SqliteConnection connexion, SqliteTransaction transaction, ResumeFiche resume, string contenu)
    {
        var commande = connexion.CreateCommand();
        commande.Transaction = transaction;
        commande.CommandText = SchemaFiches.Ecriture;
        commande.Parameters.Add("$id", SqliteType.Blob).Value = resume.Id;
        commande.Parameters.Add("$numero", SqliteType.Text).Value = (object?)resume.Numero ?? DBNull.Value;
        commande.Parameters.Add("$intitule", SqliteType.Text).Value = (object?)resume.Intitule ?? DBNull.Value;
        commande.Parameters.Add("$demandeur", SqliteType.Text).Value = resume.Demandeur;
        commande.Parameters.Add("$statut", SqliteType.Integer).Value = (int)resume.Statut;
        commande.Parameters.Add("$creeeLe", SqliteType.Integer).Value = resume.CreeeLe.UtcTicks;
        commande.Parameters.Add("$confirmeeLe", SqliteType.Integer).Value =
            resume.ConfirmeeLe is { } confirmeeLe ? confirmeeLe.UtcTicks : DBNull.Value;
        commande.Parameters.Add("$contenu", SqliteType.Text).Value = contenu;
        commande.ExecuteNonQuery();
    }

    /// <summary>Une commande préparée une fois, exécutée par échange, dans la transaction de la fiche.</summary>
    private static void AjouterEchanges(SqliteConnection connexion, SqliteTransaction transaction, Guid id, ListeImmuable<Echange> echanges)
    {
        var commande = connexion.CreateCommand();
        commande.Transaction = transaction;
        commande.CommandText = SchemaFiches.AjoutEchange;
        commande.Parameters.Add("$id", SqliteType.Blob).Value = id;
        var auteur = commande.Parameters.Add("$auteur", SqliteType.Integer);
        var texte = commande.Parameters.Add("$texte", SqliteType.Text);
        var le = commande.Parameters.Add("$le", SqliteType.Integer);
        foreach (var echange in echanges)
        {
            auteur.Value = (int)echange.Auteur;
            texte.Value = echange.Texte;
            le.Value = echange.Le.UtcTicks;
            commande.ExecuteNonQuery();
        }
    }

    /// <summary>Dans la transaction de la fiche : un statut hors du cycle est refusé par la contrainte du schéma (panne).</summary>
    private static void AjouterTransition(SqliteConnection connexion, SqliteTransaction transaction, Guid id, Transition transition)
    {
        var commande = connexion.CreateCommand();
        commande.Transaction = transaction;
        commande.CommandText = SchemaFiches.AjoutTransition;
        commande.Parameters.Add("$id", SqliteType.Blob).Value = id;
        commande.Parameters.Add("$avant", SqliteType.Integer).Value = transition.Avant is { } avant ? (int)avant : DBNull.Value;
        commande.Parameters.Add("$apres", SqliteType.Integer).Value = (int)transition.Apres;
        commande.Parameters.Add("$acteur", SqliteType.Text).Value = transition.Acteur;
        commande.Parameters.Add("$le", SqliteType.Integer).Value = transition.Le.UtcTicks;
        commande.ExecuteNonQuery();
    }

    private static DateTimeOffset Instant(long ticksUtc) => new(ticksUtc, TimeSpan.Zero);

    private static string? TexteOuNull(SqliteDataReader lecteur, int colonne) =>
        lecteur.IsDBNull(colonne) ? null : lecteur.GetString(colonne);
}
