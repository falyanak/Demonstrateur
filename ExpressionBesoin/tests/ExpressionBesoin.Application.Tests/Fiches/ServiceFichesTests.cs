using System.Text;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Application.Tests.Fiches.Essais;

namespace ExpressionBesoin.Application.Tests.Fiches;

/// <summary>
/// Coquilles des cas d'usage, sur doublures en mémoire : ce qui est lu, ce qui est écrit (ou non), ce qui est appelé.
/// Les règles elles-mêmes sont couvertes par <see cref="DecisionsFichesTests"/> et les tests du Domaine.
/// </summary>
public sealed class ServiceFichesTests
{
    private sealed record Banc(
        DepotEnMemoire Depot,
        AssistantScenarise Assistant,
        GenerateurFactice Generateur,
        HorlogeReglee Horloge,
        ServiceFiches Service);

    /// <summary>Fuseau du banc : + 1 h (DEB-Q24) ; identifiant de toute fiche créée : <see cref="Essais.Id"/>.</summary>
    private static Banc Monter(bool differe, params string[] reponses)
    {
        var depot = new DepotEnMemoire(differe);
        var assistant = new AssistantScenarise(differe, reponses);
        var generateur = new GenerateurFactice(differe);
        var horloge = new HorlogeReglee(Maintenant);
        var service = new ServiceFiches(
            depot,
            assistant,
            generateur,
            new ReferentielFictif(differe),
            horloge,
            FuseauPlusUneHeure,
            () => Id);
        return new Banc(depot, assistant, generateur, horloge, service);
    }

    private static Banc Monter(params string[] reponses) => Monter(false, reponses);

    private static Transition Passage(Statut? avant, Statut apres, string acteur, DateTimeOffset le) => new(avant, apres, acteur, le);

    // --- Agents, liste ---

    [Fact]
    public async Task ListerAgents_rend_les_agents_du_referentiel()
    {
        var agents = await Monter().Service.ListerAgents(Annulation);

        var agent = Assert.Single(agents);
        Assert.Equal("agent.fictif", agent.Identifiant);
        Assert.Equal("Agent Fictif", agent.Nom);
        Assert.Equal("Bureau 4A", agent.Bureau);
        Assert.Equal("Chef Fictif", agent.Valideur);
    }

    [Fact]
    public async Task Lister_rend_les_resumes_du_depot()
    {
        var banc = Monter();
        banc.Depot.Placer(Validee());

        var resume = Assert.Single(await banc.Service.Lister(Annulation));

        Assert.Equal(ResumeFiche.De(Validee()), resume);
    }

    // --- Création, consultation ---

    [Fact]
    public async Task CreerBrouillon_enregistre_la_fiche_d_un_agent_connu()
    {
        var banc = Monter();

        var fiche = Valeur(await banc.Service.CreerBrouillon("agent.fictif", Annulation));

        Assert.Equal(Id, fiche.Id);
        Assert.Equal(Maintenant, fiche.EnTete.CreeeLe);
        Assert.Equal(1, banc.Depot.Enregistrements);
        Assert.Equal(fiche, banc.Depot.FicheDe(Id));
        Assert.Empty(banc.Depot.HistoriqueDe(Id));
        Assert.Equal(new[] { Passage(null, Statut.Brouillon, "Agent Fictif", Maintenant) }, banc.Depot.TransitionsDe(Id).ToArray());
    }

    [Fact]
    public async Task CreerBrouillon_pour_un_agent_inconnu_n_ecrit_rien()
    {
        var banc = Monter();

        var resultat = await banc.Service.CreerBrouillon("personne", Annulation);

        Assert.Equal("agent.inconnu", CodeRefus(resultat));
        Assert.Equal(0, banc.Depot.Enregistrements);
        Assert.Null(banc.Depot.FicheDe(Id));
        Assert.Empty(banc.Depot.TransitionsDe(Id));
    }

