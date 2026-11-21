using ExpressionBesoin.Web.Hebergement;
using Microsoft.Extensions.Configuration;

namespace ExpressionBesoin.Web.Tests;

/// <summary>
/// Dossier de données fixé par environnement (DEB-Q54, DEB-Q55) : chaque fichier <c>appsettings.{environnement}.json</c>
/// du projet Web porte un chemin ABSOLU de base, distinct des autres ; <c>appsettings.json</c> n'en porte aucun (pas
/// d'emplacement implicite sur un serveur oublié). Fichiers lus depuis les sources, trouvés en remontant jusqu'à la
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

    private static IConfigurationRoot Lire(string nom) =>
        new ConfigurationBuilder().SetBasePath(DossierWeb()).AddJsonFile(nom, optional: false).Build();

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
