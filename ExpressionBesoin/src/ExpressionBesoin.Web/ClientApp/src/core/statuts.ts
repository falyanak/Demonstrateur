import type { Statut } from "./contrats.ts";

// Affichage d'un statut de fiche (plan §3), commun aux pages Dialogue et Liste : un seul endroit (DEB-Q61). PUR.

export interface Badge {
  readonly texte: string;
  /** Suffixe de couleur Bootstrap (`text-bg-…`). */
  readonly couleur: "secondary" | "warning" | "info" | "primary" | "success" | "danger";
}

const STATUTS: Readonly<Record<Statut, Badge>> = {
  Brouillon: { texte: "Brouillon", couleur: "secondary" },
  Soumise: { texte: "Soumise", couleur: "info" },
  Validee: { texte: "Validée", couleur: "success" },
  ACompleter: { texte: "À compléter", couleur: "warning" },
  Refusee: { texte: "Refusée", couleur: "danger" },
};

export function badgeStatut(statut: Statut): Badge {
  return STATUTS[statut];
}
