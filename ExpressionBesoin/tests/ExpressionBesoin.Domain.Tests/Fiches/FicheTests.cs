using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

public sealed class FicheTests
{
    // --- Création ---

    [Fact]
    public void Creer_donne_un_brouillon_vierge()
    {
        var fiche = Brouillon();

        Assert.Equal(Id, fiche.Id);
        Assert.Null(fiche.Numero);
        Assert.Equal(Statut.Brouillon, fiche.Statut);
        Assert.True(fiche.EstModifiable);
        Assert.Null(fiche.ConfirmeeLe);
        Assert.Empty(fiche.Decisions);
        Assert.Equal(17, fiche.Champs.Count);
        foreach (var champ in fiche.Champs)
        {
            Assert.Equal(Champ.Initial, champ);
        }

        Assert.Equal("Agent Fictif", fiche.EnTete.Demandeur);
        Assert.Equal("Bureau 4A", fiche.EnTete.Bureau);
        Assert.Equal("Chef Fictif", fiche.EnTete.Valideur);
        Assert.Equal(CreeeLe, fiche.EnTete.CreeeLe);
    }

    [Fact]
    public void Deux_fiches_creees_pareil_sont_egales_champ_par_champ()
    {
        Assert.Equal(Brouillon(), Brouillon());
        Assert.NotEqual(Brouillon(), Saisir(Brouillon(), Rubrique.Intitule, "Titre"));
    }

    [Fact]
    public void Champ_initial_est_vide_sans_proposition()
    {
        Assert.Null(Champ.Initial.Valeur);
        Assert.Equal(Origine.Vide, Champ.Initial.Origine);
        Assert.Null(Champ.Initial.PropositionEnAttente);
        Assert.False(Champ.Initial.EstProposee);
    }

    [Theory]
    [InlineData(Origine.Vide, true, true)]
    [InlineData(Origine.Vide, false, false)]
    [InlineData(Origine.Manuelle, true, false)]
    [InlineData(Origine.AccepteeIA, true, false)]
    public void Etat_propose_seulement_sans_valeur_retenue_et_avec_proposition(Origine origine, bool avecProposition, bool proposee)
    {
        var valeur = origine == Origine.Vide ? null : "retenue";
        var champ = new Champ(valeur, origine, avecProposition ? new Proposition("proposée", "j") : null);

        Assert.Equal(proposee, champ.EstProposee);
    }

    // --- Fusion des propositions ---

    [Fact]
    public void Proposition_sur_rubrique_vide_mise_en_attente_sans_valeur_retenue()
    {
        var fiche = Proposer(Brouillon(), Rubrique.SituationActuelle, "Contrôles sur place", "dit par l'agent");
        var champ = fiche.ChampDe(Rubrique.SituationActuelle);

        Assert.Null(champ.Valeur);
        Assert.Equal(Origine.Vide, champ.Origine);
        Assert.Equal("Contrôles sur place", champ.PropositionEnAttente!.Valeur);
        Assert.Equal("dit par l'agent", champ.PropositionEnAttente.Justification);
        Assert.True(champ.EstProposee);
    }

    [Fact]
    public void Proposition_sur_rubrique_manuelle_ne_l_ecrase_jamais()
    {
        var fiche = Proposer(Saisir(Brouillon(), Rubrique.Intitule, "Mon titre"), Rubrique.Intitule, "Titre de l'IA");
        var champ = fiche.ChampDe(Rubrique.Intitule);

        Assert.Equal("Mon titre", champ.Valeur);
        Assert.Equal(Origine.Manuelle, champ.Origine);
        Assert.Equal("Titre de l'IA", champ.PropositionEnAttente!.Valeur);
    }

    [Fact]
    public void Nouvelle_proposition_remplace_la_proposition_en_attente()
    {
        var fiche = Proposer(Proposer(Brouillon(), Rubrique.Difficultes, "première"), Rubrique.Difficultes, "seconde");

        Assert.Equal("seconde", fiche.ChampDe(Rubrique.Difficultes).PropositionEnAttente!.Valeur);
    }

    [Fact]
    public void Proposition_egale_a_la_valeur_retenue_ignoree()
    {
        var fiche = Proposer(Saisir(Brouillon(), Rubrique.Intitule, "Titre"), Rubrique.Intitule, "Titre");

        Assert.Null(fiche.ChampDe(Rubrique.Intitule).PropositionEnAttente);
    }

    [Fact]
    public void Plusieurs_propositions_ne_touchent_que_leurs_rubriques()
    {
        var resultat = Fiche.AppliquerPropositions(
            Brouillon(),
            ListeImmuable<PropositionRubrique>.De(new[]
            {
                new PropositionRubrique(Rubrique.Application, new Proposition("Outil de contrôle", "j1")),
                new PropositionRubrique(Rubrique.Liens, new Proposition("Demande EB-2025-0012", "j2")),
            }));
        var fiche = Valeur(resultat);

        Assert.Equal("Outil de contrôle", fiche.ChampDe(Rubrique.Application).PropositionEnAttente!.Valeur);
        Assert.Equal("Demande EB-2025-0012", fiche.ChampDe(Rubrique.Liens).PropositionEnAttente!.Valeur);
        Assert.Equal(
            new[] { Rubrique.Application, Rubrique.Liens },
            Completude.PropositionsEnAttente(fiche).ToArray());
    }

