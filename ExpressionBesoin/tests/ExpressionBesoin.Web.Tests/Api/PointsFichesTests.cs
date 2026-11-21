using System.Net;
using ExpressionBesoin.Infrastructure.IA;
using static ExpressionBesoin.Web.Tests.Api.EssaisApi;

namespace ExpressionBesoin.Web.Tests.Api;

public sealed class PointsFichesTests(UsineWeb usine) : IClassFixture<UsineWeb>
{
    [Fact]
    public async Task Agents_du_referentiel_fictif_textes_francais_non_echappes()
    {
        using var client = usine.CreateClient();
        using var reponse = await client.GetAsync("/api/agents", Annulation);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/json", reponse.Content.Headers.ContentType?.MediaType);
        var texte = await reponse.Content.ReadAsStringAsync(Annulation);
        Assert.StartsWith(
            """[{"identifiant":"camille.martin","nom":"Camille Martin","bureau":"Bureau 1A – Commerce électronique","valideur":"Dominique Bernard"},""",
            texte);
        Assert.Equal(4, System.Text.Json.JsonDocument.Parse(texte).RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Creation_brouillon_en_tete_du_referentiel()
    {
        using var client = usine.CreateClient();
        var (statut, fiche) = await Appeler(client, HttpMethod.Post, "/api/fiches", agent: "sarah.durand");
        Assert.Equal(HttpStatusCode.Created, statut);
        Assert.Equal("Brouillon", fiche.GetProperty("statut").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, fiche.GetProperty("numero").ValueKind);
        Assert.Equal("Sarah Durand", fiche.GetProperty("enTete").GetProperty("demandeur").GetString());
        Assert.Equal("Claude Moreau", fiche.GetProperty("enTete").GetProperty("valideur").GetString());
        Assert.Equal(UsineWeb.Maintenant, fiche.GetProperty("enTete").GetProperty("creeeLe").GetDateTimeOffset());
        Assert.Equal(17, fiche.GetProperty("rubriques").GetArrayLength());
        Assert.Equal((0, true, "application"), (fiche.GetProperty("remplies").GetInt32(), fiche.GetProperty("modifiable").GetBoolean(), fiche.GetProperty("prochaineRubrique").GetString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("inconnu")]
    [InlineData("Camille.Martin")]
    public async Task Creation_agent_absent_ou_inconnu_422(string? agent)
    {
        using var client = usine.CreateClient();
        Refus(HttpStatusCode.UnprocessableEntity, "agent.inconnu", await Appeler(client, HttpMethod.Post, "/api/fiches", agent: agent));
    }

    [Fact]
    public async Task Fiche_inconnue_404_sur_chaque_route()
    {
        using var client = usine.CreateClient();
        const string id = "/api/fiches/0193a1b2-0000-7000-8000-000000000000";
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Get, id));
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Post, id + "/messages", new { message = "x" }));
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Put, id + "/champs/intitule", new { valeur = "x" }));
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Post, id + "/propositions/intitule/accepter"));
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Post, id + "/propositions/intitule/rejeter"));
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Post, id + "/soumettre", new { confirme = true }));
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Post, id + "/decision", new { decision = "Valider" }));
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Post, id + "/reprendre"));
        Refus(HttpStatusCode.NotFound, "fiche.introuvable", await Appeler(client, HttpMethod.Get, id + "/document"));
    }

    [Fact]
    public async Task Premier_tour_scenarise_intitule_propose_question_sur_l_application_historique_ecrit()
    {
        using var client = usine.CreateClient();
        var id = await Creer(client);
        var (statut, tour) = await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/messages", new { message = "  " + Demande + " " });
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal("Quelle application ou quel service du système d'information est concerné ?", tour.GetProperty("message").GetString());
        Assert.False(tour.GetProperty("horsPerimetre").GetBoolean());
        Assert.Equal(0, tour.GetProperty("ecartees").GetArrayLength());
        var intitule = Rubrique(tour.GetProperty("fiche"), "intitule");
        Assert.Equal(Demande, intitule.GetProperty("proposition").GetProperty("valeur").GetString());
        Assert.Equal(ScenarioDialogue.JustificationReprise, intitule.GetProperty("proposition").GetProperty("justification").GetString());
        Assert.Equal("Vide", intitule.GetProperty("origine").GetString());

        var (_, consultation) = await Appeler(client, HttpMethod.Get, $"/api/fiches/{id}");
        var historique = consultation.GetProperty("historique").EnumerateArray().ToArray();
        Assert.Equal(new[] { "Agent", "IA" }, historique.Select(echange => echange.GetProperty("auteur").GetString()).ToArray());
        Assert.Equal(Demande, historique[0].GetProperty("texte").GetString());
        Assert.Equal(UsineWeb.Maintenant, historique[1].GetProperty("le").GetDateTimeOffset());
    }

    [Fact]
    public async Task Message_vide_422_hors_perimetre_sans_proposition()
    {
        using var client = usine.CreateClient();
        var id = await Creer(client);
        Refus(HttpStatusCode.UnprocessableEntity, "message.vide", await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/messages", new { message = " " }));
        Refus(HttpStatusCode.UnprocessableEntity, "message.vide", await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/messages", new { }));

        var (statut, tour) = await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/messages", new { message = "Mon imprimante est en panne" });
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.True(tour.GetProperty("horsPerimetre").GetBoolean());
        Assert.Equal(ScenarioDialogue.MessageHorsPerimetre, tour.GetProperty("message").GetString());
        Assert.All(tour.GetProperty("fiche").GetProperty("rubriques").EnumerateArray(), rubrique =>
            Assert.Equal(System.Text.Json.JsonValueKind.Null, rubrique.GetProperty("proposition").ValueKind));
    }

    [Fact]
    public async Task Accepter_rejeter_saisir_et_refus_de_rubrique()
    {
        using var client = usine.CreateClient();
        var id = await Creer(client);
        await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/messages", new { message = Demande });

        var (statut, fiche) = await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/propositions/INTITULE/accepter");
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal(("AccepteeIA", Demande), (Rubrique(fiche, "intitule").GetProperty("origine").GetString(), Rubrique(fiche, "intitule").GetProperty("valeur").GetString()));

        Refus(HttpStatusCode.UnprocessableEntity, "proposition.absente", await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/propositions/intitule/rejeter"));
        Refus(HttpStatusCode.UnprocessableEntity, "rubrique.inconnue", await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/propositions/budget/accepter"));
        Refus(HttpStatusCode.UnprocessableEntity, "rubrique.inconnue", await Appeler(client, HttpMethod.Put, $"/api/fiches/{id}/champs/budget", new { valeur = "x" }));

        (statut, fiche) = await Appeler(client, HttpMethod.Put, $"/api/fiches/{id}/champs/application", new { valeur = " SICCRF " });
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal(("Manuelle", "SICCRF", 2), (Rubrique(fiche, "application").GetProperty("origine").GetString(), Rubrique(fiche, "application").GetProperty("valeur").GetString(), fiche.GetProperty("remplies").GetInt32()));

        // Proposition puis rejet : la proposition disparaît, la rubrique reste vide.
        await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/messages", new { message = "Saisie manuelle des contrôles" });
        (statut, fiche) = await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/propositions/situationActuelle/rejeter");
        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, Rubrique(fiche, "situationActuelle").GetProperty("proposition").ValueKind);
    }

    [Fact]
    public async Task Decision_inconnue_et_document_d_une_fiche_non_validee()
    {
        using var client = usine.CreateClient();
        var id = await Creer(client);
        Refus(HttpStatusCode.UnprocessableEntity, "decision.inconnue", await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/decision", new { decision = "Ajourner" }));
        Refus(HttpStatusCode.Conflict, "document.nonValidee", await Appeler(client, HttpMethod.Get, $"/api/fiches/{id}/document"));
        Refus(HttpStatusCode.UnprocessableEntity, "cycle.transitionInterdite", await Appeler(client, HttpMethod.Post, $"/api/fiches/{id}/reprendre"));
    }

    [Fact]
    public async Task Page_d_accueil_balise_vite_remplacee_et_base_des_url_posee()
    {
        using var client = usine.CreateClient();
        var html = await client.GetStringAsync("/", Annulation);
        Assert.DoesNotContain("<vite-scripts", html);
        Assert.Contains("<div id=\"maplayout\" data-url-home-link=\"/\"></div>", html);
        Assert.Contains("id=\"agent-courant\"", html);
        Assert.Contains("data-page=\"accueil\"", html);
        Assert.Contains("id=\"nouvelle-fiche\"", html);
    }

    [Fact]
    public async Task Page_dialogue_rendue_pour_un_identifiant_de_fiche()
    {
        using var client = usine.CreateClient();
        var id = new Guid("6f1c2b3a-0000-4000-8000-0000000000c1");
        var html = await client.GetStringAsync($"/Home/Dialogue/{id}", Annulation);
        Assert.Contains("<div id=\"app\" data-page=\"dialogue\" data-fiche=\"6f1c2b3a-0000-4000-8000-0000000000c1\">", html);
        Assert.DoesNotContain("<vite-scripts", html);
    }

    [Theory]
    [InlineData("/Home/Dialogue")]
    [InlineData("/Home/Dialogue/abc")]
    [InlineData("/Home/Dialogue/00000000-0000-0000-0000-000000000000")]
    public async Task Page_dialogue_sans_identifiant_de_fiche_valide_404(string url)
    {
        using var client = usine.CreateClient();
        using var reponse = await client.GetAsync(url, Annulation);
        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }
}
