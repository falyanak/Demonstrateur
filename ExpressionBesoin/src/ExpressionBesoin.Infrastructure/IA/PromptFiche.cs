using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>Auteur d'un message transmis à l'IA réelle : l'agent (rôle « user ») ou l'IA (rôle « assistant »).</summary>
public enum RoleMessage : byte
{
    Agent,
    IA,
}

/// <summary>Message transmis à l'IA réelle, indépendant de la bibliothèque qui l'envoie.</summary>
public sealed record MessageIA(RoleMessage Role, string Texte);

/// <summary>
/// Prompt de l'IA réelle (lot A1, DEB-Q88 ; périmètre, lot A2, DEB-Q89 ; questionnement, lots A3 à A5, DEB-A3, DEB-A4,
/// DEB-Q100 ; question contextualisée, lot A7, DEB-Q116 à DEB-Q120, DEB-Q122), fonctions pures. Méta-prompt d'A7, sondé
/// avant le lot (consignes-A7.txt, seuil de DEB-Q120 atteint, DEB-Q121) : l'IA repère la nature de la demande pour
/// adapter ses questions, propose, et rédige des questions candidates dans le champ <c>questions</c> ; l'application
/// choisit la rubrique après la fusion et pose la candidate retenue ou la question type (<see cref="Dialogue.Composer"/>).
/// Partie FIXE en tête (<see cref="Consignes"/> : rôle, nature de la demande, règles des propositions et des questions,
/// contrat de réponse, rubriques du <see cref="Catalogue"/> avec leur <see cref="Guide"/>), identique d'un tour à l'autre
/// pour profiter du cache du fournisseur (plan §4) ; partie VARIABLE ensuite (<see cref="Messages"/> : historique, puis
/// un dernier message qui porte l'état de la fiche, les rubriques à questionner, la rubrique questionnée et le message de
/// l'agent).
/// Réponse contrainte par <see cref="Schema"/> (DEB-Q85), relue par <see cref="ReponseIA.Lire"/> comme celle de l'IA
/// scénarisée. Textes en « \n », jamais <c>Environment.NewLine</c> : mêmes octets sur tout poste.
/// </summary>
public static class PromptFiche
{
    /// <summary>Nom du schéma de réponse transmis au fournisseur (<c>response_format.json_schema.name</c>).</summary>
    public const string NomSchema = "reponse_fiche";

    public const string Vide = "vide";

    public const string Aucune = "aucune";

    private static readonly string CodeResume = Catalogue.Definition(Rubrique.Resume).Code;

    /// <summary>Consignes système : partie fixe du prompt, construite une fois (après <see cref="CodeResume"/>).</summary>
    public static readonly string Consignes = ConstruireConsignes();

    /// <summary>Schéma JSON de la réponse (contrat du plan §2), codes de rubrique énumérés : texte compact.</summary>
    public static readonly string SchemaJson = ConstruireSchema();

    /// <summary><see cref="SchemaJson"/> analysé une fois, pour <c>ChatResponseFormat.ForJsonSchema</c>.</summary>
    public static readonly JsonElement Schema = Analyser(SchemaJson);

    /// <summary>
    /// Messages d'un tour : l'historique dans l'ordre (agent → <see cref="RoleMessage.Agent"/>, IA →
    /// <see cref="RoleMessage.IA"/>), puis le tour courant (<see cref="Tour"/>).
    /// </summary>
    public static ListeImmuable<MessageIA> Messages(ContexteTour contexte) =>
        ListeImmuable<MessageIA>.De(contexte.Historique
            .Select(echange => new MessageIA(echange.Auteur == AuteurEchange.Agent ? RoleMessage.Agent : RoleMessage.IA, echange.Texte))
            .Append(new MessageIA(RoleMessage.Agent, Tour(contexte))));

