import { describe, expect, it } from "vitest";
import { dateCourte, dateHeure, FUSEAU } from "../src/core/dates.ts";

// Dates affichées (core/dates.ts, déplacé de features/liste/vue.ts par L2). Attendus recalculés par une réplique Python
// (zoneinfo, Europe/Paris), jamais recopiés de la sortie du code.

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

describe("dateHeure", () => {
  it.each([
    ["2026-11-20T09:00:00+00:00", "20/11/2026 10:00"],
    ["2026-11-20T11:30:00+00:00", "20/11/2026 12:30"],
    ["2026-11-20T23:30:00+00:00", "21/11/2026 00:30"],
    ["2026-12-31T23:00:00+00:00", "01/01/2027 00:00"],
    ["2026-03-29T00:59:00+00:00", "29/03/2026 01:59"],
    ["2026-03-29T01:00:00+00:00", "29/03/2026 03:00"],
    ["2026-10-25T00:59:00+00:00", "25/10/2026 02:59"],
    ["2026-10-25T01:00:00+00:00", "25/10/2026 02:00"],
    ["2026-07-14T21:59:59+00:00", "14/07/2026 23:59"],
    ["2026-11-20T23:30:00-05:00", "21/11/2026 05:30"],
  ])("%s → %s à Paris (24 h, minuit en 00)", (instant, attendu) => {
    expect(dateHeure(instant, FUSEAU)).toBe(attendu);
  });

  it("le fuseau est un paramètre, pas une lecture du poste", () => {
    expect(dateHeure("2026-11-20T23:30:00+00:00", "America/Cayenne")).toBe("20/11/2026 20:30");
  });

  it.each([["pas une date"], [""]])("texte %j non daté : rendu tel quel", (texte) => {
    expect(dateHeure(texte, FUSEAU)).toBe(texte);
  });
});
