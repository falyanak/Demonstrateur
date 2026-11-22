using System.Security.Cryptography;
using System.Text;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// Prompt de l'IA réelle (DEB-Q88 ; questionnement DEB-A3, DEB-A4, question déterministe DEB-Q100 ; question
/// contextualisée, lot A7, DEB-Q116 à DEB-Q122) : attendus écrits à la main ou tirés des fichiers de la sonde d'A7
/// (consignes-A7.txt, Sonder-Question.ps1) par une réplique indépendante (Python) avant livraison ; jamais recopiés de la
/// sortie.
/// </summary>
public sealed class PromptFicheTests
{
    private const string Demande = "Contrôle des plates-formes e-commerce et prélèvements à distance";

    private static EtatRubrique EtatDe(string code, string? valeur = null, string? proposition = null) =>
        new(code, Catalogue.Toutes.Single(definition => definition.Code == code).Libelle, true, valeur, proposition);

    private static ContexteTour Contexte(
        string message,
        string? prochaine,
        EtatRubrique[] rubriques,
        string[] manquantes,
        string[] aQuestionner,
        params Echange[] historique) =>
        new(
            message,
            ListeImmuable<Echange>.De(historique),
            ListeImmuable<EtatRubrique>.De(rubriques),
            ListeImmuable<string>.De(manquantes),
            prochaine,
            ListeImmuable<string>.De(aQuestionner));

    private const string RelanceApplication =
        "Je comprends une demande d'évolution.\n\nApplication ou service concerné : Quelle application ou quel équipement est concerné ?";

    private static readonly Echange[] DeuxEchanges =
    [
        new(AuteurEchange.Agent, Demande, Essais.T0),
        new(AuteurEchange.IA, RelanceApplication, Essais.T0.AddSeconds(2)),
    ];

    [Theory]
    [InlineData(null, null, "vide")]
    [InlineData("SORA", null, "retenue « SORA »")]
    [InlineData(null, "Contrôle e-commerce", "proposition en attente « Contrôle e-commerce »")]
    [InlineData("Saisie double", "Saisies en double", "retenue « Saisie double » ; proposition en attente « Saisies en double »")]
    public void Etat_d_une_rubrique(string? valeur, string? proposition, string attendu) =>
        Assert.Equal(attendu, PromptFiche.Etat(EtatDe("difficultes", valeur, proposition)));

    [Fact]
    public void Premier_tour_demande_initiale_rubriques_a_questionner_sans_rubrique_questionnee()
    {
        var contexte = Contexte(Demande, "application", [EtatDe("application"), EtatDe("intitule")], ["application", "intitule"], ["application", "intitule"]);

        Assert.Equal(
            "État de la fiche :\n"
            + "- application : vide\n"
            + "- intitule : vide\n"
            + "Rubriques obligatoires manquantes : application, intitule\n"
            + "Rubriques à questionner : application, intitule\n"
            + "Premier message de l'agent : sa demande initiale.\n"
            + "\n"
            + "Message de l'agent :\n"
            + "Contrôle des plates-formes e-commerce et prélèvements à distance",
            PromptFiche.Tour(contexte));
    }

    /// <summary>
    /// La rubrique questionnée est lue dans le dernier message de l'application (ici l'application), même si la prochaine
    /// rubrique du contexte a changé entre-temps (l'agent a saisi l'application à la main) : leçon de DEB-A3.
    /// </summary>
    [Fact]
    public void Tour_suivant_rubrique_questionnee_lue_dans_l_historique()
    {
        var contexte = Contexte(
            "Les contrôleurs saisissent tout à la main.",
            "situationActuelle",
            [
                EtatDe("application", "SORA"),
                EtatDe("intitule", proposition: "Contrôle e-commerce"),
                EtatDe("situationActuelle"),
                EtatDe("difficultes", "Saisie double", "Saisies en double"),
                EtatDe("resultatRecherche"),
            ],
            ["intitule", "situationActuelle", "resultatRecherche"],
            ["situationActuelle", "resultatRecherche"],
            DeuxEchanges);

        Assert.Equal(
            "État de la fiche :\n"
            + "- application : retenue « SORA »\n"
            + "- intitule : proposition en attente « Contrôle e-commerce »\n"
            + "- situationActuelle : vide\n"
            + "- difficultes : retenue « Saisie double » ; proposition en attente « Saisies en double »\n"
            + "- resultatRecherche : vide\n"
            + "Rubriques obligatoires manquantes : intitule, situationActuelle, resultatRecherche\n"
            + "Rubriques à questionner : situationActuelle, resultatRecherche\n"
            + "Rubrique questionnée : application\n"
            + "\n"
            + "Message de l'agent :\n"
            + "Les contrôleurs saisissent tout à la main.",
            PromptFiche.Tour(contexte));
    }

