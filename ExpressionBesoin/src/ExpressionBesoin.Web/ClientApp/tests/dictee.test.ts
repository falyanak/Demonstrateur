import { describe, expect, it } from "vitest";
import reference from "./contrats/fiche-reference.json?raw";
import referenceDictee from "./contrats/dictee-reference.json?raw";
import { creerClient, type Recuperer } from "../src/core/api.ts";
import { ContratInvalide, lireDictee, lireFiche, type ErreurVue, type FicheVue, type Statut } from "../src/core/contrats.ts";
import { CIBLE_MESSAGE, erreurMicro, insererTexte, SECONDES_MAX } from "../src/features/dialogue/dictee.ts";
import { DICTEE_INACTIVE, ETAT_INITIAL, insererDictee, reduire, type EtatDialogue } from "../src/features/dialogue/etat.ts";
import { boutonDictee } from "../src/features/dialogue/vue.ts";

// Dictée de la page Dialogue (lot S1, DEB-Q131 à DEB-Q133) : contrat, appel, insertion, réducteur, bouton, erreurs du micro.
// Fiche de référence : application = « SICCRF » (AccepteeIA), intitule manuel, situationActuelle vide.

const fiche = (statut: Statut = "Brouillon", modifiable = true): FicheVue => ({ ...lireFiche(JSON.parse(reference)), statut, modifiable });
const brouillon = (change: Partial<EtatDialogue> = {}): EtatDialogue => ({ ...ETAT_INITIAL, fiche: fiche(), ...change });
const enregistrement = (cible: string, position = 0) => ({ phase: "enregistrement" as const, cible, position });
const transcription = (cible: string, position = 0) => ({ phase: "transcription" as const, cible, position });
const erreur: ErreurVue = { code: "dictee.silence", message: "Aucune parole reconnue." };

describe("contrat et appel de la dictée", () => {
  it("référence partagée avec xUnit (PointsDicteeTests) lue telle quelle", () => {
    expect(lireDictee(JSON.parse(referenceDictee))).toEqual({ texte: "Le copieur du bureau 1A ne démarre plus." });
  });

  it.each([[null], [[]], [{}], [{ texte: 3 }]])("forme %j → ContratInvalide", (valeur) => {
    expect(() => lireDictee(valeur)).toThrow(ContratInvalide);
  });

  it("POST api/dictee : WAV en corps, type audio/wav, texte lu", async () => {
    const appels: { url: string; init: RequestInit }[] = [];
    const recuperer: Recuperer = (url, init) => {
      appels.push({ url, init });
      return Promise.resolve(new Response(referenceDictee, { status: 200 }));
    };
    const audio = new Uint8Array(new ArrayBuffer(4));
    const issue = await creerClient(recuperer, (chemin) => `/base/${chemin}`).dicter(audio);

    expect(issue).toEqual({ ok: true, valeur: { texte: "Le copieur du bureau 1A ne démarre plus." } });
    expect(appels).toHaveLength(1);
    expect(appels[0]?.url).toBe("/base/api/dictee");
    expect(appels[0]?.init.method).toBe("POST");
    expect(appels[0]?.init.headers).toEqual({ Accept: "application/json", "Content-Type": "audio/wav" });
    expect(appels[0]?.init.body).toBe(audio);
  });

  it("refus du serveur rendu tel quel", async () => {
    const recuperer: Recuperer = () => Promise.resolve(new Response(JSON.stringify(erreur), { status: 422 }));
    const issue = await creerClient(recuperer, (chemin) => chemin).dicter(new Uint8Array(new ArrayBuffer(0)));
    expect(issue).toEqual({ ok: false, statut: 422, erreur });
  });
});

describe("insererTexte", () => {
  it.each([
    ["zone vide", "", "Le copieur est en panne.", 0, "Le copieur est en panne."],
    ["à la fin, espace ajouté", "Bonjour", "le copieur", 7, "Bonjour le copieur"],
    ["au début, espace ajouté après", "est en panne.", "Le copieur", 0, "Le copieur est en panne."],
    ["au milieu, espaces des deux côtés", "Le est en panne.", "copieur", 2, "Le copieur est en panne."],
    ["blanc déjà présent : rien ajouté", "Le  est", "copieur", 3, "Le copieur est"],
    ["texte dicté sans espaces de bord", "A", "  b  ", 1, "A b"],
    ["dictée blanche : texte inchangé", "A", "   ", 1, "A"],
    ["position au-delà : fin", "A", "b", 99, "A b"],
    ["position négative : début", "A", "b", -5, "b A"],
    ["position non finie : fin", "A", "b", Number.NaN, "A b"],
    ["position décimale tronquée", "AB", "x", 1.7, "A x B"],
  ])("%s", (_cas, texte, ajout, position, attendu) => {
    expect(insererTexte(texte, ajout, position)).toBe(attendu);
  });
});

describe("insererDictee", () => {
  it("message : inséré au curseur", () => {
    expect(insererDictee(brouillon({ message: "Bonjour" }), CIBLE_MESSAGE, 7, "le copieur").message).toBe("Bonjour le copieur");
  });

  it("rubrique sans brouillon : partie de sa valeur retenue", () => {
    expect(insererDictee(brouillon(), "application", 7, "et SORA").brouillons).toEqual({ application: "SICCRF et SORA" });
  });

  it("rubrique avec brouillon : partie du brouillon, autres brouillons gardés", () => {
    const etat = brouillon({ brouillons: { application: "X", resume: "R" } });
    expect(insererDictee(etat, "application", 1, "Y").brouillons).toEqual({ application: "X Y", resume: "R" });
  });

  it("rubrique vide ou fiche absente : partie de rien", () => {
    expect(insererDictee(brouillon(), "situationActuelle", 0, "Panne").brouillons).toEqual({ situationActuelle: "Panne" });
    expect(insererDictee({ ...ETAT_INITIAL }, "application", 0, "A").brouillons).toEqual({ application: "A" });
  });
});

