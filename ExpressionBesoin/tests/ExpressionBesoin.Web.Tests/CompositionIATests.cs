using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Infrastructure.IA;
using ExpressionBesoin.Web.Hebergement;
using Microsoft.Extensions.DependencyInjection;

namespace ExpressionBesoin.Web.Tests;

/// <summary>
/// Choix de l'adaptateur IA par profils nommés (lot A6, DEB-Q105, DEB-Q126) : table de vérité de
/// <see cref="CompositionIA.Lire"/> sur une configuration en mémoire, puis démarrage.
/// </summary>
public sealed class CompositionIATests
{
    private const string Point = "https://albert.api.etalab.gouv.fr/v1";

    /// <summary>Fausse clé construite par concaténation : une archive ne la masque pas (règles §13).</summary>
    private static readonly string Cle = "secret-" + "essai";

    /// <summary>Configuration complète : profil Albert courant, profil scénarisé au catalogue, clé dans sa variable.</summary>
    private static Dictionary<string, string?> Complete() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["IA:Profil"] = "Albert",
            ["IA:Profils:Albert:Point"] = Point,
            ["IA:Profils:Albert:Modele"] = "gemma-4-31b-it",
            ["IA:Profils:Albert:VariableCle"] = "ALBERT_API_KEY",
            ["IA:Profils:Albert:Temperature"] = "0.1",
            ["IA:Profils:Scenarise:Fournisseur"] = "Scenarise",
            ["ALBERT_API_KEY"] = Cle,
        };

    /// <summary><see cref="Complete"/> où <paramref name="cle"/> vaut <paramref name="valeur"/> (<c>null</c> = absente).</summary>
    private static Dictionary<string, string?> Avec(string cle, string? valeur)
    {
        var configuration = Complete();
        configuration[cle] = valeur;
        return configuration;
    }

    private static OptionsIA? Lire(Dictionary<string, string?> configuration) =>
        CompositionIA.Lire(cle => configuration.GetValueOrDefault(cle));

    private static string Refus(Dictionary<string, string?> configuration) =>
        Assert.Throws<InvalidOperationException>(() => Lire(configuration)).Message;

    [Fact]
    public void Profil_complet()
    {
        var options = Lire(Complete());

        Assert.NotNull(options);
        Assert.Equal(
            (new Uri(Point), "gemma-4-31b-it", "secret-essai", TimeSpan.FromSeconds(60), (float?)0.1f, (int?)null),
            (options.Point, options.Modele, options.Cle, options.Delai, options.Temperature, options.JetonsMax));
    }

    [Fact]
    public void Valeurs_sans_espaces_de_bord_profil_casse_ignoree()
    {
        var configuration = Complete();
        configuration["IA:Profil"] = " albert ";
        configuration["IA:Profils:Albert:Point"] = "http://localhost:11434/v1";
        configuration["IA:Profils:Albert:Modele"] = "  gemma-4-31b-it ";
        configuration["IA:Profils:Albert:VariableCle"] = " ALBERT_API_KEY ";
        configuration["ALBERT_API_KEY"] = " secret-essai ";

        var options = Lire(configuration);

        Assert.NotNull(options);
        Assert.Equal(
            (new Uri("http://localhost:11434/v1"), "gemma-4-31b-it", "secret-essai"),
            (options.Point, options.Modele, options.Cle));
    }

    [Theory]
    [InlineData("Scenarise")]
    [InlineData(" scenarise ")]
    public void Profil_scenarise_sans_options(string fournisseur)
    {
        var configuration = Avec("IA:Profils:Scenarise:Fournisseur", fournisseur);
        configuration["IA:Profil"] = "Scenarise";

        Assert.Null(Lire(configuration));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Profil_absent_arret_explicite(string? profil) =>
        Assert.Equal(
            "Configuration IA:Profil : nom d'un profil de IA:Profils attendu (reçu «  ») ; voir appsettings.{environnement}.json.",
            Refus(Avec("IA:Profil", profil)));

    [Fact]
    public void Profil_inconnu_arret_explicite() =>
        Assert.Equal(
            "Configuration IA:Profil : profil « Mistral » absent de IA:Profils (catalogue dans appsettings.json).",
            Refus(Avec("IA:Profil", "Mistral")));

    [Fact]
    public void Fournisseur_inconnu_arret_explicite() =>
        Assert.Equal(
            "Configuration IA:Profils:Albert:Fournisseur : « Scenarise » ou absent (API compatible OpenAI) attendu (reçu « OpenAI »).",
            Refus(Avec("IA:Profils:Albert:Fournisseur", "OpenAI")));

    /// <summary>Un profil qui ne porte qu'un champ existe : le champ manquant est nommé, pas le profil.</summary>
    [Theory]
    [InlineData("Modele")]
    [InlineData("VariableCle")]
    public void Profil_partiel_champ_manquant_nomme(string champ)
    {
        var configuration = Avec("IA:Profil", "Partiel");
        configuration["IA:Profils:Partiel:" + champ] = "x";

        Assert.Equal(
            "Configuration IA:Profils:Partiel:Point : adresse http(s) absolue de l'API compatible OpenAI attendue (reçu «  »).",
            Refus(configuration));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("albert.api.etalab.gouv.fr/v1")]
    [InlineData("ftp://albert.test/v1")]
    [InlineData(@"C:\Dev\albert")]
    public void Adresse_absente_ou_non_http_arret_explicite(string? point)
    {
        var message = Refus(Avec("IA:Profils:Albert:Point", point));

        Assert.Equal(
            $"Configuration IA:Profils:Albert:Point : adresse http(s) absolue de l'API compatible OpenAI attendue (reçu « {point} »).",
            message);
        Assert.DoesNotContain(Cle, message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Modele_absent_arret_explicite(string? modele) =>
        Assert.Equal(
            "Configuration IA:Profils:Albert:Modele : nom du modèle attendu (ex. gemma-4-31b-it).",
            Refus(Avec("IA:Profils:Albert:Modele", modele)));

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Variable_de_la_cle_absente_arret_explicite(string? variable) =>
        Assert.Equal(
            "Configuration IA:Profils:Albert:VariableCle : nom de la variable d'environnement de la clé attendu (ex. ALBERT_API_KEY).",
            Refus(Avec("IA:Profils:Albert:VariableCle", variable)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Cle_absente_arret_explicite(string? cle) =>
        Assert.Equal(
            "Variable d'environnement ALBERT_API_KEY absente ou vide : clé du profil « Albert » attendue (variable utilisateur "
            + "Windows, puis relancer Visual Studio), ou IA:Profil = Scenarise.",
            Refus(Avec("ALBERT_API_KEY", cle)));

    /// <summary>La clé est lue dans la variable NOMMÉE par le profil, jamais dans une autre (profil Claude, DEB-Q107).</summary>
    [Fact]
    public void Cle_lue_dans_la_variable_du_profil()
    {
        var configuration = Avec("IA:Profils:Albert:VariableCle", "ANTHROPIC_API_KEY");
        configuration["ANTHROPIC_API_KEY"] = "autre-" + "essai";

        Assert.Equal("autre-essai", Lire(configuration)?.Cle);
        Assert.Equal(
            "Variable d'environnement ANTHROPIC_API_KEY absente ou vide : clé du profil « Albert » attendue (variable utilisateur "
            + "Windows, puis relancer Visual Studio), ou IA:Profil = Scenarise.",
            Refus(Avec("IA:Profils:Albert:VariableCle", "ANTHROPIC_API_KEY")));
    }

    /// <summary>Température facultative (DEB-Q105) : absente → non envoyée ; bornes 0 et 2 comprises.</summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("0", 0.0)]
    [InlineData("0.1", 0.1)]
    [InlineData("1", 1.0)]
    [InlineData("2", 2.0)]
    public void Temperature_facultative(string? lue, double? attendue) =>
        Assert.Equal((float?)attendue, Lire(Avec("IA:Profils:Albert:Temperature", lue))?.Temperature);

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0,1")]
    [InlineData("-0.1")]
    [InlineData("2.5")]
    [InlineData("NaN")]
    public void Temperature_invalide_arret_explicite(string lue) =>
        Assert.Equal(
            $"Configuration IA:Profils:Albert:Temperature : nombre de 0 à 2 attendu, point décimal (reçu « {lue} »), ou absente (non envoyée).",
            Refus(Avec("IA:Profils:Albert:Temperature", lue)));

    /// <summary>Plafond de jetons facultatif (DEB-Q128) : absent → non envoyé.</summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("1", 1)]
    [InlineData("4096", 4096)]
    public void Plafond_de_jetons_facultatif(string? lu, int? attendu) =>
        Assert.Equal(attendu, Lire(Avec("IA:Profils:Albert:JetonsMax", lu))?.JetonsMax);

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData(" 12")]
    [InlineData("1.5")]
    [InlineData("abc")]
    [InlineData("99999999999")]
    public void Plafond_de_jetons_invalide_arret_explicite(string lu) =>
        Assert.Equal(
            $"Configuration IA:Profils:Albert:JetonsMax : entier positif attendu (reçu « {lu} »), ou absent (non envoyé).",
            Refus(Avec("IA:Profils:Albert:JetonsMax", lu)));

    [Fact]
    public void Cles_de_configuration() =>
        Assert.Equal(
            new[] { "IA:Profil", "IA:Profils", "Fournisseur", "Point", "Modele", "VariableCle", "Temperature", "JetonsMax", "Scenarise" },
            new[]
            {
                CompositionIA.CleProfil, CompositionIA.CleProfils, CompositionIA.Fournisseur, CompositionIA.Point, CompositionIA.Modele,
                CompositionIA.VariableCle, CompositionIA.Temperature, CompositionIA.JetonsMax, CompositionIA.Scenarise,
            });

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
        Assert.Contains($"Variable d'environnement {UsineWeb.VariableCleEssai} absente ou vide", exception.ToString());
    }
}
