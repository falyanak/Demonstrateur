import { defineConfig } from "vitest/config";

// Prime sur vite.config.ts pour les tests (gabarit F7) : environnement Node, globales du navigateur simulées.
export default defineConfig({
  test: {
    environment: "node",
    globals: true,
    include: ["tests/**/*.{test,spec}.ts"],
    coverage: {
      provider: "v8",
      include: ["src/**/*.ts"],
      exclude: ["src/main.ts", "src/**/*.d.ts", "src/features/**"],
      reporter: ["text", "html"],
    },
  },
});
