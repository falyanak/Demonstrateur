using System.Globalization;
using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

public sealed class ContenuDocumentTests
{
    /// <summary>21/11/2026 14 h UTC : instant de la décision, un autre jour que la création et la confirmation.</summary>
    private static readonly DateTimeOffset PlusTard = new(2026, 11, 21, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Fuseau fixe à + 1 h, indépendant des fuseaux du système (essai déterministe).</summary>
    private static readonly TimeZoneInfo PlusUneHeure =
        TimeZoneInfo.CreateCustomTimeZone("Essai+1", TimeSpan.FromHours(1), "Essai + 1 h", "Essai + 1 h");

    private static FicheBesoin Decidee(FicheBesoin soumise, TypeDecision type, string? commentaire, DateTimeOffset le) =>
        Valeur(Cycle.Decider(soumise, type, "Chef Fictif", commentaire, le));

    /// <summary>Textes attendus des 17 rubriques de <see cref="Essais.Complete"/>, écrits à la main.</summary>
    private static readonly string[] RubriquesDeComplete =
    [
        "valeur de application",
        "valeur de intitule",
        "valeur de resume",
        "valeur de situationActuelle",
        "valeur de difficultes",
        "Non renseigné",
        "valeur de resultatRecherche",
        "valeur de criteresReussite",
        "Non renseigné",
        "valeur de utilisateursConcernes",
        "valeur de processusMetier",
        "Non renseigné",
        "valeur de beneficesAttendus",
        "Non renseigné",
        "Non renseigné",
        "Non renseigné",
        "Non renseigné",
    ];

    // --- Fiche validée : chaque champ ---

    [Fact]
    public void Construire_une_fiche_validee_champ_par_champ()
    {
        var contenu = ContenuDocument.Construire(Decidee(Soumise(), TypeDecision.Valider, null, PlusTard), PlusUneHeure);

        Assert.Equal("EB-2026-0001", contenu.Numero);
        Assert.Equal("Agent Fictif", contenu.Demandeur);
        Assert.Equal("20/11/2026", contenu.DateDemande);
        Assert.Equal("Bureau 4A", contenu.Bureau);
        Assert.Equal("Chef Fictif", contenu.Valideur);
        Assert.Equal(RubriquesDeComplete, contenu.Rubriques.ToArray());
        Assert.True(contenu.Confirmee);
        Assert.Equal("20/11/2026", contenu.DateConfirmation);
        Assert.Equal("Validée", contenu.DecisionChef);
        Assert.Equal("Chef Fictif, le 21/11/2026", contenu.ObservationsChef);
        Assert.Equal(CreeeLe, contenu.CreeeLe);
        Assert.Equal(PlusTard, contenu.ModifieeLe);
    }

    [Fact]
    public void ValeurDe_lit_la_rubrique_par_son_ordinal()
    {
        var contenu = ContenuDocument.Construire(Complete(), PlusUneHeure);

        Assert.Equal("valeur de situationActuelle", contenu.ValeurDe(Rubrique.SituationActuelle));
        Assert.Equal("Non renseigné", contenu.ValeurDe(Rubrique.PointsAClarifier));
        Assert.Equal(17, contenu.Rubriques.Count);
    }

    [Fact]
    public void Deux_constructions_de_la_meme_fiche_sont_egales()
    {
        var fiche = Decidee(Soumise(), TypeDecision.Valider, null, PlusTard);

        Assert.Equal(ContenuDocument.Construire(fiche, PlusUneHeure), ContenuDocument.Construire(fiche, PlusUneHeure));
    }

    // --- Rubriques : seule la valeur retenue compte ---

    [Fact]
    public void Une_proposition_en_attente_n_est_pas_ecrite_dans_le_document()
    {
        var fiche = Proposer(Saisir(Brouillon(), Rubrique.Intitule, "Saisi"), Rubrique.Application, "Proposé");

        var contenu = ContenuDocument.Construire(fiche, PlusUneHeure);

        Assert.Equal("Non renseigné", contenu.ValeurDe(Rubrique.Application));
        Assert.Equal("Saisi", contenu.ValeurDe(Rubrique.Intitule));
    }

    [Fact]
    public void Une_proposition_acceptee_est_ecrite()
    {
        var fiche = Valeur(Fiche.AccepterProposition(Proposer(Brouillon(), Rubrique.Application, "Accepté"), Rubrique.Application));

        Assert.Equal("Accepté", ContenuDocument.Construire(fiche, PlusUneHeure).ValeurDe(Rubrique.Application));
    }

    // --- Fiche en cours : valeurs par défaut ---

    [Fact]
    public void Un_brouillon_n_a_ni_numero_ni_confirmation_ni_decision()
    {
        var contenu = ContenuDocument.Construire(Brouillon(), PlusUneHeure);

        Assert.Equal("Non attribué", contenu.Numero);
        Assert.False(contenu.Confirmee);
        Assert.Equal("Non confirmée", contenu.DateConfirmation);
        Assert.Equal("À valider", contenu.DecisionChef);
        Assert.Equal("", contenu.ObservationsChef);
        Assert.Equal(CreeeLe, contenu.ModifieeLe);
        Assert.All(contenu.Rubriques, texte => Assert.Equal("Non renseigné", texte));
    }

    [Fact]
    public void Une_fiche_soumise_sans_decision_est_modifiee_a_sa_confirmation()
    {
        var contenu = ContenuDocument.Construire(Soumise(), PlusUneHeure);

        Assert.True(contenu.Confirmee);
        Assert.Equal("À valider", contenu.DecisionChef);
        Assert.Equal("", contenu.ObservationsChef);
        Assert.Equal(Maintenant, contenu.ModifieeLe);
    }

    // --- Décisions (DEB-Q18 : la dernière) ---

    [Fact]
    public void Une_demande_de_complements_affiche_son_commentaire()
    {
        var contenu = ContenuDocument.Construire(
            Decidee(Soumise(), TypeDecision.DemanderComplements, "Préciser le volume", PlusTard),
            PlusUneHeure);

        Assert.Equal("À compléter", contenu.DecisionChef);
        Assert.Equal("Chef Fictif, le 21/11/2026 : Préciser le volume", contenu.ObservationsChef);
    }

    [Fact]
    public void La_derniere_decision_l_emporte_apres_un_retour_pour_complement()
    {
        var retour = Decidee(Soumise(), TypeDecision.DemanderComplements, "Préciser le volume", PlusTard);
        var resoumise = Valeur(Cycle.Soumettre(Valeur(Cycle.Reprendre(retour)), true, "EB-2026-0099", PlusTard.AddHours(1)));
        var refusee = Decidee(resoumise, TypeDecision.Refuser, "Hors priorité", PlusTard.AddDays(1));

        var contenu = ContenuDocument.Construire(refusee, PlusUneHeure);

        Assert.Equal("EB-2026-0001", contenu.Numero);
        Assert.Equal("Refusée", contenu.DecisionChef);
        Assert.Equal("Chef Fictif, le 22/11/2026 : Hors priorité", contenu.ObservationsChef);
        Assert.Equal("21/11/2026", contenu.DateConfirmation);
        Assert.Equal(PlusTard.AddDays(1), contenu.ModifieeLe);
    }

    [Theory]
    [InlineData(TypeDecision.Valider, "Validée")]
    [InlineData(TypeDecision.DemanderComplements, "À compléter")]
    [InlineData(TypeDecision.Refuser, "Refusée")]
    public void Libelle_suit_le_modele_FEB(TypeDecision type, string attendu)
    {
        Assert.Equal(attendu, ContenuDocument.Libelle(type));
    }

    [Fact]
    public void Libelle_d_un_type_inconnu_leve()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ContenuDocument.Libelle((TypeDecision)9));
    }

    // --- Dates : fuseau, culture, UTC ---

    [Fact]
    public void Les_dates_sont_lues_dans_le_fuseau()
    {
        var fiche = Brouillon() with { EnTete = EnTeteFictif with { CreeeLe = new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero) } };

        Assert.Equal("01/01/2027", ContenuDocument.Construire(fiche, PlusUneHeure).DateDemande);
        Assert.Equal("31/12/2026", ContenuDocument.Construire(fiche, TimeZoneInfo.Utc).DateDemande);
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    [InlineData("fr-FR")]
    public void Les_dates_ne_dependent_pas_de_la_culture_courante(string culture)
    {
        var avant = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            var contenu = ContenuDocument.Construire(Decidee(Soumise(), TypeDecision.Valider, null, PlusTard), PlusUneHeure);

            Assert.Equal("20/11/2026", contenu.DateDemande);
            Assert.Equal("Chef Fictif, le 21/11/2026", contenu.ObservationsChef);
        }
        finally
        {
            CultureInfo.CurrentCulture = avant;
        }
    }

    [Fact]
    public void Les_instants_des_metadonnees_sont_en_UTC()
    {
        var creeeLe = new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.FromHours(2));
        var fiche = Brouillon() with { EnTete = EnTeteFictif with { CreeeLe = creeeLe } };

        var contenu = ContenuDocument.Construire(fiche, PlusUneHeure);

        Assert.Equal(TimeSpan.Zero, contenu.CreeeLe.Offset);
        Assert.Equal(TimeSpan.Zero, contenu.ModifieeLe.Offset);
        Assert.Equal(CreeeLe, contenu.CreeeLe);
    }
}
