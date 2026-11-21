using ExpressionBesoin.Web.Vite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ExpressionBesoin.Web.Tests.Vite;

public sealed class ViteScriptsTagHelperTests
{
    private static string Rendre(EnvironnementWebEssai environnement, string pathBase, string? entree = null)
    {
        var helper = new ViteScriptsTagHelper(environnement, new LecteurManifeste(environnement))
        {
            ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { Request = { PathBase = pathBase } } },
        };
        if (entree is not null)
        {
            helper.Entree = entree;
        }

        var contexte = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "essai");
        var sortie = new TagHelperOutput(
            "vite-scripts",
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        helper.Process(contexte, sortie);
        Assert.Null(sortie.TagName);
        return sortie.Content.GetContent();
    }

    [Fact]
    public void Localhost_serveur_de_developpement_quel_que_soit_le_manifeste() =>
        Assert.Equal(ViteTags.Developpement("src/main.ts"), Rendre(new EnvironnementWebEssai("localhost", null), "/monsite"));

    [Fact]
    public void Serveur_manifeste_et_PathBase_de_la_requete()
    {
        using var racine = new WebRootTemporaire();
        racine.Ecrire(ViteTagsTests.ManifesteReel, new DateTime(2026, 11, 21, 8, 0, 0, DateTimeKind.Utc));
        Assert.Equal(
            "<link rel=\"stylesheet\" href=\"/monsite/dist/assets/main-BAH4sO9p.css\" />\n"
            + "<script type=\"module\" src=\"/monsite/dist/assets/main-BIqHqANe.js\"></script>",
            Rendre(new EnvironnementWebEssai("Production", racine.Chemin), "/monsite"));
    }

    [Fact]
    public void Serveur_sans_manifeste_commentaire_et_entree_choisie_par_l_attribut()
    {
        Assert.Equal(ViteTags.ManifesteAbsent, Rendre(new EnvironnementWebEssai("Staging", null), ""));
        using var racine = new WebRootTemporaire();
        racine.Ecrire(ViteTagsTests.ManifesteReel, new DateTime(2026, 11, 21, 8, 0, 0, DateTimeKind.Utc));
        Assert.Equal(
            "<script type=\"module\" src=\"/dist/assets/PageAccueil-CKAkceF3.js\"></script>",
            Rendre(new EnvironnementWebEssai("Production", racine.Chemin), "", "src/features/accueil/PageAccueil.ts"));
    }
}
