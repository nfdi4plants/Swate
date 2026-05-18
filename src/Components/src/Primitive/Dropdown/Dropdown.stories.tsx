import type { Meta, StoryObj } from '@storybook/react-vite';
import React, { useState } from 'react';
import { expect, userEvent, waitFor, within } from 'storybook/test';
import { Dropdown_Main_Z54EBACFD as DropdownMain } from './Dropdown.fs.js';

const DropdownExample = () => {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <DropdownMain
      isOpen={isOpen}
      setIsOpen={setIsOpen}
      toggle={
        <button type="button" className="swt:btn" onClick={() => setIsOpen((current) => !current)}>
          Toggle menu
        </button>
      }
      closeOnClick
    >
      <>
        <li>
          <button type="button">First action</button>
        </li>
        <li>
          <button type="button">Second action</button>
        </li>
      </>
    </DropdownMain>
  );
};

const meta = {
  title: 'Primitive Components/Dropdown',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: DropdownExample,
} satisfies Meta<typeof DropdownExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <DropdownExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /toggle menu/i }));

    const firstAction = await canvas.findByRole('button', { name: /first action/i });
    await userEvent.click(firstAction);

    await waitFor(() => {
      expect(canvas.queryByRole('button', { name: /first action/i })).not.toBeInTheDocument();
    });
  },
};
