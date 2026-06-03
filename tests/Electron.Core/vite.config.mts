import { defineConfig } from "vitest/config";
import { fileURLToPath } from "node:url";

const electronMockPath = fileURLToPath(new URL("./electron.mock.mts", import.meta.url));

export default defineConfig({
    resolve: {
        alias: {
            electron: electronMockPath,
        },
    },
    test: {
        environment: "node",
        include: ["output/**/*.test.js"],
        testTimeout: 120000,
        hookTimeout: 120000,
    },
});
