import React, { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, waitFor, within } from 'storybook/test';
import { Main as ArcSelector } from './ArcSelector.fs.js';

const recentARCs = [
  { name: 'Test 1', path: '/Here/Test 1', isActive: false },
  { name: 'Test 2', path: '/Here/Test 2', isActive: false },
  { name: 'Test 3', path: '/Here/Test 3', isActive: false },
];

function ArcSelectorStory({ debug = true }: { debug?: boolean }) {
  const [currentlyOpenArcPath, setCurrentlyOpenArcPath] = useState<string>();

  return (
    <ArcSelector
      recentARCs={recentARCs}
      onClick={arc => setCurrentlyOpenArcPath(arc.path)}
      currentlyOpenArcPath={currentlyOpenArcPath}
      debug={debug}
      actionbar={<button data-testid="story-action">Create a new ARC</button>}
    />
  );
}

const meta = {
  title: 'Composite Components/ArcSelector',
  tags: ['autodocs'],
  parameters: { layout: 'fullscreen' },
  component: ArcSelectorStory,
} satisfies Meta<typeof ArcSelectorStory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const DisplaySelector: Story = {};

export const ClickingArcPointerUpdatesActiveArc: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const selectorToggle = await canvas.findByTestId('selector-test');
    await userEvent.click(selectorToggle);
    await userEvent.click(await canvas.findByTestId('selector-arc-item-1'));
    await waitFor(() => expect(selectorToggle).toHaveTextContent('Test 2'));
  },
};

export const ClickingArcPointerClosesDropdown: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('selector-test'));
    await userEvent.click(await canvas.findByTestId('selector-arc-item-0'));
    await waitFor(() =>
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument(),
    );
  },
};

export const ClickingActionbarClosesDropdown: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('selector-test'));
    await userEvent.click(await canvas.findByTestId('story-action'));
    await waitFor(() =>
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument(),
    );
  },
};
