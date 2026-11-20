using ExpressionBesoin.Web.Hebergement;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ExpressionBesoin.Web.Tests;

public sealed class EnvironnementExtensionsTests
{
    [Theory]
    [InlineData("Localhost", true)]
    [InlineData("localhost", true)]
    [InlineData("LOCALHOST", true)]
    [InlineData("Development", false)]
    [InlineData("Production", false)]
    [InlineData("Localhost2", false)]
    [InlineData(" Localhost", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EstLocalhost_insensible_a_la_casse_et_exact(string? nom, bool attendu) =>
        Assert.Equal(attendu, EnvironnementExtensions.EstLocalhost(nom));

    [Theory]
    [InlineData("localhost", true)]
    [InlineData("Staging", false)]
    public void IsLocalhost_lit_le_nom_de_l_environnement(string nom, bool attendu) =>
        Assert.Equal(attendu, new EnvironnementEssai(nom).IsLocalhost());

    private sealed class EnvironnementEssai(string nom) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nom;
        public string ApplicationName { get; set; } = "Essai";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
