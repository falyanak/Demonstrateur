namespace ExpressionBesoin.Infrastructure.Fiches;

/// <summary>
/// Mise à niveau d'une base (<see cref="SchemaFiches.Planifier"/>) : <paramref name="Script"/> exécuté dans une
/// transaction, précédé du passage en mode WAL pour une base neuve (<paramref name="PoserWal"/>, hors transaction).
/// </summary>
internal sealed record PlanSchema(bool PoserWal, string Script);

/// <summary>
/// Schéma SQLite des fiches et texte des requêtes (DEB-Q12, DEB-Q29). Données pures, rejouées dans les tests sur le
/// schéma de production. Aucun identifiant entre guillemets doubles (et leur lecture comme chaîne est désactivée à
/// l'ouverture : <see cref="Sqlite.ConnexionsSqlite"/>).
/// </summary>
internal static class SchemaFiches
{
    /// <summary>Version du schéma, <c>PRAGMA user_version</c> : 1 au lot I1, 2 au lot L1 (table <c>Transitions</c>).</summary>
    public const long Version = 2;

    /// <summary>
    /// Tables du schéma 1, INCHANGÉES depuis I1 (une base migrée et une base neuve ont le même schéma, vérifié par test).
    /// <c>Fiches</c> : colonnes de liste dénormalisées (lues par <c>Lister</c> sans désérialiser) et contenu JSON
    /// (<see cref="CodecFiche"/>) ; <c>Id</c> en BLOB de 16 octets, instants en ticks UTC. <c>Echanges</c> : rangée par
    /// (fiche, rang), sans rowid, pour lire un historique d'un seul parcours de plage.
    /// </summary>
    public const string TablesSchema1 = """
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
        """;

    /// <summary>
    /// Cycle de vie (DEB-Q74) : une ligne par changement de statut, rangée par (fiche, rang) comme <c>Echanges</c> ;
    /// statuts en entiers (<c>Avant</c> NULL à la création), instant en ticks UTC, aucun texte libre (DEB-Q66). La
    /// contrainte n'admet que les six transitions du cycle (0 Brouillon, 1 Soumise, 2 Validee, 3 ACompleter, 4 Refusee) :
    /// les mêmes paires que <c>Transitions.EtapeDe</c> du Domaine, vérifié par test sur les trente paires possibles.
    /// Comparaisons par <c>IS</c>, jamais par <c>=</c> : avec <c>Avant</c> NULL, <c>Avant = 0</c> vaut NULL, et une
    /// contrainte CHECK évaluée à NULL est SATISFAITE (création suivie de n'importe quel statut admise : relevé par la
    /// réplique sqlite3 du lot).
    /// </summary>
    public const string TableTransitions = """
        CREATE TABLE Transitions (
            FicheId BLOB    NOT NULL REFERENCES Fiches (Id) ON DELETE CASCADE,
            Rang    INTEGER NOT NULL,
            Avant   INTEGER,
            Apres   INTEGER NOT NULL,
            Acteur  TEXT    NOT NULL,
            Le      INTEGER NOT NULL,
            PRIMARY KEY (FicheId, Rang),
            CHECK ((Avant IS NULL AND Apres = 0)
                OR (Avant IS 0 AND Apres = 1)
                OR (Avant IS 1 AND Apres IN (2, 3, 4))
                OR (Avant IS 3 AND Apres = 0))
        ) STRICT, WITHOUT ROWID;
        """;

    /// <summary>Base neuve : schéma courant complet.</summary>
    public const string Creation = TablesSchema1 + TableTransitions + "PRAGMA user_version = 2;";

    /// <summary>
    /// Base au schéma 1 (DEB-Q76) : la table du cycle de vie s'ajoute, les données restent ; aucune reconstitution des
    /// transitions passées (le cycle de vie d'une fiche existante commence à la migration).
    /// </summary>
    public const string Migration1Vers2 = TableTransitions + "PRAGMA user_version = 2;";

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

    /// <summary>Rang suivant calculé dans la même instruction, comme <see cref="AjoutEchange"/>.</summary>
    public const string AjoutTransition = """
        INSERT INTO Transitions (FicheId, Rang, Avant, Apres, Acteur, Le)
        SELECT $id, COALESCE(MAX(Rang), 0) + 1, $avant, $apres, $acteur, $le FROM Transitions WHERE FicheId = $id;
        """;

    public const string LectureTransitions = "SELECT Avant, Apres, Acteur, Le FROM Transitions WHERE FicheId = $id ORDER BY Rang;";

    /// <summary>Plus récente d'abord ; à date égale, la dernière insérée. Parcours inverse de l'index, sans tri.</summary>
    public const string Liste = """
        SELECT Id, Numero, Intitule, Demandeur, Statut, CreeeLe, ConfirmeeLe FROM Fiches ORDER BY CreeeLe DESC, rowid DESC;
        """;

    /// <summary>MAX en collation BINARY : ordre ordinal, comme l'exige <c>IDepotFiches.DernierNumero</c>.</summary>
    public const string DernierNumero = "SELECT MAX(Numero) FROM Fiches;";

    /// <summary>
    /// Version lue → mise à niveau (DEB-Q76) : 0 (base neuve) → création, mode WAL ; 1 → migration ;
    /// <see cref="Version"/> → rien (<c>null</c>) ; toute autre valeur (base d'une version inconnue, plus récente par
    /// exemple) lève, plutôt que d'écrire dans un schéma inconnu.
    /// </summary>
    public static PlanSchema? Planifier(long versionLue) =>
        versionLue switch
        {
            0 => new PlanSchema(true, Creation),
            1 => new PlanSchema(false, Migration1Vers2),
            Version => null,
            _ => throw new InvalidOperationException(
                $"Base de fiches au schéma {versionLue} : seuls les schémas 1 et {Version} sont connus de cette version."),
        };
}
