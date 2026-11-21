using System.Buffers;
using System.Collections.Frozen;
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
/// <list type="number">
/// <item>Message de demande hors périmètre (incident, assistance, équipement, habilitation) → <c>horsPerimetre</c>.</item>
/// <item>Premier tour : la demande initiale est proposée comme intitulé ; la question porte sur la prochaine rubrique.</item>
/// <item>Prochaine rubrique = résumé : synthèse des rubriques obligatoires retenues, une ligne « libellé : valeur » chacune ;
/// aucune retenue → rien à proposer, l'agent est invité à trancher les propositions en attente.</item>
/// <item>Sinon : le message répond à la question posée, il est proposé pour la prochaine rubrique ; question suivante.</item>
/// <item>Plus de rubrique à questionner : invitation à relire, confirmer et soumettre.</item>
/// </list>
/// </summary>
public static class ScenarioDialogue
{
    /// <summary>Mots qui signalent une demande d'assistance plutôt qu'une évolution du SI (minuscules, sans accent).</summary>
    public static readonly FrozenSet<string> MotsHorsPerimetre =
        new[] { "incident", "panne", "mot de passe", "imprimante", "ordinateur", "habilitation", "acces refuse" }.ToFrozenSet(StringComparer.Ordinal);

    public const string MessageHorsPerimetre =
        "Cette demande relève de l'assistance (incident, équipement ou habilitation), pas d'une évolution du système "
        + "d'information : adressez-la au support. Je reste disponible pour une demande d'évolution.";

    public const string MessageFin =
        "Les rubriques obligatoires sont renseignées ou proposées. Relisez la fiche, acceptez ou corrigez les "
        + "propositions, puis confirmez et soumettez.";

    public const string MessageResume = "Voici un résumé tiré des rubriques retenues : acceptez-le ou corrigez-le.";

    public const string MessageResumeImpossible =
        "Acceptez ou corrigez d'abord les propositions en attente : le résumé ne reprend que les rubriques retenues.";

    public const string JustificationReprise = "Reprend les mots de l'agent.";

    public const string JustificationResume = "Synthèse des rubriques retenues, sans ajout.";

    private static readonly FrozenDictionary<string, string> Questions = new Dictionary<string, string>
    {
        ["application"] = "Quelle application ou quel service du système d'information est concerné ?",
        ["intitule"] = "Quel intitulé court donneriez-vous à cette demande ?",
        ["situationActuelle"] = "Comment cela se passe-t-il aujourd'hui ?",
        ["difficultes"] = "Quelles difficultés rencontrez-vous ?",
        ["resultatRecherche"] = "Quel résultat recherchez-vous ?",
        ["criteresReussite"] = "À quoi verra-t-on que la demande est satisfaite ?",
        ["utilisateursConcernes"] = "Quels utilisateurs ou services sont concernés ?",
        ["processusMetier"] = "Quel processus métier est touché ?",
        ["beneficesAttendus"] = "Quels bénéfices attendez-vous ?",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly JsonWriterOptions Ecriture = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly string CodeIntitule = Catalogue.Definition(Rubrique.Intitule).Code;

    private static readonly string CodeResume = Catalogue.Definition(Rubrique.Resume).Code;

    public static string Repondre(ContexteTour contexte)
    {
        if (EstHorsPerimetre(contexte.Message))
        {
            return Json(MessageHorsPerimetre, [], horsPerimetre: true);
        }

        var cible = contexte.ProchaineRubrique;
        if (cible is null)
        {
            return Json(MessageFin, [], horsPerimetre: false);
        }

        if (contexte.Historique.Count == 0 && cible != CodeIntitule && EstLibre(contexte, CodeIntitule))
        {
            return Json(Question(contexte, cible), [(CodeIntitule, contexte.Message, JustificationReprise)], horsPerimetre: false);
        }

        if (cible == CodeResume)
        {
            var synthese = Synthese(contexte);
            return synthese.Length == 0
                ? Json(MessageResumeImpossible, [], horsPerimetre: false)
                : Json(MessageResume, [(CodeResume, synthese, JustificationResume)], horsPerimetre: false);
        }

        var suivante = Suivante(contexte, cible);
        return Json(
            suivante is null ? MessageFin : Question(contexte, suivante),
            [(cible, contexte.Message, JustificationReprise)],
            horsPerimetre: false);
    }

    /// <summary>Recherche des mots hors périmètre, casse et accents ignorés.</summary>
    internal static bool EstHorsPerimetre(string message)
    {
        var normalise = SansAccents(message.ToLowerInvariant());
        foreach (var mot in MotsHorsPerimetre)
        {
            if (normalise.Contains(mot, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static string SansAccents(string texte)
    {
        var decompose = texte.Normalize(NormalizationForm.FormD);
        var resultat = new StringBuilder(decompose.Length);
        foreach (var caractere in decompose)
        {
            if (char.GetUnicodeCategory(caractere) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                resultat.Append(caractere);
            }
        }

        return resultat.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Question de la rubrique ; à défaut d'une question écrite, formulation générique sur son libellé.</summary>
    internal static string Question(ContexteTour contexte, string code) =>
        Questions.GetValueOrDefault(code)
        ?? $"Pouvez-vous préciser : {Etat(contexte, code)?.Libelle ?? code} ?";

    /// <summary>Rubrique questionnée après <paramref name="cible"/> : ordre de questionnement, vide et sans proposition.</summary>
    internal static string? Suivante(ContexteTour contexte, string cible)
    {
        var apres = false;
        foreach (var rubrique in Catalogue.OrdreQuestionnement)
        {
            var code = Catalogue.Definition(rubrique).Code;
            if (apres && EstLibre(contexte, code))
            {
                return code;
            }

            apres |= code == cible;
        }

        return null;
    }

    internal static string Synthese(ContexteTour contexte) =>
        string.Join("\n", contexte.Rubriques
            .Where(etat => etat.Obligatoire && etat.Code != CodeResume && etat.Valeur is not null)
            .Select(etat => $"{etat.Libelle} : {etat.Valeur}"));

    private static EtatRubrique? Etat(ContexteTour contexte, string code) =>
        contexte.Rubriques.FirstOrDefault(etat => etat.Code == code);

    private static bool EstLibre(ContexteTour contexte, string code) =>
        Etat(contexte, code) is { Valeur: null, PropositionEnAttente: null };

    private static string Json(string message, (string Rubrique, string Valeur, string Justification)[] propositions, bool horsPerimetre)
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
            ecrivain.WriteBoolean("horsPerimetre", horsPerimetre);
            ecrivain.WriteEndObject();
        }

        return Encoding.UTF8.GetString(tampon.WrittenSpan);
    }
}
