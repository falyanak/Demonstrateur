using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using ExpressionBesoin.Application.Dictee;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// Adaptateur de la dictée (lot S1) éprouvé SANS réseau : ce qui part (adresse, clé, formulaire au contrat de
/// <c>/audio/transcriptions</c>, octets du WAV intacts) et ce qui revient (texte brut, pannes). Whisper réel jugé par la
/// sonde du 22/11/2026 et au navigateur.
/// </summary>
public sealed class TranscripteurWhisperTests
{
    /// <summary>WAV canonique de deux échantillons (réplique Python, comme <c>DecisionsDicteeTests</c>).</summary>
    private static readonly byte[] Wav =
        Convert.FromHexString("524946462800000057415645666D74201000000001000100803E0000007D00000200100064617461040000000000FF7F");

    /// <summary>Fausse clé construite par concaténation : une archive ne la masque pas (règles §13).</summary>
    private static readonly OptionsIA Options = new(new Uri("https://albert.test/v1"), "gemma-4-31b-it", "cle-" + "essai");

    private readonly List<TimeSpan> _attentes = [];

    private static CancellationToken Annulation => TestContext.Current.CancellationToken;

    private TranscripteurWhisper Transcripteur(HttpMessageHandler transport) =>
        TranscripteurWhisper.Creer(Options, "whisper-large-v3", transport, (attente, _) =>
        {
            _attentes.Add(attente);
            return Task.CompletedTask;
        });

    /// <summary>Requête reçue en OCTETS (le WAV n'est pas du texte) et type de contenu.</summary>
    private sealed record Capturee(HttpMethod Methode, Uri Adresse, string? Autorisation, string? Type, byte[] Corps);

    private sealed class Capture(HttpResponseMessage reponse) : HttpMessageHandler
    {
        public List<Capturee> Requetes { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken annulation)
        {
            Requetes.Add(new Capturee(
                requete.Method,
                requete.RequestUri!,
                requete.Headers.Authorization?.ToString(),
                requete.Content?.Headers.ContentType?.ToString(),
                await requete.Content!.ReadAsByteArrayAsync(annulation)));
            return reponse;
        }
    }

    /// <summary>Parties du formulaire : en-têtes et corps (octets) de chacune, lus en Latin-1 pour garder chaque octet.</summary>
    private static List<(string EnTetes, byte[] Corps)> Parties(string type, byte[] corps)
    {
        var limite = "--" + MediaTypeHeaderValue.Parse(type).Parameters.Single(p => p.Name == "boundary").Value!.Trim('"');
        var texte = Encoding.Latin1.GetString(corps);
        var parties = new List<(string, byte[])>();
        foreach (var bloc in texte.Split(limite).Skip(1))
        {
            if (bloc.StartsWith("--", StringComparison.Ordinal))
            {
                break;
            }

            var separation = bloc.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            var contenu = bloc[(separation + 4)..^2];
            parties.Add((bloc[..separation], Encoding.Latin1.GetBytes(contenu)));
        }

        return parties;
    }

    /// <summary>Partie nommée <paramref name="nom"/>, nom entre guillemets ou non (les deux formes sont valides).</summary>
    private static (string EnTetes, byte[] Corps) Partie(List<(string EnTetes, byte[] Corps)> parties, string nom) =>
        parties.Single(p => Regex.IsMatch(p.EnTetes, $"name=\"?{nom}\"?(;|\r|$)", RegexOptions.Multiline));

