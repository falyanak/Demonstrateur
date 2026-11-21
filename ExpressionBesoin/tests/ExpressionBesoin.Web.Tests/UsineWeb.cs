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
/// Usine de l'application pour les tests : environnement, base et horloge fixés ici, jamais hérités (règles §6). Une base
/// SQLite par usine, dans un dossier temporaire supprimé avec elle.
/// </summary>
public class UsineWeb : WebApplicationFactory<Program>
{
    /// <summary>21/11/2026 9 h UTC : année 2026 à Paris, numéros EB-2026-NNNN.</summary>
    public static readonly DateTimeOffset Maintenant = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);

    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "DEB-web-" + Guid.NewGuid().ToString("N"));

    /// <summary>Valeur de <see cref="CompositionFiches.CleBase"/> donnée à l'application.</summary>
    protected virtual string? Base => Path.Combine(_dossier, "fiches.db");

    protected virtual void Remplacer(IServiceCollection services)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { [CompositionFiches.CleBase] = Base }));
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
