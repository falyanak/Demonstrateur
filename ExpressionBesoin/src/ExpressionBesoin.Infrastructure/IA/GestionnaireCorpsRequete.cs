using Microsoft.Extensions.Logging;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// Maillon HTTP de l'adaptateur IA (lot A5b, DEB-A5), coquille sans décision, placé AVANT le maillon du nouvel essai :
/// le corps de la requête part nettoyé par <see cref="CorpsRequete.Nettoyer"/> (DEB-Q113) ; une réponse réussie dont le
/// contenu est vide (<see cref="CorpsRequete.ContenuVide"/>) est journalisée avec la requête réellement envoyée
/// (<see cref="CorpsRequete.Diagnostic"/>, DEB-Q114). La réponse lue est mise en mémoire : le client la relit.
/// </summary>
public sealed class GestionnaireCorpsRequete(ILogger journal) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken annulation)
    {
        byte[]? corps = null;
        if (requete.Content is not null)
        {
            corps = CorpsRequete.Nettoyer(await requete.Content.ReadAsByteArrayAsync(annulation).ConfigureAwait(false));
            var type = requete.Content.Headers.ContentType;
            requete.Content = new ByteArrayContent(corps);
            requete.Content.Headers.ContentType = type;
        }

        var reponse = await base.SendAsync(requete, annulation).ConfigureAwait(false);
        if (corps is not null && reponse.IsSuccessStatusCode)
        {
            var octets = await reponse.Content.ReadAsByteArrayAsync(annulation).ConfigureAwait(false);
            if (CorpsRequete.ContenuVide(octets))
            {
                journal.LogWarning("{Diagnostic}", CorpsRequete.Diagnostic(corps, octets));
            }
        }

        return reponse;
    }
}
