using ExpressionBesoin.Domain.Fiches;
using static ExpressionBesoin.Domain.Tests.Fiches.Essais;

namespace ExpressionBesoin.Domain.Tests.Fiches;

/// <summary>Contrat de la réponse de l'IA (plan §2), figé sur des réponses d'exemple.</summary>
public sealed class ReponseIATests
{
    private const string Nominale = """
        {
          "message": "  Comment les contrôles se déroulent-ils aujourd'hui ?  ",
          "propositions": [
            { "rubrique": "situationActuelle", "valeur": " Contrôles sur place, par échantillon. ", "justification": "L'agent l'a écrit." },
            { "rubrique": "difficultes", "valeur": "Vendeurs à distance hors d'atteinte.", "justification": "Déduit du message 2." }
          ],
          "horsPerimetre": false
        }
        """;

    // --- Réponses valides ---

    [Fact]
    public void Reponse_nominale_lue_et_nettoyee()
    {
        var reponse = Valeur(ReponseIA.Lire(Nominale));

        Assert.Equal("Comment les contrôles se déroulent-ils aujourd'hui ?", reponse.Message);
        Assert.False(reponse.HorsPerimetre);
        Assert.Empty(reponse.Ecartees);
        Assert.Equal(2, reponse.Propositions.Count);
        Assert.Equal(Rubrique.SituationActuelle, reponse.Propositions[0].Rubrique);
        Assert.Equal("Contrôles sur place, par échantillon.", reponse.Propositions[0].Proposition.Valeur);
        Assert.Equal("L'agent l'a écrit.", reponse.Propositions[0].Proposition.Justification);
        Assert.Equal(Rubrique.Difficultes, reponse.Propositions[1].Rubrique);
        Assert.Equal("Vendeurs à distance hors d'atteinte.", reponse.Propositions[1].Proposition.Valeur);
    }

    [Theory]
    [InlineData("{\"message\":\"Bonjour\"}", false)]
    [InlineData("{\"message\":\"Bonjour\",\"propositions\":null}", false)]
    [InlineData("{\"message\":\"Bonjour\",\"propositions\":[],\"horsPerimetre\":false}", false)]
    [InlineData("{\"message\":\"Demande d'assistance : hors périmètre.\",\"horsPerimetre\":true}", true)]
    public void Propositions_et_hors_perimetre_facultatifs(string json, bool horsPerimetre)
    {
        var reponse = Valeur(ReponseIA.Lire(json));

        Assert.Empty(reponse.Propositions);
        Assert.Empty(reponse.Ecartees);
        Assert.Equal(horsPerimetre, reponse.HorsPerimetre);
    }

    // --- Questions candidates (lot A7, DEB-Q117, DEB-Q119) ---

    [Fact]
    public void Questions_candidates_lues_les_fautives_ignorees_la_premiere_d_une_rubrique_gardee()
    {
        const string json = """
            {
              "message": "J'ai noté votre demande.",
              "propositions": [],
              "questions": [
                { "rubrique": "difficultes", "question": "  Que ne pouvez-vous plus faire ?  " },
                "texte",
                { "question": "Sans rubrique ?" },
                { "rubrique": "inconnue", "question": "Rubrique inconnue ?" },
                { "rubrique": "criteresReussite" },
                { "rubrique": "criteresReussite", "question": "   " },
                { "rubrique": "CriteresReussite", "question": "Que devrez-vous retrouver ?" },
                { "rubrique": "difficultes", "question": "Doublon ?" },
                { "rubrique": "resume", "question": "Voulez-vous le résumé ?" }
              ],
              "horsPerimetre": false
            }
            """;

        var reponse = Valeur(ReponseIA.Lire(json));

        Assert.Equal(
            new[]
            {
                new QuestionCandidate(Rubrique.Difficultes, "Que ne pouvez-vous plus faire ?"),
                new QuestionCandidate(Rubrique.CriteresReussite, "Que devrez-vous retrouver ?"),
                new QuestionCandidate(Rubrique.Resume, "Voulez-vous le résumé ?"),
            },
            reponse.Questions.ToArray());
        Assert.Empty(reponse.Ecartees);
    }

