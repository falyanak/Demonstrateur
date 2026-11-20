namespace ExpressionBesoin.Domain.Fiches;

/// <summary>Création et modification des champs de la fiche (plan §2, §3 ; DEB-Q15, DEB-Q16). Fonctions pures.</summary>
public static class Fiche
{
    private static readonly ListeImmuable<Champ> ChampsInitiaux =
        ListeImmuable<Champ>.De(Enumerable.Repeat(Champ.Initial, Catalogue.Toutes.Length));

    /// <summary>Brouillon vierge : tous les champs vides, sans numéro, confirmation ni décision.</summary>
    public static FicheBesoin Creer(Guid id, EnTete enTete) =>
        new(id, null, Statut.Brouillon, enTete, ChampsInitiaux, null, ListeImmuable<Decision>.Vide);

    /// <summary>
    /// Fusion des propositions de l'IA : chacune est mise en attente, en remplaçant la précédente ; la valeur retenue
    /// n'est jamais modifiée (une saisie manuelle n'est donc jamais écrasée). Une proposition égale à la valeur retenue
    /// est ignorée. Paramètre en <see cref="ListeImmuable{T}"/> : énumérateur structure, sans allocation ni branche cachée.
    /// </summary>
    public static Resultat<FicheBesoin> AppliquerPropositions(FicheBesoin fiche, ListeImmuable<PropositionRubrique> propositions)
    {
        if (!fiche.EstModifiable)
        {
            return NonModifiable(fiche);
        }

        var champs = fiche.Champs;
        foreach (var proposition in propositions)
        {
            var index = (int)proposition.Rubrique;
            var champ = champs[index];
            if (champ.Valeur != proposition.Proposition.Valeur)
            {
                champs = champs.Remplacer(index, champ with { PropositionEnAttente = proposition.Proposition });
            }
        }

        return Resultat.Succes(fiche with { Champs = champs });
    }

    /// <summary>
    /// Saisie manuelle : valeur nettoyée de ses espaces ; vide → rubrique remise à <see cref="Origine.Vide"/>. La
    /// proposition en attente est gardée, sauf si elle est égale à la valeur saisie (devenue sans objet).
    /// </summary>
    public static Resultat<FicheBesoin> ModifierChamp(FicheBesoin fiche, Rubrique rubrique, string? valeur)
    {
        if (!fiche.EstModifiable)
        {
            return NonModifiable(fiche);
        }

        var champ = fiche.ChampDe(rubrique);
        var nettoyee = valeur?.Trim();
        if (string.IsNullOrEmpty(nettoyee))
        {
            return Resultat.Succes(Remplacer(fiche, rubrique, champ with { Valeur = null, Origine = Origine.Vide }));
        }

        var proposition = champ.PropositionEnAttente;
        if (proposition is not null && proposition.Valeur == nettoyee)
        {
            proposition = null;
        }

        return Resultat.Succes(Remplacer(fiche, rubrique, new Champ(nettoyee, Origine.Manuelle, proposition)));
    }

    /// <summary>Acceptation explicite par l'agent, y compris sur une rubrique saisie à la main (DEB-Q15).</summary>
    public static Resultat<FicheBesoin> AccepterProposition(FicheBesoin fiche, Rubrique rubrique) =>
        PropositionEnAttente(fiche, rubrique)
            .Map(proposition => Remplacer(fiche, rubrique, new Champ(proposition.Valeur, Origine.AccepteeIA, null)));

    /// <summary>Rejet : la proposition disparaît, la valeur retenue reste.</summary>
    public static Resultat<FicheBesoin> RejeterProposition(FicheBesoin fiche, Rubrique rubrique) =>
        PropositionEnAttente(fiche, rubrique)
            .Map(_ => Remplacer(fiche, rubrique, fiche.ChampDe(rubrique) with { PropositionEnAttente = null }));

    private static Resultat<Proposition> PropositionEnAttente(FicheBesoin fiche, Rubrique rubrique)
    {
        if (!fiche.EstModifiable)
        {
            return NonModifiable(fiche);
        }

        var proposition = fiche.ChampDe(rubrique).PropositionEnAttente;
        if (proposition is null)
        {
            return new Refus(
                "proposition.absente",
                $"Aucune proposition en attente pour la rubrique « {Catalogue.Definition(rubrique).Libelle} ».");
        }

        return Resultat.Succes(proposition);
    }

    private static FicheBesoin Remplacer(FicheBesoin fiche, Rubrique rubrique, Champ champ) =>
        fiche with { Champs = fiche.Champs.Remplacer((int)rubrique, champ) };

    private static Refus NonModifiable(FicheBesoin fiche) =>
        new("fiche.nonModifiable", $"La fiche est au statut {fiche.Statut} : seule une fiche Brouillon est modifiable.");
}
