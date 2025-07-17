import type { StorybookConfig } from "@storybook/react-vite";
import { resolve } from 'path';

const config: StorybookConfig = {
  stories: ["../src/**/*.mdx", "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)"],
  addons: [
    "@storybook/addon-onboarding",
    "@chromatic-com/storybook",
    "@storybook/addon-vitest",
    '@storybook/addon-themes',
    '@storybook/addon-docs'
  ],
  framework: {
    name: "@storybook/react-vite",
    options: {},
  },
  viteFinal: async (config) => {
    config.optimizeDeps = {
      ...(config.optimizeDeps || {}),
      exclude: ['fs', 'path'], // Optional but good practice
    };

    config.resolve = {
      ...config.resolve,
      alias: {
        ...(config.resolve?.alias || {}),
        'fs/promises': resolve(__dirname, 'mocks/fs-promises-mock.js'),
        fs: resolve(__dirname, 'mocks/fs-mock.js'),
        path: resolve(__dirname, 'mocks/path-mock.js'),
      },
    };

    return config;
  },

};
export default config;
