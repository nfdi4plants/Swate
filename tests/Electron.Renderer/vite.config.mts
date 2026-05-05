import { defineConfig } from "vitest/config";

export default defineConfig({
    test: {
        environment: "jsdom",
        setupFiles: ["./vitest.setup.mts"],
        include: ["output/**/*.test.js"],
    },
});
