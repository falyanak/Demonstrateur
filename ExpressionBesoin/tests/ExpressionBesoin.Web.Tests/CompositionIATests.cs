using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Infrastructure.IA;
using ExpressionBesoin.Web.Hebergement;
using Microsoft.Extensions.DependencyInjection;

namespace ExpressionBesoin.Web.Tests;

/// <summary>Choix de l'adaptateur IA (DEB-Q86) : table de vérité de <see cref="CompositionIA.Lire"/>, puis démarrage.</summary>
public sealed class CompositionIATests
{
    private const string Point = "https://albert.api.etalab.gouv.fr/v1";

    /// <summary>Fausse clé construite par concaténation : une archive ne la masque pas (règles §13).</summary>
    private static readonly string Cle = "secret-" + "essai";

    [Theory]
    [InlineData("Scenarise")]
    [InlineData("scenarise")]
    public void Scenarise_sans_options_le_reste_est_ignore(string fournisseur) =>
        Assert.Null(CompositionIA.Lire(fournisseur, null, null, null));

    [Theory]
    [InlineData("Albert", Point, "openweight-medium", "secret-essai")]
    [InlineData("albert", "http://localhost:11434/v1", "  openweight-medium ", " secret-essai ")]
    public void Albert_options_completes(string fournisseur, string point, string modele, string cle)
    {
        var options = CompositionIA.Lire(fournisseur, point, modele, cle);

        Assert.NotNull(options);
        Assert.Equal((new Uri(point), "openweight-medium", "secret-essai", TimeSpan.FromSeconds(60)), (options.Point, options.Modele, options.Cle, options.Delai));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("OpenAI")]
    public void Fournisseur_absent_ou_inconnu_arret_explicite(string? fournisseur)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CompositionIA.Lire(fournisseur, Point, "openweight-medium", Cle));
        Assert.Equal(
            $"Configuration IA:Fournisseur : « Albert » ou « Scenarise » attendu (reçu « {fournisseur} ») ; voir appsettings.{{environnement}}.json.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("albert.api.etalab.gouv.fr/v1")]
    [InlineData("ftp://albert.test/v1")]
    [InlineData(@"C:\Dev\albert")]
    public void Adresse_absente_ou_non_http_arret_explicite(string? point)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CompositionIA.Lire("Albert", point, "openweight-medium", Cle));
        Assert.Equal(
            $"Configuration IA:Point : adresse http(s) absolue de l'API compatible OpenAI attendue (reçu « {point} »).",
            exception.Message);
        Assert.DoesNotContain(Cle, exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Modele_absent_arret_explicite(string? modele)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CompositionIA.Lire("Albert", Point, modele, Cle));
        Assert.Equal("Configuration IA:Modele : nom du modèle attendu (ex. openweight-medium).", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Cle_absente_arret_explicite(string? cle)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CompositionIA.Lire("Albert", Point, "openweight-medium", cle));
        Assert.Equal(
            "Variable d'environnement ALBERT_API_KEY absente ou vide : clé d'Albert API attendue (variable utilisateur Windows, puis relancer Visual Studio), ou IA:Fournisseur = Scenarise.",
            exception.Message);
    }

    [Fact]
    public void Cles_de_configuration() =>
        Assert.Equal(
            ("IA:Fournisseur", "IA:Point", "IA:Modele", "ALBERT_API_KEY", "Albert", "Scenarise"),
            (CompositionIA.CleFournisseur, CompositionIA.ClePoint, CompositionIA.CleModele, CompositionIA.VariableCle, CompositionIA.Albert, CompositionIA.Scenarise));

    [Fact]
    public void Demarrage_ia_scenarisee_par_defaut_des_tests()
    {
        using var usine = new UsineWeb();
        Assert.IsType<AssistantIAScenarise>(usine.Services.GetRequiredService<IAssistantIA>());
    }

    [Fact]
    public void Demarrage_ia_reelle_sans_appel_reseau()
    {
        using var usine = new UsineAlbert();
        Assert.IsType<AssistantIAAlbert>(usine.Services.GetRequiredService<IAssistantIA>());
    }

    [Fact]
    public void Demarrage_ia_reelle_sans_cle_s_arrete()
    {
        using var usine = new UsineAlbertSansCle();
        var exception = Record.Exception(() => usine.CreateClient());
        Assert.NotNull(exception);
        Assert.Contains("Variable d'environnement ALBERT_API_KEY absente ou vide", exception.ToString());
    }
}
