using System.Globalization;

namespace ExpressionBesoin.Domain.Fiches;

/// <summary>Numéro de fiche <c>EB-AAAA-NNNN</c>, attribué à la première soumission (plan §2).</summary>
public static class Numerotation
{
    /// <summary>
    /// Numéro suivant pour l'année <paramref name="annee"/>, d'après le dernier numéro attribué, toutes années
    /// confondues (<c>null</c> si aucun) : rang 0001 pour une année nouvelle, sinon rang + 1.
    /// </summary>
    public static Resultat<string> Suivant(int annee, string? dernier)
    {
        if (annee < 1 || annee > 9999)
        {
            return new Refus("numerotation.annee", $"Année hors du format AAAA : {annee}.");
        }

        if (dernier is null)
        {
            return Resultat.Succes(Formater(annee, 1));
        }

        if (!EssayerLire(dernier, out var anneeDernier, out var rangDernier))
        {
            return new Refus("numerotation.illisible", $"Dernier numéro illisible : « {dernier} ».");
        }

        if (anneeDernier > annee)
        {
            return new Refus("numerotation.anneeAnterieure", $"L'année {annee} précède celle du dernier numéro {dernier}.");
        }

        if (anneeDernier < annee)
        {
            return Resultat.Succes(Formater(annee, 1));
        }

        if (rangDernier == 9999)
        {
            return new Refus("numerotation.epuisee", $"Plus aucun numéro disponible pour {annee}.");
        }

        return Resultat.Succes(Formater(annee, rangDernier + 1));
    }

    internal static bool EssayerLire(string numero, out int annee, out int rang)
    {
        annee = 0;
        rang = 0;
        return numero.Length == 12
            && numero.StartsWith("EB-", StringComparison.Ordinal)
            && numero[7] == '-'
            && int.TryParse(numero.AsSpan(3, 4), NumberStyles.None, CultureInfo.InvariantCulture, out annee)
            && int.TryParse(numero.AsSpan(8, 4), NumberStyles.None, CultureInfo.InvariantCulture, out rang)
            && rang >= 1;
    }

    private static string Formater(int annee, int rang) =>
        string.Create(CultureInfo.InvariantCulture, $"EB-{annee:D4}-{rang:D4}");
}
