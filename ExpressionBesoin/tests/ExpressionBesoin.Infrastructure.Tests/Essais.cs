using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.Fiches;
using ExpressionBesoin.Infrastructure.Sqlite;
using Microsoft.Data.Sqlite;

namespace ExpressionBesoin.Infrastructure.Tests;

/// <summary>Données d'essai fictives et aides communes aux tests de l'Infrastructure.</summary>
internal static class Essais
{
    /// <summary>20/11/2026 9 h UTC ; ticks recalculés hors .NET (réplique Python) : <see cref="TicksT0"/>.</summary>
    public static readonly DateTimeOffset T0 = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    public const long TicksT0 = 639307620000000000;

    /// <summary>Identifiant de la fiche de référence ; octets BLOB attendus (ordre de <c>Guid.ToByteArray</c>) :</summary>
    public static readonly Guid IdReference = new("6f1c2b3a-0000-4000-8000-0000000000c1");

    public const string OctetsIdReference = "3A2B1C6F0000004080000000000000C1";

    public static CancellationToken Annulation => TestContext.Current.CancellationToken;

    public static Guid IdNumero(int numero) => new($"6f1c2b3a-0000-4000-8000-{numero:x12}");

    public static EnTete EnTeteFictif(DateTimeOffset creeeLe) => new("Camille Martin", "Bureau 1A", "Dominique Bernard", creeeLe);

    public static T Valeur<T>(Resultat<T> resultat) =>
        resultat.Match(valeur => valeur, refus => throw new InvalidOperationException("Refus inattendu : " + refus.Code));

    public static FicheBesoin Brouillon(int numero, DateTimeOffset creeeLe) => Fiche.Creer(IdNumero(numero), EnTeteFictif(creeeLe));

    /// <summary>
    /// Brouillon complet : rubriques obligatoires saisies (« valeur de &lt;code&gt; »), solution envisagée acceptée de
    /// l'IA, contraintes saisies.
    /// </summary>
    public static FicheBesoin Complete(int numero, DateTimeOffset creeeLe)
    {
        var fiche = Brouillon(numero, creeeLe);
        foreach (var definition in Catalogue.Toutes)
        {
            if (definition.Obligatoire)
            {
                fiche = Valeur(Fiche.ModifierChamp(fiche, definition.Rubrique, "valeur de " + definition.Code));
            }
        }

        fiche = Valeur(Fiche.AppliquerPropositions(
            fiche,
            ListeImmuable<PropositionRubrique>.Vide.Ajouter(
                new PropositionRubrique(Rubrique.SolutionEnvisagee, new Proposition("Portail unique", "citée par l'agent")))));
        fiche = Valeur(Fiche.AccepterProposition(fiche, Rubrique.SolutionEnvisagee));
        return Valeur(Fiche.ModifierChamp(fiche, Rubrique.Contraintes, "Avant juin"));
    }

    public static FicheBesoin Soumise(int numero, DateTimeOffset creeeLe, string numeroFiche) =>
        Valeur(Cycle.Soumettre(Complete(numero, creeeLe), true, numeroFiche, creeeLe.AddHours(1)));

    public static FicheBesoin Validee(int numero, DateTimeOffset creeeLe, string numeroFiche) =>
        Valeur(Cycle.Decider(Soumise(numero, creeeLe, numeroFiche), TypeDecision.Valider, "Dominique Bernard", null, creeeLe.AddHours(2)));

    /// <summary>
    /// Retour pour complément, reprise, puis une proposition de l'IA en attente : liste de décisions avec commentaire,
    /// numéro gardé, rubrique « Proposée ».
    /// </summary>
    public static FicheBesoin Reprise(int numero, DateTimeOffset creeeLe, string numeroFiche)
    {
        var fiche = Valeur(Cycle.Decider(
            Soumise(numero, creeeLe, numeroFiche), TypeDecision.DemanderComplements, "Dominique Bernard", "Chiffrer", creeeLe.AddHours(2)));
        fiche = Valeur(Cycle.Reprendre(fiche));
        return Valeur(Fiche.AppliquerPropositions(
            fiche,
            ListeImmuable<PropositionRubrique>.Vide.Ajouter(
                new PropositionRubrique(Rubrique.Echeance, new Proposition("Juin 2027", "saison des contrôles")))));
    }

