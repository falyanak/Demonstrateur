using System.Buffers.Binary;
using ExpressionBesoin.Domain;

namespace ExpressionBesoin.Application.Dictee;

/// <summary>
/// Décisions de la dictée (lot S1, DEB-Q131 à DEB-Q133), fonctions PURES. Le navigateur envoie un WAV CANONIQUE : en-tête
/// de 44 octets, PCM 16 bits, mono, 16 kHz (format accepté tel quel par Whisper, sans conversion côté serveur : ni ffmpeg
/// ni paquet). Tout autre format est refusé, jamais deviné. La transcription n'est qu'une saisie : elle revient dans la zone
/// de texte de l'agent, qui la relit et l'envoie lui-même (DEB-Q133).
/// </summary>
public static class DecisionsDictee
{
    /// <summary>Fréquence d'échantillonnage attendue (sonde du 22/11/2026 : format de la synthèse, transcrit sans écart).</summary>
    public const int Frequence = 16_000;

    /// <summary>Mono, 16 bits : deux octets par échantillon.</summary>
    public const int OctetsParSeconde = Frequence * 2;

    /// <summary>Taille de l'en-tête canonique (RIFF, fmt de 16 octets, data).</summary>
    public const int EnTete = 44;

    /// <summary>Durée d'une dictée arrêtée par le navigateur (DEB-Q133) : 120 s ; 5 s de marge pour l'arrêt.</summary>
    public const int SecondesMax = 125;

    /// <summary>Taille la plus grande acceptée : 4 000 044 octets (Albert accepte 20 Mo par fichier).</summary>
    public const int OctetsMax = EnTete + (SecondesMax * OctetsParSeconde);

    public static readonly Refus Indisponible = new(
        "dictee.indisponible",
        "Dictée indisponible : le profil de l'IA en service n'a pas de modèle de transcription.");

    public static readonly Refus TropLongue = new("dictee.tropLongue", "Dictée trop longue : deux minutes au plus.");

    public static readonly Refus FormatInvalide = new(
        "dictee.formatInvalide",
        "Format audio non reconnu : WAV mono 16 kHz 16 bits attendu.");

    public static readonly Refus Vide = new("dictee.vide", "Enregistrement vide : rien à transcrire.");

    public static readonly Refus Silence = new(
        "dictee.silence",
        "Aucune parole reconnue : rapprochez-vous du micro, puis réessayez.");

    /// <summary>
    /// Refus de la dictée avant tout appel au modèle, <c>null</c> si elle peut être transcrite : dictée indisponible,
    /// trop longue, format autre que le WAV canonique, aucun échantillon.
    /// </summary>
    public static Refus? Verifier(bool disponible, ReadOnlySpan<byte> audio)
    {
        if (!disponible)
        {
            return Indisponible;
        }

        if (audio.Length > OctetsMax)
        {
            return TropLongue;
        }

        if (!EnTeteCanonique(audio))
        {
            return FormatInvalide;
        }

        return audio.Length == EnTete ? Vide : null;
    }

    /// <summary>Texte rendu par le modèle, sans espaces de bord ; blanc ou absent → <see cref="Silence"/>.</summary>
    public static Resultat<string> Conclure(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte))
        {
            return Silence;
        }

        return Resultat.Succes(texte.Trim());
    }

    /// <summary>En-tête canonique, tailles cohérentes avec la longueur reçue, nombre pair d'octets d'échantillons.</summary>
    internal static bool EnTeteCanonique(ReadOnlySpan<byte> audio) =>
        audio.Length >= EnTete
        && audio[..4].SequenceEqual("RIFF"u8)
        && BinaryPrimitives.ReadUInt32LittleEndian(audio[4..]) == (uint)(audio.Length - 8)
        && audio[8..12].SequenceEqual("WAVE"u8)
        && audio[12..16].SequenceEqual("fmt "u8)
        && BinaryPrimitives.ReadUInt32LittleEndian(audio[16..]) == 16
        && BinaryPrimitives.ReadUInt16LittleEndian(audio[20..]) == 1
        && BinaryPrimitives.ReadUInt16LittleEndian(audio[22..]) == 1
        && BinaryPrimitives.ReadUInt32LittleEndian(audio[24..]) == Frequence
        && BinaryPrimitives.ReadUInt32LittleEndian(audio[28..]) == OctetsParSeconde
        && BinaryPrimitives.ReadUInt16LittleEndian(audio[32..]) == 2
        && BinaryPrimitives.ReadUInt16LittleEndian(audio[34..]) == 16
        && audio[36..40].SequenceEqual("data"u8)
        && BinaryPrimitives.ReadUInt32LittleEndian(audio[40..]) == (uint)(audio.Length - EnTete)
        && audio.Length % 2 == 0;
}