    [Fact]
    public async Task Consulter_rend_la_fiche_son_historique_et_ses_transitions()
    {
        var banc = Monter();
        var echange = new Echange(AuteurEchange.Agent, "Bonjour", Maintenant);
        var creation = Passage(null, Statut.Brouillon, "Agent Fictif", CreeeLe);
        banc.Depot.Placer(Brouillon(), Historique(echange), ListeImmuable<Transition>.Vide.Ajouter(creation));

        var consultee = Valeur(await banc.Service.Consulter(Id, Annulation));

        Assert.Equal(Brouillon(), consultee.Fiche);
        Assert.Equal(new[] { echange }, consultee.Historique.ToArray());
        Assert.Equal(new[] { creation }, consultee.Transitions.ToArray());
    }

    [Fact]
    public async Task Consulter_une_fiche_absente_refuse()
    {
        Assert.Equal("fiche.introuvable", CodeRefus(await Monter().Service.Consulter(IdAbsent, Annulation)));
    }

    // --- Tour de dialogue ---

    [Fact]
    public async Task EnvoyerMessage_ecrit_la_fiche_fusionnee_et_les_deux_echanges_ensemble()
    {
        var banc = Monter(ReponseAvecPropositions);
        banc.Depot.Placer(Brouillon());
        banc.Horloge.Pas = TimeSpan.FromSeconds(4);

        var tour = Valeur(await banc.Service.EnvoyerMessage(Id, " Bonjour ", Annulation));

        Assert.Equal("Quel est le processus actuel ?", tour.Message);
        Assert.Equal("Outil de contrôle", tour.Fiche.ChampDe(Rubrique.Application).PropositionEnAttente!.Valeur);
        Assert.Equal(1, banc.Depot.Enregistrements);
        Assert.Equal(tour.Fiche, banc.Depot.FicheDe(Id));
        var historique = banc.Depot.HistoriqueDe(Id);
        Assert.Equal(
            new[]
            {
                new Echange(AuteurEchange.Agent, "Bonjour", Maintenant),
                new Echange(AuteurEchange.IA, "Quel est le processus actuel ?", Maintenant.AddSeconds(4)),
            },
            historique.ToArray());
        var contexte = Assert.Single(banc.Assistant.Recus);
        Assert.Equal("Bonjour", contexte.Message);
        Assert.Equal("application", contexte.ProchaineRubrique);
        Assert.Empty(banc.Depot.TransitionsDe(Id));
    }

    [Fact]
    public async Task EnvoyerMessage_transmet_l_historique_existant_a_l_IA_et_l_allonge()
    {
        var banc = Monter(ReponseSansProposition);
        var ancien = new Echange(AuteurEchange.IA, "Bienvenue", CreeeLe);
        banc.Depot.Placer(Brouillon(), Historique(ancien));

        Valeur(await banc.Service.EnvoyerMessage(Id, "Bonjour", Annulation));

        Assert.Equal(new[] { ancien }, Assert.Single(banc.Assistant.Recus).Historique.ToArray());
        Assert.Equal(3, banc.Depot.HistoriqueDe(Id).Count);
    }

    [Fact]
    public async Task EnvoyerMessage_reponse_illisible_n_ecrit_rien()
    {
        var banc = Monter("pas du json");
        banc.Depot.Placer(Brouillon());

        var resultat = await banc.Service.EnvoyerMessage(Id, "Bonjour", Annulation);

        Assert.Equal("reponseIA.jsonInvalide", CodeRefus(resultat));
        Assert.Single(banc.Assistant.Recus);
        Assert.Equal(0, banc.Depot.Enregistrements);
        Assert.Equal(Brouillon(), banc.Depot.FicheDe(Id));
        Assert.Empty(banc.Depot.HistoriqueDe(Id));
    }

    [Fact]
    public async Task EnvoyerMessage_refuse_d_avance_n_appelle_pas_l_IA()
    {
        var banc = Monter();
        banc.Depot.Placer(Soumise());

        Assert.Equal("message.vide", CodeRefus(await banc.Service.EnvoyerMessage(Id, "  ", Annulation)));
        Assert.Equal("fiche.introuvable", CodeRefus(await banc.Service.EnvoyerMessage(IdAbsent, "Bonjour", Annulation)));
        Assert.Equal("fiche.nonModifiable", CodeRefus(await banc.Service.EnvoyerMessage(Id, "Bonjour", Annulation)));
        Assert.Empty(banc.Assistant.Recus);
        Assert.Equal(0, banc.Depot.Enregistrements);
    }

