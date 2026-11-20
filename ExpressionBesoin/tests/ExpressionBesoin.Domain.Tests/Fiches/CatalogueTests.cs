using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Domain.Tests.Fiches;

public sealed class CatalogueTests
{
    [Fact]
    public void Le_catalogue_compte_17_rubriques_dont_10_obligatoires()
    {
        Assert.Equal(17, Catalogue.Toutes.Length);
        Assert.Equal(Enum.GetValues<Rubrique>().Length, Catalogue.Toutes.Length);
        Assert.Equal(10, Catalogue.Toutes.Count(definition => definition.Obligatoire));
    }

    [Fact]
    public void Chaque_definition_est_rangee_a_l_ordinal_de_sa_rubrique()
    {
        for (var i = 0; i < Catalogue.Toutes.Length; i++)
        {
            Assert.Equal((Rubrique)i, Catalogue.Toutes[i].Rubrique);
            Assert.Same(Catalogue.Toutes[i], Catalogue.Definition((Rubrique)i));
        }
    }

    [Theory]
    [InlineData(Rubrique.Application, "application", "Application ou service concerné", Bloc.Identification, true)]
    [InlineData(Rubrique.Intitule, "intitule", "Intitulé court", Bloc.Identification, true)]
    [InlineData(Rubrique.Resume, "resume", "Résumé", Bloc.Synthese, true)]
    [InlineData(Rubrique.SituationActuelle, "situationActuelle", "Situation actuelle", Bloc.Contexte, true)]
    [InlineData(Rubrique.Difficultes, "difficultes", "Difficultés rencontrées", Bloc.Contexte, true)]
    [InlineData(Rubrique.ExempleRepresentatif, "exempleRepresentatif", "Exemple représentatif", Bloc.Contexte, false)]
    [InlineData(Rubrique.ResultatRecherche, "resultatRecherche", "Résultat recherché", Bloc.Objectifs, true)]
    [InlineData(Rubrique.CriteresReussite, "criteresReussite", "Critères de réussite", Bloc.Objectifs, true)]
    [InlineData(Rubrique.SolutionEnvisagee, "solutionEnvisagee", "Solution envisagée", Bloc.Objectifs, false)]
    [InlineData(Rubrique.UtilisateursConcernes, "utilisateursConcernes", "Utilisateurs ou services concernés", Bloc.Perimetre, true)]
    [InlineData(Rubrique.ProcessusMetier, "processusMetier", "Processus métier", Bloc.Perimetre, true)]
    [InlineData(Rubrique.Liens, "liens", "Liens avec d'autres outils ou demandes", Bloc.Perimetre, false)]
    [InlineData(Rubrique.BeneficesAttendus, "beneficesAttendus", "Bénéfices attendus", Bloc.Benefices, true)]
    [InlineData(Rubrique.Echeance, "echeance", "Échéance et justification", Bloc.Benefices, false)]
    [InlineData(Rubrique.Contraintes, "contraintes", "Contraintes identifiées", Bloc.Benefices, false)]
    [InlineData(Rubrique.DocumentsReferences, "documentsReferences", "Documents ou références", Bloc.Complements, false)]
    [InlineData(Rubrique.PointsAClarifier, "pointsAClarifier", "Points restant à clarifier", Bloc.Complements, false)]
    public void Definition_conforme_au_modele_FEB(Rubrique rubrique, string code, string libelle, Bloc bloc, bool obligatoire)
    {
        var definition = Catalogue.Definition(rubrique);

        Assert.Equal(rubrique, definition.Rubrique);
        Assert.Equal(code, definition.Code);
        Assert.Equal(libelle, definition.Libelle);
        Assert.Equal(bloc, definition.Bloc);
        Assert.Equal(obligatoire, definition.Obligatoire);
    }

    [Theory]
    [InlineData("situationActuelle", true, Rubrique.SituationActuelle)]
    [InlineData("SITUATIONACTUELLE", true, Rubrique.SituationActuelle)]
    [InlineData("pointsAClarifier", true, Rubrique.PointsAClarifier)]
    [InlineData("budget", false, Rubrique.Application)]
    [InlineData("", false, Rubrique.Application)]
    [InlineData(" intitule", false, Rubrique.Application)]
    public void Trouver_une_rubrique_par_son_code_casse_ignoree(string code, bool trouvee, Rubrique attendue)
    {
        Assert.Equal(trouvee, Catalogue.EssayerTrouver(code, out var rubrique));
        Assert.Equal(attendue, rubrique);
    }

    [Fact]
    public void Ordre_de_questionnement_obligatoires_dans_l_ordre_du_modele_resume_en_dernier()
    {
        Assert.Equal(
            new[]
            {
                Rubrique.Application,
                Rubrique.Intitule,
                Rubrique.SituationActuelle,
                Rubrique.Difficultes,
                Rubrique.ResultatRecherche,
                Rubrique.CriteresReussite,
                Rubrique.UtilisateursConcernes,
                Rubrique.ProcessusMetier,
                Rubrique.BeneficesAttendus,
                Rubrique.Resume,
            },
            Catalogue.OrdreQuestionnement.ToArray());
    }
}
