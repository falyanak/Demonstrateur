using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// Prompt de l'IA réelle (DEB-Q88 ; questionnement DEB-A3, DEB-A4, question déterministe DEB-Q100) : attendus écrits à la main, recalculés par une réplique indépendante (Python) avant
/// livraison ; jamais recopiés de la sortie.
/// </summary>
public sealed class PromptFicheTests
{
    private const string Demande = "Contrôle des plates-formes e-commerce et prélèvements à distance";

    private static EtatRubrique EtatDe(string code, string? valeur = null, string? proposition = null) =>
        new(code, Catalogue.Toutes.Single(definition => definition.Code == code).Libelle, true, valeur, proposition);

    private static ContexteTour Contexte(string message, string? prochaine, EtatRubrique[] rubriques, string[] manquantes, params Echange[] historique) =>
        new(message, ListeImmuable<Echange>.De(historique), ListeImmuable<EtatRubrique>.De(rubriques), ListeImmuable<string>.De(manquantes), prochaine);

    private const string RelanceApplication =
        "Je comprends une demande d'évolution.\n\nQuelle application ou quel équipement est concerné ?";

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
    public void Premier_tour_demande_initiale_sans_rubrique_questionnee()
    {
        var contexte = Contexte(Demande, "application", [EtatDe("application"), EtatDe("intitule")], ["application", "intitule"]);

        Assert.Equal(
            "État de la fiche :\n"
            + "- application : vide\n"
            + "- intitule : vide\n"
            + "Rubriques obligatoires manquantes : application, intitule\n"
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
            DeuxEchanges);

        Assert.Equal(
            "État de la fiche :\n"
            + "- application : retenue « SORA »\n"
            + "- intitule : proposition en attente « Contrôle e-commerce »\n"
            + "- situationActuelle : vide\n"
            + "- difficultes : retenue « Saisie double » ; proposition en attente « Saisies en double »\n"
            + "- resultatRecherche : vide\n"
            + "Rubriques obligatoires manquantes : intitule, situationActuelle, resultatRecherche\n"
            + "Rubrique questionnée : application\n"
            + "\n"
            + "Message de l'agent :\n"
            + "Les contrôleurs saisissent tout à la main.",
            PromptFiche.Tour(contexte));
    }

    [Theory]
    [InlineData("Noté.\n\nQue se passe-t-il aujourd'hui, concrètement ?", "application", "situationActuelle")]
    [InlineData("Noté.\n\nToutes les rubriques obligatoires sont renseignées : envoyez « résumé » pour que je le propose.", null, "resume")]
    [InlineData("Quelle application est concernée ?", "difficultes", "difficultes")]
    [InlineData("Relisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.", null, null)]
    public void Rubrique_questionnee_relance_reconnue_sinon_prochaine_rubrique(string dernierMessageIA, string? prochaine, string? attendue)
    {
        // Ligne 3 : message antérieur à A5 (question de l'IA), la prochaine rubrique du contexte le remplace.
        var contexte = Contexte("x", prochaine, [], [], new Echange(AuteurEchange.Agent, Demande, Essais.T0), new Echange(AuteurEchange.IA, dernierMessageIA, Essais.T0));

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
            new Echange(AuteurEchange.IA, RelanceApplication, Essais.T0),
            new Echange(AuteurEchange.Agent, "SORA", Essais.T0),
            new Echange(AuteurEchange.IA, "Noté.\n\nQue se passe-t-il aujourd'hui, concrètement ?", Essais.T0),
            new Echange(AuteurEchange.Agent, "Tout à la main", Essais.T0));

