using ExpressionBesoin.Domain;

namespace ExpressionBesoin.Application;

/// <summary>Aide des coquilles : s'arrêter au premier refus entre deux entrées/sorties, sans décision.</summary>
internal static class ResultatOuvert
{
    /// <summary>Valeur (<c>null</c> sur un refus) et refus (<c>null</c> sur un succès).</summary>
    public static (T? Valeur, Refus? Refus) Ouvrir<T>(this Resultat<T> resultat)
        where T : class =>
        resultat.Match<(T?, Refus?)>(valeur => (valeur, null), refus => (null, refus));
}
