using System.Collections.Frozen;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Corps des échanges avec l'API compatible OpenAI (lot A5b, DEB-A5, DEB-Q113, DEB-Q114), fonctions pures.
/// <see cref="Nettoyer"/> réécrit les messages de la requête comme CSAgent (<c>LlmClient.SanitizeMessages</c>) : Albert
/// (vLLM) traite mal certains champs que le client OpenAI .NET ajoute aux messages ; seuls partent <c>role</c>,
/// <c>content</c> en chaîne et les champs d'outils non vides. <see cref="ContenuVide"/> et <see cref="Diagnostic"/>
/// servent au journal d'une réponse vide.
/// </summary>
internal static class CorpsRequete
{
    /// <summary>Clés gardées dans un message ; toute autre (<c>name</c>, <c>refusal</c>…) est retirée.</summary>
    private static readonly FrozenSet<string> ClesPermises =
        new[] { "role", "content", "tool_calls", "tool_call_id" }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly JsonSerializerOptions Ecriture = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Corps nettoyé : dans chaque message objet de <c>messages</c>, clés hors <see cref="ClesPermises"/>, valeurs
    /// <c>null</c> et <c>tool_calls</c> vide retirés ; <c>content</c> fait de parties texte seulement → leur texte mis bout
    /// à bout ; <c>content</c> absent → chaîne vide. Le reste du corps est gardé. Corps illisible, racine non objet ou
    /// <c>messages</c> non tableau → les mêmes octets, intacts.
    /// </summary>
    public static byte[] Nettoyer(byte[] corps)
    {
        JsonNode? racine;
        try
        {
            racine = JsonNode.Parse(corps);
        }
        catch (JsonException)
        {
            return corps;
        }

        if (racine is not JsonObject objet || objet["messages"] is not JsonArray messages)
        {
            return corps;
        }

        var propres = new JsonArray();
        foreach (var noeud in messages)
        {
            propres.Add(noeud is JsonObject message ? Message(message) : noeud?.DeepClone());
        }

        objet["messages"] = propres;
        return Encoding.UTF8.GetBytes(objet.ToJsonString(Ecriture));
    }

    /// <summary>
    /// Réponse <c>chat.completion</c> dont le premier choix porte un message sans contenu (absent, <c>null</c> ou blanc).
    /// Corps illisible ou d'une autre forme → <c>false</c> : ce n'est pas le cas diagnostiqué.
    /// </summary>
    public static bool ContenuVide(byte[] reponse)
    {
        try
        {
            using var document = JsonDocument.Parse(reponse);
            var racine = document.RootElement;
            if (racine.ValueKind != JsonValueKind.Object
                || !racine.TryGetProperty("choices", out var choix)
                || choix.ValueKind != JsonValueKind.Array
                || choix.GetArrayLength() == 0
                || !choix[0].TryGetProperty("message", out var message))
            {
                return false;
            }

            if (!message.TryGetProperty("content", out var contenu) || contenu.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            return contenu.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(contenu.GetString());
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ligne du journal d'une réponse vide (DEB-Q114) : corps de la requête (la clé est dans un en-tête, jamais dans le
    /// corps) et corps de la réponse, en UTF-8.
    /// </summary>
    public static string Diagnostic(byte[] requete, byte[] reponse) =>
        $"Réponse vide de l'IA (DEB-A5). Requête envoyée ({requete.Length} octets) : {Encoding.UTF8.GetString(requete)} "
        + $"| Réponse reçue ({reponse.Length} octets) : {Encoding.UTF8.GetString(reponse)}";

    private static JsonObject Message(JsonObject message)
    {
        var propre = new JsonObject();
        foreach (var (cle, valeur) in message)
        {
            if (valeur is null || !ClesPermises.Contains(cle) || (cle == "tool_calls" && valeur is JsonArray { Count: 0 }))
            {
                continue;
            }

            propre[cle] = cle == "content" ? Contenu(valeur) : valeur.DeepClone();
        }

        if (!propre.ContainsKey("content"))
        {
            propre["content"] = "";
        }

        return propre;
    }

    /// <summary>Contenu en chaîne s'il n'est fait que de parties <c>{ "type": "text", "text": … }</c> ; sinon recopié.</summary>
    private static JsonNode Contenu(JsonNode valeur)
    {
        if (valeur is not JsonArray parties)
        {
            return valeur.DeepClone();
        }

        var texte = new StringBuilder();
        foreach (var partie in parties)
        {
            if (partie is not JsonObject objet
                || objet["type"] is not JsonValue type
                || !type.TryGetValue<string>(out var nom)
                || nom != "text"
                || objet["text"] is not JsonValue valeurTexte
                || !valeurTexte.TryGetValue<string>(out var morceau))
            {
                return valeur.DeepClone();
            }

            texte.Append(morceau);
        }

        return JsonValue.Create(texte.ToString());
    }
}
