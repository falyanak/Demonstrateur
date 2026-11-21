import type { ContextePage, Page } from "../../core/aiguillage.ts";
import { erreurException, type Issue } from "../../core/api.ts";
import type { ConsultationVue, FicheVue, TypeDecision } from "../../core/contrats.ts";
import { FUSEAU } from "../../core/dates.ts";
import { apresTransition, commencer, ETAT_INITIAL, reduire, type EtatDialogue, type Evenement } from "./etat.ts";
import { actions, badgeStatut, blocs, CYCLE_VIDE, DECISIONS, lignesCycle, noteEcartees, peutEnregistrer, progression, titreFiche, type Badge } from "./vue.ts";

// Page Dialogue (F2, DEB-Q48 à DEB-Q53) : COQUILLE sans décision. L'état vient du réducteur (etat.ts), l'affichage du
// modèle (vue.ts) ; ici, seulement le DOM (textContent, jamais innerHTML) et les appels de l'API.
// Une saisie (frappe) ne reconstruit pas la page : elle met l'état à jour et rafraîchit les commandes, le focus reste.
// Panneau « Cycle de vie » (L2) : lignes de vue.ts ; consultation relue après chaque transition (DEB-Q80).

type Attributs = Readonly<Record<string, string>>;

function el<K extends keyof HTMLElementTagNameMap>(balise: K, classe = "", texte?: string, attributs: Attributs = {}, enfants: readonly Node[] = []): HTMLElementTagNameMap[K] {
  const element = document.createElement(balise);
  if (classe !== "") element.className = classe;
  if (texte !== undefined) element.textContent = texte;
  for (const [nom, valeur] of Object.entries(attributs)) element.setAttribute(nom, valeur);
  element.append(...enfants);
  return element;
}

function pastille(badge: Badge, classe = ""): HTMLSpanElement {
  return el("span", `badge text-bg-${badge.couleur} ${classe}`.trim(), badge.texte);
}

function bouton(texte: string, classe: string, action: () => void): HTMLButtonElement {
  const b = el("button", `btn ${classe}`, texte, { type: "button" });
  b.addEventListener("click", action);
  return b;
}

