using System.Xml.Linq;

namespace ExpressionBesoin.Architecture.Tests;

/// <summary>Références déclarées par un fichier projet.</summary>
internal sealed record ReferencesProjet(
    IReadOnlyList<string> Projets,
    IReadOnlyList<string> Paquets,
    IReadOnlyList<string> Frameworks,
    IReadOnlyList<string> Assemblies);

/// <summary>Règle de dépendance d'un projet de src : projets autorisés ; paquets, frameworks et assemblies permis ou non.</summary>
internal sealed record RegleDependance(IReadOnlyList<string> ProjetsAutorises, bool ReferencesExternesPermises);

/// <summary>Fonctions pures : texte d'un .csproj -> références ; références + règle -> violations.</summary>
internal static class AnalyseCsproj
{
    public static ReferencesProjet Lire(string texteCsproj)
    {
        var document = XDocument.Parse(texteCsproj);
        return new ReferencesProjet(
            Inclusions(document, "ProjectReference").Select(NomProjet).ToArray(),
            Inclusions(document, "PackageReference"),
            Inclusions(document, "FrameworkReference"),
            Inclusions(document, "Reference"));
    }

    /// <summary>Nom du projet sans dossier ni extension ; séparateurs « \ » et « / » acceptés quel que soit le système.</summary>
    public static string NomProjet(string chemin)
    {
        var normalise = chemin.Trim().Replace('\\', '/');
        var nom = normalise[(normalise.LastIndexOf('/') + 1)..];
        return nom.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ? nom[..^".csproj".Length] : nom;
    }

    public static IReadOnlyList<string> Violations(string projet, ReferencesProjet references, RegleDependance regle)
    {
        var violations = references.Projets
            .Where(reference => !regle.ProjetsAutorises.Contains(reference, StringComparer.OrdinalIgnoreCase))
            .Select(reference => $"{projet} : référence de projet interdite « {reference} »")
            .ToList();
        if (!regle.ReferencesExternesPermises)
        {
            violations.AddRange(references.Paquets.Select(paquet => $"{projet} : paquet interdit « {paquet} »"));
            violations.AddRange(references.Frameworks.Select(framework => $"{projet} : framework interdit « {framework} »"));
            violations.AddRange(references.Assemblies.Select(assembly => $"{projet} : assembly interdite « {assembly} »"));
        }

        return violations;
    }

    private static string[] Inclusions(XDocument document, string element) =>
        document.Descendants()
            .Where(noeud => noeud.Name.LocalName == element)
            .Select(noeud => (string?)noeud.Attribute("Include"))
            .Where(valeur => !string.IsNullOrWhiteSpace(valeur))
            .Select(valeur => valeur!.Trim())
            .ToArray();
}
