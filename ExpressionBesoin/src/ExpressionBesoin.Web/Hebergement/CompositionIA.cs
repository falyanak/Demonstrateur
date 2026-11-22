using System.Globalization;
using ExpressionBesoin.Application.Dictee;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Web.Hebergement;

/// <summary>
/// Choix de l'adaptateur IA par profils nommés (lot A6, DEB-D8 ; DEB-Q105 et DEB-Q126 révisent DEB-Q86) :
/// <see cref="CleProfil"/> (un par environnement, remplaçable par la variable <c>IA__Profil</c>) désigne un profil du
/// catalogue <see cref="CleProfils"/> (<c>appsettings.json</c>). Un profil est l'IA scénarisée
/// (<see cref="Fournisseur"/> = <see cref="Scenarise"/>, DEB-Q41) ou une API compatible OpenAI : <see cref="Point"/>,
/// <see cref="Modele"/>, <see cref="VariableCle"/> (NOM de la variable d'environnement qui porte la clé),
/// <see cref="Temperature"/> et <see cref="JetonsMax"/> facultatifs, non envoyés si absents ; <see cref="ModeleTranscription"/>
/// facultatif (dictée, lot S1, DEB-Q132 : absent → dictée indisponible). Seul le profil courant est
/// vérifié ; aucune bascule implicite : valeur absente, inconnue ou incomplète → arrêt au démarrage, message explicite qui
/// ne cite jamais la clé.
/// </summary>
public static class CompositionIA
{
    public const string CleProfil = "IA:Profil";

    public const string CleProfils = "IA:Profils";

    public const string Fournisseur = "Fournisseur";

    public const string Point = "Point";

    public const string Modele = "Modele";

    public const string VariableCle = "VariableCle";

    public const string Temperature = "Temperature";

    public const string JetonsMax = "JetonsMax";

    public const string ModeleTranscription = "ModeleTranscription";

    public const string Scenarise = "Scenarise";

    /// <summary>
    /// Adaptateur choisi par la configuration finale (lu à la résolution, vérifié au démarrage) ; <paramref name="journal"/>
    /// reçoit le diagnostic d'une réponse vide de l'IA réelle (DEB-Q114).
    /// </summary>
    public static IAssistantIA Creer(IConfiguration configuration, ILogger journal) =>
        Lire(cle => configuration[cle]) is { } options
            ? (IAssistantIA)AssistantIAAlbert.Creer(options, journal: journal)
            : new AssistantIAScenarise();

    /// <summary>
    /// Dictée du profil courant (lot S1, DEB-Q132) : transcripteur Whisper sur le point et la clé du profil s'il porte un
    /// <see cref="ModeleTranscription"/>, sinon aucun (IA scénarisée comprise) : bouton masqué, route refusée.
    /// </summary>
    public static ServiceDictee CreerDictee(IConfiguration configuration) =>
        new(Lire(cle => configuration[cle]) is { ModeleTranscription: { } modele } options
            ? TranscripteurWhisper.Creer(options, modele)
            : null);