    /// <summary>
    /// Fiche de référence du format JSON, construite champ par champ (le codec ne juge pas la cohérence métier) :
    /// guillemets, apostrophes, accents, saut de ligne, fraction de seconde.
    /// </summary>
    public static FicheBesoin Reference()
    {
        var champs = ListeImmuable<Champ>.De(Enumerable.Repeat(Champ.Initial, Catalogue.Toutes.Length))
            .Remplacer((int)Rubrique.Application, new Champ("Outil de contrôle \"Vigie\"", Origine.Manuelle, null))
            .Remplacer((int)Rubrique.Intitule, new Champ("Contrôle e-commerce", Origine.AccepteeIA, null))
            .Remplacer((int)Rubrique.Difficultes, new Champ(null, Origine.Vide, new Proposition("Délais d'instruction", "cité par l'agent")));
        return new FicheBesoin(
            IdReference,
            "EB-2026-0007",
            Statut.ACompleter,
            EnTeteFictif(T0),
            champs,
            new DateTimeOffset(2026, 11, 20, 10, 15, 30, 500, TimeSpan.Zero),
            ListeImmuable<Decision>.Vide.Ajouter(new Decision(
                TypeDecision.DemanderComplements, "Dominique Bernard", T0.AddHours(2), "Préciser le volume\nde dossiers")));
    }

    /// <summary>JSON de <see cref="Reference"/>, écrit à la main (pas recopié de la sortie du codec).</summary>
    public const string JsonReference =
        """{"v":1,"id":"6f1c2b3a-0000-4000-8000-0000000000c1","numero":"EB-2026-0007","statut":"ACompleter","enTete":{"demandeur":"Camille Martin","bureau":"Bureau 1A","valideur":"Dominique Bernard","creeeLe":"2026-11-20T09:00:00+00:00"},"champs":{"application":{"valeur":"Outil de contrôle \"Vigie\"","origine":"Manuelle"},"intitule":{"valeur":"Contrôle e-commerce","origine":"AccepteeIA"},"difficultes":{"proposition":{"valeur":"Délais d'instruction","justification":"cité par l'agent"}}},"confirmeeLe":"2026-11-20T10:15:30.5+00:00","decisions":[{"type":"DemanderComplements","auteur":"Dominique Bernard","le":"2026-11-20T11:00:00+00:00","commentaire":"Préciser le volume\nde dossiers"}]}""";

    /// <summary>JSON du brouillon vierge <c>Brouillon(1, T0)</c> : propriétés nulles et champs initiaux omis.</summary>
    public const string JsonBrouillonVierge =
        """{"v":1,"id":"6f1c2b3a-0000-4000-8000-000000000001","statut":"Brouillon","enTete":{"demandeur":"Camille Martin","bureau":"Bureau 1A","valideur":"Dominique Bernard","creeeLe":"2026-11-20T09:00:00+00:00"},"champs":{},"decisions":[]}""";

    public static Echange EchangeAgent(string texte, DateTimeOffset le) => new(AuteurEchange.Agent, texte, le);

    public static Echange EchangeIA(string texte, DateTimeOffset le) => new(AuteurEchange.IA, texte, le);

    public static ListeImmuable<Echange> Echanges(params Echange[] echanges) => ListeImmuable<Echange>.De(echanges);
}

/// <summary>
/// Base SQLite neuve au schéma de PRODUCTION (<see cref="DepotFichesSqlite.Initialiser"/>), dans un fichier temporaire
/// supprimé à la fin du test ; sans réservoir de connexions, pour que le fichier se libère.
/// </summary>
internal sealed class BaseTemporaire : IDisposable
{
    public BaseTemporaire()
    {
        Depot = new DepotFichesSqlite(Options);
        Depot.Initialiser();
    }

    public string Chemin { get; } = Path.Combine(Path.GetTempPath(), $"deb-i1-{Guid.NewGuid():N}.db");

    public OptionsSqlite Options => new(Chemin, Pooling: false);

    public DepotFichesSqlite Depot { get; }

    /// <summary>Connexion brute, hors adaptateur, pour lire ce qui est réellement stocké.</summary>
    public SqliteConnection Brute()
    {
        var connexion = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Chemin, Pooling = false }.ToString());
        connexion.Open();
        return connexion;
    }

    public object? Scalaire(string sql)
    {
        using var connexion = Brute();
        using var commande = connexion.CreateCommand();
        commande.CommandText = sql;
        return commande.ExecuteScalar();
    }

    public void Dispose()
    {
        foreach (var suffixe in new[] { "", "-wal", "-shm" })
        {
            File.Delete(Chemin + suffixe);
        }
    }
}
