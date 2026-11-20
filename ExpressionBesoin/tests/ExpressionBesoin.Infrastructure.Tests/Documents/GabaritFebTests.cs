using System.Text;
using System.Xml.Linq;
using ExpressionBesoin.Infrastructure.Documents;
using static ExpressionBesoin.Infrastructure.Tests.Documents.EssaisDocuments;

namespace ExpressionBesoin.Infrastructure.Tests.Documents;

public sealed class GabaritFebTests
{
    // --- Anomalies : table de vérité sur un gabarit minimal ---

    [Fact]
    public void Un_gabarit_conforme_au_plan_n_a_aucune_anomalie()
    {
        Assert.Empty(GabaritFeb.Anomalies(Minimal()));
    }

    [Fact]
    public void Un_marqueur_absent_est_signale()
    {
        var gabarit = Minimal(corps => Paragraphe(corps, "Bureau : ").Remove());

        Assert.Equal(new[] { "Marqueur « Bureau :  » trouvé 0 fois (attendu : 1)." }, GabaritFeb.Anomalies(gabarit).ToArray());
    }

    [Fact]
    public void Un_marqueur_en_double_est_signale()
    {
        var gabarit = Minimal(corps => corps.Add(new XElement(W + "p", Run("Valideur : autre"))));

        Assert.Equal(new[] { "Marqueur « Valideur :  » trouvé 2 fois (attendu : 1)." }, GabaritFeb.Anomalies(gabarit).ToArray());
    }

    [Fact]
    public void Un_libelle_sans_run_de_valeur_est_signale()
    {
        var gabarit = Minimal(corps => Paragraphe(corps, "Situation actuelle : ").Elements(W + "r").Last().Remove());

        Assert.Equal(
            new[] { "Libellé « Situation actuelle :  » sans run de valeur à sa suite." },
            GabaritFeb.Anomalies(gabarit).ToArray());
    }

    [Fact]
    public void Un_libelle_fondu_dans_son_premier_run_est_signale()
    {
        var gabarit = Minimal(corps => Paragraphe(corps, "Contraintes identifiées : ")
            .ReplaceNodes(Run("Contraintes identifiées : à renseigner"), Run("suite")));

        Assert.Equal(
            new[] { "Libellé « Contraintes identifiées :  » sans run de valeur à sa suite." },
            GabaritFeb.Anomalies(gabarit).ToArray());
    }

    [Fact]
    public void Une_ligne_vide_ou_sans_cellule_n_est_pas_prise_pour_celle_du_chef()
    {
        var gabarit = Minimal(corps => corps.Element(W + "tbl")!.Add(new XElement(W + "tr"), new XElement(W + "tr", Cellule(Run("Direction métier")))));

        Assert.Empty(GabaritFeb.Anomalies(gabarit));
    }

