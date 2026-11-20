using System.Text.Json;

namespace ExpressionBesoin.Domain.Fiches;

/// <summary>Proposition retenue de la réponse de l'IA, rattachée à une rubrique connue.</summary>
public sealed record PropositionRubrique(Rubrique Rubrique, Proposition Proposition);

/// <summary>Motif pour lequel une proposition de l'IA est écartée (DEB-Q19).</summary>
public enum MotifEcart : byte
{
    NonObjet,
    RubriqueAbsente,
    RubriqueInconnue,
    ValeurAbsente,
    JustificationAbsente,
    Doublon,
}

/// <summary>
/// Proposition écartée : <paramref name="Position"/> dans le tableau <c>propositions</c> (à partir de 0),
/// <paramref name="Code"/> de rubrique tel que reçu (vide s'il manque), motif.
/// </summary>
public sealed record PropositionEcartee(int Position, string Code, MotifEcart Motif);

/// <summary>
/// Réponse de l'IA (plan §2) : <c>{ message, propositions: [{ rubrique, valeur, justification }], horsPerimetre }</c>.
/// </summary>
public sealed record ReponseIA(
    string Message,
    ListeImmuable<PropositionRubrique> Propositions,
    ListeImmuable<PropositionEcartee> Ecartees,
    bool HorsPerimetre)
{
    /// <summary>
    /// Lecture du texte rendu par l'IA. Une seule clôture <c>```json</c> (ou <c>```</c>) autour du JSON est tolérée.
    /// JSON invalide ou forme inattendue (racine non objet, <c>message</c> absent ou vide, <c>horsPerimetre</c> non
    /// booléen, <c>propositions</c> non tableau) → refus, la fiche reste inchangée. Une proposition fautive (rubrique
    /// inconnue, valeur ou justification absente, doublon) est écartée et signalée ; la première d'une rubrique est
    /// gardée. Valeurs nettoyées de leurs espaces ; <c>propositions</c> et <c>horsPerimetre</c> absents → vide, faux.
    /// </summary>
    public static Resultat<ReponseIA> Lire(string texte)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(RetirerCloture(texte));
        }
        catch (JsonException exception)
        {
            return new Refus("reponseIA.jsonInvalide", "La réponse de l'IA n'est pas un JSON valide : " + exception.Message);
        }

        try
        {
            return Interpreter(document.RootElement);
        }
        finally
        {
            document.Dispose();
        }
    }

    internal static string RetirerCloture(string texte)
    {
        var nettoye = texte.AsSpan().Trim();
        if (nettoye.Length < 6
            || !nettoye.StartsWith("```", StringComparison.Ordinal)
            || !nettoye.EndsWith("```", StringComparison.Ordinal))
        {
            return nettoye.ToString();
        }

        var interieur = nettoye[3..^3];
        var finEtiquette = interieur.IndexOf('\n');
        if (finEtiquette < 0)
        {
            return nettoye.ToString();
        }

        var etiquette = interieur[..finEtiquette].Trim();
        if (!etiquette.IsEmpty && !etiquette.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            return nettoye.ToString();
        }

        return interieur[(finEtiquette + 1)..].Trim().ToString();
    }

    private static Resultat<ReponseIA> Interpreter(JsonElement racine)
    {
        if (racine.ValueKind != JsonValueKind.Object)
        {
            return FormeInvalide("la racine n'est pas un objet");
        }

        var message = Texte(racine, "message");
        if (message is null)
        {
            return FormeInvalide("« message » absent ou vide");
        }

        var horsPerimetre = false;
        if (racine.TryGetProperty("horsPerimetre", out var horsPerimetreJson))
        {
            if (horsPerimetreJson.ValueKind != JsonValueKind.True && horsPerimetreJson.ValueKind != JsonValueKind.False)
            {
                return FormeInvalide("« horsPerimetre » n'est pas un booléen");
            }

            horsPerimetre = horsPerimetreJson.ValueKind == JsonValueKind.True;
        }

        var retenues = ListeImmuable<PropositionRubrique>.Vide;
        var ecartees = ListeImmuable<PropositionEcartee>.Vide;
        if (racine.TryGetProperty("propositions", out var propositions) && propositions.ValueKind != JsonValueKind.Null)
        {
            if (propositions.ValueKind != JsonValueKind.Array)
            {
                return FormeInvalide("« propositions » n'est pas un tableau");
            }

            var vues = new bool[Catalogue.Toutes.Length];
            var position = 0;
            foreach (var element in propositions.EnumerateArray())
            {
                var lue = LireProposition(element, position, vues);
                if (lue.Retenue is not null)
                {
                    retenues = retenues.Ajouter(lue.Retenue);
                }
                else
                {
                    ecartees = ecartees.Ajouter(lue.Ecartee!);
                }

                position++;
            }
        }

        return Resultat.Succes(new ReponseIA(message, retenues, ecartees, horsPerimetre));
    }

    private static (PropositionRubrique? Retenue, PropositionEcartee? Ecartee) LireProposition(
        JsonElement element,
        int position,
        bool[] vues)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return (null, new PropositionEcartee(position, "", MotifEcart.NonObjet));
        }

        var code = Texte(element, "rubrique");
        if (code is null)
        {
            return (null, new PropositionEcartee(position, "", MotifEcart.RubriqueAbsente));
        }

        if (!Catalogue.EssayerTrouver(code, out var rubrique))
        {
            return (null, new PropositionEcartee(position, code, MotifEcart.RubriqueInconnue));
        }

        var valeur = Texte(element, "valeur");
        if (valeur is null)
        {
            return (null, new PropositionEcartee(position, code, MotifEcart.ValeurAbsente));
        }

        var justification = Texte(element, "justification");
        if (justification is null)
        {
            return (null, new PropositionEcartee(position, code, MotifEcart.JustificationAbsente));
        }

        if (vues[(int)rubrique])
        {
            return (null, new PropositionEcartee(position, code, MotifEcart.Doublon));
        }

        vues[(int)rubrique] = true;
        return (new PropositionRubrique(rubrique, new Proposition(valeur, justification)), null);
    }

    /// <summary>Texte non vide d'une propriété (espaces retirés), <c>null</c> si absente, non textuelle ou vide.</summary>
    private static string? Texte(JsonElement objet, string nom)
    {
        if (!objet.TryGetProperty(nom, out var propriete) || propriete.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var texte = propriete.GetString()!.Trim();
        return texte.Length == 0 ? null : texte;
    }

    private static Refus FormeInvalide(string detail) =>
        new("reponseIA.formeInvalide", "Réponse de l'IA inattendue : " + detail + ".");
}
