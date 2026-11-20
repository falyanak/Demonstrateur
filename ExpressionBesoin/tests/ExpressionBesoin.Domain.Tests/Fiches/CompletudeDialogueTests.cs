using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

public sealed class CompletudeDialogueTests
{
    // --- Complétude ---

    [Fact]
    public void Brouillon_vierge_manque_les_10_obligatoires_dans_l_ordre_du_catalogue()
    {
        var fiche = Brouillon();

        Assert.Equal(
            new[]
            {
                Rubrique.Application,
                Rubrique.Intitule,
                Rubrique.Resume,
                Rubrique.SituationActuelle,
                Rubrique.Difficultes,
                Rubrique.ResultatRecherche,
                Rubrique.CriteresReussite,
                Rubrique.UtilisateursConcernes,
                Rubrique.ProcessusMetier,
                Rubrique.BeneficesAttendus,
            },
            Completude.Manquantes(fiche).ToArray());
        Assert.False(Completude.EstComplete(fiche));
    }

    [Fact]
    public void Une_facultative_saisie_ne_change_pas_les_manquantes()
    {
        var fiche = Saisir(Brouillon(), Rubrique.Liens, "Aucun lien");

        Assert.Equal(10, Completude.Manquantes(fiche).Length);
    }

    [Fact]
    public void Une_proposition_en_attente_ne_rend_pas_la_rubrique_renseignee()
    {
        var fiche = Proposer(Brouillon(), Rubrique.Application, "Outil");

        Assert.Contains(Rubrique.Application, Completude.Manquantes(fiche));
    }

    [Fact]
    public void Valeur_acceptee_ou_manuelle_renseigne_la_rubrique()
    {
        var acceptee = Valeur(Fiche.AccepterProposition(Proposer(Brouillon(), Rubrique.Application, "Outil"), Rubrique.Application));
        var manuelle = Saisir(Brouillon(), Rubrique.Application, "Outil");

        Assert.DoesNotContain(Rubrique.Application, Completude.Manquantes(acceptee));
        Assert.DoesNotContain(Rubrique.Application, Completude.Manquantes(manuelle));
    }

    [Fact]
    public void Fiche_aux_obligatoires_saisies_est_complete()
    {
        var fiche = Complete();

        Assert.Empty(Completude.Manquantes(fiche));
        Assert.True(Completude.EstComplete(fiche));
        Assert.Empty(Completude.PropositionsEnAttente(fiche));
    }

    [Fact]
    public void Propositions_en_attente_obligatoires_ou_non_dans_l_ordre_du_catalogue()
    {
        var fiche = Proposer(Proposer(Brouillon(), Rubrique.PointsAClarifier, "Volumétrie"), Rubrique.Intitule, "Titre");

        Assert.Equal(new[] { Rubrique.Intitule, Rubrique.PointsAClarifier }, Completude.PropositionsEnAttente(fiche).ToArray());
    }

    // --- Prochaine rubrique ---

    [Fact]
    public void Brouillon_vierge_questionne_d_abord_l_application()
    {
        Assert.Equal(Rubrique.Application, Dialogue.ProchaineRubrique(Brouillon()));
    }

    [Fact]
    public void Rubrique_renseignee_passe_a_la_suivante()
    {
        Assert.Equal(Rubrique.Intitule, Dialogue.ProchaineRubrique(Saisir(Brouillon(), Rubrique.Application, "Outil")));
    }

    [Fact]
    public void Rubrique_vide_avec_proposition_en_attente_sautee()
    {
        var fiche = Proposer(Saisir(Brouillon(), Rubrique.Application, "Outil"), Rubrique.Intitule, "Titre");

        Assert.Equal(Rubrique.SituationActuelle, Dialogue.ProchaineRubrique(fiche));
    }

    [Fact]
    public void Resume_questionne_en_dernier()
    {
        var fiche = Saisir(Complete(), Rubrique.Resume, null);

        Assert.Equal(Rubrique.Resume, Dialogue.ProchaineRubrique(fiche));
        Assert.Equal(Rubrique.SituationActuelle, Dialogue.ProchaineRubrique(Saisir(fiche, Rubrique.SituationActuelle, null)));
    }

    [Fact]
    public void Plus_de_question_quand_tout_est_renseigne_ou_propose()
    {
        Assert.Null(Dialogue.ProchaineRubrique(Complete()));
        Assert.Null(Dialogue.ProchaineRubrique(Proposer(Saisir(Complete(), Rubrique.Resume, null), Rubrique.Resume, "Synthèse")));
    }
}
