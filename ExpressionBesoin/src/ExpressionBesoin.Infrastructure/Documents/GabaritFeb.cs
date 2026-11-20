using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Infrastructure.Documents;

/// <summary>
/// Partie <c>word/document.xml</c> du modèle FEB : vérification, remplissage, sérialisation (DEB-Q35). Fonctions pures,
/// sans E/S ; le gabarit reçu n'est jamais modifié (le remplissage travaille sur une copie).
/// </summary>
internal static class GabaritFeb
{
    public static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    private static readonly string[] SautsDeLigne = ["\r\n", "\n", "\r"];

    private static readonly XmlWriterSettings Ecriture = new() { Encoding = new UTF8Encoding(false), Indent = false };

    /// <summary>
    /// Écarts du gabarit au plan (<see cref="PlanFeb.Marqueurs"/>) : liste vide = gabarit utilisable. Chaque marqueur
    /// repère exactement un paragraphe ; un libellé est le premier run exact, suivi d'au moins un run ; la ligne
    /// « Chef de service » existe une seule fois, avec au moins trois cellules, les deux dernières pourvues d'un run.
    /// </summary>
    public static ImmutableArray<string> Anomalies(XDocument gabarit)
    {
        var anomalies = ImmutableArray.CreateBuilder<string>();
        var paragraphes = gabarit.Descendants(W + "p").ToArray();
        foreach (var marqueur in PlanFeb.Marqueurs)
        {
            var trouves = paragraphes.Where(paragraphe => Repere(paragraphe, marqueur)).ToArray();
            if (trouves.Length != 1)
            {
                anomalies.Add($"Marqueur « {marqueur.Texte} » trouvé {trouves.Length} fois (attendu : 1).");
            }
            else if (marqueur.Forme == Forme.Libelle && !EstLibelle(trouves[0], marqueur))
            {
                anomalies.Add($"Libellé « {marqueur.Texte} » sans run de valeur à sa suite.");
            }
        }

        var lignes = LignesChef(gabarit).ToArray();
        if (lignes.Length != 1)
        {
            anomalies.Add($"Ligne « {PlanFeb.LigneChef} » trouvée {lignes.Length} fois (attendu : 1).");
        }
        else if (!CellulesChefUtilisables(lignes[0]))
        {
            anomalies.Add($"Ligne « {PlanFeb.LigneChef} » : trois cellules attendues, les deux dernières avec un run.");
        }

        return anomalies.ToImmutable();
    }

    /// <summary>
    /// Copie du gabarit remplie avec <paramref name="contenu"/>. Précondition : <see cref="Anomalies"/> vide (sinon
    /// <see cref="InvalidOperationException"/>). Seuls les paragraphes repérés et les deux cellules de la ligne du chef
    /// changent ; un libellé garde son run en gras.
    /// </summary>
    public static XDocument Remplir(XDocument gabarit, ContenuDocument contenu)
    {
        var document = new XDocument(gabarit);
        var paragraphes = document.Descendants(W + "p").ToArray();
        foreach (var remplacement in PlanFeb.Remplacements(contenu))
        {
            var runs = Runs(paragraphes.Single(paragraphe => Repere(paragraphe, remplacement.Marqueur)));
            var garde = remplacement.Marqueur.Forme == Forme.Libelle ? 1 : 0;
            EcrireTexte(runs[garde], remplacement.Texte);
            foreach (var surplus in runs.Skip(garde + 1))
            {
                surplus.Remove();
            }
        }

        var cellules = LignesChef(document).Single().Elements(W + "tc").ToArray();
        EcrireTexte(cellules[1].Descendants(W + "r").First(), contenu.DecisionChef);
        EcrireTexte(cellules[2].Descendants(W + "r").First(), contenu.ObservationsChef);
        return document;
    }

    /// <summary>UTF-8 sans BOM, sans indentation : mêmes entrées, mêmes octets (DEB-Q37).</summary>
    public static byte[] Octets(XDocument document)
    {
        using var tampon = new MemoryStream();
        using (var ecrivain = XmlWriter.Create(tampon, Ecriture))
        {
            document.Save(ecrivain);
        }

        return tampon.ToArray();
    }

    /// <summary>
    /// Texte d'un run : propriétés (<c>w:rPr</c>) gardées, contenu remplacé ; un saut de ligne devient <c>w:br</c> ; les
    /// caractères interdits en XML sont retirés (<see cref="TexteValide"/>).
    /// </summary>
    public static void EcrireTexte(XElement run, string texte)
    {
        run.Elements().Where(element => element.Name != W + "rPr").Remove();
        var lignes = TexteValide(texte).Split(SautsDeLigne, StringSplitOptions.None);
        for (var index = 0; index < lignes.Length; index++)
        {
            if (index > 0)
            {
                run.Add(new XElement(W + "br"));
            }

            run.Add(new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), lignes[index]));
        }
    }

    /// <summary>Texte sans les caractères interdits en XML 1.0 (contrôles, demi-paires de substitution isolées).</summary>
    public static string TexteValide(string texte)
    {
        var resultat = new StringBuilder(texte.Length);
        for (var index = 0; index < texte.Length; index++)
        {
            var caractere = texte[index];
            if (XmlConvert.IsXmlChar(caractere))
            {
                resultat.Append(caractere);
            }
            else if (index + 1 < texte.Length && XmlConvert.IsXmlSurrogatePair(texte[index + 1], caractere))
            {
                resultat.Append(caractere).Append(texte[index + 1]);
                index++;
            }
        }

        return resultat.ToString();
    }

    /// <summary>Texte d'un élément : ses <c>w:t</c> mis bout à bout.</summary>
    public static string Texte(XElement element) => string.Concat(element.Descendants(W + "t").Select(texte => texte.Value));

    private static XElement[] Runs(XElement paragraphe) => paragraphe.Elements(W + "r").ToArray();

    private static bool Repere(XElement paragraphe, Marqueur marqueur) =>
        paragraphe.Element(W + "r") is { } premier && Texte(premier).StartsWith(marqueur.Texte, StringComparison.Ordinal);

    private static bool EstLibelle(XElement paragraphe, Marqueur marqueur)
    {
        var runs = Runs(paragraphe);
        return runs.Length >= 2 && Texte(runs[0]) == marqueur.Texte;
    }

    private static IEnumerable<XElement> LignesChef(XDocument document) =>
        document.Descendants(W + "tr").Where(ligne => ligne.Element(W + "tc") is { } premiere && Texte(premiere) == PlanFeb.LigneChef);

    private static bool CellulesChefUtilisables(XElement ligne)
    {
        var cellules = ligne.Elements(W + "tc").ToArray();
        return cellules.Length >= 3 && cellules[1].Descendants(W + "r").Any() && cellules[2].Descendants(W + "r").Any();
    }
}
