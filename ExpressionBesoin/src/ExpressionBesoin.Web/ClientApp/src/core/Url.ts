// Toute URL applicative passe par ici (gabarit F8) : la base vient du PathBase d'ASP.NET (« / » à la racine,
// « /monsite/ » en sous-application), posée par le layout sur #maplayout (data-url-home-link).

/** Joint une base (finie ou non par « / ») et un chemin applicatif (commençant ou non par « / »). Pure. */
export function joindre(base: string, chemin: string): string {
  const racine = base.endsWith("/") ? base : `${base}/`;
  const relatif = chemin.startsWith("/") ? chemin.slice(1) : chemin;
  return racine + relatif;
}

/** Base lue dans le document ; « / » si l'élément ou l'attribut manque. */
export function lireBase(document: Pick<Document, "getElementById">): string {
  return document.getElementById("maplayout")?.dataset["urlHomeLink"] || "/";
}

export const Url = {
  resolve(chemin: string, document: Pick<Document, "getElementById"> = globalThis.document): string {
    return joindre(lireBase(document), chemin);
  },
};
