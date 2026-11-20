namespace ExpressionBesoin.Infrastructure.Fiches;

/// <summary>
/// Schéma SQLite des fiches et texte des requêtes (DEB-Q12, DEB-Q29). Données pures, rejouées dans les tests sur le
/// schéma de production. Aucun identifiant entre guillemets doubles (et leur lecture comme chaîne est désactivée à
/// l'ouverture : <see cref="Sqlite.ConnexionsSqlite"/>).
/// </summary>
internal static class SchemaFiches
{
    /// <summary>Version du schéma, <c>PRAGMA user_version</c>.</summary>
    public const long Version = 1;

    /// <summary>
    /// <c>Fiches</c> : colonnes de liste dénormalisées (lues par <c>Lister</c> sans désérialiser) et contenu JSON
    /// (<see cref="CodecFiche"/>) ; <c>Id</c> en BLOB de 16 octets, instants en ticks UTC. <c>Echanges</c> : rangée par
    /// (fiche, rang), sans rowid, pour lire un historique d'un seul parcours de plage.
    /// </summary>
    public const string Creation = """
        CREATE TABLE Fiches (
            Id          BLOB    NOT NULL PRIMARY KEY,
            Numero      TEXT,
            Intitule    TEXT,
            Demandeur   TEXT    NOT NULL,
            Statut      INTEGER NOT NULL CHECK (Statut BETWEEN 0 AND 4),
            CreeeLe     INTEGER NOT NULL,
            ConfirmeeLe INTEGER,
            Contenu     TEXT    NOT NULL
        ) STRICT;
        CREATE UNIQUE INDEX IX_Fiches_Numero ON Fiches (Numero);
        CREATE INDEX IX_Fiches_CreeeLe ON Fiches (CreeeLe);
        CREATE TABLE Echanges (
            FicheId BLOB    NOT NULL REFERENCES Fiches (Id) ON DELETE CASCADE,
            Rang    INTEGER NOT NULL,
            Auteur  INTEGER NOT NULL CHECK (Auteur IN (0, 1)),
            Texte   TEXT    NOT NULL,
            Le      INTEGER NOT NULL,
            PRIMARY KEY (FicheId, Rang)
        ) STRICT, WITHOUT ROWID;
        PRAGMA user_version = 1;
        """;

    public const string LectureVersion = "PRAGMA user_version;";

    /// <summary>Persistant dans le fichier : posé une fois, à la création, hors transaction.</summary>
    public const string ModeWal = "PRAGMA journal_mode = WAL;";

    public const string Lecture = "SELECT Contenu FROM Fiches WHERE Id = $id;";

    public const string Ecriture = """
        INSERT INTO Fiches (Id, Numero, Intitule, Demandeur, Statut, CreeeLe, ConfirmeeLe, Contenu)
        VALUES ($id, $numero, $intitule, $demandeur, $statut, $creeeLe, $confirmeeLe, $contenu)
        ON CONFLICT (Id) DO UPDATE SET
            Numero = excluded.Numero,
            Intitule = excluded.Intitule,
            Demandeur = excluded.Demandeur,
            Statut = excluded.Statut,
            CreeeLe = excluded.CreeeLe,
            ConfirmeeLe = excluded.ConfirmeeLe,
            Contenu = excluded.Contenu;
        """;

    /// <summary>Rang suivant calculé dans la même instruction (un agrégat sans GROUP BY rend toujours une ligne).</summary>
    public const string AjoutEchange = """
        INSERT INTO Echanges (FicheId, Rang, Auteur, Texte, Le)
        SELECT $id, COALESCE(MAX(Rang), 0) + 1, $auteur, $texte, $le FROM Echanges WHERE FicheId = $id;
        """;

    public const string Historique = "SELECT Auteur, Texte, Le FROM Echanges WHERE FicheId = $id ORDER BY Rang;";

    /// <summary>Plus récente d'abord ; à date égale, la dernière insérée. Parcours inverse de l'index, sans tri.</summary>
    public const string Liste = """
        SELECT Id, Numero, Intitule, Demandeur, Statut, CreeeLe, ConfirmeeLe FROM Fiches ORDER BY CreeeLe DESC, rowid DESC;
        """;

    /// <summary>MAX en collation BINARY : ordre ordinal, comme l'exige <c>IDepotFiches.DernierNumero</c>.</summary>
    public const string DernierNumero = "SELECT MAX(Numero) FROM Fiches;";

    /// <summary>
    /// Version lue → faut-il créer le schéma ? 0 (base neuve) : oui ; <see cref="Version"/> : non ; toute autre valeur
    /// (base d'une autre version) lève, plutôt que d'écrire dans un schéma inconnu.
    /// </summary>
    public static bool DoitCreer(long versionLue) =>
        versionLue switch
        {
            0 => true,
            Version => false,
            _ => throw new InvalidOperationException(
                $"Base de fiches au schéma {versionLue} : seul le schéma {Version} est connu de cette version."),
        };
}
