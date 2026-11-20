using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Domain.Tests.Fiches;

/// <summary>Données d'essai fictives et aides communes aux tests des fiches.</summary>
internal static class Essais
{
    public static readonly Guid Id = new("6f1c2b3a-0000-4000-8000-000000000001");

    public static readonly DateTimeOffset CreeeLe = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset Maintenant = new(2026, 11, 20, 9, 30, 0, TimeSpan.Zero);

    public static readonly EnTete EnTeteFictif = new("Agent Fictif", "Bureau 4A", "Chef Fictif", CreeeLe);

    public static FicheBesoin Brouillon() => Fiche.Creer(Id, EnTeteFictif);

    public static T Valeur<T>(Resultat<T> resultat) =>
        resultat.Match(valeur => valeur, refus => throw new InvalidOperationException("Refus inattendu : " + refus.Code));

    public static string CodeRefus<T>(Resultat<T> resultat) => resultat.Match(_ => "succès", refus => refus.Code);

    public static string MessageRefus<T>(Resultat<T> resultat) => resultat.Match(_ => "succès", refus => refus.Message);

    public static FicheBesoin Saisir(FicheBesoin fiche, Rubrique rubrique, string? valeur) =>
        Valeur(Fiche.ModifierChamp(fiche, rubrique, valeur));

    public static FicheBesoin Proposer(FicheBesoin fiche, Rubrique rubrique, string valeur, string justification = "justification") =>
        Valeur(Fiche.AppliquerPropositions(fiche, Une(new PropositionRubrique(rubrique, new Proposition(valeur, justification)))));

    public static ListeImmuable<PropositionRubrique> Une(PropositionRubrique proposition) =>
        ListeImmuable<PropositionRubrique>.Vide.Ajouter(proposition);

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

    public static FicheBesoin Soumise(string numero = "EB-2026-0001") =>
        Valeur(Cycle.Soumettre(Complete(), true, numero, Maintenant));
}
