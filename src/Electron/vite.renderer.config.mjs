import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// https://vitejs.dev/config
export default defineConfig({
    plugins: [
        react({
            babel: {
                // plugins: ['babel-plugin-react-compiler'],
            }
        }),
        tailwindcss()
    ],
    server: {
        watch: {
            ignored: ["./src/**/*.fs", "./src/**/*.fsproj"]
        },
    }
});