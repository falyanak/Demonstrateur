namespace ExpressionBesoin.Architecture.Tests;

/// <summary>Règles appliquées aux vrais fichiers de la solution : un projet de src, une règle ; chaque projet conforme.</summary>
public sealed class SolutionTests
{
    /// <summary>Dossier de la solution (celui du .slnx), partagé avec <see cref="SdkTests"/>.</summary>
    internal static readonly string Racine = TrouverRacine();

    [Fact]
    public void Chaque_projet_de_src_a_une_regle_et_chaque_regle_un_projet()
    {
        var projets = ProjetsSrc().Keys.Order(StringComparer.Ordinal).ToArray();
        var regles = ReglesSolution.Regles.Keys.Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(regles, projets);
    }

    [Fact]
    public void Chaque_projet_de_src_respecte_sa_regle()
    {
        var violations = ProjetsSrc()
            .SelectMany(projet => AnalyseCsproj.Violations(
                projet.Key, AnalyseCsproj.Lire(projet.Value), ReglesSolution.Regles[projet.Key]))
            .ToArray();
        Assert.Empty(violations);
    }

    [Fact]
    public void Directory_Build_props_de_la_solution_n_ajoute_aucune_reference()
    {
        // Ce fichier s'applique à tous les projets de src : une référence ajoutée ici échapperait aux règles.
        var references = AnalyseCsproj.Lire(File.ReadAllText(Path.Combine(Racine, "Directory.Build.props")));
        Assert.Empty(references.Projets);
        Assert.Empty(references.Paquets);
        Assert.Empty(references.Frameworks);
        Assert.Empty(references.Assemblies);
    }

    /// <summary>Projets de src par chemin conventionnel : src/Nom/Nom.csproj (jamais « le premier trouvé »).</summary>
    private static Dictionary<string, string> ProjetsSrc()
    {
        var projets = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var dossier in Directory.GetDirectories(Path.Combine(Racine, "src")))
        {
            var nom = Path.GetFileName(dossier);
            var csproj = Path.Combine(dossier, nom + ".csproj");
            Assert.True(File.Exists(csproj), $"projet attendu au chemin conventionnel : {csproj}");
            projets[nom] = File.ReadAllText(csproj);
        }

        return projets;
    }

    private static string TrouverRacine()
    {
        for (var dossier = new DirectoryInfo(AppContext.BaseDirectory); dossier is not null; dossier = dossier.Parent)
        {
            if (File.Exists(Path.Combine(dossier.FullName, "ExpressionBesoin.slnx")))
            {
                return dossier.FullName;
            }
        }

        throw new InvalidOperationException($"ExpressionBesoin.slnx introuvable en remontant depuis {AppContext.BaseDirectory}");
    }
}
