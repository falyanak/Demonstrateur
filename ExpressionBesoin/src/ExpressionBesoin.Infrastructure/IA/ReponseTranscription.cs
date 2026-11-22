using System.Text.Json;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Lecture de la réponse de <c>POST /audio/transcriptions</c> au format <c>json</c> (lot S1) : <c>{ "text": … }</c>.
/// Fonction PURE ; tout autre corps rend <c>null</c>, dont l'adaptateur fait une panne (jamais un texte deviné).
/// </summary>
public static class ReponseTranscription
{
    /// <summary>Longueur au plus d'un corps cité dans un message de panne (journal du site, DEB-Q114).</summary>
    public const int ExtraitMax = 300;

    public static string? Lire(string corps)
    {
        try
        {
            using var document = JsonDocument.Parse(corps);
            var racine = document.RootElement;
            return racine.ValueKind == JsonValueKind.Object
                && racine.TryGetProperty("text", out var texte)
                && texte.ValueKind == JsonValueKind.String
                ? texte.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Début d'un corps de réponse pour un message de panne : au plus <see cref="ExtraitMax"/> caractères.</summary>
    public static string Extrait(string corps) => corps.Length <= ExtraitMax ? corps : corps[..ExtraitMax] + "…";
}
