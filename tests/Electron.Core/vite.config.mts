import { configDefaults, defineConfig } from "vitest/config";
import { fileURLToPath } from "node:url";

export default defineConfig({
    resolve: {
        alias: {
            electron: fileURLToPath(new URL("./electron.mock.mts", import.meta.url)),
        },
    },
    test: {
        environment: "node",
        include: ["output/**/*.test.js", "ArcVaultHelper.wrapper.test.mts"],
        exclude: [
            ...configDefaults.exclude,
            "output/ArcVaultHelper.test.js",
        ],
        testTimeout: 120000,
        hookTimeout: 120000,
    },
});
