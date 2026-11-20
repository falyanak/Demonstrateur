namespace ExpressionBesoin.Domain.Fiches;

/// <summary>
/// Cycle des statuts (plan §3) : Brouillon → Soumise → Validee | ACompleter | Refusee ; ACompleter → Brouillon.
/// Une fonction par transition ; l'horloge entre en paramètre.
/// </summary>
public static class Cycle
{
    /// <summary>
    /// Soumission (DEB-Q5, DEB-Q17) : fiche Brouillon, confirmation cochée, complète, sans proposition en attente.
    /// Le numéro n'est pris que si la fiche n'en a pas encore : une nouvelle soumission garde le sien.
    /// </summary>
    public static Resultat<FicheBesoin> Soumettre(FicheBesoin fiche, bool confirme, string numeroSiNouveau, DateTimeOffset maintenant)
    {
        if (fiche.Statut != Statut.Brouillon)
        {
            return TransitionInterdite("Soumission", fiche);
        }

        if (!confirme)
        {
            return new Refus("soumission.confirmationAbsente", "L'agent doit confirmer le contenu de la fiche avant de la soumettre.");
        }

        var manquantes = Completude.Manquantes(fiche);
        if (!manquantes.IsEmpty)
        {
            return new Refus("soumission.incomplete", "Rubriques obligatoires à renseigner : " + Libelles(manquantes) + ".");
        }

        var enAttente = Completude.PropositionsEnAttente(fiche);
        if (!enAttente.IsEmpty)
        {
            return new Refus("soumission.propositionsEnAttente", "Propositions à accepter ou rejeter : " + Libelles(enAttente) + ".");
        }

        return Resultat.Succes(fiche with
        {
            Numero = fiche.Numero ?? numeroSiNouveau,
            Statut = Statut.Soumise,
            ConfirmeeLe = maintenant,
        });
    }

    /// <summary>Décision simulée du chef de service (DEB-Q3) : commentaire obligatoire sauf pour une validation.</summary>
    public static Resultat<FicheBesoin> Decider(
        FicheBesoin fiche,
        TypeDecision type,
        string auteur,
        string? commentaire,
        DateTimeOffset maintenant)
    {
        if (fiche.Statut != Statut.Soumise)
        {
            return TransitionInterdite("Décision", fiche);
        }

        var nettoye = commentaire?.Trim();
        if (string.IsNullOrEmpty(nettoye))
        {
            if (type != TypeDecision.Valider)
            {
                return new Refus("decision.commentaireObligatoire", "Un commentaire est obligatoire pour demander des compléments ou refuser.");
            }

            nettoye = null;
        }

        var statut = type switch
        {
            TypeDecision.Valider => Statut.Validee,
            TypeDecision.DemanderComplements => Statut.ACompleter,
            TypeDecision.Refuser => Statut.Refusee,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Type de décision inconnu."),
        };

        return Resultat.Succes(fiche with
        {
            Statut = statut,
            Decisions = fiche.Decisions.Ajouter(new Decision(type, auteur, maintenant, nettoye)),
        });
    }

    /// <summary>Reprise d'une fiche à compléter : retour au Brouillon, confirmation à refaire (DEB-Q16).</summary>
    public static Resultat<FicheBesoin> Reprendre(FicheBesoin fiche)
    {
        if (fiche.Statut != Statut.ACompleter)
        {
            return TransitionInterdite("Reprise", fiche);
        }

        return Resultat.Succes(fiche with { Statut = Statut.Brouillon, ConfirmeeLe = null });
    }

    private static Refus TransitionInterdite(string action, FicheBesoin fiche) =>
        new("cycle.transitionInterdite", $"{action} impossible au statut {fiche.Statut}.");

    private static string Libelles(IEnumerable<Rubrique> rubriques) =>
        string.Join(", ", rubriques.Select(rubrique => Catalogue.Definition(rubrique).Libelle));
}
