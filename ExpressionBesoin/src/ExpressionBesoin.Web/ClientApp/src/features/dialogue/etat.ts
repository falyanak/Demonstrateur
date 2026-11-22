import type { Issue } from "../../core/api.ts";
import type { ConsultationVue, ErreurVue, FicheVue, TourVue, TransitionVue, TypeDecision } from "../../core/contrats.ts";
import { CIBLE_MESSAGE, insererTexte } from "./dictee.ts";

// État de la page Dialogue (DEB-Q49) : réducteur PUR, état × événement → état. Aucune règle métier (DEB-Q50) : la fiche
// vient toujours du serveur ; la page ne garde que ce que l'agent est en train de saisir et l'attente d'une réponse.

/** Une ligne de la conversation ; `horsPerimetre` et `ecartees` ne sont connus que pour un tour joué dans la page. */
export interface LigneDialogue {
  readonly auteur: "Agent" | "IA";
  readonly texte: string;
  readonly horsPerimetre: boolean;
  readonly ecartees: number;
}

/**
 * Dictée en cours (lot S1) : une à la fois ; `cible` = CIBLE_MESSAGE ou code de rubrique, `position` = curseur de la zone
 * au départ (le texte dicté y sera inséré).
 */
export type EtatDictee =
  | { readonly phase: "inactive" }
  | { readonly phase: "enregistrement" | "transcription"; readonly cible: string; readonly position: number };

export const DICTEE_INACTIVE: EtatDictee = { phase: "inactive" };

export interface EtatDialogue {
  readonly fiche: FicheVue | null;
  /** Cycle de vie tel que relu au serveur (L2, DEB-Q80) : jamais calculé par la page. */
  readonly cycle: readonly TransitionVue[];
  readonly conversation: readonly LigneDialogue[];
  /** Saisie manuelle en cours, par code de rubrique (DEB-Q51) ; gardée tant qu'elle n'est pas enregistrée. */
  readonly brouillons: Readonly<Record<string, string>>;
  readonly message: string;
  readonly confirme: boolean;
  readonly decision: TypeDecision;
  readonly commentaire: string;
  /** Une requête à la fois : vrai entre le départ d'un appel et sa réponse. */
  readonly attente: boolean;
  readonly erreur: ErreurVue | null;
  readonly dictee: EtatDictee;
}

export type Evenement =
  | { readonly type: "consultation"; readonly consultation: ConsultationVue }
  | { readonly type: "tour"; readonly envoye: string; readonly tour: TourVue }
  | { readonly type: "fiche"; readonly fiche: FicheVue; readonly enregistree?: string }
  /** Transition réussie, consultation relue : fiche et cycle remplacés, conversation de la page gardée (DEB-Q80). */
  | { readonly type: "relue"; readonly consultation: ConsultationVue }
  /** Transition réussie, relecture en échec : fiche de l'action, ancien cycle, erreur affichée (DEB-Q80). */
  | { readonly type: "nonRelue"; readonly fiche: FicheVue; readonly erreur: ErreurVue }
  | { readonly type: "refus"; readonly erreur: ErreurVue }
  | { readonly type: "message"; readonly texte: string }
  | { readonly type: "brouillon"; readonly code: string; readonly texte: string }
  | { readonly type: "confirmation"; readonly coche: boolean }
  | { readonly type: "decision"; readonly decision: TypeDecision }
  | { readonly type: "commentaire"; readonly texte: string }
  /** Dictée (lot S1) : départ (micro demandé), envoi au serveur, texte reçu, échec (micro, refus ou panne). */
  | { readonly type: "dicteeDebut"; readonly cible: string; readonly position: number }
  | { readonly type: "dicteeEnvoi" }
  | { readonly type: "dicteeTexte"; readonly texte: string }
  | { readonly type: "dicteeEchec"; readonly erreur: ErreurVue };

export const ETAT_INITIAL: EtatDialogue = {
  fiche: null,
  cycle: [],
  conversation: [],
  brouillons: {},
  message: "",
  confirme: false,
  decision: "Valider",
  commentaire: "",
  attente: false,
  erreur: null,
  dictee: DICTEE_INACTIVE,
};

/** Départ d'un appel : `null` si un appel est déjà en cours (la page n'en lance pas un second). Pure. */
export function commencer(etat: EtatDialogue): EtatDialogue | null {
  return etat.attente ? null : { ...etat, attente: true, erreur: null };
}

/**
 * Après une transition réussie (soumission, décision, reprise), la consultation est relue (DEB-Q80) : relue → fiche et
 * cycle du serveur ; échec → fiche renvoyée par l'action, ancien cycle, erreur affichée. Pure.
 */
