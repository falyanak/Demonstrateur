using ExpressionBesoin.Infrastructure.Sqlite;
using Microsoft.Data.Sqlite;

namespace ExpressionBesoin.Infrastructure.Tests.Sqlite;

public sealed class ConnexionsSqliteTests
{
    [Fact]
    public void Un_identifiant_inconnu_entre_guillemets_doubles_est_refuse_et_non_lu_comme_une_chaine()
    {
        // Sans la configuration DQS, SQLite rendrait la chaîne « colonneInconnue » sans erreur (règles, annexe).
        using var essai = new BaseTemporaire();
        using var connexion = new ConnexionsSqlite(essai.Options).Ouvrir();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "SELECT \"colonneInconnue\" FROM Fiches;";

        var erreur = Assert.Throws<SqliteException>(() => commande.ExecuteScalar());

        Assert.Contains("colonneInconnue", erreur.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Les_cles_etrangeres_sont_verifiees()
    {
        using var essai = new BaseTemporaire();
        using var connexion = new ConnexionsSqlite(essai.Options).Ouvrir();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "INSERT INTO Echanges (FicheId, Rang, Auteur, Texte, Le) VALUES (x'00', 1, 0, 'orphelin', 0);";

        var erreur = Assert.Throws<SqliteException>(() => commande.ExecuteNonQuery());

        Assert.Contains("FOREIGN KEY", erreur.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Un_dossier_absent_leve_a_l_ouverture()
    {
        var chemin = Path.Combine(Path.GetTempPath(), $"deb-i1-absent-{Guid.NewGuid():N}", "fiches.db");

        Assert.Throws<SqliteException>(() => new ConnexionsSqlite(new OptionsSqlite(chemin, Pooling: false)).Ouvrir());
        Assert.False(File.Exists(chemin));
    }

    [Fact]
    public void Exiger_SQLITE_OK_ne_leve_pas()
    {
        ConnexionsSqlite.Exiger(0, "opération");
    }

    [Fact]
    public void Exiger_un_autre_code_leve_en_le_citant()
    {
        var erreur = Assert.Throws<InvalidOperationException>(() => ConnexionsSqlite.Exiger(1, "opération"));

        Assert.Equal("SQLite : opération a échoué (code 1).", erreur.Message);
    }
}