    [Fact]
    public async Task Requete_au_contrat_octets_intacts_texte_rendu_tel_quel()
    {
        var capture = new Capture(FauxFournisseurIA.Reponse(HttpStatusCode.OK, "{\"text\":\" Le copieur est en panne.\"}"));
        using var transcripteur = Transcripteur(capture);

        Assert.Equal(" Le copieur est en panne.", await transcripteur.Transcrire(Wav, Annulation));

        var requete = Assert.Single(capture.Requetes);
        Assert.Equal(
            (HttpMethod.Post, new Uri("https://albert.test/v1/audio/transcriptions"), "Bearer cle-essai"),
            (requete.Methode, requete.Adresse, requete.Autorisation));
        Assert.StartsWith("multipart/form-data", requete.Type);
        var parties = Parties(requete.Type!, requete.Corps);
        Assert.Equal(4, parties.Count);
        var fichier = Partie(parties, "file");
        Assert.Contains("dictee.wav", fichier.EnTetes);
        Assert.Contains("Content-Type: audio/wav", fichier.EnTetes);
        Assert.Equal(Wav, fichier.Corps);
        Assert.Equal(
            new[] { "whisper-large-v3", "fr", "json" },
            new[] { "model", "language", "response_format" }.Select(nom => Encoding.UTF8.GetString(Partie(parties, nom).Corps)).ToArray());
        Assert.Empty(_attentes);
    }

    [Theory]
    [InlineData("https://albert.test/v1")]
    [InlineData("https://albert.test/v1/")]
    public void Adresse_sous_le_point_du_profil(string point) =>
        Assert.Equal(new Uri("https://albert.test/v1/audio/transcriptions"), TranscripteurWhisper.Adresse(new Uri(point)));

    [Fact]
    public async Task Limite_par_minute_un_nouvel_essai_meme_requete()
    {
        var faux = new FauxFournisseurIA(
            () => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, FauxFournisseurIA.LimiteMinute, TimeSpan.FromSeconds(3)),
            () => FauxFournisseurIA.Reponse(HttpStatusCode.OK, "{\"text\":\"Bonjour\"}"));
        using var transcripteur = Transcripteur(faux);

        Assert.Equal("Bonjour", await transcripteur.Transcrire(Wav, Annulation));

        Assert.Equal(new[] { TimeSpan.FromSeconds(3) }, _attentes);
        Assert.Equal(2, faux.Requetes.Count);
        Assert.Equal(faux.Requetes[0], faux.Requetes[1]);
    }

    [Fact]
    public async Task Erreur_du_serveur_panne_avec_statut_et_extrait()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Reponse(HttpStatusCode.InternalServerError, "{\"detail\":\"erreur\"}"));
        using var transcripteur = Transcripteur(faux);

        var panne = await Assert.ThrowsAsync<HttpRequestException>(async () => await transcripteur.Transcrire(Wav, Annulation));

        Assert.Equal("Transcription : HTTP 500 : {\"detail\":\"erreur\"}", panne.Message);
        Assert.Equal(HttpStatusCode.InternalServerError, panne.StatusCode);
        Assert.DoesNotContain("cle-essai", panne.Message);
    }

    [Fact]
    public async Task Reponse_sans_texte_panne_jamais_un_texte_devine()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Reponse(HttpStatusCode.OK, "{\"detail\":\"modele inconnu\"}"));
        using var transcripteur = Transcripteur(faux);

        var panne = await Assert.ThrowsAsync<InvalidDataException>(async () => await transcripteur.Transcrire(Wav, Annulation));

        Assert.Equal("Transcription : réponse sans champ « text » : {\"detail\":\"modele inconnu\"}", panne.Message);
    }

    [Fact]
    public async Task Attente_par_defaut_reelle_et_breve()
    {
        var faux = new FauxFournisseurIA(
            () => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, FauxFournisseurIA.LimiteMinute, TimeSpan.Zero),
            () => FauxFournisseurIA.Reponse(HttpStatusCode.OK, "{\"text\":\"Bonjour\"}"));
        using var transcripteur = TranscripteurWhisper.Creer(Options, "whisper-large-v3", faux);

        Assert.Equal("Bonjour", await transcripteur.Transcrire(Wav, Annulation));
        Assert.Equal(2, faux.Requetes.Count);
    }

    [Fact]
    public void Transport_par_defaut_sans_appel_reseau_a_la_creation()
    {
        using var transcripteur = TranscripteurWhisper.Creer(Options, "whisper-large-v3");
        Assert.IsAssignableFrom<ITranscripteur>(transcripteur);
        Assert.Equal(new[] { "fr", "dictee.wav" }, new[] { TranscripteurWhisper.Langue, TranscripteurWhisper.NomFichier });
    }
}
