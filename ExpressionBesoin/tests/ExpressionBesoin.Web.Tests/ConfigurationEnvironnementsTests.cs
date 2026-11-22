using ExpressionBesoin.Infrastructure.IA;
using ExpressionBesoin.Web.Hebergement;
using Microsoft.Extensions.Configuration;

namespace ExpressionBesoin.Web.Tests;

/// <summary>
/// Dossier de données et IA fixés par environnement (DEB-Q54, DEB-Q55 ; DEB-Q126 pour l'IA) : chaque fichier
/// <c>appsettings.{environnement}.json</c> du projet Web porte un chemin ABSOLU de base, distinct des autres, et le profil
/// de l'IA ; <c>appsettings.json</c> ne porte que le catalogue des profils (ni emplacement ni profil implicite sur un
/// serveur oublié), aucun fichier ne porte de clé. Fichiers lus depuis les sources, trouvés en remontant jusqu'à la
/// solution.
/// </summary>
public sealed class ConfigurationEnvironnementsTests
{
    [Theory]
    [InlineData("Localhost", @"C:\Dev\Demonstrateur-data\fiches.db")]
    [InlineData("Staging", @"D:\Donnees\Demonstrateur-data-staging\fiches.db")]
    [InlineData("Production", @"D:\Donnees\Demonstrateur-data\fiches.db")]
    public void Chaque_environnement_fixe_son_chemin_de_base(string environnement, string attendu)
    {
        var configure = Lire($"appsettings.{environnement}.json")[CompositionFiches.CleBase];

        Assert.Equal(attendu, configure);
        Assert.Equal(attendu, CompositionFiches.CheminBase(configure));
    }

    [Fact]
    public void Configuration_commune_sans_chemin_de_base() =>
        Assert.Null(Lire("appsettings.json")[CompositionFiches.CleBase]);

    /// <summary>
    /// Profil de l'IA par environnement (lot A6, DEB-Q126, révise DEB-Q86) : chaque environnement choisit le profil
    /// <c>Albert</c>, modèle <c>gemma-4-31b-it</c> figé par son nom exact (DEB-Q104) ; aucun fichier ne porte de clé.
    /// </summary>
    [Theory]
    [InlineData("Localhost")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Chaque_environnement_choisit_son_profil(string environnement)
    {
        var seul = Lire($"appsettings.{environnement}.json");
        var configuration = Lire("appsettings.json", $"appsettings.{environnement}.json");

        Assert.Equal("Albert", seul[CompositionIA.CleProfil]);
        Assert.Empty(seul.GetSection(CompositionIA.CleProfils).GetChildren());
        Assert.Equal(((string?)null, (string?)null), (seul["ALBERT_API_KEY"], seul["ANTHROPIC_API_KEY"]));
        Assert.Equal("gemma-4-31b-it", Profil(configuration, configuration[CompositionIA.CleProfil]!)?.Modele);
    }

    /// <summary>
    /// Catalogue des profils, une seule fois dans <c>appsettings.json</c> (DEB-Q126) : Albert (température 0,1, DEB-Q84),
    /// Claude par la couche compatible OpenAI d'Anthropic (sans température : refusée sous 1 par Claude 4.7 et suivants ;
    /// plafond de jetons, DEB-Q105 à DEB-Q107, DEB-Q128), Scénarisé (hors réseau) ; clé lue dans la variable du profil.
    /// </summary>
    [Fact]
    public void Catalogue_des_profils()
    {
        var configuration = Lire("appsettings.json");

        var albert = Profil(configuration, "Albert");
        var claude = Profil(configuration, "Claude");

        Assert.NotNull(albert);
        Assert.NotNull(claude);
        Assert.Equal(
            (new Uri("https://albert.api.etalab.gouv.fr/v1"), "gemma-4-31b-it", "cle-albert", (float?)0.1f, (int?)null, "whisper-large-v3"),
            (albert.Point, albert.Modele, albert.Cle, albert.Temperature, albert.JetonsMax, albert.ModeleTranscription));
        Assert.Equal(
            (new Uri("https://api.anthropic.com/v1"), "claude-sonnet-5-5", "cle-claude", (float?)null, (int?)4096, (string?)null),
            (claude.Point, claude.Modele, claude.Cle, claude.Temperature, claude.JetonsMax, claude.ModeleTranscription));
        Assert.Null(Profil(configuration, CompositionIA.Scenarise));
    }

    [Fact]
    public void Configuration_commune_sans_profil_courant_ni_cle()
    {
        var configuration = Lire("appsettings.json");
        Assert.Equal(
            ((string?)null, (string?)null, (string?)null),
            (configuration[CompositionIA.CleProfil], configuration["ALBERT_API_KEY"], configuration["ANTHROPIC_API_KEY"]));
    }

    /// <summary>
    /// Options du profil <paramref name="profil"/> lues dans <paramref name="configuration"/>, avec de fausses clés
    /// (construites par concaténation, règles §13) dans les variables des deux profils réels.
    /// </summary>
    private static OptionsIA? Profil(IConfiguration configuration, string profil)
    {
        var essai = new Dictionary<string, string?>
        {
            [CompositionIA.CleProfil] = profil,
            ["ALBERT_API_KEY"] = "cle-" + "albert",
            ["ANTHROPIC_API_KEY"] = "cle-" + "claude",
        };
        return CompositionIA.Lire(cle => essai.TryGetValue(cle, out var valeur) ? valeur : configuration[cle]);
    }

    /// <summary>Fichiers lus dans l'ordre : le suivant remplace le précédent, comme au démarrage du site.</summary>
    private static IConfigurationRoot Lire(params string[] noms)
    {
        var constructeur = new ConfigurationBuilder().SetBasePath(DossierWeb());
        foreach (var nom in noms)
        {
            constructeur.AddJsonFile(nom, optional: false);
        }

        return constructeur.Build();
    }

    private static string DossierWeb()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !File.Exists(Path.Combine(dossier.FullName, "ExpressionBesoin.slnx")))
        {
            dossier = dossier.Parent;
        }

        Assert.NotNull(dossier);
        return Path.Combine(dossier.FullName, "src", "ExpressionBesoin.Web");
    }
}