    /// <summary>Une forme inattendue de « questions » n'est jamais un refus : la question type la remplace (DEB-Q119).</summary>
    [Theory]
    [InlineData("{\"message\":\"Bonjour\"}")]
    [InlineData("{\"message\":\"Bonjour\",\"questions\":null}")]
    [InlineData("{\"message\":\"Bonjour\",\"questions\":[]}")]
    [InlineData("{\"message\":\"Bonjour\",\"questions\":\"Quoi ?\"}")]
    [InlineData("{\"message\":\"Bonjour\",\"questions\":{\"rubrique\":\"difficultes\",\"question\":\"Quoi ?\"}}")]
    public void Questions_absentes_ou_mal_formees_aucune_question_sans_refus(string json)
    {
        var reponse = Valeur(ReponseIA.Lire(json));

        Assert.Equal("Bonjour", reponse.Message);
        Assert.Empty(reponse.Questions);
    }

    [Theory]
    [InlineData("```json\n{\"message\":\"Bonjour\"}\n```")]
    [InlineData("```\n{\"message\":\"Bonjour\"}\n```")]
    [InlineData("  ```JSON\r\n{\"message\":\"Bonjour\"}\r\n```  ")]
    public void Une_cloture_de_code_autour_du_json_toleree(string texte)
    {
        Assert.Equal("Bonjour", Valeur(ReponseIA.Lire(texte)).Message);
    }

    [Fact]
    public void Code_de_rubrique_lu_casse_ignoree_et_tel_que_recu_dans_les_ecarts()
    {
        const string json = """
            {"message":"m","propositions":[
              {"rubrique":"SituationActuelle","valeur":"v","justification":"j"},
              {"rubrique":"SITUATIONACTUELLE","valeur":"w","justification":"j"}
            ]}
            """;
        var reponse = Valeur(ReponseIA.Lire(json));

        Assert.Equal(Rubrique.SituationActuelle, Assert.Single(reponse.Propositions).Rubrique);
        var ecartee = Assert.Single(reponse.Ecartees);
        Assert.Equal(1, ecartee.Position);
        Assert.Equal("SITUATIONACTUELLE", ecartee.Code);
        Assert.Equal(MotifEcart.Doublon, ecartee.Motif);
    }

    [Fact]
    public void Propositions_fautives_ecartees_et_signalees_les_autres_retenues()
    {
        const string json = """
            {
              "message": "m",
              "propositions": [
                42,
                { "valeur": "v", "justification": "j" },
                { "rubrique": "budget", "valeur": "v", "justification": "j" },
                { "rubrique": "intitule", "justification": "j" },
                { "rubrique": "intitule", "valeur": "v" },
                { "rubrique": "intitule", "valeur": "   ", "justification": "j" },
                { "rubrique": "intitule", "valeur": 12, "justification": "j" },
                { "rubrique": "intitule", "valeur": "Titre", "justification": "j" },
                { "rubrique": "intitule", "valeur": "Autre titre", "justification": "j" },
                { "rubrique": 7, "valeur": "v", "justification": "j" },
                { "rubrique": "liens", "valeur": "Aucun", "justification": "  " }
              ]
            }
            """;
        var reponse = Valeur(ReponseIA.Lire(json));

        var retenue = Assert.Single(reponse.Propositions);
        Assert.Equal(Rubrique.Intitule, retenue.Rubrique);
        Assert.Equal("Titre", retenue.Proposition.Valeur);

        Assert.Equal(
            new[]
            {
                new PropositionEcartee(0, "", MotifEcart.NonObjet),
                new PropositionEcartee(1, "", MotifEcart.RubriqueAbsente),
                new PropositionEcartee(2, "budget", MotifEcart.RubriqueInconnue),
                new PropositionEcartee(3, "intitule", MotifEcart.ValeurAbsente),
                new PropositionEcartee(4, "intitule", MotifEcart.JustificationAbsente),
                new PropositionEcartee(5, "intitule", MotifEcart.ValeurAbsente),
                new PropositionEcartee(6, "intitule", MotifEcart.ValeurAbsente),
                new PropositionEcartee(8, "intitule", MotifEcart.Doublon),
                new PropositionEcartee(9, "", MotifEcart.RubriqueAbsente),
                new PropositionEcartee(10, "liens", MotifEcart.JustificationAbsente),
            },
            reponse.Ecartees.ToArray());
    }

