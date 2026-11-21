using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Web.Hebergement;

/// <summary>
/// Choix de l'adaptateur IA (DEB-Q86), sans bascule implicite : <c>IA:Fournisseur</c> vaut <see cref="Albert"/> (IA
/// réelle : adresse <c>IA:Point</c>, modèle <c>IA:Modele</c>, clé dans la variable d'environnement
/// <see cref="VariableCle"/>) ou <see cref="Scenarise"/> (démonstration hors réseau, DEB-Q41). Valeur absente, inconnue ou
/// incomplète → arrêt au démarrage, message explicite qui ne cite jamais la clé.
/// </summary>
public static class CompositionIA
{
    public const string CleFournisseur = "IA:Fournisseur";

    public const string ClePoint = "IA:Point";

    public const string CleModele = "IA:Modele";

    /// <summary>Variable d'environnement de la clé, la même que CSAgent ; lue par la configuration .NET.</summary>
    public const string VariableCle = "ALBERT_API_KEY";

    public const string Albert = "Albert";

    public const string Scenarise = "Scenarise";

    /// <summary>
    /// Adaptateur choisi par la configuration finale (lu à la résolution, vérifié au démarrage) ; <paramref name="journal"/>
    /// reçoit le diagnostic d'une réponse vide de l'IA réelle (DEB-Q114).
    /// </summary>
    public static IAssistantIA Creer(IConfiguration configuration, ILogger journal) =>
        Lire(configuration[CleFournisseur], configuration[ClePoint], configuration[CleModele], configuration[VariableCle]) is { } options
            ? (IAssistantIA)AssistantIAAlbert.Creer(options, journal: journal)
            : new AssistantIAScenarise();

    /// <summary>Options de l'IA réelle, <c>null</c> pour l'IA scénarisée ; configuration fautive → exception.</summary>
    internal static OptionsIA? Lire(string? fournisseur, string? point, string? modele, string? cle)
    {
        if (string.Equals(fournisseur, Scenarise, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.Equals(fournisseur, Albert, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Configuration {CleFournisseur} : « {Albert} » ou « {Scenarise} » attendu (reçu « {fournisseur} ») ; "
                + "voir appsettings.{environnement}.json.");
        }

        if (!Uri.TryCreate(point, UriKind.Absolute, out var adresse) || (adresse.Scheme != Uri.UriSchemeHttps && adresse.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                $"Configuration {ClePoint} : adresse http(s) absolue de l'API compatible OpenAI attendue (reçu « {point} »).");
        }

        if (string.IsNullOrWhiteSpace(modele))
        {
            throw new InvalidOperationException($"Configuration {CleModele} : nom du modèle attendu (ex. openweight-medium).");
        }

        if (string.IsNullOrWhiteSpace(cle))
        {
            throw new InvalidOperationException(
                $"Variable d'environnement {VariableCle} absente ou vide : clé d'Albert API attendue (variable utilisateur "
                + $"Windows, puis relancer Visual Studio), ou {CleFournisseur} = {Scenarise}.");
        }

        return new OptionsIA(adresse, modele.Trim(), cle.Trim());
    }
}
