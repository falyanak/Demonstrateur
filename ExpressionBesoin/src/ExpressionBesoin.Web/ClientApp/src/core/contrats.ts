// Contrat JSON de l'API (plan §5, DEB-Q43) : mêmes octets testés par xUnit (FicheVue, ContratJson) et Vitest
// (tests/contrats/fiche-reference.json). Lecture vérifiée : un écart de forme lève, il n'est jamais deviné.

export type Statut = "Brouillon" | "Soumise" | "Validee" | "ACompleter" | "Refusee";
export type Origine = "Vide" | "AccepteeIA" | "Manuelle";
export type TypeDecision = "Valider" | "DemanderComplements" | "Refuser";

export interface PropositionVue { readonly valeur: string; readonly justification: string }

export interface RubriqueVue {
  readonly code: string;
  readonly libelle: string;
  readonly bloc: string;
  readonly obligatoire: boolean;
  readonly valeur: string | null;
  readonly origine: Origine;
  readonly proposition: PropositionVue | null;
}

export interface EnTeteVue { readonly demandeur: string; readonly bureau: string; readonly valideur: string; readonly creeeLe: string }

export interface DecisionVue { readonly type: TypeDecision; readonly auteur: string; readonly le: string; readonly commentaire: string | null }

export interface FicheVue {
  readonly id: string;
  readonly numero: string | null;
  readonly statut: Statut;
  readonly enTete: EnTeteVue;
  readonly rubriques: readonly RubriqueVue[];
  readonly obligatoires: number;
  readonly remplies: number;
  readonly complete: boolean;
  readonly modifiable: boolean;
  readonly prochaineRubrique: string | null;
  readonly confirmeeLe: string | null;
  readonly decisions: readonly DecisionVue[];
}

export interface AgentVue { readonly identifiant: string; readonly nom: string; readonly bureau: string; readonly valideur: string }

export interface ErreurVue { readonly code: string; readonly message: string }

type Objet = Readonly<Record<string, unknown>>;

const STATUTS: readonly string[] = ["Brouillon", "Soumise", "Validee", "ACompleter", "Refusee"];
const ORIGINES: readonly string[] = ["Vide", "AccepteeIA", "Manuelle"];
const DECISIONS: readonly string[] = ["Valider", "DemanderComplements", "Refuser"];

export class ContratInvalide extends Error {
  constructor(chemin: string, attendu: string) {
    super(`Contrat de l'API : ${chemin} devait être ${attendu}.`);
    this.name = "ContratInvalide";
  }
}

function objet(valeur: unknown, chemin: string): Objet {
  if (typeof valeur !== "object" || valeur === null || Array.isArray(valeur)) throw new ContratInvalide(chemin, "un objet");
  return valeur as Objet;
}

function texte(o: Objet, cle: string, chemin: string): string {
  const v = o[cle];
  if (typeof v !== "string") throw new ContratInvalide(`${chemin}.${cle}`, "un texte");
  return v;
}

function texteOuNul(o: Objet, cle: string, chemin: string): string | null {
  return o[cle] === null ? null : texte(o, cle, chemin);
}

function booleen(o: Objet, cle: string, chemin: string): boolean {
  const v = o[cle];
  if (typeof v !== "boolean") throw new ContratInvalide(`${chemin}.${cle}`, "un booléen");
  return v;
}

function entier(o: Objet, cle: string, chemin: string): number {
  const v = o[cle];
  if (typeof v !== "number" || !Number.isInteger(v)) throw new ContratInvalide(`${chemin}.${cle}`, "un entier");
  return v;
}

function parmi<T extends string>(o: Objet, cle: string, chemin: string, valeurs: readonly string[]): T {
  const v = texte(o, cle, chemin);
  if (!valeurs.includes(v)) throw new ContratInvalide(`${chemin}.${cle}`, `l'une de ${valeurs.join(", ")}`);
  return v as T;
}

function tableau<T>(o: Objet, cle: string, chemin: string, lire: (v: unknown, c: string) => T): T[] {
  const v = o[cle];
  if (!Array.isArray(v)) throw new ContratInvalide(`${chemin}.${cle}`, "un tableau");
  return v.map((element, i) => lire(element, `${chemin}.${cle}[${i}]`));
}

function lireProposition(v: unknown, c: string): PropositionVue | null {
  if (v === null) return null;
  const o = objet(v, c);
  return { valeur: texte(o, "valeur", c), justification: texte(o, "justification", c) };
}

function lireRubrique(v: unknown, c: string): RubriqueVue {
  const o = objet(v, c);
  return {
    code: texte(o, "code", c),
    libelle: texte(o, "libelle", c),
    bloc: texte(o, "bloc", c),
    obligatoire: booleen(o, "obligatoire", c),
    valeur: texteOuNul(o, "valeur", c),
    origine: parmi<Origine>(o, "origine", c, ORIGINES),
    proposition: lireProposition(o["proposition"], `${c}.proposition`),
  };
}

function lireDecision(v: unknown, c: string): DecisionVue {
  const o = objet(v, c);
  return {
    type: parmi<TypeDecision>(o, "type", c, DECISIONS),
    auteur: texte(o, "auteur", c),
    le: texte(o, "le", c),
    commentaire: texteOuNul(o, "commentaire", c),
  };
}

/** Fiche reçue de l'API, vérifiée champ par champ. Pure. */
export function lireFiche(valeur: unknown, chemin = "fiche"): FicheVue {
  const o = objet(valeur, chemin);
  const e = objet(o["enTete"], `${chemin}.enTete`);
  const ce = `${chemin}.enTete`;
  return {
    id: texte(o, "id", chemin),
    numero: texteOuNul(o, "numero", chemin),
    statut: parmi<Statut>(o, "statut", chemin, STATUTS),
    enTete: {
      demandeur: texte(e, "demandeur", ce),
      bureau: texte(e, "bureau", ce),
      valideur: texte(e, "valideur", ce),
      creeeLe: texte(e, "creeeLe", ce),
    },
    rubriques: tableau(o, "rubriques", chemin, lireRubrique),
    obligatoires: entier(o, "obligatoires", chemin),
    remplies: entier(o, "remplies", chemin),
    complete: booleen(o, "complete", chemin),
    modifiable: booleen(o, "modifiable", chemin),
    prochaineRubrique: texteOuNul(o, "prochaineRubrique", chemin),
    confirmeeLe: texteOuNul(o, "confirmeeLe", chemin),
    decisions: tableau(o, "decisions", chemin, lireDecision),
  };
}

/** Agents du sélecteur (GET api/agents). Pure. */
export function lireAgents(valeur: unknown): AgentVue[] {
  if (!Array.isArray(valeur)) throw new ContratInvalide("agents", "un tableau");
  return valeur.map((v, i) => {
    const c = `agents[${i}]`;
    const o = objet(v, c);
    return { identifiant: texte(o, "identifiant", c), nom: texte(o, "nom", c), bureau: texte(o, "bureau", c), valideur: texte(o, "valideur", c) };
  });
}

/** Corps d'un refus ou d'une panne (`{ code, message }`) ; `null` si le corps n'a pas cette forme. Pure. */
export function lireErreur(valeur: unknown): ErreurVue | null {
  if (typeof valeur !== "object" || valeur === null) return null;
  const o = valeur as Objet;
  return typeof o["code"] === "string" && typeof o["message"] === "string" ? { code: o["code"], message: o["message"] } : null;
}
