import { cheminDialogue } from "../../core/chemins.ts";
import type { ResumeVue } from "../../core/contrats.ts";
import { dateCourte } from "../../core/dates.ts";
import { badgeStatut, type Badge } from "../../core/statuts.ts";

// Modèle d'affichage de la page Liste (F3, DEB-Q59 à DEB-Q63) : fonctions PURES, sans DOM. Aucune règle métier : le
// statut vient du serveur ; « Télécharger » suit la seule règle du contrat (document servi pour une fiche validée).

// Date et fuseau vivent dans core/dates.ts (communs avec le cycle de vie de la page Dialogue, L2) ; réexportés ici.
export { dateCourte, FUSEAU } from "../../core/dates.ts";

export interface LigneListe {
  readonly id: string;
  /** Numéro `EB-AAAA-NNNN`, « — » avant la première soumission. */
  readonly numero: string;
  /** Intitulé court, « (sans intitulé) » tant qu'il n'est pas retenu. */
  readonly intitule: string;
  readonly demandeur: string;
  /** Date de création, jj/mm/aaaa dans le fuseau. */
  readonly creeeLe: string;
  readonly badge: Badge;
  /** Action « Ouvrir » (DEB-Q60) : chemin applicatif de la page Dialogue, à résoudre. */
  readonly ouvrir: string;
  /** Action « Télécharger » : fiche validée seulement (409 sinon, plan §5). */
  readonly telecharger: boolean;
}

export const AUCUNE_FICHE = "Aucune fiche pour l'instant : choisissez un agent, puis « Nouvelle fiche ».";

/** Une ligne par résumé, dans l'ordre reçu du serveur (plus récente d'abord, DEB-Q63). Pure. */
export function lignes(resumes: readonly ResumeVue[], fuseau: string): LigneListe[] {
  return resumes.map((resume) => ({
    id: resume.id,
    numero: resume.numero ?? "—",
    intitule: resume.intitule ?? "(sans intitulé)",
    demandeur: resume.demandeur,
    creeeLe: dateCourte(resume.creeeLe, fuseau),
    badge: badgeStatut(resume.statut),
    ouvrir: cheminDialogue(resume.id),
    telecharger: resume.statut === "Validee",
  }));
}
