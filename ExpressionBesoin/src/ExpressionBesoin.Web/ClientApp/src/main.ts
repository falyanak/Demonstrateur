import "./styles/main.scss";
import { creerClient } from "./core/api.ts";
import { choisirAgent, lireMemorise, memoriser } from "./core/agentCourant.ts";
import { choisirPage, type ChargeurPage, type ContextePage } from "./core/aiguillage.ts";
import { Url } from "./core/Url.ts";

// Point d'entrée UNIQUE : coquille sans décision (sélecteur d'agent, aiguillage). Tout le reste est testé ailleurs.

const pages: Readonly<Record<string, ChargeurPage>> = {
  accueil: () => import("./features/accueil/PageAccueil.ts").then((module) => module.page),
  dialogue: () => import("./features/dialogue/PageDialogue.ts").then((module) => module.page),
};

const client = creerClient((url, init) => fetch(url, init), (chemin) => Url.resolve(chemin));

function stockage(): Storage | undefined {
  try {
    return globalThis.localStorage;
  } catch {
    return undefined;
  }
}

async function monterSelecteurAgent(): Promise<void> {
  const selecteur = document.getElementById("agent-courant");
  if (!(selecteur instanceof HTMLSelectElement)) return;
  const issue = await client.agents();
  if (!issue.ok) {
    selecteur.title = issue.erreur.message;
    return;
  }
  const retenu = choisirAgent(issue.valeur, lireMemorise(stockage()));
  selecteur.replaceChildren(...issue.valeur.map((agent) => new Option(`${agent.nom} — ${agent.bureau}`, agent.identifiant, false, agent.identifiant === retenu)));
  selecteur.disabled = issue.valeur.length === 0;
  selecteur.addEventListener("change", () => memoriser(stockage(), selecteur.value));
  if (retenu !== null) memoriser(stockage(), retenu);
}

const contexte: ContextePage = {
  client,
  agent: () => {
    const selecteur = document.getElementById("agent-courant");
    return selecteur instanceof HTMLSelectElement && selecteur.value !== "" ? selecteur.value : null;
  },
  aller: (chemin) => globalThis.location.assign(Url.resolve(chemin)),
};

async function monterPage(): Promise<void> {
  const racine = document.getElementById("app");
  if (racine === null) return;
  const charger = choisirPage(pages, racine.dataset["page"]);
  if (charger !== null) await (await charger()).monter(racine, contexte);
}

void Promise.all([monterSelecteurAgent(), monterPage()]);
