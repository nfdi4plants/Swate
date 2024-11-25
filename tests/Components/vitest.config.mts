// vite.config.ts
import { defineConfig } from 'vite'
import react from "@vitejs/plugin-react";

export default defineConfig({
    plugins: [react()],
    esbuild: {
        jsx: 'automatic', // Enables React 17+ JSX Transform
    },
    test: {
      globals: true,
      environment: "jsdom",
      setupFiles: './vitest.setup.ts', // Loads the setup file
    },
  });