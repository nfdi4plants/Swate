import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect } from 'storybook/test';
import React from 'react';
import Table from "./Table.fs.js";

const meta: Meta<typeof Table> = {
  title: "Components/VirtualTable/SafariStyleCheck",
  component: Table,
  parameters: {
    layout: 'fullscreen',
  },
};
export default meta;
type Story = StoryObj<typeof Table>;

const renderCell = (index: { x: number; y: number }) =>
  React.createElement('div', { style: { padding: 4 } }, `R${index.y}, C${index.x}`);

const renderActiveCell = (index: { x: number; y: number }) =>
  React.createElement('div', { style: { padding: 4 } }, `A${index.y}, C${index.x}`);

export const SafariStyleCheck: Story = {
  decorators: [
    (Story) => {
      // Create a userAgent safari mock to test for safari
      Object.defineProperty(window.navigator, "userAgent", {
        value:
          "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
        configurable: true,
      });

      return <Story />;
    },
  ],

  render: (args) => {
    return React.createElement(Table, { ...args });
  },

  args: {
    rowCount: 20,
    columnCount: 10,
    height: 300,
    width: 600,
    renderCell,
    renderActiveCell,
    debug: true,
  },

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const scrollContainer = await canvas.findByTestId("virtualized-table");
    expect(scrollContainer).toBeTruthy();

    const tableEl = scrollContainer.querySelector("table") as HTMLElement | null;
    expect(tableEl).not.toBeNull();

    const cells = tableEl!.querySelectorAll("th, td");
    expect(cells.length).toBeGreaterThan(0);

    const tableContainer = tableEl!.parentElement as HTMLElement | null;
    expect(tableContainer).not.toBeNull();

    const computed = window.getComputedStyle(tableContainer!);

    expect(computed.willChange).toBe("transform");
    expect(computed.contain).toBe("size layout paint");

    expect(computed.minHeight).not.toBe("");
    expect(computed.minWidth).not.toBe("");

    const rect = tableEl!.getBoundingClientRect();
    expect(rect.width).toBeGreaterThan(10);
    expect(rect.height).toBeGreaterThan(10);

    await (async () => {
      scrollContainer.scrollTop = 100;
      scrollContainer.scrollLeft = 100;
    })();

    const cellsAfter = tableEl!.querySelectorAll("th, td");
    expect(cellsAfter.length).toBeGreaterThan(0);
  },
};
