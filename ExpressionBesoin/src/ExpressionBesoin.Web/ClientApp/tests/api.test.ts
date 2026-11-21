import { describe, expect, it } from "vitest";
import reference from "./contrats/fiche-reference.json?raw";
import referenceConsultation from "./contrats/consultation-reference.json?raw";
import referenceTour from "./contrats/tour-reference.json?raw";
import referenceListe from "./contrats/liste-reference.json?raw";
import { creerClient, EN_TETE_AGENT, erreurException, erreurInattendue, interpreter, type ClientApi, type Recuperer } from "../src/core/api.ts";
import { cheminDialogue } from "../src/core/chemins.ts";
import { ContratInvalide, lireAgents } from "../src/core/contrats.ts";

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
const ecrire = { Accept: "application/json", "Content-Type": "application/json" };

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

  it("lister : GET des résumés, lus par le contrat de la liste (F3)", async () => {
    const { recuperer, appels } = serveur(200, referenceListe);
    const issue = await creerClient(recuperer, resoudre).lister();
    expect(issue.ok && issue.valeur.map((r) => [r.numero, r.statut])).toEqual([["EB-2026-0007", "ACompleter"], [null, "Brouillon"]]);
    expect(appels).toEqual([{ url: "/base/api/fiches", init: { headers: { Accept: "application/json" } } }]);
  });

  it("lister : une fiche seule n'est pas une liste", async () => {
    const { recuperer } = serveur(200, reference);
    await expect(creerClient(recuperer, resoudre).lister()).rejects.toThrow("resumes devait être un tableau");
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

  it("consulter : GET de la consultation (fiche et historique, DEB-A1), identifiant encodé", async () => {
    const { recuperer, appels } = serveur(200, referenceConsultation);
    const issue = await creerClient(recuperer, resoudre).consulter("a/b");
    expect(issue.ok && [issue.valeur.fiche.statut, issue.valeur.historique.length]).toEqual(["ACompleter", 2]);
    expect(appels).toEqual([{ url: "/base/api/fiches/a%2Fb", init: { headers: { Accept: "application/json" } } }]);
  });

  it("consulter : une fiche seule n'est pas une consultation (DEB-A1)", async () => {
    const { recuperer } = serveur(200, reference);
    await expect(creerClient(recuperer, resoudre).consulter("x")).rejects.toThrow("consultation.fiche devait être un objet");
  });

  it("envoyerMessage : POST JSON, réponse lue comme un tour", async () => {
    const { recuperer, appels } = serveur(200, referenceTour);
    const issue = await creerClient(recuperer, resoudre).envoyerMessage("f1", "Bonjour « vous »");
    expect(issue.ok && issue.valeur.horsPerimetre).toBe(true);
    expect(appels).toEqual([{ url: "/base/api/fiches/f1/messages", init: { method: "POST", headers: ecrire, body: '{"message":"Bonjour « vous »"}' } }]);
  });

  it("modifierChamp : PUT JSON sur la rubrique encodée", async () => {
    const { recuperer, appels } = serveur(200, reference);
    expect((await creerClient(recuperer, resoudre).modifierChamp("f1", "a b", "")).ok).toBe(true);
    expect(appels).toEqual([{ url: "/base/api/fiches/f1/champs/a%20b", init: { method: "PUT", headers: ecrire, body: '{"valeur":""}' } }]);
  });

  it.each([
    ["accepter", (c: ClientApi) => c.accepter("f1", "resume"), "/base/api/fiches/f1/propositions/resume/accepter"],
    ["rejeter", (c: ClientApi) => c.rejeter("f1", "resume"), "/base/api/fiches/f1/propositions/resume/rejeter"],
    ["reprendre", (c: ClientApi) => c.reprendre("f1"), "/base/api/fiches/f1/reprendre"],
  ])("%s : POST sans corps", async (_nom, appeler, url) => {
    const { recuperer, appels } = serveur(200, reference);
    expect((await appeler(creerClient(recuperer, resoudre))).ok).toBe(true);
    expect(appels).toEqual([{ url, init: { method: "POST", headers: { Accept: "application/json" } } }]);
  });

  it.each([
    ["soumettre", (c: ClientApi) => c.soumettre("f1", true), "/base/api/fiches/f1/soumettre", '{"confirme":true}'],
    ["decider", (c: ClientApi) => c.decider("f1", "DemanderComplements", "Préciser"), "/base/api/fiches/f1/decision", '{"decision":"DemanderComplements","commentaire":"Préciser"}'],
  ])("%s : POST JSON", async (_nom, appeler, url, body) => {
    const { recuperer, appels } = serveur(200, reference);
    expect((await appeler(creerClient(recuperer, resoudre))).ok).toBe(true);
    expect(appels).toEqual([{ url, init: { method: "POST", headers: ecrire, body } }]);
  });

  it("refus métier : issue en échec, corps { code, message }", async () => {
    const { recuperer } = serveur(422, '{"code":"soumission.incomplete","message":"Rubriques obligatoires à renseigner : Résumé."}');
    expect(await creerClient(recuperer, resoudre).soumettre("f1", true)).toEqual({
      ok: false,
      statut: 422,
      erreur: { code: "soumission.incomplete", message: "Rubriques obligatoires à renseigner : Résumé." },
    });
  });

  it("urlDocument : adresse résolue, aucun appel", () => {
    const { recuperer, appels } = serveur(200, "");
    expect(creerClient(recuperer, resoudre).urlDocument("a/b")).toBe("/base/api/fiches/a%2Fb/document");
    expect(appels).toEqual([]);
  });
});

describe("erreurException", () => {
  it("réponse hors contrat : message du contrat", () => {
    expect(erreurException(new ContratInvalide("fiche.id", "un texte"))).toEqual({ code: "contrat.invalide", message: "Contrat de l'API : fiche.id devait être un texte." });
  });

  it.each([[new TypeError("Failed to fetch")], ["texte"], [undefined]])("autre exception %# : serveur injoignable", (exception) => {
    expect(erreurException(exception).code).toBe("reseau.indisponible");
  });
});

describe("cheminDialogue", () => {
  it.each([
    ["6f1c2b3a-0000-4000-8000-0000000000c1", "Home/Dialogue/6f1c2b3a-0000-4000-8000-0000000000c1"],
    ["a/b?c", "Home/Dialogue/a%2Fb%3Fc"],
  ])("%s", (id, attendu) => {
    expect(cheminDialogue(id)).toBe(attendu);
  });
});
