import { describe, expect, it } from "vitest";
import reference from "./contrats/fiche-reference.json?raw";
import { lireFiche, type FicheVue, type RubriqueVue, type Statut } from "../src/core/contrats.ts";
import { ETAT_INITIAL, type EtatDialogue } from "../src/features/dialogue/etat.ts";
import {
  actions,
  badgeRubrique,
  badgeStatut,
  blocs,
  DECISIONS,
  noteEcartees,
  peutEnregistrer,
  progression,
  titreBloc,
  titreFiche,
} from "../src/features/dialogue/vue.ts";

// Fiche de référence : 17 rubriques en 7 blocs, 2/10 obligatoires remplies, prochaine « difficultes ».
const base = lireFiche(JSON.parse(reference));
const fiche = (change: Partial<FicheVue> = {}): FicheVue => ({ ...base, ...change });
const etat = (change: Partial<EtatDialogue> = {}): EtatDialogue => ({ ...ETAT_INITIAL, fiche: fiche({ statut: "Brouillon", modifiable: true }), ...change });
const rubrique = (change: Partial<RubriqueVue>): RubriqueVue => ({ code: "c", libelle: "L", bloc: "Contexte", obligatoire: true, valeur: null, origine: "Vide", proposition: null, ...change });
const proposition = { valeur: "p", justification: "j" };

describe("titres et badges", () => {
  it.each([
    ["Identification", "Identification"],
    ["Synthese", "1. Synthèse"],
    ["Contexte", "2. Contexte"],
    ["Objectifs", "3. Objectifs"],
    ["Perimetre", "4. Périmètre"],
    ["Benefices", "5. Bénéfices"],
    ["Complements", "6. Compléments"],
    ["Inconnu", "Inconnu"],
    ["toString", "toString"],
  ])("bloc %s → %s", (bloc, titre) => {
    expect(titreBloc(bloc)).toBe(titre);
  });

  it.each([
    ["Brouillon", "Brouillon", "secondary"],
    ["Soumise", "Soumise", "info"],
    ["Validee", "Validée", "success"],
    ["ACompleter", "À compléter", "warning"],
    ["Refusee", "Refusée", "danger"],
  ] as const)("statut %s", (statut, texte, couleur) => {
    expect(badgeStatut(statut)).toEqual({ texte, couleur });
  });

  it.each([
    ["vide", rubrique({}), "Vide", "secondary"],
    ["vide avec proposition : Proposée", rubrique({ proposition }), "Proposée", "warning"],
    ["acceptée", rubrique({ valeur: "v", origine: "AccepteeIA" }), "IA acceptée", "info"],
    ["acceptée avec nouvelle proposition : garde son origine", rubrique({ valeur: "v", origine: "AccepteeIA", proposition }), "IA acceptée", "info"],
    ["manuelle", rubrique({ valeur: "v", origine: "Manuelle" }), "Manuelle", "primary"],
  ])("rubrique %s", (_cas, r, texte, couleur) => {
    expect(badgeRubrique(r)).toEqual({ texte, couleur });
  });

  it("titre : numéro, sinon « Nouvelle fiche »", () => {
    expect([titreFiche(fiche()), titreFiche(fiche({ numero: null }))]).toEqual(["EB-2026-0007", "Nouvelle fiche"]);
  });

  it("décisions proposées, dans l'ordre", () => {
    expect(DECISIONS.map((d) => d.valeur)).toEqual(["Valider", "DemanderComplements", "Refuser"]);
  });
});

describe("progression", () => {
  it.each([
    [2, 10, "2/10", 20],
    [7, 10, "7/10", 70],
    [10, 10, "10/10", 100],
    [1, 3, "1/3", 33],
    [2, 3, "2/3", 67],
    [0, 0, "0/0", 0],
  ])("%i sur %i", (remplies, obligatoires, texte, pourcent) => {
    expect(progression(fiche({ remplies, obligatoires }))).toEqual({ texte, pourcent });
  });
});

describe("blocs", () => {
  it("7 blocs dans l'ordre du modèle, 17 rubriques", () => {
    const resultat = blocs(base, {});
    expect(resultat.map((b) => [b.titre, b.rubriques.length])).toEqual([
      ["Identification", 2],
      ["1. Synthèse", 1],
      ["2. Contexte", 3],
      ["3. Objectifs", 3],
      ["4. Périmètre", 3],
      ["5. Bénéfices", 3],
      ["6. Compléments", 2],
    ]);
  });

  it("rubrique : saisie = brouillon sinon valeur, proposition et question en cours", () => {
    const [identification, , contexte] = blocs(base, { intitule: "Nouvel intitulé" });
    expect(identification?.rubriques).toEqual([
      { code: "application", libelle: "Application ou service concerné", obligatoire: true, badge: { texte: "IA acceptée", couleur: "info" }, saisie: "SICCRF", proposition: null, questionnee: false },
      { code: "intitule", libelle: "Intitulé court", obligatoire: true, badge: { texte: "Manuelle", couleur: "primary" }, saisie: "Nouvel intitulé", proposition: null, questionnee: false },
    ]);
    expect(contexte?.rubriques.map((r) => [r.code, r.saisie, r.badge.texte, r.proposition?.valeur ?? null, r.questionnee, r.obligatoire])).toEqual([
      ["situationActuelle", "", "Proposée", "Contrôles <manuels> & \"à distance\"", false, true],
      ["difficultes", "", "Vide", null, true, true],
      ["exempleRepresentatif", "", "Vide", null, false, false],
    ]);
  });

  it("deux suites du même bloc séparées : deux blocs (ordre du serveur respecté)", () => {
    const f = fiche({ rubriques: [rubrique({ code: "a" }), rubrique({ code: "b", bloc: "Objectifs" }), rubrique({ code: "c" })] });
    expect(blocs(f, {}).map((b) => b.titre)).toEqual(["2. Contexte", "3. Objectifs", "2. Contexte"]);
  });

  it("fiche sans rubrique : aucun bloc", () => {
    expect(blocs(fiche({ rubriques: [] }), {})).toEqual([]);
  });
});

