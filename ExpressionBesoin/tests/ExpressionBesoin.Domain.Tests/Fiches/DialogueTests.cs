using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

/// <summary>
/// Conduite du dialogue : rubrique choisie après la fusion (lot A5, DEB-Q100 à DEB-Q103), question contextualisée de
/// l'IA ou question type, précédée du libellé (lot A7, DEB-Q117 à DEB-Q119). Attendus écrits à la main ; ceux des
/// garde-fous recalculés par une réplique Python indépendante avant livraison.
/// </summary>
public sealed class DialogueTests
{
    private const string QuestionApplication = "Application ou service concerné : Quelle application ou quel équipement est concerné ?";

    private const string QuestionSituation = "Situation actuelle : Que se passe-t-il aujourd'hui, concrètement ?";

    private const string QuestionPC = "Que devrez-vous retrouver sur votre PC pour reprendre votre travail ?";

    private static readonly ListeImmuable<QuestionCandidate> Aucune = ListeImmuable<QuestionCandidate>.Vide;

    private static ListeImmuable<QuestionCandidate> Questions(params (Rubrique Rubrique, string Question)[] questions) =>
        ListeImmuable<QuestionCandidate>.De(questions.Select(question => new QuestionCandidate(question.Rubrique, question.Question)));

    /// <summary>Fiche dont les rubriques obligatoires précédant les critères de réussite sont saisies.</summary>
    private static FicheBesoin JusquAuxCriteres()
    {
        var fiche = Brouillon();
        foreach (var rubrique in new[]
                 {
                     Rubrique.Application, Rubrique.Intitule, Rubrique.SituationActuelle, Rubrique.Difficultes,
                     Rubrique.ResultatRecherche,
                 })
        {
            fiche = Saisir(fiche, rubrique, "valeur");
        }

        return fiche;
    }

    // --- Rubriques à questionner (DEB-Q117) ---

    [Fact]
    public void A_questionner_obligatoires_vides_sans_proposition_dans_l_ordre_sans_le_resume()
    {
        Assert.Equal(
            new[]
            {
                Rubrique.Application, Rubrique.Intitule, Rubrique.SituationActuelle, Rubrique.Difficultes,
                Rubrique.ResultatRecherche, Rubrique.CriteresReussite, Rubrique.UtilisateursConcernes,
                Rubrique.ProcessusMetier, Rubrique.BeneficesAttendus,
            },
            Dialogue.AQuestionner(Brouillon()).ToArray());

        var fiche = Proposer(Saisir(Brouillon(), Rubrique.Application, "PC"), Rubrique.SituationActuelle, "Ne s'allume plus");
        Assert.Equal(
            new[]
            {
                Rubrique.Intitule, Rubrique.Difficultes, Rubrique.ResultatRecherche, Rubrique.CriteresReussite,
                Rubrique.UtilisateursConcernes, Rubrique.ProcessusMetier, Rubrique.BeneficesAttendus,
            },
            Dialogue.AQuestionner(fiche).ToArray());
        Assert.True(Dialogue.AQuestionner(Saisir(Complete(), Rubrique.Resume, null)).IsEmpty);
    }

    // --- Relance (DEB-Q102, DEB-Q117, DEB-Q118) ---

    [Fact]
    public void Relance_sans_candidate_libelle_puis_question_type_de_la_prochaine_rubrique()
    {
        Assert.Equal(QuestionApplication, Dialogue.Relance(Brouillon(), Aucune));
        var fiche = Proposer(Saisir(Brouillon(), Rubrique.Application, "Outil"), Rubrique.Intitule, "Titre");
        Assert.Equal(QuestionSituation, Dialogue.Relance(fiche, Aucune));
    }

    [Fact]
    public void Relance_candidate_valide_de_la_prochaine_rubrique_posee_sans_ses_blancs_de_bord()
    {
        var questions = Questions(
            (Rubrique.Difficultes, "Que ne pouvez-vous plus faire ?"),
            (Rubrique.CriteresReussite, "  " + QuestionPC + " "),
            (Rubrique.UtilisateursConcernes, "Qui d'autre est touché ?"));

        Assert.Equal("Critères de réussite : " + QuestionPC, Dialogue.Relance(JusquAuxCriteres(), questions));
    }

    [Theory]
    [InlineData("Que devrez-vous retrouver ? Et quand ?")]
    [InlineData("Votre PC est en panne. Que devrez-vous retrouver ?")]
    [InlineData("Que devrez-vous retrouver")]
    [InlineData("   ")]
    public void Relance_candidate_refusee_par_les_garde_fous_question_type(string candidate)
    {
        Assert.Equal(
            "Critères de réussite : À quoi verrez-vous que c'est réglé ?",
            Dialogue.Relance(JusquAuxCriteres(), Questions((Rubrique.CriteresReussite, candidate))));
    }

