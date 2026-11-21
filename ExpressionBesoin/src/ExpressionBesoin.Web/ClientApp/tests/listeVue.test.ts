import { describe, expect, it } from "vitest";
import referenceListe from "./contrats/liste-reference.json?raw";
import { lireResumes, type ResumeVue, type Statut } from "../src/core/contrats.ts";
import { AUCUNE_FICHE, dateCourte, FUSEAU, lignes } from "../src/features/liste/vue.ts";

// Attendus des dates recalculés par une réplique Python (zoneinfo, Europe/Paris), jamais recopiés de la sortie du code.

describe("dateCourte", () => {
  it.each([
    ["2026-11-20T09:00:00+00:00", "20/11/2026"],
    ["2026-11-20T23:30:00+00:00", "21/11/2026"],
    ["2026-12-31T22:59:59+00:00", "31/12/2026"],
    ["2026-12-31T23:00:00+00:00", "01/01/2027"],
    ["2026-03-29T00:30:00+00:00", "29/03/2026"],
    ["2026-07-14T21:59:00+00:00", "14/07/2026"],
    ["2026-07-14T22:00:00+00:00", "15/07/2026"],
    ["2026-11-20T23:30:00-05:00", "21/11/2026"],
  ])("%s → %s à Paris", (instant, attendu) => {
    expect(dateCourte(instant, FUSEAU)).toBe(attendu);
  });

  it("le fuseau est un paramètre, pas une lecture du poste", () => {
    expect(dateCourte("2026-11-20T23:30:00+00:00", "America/Cayenne")).toBe("20/11/2026");
  });

  it.each([["pas une date"], [""]])("texte %j non daté : rendu tel quel", (texte) => {
    expect(dateCourte(texte, FUSEAU)).toBe(texte);
  });

  it("fuseau des dates affichées : celui du numéro et du .docx", () => {
    expect(FUSEAU).toBe("Europe/Paris");
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
