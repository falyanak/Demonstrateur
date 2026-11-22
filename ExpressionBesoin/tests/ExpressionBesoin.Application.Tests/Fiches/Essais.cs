using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Application.Tests.Fiches;

/// <summary>Données d'essai fictives et aides communes aux tests de l'Application.</summary>
internal static class Essais
{
    public static readonly Guid Id = new("6f1c2b3a-0000-4000-8000-0000000000b2");

    public static readonly Guid IdAbsent = new("6f1c2b3a-0000-4000-8000-0000000000ff");

    public static readonly DateTimeOffset CreeeLe = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset Maintenant = new(2026, 11, 20, 9, 30, 0, TimeSpan.Zero);

    /// <summary>31/12/2026 23 h 30 UTC : déjà le 1er janvier 2027 dans un fuseau à + 1 h (DEB-Q24).</summary>
    public static readonly DateTimeOffset VeilleDuNouvelAn = new(2026, 12, 31, 23, 30, 0, TimeSpan.Zero);

    /// <summary>Fuseau fixe à + 1 h, indépendant des fuseaux du système (essai déterministe).</summary>
    public static readonly TimeZoneInfo FuseauPlusUneHeure =
        TimeZoneInfo.CreateCustomTimeZone("Essai+1", TimeSpan.FromHours(1), "Essai + 1 h", "Essai + 1 h");

    public static readonly Agent AgentFictif = new("agent.fictif", "Agent Fictif", "Bureau 4A", "Chef Fictif");

    public static readonly EnTete EnTeteFictif = new("Agent Fictif", "Bureau 4A", "Chef Fictif", CreeeLe);

    public static CancellationToken Annulation => TestContext.Current.CancellationToken;

    public static FicheBesoin Brouillon() => Fiche.Creer(Id, EnTeteFictif);

    public static T Valeur<T>(Resultat<T> resultat) =>
        resultat.Match(valeur => valeur, refus => throw new InvalidOperationException("Refus inattendu : " + refus.Code));

    public static string CodeRefus<T>(Resultat<T> resultat) => resultat.Match(_ => "succès", refus => refus.Code);

    public static string MessageRefus<T>(Resultat<T> resultat) => resultat.Match(_ => "succès", refus => refus.Message);

    public static FicheBesoin Saisir(FicheBesoin fiche, Rubrique rubrique, string? valeur) =>
        Valeur(Fiche.ModifierChamp(fiche, rubrique, valeur));

    public static FicheBesoin Proposer(FicheBesoin fiche, Rubrique rubrique, string valeur) =>
        Valeur(Fiche.AppliquerPropositions(
            fiche,
            ListeImmuable<PropositionRubrique>.Vide.Ajouter(new PropositionRubrique(rubrique, new Proposition(valeur, "j")))));

    /// <summary>Brouillon dont toutes les rubriques obligatoires sont saisies (« valeur de &lt;code&gt; »).</summary>
    public static FicheBesoin Complete()
    {
        var fiche = Brouillon();
        foreach (var definition in Catalogue.Toutes)
        {
            if (definition.Obligatoire)
            {
                fiche = Saisir(fiche, definition.Rubrique, "valeur de " + definition.Code);
            }
        }

        return fiche;
    }

    public static FicheBesoin Soumise() => Valeur(Cycle.Soumettre(Complete(), true, "EB-2026-0001", Maintenant));

    public static FicheBesoin Validee() => Valeur(Cycle.Decider(Soumise(), TypeDecision.Valider, "Chef Fictif", null, Maintenant));

    public static ListeImmuable<Echange> Historique(params Echange[] echanges) => ListeImmuable<Echange>.De(echanges);

    /// <summary>
    /// Réponse de l'IA : application et intitulé proposés, une rubrique inconnue écartée (position 2) ; message terminé
    /// par une question, retirée à la composition (DEB-Q103).
    /// </summary>
    public const string ReponseAvecPropositions = """
        {
          "message": "J'ai noté l'outil de contrôle. Quel est le processus actuel ?",
          "propositions": [
            { "rubrique": "application", "valeur": "Outil de contrôle", "justification": "cité par l'agent" },
            { "rubrique": "intitule", "valeur": "Contrôle e-commerce", "justification": "reformulation" },
            { "rubrique": "inconnue", "valeur": "x", "justification": "y" }
          ]
        }
        """;

    public const string ReponseHorsPerimetre = """
        {
          "message": "Cette demande relève de l'assistance.",
          "propositions": [ { "rubrique": "application", "valeur": "Imprimante", "justification": "citée" } ],
          "horsPerimetre": true
        }
        """;

    public const string ReponseSansProposition = """{ "message": "Pouvez-vous préciser ?" }""";

    /// <summary>
    /// Message composé pour <see cref="ReponseAvecPropositions"/> sur un brouillon vierge : phrase de l'IA, puis libellé
    /// et question type de la situation actuelle (application et intitulé proposés, donc sautés ; aucune candidate).
    /// </summary>
    public const string MessageComposeAvecPropositions =
        "J'ai noté l'outil de contrôle.\n\nSituation actuelle : Que se passe-t-il aujourd'hui, concrètement ?";
}
