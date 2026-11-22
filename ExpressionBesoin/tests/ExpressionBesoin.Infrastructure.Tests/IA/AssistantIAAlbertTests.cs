using System.Net;
using System.Text.Json;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.IA;
using Microsoft.Extensions.Logging;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// Adaptateur de l'IA réelle (A1) éprouvé de bout en bout SANS réseau (DEB-Q88) : agent MAF, client OpenAI et maillon du
/// nouvel essai sur un faux fournisseur. On vérifie ce qui part sur le réseau (adresse, clé, modèle, messages, schéma
/// strict) et ce qui revient (texte brut, relu ensuite par <see cref="ReponseIA.Lire"/>). L'IA réelle n'est jugée qu'au
/// critère d'A1, sur le poste.
/// </summary>
public sealed class AssistantIAAlbertTests
{
    private const string Reponse =
        "{\"message\":\"Comment les contrôles se déroulent-ils aujourd'hui ?\",\"propositions\":[{\"rubrique\":\"application\","
        + "\"valeur\":\"SORA\",\"justification\":\"L'agent cite SORA.\"}],\"horsPerimetre\":false}";

    /// <summary>
    /// Profil Albert (température 0,1, sans plafond de jetons). Fausse clé construite par concaténation : une archive ne la
    /// masque pas (règles §13).
    /// </summary>
    private static readonly OptionsIA Options =
        new(new Uri("https://albert.test/v1"), "openweight-medium", "cle-" + "essai") { Temperature = 0.1f };

    /// <summary>Profil à la manière de Claude (lot A6, DEB-Q105, DEB-Q128) : sans température, plafond de jetons.</summary>
    private static readonly OptionsIA OptionsClaude =
        new(new Uri("https://anthropic.test/v1"), "claude-essai", "cle-" + "claude") { JetonsMax = 4096 };

    private readonly List<TimeSpan> _attentes = [];

    private readonly JournalEssai _journal = new();

    private static ContexteTour Contexte() =>
        new(
            "C'est l'application SORA.",
            ListeImmuable<Echange>.De(new[]
            {
                new Echange(AuteurEchange.Agent, "Contrôle des plates-formes e-commerce", Essais.T0),
                new Echange(AuteurEchange.IA, "Quelle application est concernée ?", Essais.T0.AddSeconds(2)),
            }),
            ListeImmuable<EtatRubrique>.De(new[] { new EtatRubrique("application", "Application ou service concerné", true, null, null) }),
            ListeImmuable<string>.De(new[] { "application" }),
            "application",
            ListeImmuable<string>.De(new[] { "application" }));

    private AssistantIAAlbert Assistant(FauxFournisseurIA faux, OptionsIA? options = null) =>
        AssistantIAAlbert.Creer(
            options ?? Options,
            faux,
            (attente, _) =>
            {
                _attentes.Add(attente);
                return Task.CompletedTask;
            },
            _journal);

    /// <summary>Texte d'un message OpenAI : chaîne, ou tableau de parties <c>{ type: "text", text }</c>.</summary>
    private static string Texte(JsonElement contenu) =>
        contenu.ValueKind == JsonValueKind.String
            ? contenu.GetString()!
            : string.Concat(contenu.EnumerateArray().Select(partie => partie.GetProperty("text").GetString()));

    /// <summary>
    /// Plafond de jetons envoyé, <c>null</c> s'il est absent : <c>max_completion_tokens</c> (client OpenAI .NET 2) ou
    /// <c>max_tokens</c>, tous deux acceptés par Albert et par la couche compatible d'Anthropic.
    /// </summary>
    private static int? Jetons(JsonElement racine) =>
        racine.TryGetProperty("max_completion_tokens", out var plafond) || racine.TryGetProperty("max_tokens", out plafond)
            ? plafond.GetInt32()
            : null;

