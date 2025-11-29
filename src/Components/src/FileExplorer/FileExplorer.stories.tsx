import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect } from 'storybook/test';
import React from 'react';
import {FileExplorer} from "./FileExplorer.fs.ts";

const meta: Meta<typeof FileExplorer> = {
  title: "Components/FileExplorer",
  component: FileExplorer,
  parameters: {
    layout: 'fullscreen',
  },
};
export default meta;
type Story = StoryObj<typeof FileExplorer>;

const renderCell = (index: { x: number; y: number }) =>
  React.createElement('div', { style: { padding: 4 } }, `R${index.y}, C${index.x}`);

const renderActiveCell = (index: { x: number; y: number }) =>
  React.createElement('div', { style: { padding: 4 } }, `A${index.y}, C${index.x}`);

export const FileExplorerStory: Story = {
  decorators: [
    (Story) => {
      return <Story />;
    },
  ],

  render: (args) => {
    return React.createElement(FileExplorer, { ...args });
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

    const filecontainer = await canvas.findByTestId("file-explorer-container");
    expect(filecontainer).toBeTruthy();
  },
};