    [Fact]
    public async Task EnvoyerMessage_une_panne_de_l_IA_se_propage_sans_rien_ecrire()
    {
        var banc = Monter();
        banc.Depot.Placer(Brouillon());
        banc.Assistant.Panne = new InvalidOperationException("panne simulée");

        // Délégué typé : pas d'ambiguïté entre les surcharges Task et ValueTask de ThrowsAsync.
        Func<Task> appel = async () => await banc.Service.EnvoyerMessage(Id, "Bonjour", Annulation);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(appel);

        Assert.Equal("panne simulée", exception.Message);
        Assert.Equal(0, banc.Depot.Enregistrements);
        Assert.Empty(banc.Depot.HistoriqueDe(Id));
    }

    // --- Saisie, propositions ---

    [Fact]
    public async Task ModifierChamp_par_code_casse_ignoree_enregistre_la_fiche()
    {
        var banc = Monter();
        banc.Depot.Placer(Brouillon());

        var fiche = Valeur(await banc.Service.ModifierChamp(Id, "INTITULE", " Titre ", Annulation));

        Assert.Equal(new Champ("Titre", Origine.Manuelle, null), fiche.ChampDe(Rubrique.Intitule));
        Assert.Equal(fiche, banc.Depot.FicheDe(Id));
        Assert.Equal(1, banc.Depot.Enregistrements);
        Assert.Empty(banc.Depot.TransitionsDe(Id));
    }

    [Fact]
    public async Task ModifierChamp_refuse_sans_ecrire()
    {
        var banc = Monter();
        banc.Depot.Placer(Brouillon());

        Assert.Equal("rubrique.inconnue", CodeRefus(await banc.Service.ModifierChamp(Id, "nulle", "x", Annulation)));
        Assert.Equal("fiche.introuvable", CodeRefus(await banc.Service.ModifierChamp(IdAbsent, "intitule", "x", Annulation)));
        Assert.Equal(0, banc.Depot.Enregistrements);
    }

    [Fact]
    public async Task AccepterProposition_retient_la_valeur_proposee()
    {
        var banc = Monter();
        banc.Depot.Placer(Proposer(Brouillon(), Rubrique.Application, "Outil"));

        var fiche = Valeur(await banc.Service.AccepterProposition(Id, "application", Annulation));

        Assert.Equal(new Champ("Outil", Origine.AccepteeIA, null), fiche.ChampDe(Rubrique.Application));
        Assert.Equal(fiche, banc.Depot.FicheDe(Id));
    }

    [Fact]
    public async Task RejeterProposition_retire_la_proposition()
    {
        var banc = Monter();
        banc.Depot.Placer(Proposer(Brouillon(), Rubrique.Application, "Outil"));

        var fiche = Valeur(await banc.Service.RejeterProposition(Id, "application", Annulation));

        Assert.Equal(Champ.Initial, fiche.ChampDe(Rubrique.Application));
        Assert.Equal(fiche, banc.Depot.FicheDe(Id));
    }

    [Fact]
    public async Task Accepter_ou_rejeter_sans_proposition_refuse_sans_ecrire()
    {
        var banc = Monter();
        banc.Depot.Placer(Brouillon());

        Assert.Equal("proposition.absente", CodeRefus(await banc.Service.AccepterProposition(Id, "intitule", Annulation)));
        Assert.Equal("proposition.absente", CodeRefus(await banc.Service.RejeterProposition(Id, "intitule", Annulation)));
        Assert.Equal("rubrique.inconnue", CodeRefus(await banc.Service.AccepterProposition(Id, "nulle", Annulation)));
        Assert.Equal("rubrique.inconnue", CodeRefus(await banc.Service.RejeterProposition(Id, "nulle", Annulation)));
        Assert.Equal(0, banc.Depot.Enregistrements);
    }

    // --- Cycle ---

