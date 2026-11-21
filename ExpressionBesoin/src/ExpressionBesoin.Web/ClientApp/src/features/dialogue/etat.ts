import type { ConsultationVue, ErreurVue, FicheVue, TourVue, TypeDecision } from "../../core/contrats.ts";

// État de la page Dialogue (DEB-Q49) : réducteur PUR, état × événement → état. Aucune règle métier (DEB-Q50) : la fiche
// vient toujours du serveur ; la page ne garde que ce que l'agent est en train de saisir et l'attente d'une réponse.

/** Une ligne de la conversation ; `horsPerimetre` et `ecartees` ne sont connus que pour un tour joué dans la page. */
export interface LigneDialogue {
  readonly auteur: "Agent" | "IA";
  readonly texte: string;
  readonly horsPerimetre: boolean;
  readonly ecartees: number;
}

export interface EtatDialogue {
  readonly fiche: FicheVue | null;
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
}

export type Evenement =
  | { readonly type: "consultation"; readonly consultation: ConsultationVue }
  | { readonly type: "tour"; readonly envoye: string; readonly tour: TourVue }
  | { readonly type: "fiche"; readonly fiche: FicheVue; readonly enregistree?: string }
  | { readonly type: "refus"; readonly erreur: ErreurVue }
  | { readonly type: "message"; readonly texte: string }
  | { readonly type: "brouillon"; readonly code: string; readonly texte: string }
  | { readonly type: "confirmation"; readonly coche: boolean }
  | { readonly type: "decision"; readonly decision: TypeDecision }
  | { readonly type: "commentaire"; readonly texte: string };

export const ETAT_INITIAL: EtatDialogue = {
  fiche: null,
  conversation: [],
  brouillons: {},
  message: "",
  confirme: false,
  decision: "Valider",
  commentaire: "",
  attente: false,
  erreur: null,
};

/** Départ d'un appel : `null` si un appel est déjà en cours (la page n'en lance pas un second). Pure. */
export function commencer(etat: EtatDialogue): EtatDialogue | null {
  return etat.attente ? null : { ...etat, attente: true, erreur: null };
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

/** Réducteur de la page. Pure. */
export function reduire(etat: EtatDialogue, evenement: Evenement): EtatDialogue {
  switch (evenement.type) {
    case "consultation": {
      const conversation = evenement.consultation.historique.map((echange) => ({ auteur: echange.auteur, texte: echange.texte, horsPerimetre: false, ecartees: 0 }));
      return { ...recevoir(etat, evenement.consultation.fiche), conversation };
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
  }
}
