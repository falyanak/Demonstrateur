using System.Globalization;

namespace ExpressionBesoin.Domain.Fiches;

/// <summary>
/// Contenu du .docx au modèle FEB (plan §2 : « contenu du .docx, à partir de la fiche », partie déterministe ; DEB-Q36).
/// Textes prêts à placer : la mise en page appartient à l'adaptateur (Infrastructure). <paramref name="Rubriques"/> : un
/// texte par rubrique, indexé par l'ordinal de <see cref="Rubrique"/> (<see cref="ValeurDe"/>). <paramref name="CreeeLe"/>
/// et <paramref name="ModifieeLe"/> : instants UTC des métadonnées du paquet, tirés de la fiche (octets stables, DEB-Q37).
/// </summary>
public sealed record ContenuDocument(
    string Numero,
    string Demandeur,
    string DateDemande,
    string Bureau,
    string Valideur,
    ListeImmuable<string> Rubriques,
    bool Confirmee,
    string DateConfirmation,
    string DecisionChef,
    string ObservationsChef,
    DateTimeOffset CreeeLe,
    DateTimeOffset ModifieeLe)
{
    public const string NonRenseigne = "Non renseigné";

    public const string NonAttribue = "Non attribué";

    public const string NonConfirmee = "Non confirmée";

    /// <summary>Statut affiché tant qu'aucune décision n'est prise (libellé du modèle FEB).</summary>
    public const string AValider = "À valider";

    public string ValeurDe(Rubrique rubrique) => Rubriques[(int)rubrique];

    /// <summary>
    /// Construit le contenu d'une fiche, quel que soit son statut (l'Application ne demande le document que d'une fiche
    /// validée). Dates au format jj/mm/aaaa dans <paramref name="fuseau"/> (celui de l'année du numéro, DEB-Q24), culture
    /// invariante ; rubrique sans valeur retenue → « Non renseigné » (une proposition en attente n'est pas retenue) ;
    /// décision du chef = la DERNIÈRE de la liste (DEB-Q18).
    /// </summary>
    public static ContenuDocument Construire(FicheBesoin fiche, TimeZoneInfo fuseau)
    {
        var rubriques = ListeImmuable<string>.De(fiche.Champs.Select(champ => champ.Valeur ?? NonRenseigne));
        var derniere = fiche.Decisions.Count == 0 ? null : fiche.Decisions[^1];
        return new ContenuDocument(
            fiche.Numero ?? NonAttribue,
            fiche.EnTete.Demandeur,
            Date(fiche.EnTete.CreeeLe, fuseau),
            fiche.EnTete.Bureau,
            fiche.EnTete.Valideur,
            rubriques,
            fiche.ConfirmeeLe is not null,
            fiche.ConfirmeeLe is { } confirmeeLe ? Date(confirmeeLe, fuseau) : NonConfirmee,
            derniere is null ? AValider : Libelle(derniere.Type),
            derniere is null ? "" : Observations(derniere, fuseau),
            fiche.EnTete.CreeeLe.ToUniversalTime(),
            (derniere?.Le ?? fiche.ConfirmeeLe ?? fiche.EnTete.CreeeLe).ToUniversalTime());
    }

    /// <summary>Libellé d'une décision, tel que le modèle FEB l'écrit (« À valider / Validée / À compléter / Refusée »).</summary>
    public static string Libelle(TypeDecision type) => type switch
    {
        TypeDecision.Valider => "Validée",
        TypeDecision.DemanderComplements => "À compléter",
        TypeDecision.Refuser => "Refusée",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Type de décision inconnu."),
    };

    private static string Observations(Decision decision, TimeZoneInfo fuseau)
    {
        var entete = decision.Auteur + ", le " + Date(decision.Le, fuseau);
        return decision.Commentaire is null ? entete : entete + " : " + decision.Commentaire;
    }

    private static string Date(DateTimeOffset instant, TimeZoneInfo fuseau) =>
        TimeZoneInfo.ConvertTime(instant, fuseau).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