    [Fact]
    public async Task Requete_au_contrat_et_texte_rendu_tel_quel()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Completion(Reponse));
        using var assistant = Assistant(faux);
        var contexte = Contexte();

        Assert.Equal(Reponse, await assistant.Repondre(contexte, Essais.Annulation));

        var requete = Assert.Single(faux.Requetes);
        Assert.Equal(HttpMethod.Post, requete.Methode);
        Assert.Equal(new Uri("https://albert.test/v1/chat/completions"), requete.Adresse);
        Assert.Equal("Bearer cle-essai", requete.Autorisation);
        using var corps = JsonDocument.Parse(requete.Corps!);
        var racine = corps.RootElement;
        Assert.Equal("openweight-medium", racine.GetProperty("model").GetString());
        Assert.Equal(0.1, racine.GetProperty("temperature").GetDouble(), 3);
        Assert.Null(Jetons(racine));
        Assert.Equal(
            new[]
            {
                ("system", PromptFiche.Consignes),
                ("user", "Contrôle des plates-formes e-commerce"),
                ("assistant", "Quelle application est concernée ?"),
                ("user", PromptFiche.Tour(contexte)),
            },
            racine.GetProperty("messages").EnumerateArray()
                .Select(message => (message.GetProperty("role").GetString()!, Texte(message.GetProperty("content"))))
                .ToArray());
        var format = racine.GetProperty("response_format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        var schema = format.GetProperty("json_schema");
        Assert.Equal(("reponse_fiche", true), (schema.GetProperty("name").GetString(), schema.GetProperty("strict").GetBoolean()));
        Assert.Equal(
            new[] { "message", "propositions", "questions", "horsPerimetre" },
            schema.GetProperty("schema").GetProperty("properties").EnumerateObject().Select(propriete => propriete.Name).ToArray());
        Assert.False(racine.TryGetProperty("tools", out _));
        Assert.Empty(_attentes);
        Assert.Empty(_journal.Ecritures);
    }

    /// <summary>
    /// DEB-Q113 (lot A5b) : ce qui part sur le réseau est le corps nettoyé, comme CSAgent : chaque message n'a que
    /// <c>role</c> et <c>content</c>, en chaîne, quelle que soit la forme produite par le client OpenAI .NET.
    /// </summary>
    /// <summary>
    /// Profil sans température (Claude 4.7 et suivants refusent toute valeur inférieure à 1, DEB-Q105) et avec plafond de
    /// jetons (DEB-Q128) : le champ <c>temperature</c> est ABSENT de la requête, le plafond présent ; le reste du contrat
    /// (modèle, clé, schéma strict) est celui du profil Albert.
    /// </summary>
    [Fact]
    public async Task Profil_sans_temperature_avec_plafond_de_jetons()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Completion(Reponse));
        using var assistant = Assistant(faux, OptionsClaude);

        Assert.Equal(Reponse, await assistant.Repondre(Contexte(), Essais.Annulation));

        var requete = Assert.Single(faux.Requetes);
        Assert.Equal(new Uri("https://anthropic.test/v1/chat/completions"), requete.Adresse);
        Assert.Equal("Bearer cle-claude", requete.Autorisation);
        using var corps = JsonDocument.Parse(requete.Corps!);
        var racine = corps.RootElement;
        Assert.Equal("claude-essai", racine.GetProperty("model").GetString());
        Assert.False(racine.TryGetProperty("temperature", out _));
        Assert.Equal(4096, Jetons(racine));
        var schema = racine.GetProperty("response_format").GetProperty("json_schema");
        Assert.Equal(("reponse_fiche", true), (schema.GetProperty("name").GetString(), schema.GetProperty("strict").GetBoolean()));
    }

    [Fact]
    public async Task Messages_envoyes_role_et_contenu_en_chaine_seulement()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Completion(Reponse));
        using var assistant = Assistant(faux);

        await assistant.Repondre(Contexte(), Essais.Annulation);

        using var corps = JsonDocument.Parse(Assert.Single(faux.Requetes).Corps!);
        foreach (var message in corps.RootElement.GetProperty("messages").EnumerateArray())
        {
            Assert.Equal(new[] { "role", "content" }, message.EnumerateObject().Select(propriete => propriete.Name).ToArray());
            Assert.Equal(JsonValueKind.String, message.GetProperty("content").ValueKind);
        }

        Assert.Equal("application/json", Assert.Single(faux.Requetes).TypeContenu?.Split(';')[0]);
    }

    /// <summary>
    /// DEB-A5 (21/11/2026 : 2e tour vide, « La réponse de l'IA n'est pas un JSON valide ») et DEB-Q112, DEB-Q114 : une
    /// réponse vide est journalisée avec la requête envoyée, puis redemandée une fois.
    /// </summary>
    [Fact]
    public async Task Reponse_vide_journalisee_puis_redemandee_une_fois()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Completion(null), () => FauxFournisseurIA.Completion(Reponse));
        using var assistant = Assistant(faux);

        Assert.Equal(Reponse, await assistant.Repondre(Contexte(), Essais.Annulation));

        Assert.Equal(2, faux.Requetes.Count);
        Assert.Equal(faux.Requetes[0], faux.Requetes[1]);
        var (niveau, message) = Assert.Single(_journal.Ecritures);
        Assert.Equal(LogLevel.Warning, niveau);
        Assert.StartsWith("Réponse vide de l'IA (DEB-A5). Requête envoyée (", message);
        Assert.Contains(faux.Requetes[0].Corps!, message);
        Assert.DoesNotContain("cle-essai", message);
        Assert.Contains("\"content\":null", message);
    }

    [Fact]
    public async Task Deux_reponses_vides_panne_jamais_un_refus()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Completion(""), () => FauxFournisseurIA.Completion("  "));
        using var assistant = Assistant(faux);

        var panne = await Assert.ThrowsAsync<InvalidOperationException>(async () => await assistant.Repondre(Contexte(), Essais.Annulation));

        Assert.Equal(AssistantIAAlbert.MessageReponsesVides, panne.Message);
        Assert.Equal(2, faux.Requetes.Count);
        Assert.Equal(2, _journal.Ecritures.Count);
    }

    [Fact]
    public async Task Reponse_lue_par_le_domaine()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Completion(Reponse));
        using var assistant = Assistant(faux);

        var lue = Essais.Valeur(ReponseIA.Lire(await assistant.Repondre(Contexte(), Essais.Annulation)));

        Assert.Equal(("Comment les contrôles se déroulent-ils aujourd'hui ?", false), (lue.Message, lue.HorsPerimetre));
        var proposition = Assert.Single(lue.Propositions);
        Assert.Equal((Rubrique.Application, "SORA"), (proposition.Rubrique, proposition.Proposition.Valeur));
    }

    [Fact]
    public async Task Limite_par_minute_un_nouvel_essai_meme_requete()
    {
        var faux = new FauxFournisseurIA(
            () => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, FauxFournisseurIA.LimiteMinute, TimeSpan.FromSeconds(3)),
            () => FauxFournisseurIA.Completion(Reponse));
        using var assistant = Assistant(faux);

        Assert.Equal(Reponse, await assistant.Repondre(Contexte(), Essais.Annulation));

        Assert.Equal(new[] { TimeSpan.FromSeconds(3) }, _attentes);
        Assert.Equal(2, faux.Requetes.Count);
        Assert.Equal(faux.Requetes[0], faux.Requetes[1]);
    }

    [Fact]
    public async Task Limite_par_jour_panne_sans_nouvel_essai()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, FauxFournisseurIA.LimiteJour));
        using var assistant = Assistant(faux);

        var panne = await Record.ExceptionAsync(async () => await assistant.Repondre(Contexte(), Essais.Annulation));

        Assert.NotNull(panne);
        Assert.Single(faux.Requetes);
        Assert.Empty(_attentes);
    }

    [Fact]
    public async Task Erreur_du_serveur_panne()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Reponse(HttpStatusCode.InternalServerError, "{\"detail\":\"erreur\"}"));
        using var assistant = Assistant(faux);

        Assert.NotNull(await Record.ExceptionAsync(async () => await assistant.Repondre(Contexte(), Essais.Annulation)));
        Assert.Single(faux.Requetes);
    }

    [Fact]
    public async Task Attente_par_defaut_reelle_et_breve()
    {
        var faux = new FauxFournisseurIA(
            () => FauxFournisseurIA.Reponse(HttpStatusCode.TooManyRequests, FauxFournisseurIA.LimiteMinute, TimeSpan.Zero),
            () => FauxFournisseurIA.Completion(Reponse));
        using var assistant = AssistantIAAlbert.Creer(Options, faux);

        Assert.Equal(Reponse, await assistant.Repondre(Contexte(), Essais.Annulation));
        Assert.Equal(2, faux.Requetes.Count);
    }

    [Fact]
    public void Transport_par_defaut_sans_appel_reseau_a_la_creation()
    {
        using var assistant = AssistantIAAlbert.Creer(Options);
        Assert.IsAssignableFrom<IAssistantIA>(assistant);
    }

    [Fact]
    public void Options_d_un_tour_neuves_a_chaque_appel()
    {
        var options = AssistantIAAlbert.OptionsTour(Options);
        var claude = AssistantIAAlbert.OptionsTour(OptionsClaude);

        Assert.NotSame(options, AssistantIAAlbert.OptionsTour(Options));
        Assert.Equal(((float?)0.1f, (int?)null), (options.Temperature, options.MaxOutputTokens));
        Assert.Equal(((float?)null, (int?)4096), (claude.Temperature, claude.MaxOutputTokens));
        Assert.Equal(true, options.AdditionalProperties![AssistantIAAlbert.CleStrict]);
        Assert.Equal(new[] { TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60) }, new[] { OptionsIA.DelaiParDefaut, Options.Delai });
    }

    [Fact]
    public void Delai_reglable()
    {
        var options = new OptionsIA(new Uri("https://albert.test/v1"), "m", "cle-" + "essai") { Delai = TimeSpan.FromSeconds(5) };
        Assert.Equal((TimeSpan.FromSeconds(5), (float?)null, (int?)null), (options.Delai, options.Temperature, options.JetonsMax));
    }
}
