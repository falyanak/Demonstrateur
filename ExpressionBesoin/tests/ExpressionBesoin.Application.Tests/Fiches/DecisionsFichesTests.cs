using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Application.Tests.Fiches.Essais;

namespace ExpressionBesoin.Application.Tests.Fiches;

/// <summary>Tables de vérité des décisions des cas d'usage (pures, sans doublure).</summary>
public sealed class DecisionsFichesTests
{
    private static readonly DateTimeOffset Debut = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Fin = new(2026, 11, 20, 10, 0, 4, TimeSpan.Zero);

    // --- Existence, rubrique, agent ---

    [Fact]
    public void Existante_refuse_une_fiche_absente()
    {
        var resultat = DecisionsFiches.Existante(null, IdAbsent);

        Assert.Equal("fiche.introuvable", CodeRefus(resultat));
        Assert.Equal("Aucune fiche d'identifiant 6f1c2b3a-0000-4000-8000-0000000000ff.", MessageRefus(resultat));
    }

    [Fact]
    public void Existante_rend_la_fiche_presente()
    {
        var fiche = Brouillon();

        Assert.Same(fiche, Valeur(DecisionsFiches.Existante(fiche, Id)));
    }

    [Theory]
    [InlineData("situationActuelle")]
    [InlineData("SITUATIONACTUELLE")]
    [InlineData("situationactuelle")]
    public void TrouverRubrique_ignore_la_casse(string code)
    {
        Assert.Equal(Rubrique.SituationActuelle, Valeur(DecisionsFiches.TrouverRubrique(code)));
    }

    [Fact]
    public void TrouverRubrique_refuse_un_code_inconnu()
    {
        var resultat = DecisionsFiches.TrouverRubrique("nulle");

        Assert.Equal("rubrique.inconnue", CodeRefus(resultat));
        Assert.Equal("Rubrique inconnue : « nulle ».", MessageRefus(resultat));
    }

    [Fact]
    public void CreerBrouillon_refuse_un_agent_inconnu()
    {
        var resultat = DecisionsFiches.CreerBrouillon(Id, null, "personne", Maintenant);

        Assert.Equal("agent.inconnu", CodeRefus(resultat));
        Assert.Equal("Agent inconnu du référentiel : « personne ».", MessageRefus(resultat));
    }

    [Fact]
    public void CreerBrouillon_tire_l_en_tete_du_referentiel_et_date_la_creation()
    {
        var fiche = Valeur(DecisionsFiches.CreerBrouillon(Id, AgentFictif, "agent.fictif", Maintenant));

        Assert.Equal(Id, fiche.Id);
        Assert.Equal(Statut.Brouillon, fiche.Statut);
        Assert.Null(fiche.Numero);
        Assert.Equal("Agent Fictif", fiche.EnTete.Demandeur);
        Assert.Equal("Bureau 4A", fiche.EnTete.Bureau);
        Assert.Equal("Chef Fictif", fiche.EnTete.Valideur);
        Assert.Equal(Maintenant, fiche.EnTete.CreeeLe);
        Assert.Equal(Fiche.Creer(Id, EnTeteFictif with { CreeeLe = Maintenant }), fiche);
    }

    [Fact]
    public void Consulter_refuse_une_fiche_absente()
    {
        Assert.Equal("fiche.introuvable", CodeRefus(DecisionsFiches.Consulter(null, Historique(), IdAbsent)));
    }

    [Fact]
    public void Consulter_rend_la_fiche_et_son_historique()
    {
        var fiche = Brouillon();
        var historique = Historique(new Echange(AuteurEchange.Agent, "Bonjour", Debut));

        var consultee = Valeur(DecisionsFiches.Consulter(fiche, historique, Id));

        Assert.Same(fiche, consultee.Fiche);
        Assert.Same(historique, consultee.Historique);
    }

