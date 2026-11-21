using System.Collections.Immutable;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Web.Api;

// Contrat JSON de l'API (plan §5, DEB-Q43) : projections PURES du Domaine et de l'Application, énumérations par leur nom
// exact (comme le stockage, DEB-Q28), ordre des propriétés = ordre de déclaration. Figé à l'octet par FicheVueTests et
// par tests/contrats/fiche-reference.json du ClientApp (Vitest).

public sealed record PropositionVue(string Valeur, string Justification);

public sealed record RubriqueVue(
    string Code,
    string Libelle,
    string Bloc,
    bool Obligatoire,
    string? Valeur,
    string Origine,
    PropositionVue? Proposition);

public sealed record EnTeteVue(string Demandeur, string Bureau, string Valideur, DateTimeOffset CreeeLe);

public sealed record DecisionVue(string Type, string Auteur, DateTimeOffset Le, string? Commentaire);

/// <summary>
/// Fiche affichée : rubriques dans l'ordre du modèle ; <c>Obligatoires</c>, <c>Remplies</c> (obligatoires avec une
/// valeur retenue), <c>Complete</c>, <c>Modifiable</c> et <c>ProchaineRubrique</c> calculés par le Domaine, jamais par
/// le front.
/// </summary>
public sealed record FicheVue(
    Guid Id,
    string? Numero,
    string Statut,
    EnTeteVue EnTete,
    ImmutableArray<RubriqueVue> Rubriques,
    int Obligatoires,
    int Remplies,
    bool Complete,
    bool Modifiable,
    string? ProchaineRubrique,
    DateTimeOffset? ConfirmeeLe,
    ImmutableArray<DecisionVue> Decisions)
{
    /// <summary>Nombre de rubriques obligatoires du catalogue (DEB-Q4).</summary>
    private static readonly int NombreObligatoires = Catalogue.Toutes.Count(definition => definition.Obligatoire);

    public static FicheVue De(FicheBesoin fiche)
    {
        var manquantes = Completude.Manquantes(fiche).Length;
        return new FicheVue(
            fiche.Id,
            fiche.Numero,
            fiche.Statut.ToString(),
            new EnTeteVue(fiche.EnTete.Demandeur, fiche.EnTete.Bureau, fiche.EnTete.Valideur, fiche.EnTete.CreeeLe),
            [.. Catalogue.Toutes.Select(definition => VueRubrique(definition, fiche.ChampDe(definition.Rubrique)))],
            NombreObligatoires,
            NombreObligatoires - manquantes,
            manquantes == 0,
            fiche.EstModifiable,
            Dialogue.ProchaineRubrique(fiche) is { } prochaine ? Catalogue.Definition(prochaine).Code : null,
            fiche.ConfirmeeLe,
            [.. fiche.Decisions.Select(decision =>
                new DecisionVue(decision.Type.ToString(), decision.Auteur, decision.Le, decision.Commentaire))]);
    }

    private static RubriqueVue VueRubrique(DefinitionRubrique definition, Champ champ) =>
        new(
            definition.Code,
            definition.Libelle,
            definition.Bloc.ToString(),
            definition.Obligatoire,
            champ.Valeur,
            champ.Origine.ToString(),
            champ.PropositionEnAttente is { } proposition ? new PropositionVue(proposition.Valeur, proposition.Justification) : null);
}

public sealed record EchangeVue(string Auteur, string Texte, DateTimeOffset Le);

/// <summary>
/// Étape du cycle de vie (DEB-Q77) : <c>Etape</c> calculée par le Domaine (<see cref="Transitions.EtapeDe"/>), jamais par
/// le front ; <c>Avant</c> <c>null</c> à la création.
/// </summary>
public sealed record TransitionVue(string Etape, string? Avant, string Apres, string Acteur, DateTimeOffset Le)
{
    public static TransitionVue De(Transition transition) =>
        new(
            Transitions.EtapeDe(transition).ToString(),
            transition.Avant?.ToString(),
            transition.Apres.ToString(),
            transition.Acteur,
            transition.Le);
}

/// <summary>
/// <c>GET api/fiches/{id}</c> : fiche, historique du dialogue (DEB-Q21) et cycle de vie, dans l'ordre (DEB-Q77 ; le champ
/// <c>cycle</c> vient en dernier : le début du contrat est inchangé).
/// </summary>
public sealed record ConsultationVue(FicheVue Fiche, ImmutableArray<EchangeVue> Historique, ImmutableArray<TransitionVue> Cycle)
{
    public static ConsultationVue De(FicheConsultee consultee) =>
        new(
            FicheVue.De(consultee.Fiche),
            [.. consultee.Historique.Select(echange => new EchangeVue(echange.Auteur.ToString(), echange.Texte, echange.Le))],
            [.. consultee.Transitions.Select(transition => TransitionVue.De(transition))]);
}

public sealed record EcartVue(int Position, string Code, string Motif);

/// <summary><c>POST api/fiches/{id}/messages</c> : <c>{ message, horsPerimetre, ecartees, fiche }</c> (DEB-Q22, DEB-Q23).</summary>
public sealed record TourVue(string Message, bool HorsPerimetre, ImmutableArray<EcartVue> Ecartees, FicheVue Fiche)
{
    public static TourVue De(TourDialogue tour) =>
        new(
            tour.Message,
            tour.HorsPerimetre,
            [.. tour.Ecartees.Select(ecart => new EcartVue(ecart.Position, ecart.Code, ecart.Motif.ToString()))],
            FicheVue.De(tour.Fiche));
}

/// <summary>Ligne de la liste (<c>GET api/fiches</c>, page Liste au lot F3).</summary>
public sealed record ResumeVue(
    Guid Id,
    string? Numero,
    string? Intitule,
    string Demandeur,
    string Statut,
    DateTimeOffset CreeeLe,
    DateTimeOffset? ConfirmeeLe)
{
    public static ResumeVue De(ResumeFiche resume) =>
        new(resume.Id, resume.Numero, resume.Intitule, resume.Demandeur, resume.Statut.ToString(), resume.CreeeLe, resume.ConfirmeeLe);
}

/// <summary>Agent du sélecteur de l'en-tête (<c>GET api/agents</c>, DEB-Q6).</summary>
public sealed record AgentVue(string Identifiant, string Nom, string Bureau, string Valideur)
{
    public static AgentVue De(Agent agent) => new(agent.Identifiant, agent.Nom, agent.Bureau, agent.Valideur);
}

/// <summary>Corps d'un refus métier ou d'une panne (DEB-Q42).</summary>
public sealed record ErreurVue(string Code, string Message);