export function apresTransition(fiche: FicheVue, relecture: Issue<ConsultationVue>): Evenement {
  return relecture.ok ? { type: "relue", consultation: relecture.valeur } : { type: "nonRelue", fiche, erreur: relecture.erreur };
}

/** Brouillons sans ceux devenus égaux à la valeur retenue (rien à enregistrer) ni ceux d'une rubrique inconnue. Pure. */
export function nettoyerBrouillons(brouillons: Readonly<Record<string, string>>, fiche: FicheVue, enregistree?: string): Record<string, string> {
  const resultat: Record<string, string> = {};
  for (const rubrique of fiche.rubriques) {
    const brouillon = brouillons[rubrique.code];
    if (brouillon !== undefined && rubrique.code !== enregistree && brouillon !== (rubrique.valeur ?? "")) resultat[rubrique.code] = brouillon;
  }
  return resultat;
}

/** Nouvelle fiche reçue : brouillons nettoyés ; confirmation et commentaire remis à zéro quand le statut change. */
function recevoir(etat: EtatDialogue, fiche: FicheVue, enregistree?: string): EtatDialogue {
  const statutChange = etat.fiche?.statut !== fiche.statut;
  return {
    ...etat,
    fiche,
    brouillons: nettoyerBrouillons(etat.brouillons, fiche, enregistree),
    confirme: statutChange ? false : etat.confirme,
    commentaire: statutChange ? "" : etat.commentaire,
    attente: false,
    erreur: null,
  };
}

/**
 * Texte dicté inséré dans sa cible : la zone de message, ou le brouillon de la rubrique (à défaut sa valeur retenue).
 * Pure.
 */
export function insererDictee(etat: EtatDialogue, cible: string, position: number, texte: string): EtatDialogue {
  if (cible === CIBLE_MESSAGE) return { ...etat, message: insererTexte(etat.message, texte, position) };
  const base = etat.brouillons[cible] ?? etat.fiche?.rubriques.find((rubrique) => rubrique.code === cible)?.valeur ?? "";
  return { ...etat, brouillons: { ...etat.brouillons, [cible]: insererTexte(base, texte, position) } };
}

/** Réducteur de la page. Pure. */
export function reduire(etat: EtatDialogue, evenement: Evenement): EtatDialogue {
  switch (evenement.type) {
    case "consultation": {
      const conversation = evenement.consultation.historique.map((echange) => ({ auteur: echange.auteur, texte: echange.texte, horsPerimetre: false, ecartees: 0 }));
      return { ...recevoir(etat, evenement.consultation.fiche), cycle: evenement.consultation.cycle, conversation };
    }
    case "tour": {
      const { tour } = evenement;
      const conversation = [
        ...etat.conversation,
        { auteur: "Agent" as const, texte: evenement.envoye, horsPerimetre: false, ecartees: 0 },
        { auteur: "IA" as const, texte: tour.message, horsPerimetre: tour.horsPerimetre, ecartees: tour.ecartees.length },
      ];
      return { ...recevoir(etat, tour.fiche), conversation, message: "" };
    }
    case "fiche":
      return recevoir(etat, evenement.fiche, evenement.enregistree);
    case "relue":
      return { ...recevoir(etat, evenement.consultation.fiche), cycle: evenement.consultation.cycle };
    case "nonRelue":
      return { ...recevoir(etat, evenement.fiche), erreur: evenement.erreur };
    case "refus":
      return { ...etat, attente: false, erreur: evenement.erreur };
    case "message":
      return { ...etat, message: evenement.texte };
    case "brouillon":
      return { ...etat, brouillons: { ...etat.brouillons, [evenement.code]: evenement.texte } };
    case "confirmation":
      return { ...etat, confirme: evenement.coche };
    case "decision":
      return { ...etat, decision: evenement.decision };
    case "commentaire":
      return { ...etat, commentaire: evenement.texte };
    case "dicteeDebut":
      return etat.dictee.phase === "inactive"
        ? { ...etat, dictee: { phase: "enregistrement", cible: evenement.cible, position: evenement.position }, erreur: null }
        : etat;
    case "dicteeEnvoi":
      return etat.dictee.phase === "enregistrement" ? { ...etat, dictee: { ...etat.dictee, phase: "transcription" } } : etat;
    case "dicteeTexte":
      return etat.dictee.phase === "transcription"
        ? { ...insererDictee(etat, etat.dictee.cible, etat.dictee.position, evenement.texte), dictee: DICTEE_INACTIVE }
        : etat;
    case "dicteeEchec":
      return { ...etat, dictee: DICTEE_INACTIVE, erreur: evenement.erreur };
  }
}
