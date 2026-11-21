import { cheminDialogue } from "../../core/chemins.ts";
import type { ResumeVue } from "../../core/contrats.ts";
import { badgeStatut, type Badge } from "../../core/statuts.ts";

// Modèle d'affichage de la page Liste (F3, DEB-Q59 à DEB-Q63) : fonctions PURES, sans DOM. Aucune règle métier : le
// statut vient du serveur ; « Télécharger » suit la seule règle du contrat (document servi pour une fiche validée).

/** Fuseau des dates affichées : celui du numéro et du .docx (DEB-Q24, DEB-Q36, DEB-Q62). */
export const FUSEAU = "Europe/Paris";

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

/**
 * Instant ISO 8601 → jj/mm/aaaa dans le fuseau, chiffres latins quelle que soit la langue du poste ; texte reçu rendu
 * tel quel s'il n'est pas une date (jamais deviné, jamais masqué). Pure.
 */
export function dateCourte(instant: string, fuseau: string): string {
  const date = new Date(instant);
  if (Number.isNaN(date.getTime())) return instant;
  const parties: Readonly<Record<string, string>> = Object.fromEntries(
    new Intl.DateTimeFormat("fr-FR", { timeZone: fuseau, day: "2-digit", month: "2-digit", year: "numeric", numberingSystem: "latn" })
      .formatToParts(date)
      .map((partie) => [partie.type, partie.value]),
  );
  return `${parties["day"]}/${parties["month"]}/${parties["year"]}`;
}

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
