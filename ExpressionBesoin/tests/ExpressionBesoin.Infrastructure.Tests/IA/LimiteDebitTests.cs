using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>Décision de nouvel essai après un 429 (DEB-Q87) : tables de vérité, attendus écrits à la main.</summary>
public sealed class LimiteDebitTests
{
    [Theory]
    [InlineData(null, NatureLimite.Autre)]
    [InlineData("", NatureLimite.Autre)]
    [InlineData("Too many requests", NatureLimite.Autre)]
    [InlineData("{\"detail\":\"1280000 input tokens per day exceeded (remaining: 0).\"}", NatureLimite.Jour)]
    [InlineData("{\"detail\":\"100 requests per minute exceeded (remaining: 0).\"}", NatureLimite.Minute)]
    [InlineData("Rate limit reached (TPM)", NatureLimite.Minute)]
    [InlineData("Rate limit reached (RPD)", NatureLimite.Jour)]
    [InlineData("Quota PAR JOUR dépassé", NatureLimite.Jour)]
    [InlineData("Daily quota; also 10 per minute", NatureLimite.Jour)]
    [InlineData("limit_per_minute", NatureLimite.Minute)]
    public void Nature_de_la_limite(string? corps, NatureLimite attendue) => Assert.Equal(attendue, LimiteDebit.Nature(corps));

    [Theory]
    [InlineData("per minute", null, 2)]
    [InlineData("per minute", 0, 0)]
    [InlineData("per minute", 4, 4)]
    [InlineData("per minute", 10, 10)]
    [InlineData("per minute", 11, null)]
    [InlineData("per day", 1, null)]
    [InlineData("per day", null, null)]
    [InlineData("Too many requests", 1, null)]
    public void Un_seul_nouvel_essai_pour_une_limite_par_minute_breve(string corps, int? reessayerApres, int? attendue) =>
        Assert.Equal(
            attendue is { } secondes ? (TimeSpan?)TimeSpan.FromSeconds(secondes) : null,
            LimiteDebit.NouvelEssai(corps, reessayerApres is { } apres ? TimeSpan.FromSeconds(apres) : null));

    [Fact]
    public void Bornes_des_attentes() =>
        Assert.Equal((TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)), (LimiteDebit.AttenteMaximale, LimiteDebit.AttenteParDefaut));
}
