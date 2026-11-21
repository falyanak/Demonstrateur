using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

/// <summary>Cycle de vie (DEB-Q74, DEB-Q75) : transition déduite d'une écriture, étape d'une transition.</summary>
public sealed class TransitionsTests
{
    private static readonly DateTimeOffset PlusTard = new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);

    private static FicheBesoin Soumise() => Valeur(Cycle.Soumettre(Complete(), true, "EB-2026-0001", Maintenant));

    private static FicheBesoin Decidee(TypeDecision type) =>
        Valeur(Cycle.Decider(Soumise(), type, "Chef Fictif", type == TypeDecision.Valider ? null : "motif", Maintenant));

    /// <summary>Champ par champ : un record comparé par <c>Assert.Equal</c> seulement laisserait ses accesseurs non lus.</summary>
    private static void Verifier(Transition? transition, Statut? avant, Statut apres, string acteur, DateTimeOffset le)
    {
        Assert.NotNull(transition);
        Assert.Equal(avant, transition.Avant);
        Assert.Equal(apres, transition.Apres);
        Assert.Equal(acteur, transition.Acteur);
        Assert.Equal(le, transition.Le);
    }

    // --- Entre ---

    [Fact]
    public void Creation_sans_statut_avant_par_le_demandeur()
    {
        Verifier(Transitions.Entre(null, Brouillon(), PlusTard), null, Statut.Brouillon, "Agent Fictif", PlusTard);
    }

    [Fact]
    public void Statut_inchange_aucune_transition()
    {
        Assert.Null(Transitions.Entre(Brouillon(), Saisir(Brouillon(), Rubrique.Intitule, "Titre"), PlusTard));
        Assert.Null(Transitions.Entre(Brouillon(), Proposer(Brouillon(), Rubrique.Intitule, "Titre"), PlusTard));
        Assert.Null(Transitions.Entre(Soumise(), Soumise(), PlusTard));
    }

    [Fact]
    public void Soumission_par_le_demandeur_a_l_instant_donne_pas_a_la_confirmation()
    {
        Verifier(Transitions.Entre(Complete(), Soumise(), PlusTard), Statut.Brouillon, Statut.Soumise, "Agent Fictif", PlusTard);
    }

    [Theory]
    [InlineData(TypeDecision.Valider, Statut.Validee)]
    [InlineData(TypeDecision.DemanderComplements, Statut.ACompleter)]
    [InlineData(TypeDecision.Refuser, Statut.Refusee)]
    public void Decision_par_le_valideur(TypeDecision type, Statut attendu)
    {
        Verifier(Transitions.Entre(Soumise(), Decidee(type), PlusTard), Statut.Soumise, attendu, "Chef Fictif", PlusTard);
    }

    [Fact]
    public void Reprise_par_le_demandeur()
    {
        var aCompleter = Decidee(TypeDecision.DemanderComplements);

        Verifier(
            Transitions.Entre(aCompleter, Valeur(Cycle.Reprendre(aCompleter)), PlusTard),
            Statut.ACompleter,
            Statut.Brouillon,
            "Agent Fictif",
            PlusTard);
    }

    [Fact]
    public void L_acteur_est_lu_sur_la_fiche_apres_ecriture()
    {
        var apres = Soumise() with { EnTete = EnTeteFictif with { Demandeur = "Autre Agent" } };

        Assert.Equal("Autre Agent", Transitions.Entre(Complete(), apres, PlusTard)!.Acteur);
    }

    // --- EtapeDe : les six transitions du cycle, toute autre paire lève ---

    [Theory]
    [InlineData(null, Statut.Brouillon, Etape.Creation)]
    [InlineData(Statut.Brouillon, Statut.Soumise, Etape.Soumission)]
    [InlineData(Statut.Soumise, Statut.Validee, Etape.Validation)]
    [InlineData(Statut.Soumise, Statut.ACompleter, Etape.DemandeComplements)]
    [InlineData(Statut.Soumise, Statut.Refusee, Etape.Refus)]
    [InlineData(Statut.ACompleter, Statut.Brouillon, Etape.Reprise)]
    public void Etape_des_transitions_du_cycle(Statut? avant, Statut apres, Etape attendue)
    {
        Assert.Equal(attendue, Transitions.EtapeDe(new Transition(avant, apres, "x", PlusTard)));
    }

    [Theory]
    [InlineData(null, Statut.Soumise, "aucun → Soumise")]
    [InlineData(Statut.Brouillon, Statut.Validee, "Brouillon → Validee")]
    [InlineData(Statut.Brouillon, Statut.Brouillon, "Brouillon → Brouillon")]
    [InlineData(Statut.Soumise, Statut.Brouillon, "Soumise → Brouillon")]
    [InlineData(Statut.Validee, Statut.Brouillon, "Validee → Brouillon")]
    [InlineData(Statut.Refusee, Statut.Brouillon, "Refusee → Brouillon")]
    [InlineData(Statut.ACompleter, Statut.Soumise, "ACompleter → Soumise")]
    public void Etape_hors_du_cycle_leve(Statut? avant, Statut apres, string paire)
    {
        var erreur = Assert.Throws<InvalidOperationException>(() => Transitions.EtapeDe(new Transition(avant, apres, "x", PlusTard)));

        Assert.Equal("Transition hors du cycle : " + paire + ".", erreur.Message);
    }

    [Fact]
    public void Exactement_six_paires_du_cycle_sur_les_trente_possibles()
    {
        var avants = new Statut?[] { null }.Concat(Enum.GetValues<Statut>().Select(statut => (Statut?)statut));
        var reconnues = 0;
        foreach (var avant in avants)
        {
            foreach (var apres in Enum.GetValues<Statut>())
            {
                try
                {
                    Transitions.EtapeDe(new Transition(avant, apres, "x", PlusTard));
                    reconnues++;
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        Assert.Equal(6, reconnues);
    }
}
