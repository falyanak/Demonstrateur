namespace ExpressionBesoin.Architecture.Tests;

/// <summary>Règle de dépendance des couches (modèle générique du harnais §4.2) : un projet de src, une règle.</summary>
internal static class ReglesSolution
{
    public const string Domain = "ExpressionBesoin.Domain";
    public const string Application = "ExpressionBesoin.Application";
    public const string Infrastructure = "ExpressionBesoin.Infrastructure";
    public const string Web = "ExpressionBesoin.Web";

    public static readonly IReadOnlyDictionary<string, RegleDependance> Regles =
        new Dictionary<string, RegleDependance>(StringComparer.Ordinal)
        {
            [Domain] = new([], ReferencesExternesPermises: false),
            [Application] = new([Domain], ReferencesExternesPermises: false),
            [Infrastructure] = new([Domain, Application], ReferencesExternesPermises: true),
            [Web] = new([Domain, Application, Infrastructure], ReferencesExternesPermises: true),
        };
}
