import { defineConfig, mergeConfig } from 'vitest/config';
import { playwright } from '@vitest/browser-playwright';

import { storybookTest } from '@storybook/addon-vitest/vitest-plugin';

import path from 'node:path';
import { fileURLToPath } from 'node:url';

const dirname = path.dirname(fileURLToPath(import.meta.url));

import viteConfig from './vite.config.mts';

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      projects: [
        {
          extends: true,
          plugins: [
            storybookTest({
              // The location of your Storybook config, main.js|ts
              configDir: path.join(dirname, '.storybook'),
              // // This should match your package.json script to run Storybook
              // // The --no-open flag will skip the automatic opening of a browser
              // storybookScript: 'npm run storybook:no-compile --no-open',
            }),
          ],
          test: {
            name: 'storybook',
            // Enable browser mode
            browser: {
              enabled: true,
              // Make sure to install Playwright
              provider: playwright({}),
              headless: true,
              instances: [{ browser: 'chromium' }],
            },
            setupFiles: ['./.storybook/vitest.setup.ts'],
          },
        },
      ],
    },
  }),
);

// import { defineWorkspace } from 'vitest/config';
// import { storybookTest } from '@storybook/addon-vitest/vitest-plugin';
// import path from 'node:path';
// import { fileURLToPath } from 'node:url';

// const dirname = typeof __dirname !== 'undefined'
//   ? __dirname
//   : path.dirname(fileURLToPath(import.meta.url));

// // More info at: https://storybook.js.org/docs/writing-tests/test-addon
// export default defineWorkspace([
//   'vite.config.mts',
//   {
//     extends: 'vite.config.mts',
//     plugins: [
//       // The plugin will run tests for the stories defined in your Storybook config
//       // See options at: https://storybook.js.org/docs/writing-tests/test-addon#storybooktest
//       storybookTest({ configDir: path.join(dirname, '.storybook') })
//     ],
//     test: {
//       name: 'storybook',
//       browser: {
//         enabled: true,
//         headless: true,
//         name: 'chromium',
//         provider: 'playwright',
//       },
//       setupFiles: ['.storybook/vitest.setup.ts'],
//     },
//   },
// ]);