import type { Page } from "../../core/aiguillage.ts";

// Page d'accueil (F1) : rien à monter de plus que le sélecteur d'agent de l'en-tête ; Dialogue (F2), Liste (F3).
export const page: Page = {
  monter(racine: HTMLElement): void {
    racine.dataset["monte"] = "oui";
  },
};
