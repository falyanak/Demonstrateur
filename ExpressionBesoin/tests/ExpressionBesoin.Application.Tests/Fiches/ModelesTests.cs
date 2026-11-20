using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Application.Tests.Fiches.Essais;

namespace ExpressionBesoin.Application.Tests.Fiches;

public sealed class ModelesTests
{
    [Fact]
    public void ResumeFiche_d_un_brouillon_vierge()
    {
        var resume = ResumeFiche.De(Brouillon());

        Assert.Equal(Id, resume.Id);
        Assert.Null(resume.Numero);
        Assert.Null(resume.Intitule);
        Assert.Equal("Agent Fictif", resume.Demandeur);
        Assert.Equal(Statut.Brouillon, resume.Statut);
        Assert.Equal(CreeeLe, resume.CreeeLe);
        Assert.Null(resume.ConfirmeeLe);
    }

    [Fact]
    public void ResumeFiche_d_une_fiche_validee_reprend_numero_intitule_statut_et_confirmation()
    {
        var resume = ResumeFiche.De(Validee());

        Assert.Equal("EB-2026-0001", resume.Numero);
        Assert.Equal("valeur de intitule", resume.Intitule);
        Assert.Equal(Statut.Validee, resume.Statut);
        Assert.Equal(Maintenant, resume.ConfirmeeLe);
    }

    [Fact]
    public void Ouvrir_un_succes_rend_la_valeur_sans_refus()
    {
        var fiche = Brouillon();

        var (valeur, refus) = Resultat.Succes(fiche).Ouvrir();

        Assert.Same(fiche, valeur);
        Assert.Null(refus);
    }

    [Fact]
    public void Ouvrir_un_refus_rend_le_refus_sans_valeur()
    {
        var attendu = new Refus("code", "message");

        var (valeur, refus) = Resultat<FicheBesoin>.Echec(attendu).Ouvrir();

        Assert.Null(valeur);
        Assert.Same(attendu, refus);
    }
}
