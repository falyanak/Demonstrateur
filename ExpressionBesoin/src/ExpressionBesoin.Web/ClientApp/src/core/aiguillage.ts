// main.ts ne fait que l'aiguillage (gabarit F2) : chaque vue déclare sa page par <div id="app" data-page="…">, le
// module de la page est chargé à la demande (un fichier par page).

import type { ClientApi } from "./api.ts";

/** Effets fournis par main.ts à chaque page (injectés : une page ne compose rien). */
export interface ContextePage {
  readonly client: ClientApi;
  /** Agent courant du sélecteur de l'en-tête ; `null` tant qu'aucun n'est choisi. */
  agent(): string | null;
  /** Navigation vers un chemin applicatif (résolu par Url.resolve). */
  aller(chemin: string): void;
  /** Adresse d'un chemin applicatif (Url.resolve), pour un lien (F3). */
  adresse(chemin: string): string;
}

export interface Page {
  monter(racine: HTMLElement, contexte: ContextePage): void | Promise<void>;
}

export type ChargeurPage = () => Promise<Page>;

/** Chargeur de la page nommée ; `null` pour une vue sans page ou une page inconnue. Pure. */
export function choisirPage(pages: Readonly<Record<string, ChargeurPage>>, nom: string | undefined): ChargeurPage | null {
  // hasOwn : « toString », « __proto__ »… du prototype ne sont pas des pages.
  return nom !== undefined && Object.hasOwn(pages, nom) ? (pages[nom] as ChargeurPage) : null;
}
