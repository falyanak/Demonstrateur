namespace ExpressionBesoin.Architecture.Tests;

/// <summary>Table de vérité des fonctions pures, sur des .csproj écrits dans le test.</summary>
public sealed class AnalyseCsprojTests
{
    private const string CsprojComplet = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <ProjectReference Include="..\Couche.Domain\Couche.Domain.csproj" />
            <ProjectReference Include="../Couche.Application/Couche.Application.csproj" />
            <PackageReference Include="Microsoft.Data.Sqlite" />
            <FrameworkReference Include="Microsoft.AspNetCore.App" />
            <Reference Include="System.Drawing" />
            <PackageReference Update="Ignore.Update" />
            <InternalsVisibleTo Include="Couche.Domain.Tests" />
          </ItemGroup>
        </Project>
        """;

    private static readonly RegleDependance RegleStricte = new(["Couche.Domain"], ReferencesExternesPermises: false);
    private static readonly RegleDependance RegleOuverte = new(["Couche.Domain"], ReferencesExternesPermises: true);

    [Fact]
    public void Lire_releve_les_quatre_sortes_de_references()
    {
        var references = AnalyseCsproj.Lire(CsprojComplet);
        Assert.Equal(new[] { "Couche.Domain", "Couche.Application" }, references.Projets);
        Assert.Equal(new[] { "Microsoft.Data.Sqlite" }, references.Paquets);
        Assert.Equal(new[] { "Microsoft.AspNetCore.App" }, references.Frameworks);
        Assert.Equal(new[] { "System.Drawing" }, references.Assemblies);
    }

    [Fact]
    public void Lire_un_projet_sans_reference_rend_des_listes_vides()
    {
        var references = AnalyseCsproj.Lire("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup /></Project>");
        Assert.Empty(references.Projets);
        Assert.Empty(references.Paquets);
        Assert.Empty(references.Frameworks);
        Assert.Empty(references.Assemblies);
    }

    [Fact]
    public void Lire_ignore_une_reference_sans_Include_ou_vide()
    {
        var references = AnalyseCsproj.Lire(
            "<Project><ItemGroup><ProjectReference /><PackageReference Include=\"  \" /></ItemGroup></Project>");
        Assert.Empty(references.Projets);
        Assert.Empty(references.Paquets);
    }

    [Theory]
    [InlineData(@"..\A\Couche.Web.csproj", "Couche.Web")]
    [InlineData("../A/Couche.Web.csproj", "Couche.Web")]
    [InlineData(@"..\A/B\Couche.Web.CSPROJ", "Couche.Web")]
    [InlineData("Couche.Web.csproj", "Couche.Web")]
    [InlineData("  Couche.Web.csproj  ", "Couche.Web")]
    [InlineData("Couche.Web", "Couche.Web")]
    public void NomProjet_sans_dossier_ni_extension(string chemin, string attendu) =>
        Assert.Equal(attendu, AnalyseCsproj.NomProjet(chemin));

    [Fact]
    public void Violations_aucune_pour_un_projet_conforme()
    {
        var references = new ReferencesProjet(["Couche.Domain"], [], [], []);
        Assert.Empty(AnalyseCsproj.Violations("Couche.Application", references, RegleStricte));
    }

    [Fact]
    public void Violations_projet_autorise_compare_sans_la_casse()
    {
        var references = new ReferencesProjet(["couche.domain"], [], [], []);
        Assert.Empty(AnalyseCsproj.Violations("Couche.Application", references, RegleStricte));
    }

    [Fact]
    public void Violations_toutes_listees_quand_les_references_externes_sont_interdites()
    {
        var violations = AnalyseCsproj.Violations("Couche.Application", AnalyseCsproj.Lire(CsprojComplet), RegleStricte);
        Assert.Equal(
            new[]
            {
                "Couche.Application : référence de projet interdite « Couche.Application »",
                "Couche.Application : paquet interdit « Microsoft.Data.Sqlite »",
                "Couche.Application : framework interdit « Microsoft.AspNetCore.App »",
                "Couche.Application : assembly interdite « System.Drawing »",
            },
            violations);
    }

    [Fact]
    public void Violations_seulement_les_projets_quand_les_references_externes_sont_permises()
    {
        var violations = AnalyseCsproj.Violations("Couche.Infrastructure", AnalyseCsproj.Lire(CsprojComplet), RegleOuverte);
        Assert.Equal(
            new[] { "Couche.Infrastructure : référence de projet interdite « Couche.Application »" },
            violations);
    }

    [Fact]
    public void Violations_aucun_projet_autorise_refuse_toute_reference_de_projet()
    {
        var references = new ReferencesProjet(["Couche.Domain"], [], [], []);
        var regle = new RegleDependance([], ReferencesExternesPermises: false);
        Assert.Equal(
            new[] { "Couche.Domain : référence de projet interdite « Couche.Domain »" },
            AnalyseCsproj.Violations("Couche.Domain", references, regle));
    }

    [Theory]
    [InlineData("paquet")]
    [InlineData("framework")]
    [InlineData("assembly")]
    public void Violations_une_seule_reference_externe_suffit(string sorte)
    {
        var references = sorte switch
        {
            "paquet" => new ReferencesProjet([], ["P"], [], []),
            "framework" => new ReferencesProjet([], [], ["F"], []),
            _ => new ReferencesProjet([], [], [], ["A"]),
        };
        Assert.Single(AnalyseCsproj.Violations("Couche.Domain", references, RegleStricte));
        Assert.Empty(AnalyseCsproj.Violations("Couche.Domain", references, RegleOuverte));
    }
}