    /// <summary>
    /// Dernier message : état de la fiche, rubriques obligatoires manquantes, rubriques à questionner dans l'ordre (A7,
    /// règle 12 des consignes), puis, au premier message, sa mention, et ensuite la rubrique questionnée
    /// (<see cref="Questionnee"/>), enfin le message de l'agent.
    /// </summary>
    internal static string Tour(ContexteTour contexte)
    {
        var texte = new StringBuilder(1024);
        texte.Append("État de la fiche :\n");
        foreach (var etat in contexte.Rubriques)
        {
            texte.Append("- ").Append(etat.Code).Append(" : ").Append(Etat(etat)).Append('\n');
        }

        texte.Append("Rubriques obligatoires manquantes : ")
            .Append(contexte.Manquantes.Count == 0 ? Aucune : string.Join(", ", contexte.Manquantes))
            .Append('\n');
        texte.Append("Rubriques à questionner : ")
            .Append(contexte.AQuestionner.Count == 0 ? Aucune : string.Join(", ", contexte.AQuestionner))
            .Append('\n');
        if (contexte.Historique.Count == 0)
        {
            texte.Append("Premier message de l'agent : sa demande initiale.\n");
        }
        else
        {
            texte.Append("Rubrique questionnée : ").Append(Questionnee(contexte) ?? Aucune).Append('\n');
        }

        texte.Append("\nMessage de l'agent :\n").Append(contexte.Message);
        return texte.ToString();
    }

    /// <summary>État d'une rubrique : vide, valeur retenue, proposition en attente, ou les deux.</summary>
    internal static string Etat(EtatRubrique etat)
    {
        var proposition = etat.PropositionEnAttente is null ? null : $"proposition en attente « {etat.PropositionEnAttente} »";
        if (etat.Valeur is null)
        {
            return proposition ?? Vide;
        }

        var retenue = $"retenue « {etat.Valeur} »";
        return proposition is null ? retenue : retenue + " ; " + proposition;
    }

    /// <summary>
    /// Code de la rubrique sur laquelle portait la dernière question de l'application : lue dans le dernier message de
    /// l'IA de l'historique (<see cref="Dialogue.RubriqueQuestionnee"/>), exacte même si l'agent a modifié la fiche entre
    /// deux tours (leçon de DEB-A3) ; à défaut (historique antérieur à A5, relance de fin, hors périmètre), la prochaine
    /// rubrique du contexte ; <c>null</c> s'il n'y en a pas.
    /// </summary>
    internal static string? Questionnee(ContexteTour contexte)
    {
        var dernier = contexte.Historique.LastOrDefault(echange => echange.Auteur == AuteurEchange.IA);
        return dernier is not null && Dialogue.RubriqueQuestionnee(dernier.Texte) is { } rubrique
            ? Catalogue.Definition(rubrique).Code
            : contexte.ProchaineRubrique;
    }