    [Fact]
    public void Reponses_egales_champ_par_champ()
    {
        Assert.Equal(Valeur(ReponseIA.Lire(Nominale)), Valeur(ReponseIA.Lire(Nominale)));
    }

    // --- Refus ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Voici la fiche : {\"message\":\"x\"}")]
    [InlineData("{\"message\":")]
    [InlineData("```python\n{\"message\":\"x\"}\n```")]
    [InlineData("```{\"message\":\"x\"}```")]
    [InlineData("```json\n{\"message\":\"x\"}")]
    public void Json_invalide_refuse(string texte)
    {
        var resultat = ReponseIA.Lire(texte);

        Assert.Equal("reponseIA.jsonInvalide", CodeRefus(resultat));
        Assert.StartsWith("La réponse de l'IA n'est pas un JSON valide : ", MessageRefus(resultat));
    }

    [Theory]
    [InlineData("[]", "la racine n'est pas un objet")]
    [InlineData("\"texte\"", "la racine n'est pas un objet")]
    [InlineData("{}", "« message » absent ou vide")]
    [InlineData("{\"message\":\"  \"}", "« message » absent ou vide")]
    [InlineData("{\"message\":12}", "« message » absent ou vide")]
    [InlineData("{\"Message\":\"x\"}", "« message » absent ou vide")]
    [InlineData("{\"message\":\"x\",\"horsPerimetre\":\"oui\"}", "« horsPerimetre » n'est pas un booléen")]
    [InlineData("{\"message\":\"x\",\"horsPerimetre\":null}", "« horsPerimetre » n'est pas un booléen")]
    [InlineData("{\"message\":\"x\",\"propositions\":{}}", "« propositions » n'est pas un tableau")]
    public void Forme_inattendue_refusee(string json, string detail)
    {
        var resultat = ReponseIA.Lire(json);

        Assert.Equal("reponseIA.formeInvalide", CodeRefus(resultat));
        Assert.Equal("Réponse de l'IA inattendue : " + detail + ".", MessageRefus(resultat));
    }

    // --- Clôture de code (fonction interne) ---

    [Theory]
    [InlineData("{}", "{}")]
    [InlineData("  {}\n", "{}")]
    [InlineData("```json\n{}\n```", "{}")]
    [InlineData("```\n{}\n```", "{}")]
    [InlineData("```Json\r\n{ }\r\n```", "{ }")]
    [InlineData("```python\n{}\n```", "```python\n{}\n```")]
    [InlineData("```{}```", "```{}```")]
    [InlineData("``````", "``````")]
    [InlineData("```json\n{}", "```json\n{}")]
    [InlineData("{}\n```", "{}\n```")]
    public void Retirer_cloture(string texte, string attendu)
    {
        Assert.Equal(attendu, ReponseIA.RetirerCloture(texte));
    }

    // --- De la réponse à la fiche ---

    [Fact]
    public void Propositions_lues_appliquees_a_la_fiche_sans_ecraser_la_saisie_manuelle()
    {
        var fiche = Saisir(Brouillon(), Rubrique.SituationActuelle, "Saisie de l'agent");
        var reponse = Valeur(ReponseIA.Lire(Nominale));
        var apres = Valeur(Fiche.AppliquerPropositions(fiche, reponse.Propositions));

        Assert.Equal("Saisie de l'agent", apres.ChampDe(Rubrique.SituationActuelle).Valeur);
        Assert.Equal(Origine.Manuelle, apres.ChampDe(Rubrique.SituationActuelle).Origine);
        Assert.Equal("Contrôles sur place, par échantillon.", apres.ChampDe(Rubrique.SituationActuelle).PropositionEnAttente!.Valeur);
        Assert.True(apres.ChampDe(Rubrique.Difficultes).EstProposee);
    }
}
