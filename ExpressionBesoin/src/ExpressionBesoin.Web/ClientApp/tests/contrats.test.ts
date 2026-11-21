import { describe, expect, it } from "vitest";
import reference from "./contrats/fiche-reference.json?raw";
import referenceConsultation from "./contrats/consultation-reference.json?raw";
import referenceTour from "./contrats/tour-reference.json?raw";
import referenceListe from "./contrats/liste-reference.json?raw";
import { ContratInvalide, lireAgents, lireConsultation, lireErreur, lireFiche, lireResumes, lireTour } from "../src/core/contrats.ts";

// Mêmes octets que FicheVueTests (xUnit) : le contrat est vérifié des deux côtés (règles §7).
const brut = (): Record<string, unknown> => JSON.parse(reference) as Record<string, unknown>;

describe("lireFiche sur la fiche de référence", () => {
  const fiche = lireFiche(JSON.parse(reference));

  it("en-tête et statut", () => {
    expect(fiche.id).toBe("6f1c2b3a-0000-4000-8000-0000000000c1");
    expect(fiche.numero).toBe("EB-2026-0007");
    expect(fiche.statut).toBe("ACompleter");
    expect(fiche.enTete).toEqual({
      demandeur: "Camille Martin",
      bureau: "Bureau 1A – Commerce électronique",
      valideur: "Dominique Bernard",
      creeeLe: "2026-11-20T09:00:00+00:00",
    });
  });

  it("17 rubriques dans l'ordre du modèle, 10 obligatoires", () => {
    expect(fiche.rubriques).toHaveLength(17);
    expect(fiche.rubriques[0]?.code).toBe("application");
    expect(fiche.rubriques[16]?.code).toBe("pointsAClarifier");
    expect(fiche.rubriques.filter((r) => r.obligatoire)).toHaveLength(10);
    expect(fiche.obligatoires).toBe(10);
  });

  it("origines, valeurs et proposition en attente, caractères spéciaux intacts", () => {
    expect(fiche.rubriques[0]).toMatchObject({ valeur: "SICCRF", origine: "AccepteeIA", proposition: null });
    expect(fiche.rubriques[1]).toMatchObject({ valeur: "Contrôle des plates-formes e-commerce", origine: "Manuelle" });
    expect(fiche.rubriques[3]).toEqual({
      code: "situationActuelle",
      libelle: "Situation actuelle",
      bloc: "Contexte",
      obligatoire: true,
      valeur: null,
      origine: "Vide",
      proposition: { valeur: 'Contrôles <manuels> & "à distance"', justification: "Reprend la réponse de l'agent." },
    });
  });

  it("indicateurs calculés côté serveur", () => {
    expect([fiche.remplies, fiche.complete, fiche.modifiable, fiche.prochaineRubrique]).toEqual([2, false, false, "difficultes"]);
  });

  it("confirmation et décisions", () => {
    expect(fiche.confirmeeLe).toBe("2026-11-20T10:00:00+00:00");
    expect(fiche.decisions).toEqual([
      { type: "DemanderComplements", auteur: "Dominique Bernard", le: "2026-11-20T11:30:00+00:00", commentaire: "Préciser les critères." },
    ]);
  });

  it("le fichier est compact, sans BOM ni fin de ligne (octets émis par le serveur)", () => {
    expect(reference.startsWith("{")).toBe(true);
    expect(reference.endsWith("}")).toBe(true);
    expect(reference).not.toMatch(/\n/);
  });
});

