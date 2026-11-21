using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExpressionBesoin.Web.Tests.Api;

/// <summary>Appels de l'API pour les tests d'intégration : statut et corps JSON relevés ensemble.</summary>
internal static class EssaisApi
{
    public static CancellationToken Annulation => TestContext.Current.CancellationToken;

    public const string Demande = "Contrôle des plates-formes e-commerce et prélèvements à distance";

    public static async Task<(HttpStatusCode Statut, JsonElement Corps)> Appeler(HttpClient client, HttpMethod methode, string url, object? corps = null, string? agent = null)
    {
        using var requete = new HttpRequestMessage(methode, url);
        if (corps is not null)
        {
            requete.Content = JsonContent.Create(corps);
        }

        if (agent is not null)
        {
            requete.Headers.Add("X-Agent", agent);
        }

        using var reponse = await client.SendAsync(requete, Annulation);
        var texte = await reponse.Content.ReadAsStringAsync(Annulation);
        using var document = JsonDocument.Parse(texte);
        return (reponse.StatusCode, document.RootElement.Clone());
    }

    /// <summary>Crée un brouillon pour Camille Martin, rend son identifiant.</summary>
    public static async Task<string> Creer(HttpClient client)
    {
        var (statut, fiche) = await Appeler(client, HttpMethod.Post, "/api/fiches", agent: "camille.martin");
        Assert.Equal(HttpStatusCode.Created, statut);
        return fiche.GetProperty("id").GetString()!;
    }

    public static JsonElement Rubrique(JsonElement fiche, string code) =>
        fiche.GetProperty("rubriques").EnumerateArray().Single(rubrique => rubrique.GetProperty("code").GetString() == code);

    public static void Refus(HttpStatusCode attendu, string code, (HttpStatusCode Statut, JsonElement Corps) reponse)
    {
        Assert.Equal(attendu, reponse.Statut);
        Assert.Equal(code, reponse.Corps.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(reponse.Corps.GetProperty("message").GetString()));
    }
}
