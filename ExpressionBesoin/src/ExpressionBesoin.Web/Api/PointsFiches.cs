using ExpressionBesoin.Application.Dictee;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ExpressionBesoin.Web.Api;

/// <summary>Corps de <c>POST api/fiches/{id}/messages</c>.</summary>
public sealed record MessageRequete(string? Message);

/// <summary>Corps de <c>PUT api/fiches/{id}/champs/{rubrique}</c> (valeur vide → rubrique vidée, DEB-Q15).</summary>
public sealed record ChampRequete(string? Valeur);

/// <summary>Corps de <c>POST api/fiches/{id}/soumettre</c> (DEB-Q5).</summary>
public sealed record SoumissionRequete(bool Confirme);

/// <summary>Corps de <c>POST api/fiches/{id}/decision</c> (DEB-Q3).</summary>
public sealed record DecisionRequete(string? Decision, string? Commentaire);

/// <summary>
/// Routes des fiches (plan §5) : adaptateur mince, un appel de <see cref="ServiceFiches"/> par route, puis projection
/// (projections de <c>Vues.cs</c>) ; refus → <see cref="StatutHttp.De"/> et corps <c>{ code, message }</c> ; panne d'un port
/// (exception) → 503 <c>panne.technique</c> (DEB-Q22). L'agent courant arrive par l'en-tête <c>X-Agent</c>, jamais pris
/// pour une autorisation.
/// </summary>
public static class PointsFiches
{
    public const string EnTeteAgent = "X-Agent";

    public const string TypeDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public static IEndpointRouteBuilder MapFiches(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api").AddEndpointFilter(FiltrerPannes);

        api.MapGet("/agents", async (ServiceFiches service, CancellationToken annulation) =>
            Results.Json((await service.ListerAgents(annulation)).Select(AgentVue.De), ContratJson.Options));

        // Dictée (lot S1) : corps = WAV canonique (audio/wav), lu jusqu'à un octet au-delà du maximum pour que la décision
        // pure reconnaisse un envoi trop long ; rien n'est stocké.
        api.MapPost("/dictee", async (HttpRequest requete, ServiceDictee service, CancellationToken annulation) =>
            Repondre(
                await service.Transcrire(
                    await LireCorps(requete.Body, requete.ContentLength, DecisionsDictee.OctetsMax + 1, annulation),
                    annulation),
                DicteeVue.De));

        var fiches = api.MapGroup("/fiches");
        fiches.MapGet("", async (ServiceFiches service, CancellationToken annulation) =>
            Results.Json((await service.Lister(annulation)).Select(ResumeVue.De), ContratJson.Options));
        fiches.MapPost("", async ([FromHeader(Name = EnTeteAgent)] string? agent, ServiceFiches service, CancellationToken annulation) =>
            Repondre(await service.CreerBrouillon(agent ?? "", annulation), FicheVue.De, StatusCodes.Status201Created));
        fiches.MapGet("/{id:guid}", async (Guid id, ServiceFiches service, CancellationToken annulation) =>
            Repondre(await service.Consulter(id, annulation), ConsultationVue.De));
        fiches.MapPost("/{id:guid}/messages", async (Guid id, MessageRequete requete, ServiceFiches service, CancellationToken annulation) =>
            Repondre(await service.EnvoyerMessage(id, requete.Message, annulation), TourVue.De));
        fiches.MapPut("/{id:guid}/champs/{rubrique}", async (Guid id, string rubrique, ChampRequete requete, ServiceFiches service, CancellationToken annulation) =>
            Repondre(await service.ModifierChamp(id, rubrique, requete.Valeur, annulation), FicheVue.De));
        fiches.MapPost("/{id:guid}/propositions/{rubrique}/accepter", async (Guid id, string rubrique, ServiceFiches service, CancellationToken annulation) =>
            Repondre(await service.AccepterProposition(id, rubrique, annulation), FicheVue.De));
        fiches.MapPost("/{id:guid}/propositions/{rubrique}/rejeter", async (Guid id, string rubrique, ServiceFiches service, CancellationToken annulation) =>
            Repondre(await service.RejeterProposition(id, rubrique, annulation), FicheVue.De));
        fiches.MapPost("/{id:guid}/soumettre", async (Guid id, SoumissionRequete requete, ServiceFiches service, CancellationToken annulation) =>
            Repondre(await service.Soumettre(id, requete.Confirme, annulation), FicheVue.De));
        fiches.MapPost("/{id:guid}/decision", (Guid id, DecisionRequete requete, ServiceFiches service, CancellationToken annulation) =>
            StatutHttp.LireDecision(requete.Decision).Match(
                async decision => Repondre(await service.Decider(id, decision, requete.Commentaire, annulation), FicheVue.De),
                refus => Task.FromResult(Erreur(refus))));
        fiches.MapPost("/{id:guid}/reprendre", async (Guid id, ServiceFiches service, CancellationToken annulation) =>
            Repondre(await service.Reprendre(id, annulation), FicheVue.De));
        fiches.MapGet("/{id:guid}/document", async (Guid id, ServiceFiches service, CancellationToken annulation) =>
            (await service.Telecharger(id, annulation)).Match(
                document => Results.File(document.Contenu, TypeDocx, document.NomFichier),
                Erreur));
        return routes;
    }

    /// <summary>Succès → projection en JSON ; refus → statut de <see cref="StatutHttp.De"/>.</summary>
    internal static IResult Repondre<T, TVue>(Resultat<T> resultat, Func<T, TVue> projeter, int statut = StatusCodes.Status200OK) =>
        resultat.Match(valeur => Results.Json(projeter(valeur), ContratJson.Options, statusCode: statut), Erreur);

    /// <summary>
    /// Corps d'une requête, <paramref name="plafond"/> octets au plus : tampon à la taille annoncée quand elle est connue
    /// et inférieure au plafond (une seule allocation), sinon au plafond ; la suite éventuelle n'est pas lue.
    /// </summary>
    internal static async Task<byte[]> LireCorps(Stream corps, long? longueur, int plafond, CancellationToken annulation)
    {
        var tampon = new byte[longueur is { } annoncee && annoncee < plafond ? (int)annoncee : plafond];
        var lus = 0;
        while (lus < tampon.Length)
        {
            var nombre = await corps.ReadAsync(tampon.AsMemory(lus), annulation);
            if (nombre == 0)
            {
                break;
            }

            lus += nombre;
        }

        return lus == tampon.Length ? tampon : tampon[..lus];
    }

    internal static IResult Erreur(Refus refus) =>
        Results.Json(new ErreurVue(refus.Code, refus.Message), ContratJson.Options, statusCode: StatutHttp.De(refus.Code));

    /// <summary>
    /// Panne d'un port (base, IA, document) : 503 et corps <c>{ code, message }</c>, cause journalisée. Sans filtre
    /// d'exception : une requête annulée par le client n'a plus de lecteur, sa réponse est sans effet.
    /// </summary>
    private static async ValueTask<object?> FiltrerPannes(EndpointFilterInvocationContext contexte, EndpointFilterDelegate suivant)
    {
        try
        {
            return await suivant(contexte);
        }
        catch (Exception exception)
        {
            contexte.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(PointsFiches))
                .LogError(exception, "Panne pendant {Methode} {Chemin}.", contexte.HttpContext.Request.Method, contexte.HttpContext.Request.Path);
            return Results.Json(
                new ErreurVue(StatutHttp.Panne, "Service momentanément indisponible ; réessayer."),
                ContratJson.Options,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
