import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
    plugins: [
        react()
    ],
    esbuild: {
        jsx: 'automatic', // Enables React 17+ JSX Transform
    },
    build: {
        lib: {
            entry: './src/index.ts', // Entry file for your library
            name: 'swate-components', // Global name for UMD builds
            fileName: (format) => `swate-components.${format}.js`,
        },
        rollupOptions: {
            // Exclude peer dependencies from the final bundle
            external: ['react', 'react-dom'],
            output: {
            globals: {
                react: 'React',
                'react-dom': 'ReactDOM',
            },
            },
        },
    },
    test: {
      css: true,
      globals: true,
      environment: "jsdom",
      setupFiles: './vitest.setup.ts', // Loads the setup file
    },
  });