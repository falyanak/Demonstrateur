using ExpressionBesoin.Application.Dictee;
using Microsoft.AspNetCore.Mvc;

namespace ExpressionBesoin.Web.Controllers;

/// <summary>Modèle de la vue Dialogue : fiche consultée par le front, dictée offerte ou non (lot S1, DEB-Q132).</summary>
public sealed record ModeleDialogue(Guid Fiche, bool Dictee);

public sealed class HomeController(ServiceDictee dictee) : Controller
{
    public IActionResult Index() => View();

    /// <summary>
    /// Page Dialogue d'une fiche (DEB-Q48) : <c>Home/Dialogue/{id}</c>, identifiant Guid non vide, sinon 404. La vue ne lit
    /// pas la fiche : le front la consulte par l'API (fiche inconnue → refus <c>fiche.introuvable</c> affiché). Elle dit
    /// seulement si la dictée est offerte (attribut <c>data-dictee</c>, lot S1).
    /// </summary>
    public IActionResult Dialogue(Guid? id) =>
        id is { } fiche && fiche != Guid.Empty ? View(new ModeleDialogue(fiche, dictee.Disponible)) : NotFound();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
