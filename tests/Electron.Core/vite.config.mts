import { defineConfig } from "vitest/config";
import { fileURLToPath } from "node:url";

export default defineConfig({
    resolve: {
        alias: {
            electron: fileURLToPath(new URL("./electron.mock.mts", import.meta.url)),
        },
    },
    test: {
        environment: "node",
        include: ["output/**/*.test.js"],
        testTimeout: 120000,
        hookTimeout: 120000,
    },
});
