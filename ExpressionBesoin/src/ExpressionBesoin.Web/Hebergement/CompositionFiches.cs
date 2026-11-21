using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Infrastructure.Documents;
using ExpressionBesoin.Infrastructure.Fiches;
using ExpressionBesoin.Infrastructure.IA;
using ExpressionBesoin.Infrastructure.Organisation;
using ExpressionBesoin.Infrastructure.Sqlite;

namespace ExpressionBesoin.Web.Hebergement;

/// <summary>
/// Composition des adaptateurs des fiches (DEB-Q31, DEB-Q32), sans décision métier. Tout est lu à la RÉSOLUTION, donc
/// sur la configuration finale (celle des tests comprise, règles, annexe « Config .NET »), et vérifié au démarrage
/// (<see cref="Initialiser"/>) : une base inaccessible ou un gabarit FEB écarté arrêtent le site au lancement, jamais
/// au premier clic.
/// </summary>
public static class CompositionFiches
{
    /// <summary>Clé du chemin de la base SQLite (<c>appsettings.Localhost.json</c>), variables d'environnement développées.</summary>
    public const string CleBase = "Fiches:Base";

    /// <summary>Fuseau de l'année du numéro et des dates du document (DEB-Q24, DEB-Q36) : identifiant IANA.</summary>
    public const string Fuseau = "Europe/Paris";

    public static IServiceCollection AjouterFiches(this IServiceCollection services)
    {
        services.AddSingleton(fournisseur => new OptionsSqlite(CheminBase(fournisseur.GetRequiredService<IConfiguration>()[CleBase])));
        services.AddSingleton(fournisseur => new DepotFichesSqlite(fournisseur.GetRequiredService<OptionsSqlite>()));
        services.AddSingleton<IDepotFiches>(fournisseur => fournisseur.GetRequiredService<DepotFichesSqlite>());
        services.AddSingleton<IReferentielOrganisation, ReferentielOrganisationFictif>();
        // Un seul exemplaire : le gabarit est lu, décompressé et vérifié une fois (plan §4, performance).
        services.AddSingleton<IGenerateurDocument>(_ => GenerateurDocumentFeb.DepuisRessource());
        // IA scénarisée jusqu'au lot A1 (DEB-Q41) : démonstration jouable sans réseau.
        services.AddSingleton<IAssistantIA, AssistantIAScenarise>();
        services.AddSingleton(fournisseur => new ServiceFiches(
            fournisseur.GetRequiredService<IDepotFiches>(),
            fournisseur.GetRequiredService<IAssistantIA>(),
            fournisseur.GetRequiredService<IGenerateurDocument>(),
            fournisseur.GetRequiredService<IReferentielOrganisation>(),
            fournisseur.GetRequiredService<TimeProvider>(),
            TimeZoneInfo.FindSystemTimeZoneById(Fuseau),
            // Version 7 : identifiants croissants dans le temps, insertions groupées en fin d'index.
            Guid.CreateVersion7));
        return services;
    }

    /// <summary>Au démarrage : dossier de la base créé, schéma initialisé, gabarit FEB chargé et vérifié.</summary>
    public static void Initialiser(IServiceProvider services)
    {
        var options = services.GetRequiredService<OptionsSqlite>();
        Directory.CreateDirectory(Path.GetDirectoryName(options.Chemin)!);
        services.GetRequiredService<DepotFichesSqlite>().Initialiser();
        services.GetRequiredService<IGenerateurDocument>();
    }

    /// <summary>Chemin configuré, variables développées (<c>%LOCALAPPDATA%</c>) ; absent ou relatif → arrêt explicite.</summary>
    internal static string CheminBase(string? configure)
    {
        var chemin = Environment.ExpandEnvironmentVariables(configure ?? "");
        return Path.IsPathFullyQualified(chemin)
            ? chemin
            : throw new InvalidOperationException(
                $"Configuration {CleBase} : chemin ABSOLU du fichier SQLite attendu (reçu « {configure} ») ; voir appsettings.Localhost.json.");
    }
}
