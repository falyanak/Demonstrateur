using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// IA scénarisée (DEB-Q41) : chaque réponse est relue par <see cref="ReponseIA.Lire"/>, comme celle de l'IA réelle ; les
/// attendus sont écrits à la main, jamais recopiés de la sortie.
/// </summary>
public sealed class ScenarioDialogueTests
{
    private const string Message = "Contrôle des plates-formes e-commerce et prélèvements à distance";

    private static readonly Echange UnEchange = new(AuteurEchange.Agent, "Bonjour", Essais.T0);

    /// <summary>Contexte d'un tour : valeurs retenues et propositions en attente par code, rubriques du catalogue.</summary>
    private static ContexteTour Contexte(
        string message,
        string? prochaine,
        int echanges = 0,
        IReadOnlyDictionary<string, string>? valeurs = null,
        IReadOnlyDictionary<string, string>? propositions = null,
        bool sansRubriques = false) =>
        new(
            message,
            ListeImmuable<Echange>.De(Enumerable.Repeat(UnEchange, echanges)),
            sansRubriques
                ? ListeImmuable<EtatRubrique>.Vide
                : ListeImmuable<EtatRubrique>.De(Catalogue.Toutes.Select(definition => new EtatRubrique(
                    definition.Code,
                    definition.Libelle,
                    definition.Obligatoire,
                    valeurs?.GetValueOrDefault(definition.Code),
                    propositions?.GetValueOrDefault(definition.Code)))),
            ListeImmuable<string>.Vide,
            prochaine);

    private static ReponseIA Lue(ContexteTour contexte) => Essais.Valeur(ReponseIA.Lire(ScenarioDialogue.Repondre(contexte)));

    private static (Rubrique, string, string)[] Propositions(ReponseIA reponse) =>
        [.. reponse.Propositions.Select(proposition =>
            (proposition.Rubrique, proposition.Proposition.Valeur, proposition.Proposition.Justification))];

    /// <summary>
    /// DEB-Q89 : une demande d'assistance, d'équipement, d'accès ou une panne relève du service informatique ; l'IA
    /// scénarisée la traite comme toute autre (ancienne liste de mots retirée).
    /// </summary>
    [Theory]
    [InlineData("Mon IMPRIMANTE ne fonctionne plus")]
    [InlineData("Accès refusé à l'application")]
    [InlineData("Demande de prêt d'un ordinateur portable")]
    [InlineData("Incident : le copieur est en panne")]
    public void Demande_d_assistance_traitee_comme_une_autre(string message)
    {
        var reponse = Lue(Contexte(message, "application"));
        Assert.False(reponse.HorsPerimetre);
        Assert.Equal(new[] { (Rubrique.Intitule, message, "Reprend les mots de l'agent.") }, Propositions(reponse));
    }

    [Fact]
    public void Texte_exact_d_une_reponse_du_premier_tour() =>
        Assert.Equal(
            "{\"message\":\"Quel service, quelle application ou quel élément (poste de travail, serveur, réseau) est "
            + "concerné ?\",\"propositions\":[{\"rubrique\":\"intitule\",\"valeur\":\"panne réseau\","
            + "\"justification\":\"Reprend les mots de l'agent.\"}],\"horsPerimetre\":false}",
            ScenarioDialogue.Repondre(Contexte("panne réseau", "application")));

    [Fact]
    public void Plus_de_rubrique_a_questionner_invitation_a_soumettre()
    {
        var reponse = Lue(Contexte(Message, null, echanges: 4));
        Assert.Equal((ScenarioDialogue.MessageFin, false), (reponse.Message, reponse.HorsPerimetre));
        Assert.Empty(reponse.Propositions);
    }

    [Fact]
    public void Premier_tour_la_demande_devient_l_intitule_question_sur_l_application()
    {
        var reponse = Lue(Contexte(Message, "application"));
        Assert.Equal("Quel service, quelle application ou quel élément (poste de travail, serveur, réseau) est concerné ?", reponse.Message);
        Assert.Equal(new[] { (Rubrique.Intitule, Message, "Reprend les mots de l'agent.") }, Propositions(reponse));
    }

    [Fact]
    public void Premier_tour_intitule_deja_saisi_la_reponse_va_a_l_application()
    {
        var reponse = Lue(Contexte("SICCRF", "application", valeurs: new Dictionary<string, string> { ["intitule"] = "Contrôle" }));
        Assert.Equal("Comment cela se passe-t-il aujourd'hui ?", reponse.Message);
        Assert.Equal(new[] { (Rubrique.Application, "SICCRF", "Reprend les mots de l'agent.") }, Propositions(reponse));
    }

    [Fact]
    public void Premier_tour_intitule_propose_la_reponse_va_a_l_application()
    {
        var reponse = Lue(Contexte("SICCRF", "application", propositions: new Dictionary<string, string> { ["intitule"] = "Contrôle" }));
        Assert.Equal(new[] { (Rubrique.Application, "SICCRF", "Reprend les mots de l'agent.") }, Propositions(reponse));
    }

