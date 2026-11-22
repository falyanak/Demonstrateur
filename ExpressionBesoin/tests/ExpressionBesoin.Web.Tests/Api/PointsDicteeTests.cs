using System.Net;
using System.Text.Json;
using ExpressionBesoin.Application.Dictee;
using ExpressionBesoin.Web.Api;
using static ExpressionBesoin.Web.Tests.Api.EssaisApi;

namespace ExpressionBesoin.Web.Tests.Api;

/// <summary>
/// Route <c>POST api/dictee</c> et page Dialogue avec la dictée offerte (lot S1), modèle simulé par
/// <see cref="UsineDictee"/> : chaque issue (texte, refus, panne) sans réseau ; contrat figé à l'octet avec Vitest.
/// </summary>
public sealed class PointsDicteeTests(UsineDictee usine) : IClassFixture<UsineDictee>
{
    /// <summary>WAV canonique de deux échantillons (réplique Python, comme <c>DecisionsDicteeTests</c>).</summary>
    internal const string ReferenceHex =
        "524946462800000057415645666D74201000000001000100803E0000007D00000200100064617461040000000000FF7F";

    /// <summary>WAV canonique de <paramref name="echantillons"/> échantillons nuls : en-tête de la référence, tailles ajustées.</summary>
    private static byte[] Wav(int echantillons)
    {
        var octets = new byte[DecisionsDictee.EnTete + (2 * echantillons)];
        Convert.FromHexString(ReferenceHex).AsSpan(0, DecisionsDictee.EnTete).CopyTo(octets);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(octets.AsSpan(4), (uint)(octets.Length - 8));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(octets.AsSpan(40), (uint)(2 * echantillons));
        return octets;
    }

    /// <summary>Flux sans longueur connue : la requête part sans <c>Content-Length</c> (envoi par morceaux).</summary>
    private sealed class FluxSansLongueur(byte[] octets) : MemoryStream(octets)
    {
        public override bool CanSeek => false;
    }

    /// <summary>Flux qui ne rend que trois octets par lecture.</summary>
    private sealed class FluxParMorceaux(byte[] octets) : MemoryStream(octets)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> tampon, CancellationToken annulation = default) =>
            base.ReadAsync(tampon[..Math.Min(3, tampon.Length)], annulation);
    }

    private async Task<(HttpStatusCode Statut, string Corps)> Dicter(HttpContent contenu)
    {
        using var client = usine.CreateClient();
        using var reponse = await client.PostAsync("/api/dictee", contenu, Annulation);
        return (reponse.StatusCode, await reponse.Content.ReadAsStringAsync(Annulation));
    }

    private static string Code(string corps) => JsonDocument.Parse(corps).RootElement.GetProperty("code").GetString()!;

    [Fact]
    public async Task Page_dialogue_annonce_la_dictee()
    {
        using var client = usine.CreateClient();
        var html = await client.GetStringAsync("/Home/Dialogue/6f1c2b3a-0000-4000-8000-0000000000c1", Annulation);
        Assert.Contains(
            "<div id=\"app\" data-page=\"dialogue\" data-fiche=\"6f1c2b3a-0000-4000-8000-0000000000c1\" data-dictee=\"oui\">",
            html);
    }

    [Fact]
    public async Task Texte_de_la_dictee_au_contrat()
    {
        using var contenu = new ByteArrayContent(Wav(10));
        var (statut, corps) = await Dicter(contenu);

        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Equal("{\"texte\":\"Le copieur du bureau 1A ne démarre plus.\"}", corps);
    }

    [Fact]
    public async Task Envoi_sans_longueur_annoncee_lu_en_entier()
    {
        using var contenu = new StreamContent(new FluxSansLongueur(Wav(10)));
        var (statut, corps) = await Dicter(contenu);

        Assert.Equal(HttpStatusCode.OK, statut);
        Assert.Contains(UsineDictee.Texte, corps);
    }

    [Theory]
    [InlineData(0, "dictee.vide")]
    [InlineData(1, "dictee.silence")]
    public async Task Enregistrement_vide_ou_sans_parole_422(int echantillons, string code)
    {
        using var contenu = new ByteArrayContent(Wav(echantillons));
        var (statut, corps) = await Dicter(contenu);

        Assert.Equal((HttpStatusCode.UnprocessableEntity, code), (statut, Code(corps)));
    }

    [Fact]
    public async Task Format_autre_que_le_wav_canonique_422()
    {
        using var contenu = new ByteArrayContent("OggS-pas-un-wav"u8.ToArray());
        var (statut, corps) = await Dicter(contenu);

        Assert.Equal((HttpStatusCode.UnprocessableEntity, "dictee.formatInvalide"), (statut, Code(corps)));
    }

    [Fact]
    public async Task Envoi_au_dela_du_maximum_trop_long_422()
    {
        using var contenu = new ByteArrayContent(new byte[DecisionsDictee.OctetsMax + 1000]);
        var (statut, corps) = await Dicter(contenu);

        Assert.Equal((HttpStatusCode.UnprocessableEntity, "dictee.tropLongue"), (statut, Code(corps)));
    }

    [Fact]
    public async Task Panne_du_modele_503()
    {
        using var contenu = new ByteArrayContent(Wav(3));
        var (statut, corps) = await Dicter(contenu);

        Assert.Equal((HttpStatusCode.ServiceUnavailable, "panne.technique"), (statut, Code(corps)));
    }

    [Fact]
    public void Dictee_de_reference_serialisee_a_l_octet_comme_le_fichier_partage_avec_Vitest() =>
        Assert.Equal(
            File.ReadAllBytes(FicheVueTests.CheminReference("dictee-reference.json")),
            JsonSerializer.SerializeToUtf8Bytes(DicteeVue.De(UsineDictee.Texte), ContratJson.Options));

    [Theory]
    [InlineData(10, 10, 100, 10)]
    [InlineData(10, null, 100, 10)]
    [InlineData(200, 200, 100, 100)]
    [InlineData(200, null, 100, 100)]
    [InlineData(0, 0, 100, 0)]
    public async Task Corps_lu_jusqu_au_plafond(int taille, int? annoncee, int plafond, int attendu)
    {
        var octets = Enumerable.Range(0, taille).Select(i => (byte)i).ToArray();

        var lus = await PointsFiches.LireCorps(new FluxParMorceaux(octets), annoncee, plafond, Annulation);

        Assert.Equal(octets.Take(attendu).ToArray(), lus);
    }
}
