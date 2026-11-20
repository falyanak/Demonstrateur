using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace ExpressionBesoin.Infrastructure.Sqlite;

/// <summary>
/// Fichier de la base SQLite. <paramref name="Chemin"/> : fichier local (jamais un partage réseau), dossier existant ;
/// <paramref name="Pooling"/> : <c>false</c> dans les tests, pour que le fichier se libère dès la connexion fermée.
/// </summary>
public sealed record OptionsSqlite(string Chemin, bool Pooling = true);

/// <summary>
/// Ouverture des connexions : clés étrangères actives, identifiants entre guillemets doubles inconnus REFUSÉS au lieu
/// d'être lus comme des chaînes (règles, annexe « SQLite »). L'attente sur verrou (SQLITE_BUSY) est gérée par
/// Microsoft.Data.Sqlite jusqu'au délai de commande (30 s par défaut).
/// </summary>
internal sealed class ConnexionsSqlite(OptionsSqlite options)
{
    // sqlite3.h : SQLITE_DBCONFIG_DQS_DML, SQLITE_DBCONFIG_DQS_DDL (SQLite 3.29).
    private const int DqsDml = 1013;
    private const int DqsDdl = 1014;

    private readonly string _chaine = new SqliteConnectionStringBuilder
    {
        DataSource = options.Chemin,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Pooling = options.Pooling,
        ForeignKeys = true,
    }.ToString();

    public SqliteConnection Ouvrir()
    {
        var connexion = new SqliteConnection(_chaine);
        try
        {
            connexion.Open();
            var poignee = connexion.Handle!;
            Exiger(raw.sqlite3_db_config(poignee, DqsDml, 0, out _), "SQLITE_DBCONFIG_DQS_DML");
            Exiger(raw.sqlite3_db_config(poignee, DqsDdl, 0, out _), "SQLITE_DBCONFIG_DQS_DDL");
            return connexion;
        }
        catch
        {
            connexion.Dispose();
            throw;
        }
    }

    /// <summary>Code de retour d'un appel natif : <c>SQLITE_OK</c> (0) exigé.</summary>
    internal static void Exiger(int code, string operation)
    {
        if (code != raw.SQLITE_OK)
        {
            throw new InvalidOperationException($"SQLite : {operation} a échoué (code {code}).");
        }
    }
}
