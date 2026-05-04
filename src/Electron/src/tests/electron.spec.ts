import { test, expect, _electron as electron, ElectronApplication, Page } from 'playwright/test';
import fs from 'fs';
import os from 'os';
import path from 'path';

const ARC_VAULT_API_NAME = 'FABLE_REMOTING_IArcVaultsApi';
const ARC_IDENTIFIER = 'playwright-test-arc';
const SIDEBAR_OVERFLOW_FILE_COUNT = 160;

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

function seedArcWithOverflowFiles(arcPath: string, count: number) {
  for (let i = 0; i < count; i += 1) {
    const fileName = `scroll-file-${String(i).padStart(3, '0')}.txt`;
    fs.writeFileSync(path.join(arcPath, fileName), `seeded file ${i}\n`);
  }
}

function seedArcWithOverflowFilesInDirectory(arcPath: string, directoryName: string, count: number) {
  const targetDirectoryPath = path.join(arcPath, directoryName);
  fs.mkdirSync(targetDirectoryPath, { recursive: true });
  seedArcWithOverflowFiles(targetDirectoryPath, count);
}

function seedArcWithNestedDirectory(arcPath: string, directoryName: string, fileName: string) {
  const nestedDirectoryPath = path.join(arcPath, directoryName);
  fs.mkdirSync(nestedDirectoryPath, { recursive: true });
  fs.writeFileSync(path.join(nestedDirectoryPath, fileName), `seeded file ${fileName}\n`);
}

