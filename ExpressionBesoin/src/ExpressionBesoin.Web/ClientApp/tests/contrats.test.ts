import { describe, expect, it } from "vitest";
import reference from "./contrats/fiche-reference.json?raw";
import { ContratInvalide, lireAgents, lireErreur, lireFiche } from "../src/core/contrats.ts";

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
