using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.Documents;
using static ExpressionBesoin.Infrastructure.Tests.Documents.EssaisDocuments;
using static ExpressionBesoin.Infrastructure.Tests.Essais;

namespace ExpressionBesoin.Infrastructure.Tests.Documents;

public sealed class GenerateurDocumentFebTests
{
    /// <summary>SHA-256 du modèle FEB nettoyé versionné (DEB-Q33), relevé hors .NET : tout remplacement du gabarit se voit.</summary>
    private const string EmpreinteGabarit = "9A75437551ABA5F79027B89693682ABEAA9FB201BEF366046864FF21D87B77B4";

    private static readonly GenerateurDocumentFeb Generateur = GenerateurDocumentFeb.DepuisRessource();

    private static async Task<byte[]> Generer(ContenuDocument contenu) => await Generateur.Generer(contenu, Annulation);

    // --- Gabarit versionné (DEB-Q9, DEB-Q33) ---

    [Fact]
    public void Le_gabarit_embarque_est_celui_qui_est_versionne()
    {
        Assert.Equal(EmpreinteGabarit, Convert.ToHexString(SHA256.HashData(GabaritEmbarque())));
    }

    [Fact]
    public void Le_gabarit_embarque_ne_porte_aucune_metadonnee_personnelle()
    {
        var parties = Parties(GabaritEmbarque());

        var core = Xml(parties.Single(partie => partie.Nom == "docProps/core.xml").Octets);
        Assert.Equal("", core.Descendants().Single(element => element.Name.LocalName == "creator").Value);
        Assert.Equal("", core.Descendants().Single(element => element.Name.LocalName == "lastModifiedBy").Value);
        Assert.DoesNotContain(parties, partie => partie.Nom.EndsWith("settings.xml.rels", StringComparison.Ordinal));
        Assert.All(parties.Where(partie => partie.Nom.EndsWith(".xml", StringComparison.Ordinal) || partie.Nom.EndsWith(".rels", StringComparison.Ordinal)), partie =>
        {
            var texte = Encoding.UTF8.GetString(partie.Octets);
            Assert.DoesNotContain("file:///", texte);
            Assert.DoesNotContain("attachedTemplate", texte);
            Assert.DoesNotContain("<Template>", texte);
        });
    }

    // --- Document produit ---

    [Fact]
    public async Task Chaque_paragraphe_du_document_est_celui_attendu()
    {
        var document = Xml(Partie(await Generer(Contenu()), GenerateurDocumentFeb.PartieDocument));

        Assert.Equal(ParagraphesAttendus, document.Descendants(W + "p").Select(paragraphe => TexteAvecSauts(paragraphe)).ToArray());
    }

    [Fact]
    public async Task Seuls_les_paragraphes_vises_changent()
    {
        var gabarit = Xml(Partie(GabaritEmbarque(), GenerateurDocumentFeb.PartieDocument)).Descendants(W + "p").ToArray();
        var produit = Xml(Partie(await Generer(Contenu()), GenerateurDocumentFeb.PartieDocument)).Descendants(W + "p").ToArray();

        Assert.Equal(gabarit.Length, produit.Length);
        Assert.Equal(26, gabarit.Zip(produit, (avant, apres) => XNode.DeepEquals(avant, apres)).Count(identique => !identique));
        Assert.True(XNode.DeepEquals(
            Xml(Partie(GabaritEmbarque(), GenerateurDocumentFeb.PartieDocument)).Root!.Element(W + "body")!.Element(W + "sectPr"),
            Xml(Partie(await Generer(Contenu()), GenerateurDocumentFeb.PartieDocument)).Root!.Element(W + "body")!.Element(W + "sectPr")));
    }

    [Fact]
    public async Task Les_libelles_restent_en_gras_et_les_sauts_de_ligne_deviennent_des_retours()
    {
        var document = Xml(Partie(await Generer(Contenu()), GenerateurDocumentFeb.PartieDocument));

        var runs = Paragraphe(document, "Situation actuelle : ").Elements(W + "r").ToArray();
        Assert.Equal(2, runs.Length);
        Assert.NotNull(runs[0].Element(W + "rPr")!.Element(W + "b"));
        Assert.Single(runs[1].Elements(W + "br"));
    }

