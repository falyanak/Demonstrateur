using System.Collections.Frozen;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Web.Api;

/// <summary>Correspondance refus → HTTP et lecture des requêtes (DEB-Q42), fonctions pures.</summary>
public static class StatutHttp
{
    /// <summary>Code de la panne d'un port (DEB-Q22) : 503, jamais un refus métier.</summary>
    public const string Panne = "panne.technique";

    /// <summary><c>*.introuvable</c> → 404 ; <c>document.nonValidee</c> → 409 ; tout autre refus → 422.</summary>
    public static int De(string code) =>
        code.EndsWith(".introuvable", StringComparison.Ordinal) ? StatusCodes.Status404NotFound
        : code == "document.nonValidee" ? StatusCodes.Status409Conflict
        : StatusCodes.Status422UnprocessableEntity;

    // Noms seuls, casse ignorée : ni valeur numérique (« 2 ») ni combinaison (« Valider,Refuser ») qu'accepterait Enum.Parse.
    private static readonly FrozenDictionary<string, TypeDecision> Decisions =
        Enum.GetValues<TypeDecision>().ToFrozenDictionary(type => type.ToString(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Décision simulée par son nom (<c>Valider</c>, <c>DemanderComplements</c>, <c>Refuser</c>), casse ignorée.</summary>
    public static Resultat<TypeDecision> LireDecision(string? nom) =>
        nom is not null && Decisions.TryGetValue(nom, out var type)
            ? Resultat.Succes(type)
            : new Refus("decision.inconnue", $"Décision inconnue : « {nom} » (Valider, DemanderComplements ou Refuser).");
}
