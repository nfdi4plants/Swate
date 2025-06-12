import { defineConfig } from 'vite';
import dts from 'vite-plugin-dts'
import react from "@vitejs/plugin-react";
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
    plugins: [
        react({ include: /\.(fs|js|jsx|ts|tsx)$/ },),
        tailwindcss(),
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
                "@tanstack/react-virtual",
                "@nfdi4plants/arctrl"
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
    server: {
        watch: {
            ignored: [ "**/*.fs" ]
        },
    },
    define: {
        'process.env': {},
    },
});