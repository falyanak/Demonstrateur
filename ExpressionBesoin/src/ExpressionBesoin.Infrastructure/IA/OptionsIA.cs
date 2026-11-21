namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Réglages de l'IA réelle (DEB-Q86) : adresse compatible OpenAI, modèle (DEB-Q84), clé lue dans la variable
/// d'environnement par la composition, délai borné (DEB-Q87). Une classe et non un record : un record écrirait la clé
/// dans son <c>ToString</c>, donc dans un journal.
/// </summary>
public sealed class OptionsIA(Uri point, string modele, string cle)
{
    /// <summary>Délai d'un appel, nouvel essai compris (DEB-Q87).</summary>
    public static readonly TimeSpan DelaiParDefaut = TimeSpan.FromSeconds(60);

    public Uri Point { get; } = point;

    public string Modele { get; } = modele;

    public string Cle { get; } = cle;

    public TimeSpan Delai { get; init; } = DelaiParDefaut;
}
