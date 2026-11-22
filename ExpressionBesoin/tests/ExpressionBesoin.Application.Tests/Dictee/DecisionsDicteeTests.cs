using System.Buffers.Binary;
using ExpressionBesoin.Application.Dictee;
using ExpressionBesoin.Domain;

namespace ExpressionBesoin.Application.Tests.Dictee;

/// <summary>
/// Table de vérité de la dictée (lot S1, DEB-Q131) : WAV canonique mono 16 kHz 16 bits, chaque champ de l'en-tête faussé
/// un à un, tailles aux bornes, transcription blanche. Octets de référence produits par une réplique Python indépendante
/// (<c>struct.pack</c>), jamais par le code testé.
/// </summary>
public sealed class DecisionsDicteeTests
{
    /// <summary>WAV canonique de deux échantillons (0 et 32 767), octets de la réplique Python.</summary>
    internal const string ReferenceHex =
        "524946462800000057415645666D74201000000001000100803E0000007D00000200100064617461040000000000FF7F";

    internal static byte[] Reference() => Convert.FromHexString(ReferenceHex);

    /// <summary>WAV canonique de <paramref name="echantillons"/> échantillons nuls, écrit champ par champ.</summary>
    internal static byte[] Wav(int echantillons)
    {
        var octets = new byte[DecisionsDictee.EnTete + (2 * echantillons)];
        "RIFF"u8.CopyTo(octets);
        BinaryPrimitives.WriteUInt32LittleEndian(octets.AsSpan(4), (uint)(octets.Length - 8));
        "WAVE"u8.CopyTo(octets.AsSpan(8));
        "fmt "u8.CopyTo(octets.AsSpan(12));
        BinaryPrimitives.WriteUInt32LittleEndian(octets.AsSpan(16), 16);
        BinaryPrimitives.WriteUInt16LittleEndian(octets.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(octets.AsSpan(22), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(octets.AsSpan(24), 16_000);
        BinaryPrimitives.WriteUInt32LittleEndian(octets.AsSpan(28), 32_000);
        BinaryPrimitives.WriteUInt16LittleEndian(octets.AsSpan(32), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(octets.AsSpan(34), 16);
        "data"u8.CopyTo(octets.AsSpan(36));
        BinaryPrimitives.WriteUInt32LittleEndian(octets.AsSpan(40), (uint)(2 * echantillons));
        return octets;
    }

    private static string Code(Refus? refus) => refus?.Code ?? "aucun";

    [Fact]
    public void Aide_d_ecriture_conforme_a_la_reference_python()
    {
        var wav = Wav(2);
        wav[46] = 0xFF;
        wav[47] = 0x7F;
        Assert.Equal(Reference(), wav);
    }

    [Fact]
    public void Constantes_recalculees()
    {
        Assert.Equal(
            new[] { 16_000, 32_000, 44, 125, 4_000_044 },
            new[]
            {
                DecisionsDictee.Frequence, DecisionsDictee.OctetsParSeconde, DecisionsDictee.EnTete, DecisionsDictee.SecondesMax,
                DecisionsDictee.OctetsMax,
            });
    }

    [Fact]
    public void Wav_canonique_accepte() => Assert.Null(DecisionsDictee.Verifier(true, Reference()));

    [Fact]
    public void Indisponible_avant_tout_autre_controle()
    {
        Assert.Equal("dictee.indisponible", Code(DecisionsDictee.Verifier(false, Reference())));
        Assert.Equal("dictee.indisponible", Code(DecisionsDictee.Verifier(false, [])));
    }

    /// <summary>Chaque champ de l'en-tête faussé seul : format refusé, jamais deviné.</summary>
    [Theory]
    [InlineData(0, 0x72)] // « rIFF »
    [InlineData(3, 0x00)] // « RIF\0 »
    [InlineData(4, 0x29)] // taille RIFF ≠ longueur − 8
    [InlineData(8, 0x77)] // « wAVE »
    [InlineData(11, 0x00)] // « WAV\0 »
    [InlineData(12, 0x46)] // « Fmt »
    [InlineData(15, 0x00)] // « fmt\0 »
    [InlineData(16, 18)] // bloc fmt de 18 octets
    [InlineData(20, 3)] // flottant au lieu de PCM
    [InlineData(22, 2)] // stéréo
    [InlineData(24, 0x81)] // 16 001 Hz
    [InlineData(28, 0x01)] // octets par seconde
    [InlineData(32, 4)] // alignement de bloc
    [InlineData(34, 8)] // 8 bits
    [InlineData(36, 0x44)] // « Data »
    [InlineData(39, 0x00)] // « dat\0 »
    [InlineData(40, 0x02)] // taille des données ≠ longueur − 44
    public void Champ_d_en_tete_fausse_format_invalide(int position, int valeur)
    {
        var wav = Reference();
        wav[position] = (byte)valeur;
        Assert.Equal("dictee.formatInvalide", Code(DecisionsDictee.Verifier(true, wav)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(43)]
    public void Plus_court_que_l_en_tete_format_invalide(int longueur) =>
        Assert.Equal("dictee.formatInvalide", Code(DecisionsDictee.Verifier(true, Reference().AsSpan(0, longueur))));

    [Fact]
    public void Nombre_impair_d_octets_format_invalide()
    {
        var wav = new byte[49];
        Reference().CopyTo(wav, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(4), 41);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(40), 5);
        Assert.Equal("dictee.formatInvalide", Code(DecisionsDictee.Verifier(true, wav)));
    }

    [Fact]
    public void En_tete_seul_enregistrement_vide() =>
        Assert.Equal("dictee.vide", Code(DecisionsDictee.Verifier(true, Wav(0))));

    [Fact]
    public void Taille_maximale_acceptee_un_octet_de_plus_trop_longue()
    {
        Assert.Null(DecisionsDictee.Verifier(true, Wav((DecisionsDictee.OctetsMax - DecisionsDictee.EnTete) / 2)));
        Assert.Equal("dictee.tropLongue", Code(DecisionsDictee.Verifier(true, new byte[DecisionsDictee.OctetsMax + 1])));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    public void Transcription_blanche_silence(string? texte) =>
        Assert.Equal("dictee.silence", DecisionsDictee.Conclure(texte).Match(_ => "succès", refus => refus.Code));

    [Fact]
    public void Transcription_sans_espaces_de_bord() =>
        Assert.Equal(
            "Le copieur est en panne.",
            DecisionsDictee.Conclure(" Le copieur est en panne. \n").Match(texte => texte, refus => refus.Code));

    [Fact]
    public void Codes_et_messages_des_refus()
    {
        Refus[] refus = [DecisionsDictee.Indisponible, DecisionsDictee.TropLongue, DecisionsDictee.FormatInvalide, DecisionsDictee.Vide, DecisionsDictee.Silence];
        Assert.Equal(
            new[] { "dictee.indisponible", "dictee.tropLongue", "dictee.formatInvalide", "dictee.vide", "dictee.silence" },
            refus.Select(r => r.Code).ToArray());
        Assert.All(refus, r => Assert.False(string.IsNullOrWhiteSpace(r.Message)));
    }
}
