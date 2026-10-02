import { vi } from 'vitest';

// jsdom has no layout. Supply deterministic geometry without replacing dnd-kit.
export function installRowGeometry(): void {
  vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(function () {
    const row = this.closest<HTMLElement>('[data-testid^="sortable-list-row-"]');
    const rows = row?.parentElement?.children;
    const index = rows ? Array.from(rows).indexOf(row!) : -1;
    return new DOMRect(0, index >= 0 ? 40 + index * 40 : 0, 400, index >= 0 ? 40 : 200);
  });
}

export function restoreGeometry(): void {
  vi.restoreAllMocks();
}

export function readRowStyle(row: HTMLElement): { backgroundColor: string; height: string } {
  const style = window.getComputedStyle(row);
  return { backgroundColor: style.backgroundColor, height: style.height };
}
