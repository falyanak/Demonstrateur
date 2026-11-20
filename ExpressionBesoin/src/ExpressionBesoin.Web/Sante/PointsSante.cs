namespace ExpressionBesoin.Web.Sante;

/// <summary>Adaptateur mince : lit l'horloge et les deux singletons, délègue à <see cref="DiagnosticSante"/>.</summary>
public static class PointsSante
{
    public const string Route = "/api/sante";

    public static IEndpointRouteBuilder MapSante(this IEndpointRouteBuilder routes)
    {
        routes.MapGet(Route, (TimeProvider horloge, Demarrage demarrage, VersionApplication version) =>
            DiagnosticSante.Decrire(version.Valeur, demarrage.Instant, horloge.GetUtcNow()));
        return routes;
    }
}
