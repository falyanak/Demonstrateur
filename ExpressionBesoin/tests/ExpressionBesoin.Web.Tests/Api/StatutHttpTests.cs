using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Web.Api;

namespace ExpressionBesoin.Web.Tests.Api;

public sealed class StatutHttpTests
{
    [Theory]
    [InlineData("fiche.introuvable", 404)]
    [InlineData("x.introuvable", 404)]
    [InlineData("introuvable", 422)]
    [InlineData("document.nonValidee", 409)]
    [InlineData("document.nonvalidee", 422)]
    [InlineData("agent.inconnu", 422)]
    [InlineData("rubrique.inconnue", 422)]
    [InlineData("message.vide", 422)]
    [InlineData("soumission.incomplete", 422)]
    [InlineData("decision.inconnue", 422)]
    public void Refus_vers_statut(string code, int attendu) => Assert.Equal(attendu, StatutHttp.De(code));

    [Theory]
    [InlineData("Valider", TypeDecision.Valider)]
    [InlineData("valider", TypeDecision.Valider)]
    [InlineData("DemanderComplements", TypeDecision.DemanderComplements)]
    [InlineData("REFUSER", TypeDecision.Refuser)]
    public void Decision_par_son_nom(string nom, TypeDecision attendu) =>
        Assert.Equal(attendu, StatutHttp.LireDecision(nom).Match(type => type, refus => throw new InvalidOperationException(refus.Code)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2")]
    [InlineData("Valider,Refuser")]
    [InlineData("Ajourner")]
    [InlineData(" Valider")]
    public void Decision_inconnue_refusee(string? nom)
    {
        var refus = StatutHttp.LireDecision(nom).Match<Refus?>(_ => null, echec => echec);
        Assert.NotNull(refus);
        Assert.Equal("decision.inconnue", refus.Code);
        Assert.Equal($"Décision inconnue : « {nom} » (Valider, DemanderComplements ou Refuser).", refus.Message);
    }
}
