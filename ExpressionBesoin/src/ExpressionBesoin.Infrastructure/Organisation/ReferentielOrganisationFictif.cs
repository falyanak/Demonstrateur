using System.Collections.Frozen;
using System.Collections.Immutable;
using ExpressionBesoin.Application.Fiches;

namespace ExpressionBesoin.Infrastructure.Organisation;

/// <summary>
/// Référentiel FICTIF agent → bureau → chef de service (plan §3, DEB-Q6, DEB-Q30) : données en code, immuables, sans
/// table (rien ne change en V1). Noms inventés ; aucune donnée réelle.
/// </summary>
public sealed class ReferentielOrganisationFictif : IReferentielOrganisation
{
    public const string BureauCommerceElectronique = "Bureau 1A – Commerce électronique";
    public const string BureauProduitsIndustriels = "Bureau 2B – Produits industriels";

    /// <summary>Agents du sélecteur de l'en-tête, dans l'ordre d'affichage.</summary>
    public static ImmutableArray<Agent> Agents { get; } =
    [
        new("camille.martin", "Camille Martin", BureauCommerceElectronique, "Dominique Bernard"),
        new("louis.petit", "Louis Petit", BureauCommerceElectronique, "Dominique Bernard"),
        new("sarah.durand", "Sarah Durand", BureauProduitsIndustriels, "Claude Moreau"),
        new("hugo.leroy", "Hugo Leroy", BureauProduitsIndustriels, "Claude Moreau"),
    ];

    // Identifiant exact, casse comprise (en-tête simple, jamais une autorisation : plan §5).
    private static readonly FrozenDictionary<string, Agent> ParIdentifiant =
        Agents.ToFrozenDictionary(agent => agent.Identifiant, StringComparer.Ordinal);

    public ValueTask<Agent?> TrouverAgent(string identifiant, CancellationToken annulation) =>
        new(ParIdentifiant.GetValueOrDefault(identifiant));

    public ValueTask<ImmutableArray<Agent>> ListerAgents(CancellationToken annulation) => new(Agents);
}
