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

    /// <summary>Question type posée par l'application (DEB-Q101) : textes d'A4 (DEB-Q92), repris à l'identique.</summary>
    [Theory]
    [InlineData(Rubrique.Application, "Quelle application ou quel équipement est concerné ?")]
    [InlineData(Rubrique.Intitule, "Comment intituleriez-vous votre demande en quelques mots ?")]
    [InlineData(Rubrique.Resume, null)]
    [InlineData(Rubrique.SituationActuelle, "Que se passe-t-il aujourd'hui, concrètement ?")]
    [InlineData(Rubrique.Difficultes, "Qu'est-ce que cela vous empêche de faire dans votre travail ?")]
    [InlineData(Rubrique.ExempleRepresentatif, "Pouvez-vous citer un cas précis ?")]
    [InlineData(Rubrique.ResultatRecherche, "Qu'attendez-vous du service informatique ?")]
    [InlineData(Rubrique.CriteresReussite, "À quoi verrez-vous que c'est réglé ?")]
    [InlineData(Rubrique.SolutionEnvisagee, "Avez-vous une solution en tête ?")]
    [InlineData(Rubrique.UtilisateursConcernes, "Qui est concerné : vous seul, votre bureau, d'autres services ?")]
    [InlineData(Rubrique.ProcessusMetier, "Pour quelle activité de votre travail en avez-vous besoin ?")]
    [InlineData(Rubrique.Liens, "Cette demande est-elle liée à un autre outil ou à une autre demande ?")]
    [InlineData(Rubrique.BeneficesAttendus, "Qu'est-ce que cela vous permettra de faire une fois réglé ?")]
    [InlineData(Rubrique.Echeance, "Pour quand en avez-vous besoin, et pourquoi ?")]
    [InlineData(Rubrique.Contraintes, "Y a-t-il des contraintes à respecter ?")]
    [InlineData(Rubrique.DocumentsReferences, "Avez-vous des documents ou des références à signaler ?")]
    [InlineData(Rubrique.PointsAClarifier, null)]
    public void Question_type_de_chaque_rubrique(Rubrique rubrique, string? question)
    {
        Assert.Equal(question, Catalogue.Definition(rubrique).QuestionType);
    }

    /// <summary>Garantie dont dépend <c>Dialogue.Relance</c> : toute rubrique questionnée, sauf le résumé, a sa question.</summary>
    [Fact]
    public void Toute_rubrique_questionnee_hors_resume_a_une_question_qui_finit_par_un_point_d_interrogation()
    {
        foreach (var rubrique in Catalogue.OrdreQuestionnement.Where(rubrique => rubrique != Rubrique.Resume))
        {
            Assert.EndsWith(" ?", Catalogue.Definition(rubrique).QuestionType);
        }
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
