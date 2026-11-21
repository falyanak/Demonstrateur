using System.Net;
using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Web.Tests.Api.EssaisApi;

namespace ExpressionBesoin.Web.Tests.Api;

/// <summary>
/// Parcours du critère de fin (plan §1) par l'API, sur une base neuve (usine propre à cette classe, numéros prévisibles) :
/// saisie, soumission, retour pour complément, reprise, nouvelle soumission (numéro gardé), validation, .docx, liste,
/// cycle de vie (DEB-Q77 : critère de fin du lot L1).
/// </summary>
public sealed class ParcoursFicheTests(UsineWeb usine) : IClassFixture<UsineWeb>
{
    [Fact]
    public async Task Brouillon_soumis_complete_resoumis_valide_puis_telecharge()
    {
        using var client = usine.CreateClient();
        var id = await Creer(client);
        var url = $"/api/fiches/{id}";
        foreach (var definition in Catalogue.Toutes.Where(definition => definition.Obligatoire))
        {
            var (saisie, _) = await Appeler(client, HttpMethod.Put, $"{url}/champs/{definition.Code}", new { valeur = "Valeur de " + definition.Code });
            Assert.Equal(HttpStatusCode.OK, saisie);
        }

        Refus(HttpStatusCode.UnprocessableEntity, "soumission.confirmationAbsente", await Appeler(client, HttpMethod.Post, $"{url}/soumettre", new { confirme = false }));
        var (statut, fiche) = await Appeler(client, HttpMethod.Post, $"{url}/soumettre", new { confirme = true });
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal(("Soumise", "EB-2026-0001", false), (fiche.GetProperty("statut").GetString(), fiche.GetProperty("numero").GetString(), fiche.GetProperty("modifiable").GetBoolean()));
        Refus(HttpStatusCode.UnprocessableEntity, "fiche.nonModifiable", await Appeler(client, HttpMethod.Put, $"{url}/champs/intitule", new { valeur = "x" }));

        Refus(HttpStatusCode.UnprocessableEntity, "decision.commentaireObligatoire", await Appeler(client, HttpMethod.Post, $"{url}/decision", new { decision = "DemanderComplements" }));
        (statut, fiche) = await Appeler(client, HttpMethod.Post, $"{url}/decision", new { decision = "DemanderComplements", commentaire = "Préciser les critères." });
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal("ACompleter", fiche.GetProperty("statut").GetString());
        var decision = fiche.GetProperty("decisions")[0];
        Assert.Equal(("DemanderComplements", "Dominique Bernard", "Préciser les critères."), (decision.GetProperty("type").GetString(), decision.GetProperty("auteur").GetString(), decision.GetProperty("commentaire").GetString()));

        (statut, fiche) = await Appeler(client, HttpMethod.Post, $"{url}/reprendre");
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal(("Brouillon", System.Text.Json.JsonValueKind.Null), (fiche.GetProperty("statut").GetString(), fiche.GetProperty("confirmeeLe").ValueKind));
        await Appeler(client, HttpMethod.Put, $"{url}/champs/criteresReussite", new { valeur = "Délai de contrôle divisé par deux" });
        (statut, fiche) = await Appeler(client, HttpMethod.Post, $"{url}/soumettre", new { confirme = true });
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal("EB-2026-0001", fiche.GetProperty("numero").GetString());

        (statut, fiche) = await Appeler(client, HttpMethod.Post, $"{url}/decision", new { decision = "valider" });
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal("Validee", fiche.GetProperty("statut").GetString());

        using var document = await client.GetAsync($"{url}/document", Annulation);
        Assert.Equal(HttpStatusCode.OK, document.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", document.Content.Headers.ContentType?.MediaType);
        Assert.Equal("EB-2026-0001.docx", document.Content.Headers.ContentDisposition?.FileName);
        var octets = await document.Content.ReadAsByteArrayAsync(Annulation);
        Assert.Equal("PK"u8.ToArray(), octets[..2]);

        var (liste, resumes) = await Appeler(client, HttpMethod.Get, "/api/fiches");
        Assert.Equal(HttpStatusCode.OK, liste);
        var resume = Assert.Single(resumes.EnumerateArray());
        Assert.Equal(("EB-2026-0001", "Validee", "Valeur de intitule", "Camille Martin"), (resume.GetProperty("numero").GetString(), resume.GetProperty("statut").GetString(), resume.GetProperty("intitule").GetString(), resume.GetProperty("demandeur").GetString()));

        var (consultee, consultation) = await Appeler(client, HttpMethod.Get, url);
        Assert.Equal(HttpStatusCode.OK, consultee);
        var cycle = consultation.GetProperty("cycle").EnumerateArray().ToArray();
        Assert.Equal(
            new[] { "Creation", "Soumission", "DemandeComplements", "Reprise", "Soumission", "Validation" },
            cycle.Select(transition => transition.GetProperty("etape").GetString()).ToArray());
        Assert.Equal(
            new[] { "Camille Martin", "Camille Martin", "Dominique Bernard", "Camille Martin", "Camille Martin", "Dominique Bernard" },
            cycle.Select(transition => transition.GetProperty("acteur").GetString()).ToArray());
        Assert.Equal(
            new[] { null, "Brouillon", "Soumise", "ACompleter", "Brouillon", "Soumise" },
            cycle.Select(transition => transition.GetProperty("avant").GetString()).ToArray());
        Assert.All(cycle, transition => Assert.Equal(UsineWeb.Maintenant, transition.GetProperty("le").GetDateTimeOffset()));
    }
}

/// <summary>Panne d'un port : 503 <c>panne.technique</c>, et rien n'est écrit (DEB-Q22).</summary>
public sealed class PanneApiTests(UsinePanne usine) : IClassFixture<UsinePanne>
{
    [Fact]
    public async Task IA_en_panne_503_fiche_et_historique_inchanges()
    {
        using var client = usine.CreateClient();
        var id = await Creer(client);
        Refus(HttpStatusCode.ServiceUnavailable, "panne.technique", await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/messages", new { message = Demande }));
        var (_, consultation) = await Appeler(client, HttpMethod.Get, $"/api/fiches/{id}");
        Assert.Equal(0, consultation.GetProperty("historique").GetArrayLength());
        Assert.Equal("Creation", Assert.Single(consultation.GetProperty("cycle").EnumerateArray()).GetProperty("etape").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, EssaisApi.Rubrique(consultation.GetProperty("fiche"), "intitule").GetProperty("proposition").ValueKind);
    }
}