    [Theory]
    [InlineData(Statut.Soumise)]
    [InlineData(Statut.Validee)]
    [InlineData(Statut.ACompleter)]
    [InlineData(Statut.Refusee)]
    public void Fiche_hors_brouillon_non_modifiable(Statut statut)
    {
        var fiche = Proposer(Brouillon(), Rubrique.Intitule, "Titre") with { Statut = statut };
        var proposition = Une(new PropositionRubrique(Rubrique.Intitule, new Proposition("autre", "j")));

        Assert.False(fiche.EstModifiable);
        Assert.Equal("fiche.nonModifiable", CodeRefus(Fiche.AppliquerPropositions(fiche, proposition)));
        Assert.Equal("fiche.nonModifiable", CodeRefus(Fiche.ModifierChamp(fiche, Rubrique.Intitule, "x")));
        Assert.Equal("fiche.nonModifiable", CodeRefus(Fiche.AccepterProposition(fiche, Rubrique.Intitule)));
        Assert.Equal("fiche.nonModifiable", CodeRefus(Fiche.RejeterProposition(fiche, Rubrique.Intitule)));
        Assert.Equal(
            $"La fiche est au statut {statut} : seule une fiche Brouillon est modifiable.",
            MessageRefus(Fiche.ModifierChamp(fiche, Rubrique.Intitule, "x")));
    }

    // --- Saisie manuelle ---

    [Fact]
    public void Saisie_manuelle_nettoyee_de_ses_espaces()
    {
        var champ = Saisir(Brouillon(), Rubrique.Intitule, "  Contrôle e-commerce \r\n").ChampDe(Rubrique.Intitule);

        Assert.Equal("Contrôle e-commerce", champ.Valeur);
        Assert.Equal(Origine.Manuelle, champ.Origine);
        Assert.Null(champ.PropositionEnAttente);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Saisie_vide_remet_la_rubrique_a_vide_et_garde_la_proposition(string? valeur)
    {
        var avant = Proposer(Saisir(Brouillon(), Rubrique.Intitule, "Titre"), Rubrique.Intitule, "Titre de l'IA");
        var champ = Saisir(avant, Rubrique.Intitule, valeur).ChampDe(Rubrique.Intitule);

        Assert.Null(champ.Valeur);
        Assert.Equal(Origine.Vide, champ.Origine);
        Assert.Equal("Titre de l'IA", champ.PropositionEnAttente!.Valeur);
    }

    [Fact]
    public void Saisie_differente_de_la_proposition_garde_la_proposition()
    {
        var champ = Saisir(Proposer(Brouillon(), Rubrique.Intitule, "Titre de l'IA"), Rubrique.Intitule, "Mon titre")
            .ChampDe(Rubrique.Intitule);

        Assert.Equal("Mon titre", champ.Valeur);
        Assert.Equal("Titre de l'IA", champ.PropositionEnAttente!.Valeur);
    }

    [Fact]
    public void Saisie_egale_a_la_proposition_la_rend_sans_objet()
    {
        var champ = Saisir(Proposer(Brouillon(), Rubrique.Intitule, "Titre"), Rubrique.Intitule, " Titre ")
            .ChampDe(Rubrique.Intitule);

        Assert.Equal("Titre", champ.Valeur);
        Assert.Equal(Origine.Manuelle, champ.Origine);
        Assert.Null(champ.PropositionEnAttente);
    }

    // --- Acceptation et rejet ---

    [Fact]
    public void Accepter_retient_la_valeur_proposee()
    {
        var fiche = Valeur(Fiche.AccepterProposition(Proposer(Brouillon(), Rubrique.Resume, "Synthèse"), Rubrique.Resume));
        var champ = fiche.ChampDe(Rubrique.Resume);

        Assert.Equal("Synthèse", champ.Valeur);
        Assert.Equal(Origine.AccepteeIA, champ.Origine);
        Assert.Null(champ.PropositionEnAttente);
    }

    [Fact]
    public void Accepter_sur_rubrique_manuelle_remplace_la_saisie_par_choix_de_l_agent()
    {
        var avant = Proposer(Saisir(Brouillon(), Rubrique.Intitule, "Mon titre"), Rubrique.Intitule, "Titre de l'IA");
        var champ = Valeur(Fiche.AccepterProposition(avant, Rubrique.Intitule)).ChampDe(Rubrique.Intitule);

        Assert.Equal("Titre de l'IA", champ.Valeur);
        Assert.Equal(Origine.AccepteeIA, champ.Origine);
    }

    [Fact]
    public void Rejeter_retire_la_proposition_et_garde_la_valeur_retenue()
    {
        var avant = Proposer(Saisir(Brouillon(), Rubrique.Intitule, "Mon titre"), Rubrique.Intitule, "Titre de l'IA");
        var champ = Valeur(Fiche.RejeterProposition(avant, Rubrique.Intitule)).ChampDe(Rubrique.Intitule);

        Assert.Equal("Mon titre", champ.Valeur);
        Assert.Equal(Origine.Manuelle, champ.Origine);
        Assert.Null(champ.PropositionEnAttente);
    }

    [Fact]
    public void Accepter_ou_rejeter_sans_proposition_refuse()
    {
        var fiche = Brouillon();

        Assert.Equal("proposition.absente", CodeRefus(Fiche.AccepterProposition(fiche, Rubrique.SituationActuelle)));
        Assert.Equal("proposition.absente", CodeRefus(Fiche.RejeterProposition(fiche, Rubrique.SituationActuelle)));
        Assert.Equal(
            "Aucune proposition en attente pour la rubrique « Situation actuelle ».",
            MessageRefus(Fiche.AccepterProposition(fiche, Rubrique.SituationActuelle)));
    }
}
