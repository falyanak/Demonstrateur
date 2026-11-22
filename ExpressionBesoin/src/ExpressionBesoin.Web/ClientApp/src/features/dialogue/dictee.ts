import type { ErreurVue } from "../../core/contrats.ts";

// Dictée de la page Dialogue (lot S1, DEB-Q133) : fonctions PURES. Le texte dicté s'insère dans la zone choisie, à la
// position du curseur au départ de la dictée ; il n'est jamais envoyé ni enregistré de lui-même : l'agent le relit, puis
// clique sur « Envoyer » ou « Enregistrer » (acte explicite, comme DEB-Q51).

/** Cible de la dictée pour la zone de message ; les rubriques sont désignées par leur code (camelCase, sans « @ »). */
export const CIBLE_MESSAGE = "@message";

/** Durée d'une dictée : arrêt automatique (le serveur accepte 125 s, DecisionsDictee.SecondesMax). */
export const SECONDES_MAX = 120;

/**
 * Texte dicté inséré à `position` (bornée au texte), sans espaces de bord, séparé d'un espace de ce qui le touche sauf
 * blanc déjà présent ; dictée blanche → texte inchangé. Pure.
 */
export function insererTexte(texte: string, ajout: string, position: number): string {
  const dicte = ajout.trim();
  if (dicte === "") return texte;
  const p = Math.max(0, Math.min(texte.length, Number.isFinite(position) ? Math.trunc(position) : texte.length));
  const avant = texte.slice(0, p);
  const apres = texte.slice(p);
  const espaceAvant = avant === "" || /\s$/.test(avant) ? "" : " ";
  const espaceApres = apres === "" || /^\s/.test(apres) ? "" : " ";
  return `${avant}${espaceAvant}${dicte}${espaceApres}${apres}`;
}

/** Nom d'une erreur du navigateur (`DOMException.name`…), `undefined` sinon. Pure. */
function nom(exception: unknown): string | undefined {
  if (typeof exception !== "object" || exception === null || !("name" in exception)) return undefined;
  return typeof exception.name === "string" ? exception.name : undefined;
}

/** Erreur du micro ou du décodage → message affichable, jamais propagée à la page. Pure. */
export function erreurMicro(exception: unknown): ErreurVue {
  switch (nom(exception)) {
    case "NotAllowedError":
    case "SecurityError":
      return { code: "micro.refuse", message: "Micro refusé : autorisez le micro pour ce site, puis réessayez." };
    case "NotFoundError":
    case "OverconstrainedError":
      return { code: "micro.absent", message: "Aucun micro détecté : branchez-en un, puis réessayez." };
    case "EncodingError":
      return { code: "dictee.illisible", message: "Enregistrement illisible : réessayez en parlant un peu plus longtemps." };
    default:
      return { code: "micro.indisponible", message: "Micro indisponible dans ce navigateur." };
  }
}
