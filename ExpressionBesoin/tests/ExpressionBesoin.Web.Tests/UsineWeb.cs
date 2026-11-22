using ExpressionBesoin.Application.Dictee;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Web.Hebergement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpressionBesoin.Web.Tests;

/// <summary>
/// Usine de l'application pour les tests : environnement, base, IA et horloge fixés ici, jamais hérités (règles §6). Une
/// base SQLite par usine, dans un dossier temporaire supprimé avec elle. IA scénarisée par défaut ; profils de l'IA définis
/// ici (lot A6), jamais pris au catalogue d'<c>appsettings.json</c> : le profil d'essai lit sa clé dans
/// <see cref="VariableCleEssai"/>, qui n'existe pas sur le poste, valeur <see cref="CleIA"/> (vide par défaut) ; les clés
/// réelles du poste (variables utilisateur) n'entrent jamais dans un test.
/// </summary>
public class UsineWeb : WebApplicationFactory<Program>
{
    /// <summary>21/11/2026 9 h UTC : année 2026 à Paris, numéros EB-2026-NNNN.</summary>
    public static readonly DateTimeOffset Maintenant = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Profil d'essai d'une IA compatible OpenAI, sur une adresse qui n'est jamais appelée au démarrage.</summary>
    public const string ProfilEssai = "Essai";

    /// <summary>Variable de la clé du profil d'essai, propre aux tests.</summary>
    public const string VariableCleEssai = "DEB_CLE_IA_ESSAI";

    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "DEB-web-" + Guid.NewGuid().ToString("N"));

    /// <summary>Valeur de <see cref="CompositionFiches.CleBase"/> donnée à l'application.</summary>
    protected virtual string? Base => Path.Combine(_dossier, "fiches.db");

    /// <summary>Valeur de <see cref="CompositionIA.CleProfil"/> donnée à l'application.</summary>
    protected virtual string ProfilIA => CompositionIA.Scenarise;

    /// <summary>Valeur de la variable <see cref="VariableCleEssai"/> vue par l'application.</summary>
    protected virtual string CleIA => "";

    protected virtual void Remplacer(IServiceCollection services)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [CompositionFiches.CleBase] = Base,
                [CompositionIA.CleProfil] = ProfilIA,
                [$"{CompositionIA.CleProfils}:{CompositionIA.Scenarise}:{CompositionIA.Fournisseur}"] = CompositionIA.Scenarise,
                [$"{CompositionIA.CleProfils}:{ProfilEssai}:{CompositionIA.Point}"] = "https://albert.test/v1",
                [$"{CompositionIA.CleProfils}:{ProfilEssai}:{CompositionIA.Modele}"] = "modele-essai",
                [$"{CompositionIA.CleProfils}:{ProfilEssai}:{CompositionIA.VariableCle}"] = VariableCleEssai,
                [$"{CompositionIA.CleProfils}:{ProfilEssai}:{CompositionIA.ModeleTranscription}"] = "whisper-essai",
                [VariableCleEssai] = CleIA,
            }));
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(new HorlogeFixe(Maintenant));
            Remplacer(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_dossier))
            {
                Directory.Delete(_dossier, recursive: true);
            }
        }
    }
}

/// <summary>Configuration sans chemin de base : le démarrage doit s'arrêter, explicitement.</summary>
public sealed class UsineSansBase : UsineWeb
{
    protected override string? Base => null;
}

/// <summary>IA réelle (profil d'essai) configurée avec une fausse clé : le site démarre sans appel réseau (DEB-Q86, A6).</summary>
public sealed class UsineAlbert : UsineWeb
{
    protected override string ProfilIA => ProfilEssai;

    /// <summary>Fausse clé construite par concaténation : une archive ne la masque pas (règles §13).</summary>
    protected override string CleIA => "cle-" + "essai";
}

/// <summary>IA réelle (profil d'essai) sans clé : le démarrage doit s'arrêter, explicitement (DEB-Q86, A6).</summary>
public sealed class UsineAlbertSansCle : UsineWeb
{
    protected override string ProfilIA => ProfilEssai;
}

/// <summary>IA en panne (exception) : la route répond 503, rien n'est écrit (DEB-Q22).</summary>
public sealed class UsinePanne : UsineWeb
{
    protected override void Remplacer(IServiceCollection services) => services.AddSingleton<IAssistantIA, AssistantEnPanne>();

    private sealed class AssistantEnPanne : IAssistantIA
    {
        public ValueTask<string> Repondre(ContexteTour contexte, CancellationToken annulation) =>
            throw new HttpRequestException("Fournisseur d'IA injoignable (essai).");
    }
}

/// <summary>
/// Dictée offerte, modèle simulé (lot S1) : la réponse dépend du nombre d'octets d'échantillons reçus, pour jouer chaque
/// issue de la route sans réseau : 2 → blanc (silence), 6 → panne, sinon <see cref="Texte"/>.
/// </summary>
public sealed class UsineDictee : UsineWeb
{
    public const string Texte = "Le copieur du bureau 1A ne démarre plus.";

    protected override void Remplacer(IServiceCollection services) => services.AddSingleton(new ServiceDictee(new TranscripteurEssai()));

    private sealed class TranscripteurEssai : ITranscripteur
    {
        public ValueTask<string> Transcrire(ReadOnlyMemory<byte> wav, CancellationToken annulation) =>
            (wav.Length - DecisionsDictee.EnTete) switch
            {
                2 => ValueTask.FromResult("  "),
                6 => throw new HttpRequestException("Whisper injoignable (essai)."),
                _ => ValueTask.FromResult(Texte),
            };
    }
}

public sealed class HorlogeFixe(DateTimeOffset maintenant) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => maintenant;
}
