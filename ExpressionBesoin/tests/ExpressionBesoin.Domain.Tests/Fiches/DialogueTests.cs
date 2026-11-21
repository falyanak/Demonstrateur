using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

/// <summary>
/// Question déterministe (lot A5, DEB-Q100 à DEB-Q103) : relance, garde-fou et composition du message. Attendus écrits à
/// la main ; ceux du garde-fou recalculés par une réplique Python indépendante avant livraison.
/// </summary>
public sealed class DialogueTests
{
    private const string QuestionApplication = "Quelle application ou quel équipement est concerné ?";

    private const string QuestionSituation = "Que se passe-t-il aujourd'hui, concrètement ?";

    // --- Relance (DEB-Q102) ---

    [Fact]
    public void Relance_question_type_de_la_prochaine_rubrique()
    {
        Assert.Equal(QuestionApplication, Dialogue.Relance(Brouillon()));
        var fiche = Proposer(Saisir(Brouillon(), Rubrique.Application, "Outil"), Rubrique.Intitule, "Titre");
        Assert.Equal(QuestionSituation, Dialogue.Relance(fiche));
    }

    [Fact]
    public void Relance_resume_seul_restant()
    {
        Assert.Equal(
            "Toutes les rubriques obligatoires sont renseignées : envoyez « résumé » pour que je le propose.",
            Dialogue.Relance(Saisir(Complete(), Rubrique.Resume, null)));
    }

    [Fact]
    public void Relance_plus_rien_a_questionner()
    {
        Assert.Equal(
            "Relisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.",
            Dialogue.Relance(Proposer(Saisir(Complete(), Rubrique.Resume, null), Rubrique.Resume, "Synthèse")));
    }

    // --- Composition (DEB-Q102, DEB-Q103) ---

    /// <summary>
    /// DEB-A4 (21/11/2026) : l'IA proposait l'application et demandait aussitôt « Quelle application est concernée ? ».
    /// La question de l'IA est retirée, la relance porte sur la rubrique suivante, calculée APRÈS la fusion.
    /// </summary>
    [Fact]
    public void Composer_question_de_l_IA_retiree_relance_apres_la_fusion()
    {
        var fiche = Proposer(Proposer(Brouillon(), Rubrique.Application, "Poste de travail"), Rubrique.Intitule, "PC en panne");

        Assert.Equal(
            "Je comprends une demande d'assistance utilisateur : poste en panne.\n\n" + QuestionSituation,
            Dialogue.Composer("Je comprends une demande d'assistance utilisateur : poste en panne. Quelle application est concernée ?", false, fiche));
    }

    [Fact]
    public void Composer_message_fait_d_une_seule_question_relance_seule()
    {
        Assert.Equal(QuestionApplication, Dialogue.Composer("Quel est le poste concerné ?", false, Brouillon()));
    }

    [Fact]
    public void Composer_hors_perimetre_sans_relance_ni_question()
    {
        Assert.Equal(
            "Cette demande ne relève pas du service informatique : adressez-vous aux services généraux.",
            Dialogue.Composer(
                "Cette demande ne relève pas du service informatique : adressez-vous aux services généraux. Puis-je vous aider autrement ?",
                true,
                Brouillon()));
    }

    [Fact]
    public void Composer_hors_perimetre_vide_apres_retrait_message_fixe()
    {
        Assert.Equal("Cette demande ne relève pas du service informatique.", Dialogue.Composer("Voulez-vous réserver une salle ?", true, Brouillon()));
    }

    // --- Rubrique questionnée par le dernier message de l'application ---

    [Fact]
    public void Rubrique_questionnee_reconnue_par_la_fin_du_message()
    {
        Assert.Equal(Rubrique.Application, Dialogue.RubriqueQuestionnee("J'ai noté l'intitulé.\n\n" + QuestionApplication));
        Assert.Equal(Rubrique.SituationActuelle, Dialogue.RubriqueQuestionnee(QuestionSituation));
        Assert.Equal(Rubrique.Resume, Dialogue.RubriqueQuestionnee("Noté.\n\n" + Dialogue.RelanceResume));
    }

    [Theory]
    [InlineData("Relisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.")]
    [InlineData("Cette demande ne relève pas du service informatique.")]
    [InlineData("Quelle application est concernée ?")]
    [InlineData("Avez-vous une solution en tête ?")]
    [InlineData("")]
    public void Rubrique_questionnee_inconnue(string message)
    {
        // « Avez-vous une solution en tête ? » : question type d'une facultative, jamais posée par l'application.
        Assert.Null(Dialogue.RubriqueQuestionnee(message));
    }

    [Fact]
    public void Chaque_relance_d_une_rubrique_est_reconnue()
    {
        foreach (var rubrique in Catalogue.OrdreQuestionnement)
        {
            var relance = rubrique == Rubrique.Resume ? Dialogue.RelanceResume : Catalogue.Definition(rubrique).QuestionType!;
            Assert.Equal(rubrique, Dialogue.RubriqueQuestionnee("Noté." + Dialogue.Separateur + relance));
        }
    }

    // --- Garde-fou (DEB-Q103) ---

    [Theory]
    [InlineData(
        "Je comprends une demande d'assistance utilisateur : prêt d'un ordinateur portable. Quel est votre bureau ?",
        "Je comprends une demande d'assistance utilisateur : prêt d'un ordinateur portable.")]
    [InlineData("Merci. Pouvez-vous préciser ? Je propose l'intitulé.", "Merci. Je propose l'intitulé.")]
    [InlineData("« Quel équipement ? » Je note.", "Je note.")]
    [InlineData("Version 2.0 retenue. Est-ce bien cela ?", "Version 2.0 retenue.")]
    [InlineData("Bonjour\nQuelle application ?", "Bonjour")]
    [InlineData("Quelle app ?\nSuite.", "Suite.")]
    [InlineData("Bonjour ?\r\nNoté.", "Noté.")]
    [InlineData("Tout est clair", "Tout est clair")]
    [InlineData("C'est noté !? Vraiment", "Vraiment")]
    [InlineData("Quelle application est concernée ?", "")]
    [InlineData("Noté (voir plus haut ?) ensuite.", "ensuite.")]
    [InlineData("\"Lequel ?\" Noté.", "Noté.")]
    [InlineData("“Lequel ?” Noté.", "Noté.")]
    [InlineData("‘Lequel ?’ Noté.", "Noté.")]
    [InlineData("[Est-ce urgent ?] Noté.", "Noté.")]
    [InlineData("Noté ! Quel délai ?", "Noté !")]
    [InlineData("Que faire… Je propose.", "Que faire… Je propose.")]
    [InlineData("Pourquoi?Parce que.", "Pourquoi?Parce que.")]
    [InlineData("", "")]
    [InlineData("  Noté.  ", "Noté.")]
    public void Sans_question_retire_les_phrases_interrogatives(string message, string attendu)
    {
        Assert.Equal(attendu, Dialogue.SansQuestion(message));
    }
}
