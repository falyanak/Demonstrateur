using System.Text.Json;
using System.Xml.Linq;

namespace ExpressionBesoin.Architecture.Tests;

/// <summary>
/// SDK bloqué (DEB-Q47) et tenu depuis la racine du dépôt (DEB-A2, DEB-Q58) : <c>dotnet</c> cherche <c>global.json</c>
/// depuis le dossier courant, et les commandes du harnais partent de la racine du dépôt. Un seul <c>global.json</c>, à
/// cette racine ; sa version est celle qu'exige la garde <c>VerifierSdk</c> de <c>Directory.Build.props</c>.
/// </summary>
public sealed class SdkTests
{
    private static readonly string DossierSolution = SolutionTests.Racine;
    private static readonly string RacineDepot = Directory.GetParent(DossierSolution)!.FullName;

    [Fact]
    public void Global_json_est_a_la_racine_du_depot_et_bloque_le_SDK()
    {
        var chemin = Path.Combine(RacineDepot, "global.json");
        Assert.True(File.Exists(chemin), $"global.json attendu à la racine du dépôt : {chemin}");
        Assert.Equal("disable", LireSdk(chemin).RollForward);
    }

    [Fact]
    public void Aucun_global_json_dans_le_dossier_de_solution()
    {
        // Un second fichier primerait pour les commandes lancées depuis la solution, et les deux finiraient par diverger.
        var chemin = Path.Combine(DossierSolution, "global.json");
        Assert.False(File.Exists(chemin), $"global.json inattendu : {chemin}");
    }

    [Fact]
    public void La_garde_du_build_exige_la_version_de_global_json()
    {
        var version = LireSdk(Path.Combine(RacineDepot, "global.json")).Version;
        var props = XDocument.Load(Path.Combine(DossierSolution, "Directory.Build.props"));

        Assert.Equal(new[] { version }, props.Descendants("SdkAttendu").Select(e => e.Value).ToArray());
        var garde = props.Descendants("Target").Single(e => (string?)e.Attribute("Name") == "VerifierSdk");
        Assert.Equal("BeforeBuild", (string?)garde.Attribute("BeforeTargets"));
        Assert.Equal("'$(NETCoreSdkVersion)' != '$(SdkAttendu)'", (string?)garde.Attribute("Condition"));
        Assert.Single(garde.Elements("Error"));
    }

    private static (string Version, string RollForward) LireSdk(string chemin)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(chemin));
        var sdk = document.RootElement.GetProperty("sdk");
        return (sdk.GetProperty("version").GetString()!, sdk.GetProperty("rollForward").GetString()!);
    }
}
