using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Application.Fiches;

/// <summary>Agent du référentiel fictif : nom, bureau et chef de service qui valide ses fiches.</summary>
public sealed record Agent(string Identifiant, string Nom, string Bureau, string Valideur);

/// <summary>Auteur d'un échange du dialogue.</summary>
public enum AuteurEchange : byte
{
    Agent,
    IA,
}

/// <summary>Échange du dialogue, persisté à part de la fiche (DEB-Q21).</summary>
public sealed record Echange(AuteurEchange Auteur, string Texte, DateTimeOffset Le);

/// <summary>Ligne de la page Liste (plan §6) ; colonnes dénormalisées du dépôt (plan §4, performance).</summary>
public sealed record ResumeFiche(
    Guid Id,
    string? Numero,
    string? Intitule,
    string Demandeur,
    Statut Statut,
    DateTimeOffset CreeeLe,
    DateTimeOffset? ConfirmeeLe)
{
    /// <summary>Projection d'une fiche : seule source des colonnes de liste, au dépôt comme aux tests.</summary>
    public static ResumeFiche De(FicheBesoin fiche) =>
        new(
            fiche.Id,
            fiche.Numero,
            fiche.ChampDe(Rubrique.Intitule).Valeur,
            fiche.EnTete.Demandeur,
            fiche.Statut,
            fiche.EnTete.CreeeLe,
            fiche.ConfirmeeLe);
}

/// <summary>État d'une rubrique transmis à l'IA : code du contrat, libellé, valeur retenue, proposition en attente.</summary>
public sealed record EtatRubrique(string Code, string Libelle, bool Obligatoire, string? Valeur, string? PropositionEnAttente);

/// <summary>
/// Partie VARIABLE d'un tour de dialogue (plan §4) : message de l'agent, historique, état de la fiche, rubriques
/// obligatoires manquantes, prochaine rubrique à questionner (<c>null</c> s'il n'y en a plus). La partie fixe du prompt
/// (consignes, modèle de fiche) appartient à l'adaptateur (A1).
/// </summary>
public sealed record ContexteTour(
    string Message,
    ListeImmuable<Echange> Historique,
    ListeImmuable<EtatRubrique> Rubriques,
    ListeImmuable<string> Manquantes,
    string? ProchaineRubrique);

/// <summary>Fiche et historique de son dialogue, pour l'affichage et la reprise (DEB-Q21).</summary>
public sealed record FicheConsultee(FicheBesoin Fiche, ListeImmuable<Echange> Historique);

/// <summary>
/// Issue d'un tour (plan §5 : <c>{ message, fiche }</c>) : réponse de l'IA, indicateur hors périmètre (DEB-Q23),
/// propositions écartées à la lecture (DEB-Q19), fiche après fusion.
/// </summary>
public sealed record TourDialogue(
    string Message,
    bool HorsPerimetre,
    ListeImmuable<PropositionEcartee> Ecartees,
    FicheBesoin Fiche);

/// <summary>Fiche validée au format .docx.</summary>
public sealed record DocumentFiche(string NomFichier, byte[] Contenu);
