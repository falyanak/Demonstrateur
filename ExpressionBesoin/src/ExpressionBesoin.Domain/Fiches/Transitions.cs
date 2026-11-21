namespace ExpressionBesoin.Domain.Fiches;

/// <summary>Étape du cycle de vie d'une fiche : calculée depuis les deux statuts d'une transition, jamais stockée (DEB-Q74).</summary>
public enum Etape : byte
{
    Creation,
    Soumission,
    Validation,
    DemandeComplements,
    Refus,
    Reprise,
}

/// <summary>
/// Changement de statut d'une fiche (DEB-Q74) : statut avant (<c>null</c> à la création), statut après, acteur, instant.
/// Aucun texte libre (DEB-Q66) : le commentaire d'une décision reste dans <see cref="FicheBesoin.Decisions"/>.
/// </summary>
public sealed record Transition(Statut? Avant, Statut Apres, string Acteur, DateTimeOffset Le);

/// <summary>
/// Cycle de vie (DEB-Q73 à DEB-Q75) : fonctions pures qui déduisent la transition d'une écriture de fiche et l'étape d'une
/// transition. Les fonctions de <see cref="Cycle"/> restent inchangées : la transition se lit sur l'état avant et après.
/// </summary>
public static class Transitions
{
    /// <summary>
    /// Transition entre <paramref name="avant"/> (<c>null</c> : fiche créée) et <paramref name="apres"/>, datée de
    /// <paramref name="maintenant"/> ; <c>null</c> si le statut n'a pas changé (tour de dialogue, saisie, proposition).
    /// Acteur : le valideur de l'en-tête pour une décision (DEB-Q25), le demandeur sinon.
    /// </summary>
    public static Transition? Entre(FicheBesoin? avant, FicheBesoin apres, DateTimeOffset maintenant) =>
        avant is not null && avant.Statut == apres.Statut
            ? null
            : new Transition(avant?.Statut, apres.Statut, Acteur(apres), maintenant);

    /// <summary>
    /// Étape d'une transition du cycle (plan §3). Toute autre paire de statuts lève : elle ne peut venir que d'une donnée
    /// corrompue, que le schéma SQLite refuse déjà à l'écriture (contrainte de la table <c>Transitions</c>).
    /// </summary>
    public static Etape EtapeDe(Transition transition) =>
        (transition.Avant, transition.Apres) switch
        {
            (null, Statut.Brouillon) => Etape.Creation,
            (Statut.Brouillon, Statut.Soumise) => Etape.Soumission,
            (Statut.Soumise, Statut.Validee) => Etape.Validation,
            (Statut.Soumise, Statut.ACompleter) => Etape.DemandeComplements,
            (Statut.Soumise, Statut.Refusee) => Etape.Refus,
            (Statut.ACompleter, Statut.Brouillon) => Etape.Reprise,
            _ => throw new InvalidOperationException(
                $"Transition hors du cycle : {transition.Avant?.ToString() ?? "aucun"} → {transition.Apres}."),
        };

    private static string Acteur(FicheBesoin apres) =>
        apres.Statut is Statut.Validee or Statut.ACompleter or Statut.Refusee ? apres.EnTete.Valideur : apres.EnTete.Demandeur;
}
