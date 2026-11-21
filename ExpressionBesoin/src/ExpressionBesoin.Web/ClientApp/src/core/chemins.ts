// Chemins applicatifs des pages Razor (DEB-Q48), à résoudre par Url.resolve : jamais d'URL écrite en dur.

/** Page Dialogue d'une fiche : action `Home/Dialogue/{id}`. Pure. */
export function cheminDialogue(id: string): string {
  return `Home/Dialogue/${encodeURIComponent(id)}`;
}
