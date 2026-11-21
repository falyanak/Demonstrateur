using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// Prompt de l'IA réelle (DEB-Q88 ; questionnement DEB-A3, DEB-A4) : attendus écrits à la main, recalculés par une réplique indépendante (Python) avant
/// livraison ; jamais recopiés de la sortie.
/// </summary>
public sealed class PromptFicheTests
{
    private const string Demande = "Contrôle des plates-formes e-commerce et prélèvements à distance";

    private static EtatRubrique EtatDe(string code, string? valeur = null, string? proposition = null) =>
        new(code, Catalogue.Toutes.Single(definition => definition.Code == code).Libelle, true, valeur, proposition);

    private static ContexteTour Contexte(string message, string? prochaine, EtatRubrique[] rubriques, string[] manquantes, params Echange[] historique) =>
        new(message, ListeImmuable<Echange>.De(historique), ListeImmuable<EtatRubrique>.De(rubriques), ListeImmuable<string>.De(manquantes), prochaine);

    private static readonly Echange[] DeuxEchanges =
    [
        new(AuteurEchange.Agent, Demande, Essais.T0),
        new(AuteurEchange.IA, "Quelle application est concernée ?", Essais.T0.AddSeconds(2)),
    ];

    [Theory]
    [InlineData(null, null, "vide")]
    [InlineData("SORA", null, "retenue « SORA »")]
    [InlineData(null, "Contrôle e-commerce", "proposition en attente « Contrôle e-commerce »")]
    [InlineData("Saisie double", "Saisies en double", "retenue « Saisie double » ; proposition en attente « Saisies en double »")]
    public void Etat_d_une_rubrique(string? valeur, string? proposition, string attendu) =>
        Assert.Equal(attendu, PromptFiche.Etat(EtatDe("difficultes", valeur, proposition)));

    [Fact]
    public void Premier_tour_demande_initiale_et_rubrique_a_questionner()
    {
        var contexte = Contexte(Demande, "application", [EtatDe("application"), EtatDe("intitule")], ["application", "intitule"]);

        Assert.Equal(
            "État de la fiche :\n"
            + "- application : vide\n"
            + "- intitule : vide\n"
            + "Rubriques obligatoires manquantes : application, intitule\n"
            + "Premier message de l'agent : sa demande initiale.\n"
            + "Rubriques à questionner, dans l'ordre : application, intitule\n"
            + "\n"
            + "Message de l'agent :\n"
            + "Contrôle des plates-formes e-commerce et prélèvements à distance",
            PromptFiche.Tour(contexte));
    }

