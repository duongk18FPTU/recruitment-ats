import { defineConfig } from "vitest/config";

// Each app has its own test settings in its vite.config.ts; packages with tests have a vitest.config.ts.
export default defineConfig({
  test: {
    projects: ["apps/*", "packages/api-client"],
  },
});
