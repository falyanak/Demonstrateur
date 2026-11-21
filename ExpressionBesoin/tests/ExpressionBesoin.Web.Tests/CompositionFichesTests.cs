using ExpressionBesoin.Web.Hebergement;

namespace ExpressionBesoin.Web.Tests;

public sealed class CompositionFichesTests
{
    [Fact]
    public void Chemin_absolu_garde()
    {
        var chemin = Path.Combine(Path.GetTempPath(), "DEB", "fiches.db");
        Assert.Equal(chemin, CompositionFiches.CheminBase(chemin));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fiches.db")]
    [InlineData("donnees/fiches.db")]
    public void Chemin_absent_ou_relatif_arret_explicite(string? configure)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CompositionFiches.CheminBase(configure));
        Assert.Equal(
            $"Configuration Fiches:Base : chemin ABSOLU du fichier SQLite attendu (reçu « {configure} ») ; voir appsettings.{{environnement}}.json (Localhost, Staging, Production) ou la variable Fiches__Base.",
            exception.Message);
    }

    [Fact]
    public void Demarrage_sans_base_configuree_s_arrete()
    {
        using var usine = new UsineSansBase();
        var exception = Record.Exception(() => usine.CreateClient());
        Assert.NotNull(exception);
        Assert.Contains("Configuration Fiches:Base", exception.ToString());
    }

    [Fact]
    public void Fuseau_et_cle_de_configuration() =>
        Assert.Equal(("Europe/Paris", "Fiches:Base"), (CompositionFiches.Fuseau, CompositionFiches.CleBase));
}
