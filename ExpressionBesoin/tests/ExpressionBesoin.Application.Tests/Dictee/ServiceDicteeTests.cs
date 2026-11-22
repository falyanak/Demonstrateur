using ExpressionBesoin.Application.Dictee;
using ExpressionBesoin.Domain;

namespace ExpressionBesoin.Application.Tests.Dictee;

/// <summary>
/// Transcripteur en mémoire : rend le texte fourni et garde les octets reçus. <paramref name="differe"/> = vrai : rend la
/// main avant de répondre (attente non terminée, branche de la machine asynchrone), comme les doublures des fiches.
/// </summary>
internal sealed class TranscripteurEnMemoire(Func<string> transcrire, bool differe = false) : ITranscripteur
{
    public List<byte[]> Recus { get; } = [];

    public async ValueTask<string> Transcrire(ReadOnlyMemory<byte> wav, CancellationToken annulation)
    {
        Recus.Add(wav.ToArray());
        if (differe)
        {
            await Task.Yield();
        }

        return transcrire();
    }
}

/// <summary>Cas d'usage de la dictée (lot S1) : vérifier, transcrire, conclure ; aucune écriture.</summary>
public sealed class ServiceDicteeTests
{
    private static CancellationToken Annulation => TestContext.Current.CancellationToken;

    private static string Issue(Resultat<string> resultat) => resultat.Match(texte => "texte:" + texte, refus => refus.Code);

    [Fact]
    public async Task Sans_transcripteur_indisponible()
    {
        var service = new ServiceDictee(null);

        Assert.False(service.Disponible);
        Assert.Equal("dictee.indisponible", Issue(await service.Transcrire(DecisionsDicteeTests.Reference(), Annulation)));
    }

    [Fact]
    public async Task Format_invalide_le_modele_n_est_pas_appele()
    {
        var transcripteur = new TranscripteurEnMemoire(() => "jamais");
        var service = new ServiceDictee(transcripteur);

        Assert.True(service.Disponible);
        Assert.Equal("dictee.formatInvalide", Issue(await service.Transcrire(new byte[] { 1, 2, 3 }, Annulation)));
        Assert.Empty(transcripteur.Recus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Texte_du_modele_sans_espaces_de_bord_octets_transmis_tels_quels(bool differe)
    {
        var transcripteur = new TranscripteurEnMemoire(() => " Le copieur est en panne. ", differe);
        var service = new ServiceDictee(transcripteur);

        Assert.Equal("texte:Le copieur est en panne.", Issue(await service.Transcrire(DecisionsDicteeTests.Reference(), Annulation)));
        Assert.Equal(DecisionsDicteeTests.Reference(), Assert.Single(transcripteur.Recus));
    }

    [Fact]
    public async Task Transcription_blanche_silence()
    {
        var service = new ServiceDictee(new TranscripteurEnMemoire(() => "   "));
        Assert.Equal("dictee.silence", Issue(await service.Transcrire(DecisionsDicteeTests.Reference(), Annulation)));
    }

    [Fact]
    public async Task Panne_du_modele_propagee_jamais_un_refus()
    {
        var service = new ServiceDictee(new TranscripteurEnMemoire(() => throw new HttpRequestException("Whisper injoignable (essai).")));
        await Assert.ThrowsAsync<HttpRequestException>(async () => await service.Transcrire(DecisionsDicteeTests.Reference(), Annulation));
    }
}
