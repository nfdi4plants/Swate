import type { Meta, StoryObj } from '@storybook/react-vite';
import React, { useState } from 'react';
import { expect, userEvent, within } from 'storybook/test';
import QuickAccessButton from './QuickAccessButton.fs.js';

const QuickAccessButtonExample = () => {
  const [clicks, setClicks] = useState(0);

  return (
    <div className="swt:flex swt:items-center swt:gap-3">
      <QuickAccessButton desc="Quick action" onclick={() => setClicks((current) => current + 1)}>
        <span aria-hidden="true">Q</span>
      </QuickAccessButton>
      <span>Clicked: {clicks}</span>
    </div>
  );
};

const meta = {
  title: 'Primitive Components/QuickAccessButton',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: QuickAccessButtonExample,
} satisfies Meta<typeof QuickAccessButtonExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <QuickAccessButtonExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTitle(/quick action/i));

    expect(canvas.getByText('Clicked: 1')).toBeInTheDocument();
  },
};
