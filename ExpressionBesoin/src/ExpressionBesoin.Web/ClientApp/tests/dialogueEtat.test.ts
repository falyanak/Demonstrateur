import { describe, expect, it } from "vitest";
import reference from "./contrats/fiche-reference.json?raw";
import referenceConsultation from "./contrats/consultation-reference.json?raw";
import referenceTour from "./contrats/tour-reference.json?raw";
import { lireConsultation, lireFiche, lireTour, type FicheVue, type Statut } from "../src/core/contrats.ts";
import { commencer, ETAT_INITIAL, nettoyerBrouillons, reduire, type EtatDialogue } from "../src/features/dialogue/etat.ts";

// Fiche de référence : statut ACompleter ; application = « SICCRF » (AccepteeIA), intitule = « Contrôle des
// plates-formes e-commerce » (Manuelle), situationActuelle vide avec une proposition, les autres vides.
const fiche = (statut: Statut = "Brouillon"): FicheVue => ({ ...lireFiche(JSON.parse(reference)), statut });
const erreur = { code: "message.vide", message: "Le message de l'agent est vide." };

describe("commencer : une requête à la fois", () => {
  it("libre → attente, erreur effacée", () => {
    expect(commencer({ ...ETAT_INITIAL, erreur })).toEqual({ ...ETAT_INITIAL, attente: true, erreur: null });
  });

  it("déjà en attente → null", () => {
    expect(commencer({ ...ETAT_INITIAL, attente: true })).toBeNull();
  });
});

describe("nettoyerBrouillons", () => {
  const f = fiche();
  it.each([
    ["différent de la valeur : gardé", { application: "SICCRF 2" }, undefined, { application: "SICCRF 2" }],
    ["égal à la valeur retenue : retiré", { application: "SICCRF" }, undefined, {}],
    ["vide sur une rubrique vide : retiré", { resume: "" }, undefined, {}],
    ["texte sur une rubrique vide : gardé", { resume: "R" }, undefined, { resume: "R" }],
    ["rubrique enregistrée : retiré même si différent", { application: "X", resume: "R" }, "application", { resume: "R" }],
    ["rubrique inconnue : retiré", { inconnue: "X" }, undefined, {}],
  ])("%s", (_cas, brouillons, enregistree, attendu) => {
    expect(nettoyerBrouillons(brouillons, f, enregistree)).toEqual(attendu);
  });
});

describe("reduire", () => {
  it("consultation : fiche et conversation chargées, attente finie", () => {
    const consultation = lireConsultation(JSON.parse(referenceConsultation));
    const etat = reduire({ ...ETAT_INITIAL, attente: true }, { type: "consultation", consultation });
    expect(etat.fiche).toBe(consultation.fiche);
    expect(etat.conversation).toEqual([
      { auteur: "Agent", texte: "Contrôle des plates-formes e-commerce\net prélèvements à distance", horsPerimetre: false, ecartees: 0 },
      { auteur: "IA", texte: "Quelle application est concernée ?", horsPerimetre: false, ecartees: 0 },
    ]);
    expect(etat.attente).toBe(false);
  });

  it("tour : message de l'agent puis réponse de l'IA ajoutés, saisie vidée, fiche remplacée", () => {
    const tour = lireTour(JSON.parse(referenceTour));
    const avant: EtatDialogue = {
      ...ETAT_INITIAL,
      fiche: fiche("ACompleter"),
      conversation: [{ auteur: "IA", texte: "Bonjour", horsPerimetre: false, ecartees: 0 }],
      message: "Pour SICCRF",
      attente: true,
    };
    const etat = reduire(avant, { type: "tour", envoye: "Pour SICCRF", tour });
    expect(etat.conversation).toEqual([
      { auteur: "IA", texte: "Bonjour", horsPerimetre: false, ecartees: 0 },
      { auteur: "Agent", texte: "Pour SICCRF", horsPerimetre: false, ecartees: 0 },
      { auteur: "IA", texte: "Quelle application est concernée ? Précisez « le service » s'il y a lieu.", horsPerimetre: true, ecartees: 2 },
    ]);
    expect([etat.message, etat.attente, etat.fiche]).toEqual(["", false, tour.fiche]);
  });

  it("fiche : brouillon enregistré retiré, autres gardés ; confirmation gardée si le statut ne change pas", () => {
    const avant: EtatDialogue = { ...ETAT_INITIAL, fiche: fiche(), brouillons: { application: "X", resume: "R" }, confirme: true, commentaire: "c", attente: true, erreur };
    const etat = reduire(avant, { type: "fiche", fiche: fiche(), enregistree: "application" });
    expect(etat).toEqual({ ...avant, brouillons: { resume: "R" }, attente: false, erreur: null });
  });

  it("fiche : statut changé → confirmation et commentaire remis à zéro (reprise, décision)", () => {
    const avant: EtatDialogue = { ...ETAT_INITIAL, fiche: fiche("Soumise"), confirme: true, commentaire: "Préciser", decision: "Refuser", attente: true };
    const etat = reduire(avant, { type: "fiche", fiche: fiche("ACompleter") });
    expect([etat.confirme, etat.commentaire, etat.decision, etat.fiche?.statut]).toEqual([false, "", "Refuser", "ACompleter"]);
  });

  it("refus : attente finie, erreur gardée, rien d'autre ne change", () => {
    const avant: EtatDialogue = { ...ETAT_INITIAL, fiche: fiche(), message: "m", attente: true };
    expect(reduire(avant, { type: "refus", erreur })).toEqual({ ...avant, attente: false, erreur });
  });

  it.each([
    [{ type: "message", texte: "Bonjour" } as const, { message: "Bonjour" }],
    [{ type: "brouillon", code: "resume", texte: "R" } as const, { brouillons: { application: "A", resume: "R" } }],
    [{ type: "confirmation", coche: true } as const, { confirme: true }],
    [{ type: "decision", decision: "DemanderComplements" } as const, { decision: "DemanderComplements" }],
    [{ type: "commentaire", texte: "Préciser" } as const, { commentaire: "Préciser" }],
  ])("saisie %# : seul le champ visé change", (evenement, change) => {
    const avant: EtatDialogue = { ...ETAT_INITIAL, fiche: fiche(), brouillons: { application: "A" } };
    expect(reduire(avant, evenement)).toEqual({ ...avant, ...change });
  });
});
