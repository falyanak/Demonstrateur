using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.Documents;

namespace ExpressionBesoin.Infrastructure.Tests.Documents;

/// <summary>Données d'essai fictives et aides des tests du document FEB (lot I2).</summary>
internal static class EssaisDocuments
{
    public static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    /// <summary>
    /// Textes des 17 rubriques : guillemets, « & » et « < » (échappement XML), emoji (paire de substitution), sauts de
    /// ligne LF et CRLF, caractère de contrôle (retiré), valeur par défaut du Domaine.
    /// </summary>
    public static readonly string[] Rubriques =
    [
        "Outil « Vigie » & <portail>",
        "valeur de intitule",
        "Synthèse \U0001F600",
        "Ligne 1\nLigne 2",
        "Délais\r\nd\u2019instruction",
        "Non renseigné",
        "valeur de resultatRecherche",
        "valeur de criteresReussite",
        "valeur de solutionEnvisagee",
        "valeur de utilisateursConcernes",
        "valeur de processusMetier",
        "valeur de liens",
        "valeur de beneficesAttendus",
        "valeur de echeance",
        "valeur de contraintes",
        "valeur de documentsReferences",
        "contrôle\u0001 retiré",
    ];

    /// <summary>Contenu de référence, écrit champ par champ (le générateur ne dépend pas des règles du Domaine).</summary>
    public static ContenuDocument Contenu(bool confirmee = true) =>
        new(
            "EB-2026-0007",
            "Camille Martin",
            "20/11/2026",
            "Bureau 1A",
            "Dominique Bernard",
            ListeImmuable<string>.De(Rubriques),
            confirmee,
            confirmee ? "20/11/2026" : "Non confirmée",
            "Validée",
            "Dominique Bernard, le 20/11/2026",
            Essais.T0,
            Essais.T0.AddHours(2));

    /// <summary>
    /// Textes attendus des 55 paragraphes de word/document.xml pour <see cref="Contenu"/> (w:br rendu « \n ») :
    /// calculés par une réplique Python du remplissage sur le gabarit embarqué, pas recopiés de la sortie du code.
    /// </summary>
    public static readonly string[] ParagraphesAttendus =
    [
        "",
        "",
        "",
        "",
        "",
        "N° de demande : EB-2026-0007",
        "Demande exprimée par : Camille Martin",
        "Date de la demande : 20/11/2026",
        "Bureau : Bureau 1A",
        "Valideur : Dominique Bernard",
        "",
        "EXPRESSION DE BESOIN",
        "Demande d\u2019évolution du système d\u2019information",
        "Modèle de fiche destiné à être renseigné par le demandeur, notamment avec l\u2019assistance de l\u2019IA.",
        "Application ou service numérique concerné : Outil « Vigie » & <portail>",
        "Intitulé court de la demande : valeur de intitule",
        "1. Synthèse de la demande",
        "Résumé en quelques lignes : Synthèse \U0001F600",
        "2. Contexte et problème à résoudre",
        "Situation actuelle : Ligne 1\nLigne 2",
        "Difficultés rencontrées : Délais\nd\u2019instruction",
        "Exemple représentatif : Non renseigné",
        "3. Objectifs et résultats attendus",
        "Résultat recherché : valeur de resultatRecherche",
        "Critères de réussite : valeur de criteresReussite",
        "Solution envisagée par le demandeur : valeur de solutionEnvisagee",
        "4. Périmètre et bénéficiaires",
        "Utilisateurs ou services concernés : valeur de utilisateursConcernes",
        "Processus métier concerné : valeur de processusMetier",
        "Liens avec d\u2019autres outils ou demandes : valeur de liens",
        "5. Bénéfices, échéance et contraintes",
        "Bénéfices attendus : valeur de beneficesAttendus",
        "Échéance souhaitée et justification : valeur de echeance",
        "Contraintes identifiées : valeur de contraintes",
        "6. Pièces et précisions complémentaires",
        "Documents joints ou références utiles : valeur de documentsReferences",
        "Points restant à clarifier : contrôle retiré",
        "Confirmation du demandeur",
        "\u2612 Je confirme que cette fiche reflète fidèlement le besoin que je souhaite soumettre à validation.",
        "Date de confirmation : 20/11/2026",
        "Circuit de validation \u2014 suivi dans le portail",
        "Le chef de service est identifié à partir du référentiel organisationnel ou sélectionné parmi les responsables habilités. La décision est enregistrée dans le portail, avec sa date, son auteur et ses commentaires.",
        "Note : cette section est informative dans le document exporté ; le portail constitue la source de référence des validations et de leur historique.",
        "Étape",
        "Décision / statut",
        "Observations",
        "Chef de service",
        "Validée",
        "Dominique Bernard, le 20/11/2026",
        "Direction métier",
        "Selon circuit applicable",
        "",
        "SICCRF",
        "À instruire / En étude / Arbitrée",
        "",
    ];

