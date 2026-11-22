using ExpressionBesoin.Domain;

namespace ExpressionBesoin.Application.Dictee;

/// <summary>
/// Cas d'usage de la dictée (lot S1, route <c>POST api/dictee</c>) : coquille vérifier (pur) → transcrire (port) →
/// conclure (pur), sans décision ni écriture : rien n'est stocké, ni l'audio ni le texte (DEB-Q131).
/// <paramref name="transcripteur"/> est <c>null</c> quand le profil de l'IA n'a pas de modèle de transcription
/// (DEB-Q132) : la page n'affiche pas le bouton, la route refuse <c>dictee.indisponible</c>.
/// </summary>
public sealed class ServiceDictee(ITranscripteur? transcripteur)
{
    public bool Disponible => transcripteur is not null;

    public async ValueTask<Resultat<string>> Transcrire(ReadOnlyMemory<byte> audio, CancellationToken annulation)
    {
        if (DecisionsDictee.Verifier(transcripteur is not null, audio.Span) is { } refus)
        {
            return refus;
        }

        return DecisionsDictee.Conclure(await transcripteur!.Transcrire(audio, annulation));
    }
}
