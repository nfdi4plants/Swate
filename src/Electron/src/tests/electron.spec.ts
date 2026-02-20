import { test, expect, _electron as electron, ElectronApplication, Page } from '@playwright/test';
import fs from 'fs';
import os from 'os';
import path from 'path';

test.describe('Swate Electron App', () => {
  let electronApp: ElectronApplication;
  let window: Page;
  let tempArcPath: string;

  test.beforeEach(async () => {
    // create a temporary dummy ARC file
    tempArcPath = path.join(os.tmpdir(), 'test.arc');
    fs.writeFileSync(tempArcPath, 'dummy ARC content');

    electronApp = await electron.launch({ args: ['.'] });
    window = await electronApp.firstWindow();
    await window.waitForLoadState('domcontentloaded');
  });

  test.afterEach(async () => {
    await electronApp.close();
    fs.unlinkSync(tempArcPath); // clean up temp file
  });

  test('open local ARC and display file view', async () => {
    const openArcButton = window.locator('text=Open a locally existing ARC!');
    await expect(openArcButton).toBeVisible({ timeout: 30000 });

    const [fileChooser] = await Promise.all([
      window.waitForEvent('filechooser'),
      openArcButton.click(),
    ]);

    await fileChooser.setFiles(tempArcPath);

    const fileView = window.locator('#file-view'); // replace with actual selector
    await expect(fileView).toBeVisible({ timeout: 30000 });

    console.log('ARC file successfully loaded and file view visible');
  });
});
