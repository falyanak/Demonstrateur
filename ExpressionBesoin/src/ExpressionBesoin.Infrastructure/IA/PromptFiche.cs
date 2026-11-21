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
/// Prompt de l'IA réelle (lot A1, DEB-Q88 ; périmètre du service informatique, lot A2, DEB-Q89), fonctions pures. Partie FIXE en tête (<see cref="Consignes"/> : rôle, règles,
/// contrat de réponse, rubriques du <see cref="Catalogue"/>), identique d'un tour à l'autre pour profiter du cache du
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
    /// Dernier message : état de la fiche, rubriques obligatoires manquantes, rubrique questionnée et suivante (même
    /// ordre que l'IA scénarisée : <see cref="ScenarioDialogue.Suivante"/>), puis le message de l'agent.
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
        texte.Append(Questionnement(contexte)).Append("\n\nMessage de l'agent :\n").Append(contexte.Message);
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
    /// Premier tour : la demande initiale, rubrique à questionner ensuite = prochaine rubrique. Tours suivants : la
    /// dernière question portait sur la prochaine rubrique (le message y répond en général), la suivante vient après.
    /// </summary>
    internal static string Questionnement(ContexteTour contexte)
    {
        if (contexte.ProchaineRubrique is not { } cible)
        {
            return "Rubrique à questionner ensuite : " + Aucune;
        }

        if (contexte.Historique.Count == 0)
        {
            return "Premier message de l'agent : sa demande initiale.\nRubrique à questionner ensuite : " + cible;
        }

        return "Dernière question posée sur : " + cible + "\nRubrique à questionner ensuite : "
            + (ScenarioDialogue.Suivante(contexte, cible) ?? Aucune);
    }

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
            + "d'assistance utilisateur : prêt d'un ordinateur portable. »), sans en faire une valeur de rubrique.\n"
            + "4. Propose une valeur pour chaque rubrique que le dernier message de l'agent renseigne clairement, en "
            + "particulier la rubrique sur laquelle portait la dernière question. Ne propose rien pour une rubrique dont "
            + "la valeur retenue dit déjà la même chose.\n"
            + "5. Au premier message, propose pour la rubrique intitule un intitulé court (huit mots au plus) tiré de la "
            + "demande de l'agent, si elle est vide. La rubrique application reçoit l'application, le service ou "
            + "l'élément concerné cité par l'agent (poste de travail, matériel, serveur, réseau, nom d'application).\n"
            + "6. Pose une seule question, courte, sur la rubrique à questionner ensuite. Quand c'est resume, ne pose "
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
            + "10. Quand il n'y a plus de rubrique à questionner, invite l'agent à relire la fiche, à accepter ou "
            + "corriger les propositions, puis à confirmer et soumettre.\n"
            + "\n"
            + "Réponse : un objet JSON seulement, { \"message\": texte pour l'agent, \"propositions\": [ { \"rubrique\": "
            + "code, \"valeur\": texte, \"justification\": texte } ], \"horsPerimetre\": booléen }.\n"
            + "\n"
            + "Rubriques de la fiche (code : libellé), dans l'ordre du modèle :\n");
        foreach (var definition in Catalogue.Toutes)
        {
            texte.Append("- ").Append(definition.Code).Append(" : ").Append(definition.Libelle)
                .Append(definition.Obligatoire ? " (obligatoire)" : "").Append('\n');
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
