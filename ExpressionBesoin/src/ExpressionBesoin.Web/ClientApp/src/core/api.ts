import { lireAgents, lireErreur, lireFiche, type AgentVue, type ErreurVue, type FicheVue } from "./contrats.ts";

// Client de l'API (plan §5). Les effets (fetch, résolution d'URL) sont INJECTÉS : le client se teste sans réseau.

/** Issue d'un appel : valeur, ou erreur `{ code, message }` (refus métier 404/409/422, panne 503). */
export type Issue<T> = { readonly ok: true; readonly valeur: T } | { readonly ok: false; readonly statut: number; readonly erreur: ErreurVue };

export type Recuperer = (url: string, init: RequestInit) => Promise<Response>;

export const EN_TETE_AGENT = "X-Agent";

/** Erreur d'un corps illisible : le statut seul est connu. Pure. */
export function erreurInattendue(statut: number): ErreurVue {
  return { code: "http.inattendu", message: `Réponse inattendue du serveur (HTTP ${statut}).` };
}

async function corpsJson(reponse: Response): Promise<unknown> {
  const texte = await reponse.text();
  if (texte === "") return null;
  try {
    return JSON.parse(texte) as unknown;
  } catch {
    return undefined;
  }
}

/** Réponse HTTP → issue ; `lire` vérifie le contrat du succès. */
export async function interpreter<T>(reponse: Response, lire: (v: unknown) => T): Promise<Issue<T>> {
  const corps = await corpsJson(reponse);
  if (reponse.ok) return { ok: true, valeur: lire(corps) };
  return { ok: false, statut: reponse.status, erreur: lireErreur(corps) ?? erreurInattendue(reponse.status) };
}

export interface ClientApi {
  agents(): Promise<Issue<AgentVue[]>>;
  creerFiche(agent: string): Promise<Issue<FicheVue>>;
  fiche(id: string): Promise<Issue<FicheVue>>;
}

export function creerClient(recuperer: Recuperer, resoudre: (chemin: string) => string): ClientApi {
  const appeler = <T>(chemin: string, init: RequestInit, lire: (v: unknown) => T) =>
    recuperer(resoudre(chemin), init).then((reponse) => interpreter(reponse, lire));
  const json = { Accept: "application/json" };
  return {
    agents: () => appeler("api/agents", { headers: json }, lireAgents),
    creerFiche: (agent) => appeler("api/fiches", { method: "POST", headers: { ...json, [EN_TETE_AGENT]: agent } }, (v) => lireFiche(v)),
    fiche: (id) => appeler(`api/fiches/${encodeURIComponent(id)}`, { headers: json }, (v) => lireFiche(v)),
  };
}
