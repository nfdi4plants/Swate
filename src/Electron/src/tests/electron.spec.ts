import { test, expect, _electron as electron, ElectronApplication, Page } from 'playwright/test';
import fs from 'fs';
import os from 'os';
import path from 'path';

const ARC_VAULT_API_NAME = 'FABLE_REMOTING_IArcVaultsApi';
const ARC_IDENTIFIER = 'playwright-test-arc';

async function waitForArcVaultApi(window: Page) {
  await window.waitForFunction((apiName) => typeof (window as any)[apiName] !== 'undefined', ARC_VAULT_API_NAME);
}

async function mockOpenDialogSelection(electronApp: ElectronApplication, selectedPath: string) {
  await electronApp.evaluate(({ dialog }, targetPath) => {
    dialog.showOpenDialog = async () => ({
      canceled: false,
      filePaths: [targetPath],
    });
  }, selectedPath);
}

async function createArc(window: Page, identifier: string) {
  await waitForArcVaultApi(window);

  return window.evaluate(
    async ({ apiName, arcIdentifier }) => {
      const api = (window as any)[apiName];
      const result = await api.createARC(null)(arcIdentifier);

      if (!result || result.tag !== 0) {
        const error = result?.fields?.[0];
        throw new Error(error?.message ?? 'Failed to create ARC');
      }

      return result.fields[0] as string;
    },
    { apiName: ARC_VAULT_API_NAME, arcIdentifier: identifier }
  );
}

test.describe('Swate Electron App', () => {
  let electronApp: ElectronApplication;
  let window: Page;
  let tempHomeDir: string;
  let tempArcParentDir: string;

  test.beforeEach(async () => {
    tempHomeDir = fs.mkdtempSync(path.join(os.tmpdir(), 'swate-electron-home-'));
    tempArcParentDir = fs.mkdtempSync(path.join(os.tmpdir(), 'swate-electron-arc-'));

    electronApp = await electron.launch({
      args: ['.'],
      env: {
        ...process.env,
        HOME: tempHomeDir,
      },
    });

    window = await electronApp.firstWindow();
    await window.waitForLoadState('domcontentloaded');
    await waitForArcVaultApi(window);
  });

  test.afterEach(async () => {
    if (electronApp) {
      await electronApp.close();
    }
    fs.rmSync(tempArcParentDir, { recursive: true, force: true });
    fs.rmSync(tempHomeDir, { recursive: true, force: true });
  });

  test('creates a Swate ARC and shows its file tree', async () => {
    await mockOpenDialogSelection(electronApp, tempArcParentDir);

    const arcPath = await createArc(window, ARC_IDENTIFIER);
    expect(arcPath).toBe(path.join(tempArcParentDir, ARC_IDENTIFIER));

    const homeButton = window.locator('[aria-label="Home"] button');
    await expect(homeButton).toBeVisible({ timeout: 30000 });
    await homeButton.click();

    const fileExplorer = window.getByTestId('file-explorer-container');
    await expect(fileExplorer).toBeVisible({ timeout: 30000 });
    await expect(fileExplorer).toContainText(ARC_IDENTIFIER, { timeout: 30000 });
    await expect(fileExplorer.locator('li').first()).toBeVisible({ timeout: 30000 });
  });
});
