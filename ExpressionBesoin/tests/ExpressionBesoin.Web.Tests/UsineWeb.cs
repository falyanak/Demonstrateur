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
/// base SQLite par usine, dans un dossier temporaire supprimé avec elle. IA scénarisée par défaut ; la clé d'Albert du
/// poste (variable utilisateur) n'entre jamais dans un test : elle est remplacée par <see cref="CleIA"/>, vide par défaut.
/// </summary>
public class UsineWeb : WebApplicationFactory<Program>
{
    /// <summary>21/11/2026 9 h UTC : année 2026 à Paris, numéros EB-2026-NNNN.</summary>
    public static readonly DateTimeOffset Maintenant = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);

    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "DEB-web-" + Guid.NewGuid().ToString("N"));

    /// <summary>Valeur de <see cref="CompositionFiches.CleBase"/> donnée à l'application.</summary>
    protected virtual string? Base => Path.Combine(_dossier, "fiches.db");

    /// <summary>Valeur de <see cref="CompositionIA.CleFournisseur"/> donnée à l'application.</summary>
    protected virtual string FournisseurIA => CompositionIA.Scenarise;

    /// <summary>Valeur de la variable <see cref="CompositionIA.VariableCle"/> vue par l'application.</summary>
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
                [CompositionIA.CleFournisseur] = FournisseurIA,
                [CompositionIA.ClePoint] = "https://albert.test/v1",
                [CompositionIA.CleModele] = "modele-essai",
                [CompositionIA.VariableCle] = CleIA,
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

/// <summary>IA réelle configurée avec une fausse clé : le site démarre sans appel réseau (DEB-Q86).</summary>
public sealed class UsineAlbert : UsineWeb
{
    protected override string FournisseurIA => CompositionIA.Albert;

    /// <summary>Fausse clé construite par concaténation : une archive ne la masque pas (règles §13).</summary>
    protected override string CleIA => "cle-" + "essai";
}

/// <summary>IA réelle sans clé : le démarrage doit s'arrêter, explicitement (DEB-Q86).</summary>
public sealed class UsineAlbertSansCle : UsineWeb
{
    protected override string FournisseurIA => CompositionIA.Albert;
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

public sealed class HorlogeFixe(DateTimeOffset maintenant) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => maintenant;
}
