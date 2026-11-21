import { describe, expect, it } from "vitest";
import { joindre, lireBase, Url } from "../src/core/Url.ts";

function documentAvec(base: string | undefined): Pick<Document, "getElementById"> {
  return {
    getElementById: (id: string) =>
      id === "maplayout" && base !== undefined ? ({ dataset: { urlHomeLink: base } } as unknown as HTMLElement) : null,
  };
}

describe("joindre", () => {
  it.each([
    ["/", "api/fiches", "/api/fiches"],
    ["/", "/api/fiches", "/api/fiches"],
    ["/monsite/", "api/fiches", "/monsite/api/fiches"],
    ["/monsite", "api/fiches", "/monsite/api/fiches"],
    ["/monsite", "/api/fiches", "/monsite/api/fiches"],
    ["/monsite/", "", "/monsite/"],
  ])("%s + %s = %s", (base, chemin, attendu) => {
    expect(joindre(base, chemin)).toBe(attendu);
  });
});

describe("lireBase", () => {
  it("lit data-url-home-link", () => expect(lireBase(documentAvec("/monsite/"))).toBe("/monsite/"));
  it("« / » sans élément", () => expect(lireBase(documentAvec(undefined))).toBe("/"));
  it("« / » si l'attribut est vide", () => expect(lireBase(documentAvec(""))).toBe("/"));
});

describe("Url.resolve", () => {
  it("résout contre la base du document fourni", () => {
    expect(Url.resolve("api/agents", documentAvec("/sous/"))).toBe("/sous/api/agents");
  });

  it("lit le document global par défaut", () => {
    vi.stubGlobal("document", documentAvec("/global/"));
    try {
      expect(Url.resolve("api/agents")).toBe("/global/api/agents");
    } finally {
      vi.unstubAllGlobals();
    }
  });
});
