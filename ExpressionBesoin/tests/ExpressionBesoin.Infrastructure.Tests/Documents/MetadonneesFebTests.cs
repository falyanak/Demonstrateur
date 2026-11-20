using System.Text;
using ExpressionBesoin.Infrastructure.Documents;
using static ExpressionBesoin.Infrastructure.Tests.Documents.EssaisDocuments;

namespace ExpressionBesoin.Infrastructure.Tests.Documents;

public sealed class MetadonneesFebTests
{
    [Fact]
    public void Core_du_contenu_de_reference_a_l_octet()
    {
        Assert.Equal(CoreAttendu, MetadonneesFeb.Core(Contenu()));
        Assert.Equal(Encoding.UTF8.GetBytes(CoreAttendu), MetadonneesFeb.Octets(Contenu()));
    }

    [Fact]
    public void Core_echappe_les_textes_et_retire_les_caracteres_interdits()
    {
        var contenu = Contenu() with { Numero = "A&B", Demandeur = "<Camille>\u0001", Valideur = "Dominique \"D.\" Bernard" };

        var core = MetadonneesFeb.Core(contenu);

        Assert.Contains("<dc:title>Expression de besoin A&amp;B</dc:title>", core);
        Assert.Contains("<dc:creator>&lt;Camille&gt;</dc:creator>", core);
        Assert.Contains("<cp:lastModifiedBy>Dominique \"D.\" Bernard</cp:lastModifiedBy>", core);
    }

    [Fact]
    public void Core_ecrit_les_instants_en_UTC_quelle_que_soit_leur_forme()
    {
        var contenu = Contenu() with
        {
            CreeeLe = new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.FromHours(2)),
            ModifieeLe = new DateTimeOffset(2026, 12, 31, 23, 59, 59, 999, TimeSpan.Zero),
        };

        var core = MetadonneesFeb.Core(contenu);

        Assert.Contains("\">2026-11-20T09:00:00Z</dcterms:created>", core);
        Assert.Contains("\">2026-12-31T23:59:59Z</dcterms:modified>", core);
    }
}