    [Theory]
    [InlineData("Noté.\n\nSituation actuelle : Depuis quand votre PC ne s'allume-t-il plus ?", "application", "situationActuelle")]
    [InlineData("Noté.\n\nCritères de réussite : À quoi verrez-vous que c'est réglé ?", "application", "criteresReussite")]
    [InlineData("Noté.\n\nQue se passe-t-il aujourd'hui, concrètement ?", "application", "situationActuelle")]
    [InlineData("Noté.\n\nToutes les rubriques obligatoires sont renseignées : envoyez « résumé » pour que je le propose.", null, "resume")]
    [InlineData("Quelle application est concernée ?", "difficultes", "difficultes")]
    [InlineData("Relisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.", null, null)]
    public void Rubrique_questionnee_relance_reconnue_sinon_prochaine_rubrique(string dernierMessageIA, string? prochaine, string? attendue)
    {
        // Lignes 1 et 2 : libellé en tête (A7, candidate de l'IA ou question type) ; ligne 3 : question type seule
        // (A5) ; ligne 5 : message antérieur à A5 (question de l'IA), la prochaine rubrique du contexte le remplace.
        var contexte = Contexte("x", prochaine, [], [], [], new Echange(AuteurEchange.Agent, Demande, Essais.T0), new Echange(AuteurEchange.IA, dernierMessageIA, Essais.T0));

        Assert.Equal(attendue, PromptFiche.Questionnee(contexte));
    }

    [Fact]
    public void Rubrique_questionnee_dernier_message_de_l_IA_seulement()
    {
        var contexte = Contexte(
            "x",
            "difficultes",
            [],
            [],
            [],
            new Echange(AuteurEchange.IA, RelanceApplication, Essais.T0),
            new Echange(AuteurEchange.Agent, "SORA", Essais.T0),
            new Echange(AuteurEchange.IA, "Noté.\n\nSituation actuelle : Comment saisissez-vous les contrôles aujourd'hui ?", Essais.T0),
            new Echange(AuteurEchange.Agent, "Tout à la main", Essais.T0));

        Assert.Equal("situationActuelle", PromptFiche.Questionnee(contexte));
    }

    [Fact]
    public void Rubrique_questionnee_historique_sans_message_de_l_IA_prochaine_rubrique()
    {
        var contexte = Contexte("x", "intitule", [], [], ["intitule"], new Echange(AuteurEchange.Agent, Demande, Essais.T0));

        Assert.Equal("intitule", PromptFiche.Questionnee(contexte));
        Assert.Contains("Rubriques à questionner : intitule\nRubrique questionnée : intitule\n\nMessage de l'agent :\nx", PromptFiche.Tour(contexte));
    }

    [Fact]
    public void Plus_de_rubrique_questionnee()
    {
        var contexte = Contexte(
            "C'est tout.",
            null,
            [EtatDe("application", "SORA")],
            [],
            [],
            new Echange(AuteurEchange.Agent, Demande, Essais.T0),
            new Echange(AuteurEchange.IA, "Noté.\n\nRelisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.", Essais.T0));

        Assert.Equal(
            "État de la fiche :\n"
            + "- application : retenue « SORA »\n"
            + "Rubriques obligatoires manquantes : aucune\n"
            + "Rubriques à questionner : aucune\n"
            + "Rubrique questionnée : aucune\n"
            + "\n"
            + "Message de l'agent :\n"
            + "C'est tout.",
            PromptFiche.Tour(contexte));
    }

