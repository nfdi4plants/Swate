import fs from 'node:fs';
import path from 'node:path';
import { chromium, expect, type Browser, type Page } from '@playwright/test';

export type E2eMetadata = {
  debugPort: number;
  tempArcParentDir: string;
  fableBuildStartMs: number;
};

export function readE2eMetadata(): E2eMetadata {
  const metadataPath = path.resolve('src/tests/test-results/e2e-env.json');
  return JSON.parse(fs.readFileSync(metadataPath, 'utf8')) as E2eMetadata;
}

export async function connectToElectronRenderer(metadata = readE2eMetadata()): Promise<{
  browser: Browser;
  page: Page;
}> {
  const browser = await chromium.connectOverCDP(`http://127.0.0.1:${metadata.debugPort}`);
  const page = await waitForRendererPage(browser);

  await page.waitForLoadState('domcontentloaded');
  await page.waitForFunction(() => typeof window.FABLE_REMOTING_IArcVaultsApi !== 'undefined');

  return { browser, page };
}

async function waitForRendererPage(browser: Browser): Promise<Page> {
  const deadline = Date.now() + 60000;

  while (Date.now() < deadline) {
    for (const context of browser.contexts()) {
      const page = context
        .pages()
        .find((candidate) => !candidate.url().startsWith('devtools://') && !candidate.isClosed());

      if (page) {
        return page;
      }
    }

    await new Promise((resolve) => setTimeout(resolve, 250));
  }

  throw new Error('Timed out waiting for Electron renderer page.');
}

export async function callArcVaultApi<T>(page: Page, method: string, ...args: unknown[]): Promise<T> {
  return page.evaluate(
    async ({ methodName, methodArgs }) => {
      const api = window.FABLE_REMOTING_IArcVaultsApi;
      const fn = api?.[methodName];

      if (typeof fn !== 'function') {
        throw new Error(`FABLE_REMOTING_IArcVaultsApi.${methodName} is not available.`);
      }

      const withEvent = fn(null);
      return typeof withEvent === 'function' ? await withEvent(...methodArgs) : await withEvent;
    },
    { methodName: method, methodArgs: args },
  ) as Promise<T>;
}

export function unwrapOk<T>(result: any): T {
  if (!result || result.tag !== 0) {
    const error = result?.fields?.[0];
    throw new Error(error?.message ?? error?.Message ?? 'Expected F# Result.Ok.');
  }

  return result.fields?.[0] as T;
}

export async function getArcVaultFileTreeKeys(page: Page): Promise<string[]> {
  return page.evaluate(async () => {
    const api = window.FABLE_REMOTING_IArcVaultsApi;
    const fn = api?.getFileTree;

    if (typeof fn !== 'function') {
      throw new Error('FABLE_REMOTING_IArcVaultsApi.getFileTree is not available.');
    }

    const withEvent = fn(null);
    const result = typeof withEvent === 'function' ? await withEvent() : await withEvent;

    if (!result || result.tag !== 0) {
      const error = result?.fields?.[0];
      throw new Error(error?.message ?? error?.Message ?? 'Expected getFileTree to return Result.Ok.');
    }

    const tree = result.fields?.[0];

    if (tree instanceof Map) {
      return Array.from(tree.keys()).map(String);
    }

    return Object.keys(tree ?? {});
  });
}

export async function expectCreatedArcVisible(page: Page, arcIdentifier: string, arcPath: string) {
  const fileExplorer = page.getByTestId('left-sidebar-file-explorer');

  if (!(await fileExplorer.isVisible())) {
    const sidebarToggle = page.getByRole('button', { name: 'Toggle left sidebar' }).first();

    if ((await sidebarToggle.count()) > 0) {
      await sidebarToggle.click();
    } else {
      const fileExplorerButton = page.getByRole('button', { name: 'File explorer' }).first();

      if ((await fileExplorerButton.count()) > 0) {
        await fileExplorerButton.click();
      }
    }
  }

  await expect(fileExplorer).toBeVisible({ timeout: 60000 });

  const arcActions = page.getByTestId('arc-vault-actions-btn');
  await expect(arcActions).toBeVisible({ timeout: 60000 });
  await expect(arcActions).toHaveText(arcIdentifier, { timeout: 60000 });
  await expect(arcActions).toHaveAttribute('title', arcPath, { timeout: 60000 });
}

declare global {
  interface Window {
    FABLE_REMOTING_IArcVaultsApi?: Record<string, (...args: unknown[]) => unknown>;
  }
}
