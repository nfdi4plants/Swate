import { defineConfig } from "vite";
import { nodePolyfills } from 'vite-plugin-node-polyfills'

const proxyPort = process.env.SERVER_PROXY_PORT || "5000";
const proxyTarget = "http://localhost:" + proxyPort;

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [
        nodePolyfills(),
      ],
    server: {
        port: 8081
    }
});