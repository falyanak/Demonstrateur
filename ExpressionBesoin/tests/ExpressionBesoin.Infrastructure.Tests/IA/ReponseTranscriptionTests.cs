using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>Lecture de la réponse de Whisper (lot S1) : table de vérité, aucun texte deviné.</summary>
public sealed class ReponseTranscriptionTests
{
    [Theory]
    [InlineData("{\"text\":\" Le copieur est en panne.\"}", " Le copieur est en panne.")]
    [InlineData("{\"text\":\"\",\"usage\":{\"seconds\":2}}", "")]
    [InlineData("{\"task\":\"transcribe\",\"text\":\"Bonjour\"}", "Bonjour")]
    public void Champ_text_rendu_tel_quel(string corps, string attendu) => Assert.Equal(attendu, ReponseTranscription.Lire(corps));

    [Theory]
    [InlineData("")]
    [InlineData("Internal Server Error")]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("{\"texte\":\"x\"}")]
    [InlineData("{\"text\":12}")]
    [InlineData("{\"text\":null}")]
    public void Autre_corps_null(string corps) => Assert.Null(ReponseTranscription.Lire(corps));

    [Fact]
    public void Extrait_borne_a_trois_cents_caracteres()
    {
        var juste = new string('a', ReponseTranscription.ExtraitMax);
        Assert.Equal(300, ReponseTranscription.ExtraitMax);
        Assert.Equal(juste, ReponseTranscription.Extrait(juste));
        Assert.Equal(juste + "…", ReponseTranscription.Extrait(juste + "bc"));
    }
}
