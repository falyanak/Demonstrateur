import { describe, expect, it } from "vitest";
import reference from "./contrats/fiche-reference.json?raw";
import { creerClient, EN_TETE_AGENT, erreurInattendue, interpreter, type Recuperer } from "../src/core/api.ts";
import { lireAgents } from "../src/core/contrats.ts";

interface Appel { readonly url: string; readonly init: RequestInit }

function serveur(statut: number, corps: string): { recuperer: Recuperer; appels: Appel[] } {
  const appels: Appel[] = [];
  const recuperer: Recuperer = (url, init) => {
    appels.push({ url, init });
    return Promise.resolve(new Response(corps === "" ? null : corps, { status: statut }));
  };
  return { recuperer, appels };
}

const resoudre = (chemin: string) => `/base/${chemin}`;

describe("interpreter", () => {
  it("succès : corps lu par le contrat", async () => {
    const issue = await interpreter(new Response('[{"identifiant":"a","nom":"N","bureau":"B","valideur":"V"}]'), lireAgents);
    expect(issue).toEqual({ ok: true, valeur: [{ identifiant: "a", nom: "N", bureau: "B", valideur: "V" }] });
  });

  it.each([
    [404, '{"code":"fiche.introuvable","message":"Aucune fiche."}', { code: "fiche.introuvable", message: "Aucune fiche." }],
    [503, '{"code":"panne.technique","message":"Indisponible."}', { code: "panne.technique", message: "Indisponible." }],
    [500, "<html>erreur</html>", erreurInattendue(500)],
    [502, "", erreurInattendue(502)],
    [422, '{"autre":1}', erreurInattendue(422)],
  ])("échec HTTP %i", async (statut, corps, erreur) => {
    expect(await interpreter(new Response(corps === "" ? null : corps, { status: statut }), lireAgents)).toEqual({ ok: false, statut, erreur });
  });

  it("succès au corps hors contrat : lève, jamais deviné", async () => {
    await expect(interpreter(new Response("{}"), lireAgents)).rejects.toThrow("agents devait être un tableau");
  });

  it("erreurInattendue cite le statut", () => {
    expect(erreurInattendue(500)).toEqual({ code: "http.inattendu", message: "Réponse inattendue du serveur (HTTP 500)." });
  });
});

describe("creerClient", () => {
  it("agents : GET résolu contre la base", async () => {
    const { recuperer, appels } = serveur(200, "[]");
    expect(await creerClient(recuperer, resoudre).agents()).toEqual({ ok: true, valeur: [] });
    expect(appels).toEqual([{ url: "/base/api/agents", init: { headers: { Accept: "application/json" } } }]);
  });

  it("creerFiche : POST avec l'en-tête de l'agent", async () => {
    const { recuperer, appels } = serveur(201, reference);
    const issue = await creerClient(recuperer, resoudre).creerFiche("camille.martin");
    expect(issue.ok && issue.valeur.numero).toBe("EB-2026-0007");
    expect(EN_TETE_AGENT).toBe("X-Agent");
    expect(appels).toEqual([
      { url: "/base/api/fiches", init: { method: "POST", headers: { Accept: "application/json", "X-Agent": "camille.martin" } } },
    ]);
  });

  it("fiche : identifiant encodé dans le chemin", async () => {
    const { recuperer, appels } = serveur(200, reference);
    const issue = await creerClient(recuperer, resoudre).fiche("a/b");
    expect(issue.ok && issue.valeur.statut).toBe("ACompleter");
    expect(appels[0]?.url).toBe("/base/api/fiches/a%2Fb");
  });
});