    [Fact]
    public async Task Une_fiche_non_confirmee_garde_la_case_du_modele()
    {
        var document = Xml(Partie(await Generer(Contenu(confirmee: false)), GenerateurDocumentFeb.PartieDocument));
        var textes = document.Descendants(W + "p").Select(paragraphe => TexteAvecSauts(paragraphe)).ToArray();

        Assert.Equal("☐ Je confirme que cette fiche reflète fidèlement le besoin que je souhaite soumettre à validation.", textes[38]);
        Assert.Equal("Date de confirmation : Non confirmée", textes[39]);
    }

    [Fact]
    public async Task Les_metadonnees_viennent_du_contenu()
    {
        Assert.Equal(Encoding.UTF8.GetBytes(CoreAttendu), Partie(await Generer(Contenu()), GenerateurDocumentFeb.PartieCore));
    }

    [Fact]
    public async Task Les_autres_parties_sont_recopiees_a_l_octet_dans_l_ordre_du_gabarit()
    {
        var gabarit = Parties(GabaritEmbarque());
        var produit = Parties(await Generer(Contenu()));

        Assert.Equal(gabarit.Select(partie => partie.Nom).ToArray(), produit.Select(partie => partie.Nom).ToArray());
        Assert.Equal("[Content_Types].xml", produit[0].Nom);
        for (var index = 0; index < gabarit.Length; index++)
        {
            if (gabarit[index].Nom is not GenerateurDocumentFeb.PartieDocument and not GenerateurDocumentFeb.PartieCore)
            {
                Assert.Equal(gabarit[index].Octets, produit[index].Octets);
            }
        }
    }

    [Fact]
    public async Task Les_entrees_ont_un_horodatage_fixe_et_l_image_n_est_pas_recompressee()
    {
        using var archive = new ZipArchive(new MemoryStream(await Generer(Contenu())), ZipArchiveMode.Read);

        Assert.All(archive.Entries, entree => Assert.Equal(new DateTime(1980, 1, 1), entree.LastWriteTime.DateTime));
        var image = archive.GetEntry("word/media/image1.jpeg")!;
        Assert.Equal(image.Length, image.CompressedLength);
        var document = archive.GetEntry(GenerateurDocumentFeb.PartieDocument)!;
        Assert.True(document.CompressedLength < document.Length);
    }

    [Theory]
    [InlineData("word/media/image1.jpeg", CompressionLevel.NoCompression)]
    [InlineData("word/media/LOGO.PNG", CompressionLevel.NoCompression)]
    [InlineData("word/document.xml", CompressionLevel.Optimal)]
    [InlineData("[Content_Types].xml", CompressionLevel.Optimal)]
    [InlineData("sans-extension", CompressionLevel.Optimal)]
    public void Compression_selon_l_extension(string nom, CompressionLevel attendue)
    {
        Assert.Equal(attendue, GenerateurDocumentFeb.Compression(nom));
    }

    // --- Octets stables (DEB-Q37) ---

    [Fact]
    public async Task Memes_entrees_memes_octets_d_un_appel_et_d_un_exemplaire_a_l_autre()
    {
        var premier = await Generer(Contenu());

        Assert.Equal(premier, await Generer(Contenu()));
        Assert.Equal(premier, await GenerateurDocumentFeb.DepuisRessource().Generer(Contenu(), Annulation));
        Assert.NotEqual(premier, await Generer(Contenu() with { Numero = "EB-2026-0008" }));
    }

