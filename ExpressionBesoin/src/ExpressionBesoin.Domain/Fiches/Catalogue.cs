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
/// <c>{rubrique}</c> de l'API), libellé du modèle FEB, bloc, caractère obligatoire (DEB-Q4).
/// </summary>
public sealed record DefinitionRubrique(Rubrique Rubrique, string Code, string Libelle, Bloc Bloc, bool Obligatoire);

/// <summary>Catalogue des rubriques (plan §3) : 17 rubriques, dont 10 obligatoires.</summary>
public static class Catalogue
{
    /// <summary>Toutes les rubriques ; <c>Toutes[(int)r].Rubrique == r</c>.</summary>
    public static ImmutableArray<DefinitionRubrique> Toutes { get; } =
    [
        new(Rubrique.Application, "application", "Application ou service concerné", Bloc.Identification, true),
        new(Rubrique.Intitule, "intitule", "Intitulé court", Bloc.Identification, true),
        new(Rubrique.Resume, "resume", "Résumé", Bloc.Synthese, true),
        new(Rubrique.SituationActuelle, "situationActuelle", "Situation actuelle", Bloc.Contexte, true),
        new(Rubrique.Difficultes, "difficultes", "Difficultés rencontrées", Bloc.Contexte, true),
        new(Rubrique.ExempleRepresentatif, "exempleRepresentatif", "Exemple représentatif", Bloc.Contexte, false),
        new(Rubrique.ResultatRecherche, "resultatRecherche", "Résultat recherché", Bloc.Objectifs, true),
        new(Rubrique.CriteresReussite, "criteresReussite", "Critères de réussite", Bloc.Objectifs, true),
        new(Rubrique.SolutionEnvisagee, "solutionEnvisagee", "Solution envisagée", Bloc.Objectifs, false),
        new(Rubrique.UtilisateursConcernes, "utilisateursConcernes", "Utilisateurs ou services concernés", Bloc.Perimetre, true),
        new(Rubrique.ProcessusMetier, "processusMetier", "Processus métier", Bloc.Perimetre, true),
        new(Rubrique.Liens, "liens", "Liens avec d'autres outils ou demandes", Bloc.Perimetre, false),
        new(Rubrique.BeneficesAttendus, "beneficesAttendus", "Bénéfices attendus", Bloc.Benefices, true),
        new(Rubrique.Echeance, "echeance", "Échéance et justification", Bloc.Benefices, false),
        new(Rubrique.Contraintes, "contraintes", "Contraintes identifiées", Bloc.Benefices, false),
        new(Rubrique.DocumentsReferences, "documentsReferences", "Documents ou références", Bloc.Complements, false),
        new(Rubrique.PointsAClarifier, "pointsAClarifier", "Points restant à clarifier", Bloc.Complements, false),
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