export const page: Page = {
  async monter(racine: HTMLElement, { client }: ContextePage): Promise<void> {
    const id = racine.dataset["fiche"] ?? "";
    let etat: EtatDialogue = ETAT_INITIAL;
    // Commandes à rafraîchir sans reconstruire la page : élément → condition d'activation.
    let commandes: { readonly element: HTMLButtonElement | HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement; readonly actif: (e: EtatDialogue) => boolean }[] = [];

    const appliquer = (evenement: Evenement, reconstruire: boolean) => {
      etat = reduire(etat, evenement);
      if (reconstruire) rendre();
      else rafraichir();
    };

    async function lancer(produire: () => Promise<Evenement>): Promise<void> {
      const depart = commencer(etat);
      if (depart === null) return;
      etat = depart;
      rendre();
      let evenement: Evenement;
      try {
        evenement = await produire();
      } catch (exception) {
        evenement = { type: "refus", erreur: erreurException(exception) };
      }
      appliquer(evenement, true);
    }

    function executer<T>(appel: () => Promise<Issue<T>>, succes: (valeur: T) => Evenement): Promise<void> {
      return lancer(async () => {
        const issue = await appel();
        return issue.ok ? succes(issue.valeur) : { type: "refus", erreur: issue.erreur };
      });
    }

    // Soumission, décision, reprise : les routes qui changent le statut (plan §5). La consultation est relue dans la même
    // attente (DEB-Q80) ; une panne de la relecture n'efface pas l'action réussie (apresTransition).
    function transitionner(appel: () => Promise<Issue<FicheVue>>): Promise<void> {
      return lancer(async () => {
        const action = await appel();
        if (!action.ok) return { type: "refus", erreur: action.erreur };
        const relecture = await client.consulter(id).catch((exception: unknown): Issue<ConsultationVue> => ({ ok: false, statut: 0, erreur: erreurException(exception) }));
        return apresTransition(action.valeur, relecture);
      });
    }

    function rafraichir(): void {
      for (const { element, actif } of commandes) element.disabled = !actif(etat);
    }

    function commande<T extends HTMLButtonElement | HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>(element: T, actif: (e: EtatDialogue) => boolean): T {
      commandes.push({ element, actif });
      return element;
    }

    function rendreConversation(): HTMLElement {
      const fil = el("div", "dialogue-fil border rounded p-2 mb-2", undefined, { "aria-live": "polite" });
      if (etat.conversation.length === 0) fil.append(el("p", "text-body-secondary small mb-0", "Décrivez votre besoin en quelques phrases ; l'assistant vous posera ensuite ses questions."));
      for (const ligne of etat.conversation) {
        const agent = ligne.auteur === "Agent";
        const bulle = el("div", `dialogue-bulle ${agent ? "dialogue-agent" : "dialogue-ia"}${ligne.horsPerimetre ? " border border-warning" : ""}`);
        bulle.append(el("div", "small fw-semibold", agent ? "Agent" : "Assistant"), el("div", "dialogue-texte", ligne.texte));
        if (ligne.horsPerimetre) bulle.append(el("div", "small text-warning-emphasis mt-1", "Demande hors du périmètre des évolutions du SI."));
        const note = noteEcartees(ligne.ecartees);
        if (note !== null) bulle.append(el("div", "small text-body-secondary mt-1", note));
        fil.append(bulle);
      }
      if (etat.attente) fil.append(el("div", "text-body-secondary small", undefined, {}, [el("span", "spinner-border spinner-border-sm me-2", undefined, { "aria-hidden": "true" }), document.createTextNode("Réponse en cours…")]));

      const zone = commande(el("textarea", "form-control", undefined, { rows: "3", "aria-label": "Votre message", placeholder: "Votre message…" }), (e) => actions(e).dialoguer);
      zone.value = etat.message;
      zone.addEventListener("input", () => appliquer({ type: "message", texte: zone.value }, false));
      const envoyer = commande(el("button", "btn btn-primary", "Envoyer", { type: "submit" }), (e) => actions(e).dialoguer);
      const formulaire = el("form", "d-flex gap-2 align-items-end", undefined, {}, [zone, envoyer]);
      const soumettreMessage = () => {
        const texte = etat.message;
        void executer(() => client.envoyerMessage(id, texte), (tour) => ({ type: "tour", envoye: texte, tour }));
      };
      formulaire.addEventListener("submit", (evenement) => {
        evenement.preventDefault();
        soumettreMessage();
      });
      zone.addEventListener("keydown", (evenement) => {
        if (evenement.key === "Enter" && evenement.ctrlKey) {
          evenement.preventDefault();
          soumettreMessage();
        }
      });
      return el("section", "col-lg-5", undefined, {}, [el("h2", "h5", "Dialogue"), fil, formulaire, el("div", "form-text", "Ctrl+Entrée pour envoyer.")]);
    }

    function rendreFiche(): HTMLElement {
      const fiche = etat.fiche;
      const section = el("section", "col-lg-7");
      if (fiche === null) return section;
      const avance = progression(fiche);
      const barre = el("div", "progress flex-grow-1", undefined, { role: "progressbar", "aria-label": "Rubriques obligatoires renseignées", "aria-valuenow": String(avance.pourcent), "aria-valuemin": "0", "aria-valuemax": "100" }, [
        el("div", "progress-bar", undefined, { style: `width: ${avance.pourcent}%` }),
      ]);
      section.append(
        el("div", "d-flex align-items-center gap-2 mb-1", undefined, {}, [el("h2", "h5 mb-0", titreFiche(fiche)), pastille(badgeStatut(fiche.statut)), barre, el("span", "small text-nowrap", avance.texte)]),
        el("p", "small text-body-secondary", `${fiche.enTete.demandeur} — ${fiche.enTete.bureau} ; valideur : ${fiche.enTete.valideur}`),
      );

      for (const bloc of blocs(fiche, etat.brouillons)) {
        section.append(el("h3", "h6 mt-3 border-bottom pb-1", bloc.titre));
        for (const rubrique of bloc.rubriques) {
          const identifiant = `rubrique-${rubrique.code}`;
          const entete = el("div", "d-flex align-items-center gap-2", undefined, {}, [
            el("label", "form-label mb-0 fw-semibold", rubrique.libelle + (rubrique.obligatoire ? " *" : ""), { for: identifiant }),
            pastille(rubrique.badge),
          ]);
          if (rubrique.questionnee) entete.append(el("span", "badge text-bg-light border", "question en cours"));
          const zone = commande(el("textarea", "form-control form-control-sm", undefined, { id: identifiant, rows: "2" }), (e) => actions(e).editer);
          zone.value = rubrique.saisie;
          const enregistrer = commande(
            bouton("Enregistrer", "btn-sm btn-outline-primary", () => {
              const valeur = etat.brouillons[rubrique.code] ?? "";
              void executer(() => client.modifierChamp(id, rubrique.code, valeur), (f) => ({ type: "fiche", fiche: f, enregistree: rubrique.code }));
            }),
            (e) => peutEnregistrer(e, rubrique.code),
          );
          zone.addEventListener("input", () => appliquer({ type: "brouillon", code: rubrique.code, texte: zone.value }, false));
          const champ = el("div", `mb-2${rubrique.questionnee ? " rubrique-questionnee" : ""}`, undefined, {}, [entete, el("div", "d-flex gap-2 align-items-start mt-1", undefined, {}, [zone, enregistrer])]);
          if (rubrique.proposition !== null) {
            champ.append(
              el("div", "proposition border-start border-warning border-3 ps-2 mt-1 small", undefined, {}, [
                el("div", "", undefined, {}, [el("span", "fw-semibold", "Proposition : "), document.createTextNode(rubrique.proposition.valeur)]),
                el("div", "text-body-secondary", rubrique.proposition.justification),
                el("div", "d-flex gap-1 mt-1", undefined, {}, [
                  commande(bouton("✓ Accepter", "btn-sm btn-outline-success", () => void executer(() => client.accepter(id, rubrique.code), (f) => ({ type: "fiche", fiche: f }))), (e) => actions(e).editer),
                  commande(bouton("✗ Rejeter", "btn-sm btn-outline-danger", () => void executer(() => client.rejeter(id, rubrique.code), (f) => ({ type: "fiche", fiche: f }))), (e) => actions(e).editer),
                ]),
              ]),
            );
          }
          section.append(champ);
        }
      }
      section.append(rendreCircuit(), rendreCycle());
      return section;
    }

    function rendreCycle(): HTMLElement {
      const panneau = el("section", "border-top mt-3 pt-3", undefined, { "aria-labelledby": "titre-cycle" }, [el("h3", "h6", "Cycle de vie", { id: "titre-cycle" })]);
      const lignes = lignesCycle(etat.cycle, FUSEAU);
      if (lignes.length === 0) {
        panneau.append(el("p", "small text-body-secondary mb-0", CYCLE_VIDE));
        return panneau;
      }
      const liste = el("ol", "list-group list-group-numbered small");
      for (const ligne of lignes) {
        liste.append(
          el("li", "list-group-item d-flex flex-wrap align-items-center gap-2", undefined, {}, [
            el("span", "fw-semibold", ligne.etape),
            ligne.avant === null ? el("span", "text-body-secondary", "—") : pastille(ligne.avant),
            el("span", "", "→", { "aria-label": "devient" }),
            pastille(ligne.apres),
            el("span", "", ligne.acteur),
            el("span", "text-body-secondary ms-auto text-nowrap", ligne.le),
          ]),
        );
      }
      panneau.append(liste);
      return panneau;
    }

    function rendreCircuit(): HTMLElement {
      const fiche = etat.fiche;
      const pied = el("div", "border-top mt-3 pt-3 d-flex flex-column gap-2");
      if (fiche === null) return pied;
      if (fiche.statut === "Brouillon") {
        const case_ = commande(el("input", "form-check-input", undefined, { type: "checkbox", id: "confirmation" }), (e) => actions(e).editer);
        case_.checked = etat.confirme;
        case_.addEventListener("change", () => appliquer({ type: "confirmation", coche: case_.checked }, false));
        pied.append(
          el("div", "form-check", undefined, {}, [case_, el("label", "form-check-label", "Je confirme que le contenu de la fiche reflète fidèlement mon besoin.", { for: "confirmation" })]),
          el("div", "", undefined, {}, [
            commande(bouton("Soumettre", "btn-primary", () => void transitionner(() => client.soumettre(id, etat.confirme))), (e) => actions(e).soumettre),
          ]),
        );
      }
      if (fiche.statut === "Soumise") {
        const choix = commande(el("select", "form-select form-select-sm w-auto", undefined, { "aria-label": "Décision" }), (e) => actions(e).decider);
        choix.append(...DECISIONS.map((d) => new Option(d.libelle, d.valeur, false, d.valeur === etat.decision)));
        choix.addEventListener("change", () => appliquer({ type: "decision", decision: choix.value as TypeDecision }, false));
        const commentaire = commande(el("textarea", "form-control form-control-sm", undefined, { rows: "2", placeholder: "Commentaire (obligatoire pour des compléments ou un refus)", "aria-label": "Commentaire" }), (e) => actions(e).decider);
        commentaire.value = etat.commentaire;
        commentaire.addEventListener("input", () => appliquer({ type: "commentaire", texte: commentaire.value }, false));
        pied.append(
          el("h3", "h6 mb-0", `Simulation de la décision du chef de service (${fiche.enTete.valideur})`),
          el("div", "d-flex gap-2 align-items-start", undefined, {}, [
            choix,
            commentaire,
            commande(bouton("Décider", "btn-sm btn-primary", () => void transitionner(() => client.decider(id, etat.decision, etat.commentaire))), (e) => actions(e).decider),
          ]),
        );
      }
      const derniere = fiche.decisions[fiche.decisions.length - 1];
      if (derniere !== undefined && derniere.commentaire !== null) pied.append(el("div", "small", `Commentaire de ${derniere.auteur} : ${derniere.commentaire}`));
      if (fiche.statut === "ACompleter") {
        pied.append(el("div", "", undefined, {}, [commande(bouton("Reprendre la fiche", "btn-warning", () => void transitionner(() => client.reprendre(id))), (e) => actions(e).reprendre)]));
      }
      if (actions(etat).telecharger) {
        pied.append(el("div", "", undefined, {}, [el("a", "btn btn-success", "Télécharger la fiche (.docx)", { href: client.urlDocument(id), download: "" })]));
      }
      return pied;
    }

    function rendre(): void {
      commandes = [];
      const erreur = etat.erreur === null ? [] : [el("div", "alert alert-danger py-2", etat.erreur.message, { role: "alert" })];
      if (etat.fiche === null) {
        // Fiche pas encore chargée (ou introuvable) : rien d'autre à montrer que l'attente ou l'erreur.
        const attente = etat.attente ? [el("p", "text-body-secondary", "Chargement de la fiche…")] : [];
        racine.replaceChildren(...erreur, ...attente);
        return;
      }
      const ligne = el("div", "row g-4", undefined, {}, [rendreConversation(), rendreFiche()]);
      racine.replaceChildren(...erreur, ligne);
      rafraichir();
      const fil = racine.querySelector(".dialogue-fil");
      if (fil !== null) fil.scrollTop = fil.scrollHeight;
    }

    rendre();
    await executer(() => client.consulter(id), (consultation) => ({ type: "consultation", consultation }));
  },
};
