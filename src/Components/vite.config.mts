import { defineConfig } from 'vite';
import dts from 'vite-plugin-dts'
import react from "@vitejs/plugin-react";
import tailwindcss from "tailwindcss";

export default defineConfig({
    plugins: [
        react({ include: /\.(fs|js|jsx|ts|tsx)$/, jsxRuntime: "classic" },),
        dts({
            include: ['src'],
            tsconfigPath: 'tsconfig.json',
        })
    ],
    esbuild: {
        jsx: 'automatic', // Enables React 17+ JSX Transform
    },
    build: {
        sourcemap: true,
        lib: {
            entry: './src/index.js', // Entry file
            name: "@nfdi4plants/swate-components",
            formats: ['es', 'cjs'],
            fileName: (format) => `index.${format}.js`,
        },
        rollupOptions: {
            // Exclude peer dependencies from the final bundle
            external: [
                'react',
                'react-dom',
                'tailwindcss',
                '@fable-org/fable-library-js',
                "@floating-ui/react",
                "@fortawesome/fontawesome-free",
                "@tanstack/react-virtual"
            ],
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