namespace ExpressionBesoin.Web.Hebergement;

/// <summary>
/// Environnement « Localhost » : poste du développeur (Vite tourne toujours en local, jamais sur un serveur).
/// Comparaison insensible à la casse ; les serveurs gardent Development, Staging, Production.
/// </summary>
public static class EnvironnementExtensions
{
    public const string Localhost = "Localhost";

    public static bool IsLocalhost(this IHostEnvironment environnement) => EstLocalhost(environnement.EnvironmentName);

    internal static bool EstLocalhost(string? nomEnvironnement) =>
        string.Equals(nomEnvironnement, Localhost, StringComparison.OrdinalIgnoreCase);
}
