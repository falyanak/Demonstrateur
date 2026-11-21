import type { Etape, FicheVue, RubriqueVue, TransitionVue, TypeDecision } from "../../core/contrats.ts";
import { dateHeure } from "../../core/dates.ts";
import { badgeStatut, type Badge } from "../../core/statuts.ts";
import type { EtatDialogue } from "./etat.ts";

// Modèle d'affichage de la page Dialogue (DEB-Q49) : fonctions PURES, sans DOM. Les actions ne dépendent que des champs
// calculés par le Domaine (statut, modifiable, complete) et de la case de confirmation (DEB-Q50, DEB-Q53).

// Badge et badgeStatut vivent dans core/statuts.ts (commun avec la page Liste, F3) ; réexportés pour la page Dialogue.
export { badgeStatut, type Badge };

export interface RubriqueAffichee {
  readonly code: string;
  readonly libelle: string;
  readonly obligatoire: boolean;
  readonly badge: Badge;
  /** Texte de la zone de saisie : le brouillon s'il existe, sinon la valeur retenue. */
  readonly saisie: string;
  readonly proposition: { readonly valeur: string; readonly justification: string } | null;
  readonly questionnee: boolean;
}

export interface BlocAffiche {
  readonly titre: string;
  readonly rubriques: readonly RubriqueAffichee[];
}

export interface Actions {
  readonly dialoguer: boolean;
  readonly editer: boolean;
  readonly soumettre: boolean;
  readonly decider: boolean;
  readonly reprendre: boolean;
  readonly telecharger: boolean;
}

const TITRES_BLOCS: Readonly<Record<string, string>> = {
  Identification: "Identification",
  Synthese: "1. Synthèse",
  Contexte: "2. Contexte",
  Objectifs: "3. Objectifs",
  Perimetre: "4. Périmètre",
  Benefices: "5. Bénéfices",
  Complements: "6. Compléments",
};

export const DECISIONS: readonly { readonly valeur: TypeDecision; readonly libelle: string }[] = [
  { valeur: "Valider", libelle: "Valider" },
  { valeur: "DemanderComplements", libelle: "Demander des compléments" },
  { valeur: "Refuser", libelle: "Refuser" },
];

/** Titre d'un bloc du modèle FEB ; un bloc inconnu garde son nom. Pure. */
export function titreBloc(bloc: string): string {
  return Object.hasOwn(TITRES_BLOCS, bloc) ? (TITRES_BLOCS[bloc] as string) : bloc;
}

/** Origine affichée (plan §6) : « Proposée » = vide avec une proposition en attente (DEB-Q13). Pure. */
export function badgeRubrique(rubrique: RubriqueVue): Badge {
  if (rubrique.valeur === null && rubrique.proposition !== null) return { texte: "Proposée", couleur: "warning" };
  switch (rubrique.origine) {
    case "AccepteeIA":
      return { texte: "IA acceptée", couleur: "info" };
    case "Manuelle":
      return { texte: "Manuelle", couleur: "primary" };
    case "Vide":
      return { texte: "Vide", couleur: "secondary" };
  }
}

/** Progression des rubriques obligatoires : « 7/10 » et pourcentage entier (0 sans obligatoire). Pure. */
export function progression(fiche: FicheVue): { readonly texte: string; readonly pourcent: number } {
  const pourcent = fiche.obligatoires === 0 ? 0 : Math.round((fiche.remplies * 100) / fiche.obligatoires);
  return { texte: `${fiche.remplies}/${fiche.obligatoires}`, pourcent };
}

export function titreFiche(fiche: FicheVue): string {
  return fiche.numero ?? "Nouvelle fiche";
}