function seedArcWithEmptyDirectory(arcPath: string, directoryName: string) {
  const nestedDirectoryPath = path.join(arcPath, directoryName);
  fs.mkdirSync(nestedDirectoryPath, { recursive: true });
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

    const arcNameLabel = window.getByTestId('left-sidebar-file-explorer-arc-name');
    await expect(arcNameLabel).toBeVisible({ timeout: 30000 });
    await expect(arcNameLabel).toHaveText(ARC_IDENTIFIER, { timeout: 30000 });
    await expect(arcNameLabel).toHaveAttribute('title', arcPath, { timeout: 30000 });

    await arcNameLabel.click();
    const arcPathValue = window.getByTestId('file-explorer-arc-path-value');
    await expect(arcPathValue).toHaveText(arcPath, { timeout: 30000 });
    await expect(window.getByTestId('file-explorer-arc-path-copy')).toBeVisible({ timeout: 30000 });
    await expect(window.getByTestId('file-explorer-arc-path-open-folder')).toBeVisible({ timeout: 30000 });

    const fileExplorer = window.getByTestId('file-explorer-container');
    await expect(fileExplorer).toBeVisible({ timeout: 30000 });

    const rootFolderToggle = fileExplorer.getByRole('button', { name: `Expand ${ARC_IDENTIFIER}` });
    await expect(rootFolderToggle).toHaveCount(0);

    await expect(fileExplorer.locator('li').first()).toBeVisible({ timeout: 30000 });
  });

  test('uses the File explorer button to collapse and reopen the sidebar', async () => {
    await mockOpenDialogSelection(electronApp, tempArcParentDir);

    const arcIdentifier = `${ARC_IDENTIFIER}-left-sidebar-toggle`;
    const arcPath = await createArc(window, arcIdentifier);
    expect(arcPath).toBe(path.join(tempArcParentDir, arcIdentifier));

    const homeButton = window.locator('[aria-label="Home"] button');
    await expect(homeButton).toBeVisible({ timeout: 30000 });
    await homeButton.click();

    const navbarLeftSidebarToggle = window.getByRole('button', { name: 'Toggle left sidebar' });
    await expect(navbarLeftSidebarToggle).toHaveCount(0);

    const fileExplorerButton = window.locator('[aria-label="File explorer"] button');
    await expect(fileExplorerButton).toBeVisible({ timeout: 30000 });

    const leftSidebar = window.getByTestId('layout-main-left-sidebar');
    const fileExplorer = window.getByTestId('file-explorer-container');

    const readLeftSidebarWidth = () =>
      leftSidebar.evaluate((element: HTMLElement) => element.getBoundingClientRect().width);

    if ((await readLeftSidebarWidth()) <= 1) {
      await fileExplorerButton.click();
    }

    await expect.poll(readLeftSidebarWidth, { timeout: 30000 }).toBeGreaterThan(1);
    await expect(fileExplorer).toBeVisible({ timeout: 30000 });

    await fileExplorerButton.click();
    await expect.poll(readLeftSidebarWidth, { timeout: 30000 }).toBeLessThanOrEqual(1);

    await fileExplorerButton.click();
    await expect.poll(readLeftSidebarWidth, { timeout: 30000 }).toBeGreaterThan(1);
    await expect(fileExplorer).toBeVisible({ timeout: 30000 });
    await expect(fileExplorer.locator('li').first()).toBeVisible({ timeout: 30000 });
  });

  test('hides arrows for empty folders and lazy-loads non-empty folders in the normal filetree explorer', async () => {
    await mockOpenDialogSelection(electronApp, tempArcParentDir);

    const emptyDirectoryName = 'arrowless-empty-dir';
    const nestedDirectoryName = 'arrow-load-dir';
    const nestedChildFileName = 'arrow-load-child.txt';
    const arcIdentifier = `${ARC_IDENTIFIER}-arrow-load`;

    const arcPath = await createArc(window, arcIdentifier);
    seedArcWithEmptyDirectory(arcPath, emptyDirectoryName);
    seedArcWithNestedDirectory(arcPath, nestedDirectoryName, nestedChildFileName);

    const homeButton = window.locator('[aria-label="Home"] button');
    await expect(homeButton).toBeVisible({ timeout: 30000 });
    await homeButton.click();

    const fileExplorer = window.getByTestId('file-explorer-container');
    await expect(fileExplorer).toBeVisible({ timeout: 30000 });

    const rootFolderToggle = fileExplorer.getByRole('button', { name: `Expand ${arcIdentifier}` });
    await expect(rootFolderToggle).toHaveCount(0);

    const emptyFolderToggle = fileExplorer.getByRole('button', { name: `Expand ${emptyDirectoryName}` });
    await expect(emptyFolderToggle).toHaveCount(0);

    const nestedFolderToggle = fileExplorer.getByRole('button', { name: `Expand ${nestedDirectoryName}` });
    await expect(nestedFolderToggle).toBeVisible({ timeout: 30000 });
    await expect(fileExplorer).not.toContainText(nestedChildFileName);

    await nestedFolderToggle.click();

    await expect(fileExplorer).toContainText(nestedChildFileName, { timeout: 30000 });
  });

  test('keeps the file explorer toolbar visible while the file tree scrolls', async () => {
    await mockOpenDialogSelection(electronApp, tempArcParentDir);

    const arcIdentifier = `${ARC_IDENTIFIER}-scroll`;
    const scrollDirectoryName = 'scroll-directory';
    const arcPath = await createArc(window, arcIdentifier);
    seedArcWithOverflowFilesInDirectory(arcPath, scrollDirectoryName, SIDEBAR_OVERFLOW_FILE_COUNT);

    const homeButton = window.locator('[aria-label="Home"] button');
    await expect(homeButton).toBeVisible({ timeout: 30000 });
    await homeButton.click();

    const fileExplorer = window.getByTestId('file-explorer-container');
    await expect(fileExplorer).toBeVisible({ timeout: 30000 });

    const rootFolderToggle = fileExplorer.getByRole('button', { name: `Expand ${arcIdentifier}` });
    await expect(rootFolderToggle).toHaveCount(0);

    const scrollFolderToggle = fileExplorer.getByRole('button', { name: `Expand ${scrollDirectoryName}` });
    await expect(scrollFolderToggle).toBeVisible({ timeout: 30000 });
    await expect(scrollFolderToggle).toHaveClass(/swt:h-5/, { timeout: 30000 });
    await expect(scrollFolderToggle).toHaveClass(/swt:w-5/, { timeout: 30000 });
    const scrollFolderRowContainer = scrollFolderToggle.locator('xpath=ancestor::li[@data-file-item-id][1]/div[@data-file-item-id]');
    await expect(scrollFolderRowContainer).toBeVisible({ timeout: 30000 });
    const scrollFolderNameButton = fileExplorer.getByRole('button', { name: scrollDirectoryName });
    await expect(scrollFolderNameButton).toBeVisible({ timeout: 30000 });

    const rootFolderRowBox = await scrollFolderRowContainer.boundingBox();
    const rootFolderRowPaddingRight = await scrollFolderRowContainer.evaluate((element: HTMLElement) => {
      return Number.parseFloat(window.getComputedStyle(element).paddingRight || '0');
    });
    const rootFolderToggleBox = await scrollFolderToggle.boundingBox();

    expect(rootFolderRowBox).toBeTruthy();
    expect(rootFolderToggleBox).toBeTruthy();

    if (rootFolderRowBox && rootFolderToggleBox) {
      const treeRight = rootFolderRowBox.x + rootFolderRowBox.width - rootFolderRowPaddingRight;
      const arrowRight = rootFolderToggleBox.x + rootFolderToggleBox.width;
      expect(Math.abs(treeRight - arrowRight)).toBeLessThanOrEqual(2);
    }

    await scrollFolderNameButton.click();
    await expect(scrollFolderToggle).toHaveClass(/swt:bg-base-300/, { timeout: 30000 });
    await expect(scrollFolderToggle.locator('i')).toHaveClass(/swt:fluent--caret-right-24-filled/, { timeout: 30000 });
    await scrollFolderToggle.click();

    const rootFolderCollapseToggle = fileExplorer.getByRole('button', { name: `Collapse ${scrollDirectoryName}` });
    await expect(rootFolderCollapseToggle).toBeVisible({ timeout: 30000 });
    await expect(rootFolderCollapseToggle.locator('i')).toHaveClass(/swt:fluent--caret-down-24-filled/, {
      timeout: 30000,
    });

    await expect(fileExplorer).toContainText('scroll-file-159.txt', { timeout: 30000 });

    const treeViewport = window.getByTestId('left-sidebar-file-explorer-tree');
    await expect(treeViewport).toBeVisible({ timeout: 30000 });
    await expect(treeViewport).toHaveClass(/swt:overflow-x-auto/, { timeout: 30000 });

    const hasOverflow = await treeViewport.evaluate((element: HTMLElement) => {
      return element.scrollHeight > element.clientHeight;
    });
    expect(hasOverflow).toBeTruthy();

    const toolbar = window.getByTestId('left-sidebar-file-explorer-toolbar');
    await expect(toolbar).toBeVisible({ timeout: 30000 });

    const toolbarTopBeforeScroll = await toolbar.evaluate((element: HTMLElement) => {
      return element.getBoundingClientRect().top;
    });

    await treeViewport.evaluate((element: HTMLElement) => {
      element.scrollTop = Math.min(320, element.scrollHeight);
    });

    await window.waitForTimeout(100);

    const scrollTop = await treeViewport.evaluate((element: HTMLElement) => {
      return element.scrollTop;
    });
    expect(scrollTop).toBeGreaterThan(0);

    const toolbarTopAfterScroll = await toolbar.evaluate((element: HTMLElement) => {
      return element.getBoundingClientRect().top;
    });
    expect(Math.abs(toolbarTopAfterScroll - toolbarTopBeforeScroll)).toBeLessThanOrEqual(2);

    const targetFile = fileExplorer.locator('a').filter({ hasText: 'scroll-file-030.txt' }).first();
    await targetFile.scrollIntoViewIfNeeded();
    await targetFile.click();
    await expect(targetFile).toHaveClass(/swt:bg-base-300/, { timeout: 30000 });
    const targetFileLabel = targetFile.locator('span').filter({ hasText: 'scroll-file-030.txt' }).first();
    await expect(targetFileLabel).toHaveClass(/swt:font-semibold/, { timeout: 30000 });
    await expect(targetFileLabel).toHaveClass(/swt:text-primary/, { timeout: 30000 });

    const collapseRootAfterSelection = fileExplorer.getByRole('button', { name: `Collapse ${scrollDirectoryName}` });
    await expect(collapseRootAfterSelection).toBeVisible({ timeout: 30000 });
    await expect(collapseRootAfterSelection).toHaveClass(/swt:bg-base-300/, { timeout: 30000 });
    await collapseRootAfterSelection.click();

    const expandRootAfterCollapse = fileExplorer.getByRole('button', { name: `Expand ${scrollDirectoryName}` });
    await expect(expandRootAfterCollapse).toBeVisible({ timeout: 30000 });
    await expandRootAfterCollapse.click();

    const targetFileAfterReopen = fileExplorer.locator('a').filter({ hasText: 'scroll-file-030.txt' }).first();
    await expect(targetFileAfterReopen).toHaveClass(/swt:bg-base-300/, { timeout: 30000 });
    const targetFileLabelAfterReopen = targetFileAfterReopen.locator('span').filter({ hasText: 'scroll-file-030.txt' }).first();
    await expect(targetFileLabelAfterReopen).toHaveClass(/swt:font-semibold/, { timeout: 30000 });
    await expect(targetFileLabelAfterReopen).toHaveClass(/swt:text-primary/, { timeout: 30000 });
  });
});
