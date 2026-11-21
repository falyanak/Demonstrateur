using System.Net;
using System.Text.Json;

namespace ExpressionBesoin.Web.Tests;

public sealed class SanteContratTests(UsineWeb usine) : IClassFixture<UsineWeb>
{
    [Fact]
    public async Task Sante_renvoie_exactement_les_quatre_champs_du_contrat()
    {
        var annulation = TestContext.Current.CancellationToken;
        using var client = usine.CreateClient();
        using var reponse = await client.GetAsync("/api/sante", annulation);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/json", reponse.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(annulation));
        var racine = document.RootElement;
        var noms = racine.EnumerateObject().Select(propriete => propriete.Name).ToArray();
        Assert.Equal(new[] { "etat", "version", "demarrage", "dureeSecondes" }, noms);

        Assert.Equal("ok", racine.GetProperty("etat").GetString());
        Assert.False(string.IsNullOrWhiteSpace(racine.GetProperty("version").GetString()));
        Assert.Equal(JsonValueKind.String, racine.GetProperty("demarrage").ValueKind);
        Assert.True(racine.GetProperty("demarrage").TryGetDateTimeOffset(out _));
        Assert.True(racine.GetProperty("dureeSecondes").TryGetInt64(out var duree));
        Assert.True(duree >= 0);
    }

    [Fact]
    public async Task Version_publiee_commence_par_la_version_du_projet()
    {
        var annulation = TestContext.Current.CancellationToken;
        using var client = usine.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/sante", annulation));
        Assert.StartsWith("0.1.0", document.RootElement.GetProperty("version").GetString());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Error")]
    public async Task Pages_du_socle_repondent(string chemin)
    {
        var annulation = TestContext.Current.CancellationToken;
        using var client = usine.CreateClient();
        using var reponse = await client.GetAsync(chemin, annulation);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Contains("Expression de besoin", await reponse.Content.ReadAsStringAsync(annulation));
    }
}
