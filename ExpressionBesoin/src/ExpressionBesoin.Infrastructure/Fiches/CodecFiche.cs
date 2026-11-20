using System.Buffers;
using System.Collections.Frozen;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExpressionBesoin.Domain;
using ExpressionBesoin.Domain.Fiches;

namespace ExpressionBesoin.Infrastructure.Fiches;

/// <summary>
/// Contenu JSON d'une fiche, colonne <c>Contenu</c> du dépôt (DEB-Q12, DEB-Q28). Fonctions pures. Format version 1 :
/// rubriques indexées par leur code du <see cref="Catalogue"/> (jamais par l'ordinal de <see cref="Rubrique"/>),
/// énumérations par leur nom, instants en ISO 8601 ; seuls les champs différents de <see cref="Champ.Initial"/> et les
/// propriétés non nulles sont écrits. Écriture et lecture à la main (<see cref="Utf8JsonWriter"/>,
/// <see cref="JsonDocument"/>) : ni réflexion, ni code généré à couvrir.
/// </summary>
public static class CodecFiche
{
    /// <summary>Version du format, propriété <c>v</c>.</summary>
    public const int Version = 1;

    // Contenu stocké en base, jamais inséré dans une page : les accents et apostrophes restent lisibles et compacts.
    private static readonly JsonWriterOptions OptionsEcriture = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Encoder(FicheBesoin fiche)
    {
        var tampon = new ArrayBufferWriter<byte>(2048);
        var ecrivain = new Utf8JsonWriter(tampon, OptionsEcriture);
        try
        {
            Ecrire(ecrivain, fiche);
        }
        finally
        {
            ecrivain.Dispose();
        }

        return Encoding.UTF8.GetString(tampon.WrittenSpan);
    }

    /// <summary>
    /// Fiche relue. Un contenu illisible (JSON invalide, version, rubrique ou nom inconnus, propriété absente) lève
    /// <see cref="InvalidDataException"/> : c'est une panne du stockage, jamais un refus métier (DEB-Q22).
    /// </summary>
    public static FicheBesoin Decoder(string contenu)
    {
        try
        {
            var document = JsonDocument.Parse(contenu);
            try
            {
                return Lire(document.RootElement);
            }
            finally
            {
                document.Dispose();
            }
        }
        catch (Exception erreur)
        {
            throw new InvalidDataException("Contenu de fiche illisible : " + erreur.Message, erreur);
        }
    }

    private static void Ecrire(Utf8JsonWriter ecrivain, FicheBesoin fiche)
    {
        ecrivain.WriteStartObject();
        ecrivain.WriteNumber("v", Version);
        ecrivain.WriteString("id", fiche.Id);
        EcrireSiPresent(ecrivain, "numero", fiche.Numero);
        ecrivain.WriteString("statut", NomsEnum<Statut>.Nom(fiche.Statut));

        ecrivain.WriteStartObject("enTete");
        ecrivain.WriteString("demandeur", fiche.EnTete.Demandeur);
        ecrivain.WriteString("bureau", fiche.EnTete.Bureau);
        ecrivain.WriteString("valideur", fiche.EnTete.Valideur);
        ecrivain.WriteString("creeeLe", fiche.EnTete.CreeeLe);
        ecrivain.WriteEndObject();

        ecrivain.WriteStartObject("champs");
        foreach (var definition in Catalogue.Toutes)
        {
            var champ = fiche.ChampDe(definition.Rubrique);
            if (champ != Champ.Initial)
            {
                EcrireChamp(ecrivain, definition.Code, champ);
            }
        }

        ecrivain.WriteEndObject();

        if (fiche.ConfirmeeLe is { } confirmeeLe)
        {
            ecrivain.WriteString("confirmeeLe", confirmeeLe);
        }

        ecrivain.WriteStartArray("decisions");
        foreach (var decision in fiche.Decisions)
        {
            ecrivain.WriteStartObject();
            ecrivain.WriteString("type", NomsEnum<TypeDecision>.Nom(decision.Type));
            ecrivain.WriteString("auteur", decision.Auteur);
            ecrivain.WriteString("le", decision.Le);
            EcrireSiPresent(ecrivain, "commentaire", decision.Commentaire);
            ecrivain.WriteEndObject();
        }

        ecrivain.WriteEndArray();
        ecrivain.WriteEndObject();
    }

