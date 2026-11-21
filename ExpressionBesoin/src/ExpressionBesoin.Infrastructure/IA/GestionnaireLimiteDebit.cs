using System.Net;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Maillon HTTP de l'adaptateur IA (DEB-Q87), coquille sans décision : sur une réponse 429, la décision est
/// <see cref="LimiteDebit.NouvelEssai"/> ; au plus UN nouvel essai, sur une copie de la requête (contenu lu une fois,
/// en octets). Les autres réponses passent telles quelles ; le client OpenAI lève sur un statut d'échec, la route répond
/// alors 503 (DEB-Q22). <paramref name="attendre"/> est injecté : les tests n'attendent pas réellement.
/// </summary>
public sealed class GestionnaireLimiteDebit(Func<TimeSpan, CancellationToken, Task> attendre) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken annulation)
    {
        var octets = requete.Content is null ? null : await requete.Content.ReadAsByteArrayAsync(annulation).ConfigureAwait(false);
        var reponse = await base.SendAsync(Copie(requete, octets), annulation).ConfigureAwait(false);
        if (reponse.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return reponse;
        }

        // Corps mis en mémoire par la lecture : le client le relit si la réponse lui est rendue.
        var corps = await reponse.Content.ReadAsStringAsync(annulation).ConfigureAwait(false);
        if (LimiteDebit.NouvelEssai(corps, reponse.Headers.RetryAfter?.Delta) is not { } attente)
        {
            return reponse;
        }

        reponse.Dispose();
        await attendre(attente, annulation).ConfigureAwait(false);
        return await base.SendAsync(Copie(requete, octets), annulation).ConfigureAwait(false);
    }

    /// <summary>Requête neuve : méthode, adresse, version, en-têtes ; contenu recréé depuis ses octets, en-têtes compris.</summary>
    internal static HttpRequestMessage Copie(HttpRequestMessage source, byte[]? octets)
    {
        var copie = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy,
        };
        foreach (var (nom, valeurs) in source.Headers)
        {
            copie.Headers.TryAddWithoutValidation(nom, valeurs);
        }

        if (octets is not null)
        {
            var contenu = new ByteArrayContent(octets);
            foreach (var (nom, valeurs) in source.Content!.Headers)
            {
                contenu.Headers.TryAddWithoutValidation(nom, valeurs);
            }

            copie.Content = contenu;
        }

        return copie;
    }
}
