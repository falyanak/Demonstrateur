import { encoderWav, FREQUENCE_DICTEE, mixerMono } from "./wav.ts";

// Micro de la dictée (lot S1) : COQUILLE sans décision, éprouvée au navigateur (hors couverture, comme les Page*.ts).
// Le navigateur enregistre dans son format (webm/opus, ogg…), puis le décode et le rééchantillonne à 16 kHz par Web
// Audio ; le mélange mono et l'encodage WAV sont les fonctions pures de wav.ts. Le micro exige une page sûre : localhost
// sur le poste, HTTPS sur un serveur (évolution V2). Rien n'est gardé : ni fichier, ni stockage du navigateur.

export interface Enregistrement {
  /** Arrête l'enregistrement, libère le micro, rend le WAV canonique (mono, 16 kHz, 16 bits). */
  arreter(): Promise<Uint8Array<ArrayBuffer>>;
}

/** Vrai si le navigateur offre le micro, l'enregistrement et le décodage. */
export function microDisponible(): boolean {
  return (
    typeof navigator !== "undefined" &&
    typeof navigator.mediaDevices?.getUserMedia === "function" &&
    typeof MediaRecorder !== "undefined" &&
    typeof OfflineAudioContext !== "undefined"
  );
}

function liberer(flux: MediaStream): void {
  for (const piste of flux.getTracks()) piste.stop();
}

/**
 * Démarre un enregistrement (demande d'accès au micro au premier usage) ; `surDelai` est appelé après `secondesMax`
 * secondes : la page arrête alors la dictée comme sur un clic. Lève si le micro est refusé ou absent.
 */
export async function enregistrer(secondesMax: number, surDelai: () => void): Promise<Enregistrement> {
  const flux = await navigator.mediaDevices.getUserMedia({ audio: { channelCount: 1, echoCancellation: true, noiseSuppression: true } });
  let enregistreur: MediaRecorder;
  try {
    enregistreur = new MediaRecorder(flux);
  } catch (exception) {
    liberer(flux);
    throw exception;
  }
  const morceaux: Blob[] = [];
  enregistreur.addEventListener("dataavailable", (evenement) => {
    if (evenement.data.size > 0) morceaux.push(evenement.data);
  });
  const fin = new Promise<void>((resoudre) => enregistreur.addEventListener("stop", () => resoudre(), { once: true }));
  enregistreur.start();
  const minuterie = setTimeout(surDelai, secondesMax * 1000);
  return {
    async arreter() {
      clearTimeout(minuterie);
      if (enregistreur.state !== "inactive") enregistreur.stop();
      await fin;
      liberer(flux);
      const brut = await new Blob(morceaux, { type: enregistreur.mimeType }).arrayBuffer();
      // decodeAudioData rééchantillonne à la fréquence du contexte : 16 kHz.
      const decode = await new OfflineAudioContext(1, 1, FREQUENCE_DICTEE).decodeAudioData(brut);
      const canaux = Array.from({ length: decode.numberOfChannels }, (_, i) => decode.getChannelData(i));
      return encoderWav(mixerMono(canaux), decode.sampleRate);
    },
  };
}
