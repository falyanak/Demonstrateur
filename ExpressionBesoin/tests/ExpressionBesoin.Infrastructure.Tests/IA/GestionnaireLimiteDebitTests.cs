using System.Net;
using System.Text;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>Maillon HTTP du nouvel essai (DEB-Q87), éprouvé sur un faux fournisseur, attentes relevées sans attendre.</summary>
public sealed class GestionnaireLimiteDebitTests
{
    private readonly List<TimeSpan> _attentes = [];

    private HttpMessageInvoker Invocateur(FauxFournisseurIA faux) =>
        new(new GestionnaireLimiteDebit((attente, _) =>
        {
            _attentes.Add(attente);
            return Task.CompletedTask;
        })
        {
            InnerHandler = faux,
        });

    private static HttpRequestMessage Requete()
    {
        var requete = new HttpRequestMessage(HttpMethod.Post, "https://albert.test/v1/chat/completions")
        {
            Content = new StringContent("{\"model\":\"m\"}", Encoding.UTF8, "application/json"),
        };
        requete.Headers.Add(FauxFournisseurIA.EnTeteEssai, "1");
        return requete;
    }

    [Fact]
    public async Task Reponse_ordinaire_transmise_requete_copiee_a_l_identique()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Reponse(HttpStatusCode.OK, "{}"));
        using var invocateur = Invocateur(faux);

        using var reponse = await invocateur.SendAsync(Requete(), Essais.Annulation);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var recue = Assert.Single(faux.Requetes);
        Assert.Equal(
            new RequeteRecue(HttpMethod.Post, new Uri("https://albert.test/v1/chat/completions"), null, "1", "application/json; charset=utf-8", "{\"model\":\"m\"}"),
            recue);
        Assert.Empty(_attentes);
    }

    [Fact]
    public async Task Requete_sans_contenu()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Reponse(HttpStatusCode.OK, "{}"));
        using var invocateur = Invocateur(faux);

        using var reponse = await invocateur.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://albert.test/v1/models"), Essais.Annulation);

        Assert.Equal(new RequeteRecue(HttpMethod.Get, new Uri("https://albert.test/v1/models"), null, null, null, null), Assert.Single(faux.Requetes));
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(null, 2)]
    public async Task Limite_par_minute_un_nouvel_essai_apres_l_attente(int? reessayerApres, int attente)
    {
        var faux = new FauxFournisseurIA(
            () => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, FauxFournisseurIA.LimiteMinute, reessayerApres is { } s ? TimeSpan.FromSeconds(s) : null),
            () => FauxFournisseurIA.Reponse(HttpStatusCode.OK, "{\"ok\":true}"));
        using var invocateur = Invocateur(faux);

        using var reponse = await invocateur.SendAsync(Requete(), Essais.Annulation);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("{\"ok\":true}", await reponse.Content.ReadAsStringAsync(Essais.Annulation));
        Assert.Equal(new[] { TimeSpan.FromSeconds(attente) }, _attentes);
        Assert.Equal(2, faux.Requetes.Count);
        Assert.Equal(faux.Requetes[0], faux.Requetes[1]);
    }

    [Theory]
    [InlineData(FauxFournisseurIA.LimiteJour, 1)]
    [InlineData(FauxFournisseurIA.LimiteMinute, 11)]
    [InlineData("Too many requests", 1)]
    public async Task Sans_nouvel_essai_le_429_est_rendu_corps_lisible(string corps, int reessayerApres)
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, corps, TimeSpan.FromSeconds(reessayerApres)));
        using var invocateur = Invocateur(faux);

        using var reponse = await invocateur.SendAsync(Requete(), Essais.Annulation);

        Assert.Equal(HttpStatusCode.TooManyRequests, reponse.StatusCode);
        Assert.Equal(corps, await reponse.Content.ReadAsStringAsync(Essais.Annulation));
        Assert.Single(faux.Requetes);
        Assert.Empty(_attentes);
    }

    [Fact]
    public async Task Un_seul_nouvel_essai_meme_si_la_limite_persiste()
    {
        var faux = new FauxFournisseurIA(
            () => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, FauxFournisseurIA.LimiteMinute, TimeSpan.FromSeconds(1)),
            () => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, FauxFournisseurIA.LimiteMinute, TimeSpan.FromSeconds(1)));
        using var invocateur = Invocateur(faux);

        using var reponse = await invocateur.SendAsync(Requete(), Essais.Annulation);

        Assert.Equal(HttpStatusCode.TooManyRequests, reponse.StatusCode);
        Assert.Equal(2, faux.Requetes.Count);
        Assert.Equal(new[] { TimeSpan.FromSeconds(1) }, _attentes);
    }
}
