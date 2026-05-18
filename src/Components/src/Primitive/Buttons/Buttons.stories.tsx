import type { Meta, StoryObj } from '@storybook/react-vite';
import React, { useState } from 'react';
import { expect, userEvent, within } from 'storybook/test';
import {
  DeleteButton,
  CircularExitButton,
  CollapseButton,
  QuickAccessButton,
} from './Buttons.fs.js';

const ButtonsExample = () => {
  const [isCollapsed, setIsCollapsed] = useState(false);

  return (
    <div className="swt:flex swt:flex-col swt:items-start swt:gap-4">
      <div className="swt:flex swt:items-center swt:gap-2">
        <DeleteButton />
        <CircularExitButton />
        <CollapseButton isCollapsed={isCollapsed} setIsCollapsed={setIsCollapsed} />
        <QuickAccessButton title="Quick Access" onclick={() => console.log('Quick Access clicked')} >
            <span aria-hidden="true">Q</span>
        </QuickAccessButton>
      </div>
      <span>Collapsed: {isCollapsed ? 'yes' : 'no'}</span>
    </div>
  );
};

const meta = {
  title: 'Primitive Components/Buttons',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: ButtonsExample,
} satisfies Meta<typeof ButtonsExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <ButtonsExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const checkbox = canvas.getByRole('checkbox');

    expect(checkbox).not.toBeChecked();
    await userEvent.click(checkbox);

    expect(canvas.getByText('Collapsed: yes')).toBeInTheDocument();
  },
};
