using System.Net;
using System.Text;
using System.Text.Json;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>Requête reçue par le faux fournisseur : ce que le réseau aurait vu.</summary>
internal sealed record RequeteRecue(HttpMethod Methode, Uri Adresse, string? Autorisation, string? Essai, string? TypeContenu, string? Corps);

/// <summary>
/// Faux fournisseur d'IA compatible OpenAI, sans réseau (DEB-Q88) : rend les réponses dans l'ordre et garde chaque requête
/// reçue. Une requête de trop fait échouer le test.
/// </summary>
internal sealed class FauxFournisseurIA(params Func<HttpResponseMessage>[] reponses) : HttpMessageHandler
{
    public const string EnTeteEssai = "X-Essai";

    private int _rang;

    public List<RequeteRecue> Requetes { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken annulation)
    {
        var corps = requete.Content is null ? null : await requete.Content.ReadAsStringAsync(annulation);
        Requetes.Add(new RequeteRecue(
            requete.Method,
            requete.RequestUri!,
            requete.Headers.Authorization?.ToString(),
            requete.Headers.TryGetValues(EnTeteEssai, out var valeurs) ? string.Join(",", valeurs) : null,
            requete.Content?.Headers.ContentType?.ToString(),
            corps));
        Assert.True(_rang < reponses.Length, $"Requête inattendue n° {_rang + 1}.");
        return reponses[_rang++]();
    }

    public static HttpResponseMessage Reponse(HttpStatusCode statut, string corps, TimeSpan? reessayerApres = null)
    {
        var reponse = new HttpResponseMessage(statut) { Content = new StringContent(corps, Encoding.UTF8, "application/json") };
        if (reessayerApres is { } attente)
        {
            reponse.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(attente);
        }

        return reponse;
    }

    /// <summary>
    /// Réponse <c>chat.completion</c> d'un serveur compatible OpenAI dont le message porte <paramref name="contenu"/>
    /// (<c>null</c> : <c>"content": null</c>, réponse vide d'Albert, DEB-A5).
    /// </summary>
    public static HttpResponseMessage Completion(string? contenu) =>
        Reponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            id = "chatcmpl-essai",
            @object = "chat.completion",
            created = 1763715600,
            model = "openweight-medium",
            choices = new[] { new { index = 0, message = new { role = "assistant", content = contenu }, finish_reason = "stop" } },
            usage = new { prompt_tokens = 120, completion_tokens = 30, total_tokens = 150 },
        }));

    /// <summary>Corps 429 d'Albert, limite par minute (forme relevée dans CSAgent).</summary>
    public const string LimiteMinute = "{\"detail\":\"100 requests per minute exceeded (remaining: 0).\"}";

    /// <summary>Corps 429 d'Albert, limite par jour.</summary>
    public const string LimiteJour = "{\"detail\":\"1280000 input tokens per day exceeded (remaining: 0).\"}";
}