    [Fact]
    public void Messages_historique_dans_l_ordre_puis_le_tour()
    {
        var contexte = Contexte(
            "Les contrôleurs saisissent tout à la main.",
            "situationActuelle",
            [EtatDe("situationActuelle")],
            ["situationActuelle"],
            ["situationActuelle"],
            DeuxEchanges);

        Assert.Equal(
            ListeImmuable<MessageIA>.De(new[]
            {
                new MessageIA(RoleMessage.Agent, Demande),
                new MessageIA(RoleMessage.IA, RelanceApplication),
                new MessageIA(RoleMessage.Agent, PromptFiche.Tour(contexte)),
            }),
            PromptFiche.Messages(contexte));
    }

    [Fact]
    public void Premier_tour_un_seul_message()
    {
        var contexte = Contexte(Demande, "application", [EtatDe("application")], ["application"], ["application"]);
        Assert.Equal(ListeImmuable<MessageIA>.De(new[] { new MessageIA(RoleMessage.Agent, PromptFiche.Tour(contexte)) }), PromptFiche.Messages(contexte));
    }

    /// <summary>
    /// Les consignes sont, à l'octet près, le méta-prompt sondé (consignes-A7.txt, fins de ligne en « \n », blancs de
    /// bord retirés, seuil de DEB-Q120 atteint, DEB-Q121), dont la règle 6 reçoit la phrase de DEB-Q122. Empreinte
    /// calculée par la réplique Python sur ce texte, jamais sur la sortie du code.
    /// </summary>
    [Fact]
    public void Consignes_meta_prompt_sonde_plus_DEB_Q122_a_l_octet()
    {
        var octets = Encoding.UTF8.GetBytes(PromptFiche.Consignes);

        Assert.Equal(9667, octets.Length);
        Assert.Equal("FEC0FACB4CCAB1357DB67002F49FB0C903CC38D7210E6B21B257438440582C90", Convert.ToHexString(SHA256.HashData(octets)));
    }

    [Fact]
    public void Consignes_role_nature_regles_contrat_puis_rubriques_du_catalogue()
    {
        var consignes = PromptFiche.Consignes;

        Assert.StartsWith(
            "Tu aides un agent de l'administration à rédiger la fiche d'expression de besoin (FEB) d'une demande adressée au service informatique.",
            consignes);
        Assert.Contains("pose, après ton message, la question que tu as rédigée pour elle, ou à défaut sa question type.\n", consignes);
        Assert.Contains("Nature de la demande : au premier message, repère sa nature parmi : panne ou incident", consignes);
        Assert.Contains("ne la propose jamais comme valeur. Aux tours suivants, déduis-la de l'historique.\n", consignes);
        Assert.Contains("Règles des propositions :\n1. N'invente jamais rien.", consignes);
        Assert.Contains("réponds alors horsPerimetre = true, sans aucune proposition ni question", consignes);
        Assert.Contains("Règles des questions :\n12. Le message du tour donne les « Rubriques à questionner », dans l'ordre.", consignes);
        Assert.Contains("Jamais de question pour une rubrique que tu proposes, ni pour resume, ni pour pointsAClarifier.\n", consignes);
        Assert.Contains("13. Une question tient en une seule phrase, terminée par « ? », sans autre « ? », de vingt-cinq mots au plus.\n", consignes);
        Assert.Contains(
            "\"questions\": [ { \"rubrique\": code, \"question\": texte } ], \"horsPerimetre\": booléen }.\n",
            consignes);
        Assert.Contains(
            "- criteresReussite : Critères de réussite (obligatoire) — ce qui montrera que la demande est satisfaite. Panne : ce que l'agent devra retrouver pour reprendre son travail (démarrage, applications, fichiers, accès).",
            consignes);
        Assert.EndsWith(
            "- pointsAClarifier : Points restant à clarifier — les questions restées ouvertes, rédigées par toi (règles 4 et 8).\n"
            + "Les rubriques obligatoires sont questionnées dans cet ordre, resume en dernier.",
            consignes);
        Assert.DoesNotContain("\r", consignes);

        var regles = Enumerable.Range(1, 17).Select(numero => consignes.IndexOf("\n" + numero + ". ", StringComparison.Ordinal)).ToArray();
        Assert.All(regles, position => Assert.True(position > 0));
        Assert.Equal(regles.Order().ToArray(), regles);
    }

