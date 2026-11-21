import { defineConfig } from "vite";

// Gabarit front du harnais (regles/gabarit-frontend-vite-aspnet.md, F6), réduit aux bibliothèques retenues (DEB-Q39).
export default defineConfig(({ command }) => ({
  // Relatif au build : le même dist sert à la racine comme en sous-application IIS ; absolu en développement.
  base: command === "build" ? "./" : "/",
  build: {
    outDir: "../wwwroot/dist",
    emptyOutDir: true,
    // Lu par ViteScriptsTagHelper (noms avec empreinte).
    manifest: true,
    rollupOptions: {
      input: "src/main.ts",
      output: {
        // Bootstrap dans son propre fichier : cache du navigateur indépendant du code applicatif.
        manualChunks(id: string) {
          return id.includes("node_modules") ? "vendor" : undefined;
        },
      },
    },
  },
  css: {
    preprocessorOptions: {
      scss: {
        // Avertissements Sass propres à Bootstrap 5.3, tus par catégorie (jamais globalement).
        silenceDeprecations: ["import", "global-builtin", "color-functions", "if-function"],
      },
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    // Polices et images importées par le CSS servies par Vite, pas par la page en 5080 (gabarit F10).
    origin: "http://localhost:5173",
    proxy: {
      // Page ouverte directement en 5173 : l'API paraît de même origine (port du profil Localhost).
      "/api": { target: "http://localhost:5080", changeOrigin: false },
    },
  },
}));
