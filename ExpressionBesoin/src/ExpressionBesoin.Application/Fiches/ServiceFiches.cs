using System.Collections.Immutable;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Application.Fiches;

/// <summary>
/// Cas d'usage des fiches, un par route du plan §5 : coquille lire → décider → écrire, sans décision (elles sont dans
/// <see cref="DecisionsFiches"/> et le Domaine). <paramref name="fuseau"/> : fuseau de l'année du numéro (DEB-Q24) et des
/// dates du document (DEB-Q36) ;
/// <paramref name="nouvelIdentifiant"/> : identifiant d'une fiche créée. Une exception d'un port (panne technique) se
/// propage : rien n'est écrit après elle (DEB-Q22).
/// </summary>
public sealed class ServiceFiches(
    IDepotFiches depot,
    IAssistantIA assistant,
    IGenerateurDocument generateur,
    IReferentielOrganisation referentiel,
    TimeProvider horloge,
    TimeZoneInfo fuseau,
    Func<Guid> nouvelIdentifiant)
{
    public ValueTask<ImmutableArray<Agent>> ListerAgents(CancellationToken annulation) => referentiel.ListerAgents(annulation);

    public ValueTask<ImmutableArray<ResumeFiche>> Lister(CancellationToken annulation) => depot.Lister(annulation);

    public async ValueTask<Resultat<FicheBesoin>> CreerBrouillon(string identifiantAgent, CancellationToken annulation)
    {
        var agent = await referentiel.TrouverAgent(identifiantAgent, annulation);
        return await Ecrire(
            DecisionsFiches.CreerBrouillon(nouvelIdentifiant(), agent, identifiantAgent, horloge.GetUtcNow()),
            annulation);
    }

    public async ValueTask<Resultat<FicheConsultee>> Consulter(Guid id, CancellationToken annulation)
    {
        var fiche = await depot.Lire(id, annulation);
        var historique = await depot.LireHistorique(id, annulation);
        return DecisionsFiches.Consulter(fiche, historique, id);
    }

    /// <summary>Tour de dialogue : lire, préparer (pur), appeler l'IA, conclure (pur), écrire fiche et échanges ensemble.</summary>
    public async ValueTask<Resultat<TourDialogue>> EnvoyerMessage(Guid id, string? message, CancellationToken annulation)
    {
        var fiche = await depot.Lire(id, annulation);
        var historique = await depot.LireHistorique(id, annulation);
        var debut = horloge.GetUtcNow();
        var (tour, refus) = DecisionsFiches.PreparerTour(fiche, historique, id, message).Ouvrir();
        if (tour is null)
        {
            return refus!;
        }

        var texte = await assistant.Repondre(tour.Contexte, annulation);
        var (issue, refusIA) = DecisionsFiches.ConclureTour(tour, texte, debut, horloge.GetUtcNow()).Ouvrir();
        if (issue is null)
        {
            return refusIA!;
        }

        await depot.Enregistrer(issue.Fiche, issue.Echanges, annulation);
        return Resultat.Succes(issue.Tour);
    }

    public ValueTask<Resultat<FicheBesoin>> ModifierChamp(Guid id, string rubrique, string? valeur, CancellationToken annulation) =>
        Modifier(
            id,
            fiche => DecisionsFiches.TrouverRubrique(rubrique).Bind(trouvee => Fiche.ModifierChamp(fiche, trouvee, valeur)),
            annulation);

    public ValueTask<Resultat<FicheBesoin>> AccepterProposition(Guid id, string rubrique, CancellationToken annulation) =>
        Modifier(
            id,
            fiche => DecisionsFiches.TrouverRubrique(rubrique).Bind(trouvee => Fiche.AccepterProposition(fiche, trouvee)),
            annulation);

    public ValueTask<Resultat<FicheBesoin>> RejeterProposition(Guid id, string rubrique, CancellationToken annulation) =>
        Modifier(
            id,
            fiche => DecisionsFiches.TrouverRubrique(rubrique).Bind(trouvee => Fiche.RejeterProposition(fiche, trouvee)),
            annulation);

    public async ValueTask<Resultat<FicheBesoin>> Soumettre(Guid id, bool confirme, CancellationToken annulation)
    {
        var dernier = await depot.DernierNumero(annulation);
        return await Modifier(
            id,
            fiche => DecisionsFiches.Soumettre(fiche, confirme, dernier, horloge.GetUtcNow(), fuseau),
            annulation);
    }

    public ValueTask<Resultat<FicheBesoin>> Decider(Guid id, TypeDecision decision, string? commentaire, CancellationToken annulation) =>
        Modifier(id, fiche => DecisionsFiches.Decider(fiche, decision, commentaire, horloge.GetUtcNow()), annulation);

    public ValueTask<Resultat<FicheBesoin>> Reprendre(Guid id, CancellationToken annulation) =>
        Modifier(id, fiche => Cycle.Reprendre(fiche), annulation);

    public async ValueTask<Resultat<DocumentFiche>> Telecharger(Guid id, CancellationToken annulation)
    {
        var lue = await depot.Lire(id, annulation);
        var (fiche, refus) = DecisionsFiches.Existante(lue, id).Bind(trouvee => DecisionsFiches.Documentable(trouvee)).Ouvrir();
        if (fiche is null)
        {
            return refus!;
        }

        var octets = await generateur.Generer(ContenuDocument.Construire(fiche, fuseau), annulation);
        return Resultat.Succes(new DocumentFiche(DecisionsFiches.NomDocument(fiche), octets));
    }

    private async ValueTask<Resultat<FicheBesoin>> Modifier(
        Guid id,
        Func<FicheBesoin, Resultat<FicheBesoin>> decider,
        CancellationToken annulation)
    {
        var fiche = await depot.Lire(id, annulation);
        return await Ecrire(DecisionsFiches.Existante(fiche, id).Bind(decider), annulation);
    }

    /// <summary>Écrit la fiche décidée, sans échange ; un refus n'écrit rien.</summary>
    private async ValueTask<Resultat<FicheBesoin>> Ecrire(Resultat<FicheBesoin> decision, CancellationToken annulation)
    {
        var (fiche, _) = decision.Ouvrir();
        if (fiche is not null)
        {
            await depot.Enregistrer(fiche, ListeImmuable<Echange>.Vide, annulation);
        }

        return decision;
    }
}
