using System.Text;
using ExpressionBesoin.Infrastructure.IA;

namespace ExpressionBesoin.Infrastructure.Tests.IA;

/// <summary>
/// Corps des échanges avec l'API compatible OpenAI (lot A5b, DEB-A5, DEB-Q113, DEB-Q114) : tables de vérité ; attendus
/// recalculés par une réplique Python indépendante avant livraison.
/// </summary>
public sealed class CorpsRequeteTests
{
    private static byte[] Octets(string texte) => Encoding.UTF8.GetBytes(texte);

    private static string Nettoye(string corps) => Encoding.UTF8.GetString(CorpsRequete.Nettoyer(Octets(corps)));

    /// <summary>
    /// Forme des messages du client OpenAI .NET : contenu en parties, champs ajoutés au message assistant (nom, refus nul,
    /// outils vides). Seuls partent rôle, contenu en chaîne et outils non vides ; le reste du corps est gardé.
    /// </summary>
    [Fact]
    public void Nettoyer_messages_comme_CSAgent_reste_du_corps_garde()
    {
        const string corps =
            "{\"model\":\"m\",\"messages\":[{\"role\":\"system\",\"content\":\"Consignes é\"},"
            + "{\"role\":\"user\",\"content\":[{\"type\":\"text\",\"text\":\"Bon\"},{\"type\":\"text\",\"text\":\"jour\"}]},"
            + "{\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"Question ?\"}],\"name\":\"Agent\",\"refusal\":null,\"tool_calls\":[]},"
            + "{\"role\":\"assistant\",\"content\":null,\"tool_calls\":[{\"id\":\"t1\"}]},"
            + "{\"role\":\"tool\",\"tool_call_id\":\"t1\"},42,null],"
            + "\"response_format\":{\"type\":\"json_schema\"},\"temperature\":0.1}";

        Assert.Equal(
            "{\"model\":\"m\",\"messages\":[{\"role\":\"system\",\"content\":\"Consignes é\"},"
            + "{\"role\":\"user\",\"content\":\"Bonjour\"},"
            + "{\"role\":\"assistant\",\"content\":\"Question ?\"},"
            + "{\"role\":\"assistant\",\"tool_calls\":[{\"id\":\"t1\"}],\"content\":\"\"},"
            + "{\"role\":\"tool\",\"tool_call_id\":\"t1\",\"content\":\"\"},42,null],"
            + "\"response_format\":{\"type\":\"json_schema\"},\"temperature\":0.1}",
            Nettoye(corps));
    }

    [Theory]
    [InlineData("[{\"type\":\"image_url\",\"image_url\":{\"url\":\"x\"}}]", "[{\"type\":\"image_url\",\"image_url\":{\"url\":\"x\"}}]")]
    [InlineData("[{\"text\":\"a\"}]", "[{\"text\":\"a\"}]")]
    [InlineData("[{\"type\":1,\"text\":\"a\"}]", "[{\"type\":1,\"text\":\"a\"}]")]
    [InlineData("[{\"type\":\"text\",\"text\":1}]", "[{\"type\":\"text\",\"text\":1}]")]
    [InlineData("[{\"type\":\"text\"}]", "[{\"type\":\"text\"}]")]
    [InlineData("[\"a\"]", "[\"a\"]")]
    [InlineData("[]", "\"\"")]
    [InlineData("12", "12")]
    [InlineData("\"<b> & é\"", "\"<b> & é\"")]
    public void Nettoyer_contenu_en_chaine_seulement_s_il_n_a_que_des_parties_texte(string contenu, string attendu) =>
        Assert.Equal(
            "{\"messages\":[{\"role\":\"user\",\"content\":" + attendu + "}]}",
            Nettoye("{\"messages\":[{\"role\":\"user\",\"content\":" + contenu + "}]}"));

    [Theory]
    [InlineData("")]
    [InlineData("pas du json")]
    [InlineData("null")]
    [InlineData("[1,2]")]
    [InlineData("{\"model\":\"m\"}")]
    [InlineData("{\"messages\":\"x\"}")]
    public void Nettoyer_corps_d_une_autre_forme_octets_intacts(string corps)
    {
        var octets = Octets(corps);
        Assert.Same(octets, CorpsRequete.Nettoyer(octets));
    }

    [Theory]
    [InlineData("{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null},\"finish_reason\":\"stop\"}]}", true)]
    [InlineData("{\"choices\":[{\"message\":{\"role\":\"assistant\"}}]}", true)]
    [InlineData("{\"choices\":[{\"message\":{\"content\":\"\"}}]}", true)]
    [InlineData("{\"choices\":[{\"message\":{\"content\":\"  \\n\"}}]}", true)]
    [InlineData("{\"choices\":[{\"message\":{\"content\":\"{}\"}}]}", false)]
    [InlineData("{\"choices\":[{\"message\":{\"content\":[]}}]}", false)]
    [InlineData("{\"choices\":[{\"index\":0}]}", false)]
    [InlineData("{\"choices\":[]}", false)]
    [InlineData("{\"choices\":{}}", false)]
    [InlineData("{\"detail\":\"erreur\"}", false)]
    [InlineData("[]", false)]
    [InlineData("pas du json", false)]
    public void Contenu_vide_du_premier_choix(string reponse, bool vide) =>
        Assert.Equal(vide, CorpsRequete.ContenuVide(Octets(reponse)));

    [Fact]
    public void Diagnostic_requete_et_reponse_en_clair_avec_leur_taille() =>
        Assert.Equal(
            "Réponse vide de l'IA (DEB-A5). Requête envoyée (14 octets) : {\"model\":\"é\"} | Réponse reçue (2 octets) : {}",
            CorpsRequete.Diagnostic(Octets("{\"model\":\"é\"}"), Octets("{}")));
}