    [Fact]
    public async Task Soumettre_lit_le_dernier_numero_et_numerote_dans_le_fuseau()
    {
        var banc = Monter();
        banc.Depot.Placer(Complete());
        banc.Depot.DernierImpose = "EB-2026-0042";
        banc.Horloge.Maintenant = VeilleDuNouvelAn;

        var fiche = Valeur(await banc.Service.Soumettre(Id, true, Annulation));

        Assert.Equal("EB-2027-0001", fiche.Numero);
        Assert.Equal(VeilleDuNouvelAn, fiche.ConfirmeeLe);
        Assert.Equal(fiche, banc.Depot.FicheDe(Id));
        Assert.Equal(
            new[] { Passage(Statut.Brouillon, Statut.Soumise, "Agent Fictif", VeilleDuNouvelAn) },
            banc.Depot.TransitionsDe(Id).ToArray());
    }

    [Fact]
    public async Task Soumettre_une_fiche_incomplete_n_ecrit_rien()
    {
        var banc = Monter();
        banc.Depot.Placer(Brouillon());

        Assert.Equal("soumission.incomplete", CodeRefus(await banc.Service.Soumettre(Id, true, Annulation)));
        Assert.Equal(0, banc.Depot.Enregistrements);
        Assert.Empty(banc.Depot.TransitionsDe(Id));
    }

    [Fact]
    public async Task Decider_enregistre_la_decision_du_valideur_a_l_heure_de_l_horloge()
    {
        var banc = Monter();
        banc.Depot.Placer(Soumise());
        banc.Horloge.Maintenant = VeilleDuNouvelAn;

        var fiche = Valeur(await banc.Service.Decider(Id, TypeDecision.Refuser, "hors budget", Annulation));

        Assert.Equal(Statut.Refusee, fiche.Statut);
        Assert.Equal(
            new[] { new Decision(TypeDecision.Refuser, "Chef Fictif", VeilleDuNouvelAn, "hors budget") },
            fiche.Decisions.ToArray());
        Assert.Equal(fiche, banc.Depot.FicheDe(Id));
        Assert.Equal(
            new[] { Passage(Statut.Soumise, Statut.Refusee, "Chef Fictif", VeilleDuNouvelAn) },
            banc.Depot.TransitionsDe(Id).ToArray());
    }

    [Fact]
    public async Task Reprendre_une_fiche_a_completer_et_refus_hors_de_ce_statut()
    {
        var banc = Monter();
        var aCompleter = Valeur(Cycle.Decider(Soumise(), TypeDecision.DemanderComplements, "Chef Fictif", "préciser", Maintenant));
        banc.Depot.Placer(aCompleter);

        var fiche = Valeur(await banc.Service.Reprendre(Id, Annulation));

        Assert.Equal(Statut.Brouillon, fiche.Statut);
        Assert.Null(fiche.ConfirmeeLe);
        Assert.Equal(1, banc.Depot.Enregistrements);
        Assert.Equal("cycle.transitionInterdite", CodeRefus(await banc.Service.Reprendre(Id, Annulation)));
        Assert.Equal(1, banc.Depot.Enregistrements);
        Assert.Equal(
            new[] { Passage(Statut.ACompleter, Statut.Brouillon, "Agent Fictif", Maintenant) },
            banc.Depot.TransitionsDe(Id).ToArray());
    }

    // --- Document ---

    [Fact]
    public async Task Telecharger_une_fiche_validee_rend_le_document_nomme_par_son_numero()
    {
        var banc = Monter();
        banc.Depot.Placer(Validee());

        var document = Valeur(await banc.Service.Telecharger(Id, Annulation));

        Assert.Equal("EB-2026-0001.docx", document.NomFichier);
        Assert.Equal(Encoding.UTF8.GetBytes("docx de EB-2026-0001"), document.Contenu);
        Assert.Equal(ContenuDocument.Construire(Validee(), FuseauPlusUneHeure), Assert.Single(banc.Generateur.Recus));
    }

    [Fact]
    public async Task Telecharger_date_le_document_dans_le_fuseau_du_service()
    {
        var banc = Monter();
        banc.Depot.Placer(Validee() with { EnTete = EnTeteFictif with { CreeeLe = VeilleDuNouvelAn } });

        Valeur(await banc.Service.Telecharger(Id, Annulation));

        Assert.Equal("01/01/2027", Assert.Single(banc.Generateur.Recus).DateDemande);
    }

