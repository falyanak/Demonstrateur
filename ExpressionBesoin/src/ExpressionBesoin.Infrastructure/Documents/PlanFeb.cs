using System.Collections.Immutable;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Infrastructure.Documents;

/// <summary>
/// Forme d'un paragraphe du modèle FEB (DEB-Q35). <see cref="Libelle"/> : premier run = le marqueur exact (libellé en
/// gras), le texte va dans le run suivant. <see cref="Ligne"/> : un run qui COMMENCE par le marqueur, remplacé en entier.
/// </summary>
internal enum Forme : byte
{
    Libelle,
    Ligne,
}

/// <summary>Paragraphe du modèle, repéré par le début de son premier run.</summary>
internal sealed record Marqueur(string Texte, Forme Forme);

/// <summary>Texte à placer dans le paragraphe repéré par <paramref name="Marqueur"/>.</summary>
internal sealed record Remplacement(Marqueur Marqueur, string Texte);

/// <summary>
/// Plan de placement du contenu dans le modèle FEB (DEB-Q35) : fonctions pures. Les libellés sont ceux du modèle,
/// caractère pour caractère (apostrophe typographique comprise) ; le gabarit est vérifié contre eux à la construction
/// du générateur (<see cref="GabaritFeb.Anomalies"/>).
/// </summary>
internal static class PlanFeb
{
    public static readonly Marqueur Numero = new("N° de demande : ", Forme.Libelle);
    public static readonly Marqueur Demandeur = new("Demande exprimée par : ", Forme.Ligne);
    public static readonly Marqueur DateDemande = new("Date de la demande : ", Forme.Ligne);
    public static readonly Marqueur Bureau = new("Bureau : ", Forme.Ligne);
    public static readonly Marqueur Valideur = new("Valideur : ", Forme.Ligne);
    public static readonly Marqueur Confirmation = new("☐ Je confirme", Forme.Ligne);
    public static readonly Marqueur DateConfirmation = new("Date de confirmation : ", Forme.Ligne);

    /// <summary>Phrase de confirmation, case cochée (☒ au lieu du ☐ du modèle).</summary>
    public const string ConfirmationCochee =
        "☒ Je confirme que cette fiche reflète fidèlement le besoin que je souhaite soumettre à validation.";

    /// <summary>Première cellule de la ligne du circuit qui reçoit la décision (colonne 2) et les observations (colonne 3).</summary>
    public const string LigneChef = "Chef de service";

    /// <summary>Libellé du modèle de chaque rubrique, dans l'ordre de <see cref="Rubrique"/> (index = ordinal).</summary>
    public static ImmutableArray<Marqueur> Rubriques { get; } =
    [
        new("Application ou service numérique concerné : ", Forme.Libelle),
        new("Intitulé court de la demande : ", Forme.Libelle),
        new("Résumé en quelques lignes : ", Forme.Libelle),
        new("Situation actuelle : ", Forme.Libelle),
        new("Difficultés rencontrées : ", Forme.Libelle),
        new("Exemple représentatif : ", Forme.Libelle),
        new("Résultat recherché : ", Forme.Libelle),
        new("Critères de réussite : ", Forme.Libelle),
        new("Solution envisagée par le demandeur : ", Forme.Libelle),
        new("Utilisateurs ou services concernés : ", Forme.Libelle),
        new("Processus métier concerné : ", Forme.Libelle),
        new("Liens avec d’autres outils ou demandes : ", Forme.Libelle),
        new("Bénéfices attendus : ", Forme.Libelle),
        new("Échéance souhaitée et justification : ", Forme.Libelle),
        new("Contraintes identifiées : ", Forme.Libelle),
        new("Documents joints ou références utiles : ", Forme.Libelle),
        new("Points restant à clarifier : ", Forme.Libelle),
    ];

    /// <summary>Tous les marqueurs que le gabarit doit porter, chacun une seule fois.</summary>
    public static ImmutableArray<Marqueur> Marqueurs { get; } =
        [Numero, Demandeur, DateDemande, Bureau, Valideur, .. Rubriques, Confirmation, DateConfirmation];

    /// <summary>
    /// Remplacements d'un contenu : une ligne garde son préfixe ; la case n'est cochée que si la fiche est confirmée
    /// (sinon le paragraphe du modèle reste tel quel).
    /// </summary>
    public static ImmutableArray<Remplacement> Remplacements(ContenuDocument contenu)
    {
        var remplacements = ImmutableArray.CreateBuilder<Remplacement>(Marqueurs.Length);
        remplacements.Add(new(Numero, contenu.Numero));
        remplacements.Add(Ligne(Demandeur, contenu.Demandeur));
        remplacements.Add(Ligne(DateDemande, contenu.DateDemande));
        remplacements.Add(Ligne(Bureau, contenu.Bureau));
        remplacements.Add(Ligne(Valideur, contenu.Valideur));
        for (var index = 0; index < Rubriques.Length; index++)
        {
            remplacements.Add(new(Rubriques[index], contenu.ValeurDe((Rubrique)index)));
        }

        if (contenu.Confirmee)
        {
            remplacements.Add(new(Confirmation, ConfirmationCochee));
        }

        remplacements.Add(Ligne(DateConfirmation, contenu.DateConfirmation));
        return remplacements.DrainToImmutable();
    }

    private static Remplacement Ligne(Marqueur marqueur, string valeur) => new(marqueur, marqueur.Texte + valeur);
}