    /// <summary>
    /// Sonde A7 (22/11/2026) : candidates pour l'échéance (facultative, absente des rubriques à questionner) et pour le
    /// résumé (exclu par la règle 12) ; aucune n'est posée.
    /// </summary>
    [Fact]
    public void Relance_candidate_d_une_autre_rubrique_ou_du_resume_jamais_posee()
    {
        var autres = Questions(
            (Rubrique.Echeance, "Pour quelle date souhaitez-vous que votre PC soit réparé ?"),
            (Rubrique.Resume, "Souhaitez-vous obtenir le résumé de votre demande ?"));

        Assert.Equal("Critères de réussite : À quoi verrez-vous que c'est réglé ?", Dialogue.Relance(JusquAuxCriteres(), autres));
        Assert.Equal(Dialogue.RelanceResume, Dialogue.Relance(Saisir(Complete(), Rubrique.Resume, null), autres));
    }

    [Fact]
    public void Relance_premiere_candidate_de_la_rubrique_seule_examinee()
    {
        var questions = ListeImmuable<QuestionCandidate>.De(new[]
        {
            new QuestionCandidate(Rubrique.CriteresReussite, "Trop ? Long ?"),
            new QuestionCandidate(Rubrique.CriteresReussite, QuestionPC),
        });

        Assert.Equal("Critères de réussite : À quoi verrez-vous que c'est réglé ?", Dialogue.Relance(JusquAuxCriteres(), questions));
    }

    [Fact]
    public void Relance_resume_seul_restant()
    {
        Assert.Equal(
            "Toutes les rubriques obligatoires sont renseignées : envoyez « résumé » pour que je le propose.",
            Dialogue.Relance(Saisir(Complete(), Rubrique.Resume, null), Aucune));
    }

    [Fact]
    public void Relance_plus_rien_a_questionner()
    {
        Assert.Equal(
            "Relisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.",
            Dialogue.Relance(Proposer(Saisir(Complete(), Rubrique.Resume, null), Rubrique.Resume, "Synthèse"), Aucune));
    }

    // --- Garde-fous d'une question candidate (DEB-Q119) ---

