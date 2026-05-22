import path from 'node:path';
import { expect, test } from '@playwright/test';
import {
  callArcVaultApi,
  connectToElectronRenderer,
  expectCreatedArcVisible,
  getArcVaultFileTreeKeys,
  readE2eMetadata,
} from './e2e-utils';

test.describe('Swate Electron renderer integration', () => {
  test('creates an ARC through the real UI and hydrates renderer state from IPC', async () => {
    const metadata = readE2eMetadata();
    const arcIdentifier = `playwright-test-arc-${Date.now()}`;
    const expectedArcPath = path.join(metadata.tempArcParentDir, arcIdentifier);
    const { browser, page } = await connectToElectronRenderer(metadata);

    try {
      await expect(page.getByRole('button', { name: /New ARC/ })).toBeVisible({ timeout: 60000 });
      await page.getByRole('button', { name: /New ARC/ }).click();

      await expect(page.getByText('ARC Identifier')).toBeVisible({ timeout: 30000 });
      await page.locator('input[required]').fill(arcIdentifier);
      await page.getByRole('button', { name: 'Create new ARC' }).click();

      await expectCreatedArcVisible(page, arcIdentifier, expectedArcPath);

      const openPath = await callArcVaultApi<string | undefined>(page, 'getOpenPath');
      expect(openPath).toBe(expectedArcPath);

      const fileTreeKeys = await getArcVaultFileTreeKeys(page);
      expect(fileTreeKeys.length).toBeGreaterThan(0);
    } finally {
      await browser.close();
    }
  });
});
