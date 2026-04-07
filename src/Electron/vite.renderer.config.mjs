import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// https://vitejs.dev/config
export default defineConfig({
    plugins: [
        react({
            include: /\.(js|jsx|ts|tsx)$/,
        }),
        tailwindcss()
    ],
    server: {
        watch: {
            // Ignore raw F# source and non-renderer generated outputs to avoid unnecessary full reloads.
            ignored: (watchPath) => {
                const p = watchPath.replace(/\\/g, '/');

                const isFSharpSource =
                    p.endsWith('.fs') ||
                    p.endsWith('.fsx') ||
                    p.endsWith('.fsi') ||
                    p.endsWith('.fsproj');

                const isMainOrPreloadOutput =
                    p.includes('/src/fable_output/Main/') ||
                    p.includes('/src/fable_output/Preload/');

                return isFSharpSource || isMainOrPreloadOutput;
            },
            awaitWriteFinish: {
                stabilityThreshold: 150,
                pollInterval: 25,
            },
        },
    }
});