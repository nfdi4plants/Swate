import fs from 'node:fs';
import path from 'node:path';
import { expect, test } from '@playwright/test';
import { readE2eMetadata } from './e2e-utils';

const generatedEntrypoints = [
  'Main/main.fs.jsx',
  'Preload/preload.fs.jsx',
  'Renderer/Program.fs.jsx',
  'Renderer/Api.fs.jsx',
  'Swate.Electron.Shared/IPCTypes.fs.jsx',
];

const tokenChecks = [
  {
    file: 'Preload/preload.fs.jsx',
    tokens: ['IArcVaultsApi_$reflection'],
  },
  {
    file: 'Renderer/Api.fs.jsx',
    tokens: ['ipcArcVaultApi', 'IArcVaultsApi_$reflection'],
  },
  {
    file: 'Swate.Electron.Shared/IPCTypes.fs.jsx',
    tokens: ['IArcVaultsApi', 'createARC', 'getOpenPath', 'getFileTree', 'saveArcFile', 'writeFile'],
  },
];

test.describe('Fable Electron output', () => {
  test('generates the Electron entrypoints used by Forge', () => {
    const metadata = readE2eMetadata();
    const outputRoot = path.resolve('src/fable_output');

    for (const relativePath of generatedEntrypoints) {
      const filePath = path.join(outputRoot, relativePath);
      expect(fs.existsSync(filePath), `${relativePath} should exist`).toBe(true);

      const stats = fs.statSync(filePath);
      expect(stats.mtimeMs, `${relativePath} should be newer than the E2E Fable build start`).toBeGreaterThanOrEqual(
        metadata.fableBuildStartMs - 1000,
      );
    }
  });

  test('emits the ARC vault bridge/client API shape', () => {
    const outputRoot = path.resolve('src/fable_output');

    for (const check of tokenChecks) {
      const fileContent = fs.readFileSync(path.join(outputRoot, check.file), 'utf8');

      for (const token of check.tokens) {
        expect(fileContent, `${check.file} should contain ${token}`).toContain(token);
      }
    }
  });
});
