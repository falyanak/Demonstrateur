using ExpressionBesoin.Web.Sante;

namespace ExpressionBesoin.Web.Tests;

public sealed class DiagnosticSanteTests
{
    private static readonly DateTimeOffset Demarrage = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    // Attendus calculés à la main : ticks écoulés / 10 000 000, tronqués ; négatif ou nul -> 0.
    [Theory]
    [InlineData(0L, 0L)]
    [InlineData(9_999_999L, 0L)]
    [InlineData(10_000_000L, 1L)]
    [InlineData(15_000_000L, 1L)]
    [InlineData(19_999_999L, 1L)]
    [InlineData(36_000_000_000L, 3600L)]
    [InlineData(-1L, 0L)]
    [InlineData(-50_000_000L, 0L)]
    public void Duree_en_secondes_tronquee_jamais_negative(long ticksEcoules, long attendu)
    {
        var etat = DiagnosticSante.Decrire("1.0.0", Demarrage, Demarrage.AddTicks(ticksEcoules));
        Assert.Equal(attendu, etat.DureeSecondes);
    }

    [Fact]
    public void Duree_ne_depend_pas_du_fuseau_des_instants()
    {
        var maintenant = new DateTimeOffset(2026, 11, 20, 12, 0, 30, TimeSpan.FromHours(2));
        Assert.Equal(30L, DiagnosticSante.Decrire("1.0.0", Demarrage, maintenant).DureeSecondes);
    }

    [Theory]
    [InlineData("0.1.0+56388a3", "0.1.0+56388a3")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData(null, "inconnue")]
    [InlineData("", "inconnue")]
    [InlineData("   ", "inconnue")]
    public void Version_absente_ou_vide_devient_inconnue(string? version, string attendu)
    {
        Assert.Equal(attendu, DiagnosticSante.Decrire(version, Demarrage, Demarrage).Version);
    }

    [Fact]
    public void Etat_est_ok_et_demarrage_recopie()
    {
        var etat = DiagnosticSante.Decrire("1.0.0", Demarrage, Demarrage.AddSeconds(5));
        Assert.Equal("ok", etat.Etat);
        Assert.Equal(Demarrage, etat.Demarrage);
    }
}