    /// <summary>
    /// DEB-Q122 : le message de l'IA n'annonce jamais la suite (sonde A7 : « Je vous propose maintenant de définir une
    /// date limite » quand l'application relançait sur le résumé). DEB-Q103 reste : aucune question dans le message.
    /// </summary>
    [Fact]
    public void Consignes_message_sans_question_ni_annonce_de_la_suite()
    {
        var consignes = PromptFiche.Consignes;

        Assert.Contains(
            "6. Ton message ne contient aucune question, même reformulée : il accuse réception et dit ce que tu proposes, en mots simples, "
            + "en deux phrases au plus. Il n'annonce jamais la suite (ni la rubrique suivante, ni la question à venir) : l'application la "
            + "choisit après ta réponse. Les questions vont seulement dans « questions » (règles 12 à 17).\n",
            consignes);
        Assert.Contains("un exemple éventuel ne contient aucun fait que l'agent n'a pas donné (ni date, ni cause, ni chiffre).\n", consignes);
        foreach (var definition in Catalogue.Toutes.Where(definition => definition.QuestionType is not null))
        {
            Assert.DoesNotContain(definition.QuestionType!, consignes);
        }
    }

    [Fact]
    public void Guide_chaque_rubrique_a_un_role_sans_question()
    {
        foreach (var definition in Catalogue.Toutes)
        {
            var guide = PromptFiche.Guide(definition.Rubrique);
            Assert.False(string.IsNullOrWhiteSpace(guide), definition.Code);
            Assert.EndsWith(".", guide);
            Assert.DoesNotContain("?", guide);
            Assert.Contains($"- {definition.Code} : {definition.Libelle}", PromptFiche.Consignes);
            Assert.Contains(" — " + guide + "\n", PromptFiche.Consignes);
        }
    }

    [Fact]
    public void Guide_rubrique_inconnue_leve()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => PromptFiche.Guide((Rubrique)200));
        Assert.Equal("rubrique", exception.ParamName);
    }

    /// <summary>Schéma de la sonde A7 (Sonder-Question.ps1), codes du catalogue énumérés pour les deux tableaux.</summary>
    [Fact]
    public void Schema_de_la_reponse_codes_du_catalogue_enumeres()
    {
        const string attendu =
            "{\"type\":\"object\",\"additionalProperties\":false,\"required\":[\"message\",\"propositions\","
            + "\"questions\",\"horsPerimetre\"],\"properties\":{\"message\":{\"type\":\"string\"},"
            + "\"propositions\":{\"type\":\"array\",\"items\":{\"type\":\"object\",\"additionalProperties\":false,"
            + "\"required\":[\"rubrique\",\"valeur\",\"justification\"],\"properties\":{\"rubrique\":{\"type\":\"string\","
            + "\"enum\":[\"application\",\"intitule\",\"resume\",\"situationActuelle\",\"difficultes\","
            + "\"exempleRepresentatif\",\"resultatRecherche\",\"criteresReussite\",\"solutionEnvisagee\","
            + "\"utilisateursConcernes\",\"processusMetier\",\"liens\",\"beneficesAttendus\",\"echeance\",\"contraintes\","
            + "\"documentsReferences\",\"pointsAClarifier\"]},\"valeur\":{\"type\":\"string\"},"
            + "\"justification\":{\"type\":\"string\"}}}},\"questions\":{\"type\":\"array\",\"items\":{\"type\":\"object\","
            + "\"additionalProperties\":false,\"required\":[\"rubrique\",\"question\"],"
            + "\"properties\":{\"rubrique\":{\"type\":\"string\",\"enum\":[\"application\",\"intitule\",\"resume\","
            + "\"situationActuelle\",\"difficultes\",\"exempleRepresentatif\",\"resultatRecherche\",\"criteresReussite\","
            + "\"solutionEnvisagee\",\"utilisateursConcernes\",\"processusMetier\",\"liens\",\"beneficesAttendus\","
            + "\"echeance\",\"contraintes\",\"documentsReferences\",\"pointsAClarifier\"]},"
            + "\"question\":{\"type\":\"string\"}}}},\"horsPerimetre\":{\"type\":\"boolean\"}}}";

        Assert.Equal(attendu, PromptFiche.SchemaJson);
        Assert.Equal(attendu, PromptFiche.Schema.GetRawText());
        Assert.Equal("reponse_fiche", PromptFiche.NomSchema);
    }
}
