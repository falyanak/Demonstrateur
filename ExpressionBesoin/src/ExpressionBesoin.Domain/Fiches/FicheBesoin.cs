namespace ExpressionBesoin.Domain.Fiches;

/// <summary>Origine de la valeur d'un champ (DEB-Q13 : une proposition vit toujours dans la proposition en attente).</summary>
public enum Origine : byte
{
    Vide,
    AccepteeIA,
    Manuelle,
}

/// <summary>Statut de la fiche (plan §3).</summary>
public enum Statut : byte
{
    Brouillon,
    Soumise,
    Validee,
    ACompleter,
    Refusee,
}

/// <summary>Décision simulée du chef de service (DEB-Q3).</summary>
public enum TypeDecision : byte
{
    Valider,
    DemanderComplements,
    Refuser,
}

/// <summary>Valeur proposée par l'IA, avec sa justification.</summary>
public sealed record Proposition(string Valeur, string Justification);

/// <summary>Champ d'une rubrique : valeur retenue, son origine, au plus une proposition en attente.</summary>
public sealed record Champ(string? Valeur, Origine Origine, Proposition? PropositionEnAttente)
{
    public static Champ Initial { get; } = new(null, Origine.Vide, null);

    /// <summary>État affiché « Proposée » : aucune valeur retenue, une proposition en attente (DEB-Q13).</summary>
    public bool EstProposee => Origine == Origine.Vide && PropositionEnAttente is not null;
}

/// <summary>En-tête renseigné par le système (référentiel fictif agent → service → chef de service).</summary>
public sealed record EnTete(string Demandeur, string Bureau, string Valideur, DateTimeOffset CreeeLe);

/// <summary>Décision du circuit ; la liste des décisions ne fait que s'allonger (DEB-Q18).</summary>
public sealed record Decision(TypeDecision Type, string Auteur, DateTimeOffset Le, string? Commentaire);

/// <summary>
/// Fiche d'expression de besoin, immuable. <paramref name="Champs"/> : un champ par rubrique, indexé par l'ordinal de
/// <see cref="Rubrique"/> (<see cref="ChampDe"/>). <paramref name="Numero"/> : attribué à la première soumission.
/// </summary>
public sealed record FicheBesoin(
    Guid Id,
    string? Numero,
    Statut Statut,
    EnTete EnTete,
    ListeImmuable<Champ> Champs,
    DateTimeOffset? ConfirmeeLe,
    ListeImmuable<Decision> Decisions)
{
    public Champ ChampDe(Rubrique rubrique) => Champs[(int)rubrique];

    /// <summary>Seule une fiche au statut Brouillon se modifie (DEB-Q16).</summary>
    public bool EstModifiable => Statut == Statut.Brouillon;
}
