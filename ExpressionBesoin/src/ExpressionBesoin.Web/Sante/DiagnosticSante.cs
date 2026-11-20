namespace ExpressionBesoin.Web.Sante;

/// <summary>Contrat de <c>GET api/sante</c> (camelCase à la sérialisation) : exactement ces quatre champs.</summary>
public sealed record EtatSante(string Etat, string Version, DateTimeOffset Demarrage, long DureeSecondes);

/// <summary>Instant de démarrage de l'application, enregistré à la composition.</summary>
public sealed record Demarrage(DateTimeOffset Instant);

/// <summary>Version informative de l'assembly Web, enregistrée à la composition (absente : <c>null</c>).</summary>
public sealed record VersionApplication(string? Valeur);

public static class DiagnosticSante
{
    public const string VersionInconnue = "inconnue";

    /// <summary>Fonction pure : durée en secondes entières, tronquée, jamais négative.</summary>
    public static EtatSante Decrire(string? version, DateTimeOffset demarrage, DateTimeOffset maintenant)
    {
        var ticks = (maintenant - demarrage).Ticks;
        var dureeSecondes = ticks <= 0 ? 0 : ticks / TimeSpan.TicksPerSecond;
        var versionLue = string.IsNullOrWhiteSpace(version) ? VersionInconnue : version;
        return new EtatSante("ok", versionLue, demarrage, dureeSecondes);
    }
}
