namespace ExpressionBesoin.Application.Dictee;

/// <summary>
/// Transcription d'une dictée (lot S1, DEB-Q131) : un WAV déjà vérifié par <see cref="DecisionsDictee.Verifier"/> → texte
/// BRUT du modèle de transcription (Whisper d'Albert API), relu par <see cref="DecisionsDictee.Conclure"/>. Une panne
/// technique (réseau, délai, statut d'échec, réponse illisible) lève une exception : 503 (DEB-Q22), jamais un refus.
/// </summary>
public interface ITranscripteur
{
    ValueTask<string> Transcrire(ReadOnlyMemory<byte> wav, CancellationToken annulation);
}
