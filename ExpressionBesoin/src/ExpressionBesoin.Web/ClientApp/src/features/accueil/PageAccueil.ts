import type { ContextePage, Page } from "../../core/aiguillage.ts";
import { erreurException } from "../../core/api.ts";
import { cheminDialogue } from "../../core/chemins.ts";

// Page d'accueil (F1, F2) : COQUILLE. « Nouvelle fiche » crée un brouillon pour l'agent courant puis ouvre sa page
// Dialogue (DEB-Q48). La Liste arrive au lot F3.
export const page: Page = {
  monter(racine: HTMLElement, { client, agent, aller }: ContextePage): void {
    racine.dataset["monte"] = "oui";
    const bouton = racine.querySelector<HTMLButtonElement>("#nouvelle-fiche");
    const message = racine.querySelector<HTMLElement>("#accueil-erreur");
    if (bouton === null || message === null) return;
    bouton.addEventListener("click", async () => {
      const courant = agent();
      if (courant === null) {
        message.textContent = "Choisissez d'abord un agent dans l'en-tête.";
        return;
      }
      bouton.disabled = true;
      message.textContent = "";
      try {
        const issue = await client.creerFiche(courant);
        if (issue.ok) {
          aller(cheminDialogue(issue.valeur.id));
          return;
        }
        message.textContent = issue.erreur.message;
      } catch (exception) {
        message.textContent = erreurException(exception).message;
      }
      bouton.disabled = false;
    });
  },
};