    [Fact]
    public void Tour_suivant_rubriques_a_questionner_vides_et_sans_proposition()
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
            + "Rubriques à questionner, dans l'ordre : situationActuelle, resultatRecherche\n"
            + "\n"
            + "Message de l'agent :\n"
            + "Les contrôleurs saisissent tout à la main.",
            PromptFiche.Tour(contexte));
    }

    /// <summary>
    /// DEB-A3 (dialogue du 21/11/2026) : l'IA avait questionné les bénéfices sans rien proposer pour le processus métier ;
    /// le contexte désignait alors le processus comme « dernière question » et les bénéfices comme suivante, et l'IA a
    /// reposé la question des bénéfices. Désormais, aucune dernière question n'est déduite : la liste ordonnée seule.
    /// </summary>
    [Fact]
    public void Aucune_derniere_question_deduite_liste_ordonnee_seule()
    {
        var contexte = Contexte(
            "pouvoir appeler mes collègues",
            "processusMetier",
            [EtatDe("utilisateursConcernes", "Moi seul."), EtatDe("processusMetier"), EtatDe("beneficesAttendus"), EtatDe("resume")],
            ["processusMetier", "beneficesAttendus", "resume"],
            DeuxEchanges);

        var tour = PromptFiche.Tour(contexte);

        Assert.Equal(new[] { "processusMetier", "beneficesAttendus", "resume" }, PromptFiche.AQuestionner(contexte).ToArray());
        Assert.Contains("Rubriques à questionner, dans l'ordre : processusMetier, beneficesAttendus, resume\n\nMessage de l'agent :\n", tour);
        Assert.DoesNotContain("Dernière question", tour);
        Assert.DoesNotContain("Premier message", tour);
    }

    [Fact]
    public void A_questionner_ordre_de_questionnement_obligatoires_libres_du_contexte()
    {
        var contexte = Contexte(
            "x",
            "beneficesAttendus",
            [EtatDe("resume"), EtatDe("exempleRepresentatif"), EtatDe("application", proposition: "SORA"), EtatDe("beneficesAttendus")],
            [],
            DeuxEchanges);

        Assert.Equal(new[] { "beneficesAttendus", "resume" }, PromptFiche.AQuestionner(contexte).ToArray());
    }

    [Fact]
    public void Plus_de_rubrique_a_questionner()
    {
        var contexte = Contexte("C'est tout.", null, [EtatDe("application", "SORA")], [], DeuxEchanges);

        Assert.Equal(
            "État de la fiche :\n"
            + "- application : retenue « SORA »\n"
            + "Rubriques obligatoires manquantes : aucune\n"
            + "Rubriques à questionner, dans l'ordre : aucune\n"
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
                new MessageIA(RoleMessage.IA, "Quelle application est concernée ?"),
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
        Assert.Contains("1. N'invente jamais rien.", consignes);
        Assert.Contains(
            "assistance utilisateur (poste de travail, matériel, logiciels, comptes, accès et habilitations), exploitation des serveurs, réseau et télécommunications, applications existantes (maintenance, évolution), développement applicatif.",
            consignes);
        Assert.Contains("repère dans la demande le service, la tâche ou la prestation attendue du service informatique", consignes);
        Assert.Contains("seulement si la demande ne porte sur aucun service, aucune tâche ni aucune prestation", consignes);
        Assert.Contains("réponds alors horsPerimetre = true, sans aucune proposition", consignes);
        Assert.Contains(
            "Une panne, un incident, une demande de matériel, d'accès ou d'habilitation relèvent du service informatique : horsPerimetre = false.",
            consignes);
        Assert.Contains("Quand il ne reste que resume, ne pose pas de question : propose le résumé", consignes);
        Assert.Contains("sans en faire une valeur de rubrique, puis applique les règles 5 et 6.", consignes);
        Assert.Contains("propose-la telle quelle et ajoute la précision manquante, sous forme de question, dans pointsAClarifier.", consignes);
        Assert.Contains("Ne repose jamais une question à laquelle l'agent a déjà répondu, même en la reformulant.", consignes);
        Assert.Contains("Quand la liste « Rubriques à questionner » est vide (aucune), invite l'agent", consignes);
        Assert.Contains(
            "Réponse : un objet JSON seulement, { \"message\": texte pour l'agent, \"propositions\": [ { \"rubrique\": code, \"valeur\": texte, \"justification\": texte } ], \"horsPerimetre\": booléen }.",
            consignes);
        Assert.EndsWith(
            "Rubriques de la fiche (code : libellé — rôle et question type), dans l'ordre du modèle :\n"
            + "- application : Application ou service concerné (obligatoire) — l'application, le service ou l'élément en cause (nom d'application, poste de travail, matériel, serveur, réseau). Question type : « Quelle application ou quel équipement est concerné ? »\n"
            + "- intitule : Intitulé court (obligatoire) — le titre de la demande, huit mots au plus, tiré des mots de l'agent. Question type : « Comment intituleriez-vous votre demande en quelques mots ? »\n"
            + "- resume : Résumé (obligatoire) — la synthèse des seules valeurs retenues et proposées, en dernier. Pas de question : propose-le (règle 6).\n"
            + "- situationActuelle : Situation actuelle (obligatoire) — ce qui se passe aujourd'hui ; pour une panne ou un incident, le symptôme constaté. Question type : « Que se passe-t-il aujourd'hui, concrètement ? »\n"
            + "- difficultes : Difficultés rencontrées (obligatoire) — les conséquences sur le travail de l'agent ou de son service, pas le symptôme. Question type : « Qu'est-ce que cela vous empêche de faire dans votre travail ? »\n"
            + "- exempleRepresentatif : Exemple représentatif — un cas précis donné par l'agent. Question type : « Pouvez-vous citer un cas précis ? »\n"
            + "- resultatRecherche : Résultat recherché (obligatoire) — ce que l'agent attend du service informatique ; pour une panne, la remise en état ou un remplacement. Question type : « Qu'attendez-vous du service informatique ? »\n"
            + "- criteresReussite : Critères de réussite (obligatoire) — ce qui montrera que la demande est satisfaite. Question type : « À quoi verrez-vous que c'est réglé ? »\n"
            + "- solutionEnvisagee : Solution envisagée — une solution que l'agent a en tête, qui n'est pas le besoin (règle 7). Question type : « Avez-vous une solution en tête ? »\n"
            + "- utilisateursConcernes : Utilisateurs ou services concernés (obligatoire) — les personnes ou services touchés. Question type : « Qui est concerné : vous seul, votre bureau, d'autres services ? »\n"
            + "- processusMetier : Processus métier (obligatoire) — l'activité de travail touchée. Question type : « Pour quelle activité de votre travail en avez-vous besoin ? »\n"
            + "- liens : Liens avec d'autres outils ou demandes — les autres outils ou demandes liés. Question type : « Cette demande est-elle liée à un autre outil ou à une autre demande ? »\n"
            + "- beneficesAttendus : Bénéfices attendus (obligatoire) — ce que la réponse apportera au travail de l'agent ou du service. Question type : « Qu'est-ce que cela vous permettra de faire une fois réglé ? »\n"
            + "- echeance : Échéance et justification — la date souhaitée et sa raison. Question type : « Pour quand en avez-vous besoin, et pourquoi ? »\n"
            + "- contraintes : Contraintes identifiées — les contraintes connues (règles, technique, calendrier). Question type : « Y a-t-il des contraintes à respecter ? »\n"
            + "- documentsReferences : Documents ou références — les documents ou références cités par l'agent. Question type : « Avez-vous des documents ou des références à signaler ? »\n"
            + "- pointsAClarifier : Points restant à clarifier — les questions restées ouvertes, rédigées par toi (règles 4 et 8). Pas de question.\n"
            + "Les rubriques obligatoires sont questionnées dans cet ordre, resume en dernier.",
            consignes);
        Assert.DoesNotContain("\r", consignes);
    }

    /// <summary>
    /// DEB-A4 (dialogue du 21/11/2026, « mon pc ne s'allume plus ») : la panne n'était proposée pour aucune rubrique au
    /// premier message, l'IA a posé une question incohérente (« Quel est votre poste de travail ? ») puis demandé les
    /// difficultés comme une seconde fois le problème, et son exemple portait des faits inventés. Les consignes portent
    /// désormais le symptôme au premier message, la question type de chaque rubrique, l'interdiction de redemander une
    /// information donnée et un exemple sans fait inventé.
    /// </summary>
    [Fact]
    public void Consignes_panne_symptome_question_type_et_exemple_sans_fait_invente()
    {
        var consignes = PromptFiche.Consignes;

        Assert.Contains("5. Au premier message, propose toute rubrique vide que la demande renseigne clairement", consignes);
        Assert.Contains("pour une panne ou un incident, le symptôme décrit va dans situationActuelle.\n", consignes);
        Assert.Contains("Pars de la question type de cette rubrique (liste ci-dessous), adaptée à la demande, en mots simples.", consignes);
        Assert.Contains(
            "Ne demande jamais une information déjà donnée : dans une valeur retenue, une proposition en attente, une proposition de cette réponse ou un message de l'agent.",
            consignes);
        Assert.Contains(
            "11. Si l'agent ne comprend pas la question, reformule-la plus simplement à partir de sa question type ; un exemple éventuel ne contient aucun fait que l'agent n'a pas donné (ni date, ni cause, ni chiffre).\n",
            consignes);
        Assert.Contains("les conséquences sur le travail de l'agent ou de son service, pas le symptôme.", consignes);
        Assert.True(
            consignes.IndexOf("10. Quand la liste", StringComparison.Ordinal) < consignes.IndexOf("11. Si l'agent", StringComparison.Ordinal),
            "règle 11 après la règle 10");
    }

    [Fact]
    public void Guide_chaque_rubrique_a_un_role_et_les_rubriques_questionnees_une_question_type()
    {
        foreach (var definition in Catalogue.Toutes)
        {
            var guide = PromptFiche.Guide(definition.Rubrique);
            Assert.False(string.IsNullOrWhiteSpace(guide), definition.Code);
            Assert.Contains($"- {definition.Code} : {definition.Libelle}", PromptFiche.Consignes);
            Assert.Contains(" — " + guide + "\n", PromptFiche.Consignes);
        }

        foreach (var rubrique in Catalogue.OrdreQuestionnement.Where(rubrique => rubrique != Rubrique.Resume))
        {
            var guide = PromptFiche.Guide(rubrique);
            Assert.Contains(" Question type : « ", guide);
            Assert.EndsWith(" ? »", guide);
        }

        Assert.Contains("Pas de question", PromptFiche.Guide(Rubrique.Resume));
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
