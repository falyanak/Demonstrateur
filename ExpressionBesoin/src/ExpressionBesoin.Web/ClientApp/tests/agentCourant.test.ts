import { describe, expect, it } from "vitest";
import { choisirAgent, CLE_AGENT, lireMemorise, memoriser } from "../src/core/agentCourant.ts";
import { choisirPage, type ChargeurPage } from "../src/core/aiguillage.ts";

const agent = (identifiant: string) => ({ identifiant, nom: identifiant, bureau: "B", valideur: "V" });

describe("choisirAgent", () => {
  it.each([
    [["a", "b"], "b", "b"],
    [["a", "b"], "z", "a"],
    [["a", "b"], null, "a"],
    [[], "a", null],
    [[], null, null],
  ])("%j, mémorisé %s → %s", (identifiants, memorise, attendu) => {
    expect(choisirAgent(identifiants.map(agent), memorise)).toBe(attendu);
  });
});

describe("stockage tolérant", () => {
  it("lit et écrit sous la clé du projet", () => {
    const valeurs = new Map<string, string>();
    const stockage = { getItem: (k: string) => valeurs.get(k) ?? null, setItem: (k: string, v: string) => void valeurs.set(k, v) };
    expect(lireMemorise(stockage)).toBeNull();
    memoriser(stockage, "louis.petit");
    expect(valeurs.get(CLE_AGENT)).toBe("louis.petit");
    expect(lireMemorise(stockage)).toBe("louis.petit");
    expect(CLE_AGENT).toBe("expressionBesoin.agent");
  });

  it("stockage absent ou qui lève : sans effet", () => {
    const leve = {
      getItem: (): string | null => {
        throw new Error("SecurityError");
      },
      setItem: (): void => {
        throw new Error("QuotaExceededError");
      },
    };
    expect(lireMemorise(undefined)).toBeNull();
    expect(lireMemorise(leve)).toBeNull();
    expect(() => memoriser(leve, "a")).not.toThrow();
    expect(() => memoriser(undefined, "a")).not.toThrow();
  });
});

describe("choisirPage", () => {
  const accueil: ChargeurPage = () => Promise.resolve({ monter: () => undefined });
  const pages = { accueil };

  it.each([
    ["accueil", accueil],
    ["inconnue", null],
    ["", null],
    [undefined, null],
    ["toString", null],
    ["__proto__", null],
  ])("%s", (nom, attendu) => {
    expect(choisirPage(pages, nom)).toBe(attendu);
  });
});