    [Fact]
    public async Task Telecharger_refuse_sans_generer()
    {
        var banc = Monter();
        banc.Depot.Placer(Soumise());

        Assert.Equal("document.nonValidee", CodeRefus(await banc.Service.Telecharger(Id, Annulation)));
        Assert.Equal("fiche.introuvable", CodeRefus(await banc.Service.Telecharger(IdAbsent, Annulation)));
        Assert.Empty(banc.Generateur.Recus);
    }

    // --- Parcours du critère de fin de la V1 (plan §1), attentes terminées puis différées ---

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Parcours_complet_avec_retour_pour_complement(bool differe)
    {
        var banc = Monter(differe, ReponseAvecPropositions);
        var service = banc.Service;

        Assert.Equal("agent.fictif", Assert.Single(await service.ListerAgents(Annulation)).Identifiant);
        Assert.Equal(Id, Valeur(await service.CreerBrouillon("agent.fictif", Annulation)).Id);

        var tour = Valeur(await service.EnvoyerMessage(
            Id, "contrôle des plates-formes e-commerce et prélèvements à distance", Annulation));
        Assert.Equal(2, Completude.PropositionsEnAttente(tour.Fiche).Length);
        Valeur(await service.AccepterProposition(Id, "application", Annulation));
        Valeur(await service.RejeterProposition(Id, "intitule", Annulation));
        foreach (var definition in Catalogue.Toutes)
        {
            if (definition.Obligatoire && definition.Rubrique != Rubrique.Application)
            {
                Valeur(await service.ModifierChamp(Id, definition.Code, "valeur de " + definition.Code, Annulation));
            }
        }

        Assert.Equal("EB-2026-0001", Valeur(await service.Soumettre(Id, true, Annulation)).Numero);
        Assert.Equal(
            Statut.ACompleter,
            Valeur(await service.Decider(Id, TypeDecision.DemanderComplements, "Préciser les critères", Annulation)).Statut);
        Assert.Equal(Statut.Brouillon, Valeur(await service.Reprendre(Id, Annulation)).Statut);
        Valeur(await service.ModifierChamp(Id, "criteresReussite", "Délai de contrôle divisé par deux", Annulation));
        Assert.Equal("EB-2026-0001", Valeur(await service.Soumettre(Id, true, Annulation)).Numero);
        Assert.Equal(Statut.Validee, Valeur(await service.Decider(Id, TypeDecision.Valider, null, Annulation)).Statut);

        Assert.Equal("EB-2026-0001.docx", Valeur(await service.Telecharger(Id, Annulation)).NomFichier);
        var resume = Assert.Single(await service.Lister(Annulation));
        Assert.Equal("EB-2026-0001", resume.Numero);
        Assert.Equal(Statut.Validee, resume.Statut);
        var consultee = Valeur(await service.Consulter(Id, Annulation));
        Assert.Equal(2, consultee.Historique.Count);
        Assert.Equal(
            new[] { TypeDecision.DemanderComplements, TypeDecision.Valider },
            consultee.Fiche.Decisions.Select(decision => decision.Type).ToArray());
        Assert.Equal(19, banc.Depot.Enregistrements);
        // Cycle de vie (DEB-Q74) : six transitions, seules les étapes du cycle ; horloge du banc fixe.
        Assert.Equal(
            new[]
            {
                Passage(null, Statut.Brouillon, "Agent Fictif", Maintenant),
                Passage(Statut.Brouillon, Statut.Soumise, "Agent Fictif", Maintenant),
                Passage(Statut.Soumise, Statut.ACompleter, "Chef Fictif", Maintenant),
                Passage(Statut.ACompleter, Statut.Brouillon, "Agent Fictif", Maintenant),
                Passage(Statut.Brouillon, Statut.Soumise, "Agent Fictif", Maintenant),
                Passage(Statut.Soumise, Statut.Validee, "Chef Fictif", Maintenant),
            },
            consultee.Transitions.ToArray());
        Assert.Equal(
            new[] { Etape.Creation, Etape.Soumission, Etape.DemandeComplements, Etape.Reprise, Etape.Soumission, Etape.Validation },
            consultee.Transitions.Select(transition => Transitions.EtapeDe(transition)).ToArray());
    }
}
