using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExpressionBesoin.Application.Fiches;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Infrastructure.IA;

/// <summary>
/// IA SCÉNARISÉE (plan §7, DEB-Q41), fonction pure : rend une réponse au contrat du plan §2, relue par
/// <see cref="ReponseIA.Lire"/> comme celle de l'IA réelle. Elle n'invente rien : ses propositions reprennent mot pour
/// mot le message de l'agent, ou les valeurs déjà retenues pour le résumé.
/// Elle ne classe jamais une demande hors périmètre (DEB-Q89) : toute demande adressée au service informatique
/// (assistance, exploitation, réseau, applications, développement) est une fiche ; reconnaître une demande étrangère
/// au service informatique relève de l'IA réelle, aucune liste de mots ne le fait sûrement.
/// Comme l'IA réelle depuis A5 (DEB-Q100), elle ne pose aucune question : l'application ajoute la question type de la
/// rubrique suivante après la fusion (<see cref="Dialogue.Composer"/>). La rubrique questionnée est la prochaine
/// rubrique du contexte.
/// <list type="number">
/// <item>Premier tour : la demande initiale est proposée comme intitulé.</item>
/// <item>Rubrique questionnée = résumé : synthèse des rubriques obligatoires retenues, une ligne « libellé : valeur »
/// chacune ; aucune retenue → rien à proposer, l'agent est invité à trancher les propositions en attente.</item>
/// <item>Sinon : le message répond à la question posée, il est proposé pour la rubrique questionnée.</item>
/// <item>Plus de rubrique questionnée : rien à proposer.</item>
/// </list>
/// </summary>
public static class ScenarioDialogue
{
    public const string MessageIntitule = "Je propose votre demande comme intitulé.";

    public const string MessageReprise = "Je propose votre réponse pour la rubrique questionnée.";

    public const string MessageRien = "Je n'ai rien de plus à proposer.";

    public const string MessageResume = "Voici un résumé tiré des rubriques retenues : acceptez-le ou corrigez-le.";

    public const string MessageResumeImpossible =
        "Acceptez ou corrigez d'abord les propositions en attente : le résumé ne reprend que les rubriques retenues.";

    public const string JustificationReprise = "Reprend les mots de l'agent.";

    public const string JustificationResume = "Synthèse des rubriques retenues, sans ajout.";

    private static readonly JsonWriterOptions Ecriture = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly string CodeIntitule = Catalogue.Definition(Rubrique.Intitule).Code;

    private static readonly string CodeResume = Catalogue.Definition(Rubrique.Resume).Code;

    public static string Repondre(ContexteTour contexte)
    {
        var cible = contexte.ProchaineRubrique;
        if (cible is null)
        {
            return Json(MessageRien, []);
        }

        if (contexte.Historique.Count == 0 && cible != CodeIntitule && EstLibre(contexte, CodeIntitule))
        {
            return Json(MessageIntitule, [(CodeIntitule, contexte.Message, JustificationReprise)]);
        }

        if (cible == CodeResume)
        {
            var synthese = Synthese(contexte);
            return synthese.Length == 0
                ? Json(MessageResumeImpossible, [])
                : Json(MessageResume, [(CodeResume, synthese, JustificationResume)]);
        }

        return Json(MessageReprise, [(cible, contexte.Message, JustificationReprise)]);
    }

    internal static string Synthese(ContexteTour contexte) =>
        string.Join("\n", contexte.Rubriques
            .Where(etat => etat.Obligatoire && etat.Code != CodeResume && etat.Valeur is not null)
            .Select(etat => $"{etat.Libelle} : {etat.Valeur}"));

    private static EtatRubrique? Etat(ContexteTour contexte, string code) =>
        contexte.Rubriques.FirstOrDefault(etat => etat.Code == code);

    private static bool EstLibre(ContexteTour contexte, string code) =>
        Etat(contexte, code) is { Valeur: null, PropositionEnAttente: null };

    private static string Json(string message, (string Rubrique, string Valeur, string Justification)[] propositions)
    {
        var tampon = new ArrayBufferWriter<byte>(256);
        using (var ecrivain = new Utf8JsonWriter(tampon, Ecriture))
        {
            ecrivain.WriteStartObject();
            ecrivain.WriteString("message", message);
            ecrivain.WriteStartArray("propositions");
            foreach (var (rubrique, valeur, justification) in propositions)
            {
                ecrivain.WriteStartObject();
                ecrivain.WriteString("rubrique", rubrique);
                ecrivain.WriteString("valeur", valeur);
                ecrivain.WriteString("justification", justification);
                ecrivain.WriteEndObject();
            }

            ecrivain.WriteEndArray();
            ecrivain.WriteBoolean("horsPerimetre", false);
            ecrivain.WriteEndObject();
        }

        return Encoding.UTF8.GetString(tampon.WrittenSpan);
    }
}
