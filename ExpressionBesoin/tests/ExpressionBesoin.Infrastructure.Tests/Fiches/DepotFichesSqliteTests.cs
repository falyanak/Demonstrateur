using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.Fiches;
using Microsoft.Data.Sqlite;
using static ExpressionBesoin.Infrastructure.Tests.Essais;

namespace ExpressionBesoin.Infrastructure.Tests.Fiches;

/// <summary>Adaptateur SQLite sur le schéma de production (fichier temporaire) : ce que la coquille ajoute au codec.</summary>
public sealed class DepotFichesSqliteTests
{
    [Fact]
    public async Task Base_vide_aucune_fiche_aucun_echange_aucun_numero()
    {
        using var essai = new BaseTemporaire();

        Assert.Null(await essai.Depot.Lire(IdReference, Annulation));
        Assert.Empty(await essai.Depot.LireHistorique(IdReference, Annulation));
        Assert.Empty(await essai.Depot.Lister(Annulation));
        Assert.Null(await essai.Depot.DernierNumero(Annulation));
    }

    [Fact]
    public async Task Enregistrer_puis_relire_rend_la_fiche_et_l_historique_meme_depuis_une_autre_instance()
    {
        using var essai = new BaseTemporaire();
        var fiche = Reprise(1, T0, "EB-2026-0001");
        var echanges = Echanges(EchangeAgent("Contrôler l'e-commerce", T0), EchangeIA("Quel processus ?", T0.AddMinutes(1)));

        await essai.Depot.Enregistrer(fiche, echanges, Annulation);
        var autre = new DepotFichesSqlite(essai.Options);

        Assert.Equal(fiche, await autre.Lire(fiche.Id, Annulation));
        Assert.Equal(echanges, await autre.LireHistorique(fiche.Id, Annulation));
    }

    [Fact]
    public async Task Les_colonnes_stockees_suivent_le_schema_blob_ticks_utc_statut_entier_contenu_du_codec()
    {
        using var essai = new BaseTemporaire();
        var fiche = Reference();

        await essai.Depot.Enregistrer(fiche, Echanges(EchangeIA("Bonjour", T0)), Annulation);

        using var connexion = essai.Brute();
        using var commande = connexion.CreateCommand();
        commande.CommandText = """
            SELECT hex(Id), Numero, Intitule, Demandeur, Statut, CreeeLe, ConfirmeeLe, Contenu,
                   (SELECT hex(FicheId) || '|' || Rang || '|' || Auteur || '|' || Texte || '|' || Le FROM Echanges)
            FROM Fiches;
            """;
        using var lecteur = commande.ExecuteReader();
        Assert.True(lecteur.Read());
        Assert.Equal(OctetsIdReference, lecteur.GetString(0));
        Assert.Equal("EB-2026-0007", lecteur.GetString(1));
        Assert.Equal("Contrôle e-commerce", lecteur.GetString(2));
        Assert.Equal("Camille Martin", lecteur.GetString(3));
        Assert.Equal(3L, lecteur.GetInt64(4)); // Statut.ACompleter
        Assert.Equal(TicksT0, lecteur.GetInt64(5));
        Assert.Equal(639307665305000000L, lecteur.GetInt64(6)); // 10:15:30,5 UTC (réplique Python)
        Assert.Equal(JsonReference, lecteur.GetString(7));
        Assert.Equal(OctetsIdReference + "|1|1|Bonjour|" + TicksT0, lecteur.GetString(8));
        Assert.False(lecteur.Read());
    }

    [Fact]
    public async Task Enregistrer_remplace_la_fiche_et_ajoute_les_echanges_a_la_suite()
    {
        using var essai = new BaseTemporaire();
        var brouillon = Brouillon(1, T0);
        var complete = Complete(1, T0);
        var premier = EchangeAgent("premier", T0);
        var second = EchangeIA("second", T0.AddMinutes(1));
        // Décalage non nul : l'instant est gardé (stocké en ticks UTC), pas le décalage.
        var troisieme = EchangeAgent("troisième", new DateTimeOffset(2026, 11, 20, 10, 2, 0, TimeSpan.FromHours(1)));

        await essai.Depot.Enregistrer(brouillon, Echanges(), Annulation);
        Assert.Empty(await essai.Depot.LireHistorique(brouillon.Id, Annulation));
        await essai.Depot.Enregistrer(brouillon, Echanges(premier, second), Annulation);
        await essai.Depot.Enregistrer(complete, Echanges(troisieme), Annulation);

        Assert.Equal(complete, await essai.Depot.Lire(brouillon.Id, Annulation));
        Assert.Equal(Echanges(premier, second, troisieme), await essai.Depot.LireHistorique(brouillon.Id, Annulation));
        Assert.Equal(T0.AddMinutes(2), (await essai.Depot.LireHistorique(brouillon.Id, Annulation))[2].Le);
        Assert.Equal(1L, essai.Scalaire("SELECT COUNT(*) FROM Fiches;"));
    }

