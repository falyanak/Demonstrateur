using ExpressionBesoin.Web.Vite;

namespace ExpressionBesoin.Web.Tests.Vite;

public sealed class ViteTagsTests
{
    // Forme réelle produite par Vite 8.3.8 (build de la réplique du ClientApp) : clés supplémentaires ignorées.
    internal const string ManifesteReel = """
        {
          "src/features/accueil/PageAccueil.ts": {
            "file": "assets/PageAccueil-CKAkceF3.js",
            "name": "PageAccueil",
            "src": "src/features/accueil/PageAccueil.ts",
            "isDynamicEntry": true
          },
          "src/main.ts": {
            "file": "assets/main-BIqHqANe.js",
            "name": "main",
            "src": "src/main.ts",
            "isEntry": true,
            "dynamicImports": [
              "src/features/accueil/PageAccueil.ts"
            ],
            "css": [
              "assets/main-BAH4sO9p.css"
            ]
          }
        }
        """;

    [Fact]
    public void Developpement_client_de_rechargement_puis_entree_servis_par_Vite() =>
        Assert.Equal(
            "<script type=\"module\" src=\"http://localhost:5173/@vite/client\"></script>\n"
            + "<script type=\"module\" src=\"http://localhost:5173/src/main.ts\"></script>",
            ViteTags.Developpement("/src/main.ts"));

    [Fact]
    public void LireManifeste_garde_file_et_css_par_source()
    {
        var manifeste = ViteTags.LireManifeste(ManifesteReel);
        Assert.Equal(2, manifeste.Count);
        Assert.Equal("assets/main-BIqHqANe.js", manifeste["src/main.ts"].Fichier);
        Assert.Equal(new[] { "assets/main-BAH4sO9p.css" }, manifeste["src/main.ts"].Css.ToArray());
        Assert.Empty(manifeste["src/features/accueil/PageAccueil.ts"].Css);
    }

    [Fact]
    public void LireManifeste_ignore_la_casse_des_cles() =>
        Assert.Equal("a.js", ViteTags.LireManifeste("""{"e":{"FILE":"a.js","Css":["a.css"]}}""")["e"].Fichier);

    [Theory]
    [InlineData("{", "illisible")]
    [InlineData("[]", "illisible")]
    [InlineData("null", "vide")]
    [InlineData("""{"e":null}""", "« e » n'a pas de file")]
    [InlineData("""{"e":{"css":[]}}""", "« e » n'a pas de file")]
    [InlineData("""{"e":{"file":""}}""", "« e » n'a pas de file")]
    public void LireManifeste_refuse_un_manifeste_inutilisable(string json, string extrait)
    {
        var exception = Assert.Throws<InvalidDataException>(() => ViteTags.LireManifeste(json));
        Assert.Contains(extrait, exception.Message);
    }

    [Theory]
    [InlineData("", "/dist/")]
    [InlineData("/", "/dist/")]
    [InlineData("/monsite", "/monsite/dist/")]
    [InlineData("/monsite/", "/monsite/dist/")]
    public void Production_css_puis_script_prefixes_par_le_PathBase(string pathBase, string racine) =>
        Assert.Equal(
            $"<link rel=\"stylesheet\" href=\"{racine}assets/main-BAH4sO9p.css\" />\n"
            + $"<script type=\"module\" src=\"{racine}assets/main-BIqHqANe.js\"></script>",
            ViteTags.Production(ViteTags.LireManifeste(ManifesteReel), "src/main.ts", pathBase));

    [Fact]
    public void Production_plusieurs_css_dans_l_ordre_du_manifeste_et_sans_css() =>
        Assert.Equal(
            "<link rel=\"stylesheet\" href=\"/dist/a.css\" />\n<link rel=\"stylesheet\" href=\"/dist/b.css\" />\n"
            + "<script type=\"module\" src=\"/dist/a.js\"></script>|<script type=\"module\" src=\"/dist/c.js\"></script>",
            ViteTags.Production(ViteTags.LireManifeste("""{"a":{"file":"a.js","css":["a.css","b.css"]}}"""), "a", "")
            + "|" + ViteTags.Production(ViteTags.LireManifeste("""{"c":{"file":"c.js"}}"""), "c", ""));

    [Fact]
    public void Production_sans_manifeste_commentaire_explicite() =>
        Assert.Equal(
            "<!-- vite : manifeste introuvable (wwwroot/dist/.vite/manifest.json) : lancer npm run build -->",
            ViteTags.Production(null, "src/main.ts", ""));

    [Fact]
    public void Production_entree_absente_commentaire_encode() =>
        Assert.Equal(
            "<!-- vite : entrée « src/&lt;x&gt;.ts » absente du manifeste : lancer npm run build -->",
            ViteTags.Production(ViteTags.LireManifeste(ManifesteReel), "src/<x>.ts", ""));
}
