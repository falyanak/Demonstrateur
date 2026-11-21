using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// Prompt de l'IA réelle (DEB-Q88) : attendus écrits à la main, recalculés par une réplique indépendante (Python) avant
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
        Assert.Contains("sans en faire une valeur de rubrique, puis pose la question de la règle 6.", consignes);
        Assert.Contains("propose-la telle quelle et ajoute la précision manquante, sous forme de question, dans pointsAClarifier.", consignes);
        Assert.Contains("Ne repose jamais une question à laquelle l'agent a déjà répondu, même en la reformulant.", consignes);
        Assert.Contains("Quand la liste « Rubriques à questionner » est vide (aucune), invite l'agent", consignes);
        Assert.Contains(
            "Réponse : un objet JSON seulement, { \"message\": texte pour l'agent, \"propositions\": [ { \"rubrique\": code, \"valeur\": texte, \"justification\": texte } ], \"horsPerimetre\": booléen }.",
            consignes);
        Assert.EndsWith(
            "Rubriques de la fiche (code : libellé), dans l'ordre du modèle :\n"
            + "- application : Application ou service concerné (obligatoire)\n"
            + "- intitule : Intitulé court (obligatoire)\n"
            + "- resume : Résumé (obligatoire)\n"
            + "- situationActuelle : Situation actuelle (obligatoire)\n"
            + "- difficultes : Difficultés rencontrées (obligatoire)\n"
            + "- exempleRepresentatif : Exemple représentatif\n"
            + "- resultatRecherche : Résultat recherché (obligatoire)\n"
            + "- criteresReussite : Critères de réussite (obligatoire)\n"
            + "- solutionEnvisagee : Solution envisagée\n"
            + "- utilisateursConcernes : Utilisateurs ou services concernés (obligatoire)\n"
            + "- processusMetier : Processus métier (obligatoire)\n"
            + "- liens : Liens avec d'autres outils ou demandes\n"
            + "- beneficesAttendus : Bénéfices attendus (obligatoire)\n"
            + "- echeance : Échéance et justification\n"
            + "- contraintes : Contraintes identifiées\n"
            + "- documentsReferences : Documents ou références\n"
            + "- pointsAClarifier : Points restant à clarifier\n"
            + "Les rubriques obligatoires sont questionnées dans cet ordre, resume en dernier.",
            consignes);
        Assert.DoesNotContain("\r", consignes);
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
