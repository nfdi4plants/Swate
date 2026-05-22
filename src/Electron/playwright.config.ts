import { defineConfig } from 'playwright/test';

const helperPort = Number(process.env.SWATE_ELECTRON_E2E_HELPER_PORT ?? 39001);

export default defineConfig({
  timeout: 120000,
  testDir: './src/tests',
  outputDir: './src/tests/test-results',
  workers: 1,
  reporter: [['list']],
  use: {
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
  },
  webServer: {
    command: 'npm run test:e2e:server',
    url: `http://127.0.0.1:${helperPort}/health`,
    timeout: 300000,
    reuseExistingServer: false,
  },
});