    // --- Préparation d'un tour ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PreparerTour_refuse_un_message_vide_avant_tout_autre_controle(string? message)
    {
        Assert.Equal("message.vide", CodeRefus(DecisionsFiches.PreparerTour(Brouillon(), Historique(), Id, message)));
        Assert.Equal("message.vide", CodeRefus(DecisionsFiches.PreparerTour(null, Historique(), IdAbsent, message)));
        Assert.Equal("Le message de l'agent est vide.", MessageRefus(DecisionsFiches.PreparerTour(null, Historique(), IdAbsent, message)));
    }

    [Fact]
    public void PreparerTour_refuse_une_fiche_absente()
    {
        Assert.Equal("fiche.introuvable", CodeRefus(DecisionsFiches.PreparerTour(null, Historique(), IdAbsent, "Bonjour")));
    }

    [Fact]
    public void PreparerTour_refuse_une_fiche_non_modifiable_avec_le_refus_du_domaine()
    {
        var resultat = DecisionsFiches.PreparerTour(Soumise(), Historique(), Id, "Bonjour");

        Assert.Equal("fiche.nonModifiable", CodeRefus(resultat));
        Assert.Equal("La fiche est au statut Soumise : seule une fiche Brouillon est modifiable.", MessageRefus(resultat));
    }

    [Fact]
    public void PreparerTour_sur_un_brouillon_vierge()
    {
        var historique = Historique(new Echange(AuteurEchange.IA, "Bienvenue", Debut));

        var tour = Valeur(DecisionsFiches.PreparerTour(Brouillon(), historique, Id, "  Bonjour  "));

        Assert.Equal(Brouillon(), tour.Fiche);
        Assert.Equal("Bonjour", tour.Contexte.Message);
        Assert.Same(historique, tour.Contexte.Historique);
        Assert.Equal(17, tour.Contexte.Rubriques.Count);
        var application = tour.Contexte.Rubriques[0];
        Assert.Equal("application", application.Code);
        Assert.Equal("Application ou service concerné", application.Libelle);
        Assert.True(application.Obligatoire);
        Assert.Null(application.Valeur);
        Assert.Null(application.PropositionEnAttente);
        Assert.Equal(
            new[]
            {
                "application", "intitule", "resume", "situationActuelle", "difficultes", "resultatRecherche",
                "criteresReussite", "utilisateursConcernes", "processusMetier", "beneficesAttendus",
            },
            tour.Contexte.Manquantes.ToArray());
        Assert.Equal("application", tour.Contexte.ProchaineRubrique);
    }

    [Fact]
    public void PreparerTour_transmet_valeurs_et_propositions_et_saute_les_rubriques_proposees()
    {
        var fiche = Proposer(Saisir(Brouillon(), Rubrique.Intitule, "Titre"), Rubrique.Application, "Outil");

        var contexte = Valeur(DecisionsFiches.PreparerTour(fiche, Historique(), Id, "Suite")).Contexte;

        Assert.Equal(
            new[] { "application", "intitule", "resume", "situationActuelle", "difficultes" },
            contexte.Rubriques.Take(5).Select(etat => etat.Code).ToArray());
        Assert.Null(contexte.Rubriques[0].Valeur);
        Assert.Equal("Outil", contexte.Rubriques[0].PropositionEnAttente);
        Assert.Equal("Titre", contexte.Rubriques[1].Valeur);
        Assert.Null(contexte.Rubriques[1].PropositionEnAttente);
        Assert.Equal("Exemple représentatif", contexte.Rubriques[5].Libelle);
        Assert.False(contexte.Rubriques[5].Obligatoire);
        Assert.Equal(
            new[]
            {
                "application", "resume", "situationActuelle", "difficultes", "resultatRecherche", "criteresReussite",
                "utilisateursConcernes", "processusMetier", "beneficesAttendus",
            },
            contexte.Manquantes.ToArray());
        Assert.Equal("situationActuelle", contexte.ProchaineRubrique);
    }

