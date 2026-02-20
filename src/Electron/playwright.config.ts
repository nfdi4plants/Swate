import { defineConfig } from '@playwright/test';

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