describe("lireFiche refuse une forme inattendue", () => {
  const cas: Array<[string, (f: Record<string, unknown>) => unknown, string]> = [
    ["racine non objet", () => [], "fiche devait être un objet"],
    ["racine nulle", () => null, "fiche devait être un objet"],
    ["id absent", (f) => ({ ...f, id: undefined }), "fiche.id devait être un texte"],
    ["statut inconnu", (f) => ({ ...f, statut: "Archivee" }), "fiche.statut devait être l'une de"],
    ["enTete absent", (f) => ({ ...f, enTete: null }), "fiche.enTete devait être un objet"],
    ["rubriques non tableau", (f) => ({ ...f, rubriques: {} }), "fiche.rubriques devait être un tableau"],
    ["entier décimal", (f) => ({ ...f, remplies: 2.5 }), "fiche.remplies devait être un entier"],
    ["booléen en texte", (f) => ({ ...f, complete: "false" }), "fiche.complete devait être un booléen"],
    [
      "origine inconnue",
      (f) => ({ ...f, rubriques: [{ ...(f["rubriques"] as object[])[0], origine: "ProposeeIA" }] }),
      "fiche.rubriques[0].origine devait être l'une de",
    ],
    [
      "proposition sans justification",
      (f) => ({ ...f, rubriques: [{ ...(f["rubriques"] as object[])[0], proposition: { valeur: "x" } }] }),
      "fiche.rubriques[0].proposition.justification devait être un texte",
    ],
    [
      "décision inconnue",
      (f) => ({ ...f, decisions: [{ type: "Ajourner", auteur: "a", le: "l", commentaire: null }] }),
      "fiche.decisions[0].type devait être l'une de",
    ],
  ];

  it.each(cas)("%s", (_nom, alterer, message) => {
    const lire = () => lireFiche(alterer(brut()));
    expect(lire).toThrow(ContratInvalide);
    expect(lire).toThrow(message);
  });
});

describe("lireAgents et lireErreur", () => {
  it("agents", () => {
    expect(lireAgents([{ identifiant: "a", nom: "N", bureau: "B", valideur: "V" }])).toEqual([
      { identifiant: "a", nom: "N", bureau: "B", valideur: "V" },
    ]);
    expect(() => lireAgents({})).toThrow("agents devait être un tableau");
    expect(() => lireAgents([{ identifiant: "a", nom: "N", bureau: "B" }])).toThrow("agents[0].valideur devait être un texte");
  });

  it.each([
    [{ code: "fiche.introuvable", message: "m" }, { code: "fiche.introuvable", message: "m" }],
    [{ code: "c" }, null],
    [{ code: 1, message: "m" }, null],
    [null, null],
    ["texte", null],
  ])("erreur %j → %j", (corps, attendu) => {
    expect(lireErreur(corps)).toEqual(attendu);
  });
});

describe("lireConsultation et lireTour sur les références partagées avec xUnit (DEB-Q52)", () => {
  it("consultation : fiche de référence et historique, retour à la ligne intact", () => {
    const consultation = lireConsultation(JSON.parse(referenceConsultation));
    expect(consultation.fiche).toEqual(lireFiche(JSON.parse(reference)));
    expect(consultation.historique).toEqual([
      { auteur: "Agent", texte: "Contrôle des plates-formes e-commerce\net prélèvements à distance", le: "2026-11-20T09:05:00+00:00" },
      { auteur: "IA", texte: "Quelle application est concernée ?", le: "2026-11-20T09:05:01+00:00" },
    ]);
  });

  it("tour : message, hors périmètre, écarts et fiche", () => {
    const tour = lireTour(JSON.parse(referenceTour));
    expect(tour.message).toBe("Quelle application est concernée ? Précisez « le service » s'il y a lieu.");
    expect(tour.horsPerimetre).toBe(true);
    expect(tour.ecartees).toEqual([
      { position: 1, code: "inconnue", motif: "RubriqueInconnue" },
      { position: 2, code: "intitule", motif: "Doublon" },
    ]);
    expect(tour.fiche.numero).toBe("EB-2026-0007");
  });

  it.each([
    ["consultation-reference.json", referenceConsultation],
    ["tour-reference.json", referenceTour],
  ])("%s : compact, sans BOM ni fin de ligne, contient la fiche de référence à l'octet", (_nom, texte) => {
    expect(texte.charCodeAt(0)).not.toBe(0xfeff);
    expect(texte.endsWith("}")).toBe(true);
    expect(texte).toContain(reference);
  });

  it.each([
    [() => lireConsultation([]), "consultation devait être un objet"],
    [() => lireConsultation({ fiche: null, historique: [] }), "consultation.fiche devait être un objet"],
    [() => lireConsultation({ ...JSON.parse(referenceConsultation), historique: {} }), "consultation.historique devait être un tableau"],
    [() => lireConsultation({ ...JSON.parse(referenceConsultation), historique: [{ auteur: "Chef", texte: "t", le: "l" }] }), "consultation.historique[0].auteur devait être l'une de Agent, IA"],
    [() => lireConsultation({ ...JSON.parse(referenceConsultation), historique: [{ auteur: "IA", texte: 1, le: "l" }] }), "consultation.historique[0].texte devait être un texte"],
    [() => lireTour(null), "tour devait être un objet"],
    [() => lireTour({ ...JSON.parse(referenceTour), message: null }), "tour.message devait être un texte"],
    [() => lireTour({ ...JSON.parse(referenceTour), horsPerimetre: "non" }), "tour.horsPerimetre devait être un booléen"],
    [() => lireTour({ ...JSON.parse(referenceTour), ecartees: [{ position: 1.5, code: "c", motif: "m" }] }), "tour.ecartees[0].position devait être un entier"],
    [() => lireTour({ ...JSON.parse(referenceTour), ecartees: [{ position: 1, code: "c" }] }), "tour.ecartees[0].motif devait être un texte"],
    [() => lireTour({ ...JSON.parse(referenceTour), fiche: { id: 1 } }), "tour.fiche.enTete devait être un objet"],
  ])("refus %#", (lire, message) => {
    expect(lire).toThrow(ContratInvalide);
    expect(lire).toThrow(message);
  });
});

