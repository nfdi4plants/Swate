import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import basicSsl from '@vitejs/plugin-basic-ssl'
import tailwindcss from '@tailwindcss/vite'
// import devCerts from 'office-addin-dev-certs';


const proxyPort = process.env.SERVER_PROXY_PORT || "5000";
const proxyTarget = "http://localhost:" + proxyPort;

// const httpsOptions = await devCerts.getHttpsServerOptions();
// https://vitejs.dev/config/
export default defineConfig({
    plugins: [
        basicSsl(),
        react({ include: /\.(fs|js|jsx|ts|tsx)$/ },),
        tailwindcss(),
    ],
    esbuild: {
        jsx: 'automatic', // Enables React 17+ JSX Transform
    },
    build: {
        outDir: "../../deploy/public",
    },
    server: {
        // https: {
        //     key: httpsOptions.key,
        //     cert: httpsOptions.cert,
        // },
        port: 3000,
        proxy: {
            // redirect requests that start with /api/ to the server on port 5000
            "/api/": {
                target: proxyTarget,
                changeOrigin: true,
            },
            //// redirect websocket requests that start with /socket/ to the server on the port 5000
            //"/socket/": {
            //    target: proxyTarget,
            //    ws: true,
            //},
        },
        watch: {
            ignored: ["**/*.fs"]
        },
    },
});