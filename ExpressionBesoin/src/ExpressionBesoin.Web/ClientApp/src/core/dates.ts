// Dates affichées (F3, L2 ; DEB-Q62, DEB-Q82) : un seul endroit, commun aux pages Liste et Dialogue. PUR : le fuseau
// est un paramètre, jamais lu sur le poste ; chiffres latins quelle que soit la langue du navigateur.

/** Fuseau des dates affichées : celui du numéro et du .docx (DEB-Q24, DEB-Q36, DEB-Q62). */
export const FUSEAU = "Europe/Paris";

type Parties = Readonly<Record<string, string>>;

/** Parties de la date dans le fuseau ; `null` si le texte n'est pas une date. */
function parties(instant: string, fuseau: string, heure: boolean): Parties | null {
  const date = new Date(instant);
  if (Number.isNaN(date.getTime())) return null;
  const options: Intl.DateTimeFormatOptions = { timeZone: fuseau, day: "2-digit", month: "2-digit", year: "numeric", numberingSystem: "latn" };
  const format = heure ? { ...options, hour: "2-digit", minute: "2-digit", hourCycle: "h23" } satisfies Intl.DateTimeFormatOptions : options;
  return Object.fromEntries(new Intl.DateTimeFormat("fr-FR", format).formatToParts(date).map((partie) => [partie.type, partie.value]));
}

/** Instant ISO 8601 → jj/mm/aaaa dans le fuseau ; texte non daté rendu tel quel (jamais deviné, jamais masqué). Pure. */
export function dateCourte(instant: string, fuseau: string): string {
  const p = parties(instant, fuseau, false);
  return p === null ? instant : `${p["day"]}/${p["month"]}/${p["year"]}`;
}

/** Instant ISO 8601 → jj/mm/aaaa hh:mm (24 h) dans le fuseau ; texte non daté rendu tel quel. Pure. */
export function dateHeure(instant: string, fuseau: string): string {
  const p = parties(instant, fuseau, true);
  return p === null ? instant : `${p["day"]}/${p["month"]}/${p["year"]} ${p["hour"]}:${p["minute"]}`;
}
