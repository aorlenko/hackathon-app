import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/market-api": {
        target: "http://localhost:7001",
        changeOrigin: true,
        ws: true,
        rewrite: (path) => path.replace(/^\/market-api/, ""),
      },
      "/trade-api": {
        target: "http://localhost:7002",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/trade-api/, ""),
      },
      "/settlement-api": {
        target: "http://localhost:7003",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/settlement-api/, ""),
      },
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: "./src/vitest.setup.ts",
    globals: true,
  },
});
