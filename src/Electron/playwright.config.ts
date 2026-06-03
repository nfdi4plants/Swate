import { defineConfig } from 'playwright/test';

// Playwright launches Electron with Node/Chromium debugging flags. If this leaks
// in from a parent shell, Electron runs as Node and rejects those flags.
delete process.env.ELECTRON_RUN_AS_NODE;

export default defineConfig({
  timeout: 120000,
  testDir: './src/tests',
  outputDir: './src/tests/test-results',

  // reporter: [
  //   ['list'],
  //   ['html', { outputFolder: './src/tests/playwright-report', open: 'always' }]
  // ],
  use: {
    headless: false,
    video: 'retain-on-failure',
  },
});