    /// <summary>
    /// Rôle d'une rubrique et ce qu'il faut y chercher selon la nature de la demande (DEB-A4, DEB-Q92, révisée par
    /// DEB-Q101 ; nature de la demande, lot A7, DEB-Q116) : consigne pour l'IA, rédigée pour un agent non informaticien ;
    /// elle oriente les propositions et les questions candidates. La question type est une règle de l'application, dans
    /// le <see cref="Catalogue"/> (A5). Rubrique hors de l'énumération → <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    internal static string Guide(Rubrique rubrique) => rubrique switch
    {
        Rubrique.Application =>
            "l'application, le service ou l'élément en cause. Panne : l'équipement ou le logiciel qui ne "
            + "marche plus. Matériel ou accès : l'équipement, le compte ou l'application visés. Évolution : le "
            + "nom de l'application. Nouveau besoin : l'outil existant, s'il y en a un.",
        Rubrique.Intitule =>
            "le titre de la demande, huit mots au plus, tiré des mots de l'agent.",
        Rubrique.Resume =>
            "la synthèse des seules valeurs retenues et proposées, proposée quand l'agent la demande "
            + "(règle 10).",
        Rubrique.SituationActuelle =>
            "ce qui se passe aujourd'hui. Panne : le symptôme constaté, depuis quand, ce qui a déjà été "
            + "essayé. Matériel ou accès : ce dont l'agent dispose aujourd'hui. Évolution ou nouveau besoin : "
            + "comment le travail se fait aujourd'hui.",
        Rubrique.Difficultes =>
            "les conséquences sur le travail, pas le symptôme. Panne : ce que l'agent ne peut plus faire, et "
            + "s'il a un moyen de contourner. Évolution ou nouveau besoin : les tâches longues, les erreurs, les "
            + "ressaisies.",
        Rubrique.ExempleRepresentatif =>
            "un cas précis donné par l'agent. Panne : le message d'erreur exact ou le moment où le problème "
            + "survient. Évolution ou nouveau besoin : un dossier ou une journée typique.",
        Rubrique.ResultatRecherche =>
            "ce que l'agent attend du service informatique. Panne : remise en état, remplacement, ou poste de "
            + "prêt en attendant. Matériel ou accès : l'équipement ou le droit précis. Évolution ou nouveau "
            + "besoin : ce que l'outil devra permettre de faire.",
        Rubrique.CriteresReussite =>
            "ce qui montrera que la demande est satisfaite. Panne : ce que l'agent devra retrouver pour "
            + "reprendre son travail (démarrage, applications, fichiers, accès). Matériel ou accès : ce qu'il "
            + "pourra faire avec. Évolution ou nouveau besoin : ce qu'il pourra vérifier lui-même (un délai, une "
            + "tâche faite sans ressaisie).",
        Rubrique.SolutionEnvisagee =>
            "une solution que l'agent a en tête, qui n'est pas le besoin (règle 7).",
        Rubrique.UtilisateursConcernes =>
            "les personnes ou services touchés. Panne : l'agent seul, ou des collègues qui ont le même "
            + "problème. Évolution ou nouveau besoin : qui utilise ou utilisera l'outil.",
        Rubrique.ProcessusMetier =>
            "l'activité de travail touchée. Panne : la tâche interrompue. Évolution ou nouveau besoin : la "
            + "procédure ou la mission concernée.",
        Rubrique.Liens =>
            "les autres outils ou demandes liés.",
        Rubrique.BeneficesAttendus =>
            "ce que la réponse apportera au travail de l'agent ou du service. Panne : ce qu'il pourra "
            + "reprendre en priorité, au-delà de la seule remise en état. Évolution ou nouveau besoin : le temps "
            + "gagné, les erreurs évitées.",
        Rubrique.Echeance =>
            "la date souhaitée et sa raison. Panne : l'urgence, d'après une échéance de travail de l'agent.",
        Rubrique.Contraintes =>
            "les contraintes connues (règles, technique, calendrier).",
        Rubrique.DocumentsReferences =>
            "les documents ou références cités par l'agent.",
        Rubrique.PointsAClarifier =>
            "les questions restées ouvertes, rédigées par toi (règles 4 et 8).",
        _ => throw new ArgumentOutOfRangeException(nameof(rubrique), rubrique, "Rubrique inconnue."),
    };