    [Fact]
    public void Une_ligne_du_chef_absente_ou_en_double_est_signalee()
    {
        var sans = Minimal(corps => corps.Element(W + "tbl")!.RemoveNodes());
        var double_ = Minimal(corps => corps.Element(W + "tbl")!.Add(LigneChef()));

        Assert.Equal(new[] { "Ligne « Chef de service » trouvée 0 fois (attendu : 1)." }, GabaritFeb.Anomalies(sans).ToArray());
        Assert.Equal(new[] { "Ligne « Chef de service » trouvée 2 fois (attendu : 1)." }, GabaritFeb.Anomalies(double_).ToArray());
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Une_ligne_du_chef_inutilisable_est_signalee(int cellule, bool viderSeulement)
    {
        var gabarit = Minimal(corps =>
        {
            var cible = corps.Descendants(W + "tc").ElementAt(cellule);
            if (viderSeulement)
            {
                cible.Descendants(W + "r").Remove();
            }
            else
            {
                cible.Remove();
            }
        });

        Assert.Equal(
            new[] { "Ligne « Chef de service » : trois cellules attendues, les deux dernières avec un run." },
            GabaritFeb.Anomalies(gabarit).ToArray());
    }

    // --- Remplissage ---

    [Fact]
    public void Remplir_ecrit_chaque_marqueur_et_garde_le_libelle_en_gras()
    {
        var rempli = GabaritFeb.Remplir(Minimal(), Contenu());

        var libelle = Paragraphe(rempli, "Situation actuelle : ");
        var runs = libelle.Elements(W + "r").ToArray();
        Assert.Equal(2, runs.Length);
        Assert.NotNull(runs[0].Element(W + "rPr")!.Element(W + "b"));
        Assert.Equal("Situation actuelle : Ligne 1\nLigne 2", TexteAvecSauts(libelle));
        Assert.Equal("N° de demande : EB-2026-0007", TexteAvecSauts(Paragraphe(rempli, "N° de demande : ")));
        Assert.Equal("Demande exprimée par : Camille Martin", TexteAvecSauts(Paragraphe(rempli, "Demande exprimée par : ")));
        Assert.Equal("Date de confirmation : 20/11/2026", TexteAvecSauts(Paragraphe(rempli, "Date de confirmation : ")));
        Assert.Equal(
            "☒ Je confirme que cette fiche reflète fidèlement le besoin que je souhaite soumettre à validation.",
            TexteAvecSauts(Paragraphe(rempli, "☒")));
    }

    [Fact]
    public void Remplir_ecrit_la_decision_et_les_observations_du_chef()
    {
        var cellules = GabaritFeb.Remplir(Minimal(), Contenu()).Descendants(W + "tc").ToArray();

        Assert.Equal("Chef de service", TexteAvecSauts(cellules[0].Element(W + "p")!));
        Assert.Equal("Validée", TexteAvecSauts(cellules[1].Element(W + "p")!));
        Assert.Equal("Dominique Bernard, le 20/11/2026", TexteAvecSauts(cellules[2].Element(W + "p")!));
        Assert.NotNull(cellules[2].Descendants(W + "rPr").SingleOrDefault());
    }

    [Fact]
    public void Remplir_sans_confirmation_laisse_la_case_vide()
    {
        var rempli = GabaritFeb.Remplir(Minimal(), Contenu(confirmee: false));

        Assert.Equal("☐ Je confirmeaide", TexteAvecSauts(Paragraphe(rempli, "☐ Je confirme")));
        Assert.Equal("Date de confirmation : Non confirmée", TexteAvecSauts(Paragraphe(rempli, "Date de confirmation : ")));
    }

    [Fact]
    public void Remplir_retire_les_runs_en_surplus()
    {
        var gabarit = Minimal(corps =>
        {
            Paragraphe(corps, "Bénéfices attendus : ").Add(Run(" (suite de l'aide)"));
            Paragraphe(corps, "Bureau : ").Add(Run(" (suite)"), Run(" (fin)"));
        });

        var rempli = GabaritFeb.Remplir(gabarit, Contenu());

        Assert.Equal(2, Paragraphe(rempli, "Bénéfices attendus : ").Elements(W + "r").Count());
        Assert.Equal("Bénéfices attendus : valeur de beneficesAttendus", TexteAvecSauts(Paragraphe(rempli, "Bénéfices attendus : ")));
        Assert.Single(Paragraphe(rempli, "Bureau : ").Elements(W + "r"));
        Assert.Equal("Bureau : Bureau 1A", TexteAvecSauts(Paragraphe(rempli, "Bureau : ")));
    }

    [Fact]
    public void Remplir_ne_modifie_pas_le_gabarit()
    {
        var gabarit = Minimal();
        var copie = new XDocument(gabarit);

        GabaritFeb.Remplir(gabarit, Contenu());

        Assert.True(XNode.DeepEquals(copie, gabarit));
    }

    [Fact]
    public void Remplir_un_gabarit_non_verifie_leve()
    {
        var gabarit = Minimal(corps => Paragraphe(corps, "Bureau : ").Remove());

        Assert.Throws<InvalidOperationException>(() => GabaritFeb.Remplir(gabarit, Contenu()));
    }

    // --- Texte d'un run ---

    [Theory]
    [InlineData("une ligne", new[] { "t:une ligne" })]
    [InlineData("a\nb", new[] { "t:a", "br", "t:b" })]
    [InlineData("a\r\nb\rc", new[] { "t:a", "br", "t:b", "br", "t:c" })]
    [InlineData("", new[] { "t:" })]
    [InlineData("\n", new[] { "t:", "br", "t:" })]
    public void EcrireTexte_garde_les_proprietes_et_change_les_sauts_de_ligne(string texte, string[] attendu)
    {
        var run = new XElement(W + "r", new XElement(W + "rPr", new XElement(W + "b")), new XElement(W + "t", "ancien"), new XElement(W + "tab"));

        GabaritFeb.EcrireTexte(run, texte);

        var elements = run.Elements().ToArray();
        Assert.Equal(W + "rPr", elements[0].Name);
        Assert.Equal(attendu, elements.Skip(1).Select(element => element.Name == W + "br" ? "br" : "t:" + element.Value).ToArray());
        Assert.All(run.Elements(W + "t"), t => Assert.Equal("preserve", (string?)t.Attribute(XNamespace.Xml + "space")));
    }

    [Theory]
    [InlineData("abc", "abc")]
    [InlineData("a\u0001b\u001Fc", "abc")]
    [InlineData("\t\n\r", "\t\n\r")]
    [InlineData("x￾y￿", "xy")]
    [InlineData("é « ’ » ☒", "é « ’ » ☒")]
    public void TexteValide_retire_les_caracteres_interdits(string texte, string attendu)
    {
        Assert.Equal(attendu, GabaritFeb.TexteValide(texte));
    }

    [Fact]
    public void TexteValide_garde_les_paires_de_substitution_et_retire_les_demi_paires()
    {
        Assert.Equal("a\U0001F600b", GabaritFeb.TexteValide("a\U0001F600b"));
        Assert.Equal("x", GabaritFeb.TexteValide("x\uD83D"));
        Assert.Equal("y", GabaritFeb.TexteValide("\uD83Dy"));
        Assert.Equal("z", GabaritFeb.TexteValide("\uDE00z"));
    }

    [Fact]
    public void Texte_met_bout_a_bout_les_textes_de_l_element()
    {
        var paragraphe = new XElement(W + "p", Run("un "), new XElement(W + "r", new XElement(W + "t", "deux"), new XElement(W + "br"), new XElement(W + "t", " trois")));

        Assert.Equal("un deux trois", GabaritFeb.Texte(paragraphe));
    }

    // --- Sérialisation ---

    [Fact]
    public void Octets_UTF8_sans_BOM_relus_a_l_identique_et_stables()
    {
        var rempli = GabaritFeb.Remplir(Minimal(), Contenu());

        var octets = GabaritFeb.Octets(rempli);

        Assert.Equal((byte)'<', octets[0]);
        Assert.Contains("standalone=\"yes\"", Encoding.UTF8.GetString(octets, 0, 80));
        Assert.True(XNode.DeepEquals(rempli, Xml(octets)));
        Assert.Equal(octets, GabaritFeb.Octets(GabaritFeb.Remplir(Minimal(), Contenu())));
    }

    [Fact]
    public void Octets_echappe_les_caracteres_speciaux_du_texte()
    {
        var texte = Encoding.UTF8.GetString(GabaritFeb.Octets(GabaritFeb.Remplir(Minimal(), Contenu())));

        // Comparaisons ORDINALES : selon la culture (ICU), un caractère de contrôle est « ignorable » et serait trouvé
        // partout (échec d'I2 du 21/11/2026). XML n'exige d'échapper que « & » et « < ».
        Assert.Contains("Outil « Vigie » &amp; &lt;portail", texte, StringComparison.Ordinal);
        Assert.DoesNotContain("\u0001", texte, StringComparison.Ordinal);
    }
}
