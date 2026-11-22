// Encodage de la dictée (lot S1, DEB-Q131) : fonctions PURES, sans Web Audio. Le serveur n'accepte que le WAV CANONIQUE
// (en-tête de 44 octets, PCM 16 bits, mono, 16 kHz), transmis tel quel à Whisper : ni ffmpeg ni paquet ajouté.

/** Fréquence de la dictée : celle attendue par le serveur (DecisionsDictee.Frequence). */
export const FREQUENCE_DICTEE = 16_000;

/** Taille de l'en-tête canonique. */
export const EN_TETE_WAV = 44;

/** Moyenne des canaux, échantillon par échantillon (longueur du premier canal) ; aucun canal → vide. Pure. */
export function mixerMono(canaux: readonly Float32Array[]): Float32Array {
  const premier = canaux[0];
  if (premier === undefined) return new Float32Array(0);
  if (canaux.length === 1) return Float32Array.from(premier);
  const mono = new Float32Array(premier.length);
  for (let i = 0; i < mono.length; i++) {
    let somme = 0;
    for (const canal of canaux) somme += canal[i] ?? 0;
    mono[i] = somme / canaux.length;
  }
  return mono;
}

/** Échantillon flottant → entier 16 bits : borné à [−1, 1], arrondi au plus proche ; NaN → 0. Pure. */
export function versEntier16(echantillon: number): number {
  if (Number.isNaN(echantillon)) return 0;
  const borne = Math.max(-1, Math.min(1, echantillon));
  return borne < 0 ? Math.round(borne * 0x8000) : Math.round(borne * 0x7fff);
}

function ecrireTexte(vue: DataView, position: number, texte: string): void {
  for (let i = 0; i < texte.length; i++) vue.setUint8(position + i, texte.charCodeAt(i));
}

/** WAV canonique mono 16 bits des échantillons, à la fréquence donnée. Pure. */
export function encoderWav(echantillons: Float32Array, frequence: number = FREQUENCE_DICTEE): Uint8Array<ArrayBuffer> {
  const donnees = echantillons.length * 2;
  const octets = new Uint8Array(EN_TETE_WAV + donnees);
  const vue = new DataView(octets.buffer);
  ecrireTexte(vue, 0, "RIFF");
  vue.setUint32(4, 36 + donnees, true);
  ecrireTexte(vue, 8, "WAVE");
  ecrireTexte(vue, 12, "fmt ");
  vue.setUint32(16, 16, true);
  vue.setUint16(20, 1, true);
  vue.setUint16(22, 1, true);
  vue.setUint32(24, frequence, true);
  vue.setUint32(28, frequence * 2, true);
  vue.setUint16(32, 2, true);
  vue.setUint16(34, 16, true);
  ecrireTexte(vue, 36, "data");
  vue.setUint32(40, donnees, true);
  echantillons.forEach((echantillon, i) => vue.setInt16(EN_TETE_WAV + i * 2, versEntier16(echantillon), true));
  return octets;
}
