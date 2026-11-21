using ExpressionBesoin.Web.Vite;

namespace ExpressionBesoin.Web.Tests.Vite;

public sealed class LecteurManifesteTests
{
    private static readonly DateTime Date1 = new(2026, 11, 21, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Date2 = new(2026, 11, 21, 8, 5, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Sans_wwwroot_ni_chemin_ni_manifeste(string? webRoot)
    {
        var lecteur = new LecteurManifeste(new EnvironnementWebEssai("Production", webRoot));
        Assert.Null(lecteur.Chemin);
        Assert.Null(lecteur.Lire());
    }

    [Fact]
    public void Manifeste_absent_null()
    {
        using var racine = new WebRootTemporaire();
        var lecteur = new LecteurManifeste(new EnvironnementWebEssai("Production", racine.Chemin));
        Assert.Equal(racine.Manifeste, lecteur.Chemin);
        Assert.Null(lecteur.Lire());
    }

    [Fact]
    public void Meme_date_lecture_en_cache_date_changee_relu()
    {
        using var racine = new WebRootTemporaire();
        var lecteur = new LecteurManifeste(new EnvironnementWebEssai("Production", racine.Chemin));
        racine.Ecrire("""{"e":{"file":"v1.js"}}""", Date1);
        Assert.Equal("v1.js", lecteur.Lire()!["e"].Fichier);

        // Contenu changé, date inchangée : le cache répond (preuve qu'aucune relecture n'a lieu).
        racine.Ecrire("""{"e":{"file":"v2.js"}}""", Date1);
        Assert.Equal("v1.js", lecteur.Lire()!["e"].Fichier);

        racine.Ecrire("""{"e":{"file":"v3.js"}}""", Date2);
        Assert.Equal("v3.js", lecteur.Lire()!["e"].Fichier);
    }

    [Fact]
    public void Autre_wwwroot_meme_date_relu()
    {
        using var premier = new WebRootTemporaire();
        using var second = new WebRootTemporaire();
        premier.Ecrire("""{"e":{"file":"premier.js"}}""", Date1);
        second.Ecrire("""{"e":{"file":"second.js"}}""", Date1);
        var environnement = new EnvironnementWebEssai("Production", premier.Chemin);
        var lecteur = new LecteurManifeste(environnement);
        Assert.Equal("premier.js", lecteur.Lire()!["e"].Fichier);

        environnement.WebRootPath = second.Chemin;
        Assert.Equal("second.js", lecteur.Lire()!["e"].Fichier);
    }
}
