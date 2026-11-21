using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using ExpressionBesoin.Application.Fiches;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Adaptateur d'<see cref="IAssistantIA"/> vers l'IA réelle (lot A1, DEB-D5, DEB-Q83 à DEB-Q88) : un agent Microsoft
/// Agent Framework (<see cref="ChatClientAgent"/>, sans outils ni session) sur l'<see cref="IChatClient"/> OpenAI de
/// Microsoft.Extensions.AI, pointé sur Albert API. Coquille sans décision : le prompt est <see cref="PromptFiche"/>, le
/// nouvel essai <see cref="GestionnaireLimiteDebit"/> ; le texte rendu est relu par <c>ReponseIA.Lire</c>. Une panne
/// (réseau, délai, statut d'échec) lève une exception : 503 (DEB-Q22).
/// </summary>
public sealed class AssistantIAAlbert : IAssistantIA, IDisposable
{
    public const string NomAgent = "AssistantFicheBesoin";

    /// <summary>Température de la sonde du 21/11/2026 (DEB-Q84) : réponses stables, peu d'écart aux mots de l'agent.</summary>
    public const float Temperature = 0.1f;

    /// <summary>Clé des options lue par Microsoft.Extensions.AI.OpenAI : schéma de réponse strict (DEB-Q85).</summary>
    public const string CleStrict = "strict";

    private readonly HttpClient _http;

    private readonly ChatClientAgent _agent;

    private AssistantIAAlbert(HttpClient http, ChatClientAgent agent)
    {
        _http = http;
        _agent = agent;
    }

    /// <summary>
    /// Assemblage. <paramref name="transport"/> remplace le réseau (tests) ; à défaut, connexions directes ou par le proxy
    /// du système, auquel seul sont remis les identifiants Windows (proxy d'entreprise, HTTP 407 ; comme CSAgent).
    /// <paramref name="attendre"/> remplace l'attente du nouvel essai (tests). Les nouvels essais du client OpenAI sont
    /// désactivés : un seul, décidé par <see cref="LimiteDebit"/>.
    /// </summary>
    public static AssistantIAAlbert Creer(
        OptionsIA options,
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
        var client = new OpenAIClient(
            new ApiKeyCredential(options.Cle),
            new OpenAIClientOptions
            {
                Endpoint = options.Point,
                Transport = new HttpClientPipelineTransport(http),
                RetryPolicy = new ClientRetryPolicy(0),
                NetworkTimeout = options.Delai,
            });
        var agent = new ChatClientAgent(client.GetChatClient(options.Modele).AsIChatClient(), PromptFiche.Consignes, NomAgent);
        return new AssistantIAAlbert(http, agent);
    }

    public async ValueTask<string> Repondre(ContexteTour contexte, CancellationToken annulation)
    {
        List<ChatMessage> messages = [.. PromptFiche.Messages(contexte).Select(Convertir)];
        var reponse = await _agent.RunAsync(messages, null, new ChatClientAgentRunOptions(OptionsTour()), annulation)
            .ConfigureAwait(false);
        return reponse.Text;
    }

    /// <summary>Options d'un tour, neuves à chaque appel : température, réponse contrainte par le schéma, strict.</summary>
    internal static ChatOptions OptionsTour() =>
        new()
        {
            Temperature = AssistantIAAlbert.Temperature,
            ResponseFormat = ChatResponseFormat.ForJsonSchema(PromptFiche.Schema, PromptFiche.NomSchema),
            AdditionalProperties = new() { [CleStrict] = true },
        };

    public void Dispose() => _http.Dispose();

    private static ChatMessage Convertir(MessageIA message) =>
        new(message.Role == RoleMessage.Agent ? ChatRole.User : ChatRole.Assistant, message.Texte);

    private static Task Attendre(TimeSpan attente, CancellationToken annulation) => Task.Delay(attente, annulation);
}
