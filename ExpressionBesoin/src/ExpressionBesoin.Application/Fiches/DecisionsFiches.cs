using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Application.Fiches;

/// <summary>Tour préparé : fiche modifiable et contexte transmis à l'IA.</summary>
internal sealed record TourPrepare(FicheBesoin Fiche, ContexteTour Contexte);

/// <summary>Issue d'un tour à écrire : fiche fusionnée, échanges à ajouter à l'historique, réponse au client.</summary>
internal sealed record IssueTour(FicheBesoin Fiche, ListeImmuable<Echange> Echanges, TourDialogue Tour);

/// <summary>
/// Décisions des cas d'usage (« décider » de lire → décider → écrire) : fonctions pures, horloge, fuseau et
/// identifiants en paramètre. Les règles métier restent dans le Domaine ; ici, seulement ce qui relie ses fonctions aux
/// données lues (DEB-Q21 à DEB-Q27).
/// </summary>
internal static class DecisionsFiches
{
    public static Resultat<FicheBesoin> Existante(FicheBesoin? fiche, Guid id) =>
        fiche is null
            ? new Refus("fiche.introuvable", $"Aucune fiche d'identifiant {id}.")
            : Resultat.Succes(fiche);

    /// <summary>Rubrique d'un code de route, casse ignorée (DEB-Q27).</summary>
    public static Resultat<Rubrique> TrouverRubrique(string code) =>
        Catalogue.EssayerTrouver(code, out var rubrique)
            ? Resultat.Succes(rubrique)
            : new Refus("rubrique.inconnue", $"Rubrique inconnue : « {code} ».");

    /// <summary>Brouillon vierge, en-tête tiré du référentiel ; date de création = <paramref name="maintenant"/>.</summary>
    public static Resultat<FicheBesoin> CreerBrouillon(Guid id, Agent? agent, string identifiantAgent, DateTimeOffset maintenant) =>
        agent is null
            ? new Refus("agent.inconnu", $"Agent inconnu du référentiel : « {identifiantAgent} ».")
            : Resultat.Succes(Fiche.Creer(id, new EnTete(agent.Nom, agent.Bureau, agent.Valideur, maintenant)));

    public static Resultat<FicheConsultee> Consulter(
        FicheBesoin? fiche,
        ListeImmuable<Echange> historique,
        ListeImmuable<Transition> transitions,
        Guid id) =>
        Existante(fiche, id).Map(trouvee => new FicheConsultee(trouvee, historique, transitions));

    /// <summary>
    /// Avant l'appel de l'IA : message non vide (espaces retirés), fiche existante et modifiable (contrôle du Domaine,
    /// par une fusion sans proposition, pour ne pas dupliquer sa règle ni son refus). L'IA n'est jamais appelée pour
    /// un tour refusé d'avance.
    /// </summary>
    public static Resultat<TourPrepare> PreparerTour(FicheBesoin? fiche, ListeImmuable<Echange> historique, Guid id, string? message)
    {
        var nettoye = message?.Trim();
        if (string.IsNullOrEmpty(nettoye))
        {
            return new Refus("message.vide", "Le message de l'agent est vide.");
        }

        string texte = nettoye;
        return Existante(fiche, id)
            .Bind(trouvee => Fiche.AppliquerPropositions(trouvee, ListeImmuable<PropositionRubrique>.Vide))
            .Map(modifiable => new TourPrepare(
                modifiable,
                new ContexteTour(texte, historique, EtatRubriques(modifiable), Manquantes(modifiable), ProchaineRubrique(modifiable))));
    }