    [Theory]
    [InlineData("Que devrez-vous retrouver sur votre PC pour reprendre votre travail ?", true)]
    [InlineData("Que souhaitez-vous pour votre PC : une réparation, un remplacement ou un poste de prêt ?", true)]
    [InlineData("  Qui est touché ?\n", true)]
    [InlineData("La version 2.0 vous convient-elle ?", true)]
    [InlineData("Que faire…vraiment ?", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Qui est touché", false)]
    [InlineData("Qui est touché ? Depuis quand ?", false)]
    [InlineData("Qui ?est touché ?", false)]
    [InlineData("Votre PC est en panne. Depuis quand ?", false)]
    [InlineData("C'est urgent ! Pour quand ?", false)]
    [InlineData("Que faire… vraiment ?", false)]
    [InlineData("Qui est touché\nexactement ?", false)]
    [InlineData("Qui est touché\rexactement ?", false)]
    [InlineData("Qui est touché ?.", false)]
    public void Question_valide_table_de_verite(string question, bool attendu)
    {
        Assert.Equal(attendu, Dialogue.QuestionValide(question));
    }

    [Fact]
    public void Question_valide_deux_cents_caracteres_au_plus_blancs_de_bord_exclus()
    {
        Assert.Equal(200, Dialogue.LongueurMaxQuestion);
        Assert.True(Dialogue.QuestionValide(new string('a', 199) + "?"));
        Assert.True(Dialogue.QuestionValide("  " + new string('a', 199) + "?  "));
        Assert.False(Dialogue.QuestionValide(new string('a', 200) + "?"));
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
            Dialogue.Composer("Je comprends une demande d'assistance utilisateur : poste en panne. Quelle application est concernée ?", false, fiche, Aucune));
    }

    /// <summary>
    /// A7 (DEB-Q117) : candidates rédigées AVANT la fusion ; celle de la rubrique tout juste proposée (application) n'est
    /// pas posée, celle de la rubrique choisie après la fusion (situation actuelle) l'est.
    /// </summary>
    [Fact]
    public void Composer_candidate_de_la_rubrique_choisie_apres_la_fusion()
    {
        var fiche = Proposer(Proposer(Brouillon(), Rubrique.Application, "PC"), Rubrique.Intitule, "PC qui ne s'allume plus");
        var questions = Questions(
            (Rubrique.Application, "Quel équipement est concerné ?"),
            (Rubrique.SituationActuelle, "Depuis quand votre PC ne s'allume-t-il plus ?"));

        Assert.Equal(
            "J'ai noté votre demande.\n\nSituation actuelle : Depuis quand votre PC ne s'allume-t-il plus ?",
            Dialogue.Composer("J'ai noté votre demande.", false, fiche, questions));
    }

    [Fact]
    public void Composer_message_fait_d_une_seule_question_relance_seule()
    {
        Assert.Equal(QuestionApplication, Dialogue.Composer("Quel est le poste concerné ?", false, Brouillon(), Aucune));
    }

    [Fact]
    public void Composer_hors_perimetre_sans_relance_ni_question()
    {
        Assert.Equal(
            "Cette demande ne relève pas du service informatique : adressez-vous aux services généraux.",
            Dialogue.Composer(
                "Cette demande ne relève pas du service informatique : adressez-vous aux services généraux. Puis-je vous aider autrement ?",
                true,
                Brouillon(),
                Questions((Rubrique.Application, "Quelle salle souhaitez-vous réserver ?"))));
    }

    [Fact]
    public void Composer_hors_perimetre_vide_apres_retrait_message_fixe()
    {
        Assert.Equal("Cette demande ne relève pas du service informatique.", Dialogue.Composer("Voulez-vous réserver une salle ?", true, Brouillon(), Aucune));
    }

    // --- Rubrique questionnée par le dernier message de l'application ---

    [Fact]
    public void Rubrique_questionnee_reconnue_par_le_libelle_en_tete_de_la_derniere_ligne()
    {
        Assert.Equal(Rubrique.CriteresReussite, Dialogue.RubriqueQuestionnee("J'ai noté vos attentes.\n\nCritères de réussite : " + QuestionPC));
        Assert.Equal(Rubrique.Application, Dialogue.RubriqueQuestionnee(QuestionApplication));
        Assert.Equal(
            Rubrique.UtilisateursConcernes,
            Dialogue.RubriqueQuestionnee("Situation actuelle : notée.\nUtilisateurs ou services concernés : Qui d'autre est touché ?"));
    }

    [Fact]
    public void Rubrique_questionnee_reconnue_par_la_question_type_messages_anterieurs_a_A7()
    {
        Assert.Equal(Rubrique.Application, Dialogue.RubriqueQuestionnee("J'ai noté l'intitulé.\n\nQuelle application ou quel équipement est concerné ?"));
        Assert.Equal(Rubrique.SituationActuelle, Dialogue.RubriqueQuestionnee("Que se passe-t-il aujourd'hui, concrètement ?"));
        Assert.Equal(Rubrique.Resume, Dialogue.RubriqueQuestionnee("Noté.\n\n" + Dialogue.RelanceResume));
    }

    [Theory]
    [InlineData("Relisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.")]
    [InlineData("Cette demande ne relève pas du service informatique.")]
    [InlineData("Quelle application est concernée ?")]
    [InlineData("Avez-vous une solution en tête ?")]
    [InlineData("Solution envisagée : Avez-vous une idée de solution ?")]
    [InlineData("Critères de réussite : notés.")]
    [InlineData("Critères de réussite ? Notés.")]
    [InlineData("Critères de réussite: À quoi le verrez-vous ?")]
    [InlineData("Critères de réussite : À quoi le verrez-vous ?\nMerci.")]
    [InlineData("")]
    public void Rubrique_questionnee_inconnue(string message)
    {
        // Facultatives jamais questionnées par l'application ; libellé sans séparateur exact ou ligne non interrogative.
        Assert.Null(Dialogue.RubriqueQuestionnee(message));
    }

    [Fact]
    public void Chaque_relance_d_une_rubrique_est_reconnue()
    {
        foreach (var rubrique in Catalogue.OrdreQuestionnement)
        {
            var type = rubrique == Rubrique.Resume ? Dialogue.RelanceResume : Dialogue.Question(rubrique, Aucune);
            Assert.Equal(rubrique, Dialogue.RubriqueQuestionnee("Noté." + Dialogue.Separateur + type));
            if (rubrique != Rubrique.Resume)
            {
                var contextualisee = Dialogue.Question(rubrique, Questions((rubrique, "Pouvez-vous préciser ce point ?")));
                Assert.EndsWith(" : Pouvez-vous préciser ce point ?", contextualisee);
                Assert.Equal(rubrique, Dialogue.RubriqueQuestionnee("Noté." + Dialogue.Separateur + contextualisee));
            }
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
