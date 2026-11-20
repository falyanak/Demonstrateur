using System.Collections.Immutable;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Application.Fiches;

/// <summary>
/// Dépôt des fiches et de l'historique du dialogue (DEB-Q12, DEB-Q21). Adaptateur SQLite au lot I1.
/// </summary>
public interface IDepotFiches
{
    /// <summary>Fiche d'identifiant <paramref name="id"/>, <c>null</c> si elle n'existe pas.</summary>
    ValueTask<FicheBesoin?> Lire(Guid id, CancellationToken annulation);

    /// <summary>Échanges du dialogue de la fiche, dans l'ordre ; liste vide si aucun (ou fiche inconnue).</summary>
    ValueTask<ListeImmuable<Echange>> LireHistorique(Guid id, CancellationToken annulation);

    /// <summary>
    /// Écrit la fiche (création ou remplacement) ET ajoute <paramref name="echangesAjoutes"/> à son historique, en une
    /// seule transaction (DEB-Q22 : un tour est atomique). Le numéro est unique (index unique, DEB-Q26).
    /// </summary>
    ValueTask Enregistrer(FicheBesoin fiche, ListeImmuable<Echange> echangesAjoutes, CancellationToken annulation);

    /// <summary>Résumés de toutes les fiches (<see cref="ResumeFiche.De"/>), de la plus récente à la plus ancienne.</summary>
    ValueTask<ImmutableArray<ResumeFiche>> Lister(CancellationToken annulation);

    /// <summary>
    /// Plus grand numéro attribué (ordre ordinal, toutes années confondues), <c>null</c> si aucun : l'entrée
    /// <c>dernier</c> de <see cref="Numerotation.Suivant"/>.
    /// </summary>
    ValueTask<string?> DernierNumero(CancellationToken annulation);
}

/// <summary>
/// Assistant conversationnel (plan §2, DEB-D5). Rend le texte BRUT de l'IA, relu par <see cref="ReponseIA.Lire"/> dans
/// les décisions : l'adaptateur ne l'interprète pas. Une panne technique lève une exception (DEB-Q22), jamais un refus.
/// </summary>
public interface IAssistantIA
{
    ValueTask<string> Repondre(ContexteTour contexte, CancellationToken annulation);
}

/// <summary>Production du .docx au modèle FEB depuis une fiche validée (lot I2).</summary>
public interface IGenerateurDocument
{
    ValueTask<byte[]> Generer(FicheBesoin fiche, CancellationToken annulation);
}

/// <summary>Référentiel fictif agent → service → chef de service (plan §3, DEB-Q6).</summary>
public interface IReferentielOrganisation
{
    /// <summary>Agent d'identifiant <paramref name="identifiant"/>, <c>null</c> s'il est inconnu.</summary>
    ValueTask<Agent?> TrouverAgent(string identifiant, CancellationToken annulation);

    /// <summary>Agents proposés par le sélecteur de l'en-tête.</summary>
    ValueTask<ImmutableArray<Agent>> ListerAgents(CancellationToken annulation);
}