    [Fact]
    public void PreparerTour_sur_une_fiche_complete_n_a_plus_de_question()
    {
        var contexte = Valeur(DecisionsFiches.PreparerTour(Complete(), Historique(), Id, "Merci")).Contexte;

        Assert.Empty(contexte.Manquantes);
        Assert.Null(contexte.ProchaineRubrique);
    }

    // --- Conclusion d'un tour ---

    private static TourPrepare Prepare() => Valeur(DecisionsFiches.PreparerTour(Brouillon(), Historique(), Id, "Bonjour"));

    [Fact]
    public void ConclureTour_refuse_une_reponse_illisible()
    {
        Assert.Equal("reponseIA.jsonInvalide", CodeRefus(DecisionsFiches.ConclureTour(Prepare(), "pas du json", Debut, Fin)));
        Assert.Equal("reponseIA.formeInvalide", CodeRefus(DecisionsFiches.ConclureTour(Prepare(), "[]", Debut, Fin)));
    }

    [Fact]
    public void ConclureTour_met_les_propositions_en_attente_et_rend_deux_echanges()
    {
        var issue = Valeur(DecisionsFiches.ConclureTour(Prepare(), ReponseAvecPropositions, Debut, Fin));

        var application = issue.Fiche.ChampDe(Rubrique.Application);
        Assert.Null(application.Valeur);
        Assert.Equal(Origine.Vide, application.Origine);
        Assert.Equal(new Proposition("Outil de contrôle", "cité par l'agent"), application.PropositionEnAttente);
        Assert.Equal(new Proposition("Contrôle e-commerce", "reformulation"), issue.Fiche.ChampDe(Rubrique.Intitule).PropositionEnAttente);

        Assert.Equal(2, issue.Echanges.Count);
        Assert.Equal(AuteurEchange.Agent, issue.Echanges[0].Auteur);
        Assert.Equal("Bonjour", issue.Echanges[0].Texte);
        Assert.Equal(Debut, issue.Echanges[0].Le);
        Assert.Equal(AuteurEchange.IA, issue.Echanges[1].Auteur);
        Assert.Equal("Quel est le processus actuel ?", issue.Echanges[1].Texte);
        Assert.Equal(Fin, issue.Echanges[1].Le);

        Assert.Equal("Quel est le processus actuel ?", issue.Tour.Message);
        Assert.False(issue.Tour.HorsPerimetre);
        Assert.Same(issue.Fiche, issue.Tour.Fiche);
        var ecartee = Assert.Single(issue.Tour.Ecartees);
        Assert.Equal(2, ecartee.Position);
        Assert.Equal("inconnue", ecartee.Code);
        Assert.Equal(MotifEcart.RubriqueInconnue, ecartee.Motif);
    }

    [Fact]
    public void ConclureTour_hors_perimetre_ignore_les_propositions()
    {
        var tour = Prepare();

        var issue = Valeur(DecisionsFiches.ConclureTour(tour, ReponseHorsPerimetre, Debut, Fin));

        Assert.Equal(tour.Fiche, issue.Fiche);
        Assert.Null(issue.Fiche.ChampDe(Rubrique.Application).PropositionEnAttente);
        Assert.True(issue.Tour.HorsPerimetre);
        Assert.Equal("Cette demande relève de l'assistance.", issue.Tour.Message);
        Assert.Empty(issue.Tour.Ecartees);
        Assert.Equal("Cette demande relève de l'assistance.", issue.Echanges[1].Texte);
    }

    // --- Soumission ---

