using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ExpressionBesoin.Web.Vite;

/// <summary>Entrée du manifeste de Vite : fichier JavaScript produit et feuilles de style qu'il importe.</summary>
public sealed record EntreeManifeste(string Fichier, ImmutableArray<string> Css);

/// <summary>
/// Balises de <c>&lt;vite-scripts /&gt;</c> (gabarit front F8), fonctions pures : en Localhost, client de rechargement à
/// chaud et point d'entrée servis par le serveur de développement de Vite ; ailleurs, CSS puis JS du manifeste, préfixés
/// par le PathBase (<c>""</c> à la racine, <c>/monsite</c> en sous-application) ; entrée ou manifeste absent →
/// commentaire HTML explicite plutôt qu'une page muette.
/// </summary>
public static class ViteTags
{
    /// <summary>Serveur de développement de Vite (<c>server.port</c> de <c>vite.config.ts</c>).</summary>
    public const string ServeurDeveloppement = "http://localhost:5173";

    /// <summary>Dossier de sortie du build sous <c>wwwroot</c> (<c>build.outDir</c>).</summary>
    public const string Dossier = "dist";

    public const string ManifesteAbsent = "<!-- vite : manifeste introuvable (wwwroot/dist/.vite/manifest.json) : lancer npm run build -->";

    private static readonly JsonSerializerOptions Lecture = new() { PropertyNameCaseInsensitive = true };

    public static string Developpement(string entree)
    {
        var encodee = HtmlEncoder.Default.Encode(entree.TrimStart('/'));
        return $"<script type=\"module\" src=\"{ServeurDeveloppement}/@vite/client\"></script>\n"
            + $"<script type=\"module\" src=\"{ServeurDeveloppement}/{encodee}\"></script>";
    }

    /// <summary>
    /// Manifeste de Vite (clés <c>file</c>, <c>css</c>, casse ignorée) indexé par fichier source. JSON illisible ou entrée
    /// sans <c>file</c> : panne (<see cref="InvalidDataException"/>), jamais deviné.
    /// </summary>
    public static FrozenDictionary<string, EntreeManifeste> LireManifeste(string json)
    {
        Dictionary<string, EntreeJson?>? brut;
        try
        {
            brut = JsonSerializer.Deserialize<Dictionary<string, EntreeJson?>>(json, Lecture);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Manifeste de Vite illisible : " + exception.Message, exception);
        }

        if (brut is null)
        {
            throw new InvalidDataException("Manifeste de Vite vide (null).");
        }

        var entrees = new Dictionary<string, EntreeManifeste>(brut.Count, StringComparer.Ordinal);
        foreach (var (source, entree) in brut)
        {
            if (entree is null || string.IsNullOrEmpty(entree.File))
            {
                throw new InvalidDataException($"Manifeste de Vite : l'entrée « {source} » n'a pas de file.");
            }

            entrees[source] = new EntreeManifeste(entree.File, entree.Css is null ? ImmutableArray<string>.Empty : [.. entree.Css]);
        }

        return entrees.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public static string Production(FrozenDictionary<string, EntreeManifeste>? manifeste, string entree, string pathBase)
    {
        if (manifeste is null)
        {
            return ManifesteAbsent;
        }

        if (!manifeste.TryGetValue(entree, out var trouvee))
        {
            return $"<!-- vite : entrée « {HtmlEncoder.Default.Encode(entree)} » absente du manifeste : lancer npm run build -->";
        }

        var racine = pathBase.TrimEnd('/') + "/" + Dossier + "/";
        var html = new StringBuilder();
        foreach (var css in trouvee.Css)
        {
            html.Append("<link rel=\"stylesheet\" href=\"").Append(HtmlEncoder.Default.Encode(racine + css)).Append("\" />\n");
        }

        return html.Append("<script type=\"module\" src=\"").Append(HtmlEncoder.Default.Encode(racine + trouvee.Fichier))
            .Append("\"></script>").ToString();
    }

    private sealed record EntreeJson(string? File, string[]? Css);
}
