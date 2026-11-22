using System.Net;
using System.Net.Http.Headers;
using ExpressionBesoin.Application.Dictee;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Adaptateur d'<see cref="ITranscripteur"/> vers <c>POST {Point}/audio/transcriptions</c> d'une API compatible OpenAI
/// (lot S1, DEB-Q131, DEB-Q132) : Whisper d'Albert API, modèle <c>whisper-large-v3</c> (sonde du 22/11/2026). Même point,
/// même clé et même délai que le profil courant de l'IA ; formulaire <c>file</c> (WAV), <c>model</c>, <c>language</c>
/// (<c>fr</c>), <c>response_format</c> (<c>json</c>), comme CSAgent (<c>AudioTools.TranscribePartAsync</c>). Coquille
/// sans décision : nouvel essai sur 429 par <see cref="GestionnaireLimiteDebit"/> (DEB-Q87), lecture par
/// <see cref="ReponseTranscription"/> ; statut d'échec ou réponse illisible → exception (503). Rien n'est écrit sur disque.
/// </summary>
public sealed class TranscripteurWhisper : ITranscripteur, IDisposable
{
    public const string Langue = "fr";

    public const string NomFichier = "dictee.wav";

    private readonly HttpClient _http;

    private readonly Uri _adresse;

    private readonly string _modele;

    private TranscripteurWhisper(HttpClient http, Uri adresse, string modele)
    {
        _http = http;
        _adresse = adresse;
        _modele = modele;
    }

    /// <summary>
    /// Assemblage, comme <see cref="AssistantIAAlbert.Creer"/> : <paramref name="transport"/> remplace le réseau (tests) ;
    /// à défaut, connexions directes ou par le proxy du système, identifiants Windows remis au seul proxy ;
    /// <paramref name="attendre"/> remplace l'attente du nouvel essai (tests).
    /// </summary>
    public static TranscripteurWhisper Creer(
        OptionsIA options,
        string modele,
        HttpMessageHandler? transport = null,
        Func<TimeSpan, CancellationToken, Task>? attendre = null)
    {
        var maillon = new GestionnaireLimiteDebit(attendre ?? Attendre)
        {
            InnerHandler = transport ?? new SocketsHttpHandler
            {
                DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            },
        };
        var http = new HttpClient(maillon) { Timeout = options.Delai };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Cle);
        return new TranscripteurWhisper(http, Adresse(options.Point), modele);
    }

    /// <summary>Adresse de la transcription sous le point du profil, barre finale ou non.</summary>
    internal static Uri Adresse(Uri point) => new(point.AbsoluteUri.TrimEnd('/') + "/audio/transcriptions");

    public async ValueTask<string> Transcrire(ReadOnlyMemory<byte> wav, CancellationToken annulation)
    {
        var fichier = new ReadOnlyMemoryContent(wav);
        fichier.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        using var formulaire = new MultipartFormDataContent();
        formulaire.Add(fichier, "file", NomFichier);
        formulaire.Add(new StringContent(_modele), "model");
        formulaire.Add(new StringContent(Langue), "language");
        formulaire.Add(new StringContent("json"), "response_format");

        using var reponse = await _http.PostAsync(_adresse, formulaire, annulation).ConfigureAwait(false);
        var corps = await reponse.Content.ReadAsStringAsync(annulation).ConfigureAwait(false);
        if (!reponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Transcription : HTTP {(int)reponse.StatusCode} : {ReponseTranscription.Extrait(corps)}",
                null,
                reponse.StatusCode);
        }

        return ReponseTranscription.Lire(corps)
            ?? throw new InvalidDataException($"Transcription : réponse sans champ « text » : {ReponseTranscription.Extrait(corps)}");
    }

    public void Dispose() => _http.Dispose();

    private static Task Attendre(TimeSpan attente, CancellationToken annulation) => Task.Delay(attente, annulation);
}
