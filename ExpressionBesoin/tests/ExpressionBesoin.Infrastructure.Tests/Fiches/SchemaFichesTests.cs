using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.Fiches;
using Microsoft.Data.Sqlite;

namespace ExpressionBesoin.Infrastructure.Tests.Fiches;

public sealed class SchemaFichesTests
{
    /// <summary>Tables, index et leur SQL, par nom : ce qu'une base neuve et une base migrée doivent avoir en commun.</summary>
    private const string Structure = "SELECT type || '|' || name || '|' || IFNULL(sql, '') FROM sqlite_schema ORDER BY name;";

    // --- Plan de mise à niveau (pur) ---

    [Fact]
    public void Planifier_une_base_neuve_creation_complete_en_mode_WAL()
    {
        var plan = SchemaFiches.Planifier(0);

        Assert.NotNull(plan);
        Assert.True(plan.PoserWal);
        Assert.Equal(SchemaFiches.Creation, plan.Script);
    }

    [Fact]
    public void Planifier_une_base_au_schema_1_migration_sans_toucher_au_mode()
    {
        var plan = SchemaFiches.Planifier(1);

        Assert.NotNull(plan);
        Assert.False(plan.PoserWal);
        Assert.Equal(SchemaFiches.Migration1Vers2, plan.Script);
    }

    [Fact]
    public void Planifier_une_base_au_schema_courant_rien_a_faire()
    {
        Assert.Equal(2L, SchemaFiches.Version);
        Assert.Null(SchemaFiches.Planifier(2));
    }

    [Theory]
    [InlineData(3L)]
    [InlineData(-1L)]
    public void Planifier_leve_pour_une_version_inconnue(long version)
    {
        var erreur = Assert.Throws<InvalidOperationException>(() => SchemaFiches.Planifier(version));

        Assert.Equal($"Base de fiches au schéma {version} : seuls les schémas 1 et 2 sont connus de cette version.", erreur.Message);
    }

    // --- Initialiser : création, migration, base inconnue ---

    [Fact]
    public void Initialiser_cree_le_schema_courant_en_mode_WAL()
    {
        using var essai = new BaseTemporaire();

        Assert.Equal(2L, essai.Scalaire("PRAGMA user_version;"));
        Assert.Equal("wal", essai.Scalaire("PRAGMA journal_mode;"));
        Assert.Equal(3L, essai.Scalaire("SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table';"));
        Assert.Equal(1L, essai.Scalaire("SELECT COUNT(*) FROM sqlite_schema WHERE name = 'IX_Fiches_Numero';"));
        Assert.Equal(1L, essai.Scalaire("SELECT COUNT(*) FROM sqlite_schema WHERE name = 'Transitions';"));
    }

    [Fact]
    public async Task Initialiser_une_seconde_fois_ne_touche_pas_aux_donnees()
    {
        using var essai = new BaseTemporaire();
        var fiche = Essais.Brouillon(1, Essais.T0);
        var creation = new Transition(null, Statut.Brouillon, "Camille Martin", Essais.T0);
        await essai.Depot.Enregistrer(fiche, Essais.Echanges(), creation, Essais.Annulation);

        var autre = new DepotFichesSqlite(essai.Options);
        autre.Initialiser();

        Assert.Equal(fiche, await autre.Lire(fiche.Id, Essais.Annulation));
        Assert.Equal(new[] { creation }, (await autre.LireTransitions(fiche.Id, Essais.Annulation)).ToArray());
        Assert.Equal(2L, essai.Scalaire("PRAGMA user_version;"));
    }

    [Fact]
    public async Task Initialiser_migre_une_base_au_schema_1_sans_perte_et_comme_une_base_neuve()
    {
        using var ancienne = new BaseTemporaire(schema1: true);
        var fiche = Essais.Reprise(1, Essais.T0, "EB-2026-0001");
        var echanges = Essais.Echanges(Essais.EchangeAgent("avant la migration", Essais.T0));
        // L'adaptateur écrit une base au schéma 1 tant qu'aucune transition n'est demandée : données d'avant L1.
        await ancienne.Depot.Enregistrer(fiche, echanges, null, Essais.Annulation);
        Assert.Equal(1L, ancienne.Scalaire("PRAGMA user_version;"));
        Assert.Equal(0L, ancienne.Scalaire("SELECT COUNT(*) FROM sqlite_schema WHERE name = 'Transitions';"));

        var migree = new DepotFichesSqlite(ancienne.Options);
        migree.Initialiser();

        Assert.Equal(2L, ancienne.Scalaire("PRAGMA user_version;"));
        Assert.Equal("wal", ancienne.Scalaire("PRAGMA journal_mode;"));
        Assert.Equal(fiche, await migree.Lire(fiche.Id, Essais.Annulation));
        Assert.Equal(echanges, await migree.LireHistorique(fiche.Id, Essais.Annulation));
        Assert.Empty(await migree.LireTransitions(fiche.Id, Essais.Annulation));
        using var neuve = new BaseTemporaire();
        Assert.Equal(Lignes(neuve, Structure), Lignes(ancienne, Structure));

        var soumission = new Transition(Statut.Brouillon, Statut.Soumise, "Camille Martin", Essais.T0.AddDays(1));
        await migree.Enregistrer(fiche with { Statut = Statut.Soumise }, Essais.Echanges(), soumission, Essais.Annulation);
        Assert.Equal(new[] { soumission }, (await migree.LireTransitions(fiche.Id, Essais.Annulation)).ToArray());
    }

