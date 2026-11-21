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
/// Prompt de l'IA réelle (lot A1, DEB-Q88 ; périmètre, lot A2, DEB-Q89 ; questionnement, lots A3 et A4, DEB-A3, DEB-A4),
/// fonctions pures. Partie FIXE en tête (<see cref="Consignes"/> : rôle, règles, contrat de réponse, rubriques du
/// <see cref="Catalogue"/> avec leur rôle et leur question type, <see cref="Guide"/>), identique d'un tour à l'autre pour profiter du cache du
/// fournisseur (plan §4) ; partie VARIABLE ensuite (<see cref="Messages"/> : historique, puis un dernier message qui porte
/// l'état de la fiche et le message de l'agent). Réponse contrainte par <see cref="Schema"/> (DEB-Q85), relue par
/// <see cref="ReponseIA.Lire"/> comme celle de l'IA scénarisée. Textes en « \n », jamais <c>Environment.NewLine</c> :
/// mêmes octets sur tout poste.
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
    /// Dernier message : état de la fiche, rubriques obligatoires manquantes, mention du premier message, rubriques encore
    /// à questionner dans l'ordre (<see cref="AQuestionner"/>), puis le message de l'agent. Aucune « dernière question »
    /// n'est déduite (DEB-A3) : l'IA la lit dans l'historique.
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
        if (contexte.Historique.Count == 0)
        {
            texte.Append("Premier message de l'agent : sa demande initiale.\n");
        }

        var aQuestionner = AQuestionner(contexte);
        texte.Append("Rubriques à questionner, dans l'ordre : ")
            .Append(aQuestionner.Count == 0 ? Aucune : string.Join(", ", aQuestionner))
            .Append("\n\nMessage de l'agent :\n")
            .Append(contexte.Message);
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
    /// Rubriques obligatoires encore à questionner, dans l'ordre de questionnement (DEB-Q14, résumé en dernier) : vides et
    /// sans proposition en attente. DEB-A3 : remplace la « dernière question » déduite de la prochaine rubrique, fausse
    /// dès que l'IA n'a rien proposé pour la rubrique qu'elle avait questionnée.
    /// </summary>
    internal static ListeImmuable<string> AQuestionner(ContexteTour contexte) =>
        ListeImmuable<string>.De(Catalogue.OrdreQuestionnement
            .Select(rubrique => Catalogue.Definition(rubrique).Code)
            .Where(code => contexte.Rubriques.Any(etat =>
                etat.Code == code && etat.Valeur is null && etat.PropositionEnAttente is null)));

    /// <summary>
    /// Rôle d'une rubrique et question type (DEB-A4, DEB-Q92) : consignes pour l'IA, rédigées pour un agent non
    /// informaticien et valables pour une évolution comme pour une panne (DEB-Q93) ; elles ne sont pas des règles métier
    /// et restent hors du Domaine. Rubrique hors de l'énumération → <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    internal static string Guide(Rubrique rubrique) => rubrique switch
    {
        Rubrique.Application => "l'application, le service ou l'élément en cause (nom d'application, poste de travail, "
            + "matériel, serveur, réseau). Question type : « Quelle application ou quel équipement est concerné ? »",
        Rubrique.Intitule => "le titre de la demande, huit mots au plus, tiré des mots de l'agent. Question type : "
            + "« Comment intituleriez-vous votre demande en quelques mots ? »",
        Rubrique.Resume => "la synthèse des seules valeurs retenues et proposées, en dernier. Pas de question : propose-le "
            + "(règle 6).",
        Rubrique.SituationActuelle => "ce qui se passe aujourd'hui ; pour une panne ou un incident, le symptôme constaté. "
            + "Question type : « Que se passe-t-il aujourd'hui, concrètement ? »",
        Rubrique.Difficultes => "les conséquences sur le travail de l'agent ou de son service, pas le symptôme. Question "
            + "type : « Qu'est-ce que cela vous empêche de faire dans votre travail ? »",
        Rubrique.ExempleRepresentatif => "un cas précis donné par l'agent. Question type : « Pouvez-vous citer un cas "
            + "précis ? »",
        Rubrique.ResultatRecherche => "ce que l'agent attend du service informatique ; pour une panne, la remise en état "
            + "ou un remplacement. Question type : « Qu'attendez-vous du service informatique ? »",
        Rubrique.CriteresReussite => "ce qui montrera que la demande est satisfaite. Question type : « À quoi verrez-vous "
            + "que c'est réglé ? »",
        Rubrique.SolutionEnvisagee => "une solution que l'agent a en tête, qui n'est pas le besoin (règle 7). Question "
            + "type : « Avez-vous une solution en tête ? »",
        Rubrique.UtilisateursConcernes => "les personnes ou services touchés. Question type : « Qui est concerné : vous "
            + "seul, votre bureau, d'autres services ? »",
        Rubrique.ProcessusMetier => "l'activité de travail touchée. Question type : « Pour quelle activité de votre "
            + "travail en avez-vous besoin ? »",
        Rubrique.Liens => "les autres outils ou demandes liés. Question type : « Cette demande est-elle liée à un autre "
            + "outil ou à une autre demande ? »",
        Rubrique.BeneficesAttendus => "ce que la réponse apportera au travail de l'agent ou du service. Question type : "
            + "« Qu'est-ce que cela vous permettra de faire une fois réglé ? »",
        Rubrique.Echeance => "la date souhaitée et sa raison. Question type : « Pour quand en avez-vous besoin, et "
            + "pourquoi ? »",
        Rubrique.Contraintes => "les contraintes connues (règles, technique, calendrier). Question type : « Y a-t-il des "
            + "contraintes à respecter ? »",
        Rubrique.DocumentsReferences => "les documents ou références cités par l'agent. Question type : « Avez-vous des "
            + "documents ou des références à signaler ? »",
        Rubrique.PointsAClarifier => "les questions restées ouvertes, rédigées par toi (règles 4 et 8). Pas de question.",
        _ => throw new ArgumentOutOfRangeException(nameof(rubrique), rubrique, "Rubrique inconnue."),
    };

    private static string ConstruireConsignes()
    {
        var texte = new StringBuilder(4096);
        texte.Append(
            "Tu aides un agent de l'administration à rédiger la fiche d'expression de besoin (FEB) d'une demande "
            + "adressée au service informatique. Tu dialogues avec lui en français, en le vouvoyant.\n"
            + "\n"
            + "Le service informatique rend notamment ces services : assistance utilisateur (poste de travail, matériel, "
            + "logiciels, comptes, accès et habilitations), exploitation des serveurs, réseau et télécommunications, "
            + "applications existantes (maintenance, évolution), développement applicatif.\n"
            + "\n"
            + "Règles :\n"
            + "1. N'invente jamais rien. Une proposition ne reprend que ce que l'agent a écrit dans ce dialogue, au plus "
            + "près de ses mots. S'il ne l'a pas dit, ne propose rien : pose une question.\n"
            + "2. Chaque proposition porte une justification qui cite ou résume les mots de l'agent.\n"
            + "3. Au premier message, repère dans la demande le service, la tâche ou la prestation attendue du service "
            + "informatique et nomme-la dans ton message en une courte phrase (par exemple : « Je comprends une demande "
            + "d'assistance utilisateur : prêt d'un ordinateur portable. »), sans en faire une valeur de rubrique, puis "
            + "applique les règles 5 et 6.\n"
            + "4. Le dernier message de l'agent répond en général à ta dernière question (voir l'historique) : propose sa "
            + "réponse pour la rubrique sur laquelle elle portait, au plus près de ses mots, même brève. Si elle est "
            + "imprécise, propose-la telle quelle et ajoute la précision manquante, sous forme de question, dans "
            + "pointsAClarifier. Propose aussi toute autre rubrique que le message renseigne clairement. Ne propose rien "
            + "pour une rubrique dont la valeur retenue dit déjà la même chose.\n"
            + "5. Au premier message, propose toute rubrique vide que la demande renseigne clairement, au plus près des mots "
            + "de l'agent : un intitulé court (huit mots au plus) ; l'application, le service ou l'élément concerné "
            + "(poste de travail, matériel, serveur, réseau, nom d'application) ; pour une panne ou un incident, le "
            + "symptôme décrit va dans situationActuelle.\n"
            + "6. Pose une seule question, courte, sur la première rubrique de la liste « Rubriques à questionner » à "
            + "laquelle l'agent n'a pas déjà répondu et que tu ne proposes pas dans cette réponse. Pars de la question "
            + "type de cette rubrique (liste ci-dessous), adaptée à la demande, en mots simples. Ne demande jamais une "
            + "information déjà donnée : dans une valeur retenue, une proposition en attente, une proposition de cette "
            + "réponse ou un message de l'agent. Ne repose jamais une "
            + "question à laquelle l'agent a déjà répondu, même en la reformulant. Quand il ne reste que resume, ne pose "
            + "pas de question : propose le résumé, synthèse des seules valeurs retenues et de celles que tu proposes "
            + "dans cette réponse, sans rien ajouter, et invite l'agent à le relire.\n"
            + "7. La solution envisagée n'est pas le besoin : si l'agent décrit une solution, propose-la dans "
            + "solutionEnvisagee et demande le besoin qu'elle sert.\n"
            + "8. Ce qui reste flou peut aller dans pointsAClarifier, sous forme de questions.\n"
            + "9. Hors périmètre, seulement si la demande ne porte sur aucun service, aucune tâche ni aucune prestation "
            + "qu'un service informatique peut rendre (réserver une salle, commander des fournitures, poser un congé, "
            + "question de ressources humaines ou de logistique) : réponds alors horsPerimetre = true, sans aucune "
            + "proposition, avec un message qui indique que la demande ne relève pas du service informatique, sans poser "
            + "de question. Une panne, un incident, une demande de matériel, d'accès ou d'habilitation relèvent du "
            + "service informatique : horsPerimetre = false.\n"
            + "10. Quand la liste « Rubriques à questionner » est vide (aucune), invite l'agent à relire la fiche, à accepter ou "
            + "corriger les propositions, puis à confirmer et soumettre.\n"
            + "11. Si l'agent ne comprend pas la question, reformule-la plus simplement à partir de sa question type ; un "
            + "exemple éventuel ne contient aucun fait que l'agent n'a pas donné (ni date, ni cause, ni chiffre).\n"
            + "\n"
            + "Réponse : un objet JSON seulement, { \"message\": texte pour l'agent, \"propositions\": [ { \"rubrique\": "
            + "code, \"valeur\": texte, \"justification\": texte } ], \"horsPerimetre\": booléen }.\n"
            + "\n"
            + "Rubriques de la fiche (code : libellé — rôle et question type), dans l'ordre du modèle :\n");
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
            Requis(ecrivain, "message", "propositions", "horsPerimetre");
            ecrivain.WriteStartObject("properties");
            Typer(ecrivain, "message", "string");
            ecrivain.WriteStartObject("propositions");
            ecrivain.WriteString("type", "array");
            ecrivain.WriteStartObject("items");
            ecrivain.WriteString("type", "object");
            ecrivain.WriteBoolean("additionalProperties", false);
            Requis(ecrivain, "rubrique", "valeur", "justification");
            ecrivain.WriteStartObject("properties");
            ecrivain.WriteStartObject("rubrique");
            ecrivain.WriteString("type", "string");
            ecrivain.WriteStartArray("enum");
            foreach (var definition in Catalogue.Toutes)
            {
                ecrivain.WriteStringValue(definition.Code);
            }

            ecrivain.WriteEndArray();
            ecrivain.WriteEndObject();
            Typer(ecrivain, "valeur", "string");
            Typer(ecrivain, "justification", "string");
            ecrivain.WriteEndObject();
            ecrivain.WriteEndObject();
            ecrivain.WriteEndObject();
            Typer(ecrivain, "horsPerimetre", "boolean");
            ecrivain.WriteEndObject();
            ecrivain.WriteEndObject();
        }

        return Encoding.UTF8.GetString(tampon.WrittenSpan);
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