        Assert.Equal("situationActuelle", PromptFiche.Questionnee(contexte));
    }

    [Fact]
    public void Rubrique_questionnee_historique_sans_message_de_l_IA_prochaine_rubrique()
    {
        var contexte = Contexte("x", "intitule", [], [], new Echange(AuteurEchange.Agent, Demande, Essais.T0));

        Assert.Equal("intitule", PromptFiche.Questionnee(contexte));
        Assert.Contains("Rubrique questionnée : intitule\n\nMessage de l'agent :\nx", PromptFiche.Tour(contexte));
    }

    [Fact]
    public void Plus_de_rubrique_questionnee()
    {
        var contexte = Contexte(
            "C'est tout.",
            null,
            [EtatDe("application", "SORA")],
            [],
            new Echange(AuteurEchange.Agent, Demande, Essais.T0),
            new Echange(AuteurEchange.IA, "Noté.\n\nRelisez la fiche, acceptez ou corrigez les propositions, puis confirmez et soumettez.", Essais.T0));

        Assert.Equal(
            "État de la fiche :\n"
            + "- application : retenue « SORA »\n"
            + "Rubriques obligatoires manquantes : aucune\n"
            + "Rubrique questionnée : aucune\n"
            + "\n"
            + "Message de l'agent :\n"
            + "C'est tout.",
            PromptFiche.Tour(contexte));
    }

    [Fact]
    public void Messages_historique_dans_l_ordre_puis_le_tour()
    {
        var contexte = Contexte("Les contrôleurs saisissent tout à la main.", "situationActuelle", [EtatDe("situationActuelle")], ["situationActuelle"], DeuxEchanges);

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
        var contexte = Contexte(Demande, "application", [EtatDe("application")], ["application"]);
        Assert.Equal(ListeImmuable<MessageIA>.De(new[] { new MessageIA(RoleMessage.Agent, PromptFiche.Tour(contexte)) }), PromptFiche.Messages(contexte));
    }

    [Fact]
    public void Consignes_role_regles_contrat_puis_rubriques_du_catalogue()
    {
        var consignes = PromptFiche.Consignes;

        Assert.StartsWith(
            "Tu aides un agent de l'administration à rédiger la fiche d'expression de besoin (FEB) d'une demande adressée au service informatique.",
            consignes);
        Assert.Contains("l'application pose elle-même les questions, après ta réponse.\n", consignes);
        Assert.Contains("1. N'invente jamais rien.", consignes);
        Assert.Contains("S'il ne l'a pas dit, ne propose rien.\n", consignes);
        Assert.Contains(
            "assistance utilisateur (poste de travail, matériel, logiciels, comptes, accès et habilitations), exploitation des serveurs, réseau et télécommunications, applications existantes (maintenance, évolution), développement applicatif.",
            consignes);
        Assert.Contains("repère dans la demande le service, la tâche ou la prestation attendue du service informatique", consignes);
        Assert.Contains("sans en faire une valeur de rubrique, puis applique la règle 5.\n", consignes);
        Assert.Contains("sur la « Rubrique questionnée » : propose sa réponse pour cette rubrique, au plus près de ses mots, même brève.", consignes);
        Assert.Contains("propose-la telle quelle et ajoute la précision manquante, sous forme de question, dans pointsAClarifier.", consignes);
        Assert.Contains("seulement si la demande ne porte sur aucun service, aucune tâche ni aucune prestation", consignes);
        Assert.Contains("réponds alors horsPerimetre = true, sans aucune proposition", consignes);
        Assert.Contains(
            "Une panne, un incident, une demande de matériel, d'accès ou d'habilitation relèvent du service informatique : horsPerimetre = false.",
            consignes);
        Assert.Contains("10. Quand la « Rubrique questionnée » est resume, l'agent demande le résumé : propose-le", consignes);
        Assert.Contains(
            "Réponse : un objet JSON seulement, { \"message\": texte pour l'agent, \"propositions\": [ { \"rubrique\": code, \"valeur\": texte, \"justification\": texte } ], \"horsPerimetre\": booléen }.",
            consignes);
        Assert.EndsWith(
            "Rubriques de la fiche (code : libellé — rôle), dans l'ordre du modèle :\n"
            + "- application : Application ou service concerné (obligatoire) — l'application, le service ou l'élément en cause (nom d'application, poste de travail, matériel, serveur, réseau).\n"
            + "- intitule : Intitulé court (obligatoire) — le titre de la demande, huit mots au plus, tiré des mots de l'agent.\n"
            + "- resume : Résumé (obligatoire) — la synthèse des seules valeurs retenues et proposées, proposée quand l'agent la demande (règle 10).\n"
            + "- situationActuelle : Situation actuelle (obligatoire) — ce qui se passe aujourd'hui ; pour une panne ou un incident, le symptôme constaté.\n"
            + "- difficultes : Difficultés rencontrées (obligatoire) — les conséquences sur le travail de l'agent ou de son service, pas le symptôme.\n"
            + "- exempleRepresentatif : Exemple représentatif — un cas précis donné par l'agent.\n"
            + "- resultatRecherche : Résultat recherché (obligatoire) — ce que l'agent attend du service informatique ; pour une panne, la remise en état ou un remplacement.\n"
            + "- criteresReussite : Critères de réussite (obligatoire) — ce qui montrera que la demande est satisfaite.\n"
            + "- solutionEnvisagee : Solution envisagée — une solution que l'agent a en tête, qui n'est pas le besoin (règle 7).\n"
            + "- utilisateursConcernes : Utilisateurs ou services concernés (obligatoire) — les personnes ou services touchés.\n"
            + "- processusMetier : Processus métier (obligatoire) — l'activité de travail touchée.\n"
            + "- liens : Liens avec d'autres outils ou demandes — les autres outils ou demandes liés.\n"
            + "- beneficesAttendus : Bénéfices attendus (obligatoire) — ce que la réponse apportera au travail de l'agent ou du service.\n"
            + "- echeance : Échéance et justification — la date souhaitée et sa raison.\n"
            + "- contraintes : Contraintes identifiées — les contraintes connues (règles, technique, calendrier).\n"
            + "- documentsReferences : Documents ou références — les documents ou références cités par l'agent.\n"
            + "- pointsAClarifier : Points restant à clarifier — les questions restées ouvertes, rédigées par toi (règles 4 et 8).\n"
            + "Les rubriques obligatoires sont questionnées dans cet ordre, resume en dernier.",
            consignes);
        Assert.DoesNotContain("\r", consignes);
    }

    /// <summary>
    /// DEB-Q100 (lot A5) : l'IA ne pose plus de question ; les consignes ne lui en demandent aucune et ne lui donnent
    /// plus de question type (elle est dans le <see cref="Catalogue"/>, DEB-Q101). Les acquis de DEB-A4 restent : panne
    /// reprise au premier message, exemple sans fait inventé.
    /// </summary>
    [Fact]
    public void Consignes_aucune_question_demandee_a_l_IA()
    {
        var consignes = PromptFiche.Consignes;

        Assert.Contains(
            "6. Ne pose aucune question dans ton message, même reformulée : l'application ajoute après lui la question suivante.",
            consignes);
        Assert.Contains("11. Si l'agent dit ne pas comprendre la question, explique-la plus simplement d'après le rôle de la rubrique (liste ci-dessous), sans poser de question ;", consignes);
        Assert.Contains("un exemple éventuel ne contient aucun fait que l'agent n'a pas donné (ni date, ni cause, ni chiffre).\n", consignes);
        Assert.Contains("pour une panne ou un incident, le symptôme décrit va dans situationActuelle.\n", consignes);
        Assert.DoesNotContain("Question type", consignes);
        Assert.DoesNotContain("pose une question", consignes);
        Assert.DoesNotContain("Pose une seule question", consignes);
        Assert.DoesNotContain("demande le besoin", consignes);
        Assert.DoesNotContain("Rubriques à questionner", consignes);
        foreach (var definition in Catalogue.Toutes.Where(definition => definition.QuestionType is not null))
        {
            Assert.DoesNotContain(definition.QuestionType!, consignes);
        }

        Assert.True(
            consignes.IndexOf("10. Quand la", StringComparison.Ordinal) < consignes.IndexOf("11. Si l'agent", StringComparison.Ordinal),
            "règle 11 après la règle 10");
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

    [Fact]
    public void Schema_de_la_reponse_codes_du_catalogue_enumeres()
    {
        const string attendu =
            "{\"type\":\"object\",\"additionalProperties\":false,\"required\":[\"message\",\"propositions\",\"horsPerimetre\"],"
            + "\"properties\":{\"message\":{\"type\":\"string\"},\"propositions\":{\"type\":\"array\",\"items\":{\"type\":\"object\","
            + "\"additionalProperties\":false,\"required\":[\"rubrique\",\"valeur\",\"justification\"],\"properties\":{\"rubrique\":"
            + "{\"type\":\"string\",\"enum\":[\"application\",\"intitule\",\"resume\",\"situationActuelle\",\"difficultes\","
            + "\"exempleRepresentatif\",\"resultatRecherche\",\"criteresReussite\",\"solutionEnvisagee\",\"utilisateursConcernes\","
            + "\"processusMetier\",\"liens\",\"beneficesAttendus\",\"echeance\",\"contraintes\",\"documentsReferences\","
            + "\"pointsAClarifier\"]},\"valeur\":{\"type\":\"string\"},\"justification\":{\"type\":\"string\"}}}},"
            + "\"horsPerimetre\":{\"type\":\"boolean\"}}}";

        Assert.Equal(attendu, PromptFiche.SchemaJson);
        Assert.Equal(attendu, PromptFiche.Schema.GetRawText());
        Assert.Equal("reponse_fiche", PromptFiche.NomSchema);
    }
}