    /// <summary>
    /// Après l'appel de l'IA : réponse relue (refus → rien à écrire, DEB-Q22) ; hors périmètre → propositions ignorées
    /// (DEB-Q23) ; sinon fusion du Domaine. Le message rendu est COMPOSÉ sur la fiche fusionnée (lot A5, DEB-Q100 à
    /// DEB-Q103, <see cref="Dialogue.Composer"/>) : message de l'IA sans ses questions, puis la question de l'application.
    /// C'est lui qui est enregistré : l'historique montre à l'IA la question réellement posée. Deux échanges à ajouter :
    /// l'agent à <paramref name="debut"/>, l'IA à <paramref name="fin"/>.
    /// </summary>
    public static Resultat<IssueTour> ConclureTour(TourPrepare tour, string texteIA, DateTimeOffset debut, DateTimeOffset fin) =>
        ReponseIA.Lire(texteIA).Bind(reponse =>
            Fiche.AppliquerPropositions(
                    tour.Fiche,
                    reponse.HorsPerimetre ? ListeImmuable<PropositionRubrique>.Vide : reponse.Propositions)
                .Map(fiche =>
                {
                    var message = Dialogue.Composer(reponse.Message, reponse.HorsPerimetre, fiche);
                    return new IssueTour(
                        fiche,
                        ListeImmuable<Echange>.Vide
                            .Ajouter(new Echange(AuteurEchange.Agent, tour.Contexte.Message, debut))
                            .Ajouter(new Echange(AuteurEchange.IA, message, fin)),
                        new TourDialogue(message, reponse.HorsPerimetre, reponse.Ecartees, fiche));
                }));

    /// <summary>
    /// Soumission (DEB-Q17, DEB-Q24) : une fiche déjà numérotée garde son numéro (le dernier numéro n'est pas lu) ; sinon
    /// numéro suivant pour l'année de <paramref name="maintenant"/> vue dans <paramref name="fuseau"/>. L'instant
    /// enregistré reste <paramref name="maintenant"/>.
    /// </summary>
    public static Resultat<FicheBesoin> Soumettre(
        FicheBesoin fiche,
        bool confirme,
        string? dernierNumero,
        DateTimeOffset maintenant,
        TimeZoneInfo fuseau)
    {
        var numero = fiche.Numero is null
            ? Numerotation.Suivant(TimeZoneInfo.ConvertTime(maintenant, fuseau).Year, dernierNumero)
            : Resultat<string>.Succes(fiche.Numero);
        return numero.Bind(attribue => Cycle.Soumettre(fiche, confirme, attribue, maintenant));
    }

    /// <summary>Décision simulée : l'auteur est le valideur de l'en-tête, jamais l'agent courant (DEB-Q25).</summary>
    public static Resultat<FicheBesoin> Decider(FicheBesoin fiche, TypeDecision type, string? commentaire, DateTimeOffset maintenant) =>
        Cycle.Decider(fiche, type, fiche.EnTete.Valideur, commentaire, maintenant);

    /// <summary>Le .docx n'est produit que pour une fiche validée (plan §5 : 409 sinon).</summary>
    public static Resultat<FicheBesoin> Documentable(FicheBesoin fiche) =>
        fiche.Statut == Statut.Validee
            ? Resultat.Succes(fiche)
            : new Refus("document.nonValidee", $"Le document n'est produit que pour une fiche validée (statut {fiche.Statut}).");

    /// <summary>Nom du .docx : numéro de la fiche, à défaut son identifiant.</summary>
    public static string NomDocument(FicheBesoin fiche) => (fiche.Numero ?? fiche.Id.ToString("D")) + ".docx";

    internal static ListeImmuable<EtatRubrique> EtatRubriques(FicheBesoin fiche) =>
        ListeImmuable<EtatRubrique>.De(Catalogue.Toutes.Select(definition =>
        {
            var champ = fiche.ChampDe(definition.Rubrique);
            return new EtatRubrique(
                definition.Code,
                definition.Libelle,
                definition.Obligatoire,
                champ.Valeur,
                champ.PropositionEnAttente?.Valeur);
        }));

    internal static ListeImmuable<string> Manquantes(FicheBesoin fiche) =>
        ListeImmuable<string>.De(Completude.Manquantes(fiche).Select(rubrique => Catalogue.Definition(rubrique).Code));

    internal static string? ProchaineRubrique(FicheBesoin fiche) =>
        Dialogue.ProchaineRubrique(fiche) is { } rubrique ? Catalogue.Definition(rubrique).Code : null;
}