    [Fact]
    public void Premier_tour_sur_l_intitule_meme_la_demande_y_va()
    {
        var reponse = Lue(Contexte(Message, "intitule", valeurs: new Dictionary<string, string> { ["application"] = "SICCRF" }));
        Assert.Equal("Comment cela se passe-t-il aujourd'hui ?", reponse.Message);
        Assert.Equal(new[] { (Rubrique.Intitule, Message, "Reprend les mots de l'agent.") }, Propositions(reponse));
    }

    [Fact]
    public void Tour_courant_la_reponse_va_a_la_rubrique_questionnee_question_suivante()
    {
        var valeurs = new Dictionary<string, string> { ["application"] = "SICCRF", ["intitule"] = "Contrôle", ["situationActuelle"] = "Manuel" };
        var reponse = Lue(Contexte("Les relevés sont ressaisis", "difficultes", echanges: 6, valeurs: valeurs));
        Assert.Equal("Quel résultat recherchez-vous ?", reponse.Message);
        Assert.Equal(new[] { (Rubrique.Difficultes, "Les relevés sont ressaisis", "Reprend les mots de l'agent.") }, Propositions(reponse));
    }

    [Fact]
    public void Derniere_question_avant_un_resume_deja_propose_invitation_a_soumettre()
    {
        var reponse = Lue(Contexte(
            "Gain de temps",
            "beneficesAttendus",
            echanges: 2,
            propositions: new Dictionary<string, string> { ["resume"] = "Résumé" }));
        Assert.Equal(ScenarioDialogue.MessageFin, reponse.Message);
        Assert.Equal(new[] { (Rubrique.BeneficesAttendus, "Gain de temps", "Reprend les mots de l'agent.") }, Propositions(reponse));
    }

    [Fact]
    public void Resume_synthese_des_seules_rubriques_obligatoires_retenues_dans_l_ordre_du_modele()
    {
        var valeurs = new Dictionary<string, string>
        {
            ["intitule"] = "Contrôle e-commerce",
            ["application"] = "SICCRF",
            ["exempleRepresentatif"] = "facultative : absente du résumé",
            ["beneficesAttendus"] = "Gain de temps",
        };
        var reponse = Lue(Contexte("ok", "resume", echanges: 18, valeurs: valeurs));
        Assert.Equal(ScenarioDialogue.MessageResume, reponse.Message);
        Assert.Equal(
            new[]
            {
                (Rubrique.Resume,
                 "Application ou service concerné : SICCRF\nIntitulé court : Contrôle e-commerce\nBénéfices attendus : Gain de temps",
                 "Synthèse des rubriques retenues, sans ajout."),
            },
            Propositions(reponse));
    }

    [Fact]
    public void Resume_sans_rubrique_retenue_rien_a_proposer()
    {
        var reponse = Lue(Contexte("ok", "resume", echanges: 2, propositions: new Dictionary<string, string> { ["application"] = "SICCRF" }));
        Assert.Equal(ScenarioDialogue.MessageResumeImpossible, reponse.Message);
        Assert.Empty(reponse.Propositions);
    }

    [Fact]
    public void Rubriques_absentes_du_contexte_aucune_question_suivante()
    {
        var reponse = Lue(Contexte("SICCRF", "application", sansRubriques: true));
        Assert.Equal(ScenarioDialogue.MessageFin, reponse.Message);
        Assert.Equal(new[] { (Rubrique.Application, "SICCRF", "Reprend les mots de l'agent.") }, Propositions(reponse));
    }

    [Fact]
    public void Message_aux_caracteres_speciaux_repris_tel_quel()
    {
        const string special = "Relevés \"à distance\" <urgent> & suivis\nligne 2\t\\fin";
        var reponse = Lue(Contexte(special, "difficultes", echanges: 2));
        Assert.Equal(special, Assert.Single(reponse.Propositions).Proposition.Valeur);
    }

    [Theory]
    [InlineData("exempleRepresentatif", "Pouvez-vous préciser : Exemple représentatif ?")]
    [InlineData("codeInconnu", "Pouvez-vous préciser : codeInconnu ?")]
    [InlineData("criteresReussite", "À quoi verra-t-on que la demande est satisfaite ?")]
    public void Question_ecrite_ou_generique(string code, string attendue) =>
        Assert.Equal(attendue, ScenarioDialogue.Question(Contexte("x", null), code));

    [Fact]
    public async Task Adaptateur_rend_le_scenario_et_respecte_l_annulation()
    {
        var contexte = Contexte(Message, "application");
        Assert.Equal(ScenarioDialogue.Repondre(contexte), await new AssistantIAScenarise().Repondre(contexte, Essais.Annulation));
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new AssistantIAScenarise().Repondre(contexte, new CancellationToken(canceled: true)));
    }
}
