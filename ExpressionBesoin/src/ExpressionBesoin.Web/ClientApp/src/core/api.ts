import {
  ContratInvalide,
  lireAgents,
  lireConsultation,
  lireDictee,
  lireErreur,
  lireFiche,
  lireResumes,
  lireTour,
  type AgentVue,
  type ConsultationVue,
  type DicteeVue,
  type ErreurVue,
  type FicheVue,
  type ResumeVue,
  type TourVue,
  type TypeDecision,
} from "./contrats.ts";

// Client de l'API (plan §5). Les effets (fetch, résolution d'URL) sont INJECTÉS : le client se teste sans réseau.

/** Issue d'un appel : valeur, ou erreur `{ code, message }` (refus métier 404/409/422, panne 503). */
export type Issue<T> = { readonly ok: true; readonly valeur: T } | { readonly ok: false; readonly statut: number; readonly erreur: ErreurVue };

export type Recuperer = (url: string, init: RequestInit) => Promise<Response>;

export const EN_TETE_AGENT = "X-Agent";

/** Erreur d'un corps illisible : le statut seul est connu. Pure. */
export function erreurInattendue(statut: number): ErreurVue {
  return { code: "http.inattendu", message: `Réponse inattendue du serveur (HTTP ${statut}).` };
}

/**
 * Exception d'un appel (réseau coupé, réponse hors contrat) → erreur affichable ; jamais propagée à la page. Pure.
 */
export function erreurException(exception: unknown): ErreurVue {
  if (exception instanceof ContratInvalide) return { code: "contrat.invalide", message: exception.message };
  return { code: "reseau.indisponible", message: "Le serveur ne répond pas ; vérifier qu'il est lancé, puis réessayer." };
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
  /** Résumés de toutes les fiches, de la plus récente à la plus ancienne (F3). */
  lister(): Promise<Issue<ResumeVue[]>>;
  creerFiche(agent: string): Promise<Issue<FicheVue>>;
  consulter(id: string): Promise<Issue<ConsultationVue>>;
  envoyerMessage(id: string, message: string): Promise<Issue<TourVue>>;
  modifierChamp(id: string, rubrique: string, valeur: string): Promise<Issue<FicheVue>>;
  accepter(id: string, rubrique: string): Promise<Issue<FicheVue>>;
  rejeter(id: string, rubrique: string): Promise<Issue<FicheVue>>;
  soumettre(id: string, confirme: boolean): Promise<Issue<FicheVue>>;
  decider(id: string, decision: TypeDecision, commentaire: string): Promise<Issue<FicheVue>>;
  reprendre(id: string): Promise<Issue<FicheVue>>;
  /** Dictée (lot S1) : WAV canonique → texte ; refus `dictee.*` (422), panne 503. */
  dicter(audio: Uint8Array<ArrayBuffer>): Promise<Issue<DicteeVue>>;
  /** Adresse du .docx (lien de téléchargement, aucun appel). */
  urlDocument(id: string): string;
}

export function creerClient(recuperer: Recuperer, resoudre: (chemin: string) => string): ClientApi {
  const appeler = <T>(chemin: string, init: RequestInit, lire: (v: unknown) => T) =>
    recuperer(resoudre(chemin), init).then((reponse) => interpreter(reponse, lire));
  const json = { Accept: "application/json" };
  const envoyer = (corps: unknown): RequestInit => ({
    method: "POST",
    headers: { ...json, "Content-Type": "application/json" },
    body: JSON.stringify(corps),
  });
  const fiche = (id: string) => `api/fiches/${encodeURIComponent(id)}`;
  const rubrique = (id: string, segment: string, code: string) => `${fiche(id)}/${segment}/${encodeURIComponent(code)}`;
  const lire = (v: unknown) => lireFiche(v);
  return {
    agents: () => appeler("api/agents", { headers: json }, lireAgents),
    lister: () => appeler("api/fiches", { headers: json }, lireResumes),
    creerFiche: (agent) => appeler("api/fiches", { method: "POST", headers: { ...json, [EN_TETE_AGENT]: agent } }, lire),
    consulter: (id) => appeler(fiche(id), { headers: json }, lireConsultation),
    envoyerMessage: (id, message) => appeler(`${fiche(id)}/messages`, envoyer({ message }), lireTour),
    modifierChamp: (id, code, valeur) => appeler(rubrique(id, "champs", code), { ...envoyer({ valeur }), method: "PUT" }, lire),
    accepter: (id, code) => appeler(`${rubrique(id, "propositions", code)}/accepter`, { method: "POST", headers: json }, lire),
    rejeter: (id, code) => appeler(`${rubrique(id, "propositions", code)}/rejeter`, { method: "POST", headers: json }, lire),
    soumettre: (id, confirme) => appeler(`${fiche(id)}/soumettre`, envoyer({ confirme }), lire),
    decider: (id, decision, commentaire) => appeler(`${fiche(id)}/decision`, envoyer({ decision, commentaire }), lire),
    reprendre: (id) => appeler(`${fiche(id)}/reprendre`, { method: "POST", headers: json }, lire),
    dicter: (audio) => appeler("api/dictee", { method: "POST", headers: { ...json, "Content-Type": "audio/wav" }, body: audio }, lireDictee),
    urlDocument: (id) => resoudre(`${fiche(id)}/document`),
  };
}
