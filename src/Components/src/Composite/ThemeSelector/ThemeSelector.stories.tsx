import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
import { Entry as ThemeSelector } from './ThemeSelector.fs.js';


const meta = {
  title: 'Composite Components/ThemeSelector',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
  component: ThemeSelector,
} satisfies Meta<typeof ThemeSelector>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const select = within(canvasElement).getByRole("combobox");
    const root = document.documentElement;

    // The markdown preview takes its syntax colors from data-color-mode.
    await userEvent.selectOptions(select, "dark");
    await waitFor(() => expect(root).toHaveAttribute("data-color-mode", "dark"));
    await userEvent.selectOptions(select, "light");
    await waitFor(() => expect(root).toHaveAttribute("data-color-mode", "light"));

    // Auto is the stored default, so later stories start without a leftover theme.
    await userEvent.selectOptions(select, "auto");
    await waitFor(() => expect(root).not.toHaveAttribute("data-color-mode"));
  },
};