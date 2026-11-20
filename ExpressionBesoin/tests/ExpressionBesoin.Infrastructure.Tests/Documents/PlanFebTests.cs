using ExpressionBesoin.Domain.Fiches;
using ExpressionBesoin.Infrastructure.Documents;
using static ExpressionBesoin.Infrastructure.Tests.Documents.EssaisDocuments;

namespace ExpressionBesoin.Infrastructure.Tests.Documents;

public sealed class PlanFebTests
{
    [Fact]
    public void Chaque_rubrique_du_catalogue_a_un_libelle()
    {
        Assert.Equal(Catalogue.Toutes.Length, PlanFeb.Rubriques.Length);
        Assert.All(PlanFeb.Rubriques, marqueur => Assert.Equal(Forme.Libelle, marqueur.Forme));
        Assert.Equal("Liens avec d’autres outils ou demandes : ", PlanFeb.Rubriques[(int)Rubrique.Liens].Texte);
        Assert.Equal("Points restant à clarifier : ", PlanFeb.Rubriques[(int)Rubrique.PointsAClarifier].Texte);
    }

    [Fact]
    public void Aucun_marqueur_n_est_le_debut_d_un_autre()
    {
        var textes = PlanFeb.Marqueurs.Select(marqueur => marqueur.Texte).ToArray();

        Assert.Equal(24, textes.Length);
        foreach (var texte in textes)
        {
            Assert.Single(textes, autre => autre.StartsWith(texte, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Remplacements_d_une_fiche_confirmee()
    {
        var remplacements = PlanFeb.Remplacements(Contenu());

        Assert.Equal(PlanFeb.Marqueurs.ToArray(), remplacements.Select(remplacement => remplacement.Marqueur).ToArray());
        Assert.Equal("EB-2026-0007", remplacements[0].Texte);
        Assert.Equal("Demande exprimée par : Camille Martin", remplacements[1].Texte);
        Assert.Equal("Date de la demande : 20/11/2026", remplacements[2].Texte);
        Assert.Equal("Bureau : Bureau 1A", remplacements[3].Texte);
        Assert.Equal("Valideur : Dominique Bernard", remplacements[4].Texte);
        Assert.Equal(Rubriques, remplacements.Skip(5).Take(17).Select(remplacement => remplacement.Texte).ToArray());
        Assert.Equal(PlanFeb.ConfirmationCochee, remplacements[22].Texte);
        Assert.Equal("Date de confirmation : 20/11/2026", remplacements[23].Texte);
    }

    [Fact]
    public void Remplacements_d_une_fiche_non_confirmee_laissent_la_case()
    {
        var remplacements = PlanFeb.Remplacements(Contenu(confirmee: false));

        Assert.Equal(23, remplacements.Length);
        Assert.DoesNotContain(remplacements, remplacement => remplacement.Marqueur == PlanFeb.Confirmation);
        Assert.Equal("Date de confirmation : Non confirmée", remplacements[^1].Texte);
    }
}