    private static string ConstruireConsignes()
    {
        var texte = new StringBuilder(10240);
        texte.Append(
            "Tu aides un agent de l'administration à rédiger la fiche d'expression de besoin (FEB) d'une demande "
            + "adressée au service informatique. Tu dialogues avec lui en français, en le vouvoyant. À chaque tour, "
            + "tu comprends son message, tu proposes des valeurs de rubrique et tu rédiges des questions candidates. "
            + "L'application choisit elle-même la rubrique questionnée ensuite et pose, après ton message, la "
            + "question que tu as rédigée pour elle, ou à défaut sa question type.\n"
            + "\n"
            + "Le service informatique rend notamment ces services : assistance utilisateur (poste de travail, "
            + "matériel, logiciels, comptes, accès et habilitations), exploitation des serveurs, réseau et "
            + "télécommunications, applications existantes (maintenance, évolution), développement applicatif.\n"
            + "\n"
            + "Nature de la demande : au premier message, repère sa nature parmi : panne ou incident (quelque chose "
            + "ne marche plus ou marche mal) ; matériel, accès ou habilitation (obtenir un équipement, un compte, un "
            + "droit) ; évolution d'une application existante ; nouveau besoin applicatif ; exploitation ou réseau. "
            + "Elle sert seulement à adapter tes questions (guide des rubriques ci-dessous) ; ne la propose jamais "
            + "comme valeur. Aux tours suivants, déduis-la de l'historique.\n"
            + "\n"
            + "Règles des propositions :\n"
            + "1. N'invente jamais rien. Une proposition ne reprend que ce que l'agent a écrit dans ce dialogue, au "
            + "plus près de ses mots. S'il ne l'a pas dit, ne propose rien.\n"
            + "2. Chaque proposition porte une justification qui cite ou résume les mots de l'agent.\n"
            + "3. Au premier message, repère dans la demande le service, la tâche ou la prestation attendue du "
            + "service informatique et nomme-la dans ton message en une courte phrase (par exemple : « Je comprends "
            + "une demande d'assistance utilisateur : prêt d'un ordinateur portable. »), sans en faire une valeur de "
            + "rubrique, puis applique la règle 5.\n"
            + "4. Le dernier message de l'agent répond en général à la question posée par l'application à la fin du "
            + "message précédent (voir l'historique), sur la « Rubrique questionnée » : propose sa réponse pour "
            + "cette rubrique, au plus près de ses mots, même brève. Si elle est imprécise, propose-la telle quelle "
            + "et ajoute la précision manquante, sous forme de question, dans pointsAClarifier. Propose aussi toute "
            + "autre rubrique que le message renseigne clairement. Ne propose rien pour une rubrique dont la valeur "
            + "retenue dit déjà la même chose.\n"
            + "5. Au premier message, propose toute rubrique vide que la demande renseigne clairement, au plus près "
            + "des mots de l'agent : un intitulé court (huit mots au plus) ; l'application, le service ou l'élément "
            + "concerné (poste de travail, matériel, serveur, réseau, nom d'application) ; pour une panne ou un "
            + "incident, le symptôme décrit va dans situationActuelle.\n"
            + "6. Ton message ne contient aucune question, même reformulée : il accuse réception et dit ce que tu "
            + "proposes, en mots simples, en deux phrases au plus. Il n'annonce jamais la suite (ni la rubrique "
            + "suivante, ni la question à venir) : l'application la choisit après ta réponse. Les questions vont "
            + "seulement dans « questions » (règles 12 à 17).\n"
            + "7. La solution envisagée n'est pas le besoin : si l'agent décrit une solution, propose-la dans "
            + "solutionEnvisagee.\n"
            + "8. Ce qui reste flou peut aller dans pointsAClarifier, sous forme de questions.\n"
            + "9. Hors périmètre, seulement si la demande ne porte sur aucun service, aucune tâche ni aucune "
            + "prestation qu'un service informatique peut rendre (réserver une salle, commander des fournitures, "
            + "poser un congé, question de ressources humaines ou de logistique) : réponds alors horsPerimetre = "
            + "true, sans aucune proposition ni question, avec un message qui indique que la demande ne relève pas "
            + "du service informatique. Une panne, un incident, une demande de matériel, d'accès ou d'habilitation "
            + "relèvent du service informatique : horsPerimetre = false.\n"
            + "10. Quand la « Rubrique questionnée » est resume, l'agent demande le résumé : propose-le, synthèse "
            + "des seules valeurs retenues et de celles que tu proposes dans cette réponse, sans rien ajouter, et "
            + "invite l'agent à le relire.\n"
            + "11. Si l'agent dit ne pas comprendre la question, explique-la plus simplement d'après le rôle de la "
            + "rubrique (guide ci-dessous), sans poser de question dans ton message ; un exemple éventuel ne "
            + "contient aucun fait que l'agent n'a pas donné (ni date, ni cause, ni chiffre).\n"
            + "\n"
            + "Règles des questions :\n"
            + "12. Le message du tour donne les « Rubriques à questionner », dans l'ordre. Rédige une question pour "
            + "chacune des trois premières de cette liste que tu ne proposes pas dans cette réponse (moins s'il en "
            + "reste moins). Jamais de question pour une rubrique que tu proposes, ni pour resume, ni pour "
            + "pointsAClarifier.\n"
            + "13. Une question tient en une seule phrase, terminée par « ? », sans autre « ? », de vingt-cinq mots "
            + "au plus.\n"
            + "14. Elle désigne le sujet avec les mots de l'agent (« votre PC », « les prélèvements à distance ») et "
            + "tient compte de tout ce qui est déjà dit, dans l'historique comme dans la fiche : elle ne redemande "
            + "jamais une information déjà donnée ; si la rubrique est en partie renseignée ailleurs, elle demande "
            + "seulement ce qui manque.\n"
            + "15. Elle ne suppose aucun fait que l'agent n'a pas donné (ni cause, ni date, ni chiffre, ni nom). "
            + "Elle peut suggérer deux ou trois pistes de réponse, seulement si elles découlent du rôle de la "
            + "rubrique (par exemple, pour une panne : réparation ou poste de remplacement).\n"
            + "16. Mots simples, vouvoiement, sans jargon technique ; jamais le nom de la rubrique ni un terme de la "
            + "fiche (« critères de réussite », « processus métier », « bénéfices attendus »).\n"
            + "17. Adapte-la à la nature de la demande, d'après le guide des rubriques.\n"
            + "\n"
            + "Réponse : un objet JSON seulement, { \"message\": texte pour l'agent, \"propositions\": [ { "
            + "\"rubrique\": code, \"valeur\": texte, \"justification\": texte } ], \"questions\": [ { \"rubrique\": "
            + "code, \"question\": texte } ], \"horsPerimetre\": booléen }.\n"
            + "\n"
            + "Rubriques de la fiche (code : libellé — rôle ; ce qu'il faut chercher selon la nature), dans l'ordre "
            + "du modèle :\n");
        foreach (var definition in Catalogue.Toutes)
        {
            texte.Append("- ").Append(definition.Code).Append(" : ").Append(definition.Libelle)
                .Append(definition.Obligatoire ? " (obligatoire)" : "").Append(" — ").Append(Guide(definition.Rubrique))
                .Append('\n');
        }

        texte.Append("Les rubriques obligatoires sont questionnées dans cet ordre, ").Append(CodeResume).Append(" en dernier.");
        return texte.ToString();
    }

