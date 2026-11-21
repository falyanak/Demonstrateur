using System.Text.Json;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Web.Api;

namespace ExpressionBesoin.Web.Tests.Api;

/// <summary>
/// Contrat JSON de la fiche (DEB-Q43), de la consultation et du tour (DEB-Q52), de la liste (F3) : la sérialisation de l'API est comparée
/// À L'OCTET aux fichiers de référence du ClientApp, produits par une réplique Python et lus aussi par Vitest
/// (tests/contrats.test.ts) : un écart d'un côté casse les deux.
/// </summary>
public sealed class FicheVueTests
{
    internal static readonly DateTimeOffset Creation = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Fiche de référence : construite champ par champ, sans passer par les règles du Domaine.</summary>
    internal static FicheBesoin Reference()
    {
        var champs = Enumerable.Repeat(Champ.Initial, Catalogue.Toutes.Length).ToArray();
        champs[(int)Rubrique.Application] = new Champ("SICCRF", Origine.AccepteeIA, null);
        champs[(int)Rubrique.Intitule] = new Champ("Contrôle des plates-formes e-commerce", Origine.Manuelle, null);
        champs[(int)Rubrique.SituationActuelle] =
            new Champ(null, Origine.Vide, new Proposition("Contrôles <manuels> & \"à distance\"", "Reprend la réponse de l'agent."));
        return new FicheBesoin(
            new Guid("6f1c2b3a-0000-4000-8000-0000000000c1"),
            "EB-2026-0007",
            Statut.ACompleter,
            new EnTete("Camille Martin", "Bureau 1A – Commerce électronique", "Dominique Bernard", Creation),
            ListeImmuable<Champ>.De(champs),
            new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero),
            ListeImmuable<Decision>.De([
                new Decision(TypeDecision.DemanderComplements, "Dominique Bernard", new DateTimeOffset(2026, 11, 20, 11, 30, 0, TimeSpan.Zero), "Préciser les critères."),
            ]));
    }

    /// <summary>
    /// Consultation de référence : la fiche de référence, deux échanges (retour à la ligne compris) et son cycle de vie
    /// (création, soumission, demande de compléments ; DEB-Q77).
    /// </summary>
    internal static FicheConsultee ConsultationReference() =>
        new(
            Reference(),
            ListeImmuable<Echange>.De([
                new Echange(AuteurEchange.Agent, "Contrôle des plates-formes e-commerce\net prélèvements à distance", new DateTimeOffset(2026, 11, 20, 9, 5, 0, TimeSpan.Zero)),
                new Echange(AuteurEchange.IA, "Quelle application est concernée ?", new DateTimeOffset(2026, 11, 20, 9, 5, 1, TimeSpan.Zero)),
            ]),
            ListeImmuable<Transition>.De([
                new Transition(null, Statut.Brouillon, "Camille Martin", Creation),
                new Transition(Statut.Brouillon, Statut.Soumise, "Camille Martin", new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero)),
                new Transition(Statut.Soumise, Statut.ACompleter, "Dominique Bernard", new DateTimeOffset(2026, 11, 20, 11, 30, 0, TimeSpan.Zero)),
            ]));

    /// <summary>Tour de référence : hors périmètre, deux propositions écartées, la fiche de référence.</summary>
    internal static TourDialogue TourReference() =>
        new(
            "Quelle application est concernée ? Précisez « le service » s'il y a lieu.",
            true,
            ListeImmuable<PropositionEcartee>.De([
                new PropositionEcartee(1, "inconnue", MotifEcart.RubriqueInconnue),
                new PropositionEcartee(2, "intitule", MotifEcart.Doublon),
            ]),
            Reference());

    /// <summary>
    /// Liste de référence (F3) : le résumé de la fiche de référence, puis un brouillon sans numéro ni intitulé, créé à
    /// 23 h 30 UTC (le lendemain à Paris : la date affichée est vérifiée par Vitest).
    /// </summary>
    internal static ResumeVue[] ListeReference() =>
    [
        ResumeVue.De(ResumeFiche.De(Reference())),
        ResumeVue.De(new ResumeFiche(
            new Guid("6f1c2b3a-0000-4000-8000-0000000000c2"),
            null,
            null,
            "Claude Petit-Lefèvre",
            Statut.Brouillon,
            new DateTimeOffset(2026, 11, 20, 23, 30, 0, TimeSpan.Zero),
            null)),
    ];

    /// <summary>Fichier de référence, trouvé en remontant jusqu'à la solution (jamais « le premier trouvé »).</summary>
    internal static string CheminReference(string nom = "fiche-reference.json")
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !File.Exists(Path.Combine(dossier.FullName, "ExpressionBesoin.slnx")))
        {
            dossier = dossier.Parent;
        }

        Assert.NotNull(dossier);
        return Path.Combine(dossier.FullName, "src", "ExpressionBesoin.Web", "ClientApp", "tests", "contrats", nom);
    }

    [Fact]
    public void Fiche_de_reference_serialisee_a_l_octet_comme_le_fichier_partage_avec_Vitest() =>
        Assert.Equal(
            File.ReadAllBytes(CheminReference()),
            JsonSerializer.SerializeToUtf8Bytes(FicheVue.De(Reference()), ContratJson.Options));

    [Fact]
    public void Consultation_de_reference_serialisee_a_l_octet_comme_le_fichier_partage_avec_Vitest() =>
        Assert.Equal(
            File.ReadAllBytes(CheminReference("consultation-reference.json")),
            JsonSerializer.SerializeToUtf8Bytes(ConsultationVue.De(ConsultationReference()), ContratJson.Options));

    [Fact]
    public void Tour_de_reference_serialise_a_l_octet_comme_le_fichier_partage_avec_Vitest() =>
        Assert.Equal(
            File.ReadAllBytes(CheminReference("tour-reference.json")),
            JsonSerializer.SerializeToUtf8Bytes(TourVue.De(TourReference()), ContratJson.Options));

    [Fact]
    public void Liste_de_reference_serialisee_a_l_octet_comme_le_fichier_partage_avec_Vitest() =>
        Assert.Equal(
            File.ReadAllBytes(CheminReference("liste-reference.json")),
            JsonSerializer.SerializeToUtf8Bytes(ListeReference(), ContratJson.Options));

    [Fact]
    public void Fiche_complete_et_sans_question_restante()
    {
        var champs = Catalogue.Toutes
            .Select(definition => definition.Obligatoire ? new Champ("v", Origine.Manuelle, null) : Champ.Initial)
            .ToArray();
        var vue = FicheVue.De(Reference() with { Statut = Statut.Brouillon, Champs = ListeImmuable<Champ>.De(champs) });
        Assert.Equal((10, 10, true, true, (string?)null), (vue.Obligatoires, vue.Remplies, vue.Complete, vue.Modifiable, vue.ProchaineRubrique));
    }

    [Fact]
    public void Consultation_tour_resume_et_agent_projetes()
    {
        var fiche = Reference();
        var le = new DateTimeOffset(2026, 11, 20, 9, 5, 0, TimeSpan.Zero);
        var consultation = ConsultationVue.De(new FicheConsultee(
            fiche,
            ListeImmuable<Echange>.De([new Echange(AuteurEchange.Agent, "Bonjour", le), new Echange(AuteurEchange.IA, "Question ?", le)]),
            ListeImmuable<Transition>.De([
                new Transition(null, Statut.Brouillon, "A", le),
                new Transition(Statut.Soumise, Statut.Refusee, "V", le),
                new Transition(Statut.ACompleter, Statut.Brouillon, "A", le),
            ])));
        Assert.Equal(
            new[] { new EchangeVue("Agent", "Bonjour", le), new EchangeVue("IA", "Question ?", le) },
            consultation.Historique.ToArray());
        Assert.Equal(
            new[]
            {
                new TransitionVue("Creation", null, "Brouillon", "A", le),
                new TransitionVue("Refus", "Soumise", "Refusee", "V", le),
                new TransitionVue("Reprise", "ACompleter", "Brouillon", "A", le),
            },
            consultation.Cycle.ToArray());
        var vue = consultation.Cycle[1];
        Assert.Equal(("Refus", "Soumise", "Refusee", "V", le), (vue.Etape, vue.Avant, vue.Apres, vue.Acteur, vue.Le));
        Assert.Equal("EB-2026-0007", consultation.Fiche.Numero);

        var tour = TourVue.De(new TourDialogue(
            "Question ?",
            true,
            ListeImmuable<PropositionEcartee>.De([new PropositionEcartee(1, "inconnue", MotifEcart.RubriqueInconnue)]),
            fiche));
        Assert.Equal(("Question ?", true, "ACompleter"), (tour.Message, tour.HorsPerimetre, tour.Fiche.Statut));
        Assert.Equal(new[] { new EcartVue(1, "inconnue", "RubriqueInconnue") }, tour.Ecartees.ToArray());

        Assert.Equal(
            new ResumeVue(fiche.Id, "EB-2026-0007", "Contrôle des plates-formes e-commerce", "Camille Martin", "ACompleter", Creation, fiche.ConfirmeeLe),
            ResumeVue.De(ResumeFiche.De(fiche)));
        Assert.Equal(new AgentVue("a", "N", "B", "V"), AgentVue.De(new Agent("a", "N", "B", "V")));
    }
}
