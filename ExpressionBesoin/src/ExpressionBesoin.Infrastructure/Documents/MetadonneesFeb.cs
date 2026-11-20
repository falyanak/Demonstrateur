using System.Globalization;
using System.Text;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Infrastructure.Documents;

/// <summary>
/// Partie <c>docProps/core.xml</c> du paquet (DEB-Q37) : titre, demandeur, valideur et dates tirés du contenu, jamais de
/// l'horloge ; aucune donnée héritée du modèle d'origine. Fonction pure.
/// </summary>
internal static class MetadonneesFeb
{
    public static string Core(ContenuDocument contenu) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n"
        + "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\""
        + " xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\""
        + " xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
        + "<dc:title>Expression de besoin " + Echapper(contenu.Numero) + "</dc:title>"
        + "<dc:creator>" + Echapper(contenu.Demandeur) + "</dc:creator>"
        + "<cp:lastModifiedBy>" + Echapper(contenu.Valideur) + "</cp:lastModifiedBy>"
        + "<cp:revision>1</cp:revision>"
        + "<dcterms:created xsi:type=\"dcterms:W3CDTF\">" + Instant(contenu.CreeeLe) + "</dcterms:created>"
        + "<dcterms:modified xsi:type=\"dcterms:W3CDTF\">" + Instant(contenu.ModifieeLe) + "</dcterms:modified>"
        + "</cp:coreProperties>";

    public static byte[] Octets(ContenuDocument contenu) => Encoding.UTF8.GetBytes(Core(contenu));

    /// <summary>Contenu d'élément XML : « & », « < », « > » échappés ; caractères interdits retirés.</summary>
    private static string Echapper(string texte) =>
        GabaritFeb.TexteValide(texte).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string Instant(DateTimeOffset instant) =>
        instant.UtcDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
}