    private static string ConstruireSchema()
    {
        var tampon = new ArrayBufferWriter<byte>(1024);
        using (var ecrivain = new Utf8JsonWriter(tampon, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            ecrivain.WriteStartObject();
            ecrivain.WriteString("type", "object");
            ecrivain.WriteBoolean("additionalProperties", false);
            Requis(ecrivain, "message", "propositions", "questions", "horsPerimetre");
            ecrivain.WriteStartObject("properties");
            Typer(ecrivain, "message", "string");
            ecrivain.WriteStartObject("propositions");
            ecrivain.WriteString("type", "array");
            ecrivain.WriteStartObject("items");
            ecrivain.WriteString("type", "object");
            ecrivain.WriteBoolean("additionalProperties", false);
            Requis(ecrivain, "rubrique", "valeur", "justification");
            ecrivain.WriteStartObject("properties");
            Rubriques(ecrivain);
            Typer(ecrivain, "valeur", "string");
            Typer(ecrivain, "justification", "string");
            ecrivain.WriteEndObject();
            ecrivain.WriteEndObject();
            ecrivain.WriteEndObject();
            ecrivain.WriteStartObject("questions");
            ecrivain.WriteString("type", "array");
            ecrivain.WriteStartObject("items");
            ecrivain.WriteString("type", "object");
            ecrivain.WriteBoolean("additionalProperties", false);
            Requis(ecrivain, "rubrique", "question");
            ecrivain.WriteStartObject("properties");
            Rubriques(ecrivain);
            Typer(ecrivain, "question", "string");
            ecrivain.WriteEndObject();
            ecrivain.WriteEndObject();
            ecrivain.WriteEndObject();
            Typer(ecrivain, "horsPerimetre", "boolean");
            ecrivain.WriteEndObject();
            ecrivain.WriteEndObject();
        }

        return Encoding.UTF8.GetString(tampon.WrittenSpan);
    }

    /// <summary>Propriété <c>rubrique</c> : texte parmi les codes du <see cref="Catalogue"/>, dans l'ordre du modèle.</summary>
    private static void Rubriques(Utf8JsonWriter ecrivain)
    {
        ecrivain.WriteStartObject("rubrique");
        ecrivain.WriteString("type", "string");
        ecrivain.WriteStartArray("enum");
        foreach (var definition in Catalogue.Toutes)
        {
            ecrivain.WriteStringValue(definition.Code);
        }

        ecrivain.WriteEndArray();
        ecrivain.WriteEndObject();
    }

    private static void Requis(Utf8JsonWriter ecrivain, params string[] noms)
    {
        ecrivain.WriteStartArray("required");
        foreach (var nom in noms)
        {
            ecrivain.WriteStringValue(nom);
        }

        ecrivain.WriteEndArray();
    }

    private static void Typer(Utf8JsonWriter ecrivain, string propriete, string type)
    {
        ecrivain.WriteStartObject(propriete);
        ecrivain.WriteString("type", type);
        ecrivain.WriteEndObject();
    }

    private static JsonElement Analyser(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
