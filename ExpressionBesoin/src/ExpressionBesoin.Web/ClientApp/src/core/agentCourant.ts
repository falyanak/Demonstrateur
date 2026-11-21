import type { AgentVue } from "./contrats.ts";

// Agent courant du sélecteur de l'en-tête (DEB-Q6) : identité de démonstration, jamais une autorisation.

export const CLE_AGENT = "expressionBesoin.agent";

/** Agent retenu : celui mémorisé s'il existe encore, sinon le premier ; `null` sans agent. Pure. */
export function choisirAgent(agents: readonly AgentVue[], memorise: string | null): string | null {
  if (memorise !== null && agents.some((agent) => agent.identifiant === memorise)) return memorise;
  return agents[0]?.identifiant ?? null;
}

/** Lecture tolérante du stockage du navigateur : indisponible (navigation privée, politique) → `null`. */
export function lireMemorise(stockage: Pick<Storage, "getItem"> | undefined): string | null {
  try {
    return stockage?.getItem(CLE_AGENT) ?? null;
  } catch {
    return null;
  }
}

/** Écriture tolérante : un stockage indisponible ne fait pas échouer la page. */
export function memoriser(stockage: Pick<Storage, "setItem"> | undefined, identifiant: string): void {
  try {
    stockage?.setItem(CLE_AGENT, identifiant);
  } catch {
    // Préférence de confort seulement : rien à faire.
  }
}
