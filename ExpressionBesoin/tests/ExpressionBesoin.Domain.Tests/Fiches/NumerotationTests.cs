using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

public sealed class NumerotationTests
{
    [Theory]
    [InlineData(2026, null, "EB-2026-0001")]
    [InlineData(2026, "EB-2026-0001", "EB-2026-0002")]
    [InlineData(2026, "EB-2026-0041", "EB-2026-0042")]
    [InlineData(2026, "EB-2026-0999", "EB-2026-1000")]
    [InlineData(2026, "EB-2026-9998", "EB-2026-9999")]
    [InlineData(2027, "EB-2026-0042", "EB-2027-0001")]
    [InlineData(2027, "EB-0000-0005", "EB-2027-0001")]
    [InlineData(1, null, "EB-0001-0001")]
    [InlineData(9999, null, "EB-9999-0001")]
    public void Numero_suivant(int annee, string? dernier, string attendu)
    {
        Assert.Equal(attendu, Valeur(Numerotation.Suivant(annee, dernier)));
    }

    [Theory]
    [InlineData(0, null, "numerotation.annee")]
    [InlineData(10000, null, "numerotation.annee")]
    [InlineData(-2026, "EB-2026-0001", "numerotation.annee")]
    [InlineData(2025, "EB-2026-0001", "numerotation.anneeAnterieure")]
    [InlineData(2026, "EB-2026-9999", "numerotation.epuisee")]
    [InlineData(2026, "", "numerotation.illisible")]
    [InlineData(2026, "EB-2026-001", "numerotation.illisible")]
    [InlineData(2026, "EB-2026-00001", "numerotation.illisible")]
    [InlineData(2026, "XB-2026-0001", "numerotation.illisible")]
    [InlineData(2026, "EB-2026_0001", "numerotation.illisible")]
    [InlineData(2026, "EB-20A6-0001", "numerotation.illisible")]
    [InlineData(2026, "EB-2026-00A1", "numerotation.illisible")]
    [InlineData(2026, "EB-2026-+001", "numerotation.illisible")]
    [InlineData(2026, "EB-2026- 001", "numerotation.illisible")]
    [InlineData(2026, "EB-2026-0000", "numerotation.illisible")]
    [InlineData(2026, "eb-2026-0001", "numerotation.illisible")]
    public void Numero_suivant_refuse(int annee, string? dernier, string code)
    {
        Assert.Equal(code, CodeRefus(Numerotation.Suivant(annee, dernier)));
    }

    [Fact]
    public void Messages_de_refus_explicites()
    {
        Assert.Equal("Dernier numéro illisible : « EB-26-1 ».", MessageRefus(Numerotation.Suivant(2026, "EB-26-1")));
        Assert.Equal("Plus aucun numéro disponible pour 2026.", MessageRefus(Numerotation.Suivant(2026, "EB-2026-9999")));
    }
}