    /// <summary>
    /// Options du profil courant, <c>null</c> pour l'IA scénarisée ; configuration fautive → exception.
    /// <paramref name="configuration"/> rend la valeur d'une clé complète (<c>IA:Profils:Albert:Point</c>, nom d'une
    /// variable d'environnement), <c>null</c> si elle est absente.
    /// </summary>
    internal static OptionsIA? Lire(Func<string, string?> configuration)
    {
        var profil = configuration(CleProfil)?.Trim();
        if (string.IsNullOrEmpty(profil))
        {
            throw new InvalidOperationException(
                $"Configuration {CleProfil} : nom d'un profil de {CleProfils} attendu (reçu « {profil} ») ; "
                + "voir appsettings.{environnement}.json.");
        }

        var prefixe = $"{CleProfils}:{profil}:";
        var fournisseur = configuration(prefixe + Fournisseur);
        var point = configuration(prefixe + Point);
        var modele = configuration(prefixe + Modele);
        var variable = configuration(prefixe + VariableCle);
        if (fournisseur is null && point is null && modele is null && variable is null)
        {
            throw new InvalidOperationException(
                $"Configuration {CleProfil} : profil « {profil} » absent de {CleProfils} (catalogue dans appsettings.json).");
        }

        if (fournisseur is not null)
        {
            return string.Equals(fournisseur.Trim(), Scenarise, StringComparison.OrdinalIgnoreCase)
                ? null
                : throw new InvalidOperationException(
                    $"Configuration {prefixe}{Fournisseur} : « {Scenarise} » ou absent (API compatible OpenAI) attendu "
                    + $"(reçu « {fournisseur} »).");
        }

        if (!Uri.TryCreate(point, UriKind.Absolute, out var adresse) || (adresse.Scheme != Uri.UriSchemeHttps && adresse.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                $"Configuration {prefixe}{Point} : adresse http(s) absolue de l'API compatible OpenAI attendue (reçu « {point} »).");
        }

        if (string.IsNullOrWhiteSpace(modele))
        {
            throw new InvalidOperationException($"Configuration {prefixe}{Modele} : nom du modèle attendu (ex. gemma-4-31b-it).");
        }

        if (string.IsNullOrWhiteSpace(variable))
        {
            throw new InvalidOperationException(
                $"Configuration {prefixe}{VariableCle} : nom de la variable d'environnement de la clé attendu (ex. ALBERT_API_KEY).");
        }

        var nomVariable = variable.Trim();
        var cle = configuration(nomVariable);
        if (string.IsNullOrWhiteSpace(cle))
        {
            throw new InvalidOperationException(
                $"Variable d'environnement {nomVariable} absente ou vide : clé du profil « {profil} » attendue (variable "
                + $"utilisateur Windows, puis relancer Visual Studio), ou {CleProfil} = {Scenarise}.");
        }

        return new OptionsIA(adresse, modele.Trim(), cle.Trim())
        {
            Temperature = LireTemperature(configuration(prefixe + Temperature), prefixe),
            JetonsMax = LireJetonsMax(configuration(prefixe + JetonsMax), prefixe),
            ModeleTranscription = LireModeleTranscription(configuration(prefixe + ModeleTranscription), prefixe),
        };
    }

    /// <summary>Absent → <c>null</c> (dictée indisponible) ; sinon nom du modèle, sans espaces de bord, jamais blanc.</summary>
    private static string? LireModeleTranscription(string? lu, string prefixe)
    {
        if (lu is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(lu)
            ? throw new InvalidOperationException(
                $"Configuration {prefixe}{ModeleTranscription} : nom du modèle de transcription attendu (ex. whisper-large-v3), "
                + "ou absent (dictée indisponible).")
            : lu.Trim();
    }

    /// <summary>Absente → <c>null</c> (non envoyée) ; sinon nombre de 0 à 2, point décimal (NaN refusé).</summary>
    private static float? LireTemperature(string? lue, string prefixe)
    {
        if (lue is null)
        {
            return null;
        }

        if (!float.TryParse(lue, NumberStyles.Float, CultureInfo.InvariantCulture, out var valeur) || valeur is not (>= 0 and <= 2))
        {
            throw new InvalidOperationException(
                $"Configuration {prefixe}{Temperature} : nombre de 0 à 2 attendu, point décimal (reçu « {lue} »), ou absente "
                + "(non envoyée).");
        }

        return valeur;
    }

    /// <summary>Absent → <c>null</c> (non envoyé) ; sinon entier strictement positif, chiffres seuls.</summary>
    private static int? LireJetonsMax(string? lu, string prefixe)
    {
        if (lu is null)
        {
            return null;
        }

        if (!int.TryParse(lu, NumberStyles.None, CultureInfo.InvariantCulture, out var nombre) || nombre == 0)
        {
            throw new InvalidOperationException(
                $"Configuration {prefixe}{JetonsMax} : entier positif attendu (reçu « {lu} »), ou absent (non envoyé).");
        }

        return nombre;
    }
}
