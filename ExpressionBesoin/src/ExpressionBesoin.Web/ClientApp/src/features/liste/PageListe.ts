import type { ContextePage, Page } from "../../core/aiguillage.ts";
import { erreurException } from "../../core/api.ts";
import { cheminDialogue } from "../../core/chemins.ts";
import { AUCUNE_FICHE, FUSEAU, lignes, type LigneListe } from "./vue.ts";

// Page Liste, à l'accueil (F3, DEB-Q59) : COQUILLE sans décision. Les lignes viennent de vue.ts ; ici, seulement le DOM
// (textContent, jamais innerHTML) et les appels de l'API. « Nouvelle fiche » crée un brouillon pour l'agent courant puis
// ouvre sa page Dialogue (DEB-Q48). La liste se recharge à chaque affichage, retour arrière compris (DEB-Q63).

function el<K extends keyof HTMLElementTagNameMap>(balise: K, classe = "", texte?: string, enfants: readonly Node[] = []): HTMLElementTagNameMap[K] {
  const element = document.createElement(balise);
  if (classe !== "") element.className = classe;
  if (texte !== undefined) element.textContent = texte;
  element.append(...enfants);
  return element;
}

function lien(texte: string, adresse: string, classe: string): HTMLAnchorElement {
  const a = el("a", `btn btn-sm ${classe}`, texte);
  a.href = adresse;
  return a;
}

const ENTETES = ["N°", "Intitulé", "Demandeur", "Créée le", "Statut", "Actions"] as const;

function tableau(liste: readonly LigneListe[], adresse: (chemin: string) => string, document: (id: string) => string): HTMLTableElement {
  const entete = el("thead", "", undefined, [el("tr", "", undefined, ENTETES.map((titre) => el("th", "", titre)))]);
  for (const th of entete.querySelectorAll("th")) th.scope = "col";
  const corps = el("tbody", "", undefined, liste.map((ligne) => {
    const actions = [lien("Ouvrir", adresse(ligne.ouvrir), "btn-outline-primary")];
    if (ligne.telecharger) actions.push(lien("Télécharger", document(ligne.id), "btn-outline-success"));
    return el("tr", "", undefined, [
      el("td", "text-nowrap", ligne.numero),
      el("td", "", ligne.intitule),
      el("td", "", ligne.demandeur),
      el("td", "text-nowrap", ligne.creeeLe),
      el("td", "", undefined, [el("span", `badge text-bg-${ligne.badge.couleur}`, ligne.badge.texte)]),
      el("td", "text-nowrap", undefined, [el("div", "d-flex gap-1", undefined, actions)]),
    ]);
  }));
  return el("table", "table table-sm table-hover align-middle", undefined, [entete, corps]);
}

export const page: Page = {
  monter(racine: HTMLElement, { client, agent, aller, adresse }: ContextePage): void {
    racine.dataset["monte"] = "oui";
    const bouton = racine.querySelector<HTMLButtonElement>("#nouvelle-fiche");
    const message = racine.querySelector<HTMLElement>("#liste-erreur");
    const zone = racine.querySelector<HTMLElement>("#liste");
    if (bouton === null || message === null || zone === null) return;

    // Seule la DERNIÈRE demande s'affiche : une réponse lente arrivée après une plus récente est ignorée.
    let demande = 0;
    const charger = async () => {
      const numero = ++demande;
      zone.replaceChildren(el("p", "text-body-secondary", "Chargement…"));
      try {
        const issue = await client.lister();
        if (numero !== demande) return;
        if (!issue.ok) {
          zone.replaceChildren(el("p", "text-danger", issue.erreur.message));
          return;
        }
        const liste = lignes(issue.valeur, FUSEAU);
        zone.replaceChildren(liste.length === 0 ? el("p", "text-body-secondary", AUCUNE_FICHE) : tableau(liste, adresse, (id) => client.urlDocument(id)));
      } catch (exception) {
        if (numero === demande) zone.replaceChildren(el("p", "text-danger", erreurException(exception).message));
      }
    };

    // Page restituée par le cache du navigateur (retour arrière) : la liste peut avoir changé depuis.
    globalThis.addEventListener("pageshow", (evenement) => {
      if (evenement.persisted) void charger();
    });
    void charger();

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
