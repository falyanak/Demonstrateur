using ExpressionBesoin.Web.Hebergement;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ExpressionBesoin.Web.Vite;

/// <summary>
/// <c>&lt;vite-scripts entry="src/main.ts" /&gt;</c> (déclaré par <c>Views/_ViewImports.cshtml</c>) : adaptateur mince,
/// lit l'environnement, le PathBase et le manifeste, délègue la mise en forme à <see cref="ViteTags"/>.
/// </summary>
[HtmlTargetElement("vite-scripts", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ViteScriptsTagHelper(IWebHostEnvironment environnement, LecteurManifeste lecteur) : TagHelper
{
    [HtmlAttributeName("entry")]
    public string Entree { get; set; } = "src/main.ts";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        output.Content.SetHtmlContent(environnement.IsLocalhost()
            ? ViteTags.Developpement(Entree)
            : ViteTags.Production(lecteur.Lire(), Entree, ViewContext.HttpContext.Request.PathBase.ToUriComponent()));
    }
}
