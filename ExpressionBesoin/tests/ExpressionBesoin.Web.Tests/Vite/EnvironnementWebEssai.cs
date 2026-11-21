using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace ExpressionBesoin.Web.Tests.Vite;

/// <summary>Environnement web d'essai : nom et dossier wwwroot modifiables.</summary>
internal sealed class EnvironnementWebEssai(string nom, string? webRoot) : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = nom;
    public string ApplicationName { get; set; } = "Essai";
    public string WebRootPath { get; set; } = webRoot!;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>Dossier wwwroot temporaire, supprimé à la libération.</summary>
internal sealed class WebRootTemporaire : IDisposable
{
    public string Chemin { get; } = Path.Combine(Path.GetTempPath(), "DEB-wwwroot-" + Guid.NewGuid().ToString("N"));

    public string Manifeste => Path.Combine(Chemin, "dist", ".vite", "manifest.json");

    public void Ecrire(string json, DateTime dateUtc)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Manifeste)!);
        File.WriteAllText(Manifeste, json);
        File.SetLastWriteTimeUtc(Manifeste, dateUtc);
    }

    public void Dispose()
    {
        if (Directory.Exists(Chemin))
        {
            Directory.Delete(Chemin, recursive: true);
        }
    }
}
