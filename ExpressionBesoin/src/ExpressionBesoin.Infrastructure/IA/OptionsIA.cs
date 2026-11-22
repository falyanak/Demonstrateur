namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Réglages d'une IA compatible OpenAI, tirés du profil courant (lot A6, DEB-Q105) : adresse, modèle, clé lue dans la
/// variable d'environnement du profil par la composition, délai borné (DEB-Q87), température et plafond de jetons
/// facultatifs. Une classe et non un record : un record écrirait la clé dans son <c>ToString</c>, donc dans un journal.
/// </summary>
public sealed class OptionsIA(Uri point, string modele, string cle)
{
    /// <summary>Délai d'un appel, nouvel essai compris (DEB-Q87).</summary>
    public static readonly TimeSpan DelaiParDefaut = TimeSpan.FromSeconds(60);

    public Uri Point { get; } = point;

    public string Modele { get; } = modele;

    public string Cle { get; } = cle;

    public TimeSpan Delai { get; init; } = DelaiParDefaut;

    /// <summary>
    /// Température envoyée au modèle ; <c>null</c> → champ absent de la requête : Claude 4.7 et suivants refusent toute
    /// valeur inférieure à 1 par la couche compatible OpenAI (DEB-Q105). Profil Albert : 0,1 (sonde du 21/11/2026, DEB-Q84).
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>Plafond de jetons de la réponse ; <c>null</c> → champ absent de la requête (DEB-Q128).</summary>
    public int? JetonsMax { get; init; }

    /// <summary>
    /// Modèle de transcription de la dictée (lot S1, DEB-Q132), sur le même point et avec la même clé ; <c>null</c> →
    /// dictée indisponible avec ce profil (Albert : <c>whisper-large-v3</c>, sonde du 22/11/2026).
    /// </summary>
    public string? ModeleTranscription { get; init; }
}