    [Fact]
    public async Task Les_octets_ne_dependent_pas_de_la_culture_courante()
    {
        var reference = await Generer(Contenu());
        var avant = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");

            Assert.Equal(reference, await Generer(Contenu()));
        }
        finally
        {
            CultureInfo.CurrentCulture = avant;
        }
    }

    [Fact]
    public void Generer_une_requete_annulee_leve_sans_produire()
    {
        Assert.Throws<OperationCanceledException>(() => { _ = Generateur.Generer(Contenu(), new CancellationToken(true)); });
    }

    // --- De la fiche au document ---

    [Fact]
    public async Task Une_fiche_validee_du_Domaine_produit_son_document()
    {
        var contenu = ContenuDocument.Construire(Validee(7, T0, "EB-2026-0007"), TimeZoneInfo.Utc);

        var textes = Xml(Partie(await Generer(contenu), GenerateurDocumentFeb.PartieDocument)).Descendants(W + "p").Select(paragraphe => TexteAvecSauts(paragraphe)).ToArray();

        Assert.Equal("N° de demande : EB-2026-0007", textes[5]);
        Assert.Equal("Demande exprimée par : Camille Martin", textes[6]);
        Assert.Equal("Solution envisagée par le demandeur : Portail unique", textes[25]);
        Assert.Equal("Exemple représentatif : Non renseigné", textes[21]);
        Assert.Equal("Validée", textes[47]);
        Assert.Equal("Dominique Bernard, le 20/11/2026", textes[48]);
    }

    /// <summary>
    /// OUTIL, pas un test : écrit un exemplaire du document dans le fichier nommé par la variable d'environnement
    /// DEB_I2_SORTIE (rien si elle est absente), pour l'ouvrir dans Word (critère de fin d'I2).
    /// </summary>
    [Fact]
    public async Task Outil_exemplaire_pour_Word_ecrit_si_la_variable_DEB_I2_SORTIE_est_posee()
    {
        var sortie = Environment.GetEnvironmentVariable("DEB_I2_SORTIE");
        if (string.IsNullOrWhiteSpace(sortie))
        {
            return;
        }

        var octets = await Generer(ContenuDocument.Construire(Validee(7, T0, "EB-2026-0007"), TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris")));
        File.WriteAllBytes(sortie, octets);
        Assert.Equal(octets, File.ReadAllBytes(sortie));
    }

    // --- Gabarit inutilisable : refusé à la construction ---

    [Fact]
    public void Un_gabarit_qui_n_est_pas_un_zip_est_refuse()
    {
        Assert.Throws<InvalidDataException>(() => new GenerateurDocumentFeb([1, 2, 3, 4]));
    }

    [Fact]
    public void Un_gabarit_sans_document_est_refuse()
    {
        var erreur = Assert.Throws<InvalidDataException>(() => new GenerateurDocumentFeb(Paquet(("docProps/core.xml", "<a/>"))));

        Assert.Equal("Gabarit FEB sans partie word/document.xml.", erreur.Message);
    }

    [Fact]
    public void Un_gabarit_sans_metadonnees_est_refuse()
    {
        var erreur = Assert.Throws<InvalidDataException>(() => new GenerateurDocumentFeb(Paquet(("word/document.xml", Minimal().ToString()))));

        Assert.Equal("Gabarit FEB sans partie docProps/core.xml.", erreur.Message);
    }

    [Fact]
    public void Un_gabarit_ecarte_du_plan_est_refuse_avec_ses_anomalies()
    {
        var document = Minimal(corps => Paragraphe(corps, "Bureau : ").Remove()).ToString();

        var erreur = Assert.Throws<InvalidDataException>(() =>
            new GenerateurDocumentFeb(Paquet(("word/document.xml", document), ("docProps/core.xml", "<a/>"))));

        Assert.Equal("Gabarit FEB inutilisable : Marqueur « Bureau :  » trouvé 0 fois (attendu : 1).", erreur.Message);
    }

    [Fact]
    public async Task Un_gabarit_minimal_conforme_est_accepte_et_rempli()
    {
        var generateur = new GenerateurDocumentFeb(Paquet(("word/document.xml", Minimal().ToString()), ("docProps/core.xml", "<a/>")));

        var paquet = await generateur.Generer(Contenu(), Annulation);

        Assert.Equal("Bureau : Bureau 1A", TexteAvecSauts(Paragraphe(Xml(Partie(paquet, "word/document.xml")), "Bureau : ")));
    }
}
