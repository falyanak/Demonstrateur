using System.Text.Encodings.Web;
using System.Text.Json;

namespace ExpressionBesoin.Web.Api;

/// <summary>
/// Options JSON de l'API (DEB-Q43), appliquées par <c>ConfigureHttpJsonOptions</c> et relues par les tests du contrat :
/// camelCase, compact. Encodeur RELÂCHÉ : les textes français et <c>+00:00</c> des instants restent lisibles ; sans
/// risque ici, les réponses sont <c>application/json</c>, jamais insérées dans du HTML, et le front n'écrit que par
/// <c>textContent</c>.
/// </summary>
public static class ContratJson
{
    public static JsonSerializerOptions Options { get; } = Configurer(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Configurer(JsonSerializerOptions options)
    {
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.WriteIndented = false;
        return options;
    }
}