describe("actions (DEB-Q50, DEB-Q53)", () => {
  const ouvertes = (dialoguer: boolean, soumettre: boolean, decider: boolean, reprendre: boolean, telecharger: boolean) => ({
    dialoguer,
    editer: dialoguer,
    soumettre,
    decider,
    reprendre,
    telecharger,
  });

  it.each([
    ["sans fiche", { fiche: null }, ouvertes(false, false, false, false, false)],
    ["brouillon incomplet", {}, ouvertes(true, false, false, false, false)],
    ["brouillon complet non confirmé", { fiche: fiche({ statut: "Brouillon", modifiable: true, complete: true }) }, ouvertes(true, false, false, false, false)],
    ["brouillon complet confirmé", { fiche: fiche({ statut: "Brouillon", modifiable: true, complete: true }), confirme: true }, ouvertes(true, true, false, false, false)],
    ["brouillon complet confirmé, en attente", { fiche: fiche({ statut: "Brouillon", modifiable: true, complete: true }), confirme: true, attente: true }, ouvertes(false, false, false, false, false)],
    ["non modifiable (le Domaine décide)", { fiche: fiche({ statut: "Brouillon", modifiable: false, complete: true }), confirme: true }, ouvertes(false, false, false, false, false)],
    ["soumise", { fiche: fiche({ statut: "Soumise", modifiable: false }) }, ouvertes(false, false, true, false, false)],
    ["soumise, en attente", { fiche: fiche({ statut: "Soumise", modifiable: false }), attente: true }, ouvertes(false, false, false, false, false)],
    ["à compléter", { fiche: fiche({ statut: "ACompleter", modifiable: false }) }, ouvertes(false, false, false, true, false)],
    ["à compléter, en attente", { fiche: fiche({ statut: "ACompleter", modifiable: false }), attente: true }, ouvertes(false, false, false, false, false)],
    ["validée", { fiche: fiche({ statut: "Validee", modifiable: false }) }, ouvertes(false, false, false, false, true)],
    ["validée, en attente : le lien reste", { fiche: fiche({ statut: "Validee", modifiable: false }), attente: true }, ouvertes(false, false, false, false, true)],
    ["refusée", { fiche: fiche({ statut: "Refusee" as Statut, modifiable: false }) }, ouvertes(false, false, false, false, false)],
  ])("%s", (_cas, change, attendu) => {
    expect(actions(etat(change))).toEqual(attendu);
  });
});

describe("peutEnregistrer (DEB-Q51)", () => {
  it.each([
    ["sans brouillon", {}, "application", false],
    ["brouillon égal à la valeur", { brouillons: { application: "SICCRF" } }, "application", false],
    ["brouillon différent", { brouillons: { application: "SICCRF 2" } }, "application", true],
    ["brouillon vide sur une rubrique remplie (vider)", { brouillons: { application: "" } }, "application", true],
    ["brouillon vide sur une rubrique vide", { brouillons: { resume: "" } }, "resume", false],
    ["texte sur une rubrique vide", { brouillons: { resume: "R" } }, "resume", true],
    ["rubrique inconnue", { brouillons: { inconnue: "X" } }, "inconnue", false],
    ["en attente", { brouillons: { resume: "R" }, attente: true }, "resume", false],
    ["fiche non modifiable", { brouillons: { resume: "R" }, fiche: fiche({ modifiable: false }) }, "resume", false],
    ["sans fiche", { brouillons: { resume: "R" }, fiche: null }, "resume", false],
  ])("%s", (_cas, change, code, attendu) => {
    expect(peutEnregistrer(etat(change), code)).toBe(attendu);
  });
});

describe("noteEcartees", () => {
  it.each([
    [0, null],
    [-1, null],
    [1, "1 proposition de l'IA écartée (forme non reconnue)."],
    [3, "3 propositions de l'IA écartées (forme non reconnue)."],
  ])("%i", (n, attendu) => {
    expect(noteEcartees(n)).toBe(attendu);
  });
});
