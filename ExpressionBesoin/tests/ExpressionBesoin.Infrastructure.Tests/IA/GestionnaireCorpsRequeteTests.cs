using System.Net;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// Maillon du corps de la requête (lot A5b) : ce que l'adaptateur ne fait pas passer, une requête sans corps. Le reste
/// (corps nettoyé, journal d'une réponse vide) est éprouvé de bout en bout par <see cref="AssistantIAAlbertTests"/>.
/// </summary>
public sealed class GestionnaireCorpsRequeteTests
{
    [Fact]
    public async Task Requete_sans_corps_transmise_telle_quelle_sans_journal()
    {
        var faux = new FauxFournisseurIA(() => FauxFournisseurIA.Completion(null));
        var journal = new JournalEssai();
        using var client = new HttpClient(new GestionnaireCorpsRequete(journal) { InnerHandler = faux });

        using var reponse = await client.GetAsync(new Uri("https://albert.test/v1/models"), Essais.Annulation);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var requete = Assert.Single(faux.Requetes);
        Assert.Equal((HttpMethod.Get, (string?)null), (requete.Methode, requete.Corps));
        Assert.Empty(journal.Ecritures);
    }
}
