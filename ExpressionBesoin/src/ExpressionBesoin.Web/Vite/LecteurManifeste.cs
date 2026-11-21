using System.Collections.Frozen;

namespace ExpressionBesoin.Web.Vite;

/// <summary>
/// Coquille d'E/S du manifeste (singleton) : un <c>stat</c> par rendu, relecture et analyse seulement quand la date
/// d'écriture change (un <c>npm run build</c> pendant que le site tourne est pris en compte). Aucune décision : l'analyse
/// est <see cref="ViteTags.LireManifeste"/>.
/// </summary>
public sealed class LecteurManifeste(IWebHostEnvironment environnement)
{
    private Lecture? _derniere;

    /// <summary>Chemin du manifeste, <c>null</c> sans dossier <c>wwwroot</c>.</summary>
    public string? Chemin => string.IsNullOrEmpty(environnement.WebRootPath)
        ? null
        : Path.Combine(environnement.WebRootPath, ViteTags.Dossier, ".vite", "manifest.json");

    /// <summary>Manifeste analysé, <c>null</c> s'il n'existe pas.</summary>
    public FrozenDictionary<string, EntreeManifeste>? Lire()
    {
        var chemin = Chemin;
        if (chemin is null)
        {
            return null;
        }

        var fichier = new FileInfo(chemin);
        if (!fichier.Exists)
        {
            return null;
        }

        var date = fichier.LastWriteTimeUtc;
        var derniere = Volatile.Read(ref _derniere);
        if (derniere is not null && derniere.Date == date && derniere.Chemin == chemin)
        {
            return derniere.Manifeste;
        }

        var manifeste = ViteTags.LireManifeste(File.ReadAllText(chemin));
        Volatile.Write(ref _derniere, new Lecture(chemin, date, manifeste));
        return manifeste;
    }

    private sealed record Lecture(string Chemin, DateTime Date, FrozenDictionary<string, EntreeManifeste> Manifeste);
}
