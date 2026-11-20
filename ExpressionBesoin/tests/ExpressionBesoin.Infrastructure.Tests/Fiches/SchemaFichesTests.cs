using ExpressionBesoin.Infrastructure.Fiches;
using Microsoft.Data.Sqlite;

namespace ExpressionBesoin.Infrastructure.Tests.Fiches;

public sealed class SchemaFichesTests
{
    [Theory]
    [InlineData(0L, true)]
    [InlineData(1L, false)]
    public void DoitCreer_une_base_neuve_seulement(long version, bool attendu)
    {
        Assert.Equal(attendu, SchemaFiches.DoitCreer(version));
    }

    [Theory]
    [InlineData(2L)]
    [InlineData(-1L)]
    public void DoitCreer_leve_pour_une_version_inconnue(long version)
    {
        var erreur = Assert.Throws<InvalidOperationException>(() => SchemaFiches.DoitCreer(version));

        Assert.Equal($"Base de fiches au schéma {version} : seul le schéma 1 est connu de cette version.", erreur.Message);
    }

    [Fact]
    public void Initialiser_cree_le_schema_courant_en_mode_WAL()
    {
        using var essai = new BaseTemporaire();

        Assert.Equal(1L, essai.Scalaire("PRAGMA user_version;"));
        Assert.Equal("wal", essai.Scalaire("PRAGMA journal_mode;"));
        Assert.Equal(2L, essai.Scalaire("SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table';"));
        Assert.Equal(1L, essai.Scalaire("SELECT COUNT(*) FROM sqlite_schema WHERE name = 'IX_Fiches_Numero';"));
    }

    [Fact]
    public async Task Initialiser_une_seconde_fois_ne_touche_pas_aux_donnees()
    {
        using var essai = new BaseTemporaire();
        var fiche = Essais.Brouillon(1, Essais.T0);
        await essai.Depot.Enregistrer(fiche, Essais.Echanges(), Essais.Annulation);

        var autre = new DepotFichesSqlite(essai.Options);
        autre.Initialiser();

        Assert.Equal(fiche, await autre.Lire(fiche.Id, Essais.Annulation));
        Assert.Equal(1L, essai.Scalaire("PRAGMA user_version;"));
    }

    [Fact]
    public void Initialiser_leve_pour_une_base_d_une_autre_version()
    {
        using var essai = new BaseTemporaire();
        essai.Scalaire("PRAGMA user_version = 2;");

        Assert.Throws<InvalidOperationException>(() => new DepotFichesSqlite(essai.Options).Initialiser());
    }

    [Fact]
    public void Lister_parcourt_l_index_des_dates_sans_tri()
    {
        using var essai = new BaseTemporaire();

        var plan = Plan(essai, SchemaFiches.Liste);

        Assert.Contains(plan, etape => etape.Contains("USING INDEX IX_Fiches_CreeeLe", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, etape => etape.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    [Fact]
    public void L_historique_se_lit_par_la_cle_primaire_sans_tri()
    {
        using var essai = new BaseTemporaire();

        var plan = Plan(essai, SchemaFiches.Historique);

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
}