/** Rubriques groupées par bloc, dans l'ordre du modèle reçu du serveur. Pure. */
export function blocs(fiche: FicheVue, brouillons: Readonly<Record<string, string>>): BlocAffiche[] {
  const resultat: { titre: string; rubriques: RubriqueAffichee[] }[] = [];
  let bloc = "";
  for (const rubrique of fiche.rubriques) {
    if (resultat.length === 0 || rubrique.bloc !== bloc) {
      bloc = rubrique.bloc;
      resultat.push({ titre: titreBloc(bloc), rubriques: [] });
    }
    const retenue = rubrique.valeur ?? "";
    const brouillon = brouillons[rubrique.code];
    (resultat[resultat.length - 1] as { rubriques: RubriqueAffichee[] }).rubriques.push({
      code: rubrique.code,
      libelle: rubrique.libelle,
      obligatoire: rubrique.obligatoire,
      badge: badgeRubrique(rubrique),
      saisie: brouillon ?? retenue,
      proposition: rubrique.proposition,
      questionnee: rubrique.code === fiche.prochaineRubrique,
    });
  }
  return resultat;
}

/** Actions permises (DEB-Q53) ; toutes fermées pendant une attente, sauf le lien du document. Pure. */
export function actions(etat: EtatDialogue): Actions {
  const fiche = etat.fiche;
  const libre = !etat.attente && fiche !== null;
  const statut = fiche?.statut;
  const modifiable = libre && fiche.modifiable;
  return {
    dialoguer: modifiable,
    editer: modifiable,
    soumettre: modifiable && fiche.complete && etat.confirme,
    decider: libre && statut === "Soumise",
    reprendre: libre && statut === "ACompleter",
    telecharger: statut === "Validee",
  };
}

/** « Enregistrer » d'une rubrique (DEB-Q51) : édition permise et brouillon différent de la valeur retenue. Pure. */
export function peutEnregistrer(etat: EtatDialogue, code: string): boolean {
  const brouillon = etat.brouillons[code];
  const rubrique = etat.fiche?.rubriques.find((r) => r.code === code);
  return actions(etat).editer && rubrique !== undefined && brouillon !== undefined && brouillon !== (rubrique.valeur ?? "");
}

/** Note sous une réponse de l'IA : propositions écartées par la lecture de la réponse ; `null` s'il n'y en a pas. Pure. */
export function noteEcartees(ecartees: number): string | null {
  if (ecartees <= 0) return null;
  return ecartees === 1 ? "1 proposition de l'IA écartée (forme non reconnue)." : `${ecartees} propositions de l'IA écartées (forme non reconnue).`;
}

/** Une ligne du panneau « Cycle de vie » (L2, DEB-Q81). */
export interface LigneCycle {
  readonly etape: string;
  /** Statut avant la transition ; `null` à la création (affiché « — »). */
  readonly avant: Badge | null;
  readonly apres: Badge;
  readonly acteur: string;
  /** Date et heure, jj/mm/aaaa hh:mm dans le fuseau. */
  readonly le: string;
}

const ETAPES: Readonly<Record<Etape, string>> = {
  Creation: "Création",
  Soumission: "Soumission",
  Validation: "Validation",
  DemandeComplements: "Demande de compléments",
  Refus: "Refus",
  Reprise: "Reprise",
};

/** Cycle vide : fiche créée avant la migration au schéma 2, sans reconstitution (DEB-Q76). */
export const CYCLE_VIDE = "Aucune transition enregistrée (fiche antérieure au suivi du cycle de vie).";

/** Libellé d'une étape (plan §3). Pure. */
export function libelleEtape(etape: Etape): string {
  return ETAPES[etape];
}

/** Lignes du panneau, dans l'ordre du serveur (rang) ; aucune transition déduite par la page (DEB-Q50, DEB-Q80). Pure. */
export function lignesCycle(cycle: readonly TransitionVue[], fuseau: string): LigneCycle[] {
  return cycle.map((transition) => ({
    etape: libelleEtape(transition.etape),
    avant: transition.avant === null ? null : badgeStatut(transition.avant),
    apres: badgeStatut(transition.apres),
    acteur: transition.acteur,
    le: dateHeure(transition.le, fuseau),
  }));
}