    /// <summary>core.xml attendu pour <see cref="Contenu"/>, écrit à la main.</summary>
    public const string CoreAttendu =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n"
        + "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
        + "<dc:title>Expression de besoin EB-2026-0007</dc:title><dc:creator>Camille Martin</dc:creator><cp:lastModifiedBy>Dominique Bernard</cp:lastModifiedBy><cp:revision>1</cp:revision>"
        + "<dcterms:created xsi:type=\"dcterms:W3CDTF\">2026-11-20T09:00:00Z</dcterms:created><dcterms:modified xsi:type=\"dcterms:W3CDTF\">2026-11-20T11:00:00Z</dcterms:modified>"
        + "</cp:coreProperties>";

    /// <summary>Octets du modèle FEB embarqué (ressource de l'assembly Infrastructure).</summary>
    public static byte[] GabaritEmbarque()
    {
        using var ressource = typeof(GenerateurDocumentFeb).Assembly.GetManifestResourceStream(GenerateurDocumentFeb.NomRessource)!;
        using var tampon = new MemoryStream();
        ressource.CopyTo(tampon);
        return tampon.ToArray();
    }

    /// <summary>Parties d'un paquet zip, dans l'ordre, octets décompressés.</summary>
    public static (string Nom, byte[] Octets)[] Parties(byte[] paquet)
    {
        using var archive = new ZipArchive(new MemoryStream(paquet), ZipArchiveMode.Read);
        return archive.Entries.Select(entree => (entree.FullName, Lire(entree))).ToArray();
    }

    public static byte[] Partie(byte[] paquet, string nom) => Parties(paquet).Single(partie => partie.Nom == nom).Octets;

    public static XDocument Xml(byte[] octets) => XDocument.Load(new MemoryStream(octets));

    /// <summary>Paquet zip construit dans l'essai : (nom, texte UTF-8) par partie.</summary>
    public static byte[] Paquet(params (string Nom, string Texte)[] parties)
    {
        using var tampon = new MemoryStream();
        using (var archive = new ZipArchive(tampon, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (nom, texte) in parties)
            {
                using var flux = archive.CreateEntry(nom).Open();
                flux.Write(Encoding.UTF8.GetBytes(texte));
            }
        }

        return tampon.ToArray();
    }

    /// <summary>Texte d'un paragraphe, run par run : w:t tel quel, w:br → « \n » (lecture indépendante du code testé).</summary>
    public static string TexteAvecSauts(XElement paragraphe) =>
        string.Concat(paragraphe.Elements(W + "r").SelectMany(run => run.Elements()).Select(element =>
            element.Name == W + "t" ? element.Value : element.Name == W + "br" ? "\n" : ""));

    /// <summary>
    /// word/document.xml minimal conforme au plan : chaque marqueur une fois (libellé en gras + run d'aide, ou ligne à un
    /// run), un paragraphe vide, la ligne « Chef de service » à trois cellules. <paramref name="retouche"/> l'altère.
    /// </summary>
    public static XDocument Minimal(Action<XElement>? retouche = null)
    {
        var corps = new XElement(W + "body", new XElement(W + "p"));
        foreach (var marqueur in PlanFeb.Marqueurs)
        {
            corps.Add(marqueur.Forme == Forme.Libelle
                ? new XElement(W + "p", RunGras(marqueur.Texte), Run("aide"))
                : new XElement(W + "p", Run(marqueur.Texte + "aide")));
        }

        corps.Add(new XElement(W + "tbl", LigneChef()));
        retouche?.Invoke(corps);
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), new XElement(W + "document", new XAttribute(XNamespace.Xmlns + "w", W.NamespaceName), corps));
    }

    public static XElement LigneChef() =>
        new(W + "tr",
            Cellule(Run("Chef de service")),
            Cellule(Run("statut")),
            Cellule(new XElement(W + "r", new XElement(W + "rPr"))));

    public static XElement Cellule(params object[] contenu) => new(W + "tc", new XElement(W + "p", contenu));

    public static XElement Run(string texte) => new(W + "r", new XElement(W + "t", texte));

    public static XElement RunGras(string texte) =>
        new(W + "r", new XElement(W + "rPr", new XElement(W + "b")), new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), texte));

    /// <summary>Paragraphe repéré par le début du texte de son premier run.</summary>
    public static XElement Paragraphe(XContainer racine, string debut) =>
        racine.Descendants(W + "p").Single(paragraphe =>
            paragraphe.Element(W + "r") is { } run && string.Concat(run.Elements(W + "t").Select(t => t.Value)).StartsWith(debut, StringComparison.Ordinal));

    private static byte[] Lire(ZipArchiveEntry entree)
    {
        using var flux = entree.Open();
        using var tampon = new MemoryStream();
        flux.CopyTo(tampon);
        return tampon.ToArray();
    }
}