describe("reduire : dictée", () => {
  it("départ : enregistrement de la cible, erreur effacée ; refusé si une dictée est en cours", () => {
    const parti = reduire(brouillon({ erreur }), { type: "dicteeDebut", cible: CIBLE_MESSAGE, position: 3 });
    expect([parti.dictee, parti.erreur]).toEqual([enregistrement(CIBLE_MESSAGE, 3), null]);
    expect(reduire(parti, { type: "dicteeDebut", cible: "resume", position: 0 })).toBe(parti);
  });

  it("envoi : enregistrement → transcription ; sans effet hors enregistrement", () => {
    const etat = brouillon({ dictee: enregistrement("resume", 2) });
    expect(reduire(etat, { type: "dicteeEnvoi" }).dictee).toEqual(transcription("resume", 2));
    const inactif = brouillon();
    expect(reduire(inactif, { type: "dicteeEnvoi" })).toBe(inactif);
  });

  it("texte : inséré dans la cible, dictée finie ; ignoré hors transcription", () => {
    const etat = brouillon({ message: "Bonjour", dictee: transcription(CIBLE_MESSAGE, 7) });
    const fini = reduire(etat, { type: "dicteeTexte", texte: "le copieur" });
    expect([fini.message, fini.dictee]).toEqual(["Bonjour le copieur", DICTEE_INACTIVE]);
    const enCours = brouillon({ dictee: enregistrement(CIBLE_MESSAGE) });
    expect(reduire(enCours, { type: "dicteeTexte", texte: "x" })).toBe(enCours);
  });

  it("échec : dictée finie, erreur affichée, saisies gardées", () => {
    const etat = brouillon({ message: "m", dictee: transcription("resume") });
    const echoue = reduire(etat, { type: "dicteeEchec", erreur });
    expect([echoue.dictee, echoue.erreur, echoue.message]).toEqual([DICTEE_INACTIVE, erreur, "m"]);
  });

  it("état initial : aucune dictée ; une fiche reçue ne l'interrompt pas", () => {
    expect(ETAT_INITIAL.dictee).toEqual({ phase: "inactive" });
    const etat = brouillon({ dictee: enregistrement("resume") });
    expect(reduire(etat, { type: "fiche", fiche: fiche() }).dictee).toEqual(enregistrement("resume"));
  });
});

describe("boutonDictee", () => {
  it.each([
    ["message, brouillon libre", brouillon(), CIBLE_MESSAGE, { libelle: "🎙 Dicter", actif: true, enCours: false }],
    ["rubrique, brouillon libre", brouillon(), "resume", { libelle: "🎙 Dicter", actif: true, enCours: false }],
    ["message, requête en cours", brouillon({ attente: true }), CIBLE_MESSAGE, { libelle: "🎙 Dicter", actif: false, enCours: false }],
    ["rubrique, fiche soumise", brouillon({ fiche: fiche("Soumise", false) }), "resume", { libelle: "🎙 Dicter", actif: false, enCours: false }],
    ["message, fiche soumise", brouillon({ fiche: fiche("Soumise", false) }), CIBLE_MESSAGE, { libelle: "🎙 Dicter", actif: false, enCours: false }],
    ["autre zone en enregistrement", brouillon({ dictee: enregistrement("resume") }), CIBLE_MESSAGE, { libelle: "🎙 Dicter", actif: false, enCours: false }],
    ["sa zone en enregistrement", brouillon({ dictee: enregistrement("resume") }), "resume", { libelle: "⏹ Arrêter", actif: true, enCours: true }],
    ["sa zone en enregistrement, requête en cours", brouillon({ attente: true, dictee: enregistrement(CIBLE_MESSAGE) }), CIBLE_MESSAGE, { libelle: "⏹ Arrêter", actif: true, enCours: true }],
    ["sa zone en transcription", brouillon({ dictee: transcription("resume") }), "resume", { libelle: "Transcription…", actif: false, enCours: true }],
  ])("%s", (_cas, etat, cible, attendu) => {
    expect(boutonDictee(etat, cible)).toEqual(attendu);
  });
});

describe("erreurMicro", () => {
  it.each([
    [new DOMException("refus", "NotAllowedError"), "micro.refuse"],
    [{ name: "SecurityError" }, "micro.refuse"],
    [new DOMException("absent", "NotFoundError"), "micro.absent"],
    [{ name: "OverconstrainedError" }, "micro.absent"],
    [new DOMException("illisible", "EncodingError"), "dictee.illisible"],
    [new TypeError("x"), "micro.indisponible"],
    [{ name: 3 }, "micro.indisponible"],
    ["texte", "micro.indisponible"],
    [null, "micro.indisponible"],
    [undefined, "micro.indisponible"],
  ])("%s → %s", (exception, code) => {
    const vue = erreurMicro(exception);
    expect(vue.code).toBe(code);
    expect(vue.message.length).toBeGreaterThan(10);
  });

  it("durée d'une dictée sous le maximum du serveur (125 s)", () => {
    expect(SECONDES_MAX).toBe(120);
  });
});