    [Theory]
    [InlineData(null, false, false, "EB-2026-0001")]
    [InlineData("EB-2026-0042", false, false, "EB-2026-0043")]
    [InlineData("EB-2025-0099", true, false, "EB-2026-0001")]
    [InlineData("EB-2026-0042", true, false, "EB-2026-0043")]
    [InlineData("EB-2026-0042", false, true, "EB-2026-0043")]
    [InlineData("EB-2026-0042", true, true, "EB-2027-0001")]
    public void Soumettre_numerote_selon_l_annee_du_fuseau(string? dernier, bool plusUneHeure, bool veille, string attendu)
    {
        var maintenant = veille ? VeilleDuNouvelAn : Maintenant;
        var fuseau = plusUneHeure ? FuseauPlusUneHeure : TimeZoneInfo.Utc;

        var fiche = Valeur(DecisionsFiches.Soumettre(Complete(), true, dernier, maintenant, fuseau));

        Assert.Equal(attendu, fiche.Numero);
        Assert.Equal(Statut.Soumise, fiche.Statut);
        Assert.Equal(maintenant, fiche.ConfirmeeLe);
        Assert.Equal(TimeSpan.Zero, fiche.ConfirmeeLe!.Value.Offset);
    }

    [Fact]
    public void Soumettre_une_fiche_deja_numerotee_garde_son_numero_sans_lire_le_dernier()
    {
        var fiche = Valeur(DecisionsFiches.Soumettre(Complete() with { Numero = "EB-2026-0007" }, true, "illisible", Maintenant, TimeZoneInfo.Utc));

        Assert.Equal("EB-2026-0007", fiche.Numero);
    }

    [Fact]
    public void Soumettre_transmet_les_refus_de_la_numerotation_et_du_cycle()
    {
        Assert.Equal("numerotation.illisible", CodeRefus(DecisionsFiches.Soumettre(Complete(), true, "illisible", Maintenant, TimeZoneInfo.Utc)));
        Assert.Equal("soumission.incomplete", CodeRefus(DecisionsFiches.Soumettre(Brouillon(), true, null, Maintenant, TimeZoneInfo.Utc)));
        Assert.Equal("soumission.confirmationAbsente", CodeRefus(DecisionsFiches.Soumettre(Complete(), false, null, Maintenant, TimeZoneInfo.Utc)));
    }

    // --- Décision, document ---

    [Fact]
    public void Decider_a_pour_auteur_le_valideur_de_l_en_tete()
    {
        var soumise = Soumise() with { EnTete = EnTeteFictif with { Valideur = "Autre Chef" } };

        var fiche = Valeur(DecisionsFiches.Decider(soumise, TypeDecision.DemanderComplements, "préciser", Maintenant));

        Assert.Equal(Statut.ACompleter, fiche.Statut);
        var decision = Assert.Single(fiche.Decisions);
        Assert.Equal(TypeDecision.DemanderComplements, decision.Type);
        Assert.Equal("Autre Chef", decision.Auteur);
        Assert.Equal(Maintenant, decision.Le);
        Assert.Equal("préciser", decision.Commentaire);
    }

    [Fact]
    public void Decider_transmet_le_refus_du_cycle()
    {
        Assert.Equal("decision.commentaireObligatoire", CodeRefus(DecisionsFiches.Decider(Soumise(), TypeDecision.Refuser, null, Maintenant)));
    }

    [Fact]
    public void Documentable_rend_une_fiche_validee()
    {
        var fiche = Validee();

        Assert.Same(fiche, Valeur(DecisionsFiches.Documentable(fiche)));
    }

    [Theory]
    [InlineData(Statut.Brouillon)]
    [InlineData(Statut.Soumise)]
    [InlineData(Statut.ACompleter)]
    [InlineData(Statut.Refusee)]
    public void Documentable_refuse_une_fiche_non_validee(Statut statut)
    {
        var resultat = DecisionsFiches.Documentable(Validee() with { Statut = statut });

        Assert.Equal("document.nonValidee", CodeRefus(resultat));
        Assert.Equal($"Le document n'est produit que pour une fiche validée (statut {statut}).", MessageRefus(resultat));
    }

    [Fact]
    public void NomDocument_porte_le_numero_a_defaut_l_identifiant()
    {
        Assert.Equal("EB-2026-0001.docx", DecisionsFiches.NomDocument(Validee()));
        Assert.Equal("6f1c2b3a-0000-4000-8000-0000000000b2.docx", DecisionsFiches.NomDocument(Validee() with { Numero = null }));
    }
}
