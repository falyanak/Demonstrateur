using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

public sealed class CycleTests
{
    private static readonly DateTimeOffset PlusTard = new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);

    // --- Soumission ---

    [Fact]
    public void Soumettre_une_fiche_complete_et_confirmee()
    {
        var fiche = Valeur(Cycle.Soumettre(Complete(), true, "EB-2026-0001", Maintenant));

        Assert.Equal(Statut.Soumise, fiche.Statut);
        Assert.Equal("EB-2026-0001", fiche.Numero);
        Assert.Equal(Maintenant, fiche.ConfirmeeLe);
        Assert.False(fiche.EstModifiable);
    }

    [Fact]
    public void Une_nouvelle_soumission_garde_le_numero_deja_attribue()
    {
        var fiche = Valeur(Cycle.Soumettre(Complete() with { Numero = "EB-2026-0007" }, true, "EB-2026-0042", Maintenant));

        Assert.Equal("EB-2026-0007", fiche.Numero);
    }

    [Theory]
    [InlineData(Statut.Soumise)]
    [InlineData(Statut.Validee)]
    [InlineData(Statut.ACompleter)]
    [InlineData(Statut.Refusee)]
    public void Soumettre_hors_brouillon_interdit(Statut statut)
    {
        var resultat = Cycle.Soumettre(Complete() with { Statut = statut }, true, "EB-2026-0001", Maintenant);

        Assert.Equal("cycle.transitionInterdite", CodeRefus(resultat));
        Assert.Equal($"Soumission impossible au statut {statut}.", MessageRefus(resultat));
    }

    [Fact]
    public void Soumettre_sans_confirmation_refuse_avant_tout_autre_controle()
    {
        Assert.Equal("soumission.confirmationAbsente", CodeRefus(Cycle.Soumettre(Brouillon(), false, "EB-2026-0001", Maintenant)));
        Assert.Equal("soumission.confirmationAbsente", CodeRefus(Cycle.Soumettre(Complete(), false, "EB-2026-0001", Maintenant)));
    }

    [Fact]
    public void Soumettre_une_fiche_incomplete_liste_les_rubriques_manquantes()
    {
        var fiche = Saisir(Saisir(Complete(), Rubrique.BeneficesAttendus, null), Rubrique.Resume, null);
        var resultat = Cycle.Soumettre(fiche, true, "EB-2026-0001", Maintenant);

        Assert.Equal("soumission.incomplete", CodeRefus(resultat));
        Assert.Equal("Rubriques obligatoires à renseigner : Résumé, Bénéfices attendus.", MessageRefus(resultat));
    }

    [Fact]
    public void Soumettre_avec_une_proposition_en_attente_meme_facultative_refuse()
    {
        var fiche = Proposer(Proposer(Complete(), Rubrique.ExempleRepresentatif, "Exemple"), Rubrique.Intitule, "Autre titre");
        var resultat = Cycle.Soumettre(fiche, true, "EB-2026-0001", Maintenant);

        Assert.Equal("soumission.propositionsEnAttente", CodeRefus(resultat));
        Assert.Equal("Propositions à accepter ou rejeter : Intitulé court, Exemple représentatif.", MessageRefus(resultat));
    }

    // --- Décision du chef de service ---

    [Theory]
    [InlineData(TypeDecision.Valider, Statut.Validee)]
    [InlineData(TypeDecision.DemanderComplements, Statut.ACompleter)]
    [InlineData(TypeDecision.Refuser, Statut.Refusee)]
    public void Decider_sur_une_fiche_soumise(TypeDecision type, Statut attendu)
    {
        var fiche = Valeur(Cycle.Decider(Soumise(), type, "Chef Fictif", "  motif  ", PlusTard));

        Assert.Equal(attendu, fiche.Statut);
        var decision = Assert.Single(fiche.Decisions);
        Assert.Equal(type, decision.Type);
        Assert.Equal("Chef Fictif", decision.Auteur);
        Assert.Equal(PlusTard, decision.Le);
        Assert.Equal("motif", decision.Commentaire);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Valider_sans_commentaire_admis(string? commentaire)
    {
        var fiche = Valeur(Cycle.Decider(Soumise(), TypeDecision.Valider, "Chef Fictif", commentaire, PlusTard));

        Assert.Equal(Statut.Validee, fiche.Statut);
        Assert.Null(Assert.Single(fiche.Decisions).Commentaire);
    }

    [Theory]
    [InlineData(TypeDecision.DemanderComplements, null)]
    [InlineData(TypeDecision.DemanderComplements, "  ")]
    [InlineData(TypeDecision.Refuser, null)]
    [InlineData(TypeDecision.Refuser, "")]
    public void Complements_ou_refus_sans_commentaire_refuses(TypeDecision type, string? commentaire)
    {
        Assert.Equal("decision.commentaireObligatoire", CodeRefus(Cycle.Decider(Soumise(), type, "Chef Fictif", commentaire, PlusTard)));
    }

    [Theory]
    [InlineData(Statut.Brouillon)]
    [InlineData(Statut.Validee)]
    [InlineData(Statut.ACompleter)]
    [InlineData(Statut.Refusee)]
    public void Decider_hors_soumission_interdit(Statut statut)
    {
        var resultat = Cycle.Decider(Soumise() with { Statut = statut }, TypeDecision.Valider, "Chef Fictif", null, PlusTard);

        Assert.Equal("cycle.transitionInterdite", CodeRefus(resultat));
        Assert.Equal($"Décision impossible au statut {statut}.", MessageRefus(resultat));
    }

    [Fact]
    public void Type_de_decision_inconnu_leve()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Cycle.Decider(Soumise(), (TypeDecision)99, "Chef Fictif", "motif", PlusTard));

        Assert.Equal("type", exception.ParamName);
    }

    // --- Reprise ---

    [Fact]
    public void Reprendre_une_fiche_a_completer_la_remet_en_brouillon_a_reconfirmer()
    {
        var aCompleter = Valeur(Cycle.Decider(Soumise(), TypeDecision.DemanderComplements, "Chef Fictif", "Préciser", PlusTard));
        var fiche = Valeur(Cycle.Reprendre(aCompleter));

        Assert.Equal(Statut.Brouillon, fiche.Statut);
        Assert.Null(fiche.ConfirmeeLe);
        Assert.Equal("EB-2026-0001", fiche.Numero);
        Assert.Single(fiche.Decisions);
    }

    [Theory]
    [InlineData(Statut.Brouillon)]
    [InlineData(Statut.Soumise)]
    [InlineData(Statut.Validee)]
    [InlineData(Statut.Refusee)]
    public void Reprendre_hors_a_completer_interdit(Statut statut)
    {
        var resultat = Cycle.Reprendre(Soumise() with { Statut = statut });

        Assert.Equal("cycle.transitionInterdite", CodeRefus(resultat));
        Assert.Equal($"Reprise impossible au statut {statut}.", MessageRefus(resultat));
    }

    // --- Parcours (critère de fin du démonstrateur, partie Domaine) ---

    [Fact]
    public void Parcours_retour_pour_complement_puis_nouvelle_soumission_et_validation()
    {
        var soumise = Soumise("EB-2026-0003");
        var aCompleter = Valeur(Cycle.Decider(soumise, TypeDecision.DemanderComplements, "Chef Fictif", "Préciser les volumes", PlusTard));
        var reprise = Saisir(Valeur(Cycle.Reprendre(aCompleter)), Rubrique.PointsAClarifier, "Volumes : 1 200 contrôles par an");

        Assert.Equal("soumission.confirmationAbsente", CodeRefus(Cycle.Soumettre(reprise, false, "EB-2026-0099", PlusTard)));

        var resoumise = Valeur(Cycle.Soumettre(reprise, true, "EB-2026-0099", PlusTard));
        var validee = Valeur(Cycle.Decider(resoumise, TypeDecision.Valider, "Chef Fictif", null, PlusTard.AddHours(1)));

        Assert.Equal("EB-2026-0003", validee.Numero);
        Assert.Equal(PlusTard, validee.ConfirmeeLe);
        Assert.Equal(Statut.Validee, validee.Statut);
        Assert.Equal(
            new[] { TypeDecision.DemanderComplements, TypeDecision.Valider },
            validee.Decisions.Select(decision => decision.Type).ToArray());
        Assert.Equal("fiche.nonModifiable", CodeRefus(Fiche.ModifierChamp(validee, Rubrique.Intitule, "x")));
    }
}