    [Fact]
    public async Task Les_historiques_de_deux_fiches_sont_separes()
    {
        using var essai = new BaseTemporaire();
        var a = Brouillon(1, T0);
        var b = Brouillon(2, T0);

        await essai.Depot.Enregistrer(a, Echanges(EchangeAgent("a1", T0)), Annulation);
        await essai.Depot.Enregistrer(b, Echanges(EchangeAgent("b1", T0)), Annulation);
        await essai.Depot.Enregistrer(a, Echanges(EchangeIA("a2", T0)), Annulation);

        Assert.Equal(Echanges(EchangeAgent("a1", T0), EchangeIA("a2", T0)), await essai.Depot.LireHistorique(a.Id, Annulation));
        Assert.Equal(Echanges(EchangeAgent("b1", T0)), await essai.Depot.LireHistorique(b.Id, Annulation));
    }

    [Fact]
    public async Task Lister_rend_les_resumes_de_la_plus_recente_a_la_plus_ancienne_derniere_inseree_d_abord_a_date_egale()
    {
        using var essai = new BaseTemporaire();
        var a = Brouillon(1, T0);
        var b = Soumise(2, T0.AddHours(2), "EB-2026-0001");
        var c = Validee(3, T0.AddHours(1), "EB-2026-0002");
        var d = Brouillon(4, T0.AddHours(2));
        foreach (var fiche in new[] { a, b, c, d })
        {
            await essai.Depot.Enregistrer(fiche, Echanges(), Annulation);
        }

        var resumes = await essai.Depot.Lister(Annulation);

        Assert.Equal(new[] { ResumeFiche.De(d), ResumeFiche.De(b), ResumeFiche.De(c), ResumeFiche.De(a) }, resumes.ToArray());
        Assert.Equal(new[] { d.Id, b.Id, c.Id, a.Id }, resumes.Select(resume => resume.Id).ToArray());
        Assert.Null(resumes[0].Numero);
        Assert.Null(resumes[0].ConfirmeeLe);
        Assert.Equal("valeur de intitule", resumes[1].Intitule);
        Assert.Equal(Statut.Validee, resumes[2].Statut);
        Assert.Equal(T0.AddHours(2), resumes[2].ConfirmeeLe);
    }

    [Fact]
    public async Task DernierNumero_est_le_plus_grand_en_ordre_ordinal()
    {
        using var essai = new BaseTemporaire();
        await essai.Depot.Enregistrer(Brouillon(1, T0), Echanges(), Annulation);
        await essai.Depot.Enregistrer(Soumise(2, T0, "EB-2026-0002"), Echanges(), Annulation);
        await essai.Depot.Enregistrer(Soumise(3, T0, "EB-2027-0001"), Echanges(), Annulation);
        await essai.Depot.Enregistrer(Soumise(4, T0, "EB-2026-0010"), Echanges(), Annulation);

        Assert.Equal("EB-2027-0001", await essai.Depot.DernierNumero(Annulation));
    }

    [Fact]
    public async Task Un_numero_deja_attribue_leve_et_rien_n_est_ecrit()
    {
        using var essai = new BaseTemporaire();
        await essai.Depot.Enregistrer(Soumise(1, T0, "EB-2026-0001"), Echanges(), Annulation);
        var doublon = Soumise(2, T0, "EB-2026-0001");

        await Assert.ThrowsAsync<SqliteException>(
            async () => await essai.Depot.Enregistrer(doublon, Echanges(EchangeAgent("perdu", T0)), Annulation));

        Assert.Null(await essai.Depot.Lire(doublon.Id, Annulation));
        Assert.Empty(await essai.Depot.LireHistorique(doublon.Id, Annulation));
    }

    [Fact]
    public async Task Un_echec_apres_l_ecriture_de_la_fiche_annule_tout_le_tour()
    {
        // DEB-Q22 : la fiche est écrite, puis le premier échange, avant que le second échoue (texte absent) : la
        // transaction entière est annulée.
        using var essai = new BaseTemporaire();
        var avant = Brouillon(1, T0);
        var historique = Echanges(EchangeAgent("avant", T0));
        await essai.Depot.Enregistrer(avant, historique, Annulation);
        var apres = Complete(1, T0);

        await Assert.ThrowsAnyAsync<Exception>(async () => await essai.Depot.Enregistrer(
            apres, Echanges(EchangeAgent("écrit puis annulé", T0), EchangeIA(null!, T0)), Annulation));

        Assert.Equal(avant, await essai.Depot.Lire(avant.Id, Annulation));
        Assert.Equal(historique, await essai.Depot.LireHistorique(avant.Id, Annulation));
    }

    [Fact]
    public async Task Une_demande_annulee_leve_avant_toute_ecriture()
    {
        using var essai = new BaseTemporaire();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var fiche = Brouillon(1, T0);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await essai.Depot.Lire(fiche.Id, source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await essai.Depot.LireHistorique(fiche.Id, source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await essai.Depot.Enregistrer(fiche, Echanges(), source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await essai.Depot.Lister(source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await essai.Depot.DernierNumero(source.Token));
        Assert.Equal(0L, essai.Scalaire("SELECT COUNT(*) FROM Fiches;"));
    }
}
