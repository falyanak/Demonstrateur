using Microsoft.AspNetCore.Mvc;

namespace ExpressionBesoin.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>
    /// Page Dialogue d'une fiche (DEB-Q48) : <c>Home/Dialogue/{id}</c>, identifiant Guid non vide, sinon 404. La vue ne lit
    /// pas la fiche : le front la consulte par l'API (fiche inconnue → refus <c>fiche.introuvable</c> affiché).
    /// </summary>
    public IActionResult Dialogue(Guid? id) => id is { } fiche && fiche != Guid.Empty ? View(fiche) : NotFound();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
