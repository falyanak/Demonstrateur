import { describe, expect, it } from "vitest";
import { EN_TETE_WAV, encoderWav, FREQUENCE_DICTEE, mixerMono, versEntier16 } from "../src/core/wav.ts";

// Encodage de la dictée (lot S1). Octets attendus produits par une réplique Python indépendante (struct.pack, arrondi au
// plus proche comme Math.round) ; le même en-tête canonique est vérifié côté serveur (DecisionsDicteeTests).

const hex = (octets: Uint8Array) => Array.from(octets, (o) => o.toString(16).padStart(2, "0")).join("").toUpperCase();

describe("versEntier16", () => {
  it.each([
    [0, 0],
    [1, 32767],
    [-1, -32768],
    [0.5, 16384],
    [-0.5, -16384],
    [2, 32767],
    [-2, -32768],
    [0.25, 8192],
    [Number.NaN, 0],
  ])("%s → %s", (echantillon, attendu) => {
    expect(versEntier16(echantillon)).toBe(attendu);
  });
});

describe("encoderWav", () => {
  it("en-tête canonique et échantillons bornés, octets de la réplique Python", () => {
    const wav = encoderWav(Float32Array.from([0, 1, -1, 0.5, -0.5, 2, -2, 0.25, Number.NaN]));
    expect(hex(wav)).toBe(
      "524946463600000057415645666D74201000000001000100803E0000007D00000200100064617461120000000000FF7F0080004000C0FF7F008000200000",
    );
  });

  it("deux échantillons : même octets que la référence du serveur (DecisionsDicteeTests.ReferenceHex)", () => {
    expect(hex(encoderWav(Float32Array.from([0, 1]), 16_000))).toBe(
      "524946462800000057415645666D74201000000001000100803E0000007D00000200100064617461040000000000FF7F",
    );
  });

  it("aucun échantillon : en-tête seul (refusé « dictee.vide » par le serveur)", () => {
    const wav = encoderWav(new Float32Array(0));
    expect([wav.length, EN_TETE_WAV, FREQUENCE_DICTEE]).toEqual([44, 44, 16_000]);
    expect(hex(wav.slice(40))).toBe("00000000");
  });

  it("fréquence écrite telle quelle (le serveur refuse tout autre que 16 kHz)", () => {
    const vue = new DataView(encoderWav(new Float32Array(1), 44_100).buffer);
    expect([vue.getUint32(24, true), vue.getUint32(28, true)]).toEqual([44_100, 88_200]);
  });
});

describe("mixerMono", () => {
  it("aucun canal → vide", () => {
    expect(mixerMono([])).toEqual(new Float32Array(0));
  });

  it("un canal → copie (le canal d'origine n'est pas partagé)", () => {
    const canal = Float32Array.from([0.25, -0.5]);
    const mono = mixerMono([canal]);
    expect(mono).toEqual(canal);
    expect(mono).not.toBe(canal);
  });

  it("plusieurs canaux → moyenne échantillon par échantillon, longueur du premier", () => {
    const mono = mixerMono([Float32Array.from([1, 0.5]), Float32Array.from([0, -0.5]), Float32Array.from([0.5, 0.25, 9])]);
    expect(mono.length).toBe(2);
    expect(mono[0]).toBeCloseTo(0.5, 6);
    expect(mono[1]).toBeCloseTo(0.0833333, 6);
  });

  it("canal plus court que le premier : échantillons absents comptés nuls", () => {
    const mono = mixerMono([Float32Array.from([1, 1]), Float32Array.from([1])]);
    expect(Array.from(mono)).toEqual([1, 0.5]);
  });
});
