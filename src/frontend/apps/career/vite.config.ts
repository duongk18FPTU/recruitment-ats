import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [react()],
  // Same origin as in production: the dev server forwards /api to the API (AD-05).
  server: { port: 5174, strictPort: true, proxy: { "/api": "http://localhost:5080" } },
  test: { environment: "jsdom" },
});
