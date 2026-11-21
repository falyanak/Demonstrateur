using System.Text.Json;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Web.Api;

namespace ExpressionBesoin.Web.Tests.Api;

/// <summary>
/// Contrat JSON de la fiche (DEB-Q43) : la sérialisation de l'API est comparée À L'OCTET au fichier de référence du
/// ClientApp, produit par une réplique Python et lu aussi par Vitest (tests/contrats.test.ts) : un écart d'un côté casse
/// les deux.
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

    /// <summary>Fichier de référence, trouvé en remontant jusqu'à la solution (jamais « le premier trouvé »).</summary>
    internal static string CheminReference()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !File.Exists(Path.Combine(dossier.FullName, "ExpressionBesoin.slnx")))
        {
            dossier = dossier.Parent;
        }

        Assert.NotNull(dossier);
        return Path.Combine(dossier.FullName, "src", "ExpressionBesoin.Web", "ClientApp", "tests", "contrats", "fiche-reference.json");
    }

    [Fact]
    public void Fiche_de_reference_serialisee_a_l_octet_comme_le_fichier_partage_avec_Vitest() =>
        Assert.Equal(
            File.ReadAllBytes(CheminReference()),
            JsonSerializer.SerializeToUtf8Bytes(FicheVue.De(Reference()), ContratJson.Options));

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
            ListeImmuable<Echange>.De([new Echange(AuteurEchange.Agent, "Bonjour", le), new Echange(AuteurEchange.IA, "Question ?", le)])));
        Assert.Equal(
            new[] { new EchangeVue("Agent", "Bonjour", le), new EchangeVue("IA", "Question ?", le) },
            consultation.Historique.ToArray());
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
