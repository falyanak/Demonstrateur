using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.Fiches;
using static ExpressionBesoin.Infrastructure.Tests.Essais;

namespace ExpressionBesoin.Infrastructure.Tests.Fiches;

public sealed class CodecFicheTests
{
    [Fact]
    public void Encoder_un_brouillon_vierge_omet_les_proprietes_nulles_et_les_champs_initiaux()
    {
        Assert.Equal(JsonBrouillonVierge, CodecFiche.Encoder(Brouillon(1, T0)));
    }

    [Fact]
    public void Encoder_la_fiche_de_reference_rend_le_json_de_reference_a_l_octet()
    {
        // Contrat du stockage (DEB-Q28) : codes de rubrique, noms d'énumération, ISO 8601, échappement minimal.
        Assert.Equal(JsonReference, CodecFiche.Encoder(Reference()));
    }

    [Fact]
    public void Decoder_le_json_de_reference_rend_la_fiche_de_reference()
    {
        Assert.Equal(Reference(), CodecFiche.Decoder(JsonReference));
    }

    [Theory]
    [InlineData("brouillon")]
    [InlineData("complete")]
    [InlineData("soumise")]
    [InlineData("validee")]
    [InlineData("reprise")]
    public void Aller_retour_rend_une_fiche_egale(string cas)
    {
        var fiche = cas switch
        {
            "brouillon" => Brouillon(2, T0),
            "complete" => Complete(2, T0),
            "soumise" => Soumise(2, T0, "EB-2026-0001"),
            "validee" => Validee(2, T0, "EB-2026-0001"),
            _ => Reprise(2, T0, "EB-2026-0001"),
        };

        Assert.Equal(fiche, CodecFiche.Decoder(CodecFiche.Encoder(fiche)));
    }

    [Fact]
    public void Aller_retour_garde_l_instant_d_un_decalage_non_nul()
    {
        var enTete = EnTeteFictif(new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(1)));
        var fiche = Fiche.Creer(IdNumero(3), enTete);

        var relue = CodecFiche.Decoder(CodecFiche.Encoder(fiche));

        Assert.Equal(T0, relue.EnTete.CreeeLe);
    }

    [Theory]
    [InlineData("\"v\":1", "\"v\":2")]
    [InlineData("\"Brouillon\"", "\"Inconnu\"")]
    [InlineData("\"Brouillon\"", "\"0\"")]
    [InlineData("\"Brouillon\"", "null")]
    [InlineData("\"champs\":{}", "\"champs\":{\"inconnue\":{}}")]
    [InlineData("\"champs\":{}", "\"champs\":{\"intitule\":{\"valeur\":\"x\",\"origine\":\"Proposee\"}}")]
    [InlineData("\"demandeur\":\"Camille Martin\"", "\"demandeur\":null")]
    [InlineData("\"decisions\":[]", "\"autre\":[]")]
    [InlineData("{\"v\":1,", "pas du json")]
    public void Decoder_un_contenu_illisible_leve_InvalidDataException(string remplace, string par)
    {
        var contenu = JsonBrouillonVierge.Replace(remplace, par, StringComparison.Ordinal);
        Assert.NotEqual(JsonBrouillonVierge, contenu);

        var erreur = Assert.Throws<InvalidDataException>(() => CodecFiche.Decoder(contenu));

        Assert.StartsWith("Contenu de fiche illisible : ", erreur.Message, StringComparison.Ordinal);
        Assert.NotNull(erreur.InnerException);
    }
}
