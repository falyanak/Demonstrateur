import { describe, expect, it } from "vitest";
import referenceListe from "./contrats/liste-reference.json?raw";
import { lireResumes, type ResumeVue, type Statut } from "../src/core/contrats.ts";
import { AUCUNE_FICHE, dateCourte, FUSEAU, lignes } from "../src/features/liste/vue.ts";
import * as dates from "../src/core/dates.ts";

// Attendus des dates recalculés par une réplique Python (zoneinfo, Europe/Paris), jamais recopiés de la sortie du code.
// dateCourte et FUSEAU sont testés dans dates.test.ts (core/dates.ts, L2) ; ici, seulement leur réexport.

describe("réexport de core/dates.ts", () => {
  it("dateCourte et FUSEAU sont ceux de core/dates.ts (un sujet, un endroit)", () => {
    expect(dateCourte).toBe(dates.dateCourte);
    expect(FUSEAU).toBe(dates.FUSEAU);
  });
});

describe("lignes", () => {
  const resume = (statut: Statut, autres: Partial<ResumeVue> = {}): ResumeVue => ({
    id: "f1",
    numero: "EB-2026-0001",
    intitule: "Intitulé",
    demandeur: "Camille Martin",
    statut,
    creeeLe: "2026-11-20T09:00:00+00:00",
    confirmeeLe: null,
    ...autres,
  });

  it("référence partagée avec xUnit : numéro et intitulé absents remplacés, date à Paris, ordre gardé", () => {
    expect(lignes(lireResumes(JSON.parse(referenceListe)), FUSEAU)).toEqual([
      {
        id: "6f1c2b3a-0000-4000-8000-0000000000c1",
        numero: "EB-2026-0007",
        intitule: "Contrôle des plates-formes e-commerce",
        demandeur: "Camille Martin",
        creeeLe: "20/11/2026",
        badge: { texte: "À compléter", couleur: "warning" },
        ouvrir: "Home/Dialogue/6f1c2b3a-0000-4000-8000-0000000000c1",
        telecharger: false,
      },
      {
        id: "6f1c2b3a-0000-4000-8000-0000000000c2",
        numero: "—",
        intitule: "(sans intitulé)",
        demandeur: "Claude Petit-Lefèvre",
        creeeLe: "21/11/2026",
        badge: { texte: "Brouillon", couleur: "secondary" },
        ouvrir: "Home/Dialogue/6f1c2b3a-0000-4000-8000-0000000000c2",
        telecharger: false,
      },
    ]);
  });

  it.each([
    ["Brouillon", "Brouillon", "secondary", false],
    ["Soumise", "Soumise", "info", false],
    ["ACompleter", "À compléter", "warning", false],
    ["Validee", "Validée", "success", true],
    ["Refusee", "Refusée", "danger", false],
  ] as const)("statut %s : badge « %s » (%s), Télécharger %s", (statut, texte, couleur, telecharger) => {
    const [ligne] = lignes([resume(statut)], FUSEAU);
    expect(ligne?.badge).toEqual({ texte, couleur });
    expect(ligne?.telecharger).toBe(telecharger);
  });

  it("« Ouvrir » pour tous les statuts : chemin de la page Dialogue, identifiant encodé (DEB-Q60)", () => {
    expect(lignes([resume("Refusee", { id: "a/b" })], FUSEAU)[0]?.ouvrir).toBe("Home/Dialogue/a%2Fb");
  });

  it("intitulé vide mais présent : gardé tel quel (seul null est remplacé)", () => {
    expect(lignes([resume("Brouillon", { intitule: "", numero: null })], FUSEAU)[0]).toMatchObject({ intitule: "", numero: "—" });
  });

  it("aucune ligne", () => {
    expect(lignes([], FUSEAU)).toEqual([]);
    expect(AUCUNE_FICHE).toBe("Aucune fiche pour l'instant : choisissez un agent, puis « Nouvelle fiche ».");
  });
});