describe("lireResumes sur la référence de la liste partagée avec xUnit (F3)", () => {
  const brutListe = (): Record<string, unknown>[] => JSON.parse(referenceListe) as Record<string, unknown>[];

  it("deux lignes dans l'ordre du serveur, nuls et caractères spéciaux intacts", () => {
    expect(lireResumes(JSON.parse(referenceListe))).toEqual([
      {
        id: "6f1c2b3a-0000-4000-8000-0000000000c1",
        numero: "EB-2026-0007",
        intitule: "Contrôle des plates-formes e-commerce",
        demandeur: "Camille Martin",
        statut: "ACompleter",
        creeeLe: "2026-11-20T09:00:00+00:00",
        confirmeeLe: "2026-11-20T10:00:00+00:00",
      },
      {
        id: "6f1c2b3a-0000-4000-8000-0000000000c2",
        numero: null,
        intitule: null,
        demandeur: "Claude Petit-Lefèvre",
        statut: "Brouillon",
        creeeLe: "2026-11-20T23:30:00+00:00",
        confirmeeLe: null,
      },
    ]);
  });

  it("le fichier est compact, sans BOM ni fin de ligne", () => {
    expect(referenceListe.charCodeAt(0)).toBe(0x5b);
    expect(referenceListe.endsWith("]")).toBe(true);
    expect(referenceListe).not.toMatch(/\n/);
  });

  it("liste vide", () => {
    expect(lireResumes([])).toEqual([]);
  });

  const cas: Array<[string, () => unknown, string]> = [
    ["racine non tableau", () => ({}), "resumes devait être un tableau"],
    ["ligne non objet", () => [null], "resumes[0] devait être un objet"],
    ["id absent", () => [{ ...brutListe()[0], id: undefined }], "resumes[0].id devait être un texte"],
    ["numéro non texte", () => [{ ...brutListe()[0], numero: 7 }], "resumes[0].numero devait être un texte"],
    ["intitulé absent", () => [{ ...brutListe()[0], intitule: undefined }], "resumes[0].intitule devait être un texte"],
    ["demandeur nul", () => [{ ...brutListe()[0], demandeur: null }], "resumes[0].demandeur devait être un texte"],
    ["statut inconnu", () => [brutListe()[0], { ...brutListe()[1], statut: "Archivee" }], "resumes[1].statut devait être l'une de"],
    ["création absente", () => [{ ...brutListe()[0], creeeLe: undefined }], "resumes[0].creeeLe devait être un texte"],
    ["confirmation non texte", () => [{ ...brutListe()[0], confirmeeLe: false }], "resumes[0].confirmeeLe devait être un texte"],
  ];

  it.each(cas)("refus : %s", (_nom, corps, message) => {
    const lire = () => lireResumes(corps());
    expect(lire).toThrow(ContratInvalide);
    expect(lire).toThrow(message);
  });
});
