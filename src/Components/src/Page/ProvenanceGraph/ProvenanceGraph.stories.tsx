import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, waitFor, within } from 'storybook/test';
import { ProvenanceGraph } from './ProvenanceGraph.fs.js';
import { emptyIndex, index } from './ProvenanceGraph.sample.fs.js';

// The page fills its parent, so every story gives it a box with a height.
const box = (width: number | string, height: number) => (Story: React.ComponentType) => (
  <div style={{ width, height, border: '1px solid #8884' }}>
    <Story />
  </div>
);

const meta = {
  title: 'Page Components/ProvenanceGraph',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  args: {
    index,
  },
  component: ProvenanceGraph,
} satisfies Meta<typeof ProvenanceGraph>;

export default meta;

type Story = StoryObj<typeof meta>;

const extraction = 'assays/Extraction/';
const extractionProcess = 'assays/Extraction/::Extraction';
const inputGroup = `${extractionProcess}#in0`;
const wait = { timeout: 15000 };

// Clicks a node and waits for a node of the next view.
async function open(canvas: ReturnType<typeof within>, nodeId: string, nextId: string) {
  await userEvent.click(await canvas.findByTestId(`graph-node-${nodeId}`, {}, wait));
  await canvas.findByTestId(`graph-node-${nextId}`, {}, wait);
}

async function openLineage(canvas: ReturnType<typeof within>) {
  await canvas.findByTestId(`graph-node-${extraction}`, {}, wait);
  await open(canvas, extraction, extractionProcess);
  await open(canvas, extractionProcess, inputGroup);
  await open(canvas, inputGroup, 'leaf-1');
  await open(canvas, 'leaf-1', 'data/m1.raw');
}

export const Datasets: Story = {
  decorators: [box('100%', 560)],
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await canvas.findByTestId(`graph-node-${extraction}`, {}, wait);
    await expect(canvas.getByTestId('provenance-crumb-0')).toHaveAttribute('aria-current', 'page');

    // The legend swatches resolve to a color of their own, so the palette reaches them.
    const swatches = canvas.getAllByTestId('provenance-legend-swatch');
    const first = getComputedStyle(swatches[0]);
    await expect(first.backgroundColor).not.toBe('');
    await expect(first.backgroundColor).not.toBe('rgba(0, 0, 0, 0)');
  },
};

// Click through all five levels, then return to the datasets with the first crumb.
export const DrillDown: Story = {
  decorators: [box('100%', 640)],
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await openLineage(canvas);
    await waitFor(() => expect(canvas.getAllByTestId(/^provenance-crumb-\d+$/)).toHaveLength(5));
    await expect(canvas.queryByTestId('provenance-hint')).not.toBeInTheDocument();

    await userEvent.click(canvas.getByTestId('provenance-crumb-0'));
    await canvas.findByTestId(`graph-node-${extraction}`, {}, wait);
    await expect(canvas.getAllByTestId(/^provenance-crumb-\d+$/)).toHaveLength(1);
  },
};

// A box taller than it is wide gets the compact layout of the canvas.
export const SmallBox: Story = {
  decorators: [box(360, 640)],
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await canvas.findByTestId(`graph-node-${extraction}`, {}, wait);
    await waitFor(() => expect(canvas.getByTestId('graph-canvas-content')).toHaveAttribute('data-compact', 'true'), wait);
  },
};

// At five crumbs the narrow header keeps the first crumb and the last two and moves the others into a menu.
export const NarrowHeader: Story = {
  decorators: [box(320, 640)],
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await openLineage(canvas);
    await waitFor(() => expect(canvas.getByTestId('provenance-crumb-menu')).toBeInTheDocument(), wait);
    await expect(canvas.queryByTestId('provenance-crumb-1')).not.toBeInTheDocument();
    await expect(canvas.getByTestId('provenance-crumb-4')).toHaveAttribute('aria-current', 'page');
  },
};

export const EmptyArc: Story = {
  args: { index: emptyIndex },
  decorators: [box('100%', 320)],
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByTestId('provenance-empty')).toHaveAttribute('data-empty', 'no-datasets');
    await expect(canvas.getByTestId('provenance-legend')).toBeInTheDocument();
  },
};