    [Fact]
    public void Initialiser_leve_pour_une_base_d_une_autre_version()
    {
        using var essai = new BaseTemporaire();
        essai.Scalaire("PRAGMA user_version = 3;");

        Assert.Throws<InvalidOperationException>(() => new DepotFichesSqlite(essai.Options).Initialiser());
        Assert.Equal(3L, essai.Scalaire("PRAGMA user_version;"));
    }

    // --- Contrainte du cycle de vie ---

    [Fact]
    public async Task La_contrainte_du_schema_admet_exactement_les_paires_du_Domaine()
    {
        using var essai = new BaseTemporaire();
        var fiche = Essais.Brouillon(1, Essais.T0);
        await essai.Depot.Enregistrer(fiche, Essais.Echanges(), null, Essais.Annulation);
        var avants = new Statut?[] { null }.Concat(Enum.GetValues<Statut>().Select(statut => (Statut?)statut)).ToArray();
        var admisesParLeSchema = new List<string>();
        var admisesParLeDomaine = new List<string>();

        using var connexion = essai.Brute();
        foreach (var avant in avants)
        {
            foreach (var apres in Enum.GetValues<Statut>())
            {
                var paire = $"{avant?.ToString() ?? "aucun"} → {apres}";
                using var commande = connexion.CreateCommand();
                commande.CommandText = SchemaFiches.AjoutTransition;
                commande.Parameters.Add("$id", SqliteType.Blob).Value = fiche.Id;
                commande.Parameters.Add("$avant", SqliteType.Integer).Value = avant is { } statut ? (int)statut : DBNull.Value;
                commande.Parameters.Add("$apres", SqliteType.Integer).Value = (int)apres;
                commande.Parameters.Add("$acteur", SqliteType.Text).Value = "x";
                commande.Parameters.Add("$le", SqliteType.Integer).Value = Essais.TicksT0;
                try
                {
                    commande.ExecuteNonQuery();
                    admisesParLeSchema.Add(paire);
                }
                catch (SqliteException erreur) when (erreur.SqliteErrorCode == 19)
                {
                    // SQLITE_CONSTRAINT : paire refusée par la contrainte CHECK.
                }

                try
                {
                    Transitions.EtapeDe(new Transition(avant, apres, "x", Essais.T0));
                    admisesParLeDomaine.Add(paire);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        Assert.Equal(30, avants.Length * Enum.GetValues<Statut>().Length);
        Assert.Equal(6, admisesParLeSchema.Count);
        Assert.Equal(admisesParLeDomaine, admisesParLeSchema);
    }

    // --- Plans de requête ---

    [Fact]
    public void Lister_parcourt_l_index_des_dates_sans_tri()
    {
        using var essai = new BaseTemporaire();

        var plan = Plan(essai, SchemaFiches.Liste);

        Assert.Contains(plan, etape => etape.Contains("USING INDEX IX_Fiches_CreeeLe", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, etape => etape.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(SchemaFiches.Historique)]
    [InlineData(SchemaFiches.LectureTransitions)]
    public void Historique_et_transitions_se_lisent_par_la_cle_primaire_sans_tri(string requete)
    {
        using var essai = new BaseTemporaire();

        var plan = Plan(essai, requete);

        Assert.Contains(plan, etape => etape.Contains("USING PRIMARY KEY (FicheId=?)", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, etape => etape.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    private static List<string> Plan(BaseTemporaire essai, string requete)
    {
        using var connexion = essai.Brute();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "EXPLAIN QUERY PLAN " + requete;
        commande.Parameters.Add("$id", SqliteType.Blob).Value = Essais.IdReference;
        var etapes = new List<string>();
        using var lecteur = commande.ExecuteReader();
        while (lecteur.Read())
        {
            etapes.Add(lecteur.GetString(3));
        }

        return etapes;
    }

    private static List<string> Lignes(BaseTemporaire essai, string requete)
    {
        using var connexion = essai.Brute();
        using var commande = connexion.CreateCommand();
        commande.CommandText = requete;
        var lignes = new List<string>();
        using var lecteur = commande.ExecuteReader();
        while (lecteur.Read())
        {
            lignes.Add(lecteur.GetString(0));
        }

        return lignes;
    }
}
