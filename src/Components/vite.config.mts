import {defineConfig} from 'vite';
import dts from 'vite-plugin-dts'
import react from "@vitejs/plugin-react";
import tailwindcss from '@tailwindcss/vite'
import pkg from './package.json';

export default defineConfig({
    plugins: [
        react({
            include: /\.(js|jsx|ts|tsx)$/,
            babel: {
                plugins: ['babel-plugin-react-compiler'],
            },
        }),
        tailwindcss(),
        dts({
            include: ['src'],
            tsconfigPath: 'tsconfig.json',
        })
    ],
    esbuild: {
        jsx: 'automatic', // Enables React 17+ JSX Transform
    },
    optimizeDeps: {
        // Avoid runtime re-optimization reloads during Vitest browser runs in CI.
        include: ["react-dom/client"],
    },
    build: { // changes to the build config below should be well tested. See README.md for more information.
    // README.md: local dev -> release -> npm -> background info
        minify: false,
        sourcemap: true,
        lib: {
            entry: './src/index.ts',
            formats: ['es'],
        },
        rollupOptions: {
            external: (id) => {
                const externalPkgs = [
                    ...Object.keys(pkg.dependencies ?? {}),
                    ...Object.keys(pkg.peerDependencies ?? {}),
                ];
                return externalPkgs.some(p => id === p || id.startsWith(`${p}/`));
            },
            output: {
                preserveModules: true,
                preserveModulesRoot: 'src',
                entryFileNames: '[name].js',
                chunkFileNames: 'chunks/[name].js',
                assetFileNames: 'assets/[name][extname]',
            },
        },
    },
    server: {
        watch: {
            ignored: ["**/*.fs"]
        },
    },
    define: {
        'process.env': {},
    },
});
