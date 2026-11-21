using System.Collections.Frozen;
using System.Collections.Immutable;

namespace ExpressionBesoin.Domain.Fiches;

/// <summary>Blocs du modèle FEB (plan §3).</summary>
public enum Bloc : byte
{
    Identification,
    Synthese,
    Contexte,
    Objectifs,
    Perimetre,
    Benefices,
    Complements,
}

/// <summary>Rubriques de la fiche, dans l'ordre du modèle FEB ; l'ordinal indexe les champs de la fiche.</summary>
public enum Rubrique : byte
{
    Application,
    Intitule,
    Resume,
    SituationActuelle,
    Difficultes,
    ExempleRepresentatif,
    ResultatRecherche,
    CriteresReussite,
    SolutionEnvisagee,
    UtilisateursConcernes,
    ProcessusMetier,
    Liens,
    BeneficesAttendus,
    Echeance,
    Contraintes,
    DocumentsReferences,
    PointsAClarifier,
}

/// <summary>
/// Définition d'une rubrique : <paramref name="Code"/> camelCase stable (contrat de la réponse de l'IA et de la route
/// <c>{rubrique}</c> de l'API), libellé du modèle FEB, bloc, caractère obligatoire (DEB-Q4), question type posée par
/// l'application quand la rubrique est questionnée (DEB-Q100, DEB-Q101 ; <c>null</c> pour le résumé, proposé à la
/// demande, et les points à clarifier, rédigés par l'IA).
/// </summary>
public sealed record DefinitionRubrique(Rubrique Rubrique, string Code, string Libelle, Bloc Bloc, bool Obligatoire, string? QuestionType);

/// <summary>Catalogue des rubriques (plan §3) : 17 rubriques, dont 10 obligatoires.</summary>
public static class Catalogue
{
    /// <summary>Toutes les rubriques ; <c>Toutes[(int)r].Rubrique == r</c>.</summary>
    public static ImmutableArray<DefinitionRubrique> Toutes { get; } =
    [
        new(Rubrique.Application, "application", "Application ou service concerné", Bloc.Identification, true,
            "Quelle application ou quel équipement est concerné ?"),
        new(Rubrique.Intitule, "intitule", "Intitulé court", Bloc.Identification, true,
            "Comment intituleriez-vous votre demande en quelques mots ?"),
        new(Rubrique.Resume, "resume", "Résumé", Bloc.Synthese, true,
            null),
        new(Rubrique.SituationActuelle, "situationActuelle", "Situation actuelle", Bloc.Contexte, true,
            "Que se passe-t-il aujourd'hui, concrètement ?"),
        new(Rubrique.Difficultes, "difficultes", "Difficultés rencontrées", Bloc.Contexte, true,
            "Qu'est-ce que cela vous empêche de faire dans votre travail ?"),
        new(Rubrique.ExempleRepresentatif, "exempleRepresentatif", "Exemple représentatif", Bloc.Contexte, false,
            "Pouvez-vous citer un cas précis ?"),
        new(Rubrique.ResultatRecherche, "resultatRecherche", "Résultat recherché", Bloc.Objectifs, true,
            "Qu'attendez-vous du service informatique ?"),
        new(Rubrique.CriteresReussite, "criteresReussite", "Critères de réussite", Bloc.Objectifs, true,
            "À quoi verrez-vous que c'est réglé ?"),
        new(Rubrique.SolutionEnvisagee, "solutionEnvisagee", "Solution envisagée", Bloc.Objectifs, false,
            "Avez-vous une solution en tête ?"),
        new(Rubrique.UtilisateursConcernes, "utilisateursConcernes", "Utilisateurs ou services concernés", Bloc.Perimetre, true,
            "Qui est concerné : vous seul, votre bureau, d'autres services ?"),
        new(Rubrique.ProcessusMetier, "processusMetier", "Processus métier", Bloc.Perimetre, true,
            "Pour quelle activité de votre travail en avez-vous besoin ?"),
        new(Rubrique.Liens, "liens", "Liens avec d'autres outils ou demandes", Bloc.Perimetre, false,
            "Cette demande est-elle liée à un autre outil ou à une autre demande ?"),
        new(Rubrique.BeneficesAttendus, "beneficesAttendus", "Bénéfices attendus", Bloc.Benefices, true,
            "Qu'est-ce que cela vous permettra de faire une fois réglé ?"),
        new(Rubrique.Echeance, "echeance", "Échéance et justification", Bloc.Benefices, false,
            "Pour quand en avez-vous besoin, et pourquoi ?"),
        new(Rubrique.Contraintes, "contraintes", "Contraintes identifiées", Bloc.Benefices, false,
            "Y a-t-il des contraintes à respecter ?"),
        new(Rubrique.DocumentsReferences, "documentsReferences", "Documents ou références", Bloc.Complements, false,
            "Avez-vous des documents ou des références à signaler ?"),
        new(Rubrique.PointsAClarifier, "pointsAClarifier", "Points restant à clarifier", Bloc.Complements, false,
            null),
    ];

    /// <summary>
    /// Ordre de questionnement (DEB-Q14) : les rubriques obligatoires dans l'ordre du modèle, le Résumé en dernier
    /// (l'IA le rédige en fin de dialogue).
    /// </summary>
    public static ImmutableArray<Rubrique> OrdreQuestionnement { get; } =
    [
        .. Toutes.Where(definition => definition.Obligatoire && definition.Rubrique != Rubrique.Resume)
            .Select(definition => definition.Rubrique),
        Rubrique.Resume,
    ];

    private static readonly FrozenDictionary<string, Rubrique> ParCode =
        Toutes.ToFrozenDictionary(definition => definition.Code, definition => definition.Rubrique, StringComparer.OrdinalIgnoreCase);

    public static DefinitionRubrique Definition(Rubrique rubrique) => Toutes[(int)rubrique];

    /// <summary>Rubrique d'un code, casse ignorée (DEB-Q19).</summary>
    public static bool EssayerTrouver(string code, out Rubrique rubrique) => ParCode.TryGetValue(code, out rubrique);
}
