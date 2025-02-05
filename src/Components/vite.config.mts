import { defineConfig } from 'vite';
import dts from 'vite-plugin-dts'
import react from "@vitejs/plugin-react";
import tailwindcss from "tailwindcss";

export default defineConfig({
    plugins: [
        react(),
        dts({
            include: ['src'],
            tsconfigPath: 'tsconfig.json',
        })
    ],
    esbuild: {
        jsx: 'automatic', // Enables React 17+ JSX Transform
    },
    build: {
        lib: {
            entry: './src/index.js', // Entry file for your library
            name: "@nfdi4plants/swate-components",
            fileName: (format) => `index.${format}.js`,
        },
        rollupOptions: {
            // Exclude peer dependencies from the final bundle
            external: ['react', 'react-dom', 'tailwindcss', '@fable-org/fable-library-js'],
            output: {
                globals: {
                    react: 'React',
                    'react-dom': 'ReactDOM',
                    tailwindcss: "tailwindcss",
                },
            },
        },
    },
    css: {
        postcss: {
          plugins: [tailwindcss],
        },
      },
  });