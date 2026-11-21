using ExpressionBesoin.Web.Hebergement;
using Microsoft.Extensions.Configuration;

namespace ExpressionBesoin.Web.Tests;

/// <summary>
/// Dossier de données et IA fixés par environnement (DEB-Q54, DEB-Q55, DEB-Q86) : chaque fichier
/// <c>appsettings.{environnement}.json</c> du projet Web porte un chemin ABSOLU de base, distinct des autres, et le choix
/// de l'IA ; <c>appsettings.json</c> n'en porte aucun (pas d'emplacement ni de fournisseur implicite sur un serveur
/// oublié), aucun fichier ne porte la clé. Fichiers lus depuis les sources, trouvés en remontant jusqu'à la solution.
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

    /// <summary>IA réelle (lot A1, DEB-Q84, DEB-Q86) : Albert API, modèle mesuré par la sonde du 21/11/2026.</summary>
    [Theory]
    [InlineData("Localhost")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Chaque_environnement_fixe_son_ia(string environnement)
    {
        var configuration = Lire($"appsettings.{environnement}.json");
        var lu = (configuration[CompositionIA.CleFournisseur], configuration[CompositionIA.ClePoint], configuration[CompositionIA.CleModele]);

        Assert.Equal(("Albert", "https://albert.api.etalab.gouv.fr/v1", "openweight-medium"), lu);
        Assert.Null(configuration[CompositionIA.VariableCle]);
        Assert.NotNull(CompositionIA.Lire(lu.Item1, lu.Item2, lu.Item3, "cle-" + "essai"));
    }

    [Fact]
    public void Configuration_commune_sans_ia_ni_cle()
    {
        var configuration = Lire("appsettings.json");
        Assert.Equal(((string?)null, (string?)null), (configuration[CompositionIA.CleFournisseur], configuration[CompositionIA.VariableCle]));
    }

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
