using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Infrastructure.Organisation;
using static ExpressionBesoin.Infrastructure.Tests.Essais;

namespace ExpressionBesoin.Infrastructure.Tests.Organisation;

public sealed class ReferentielOrganisationFictifTests
{
    private readonly ReferentielOrganisationFictif _referentiel = new();

    [Fact]
    public async Task ListerAgents_rend_les_quatre_agents_dans_l_ordre_d_affichage()
    {
        var agents = await _referentiel.ListerAgents(Annulation);

        Assert.Equal(new[] { "camille.martin", "louis.petit", "sarah.durand", "hugo.leroy" }, agents.Select(agent => agent.Identifiant).ToArray());
    }

    [Fact]
    public async Task Chaque_bureau_a_un_seul_valideur_et_deux_agents()
    {
        var agents = await _referentiel.ListerAgents(Annulation);

        var bureaux = agents.GroupBy(agent => agent.Bureau).ToDictionary(groupe => groupe.Key, groupe => groupe.ToArray());
        Assert.Equal(2, bureaux.Count);
        Assert.Equal(new[] { "Dominique Bernard" }, bureaux[ReferentielOrganisationFictif.BureauCommerceElectronique].Select(agent => agent.Valideur).Distinct().ToArray());
        Assert.Equal(new[] { "Claude Moreau" }, bureaux[ReferentielOrganisationFictif.BureauProduitsIndustriels].Select(agent => agent.Valideur).Distinct().ToArray());
        Assert.All(bureaux.Values, groupe => Assert.Equal(2, groupe.Length));
    }

    [Fact]
    public async Task TrouverAgent_rend_l_agent_connu()
    {
        var agent = await _referentiel.TrouverAgent("sarah.durand", Annulation);

        Assert.Equal(new Agent("sarah.durand", "Sarah Durand", ReferentielOrganisationFictif.BureauProduitsIndustriels, "Claude Moreau"), agent);
    }

    [Theory]
    [InlineData("inconnu")]
    [InlineData("Sarah.Durand")]
    [InlineData("")]
    public async Task TrouverAgent_rend_null_pour_un_identifiant_inconnu_casse_comprise(string identifiant)
    {
        Assert.Null(await _referentiel.TrouverAgent(identifiant, Annulation));
    }
}