    private static void EcrireChamp(Utf8JsonWriter ecrivain, string code, Champ champ)
    {
        ecrivain.WriteStartObject(code);
        EcrireSiPresent(ecrivain, "valeur", champ.Valeur);
        if (champ.Origine != Origine.Vide)
        {
            ecrivain.WriteString("origine", NomsEnum<Origine>.Nom(champ.Origine));
        }

        if (champ.PropositionEnAttente is { } proposition)
        {
            ecrivain.WriteStartObject("proposition");
            ecrivain.WriteString("valeur", proposition.Valeur);
            ecrivain.WriteString("justification", proposition.Justification);
            ecrivain.WriteEndObject();
        }

        ecrivain.WriteEndObject();
    }

    private static void EcrireSiPresent(Utf8JsonWriter ecrivain, string nom, string? valeur)
    {
        if (valeur is not null)
        {
            ecrivain.WriteString(nom, valeur);
        }
    }

    private static FicheBesoin Lire(JsonElement racine)
    {
        var version = racine.GetProperty("v").GetInt32();
        if (version != Version)
        {
            throw new FormatException($"version {version} inconnue (attendue : {Version}).");
        }

        var enTete = racine.GetProperty("enTete");
        var champs = new Champ[Catalogue.Toutes.Length];
        Array.Fill(champs, Champ.Initial);
        foreach (var propriete in racine.GetProperty("champs").EnumerateObject())
        {
            if (!Catalogue.EssayerTrouver(propriete.Name, out var rubrique))
            {
                throw new FormatException($"rubrique « {propriete.Name} » inconnue.");
            }

            champs[(int)rubrique] = LireChamp(propriete.Value);
        }

        var decisions = new List<Decision>();
        foreach (var element in racine.GetProperty("decisions").EnumerateArray())
        {
            decisions.Add(new Decision(
                NomsEnum<TypeDecision>.Lire(element.GetProperty("type").GetString()),
                Texte(element, "auteur"),
                element.GetProperty("le").GetDateTimeOffset(),
                TexteOuNull(element, "commentaire")));
        }

        return new FicheBesoin(
            racine.GetProperty("id").GetGuid(),
            TexteOuNull(racine, "numero"),
            NomsEnum<Statut>.Lire(racine.GetProperty("statut").GetString()),
            new EnTete(
                Texte(enTete, "demandeur"),
                Texte(enTete, "bureau"),
                Texte(enTete, "valideur"),
                enTete.GetProperty("creeeLe").GetDateTimeOffset()),
            ListeImmuable<Champ>.De(champs),
            racine.TryGetProperty("confirmeeLe", out var confirmeeLe) ? confirmeeLe.GetDateTimeOffset() : null,
            ListeImmuable<Decision>.De(decisions));
    }

    private static Champ LireChamp(JsonElement element) =>
        new(
            TexteOuNull(element, "valeur"),
            element.TryGetProperty("origine", out var origine) ? NomsEnum<Origine>.Lire(origine.GetString()) : Origine.Vide,
            element.TryGetProperty("proposition", out var proposition)
                ? new Proposition(Texte(proposition, "valeur"), Texte(proposition, "justification"))
                : null);

    private static string Texte(JsonElement element, string nom) =>
        element.GetProperty(nom).GetString() ?? throw new FormatException($"« {nom} » est nul.");

    private static string? TexteOuNull(JsonElement element, string nom) =>
        element.TryGetProperty(nom, out var valeur) ? valeur.GetString() : null;
}

/// <summary>
/// Noms exacts (casse comprise) des valeurs d'une énumération : aucun nombre ni combinaison « A, B » accepté à la
/// lecture, contrairement à <see cref="Enum.Parse{TEnum}(string)"/>.
/// </summary>
internal static class NomsEnum<T>
    where T : struct, Enum
{
    private static readonly FrozenDictionary<T, string> ParValeur =
        Enum.GetValues<T>().ToFrozenDictionary(valeur => valeur, valeur => valeur.ToString());

    private static readonly FrozenDictionary<string, T> ParNom =
        ParValeur.ToFrozenDictionary(paire => paire.Value, paire => paire.Key, StringComparer.Ordinal);

    public static string Nom(T valeur) => ParValeur[valeur];

    public static T Lire(string? nom) =>
        nom is not null && ParNom.TryGetValue(nom, out var valeur)
            ? valeur
            : throw new FormatException($"« {nom} » n'est pas une valeur de {typeof(T).Name}.");
}
