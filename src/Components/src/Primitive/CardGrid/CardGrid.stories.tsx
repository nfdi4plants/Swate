import type { Meta, StoryObj } from '@storybook/react-vite';
import React, { useState } from 'react';
import { expect, userEvent, within } from 'storybook/test';
import { CardGrid, CardGridButton } from './CardGrid.fs.js';

const CardGridExample = () => {
  const [lastAction, setLastAction] = useState('none');

  return (
    <div className="swt:w-[560px] swt:max-w-full swt:space-y-3">
      <CardGrid gridTitle="Quick actions">
        <>
          <CardGridButton
            icon={<span aria-hidden="true">A</span>}
            header="Create"
            description="Create a new item"
            onclick={() => setLastAction('Create')}
          />
          <CardGridButton
            icon={<span aria-hidden="true">B</span>}
            header="Import"
            description="Import existing data"
            onclick={() => setLastAction('Import')}
          />
        </>
      </CardGrid>
      <span>Last action: {lastAction}</span>
    </div>
  );
};

const meta = {
  title: 'Primitive Components/CardGrid',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: CardGridExample,
} satisfies Meta<typeof CardGridExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <CardGridExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /create/i }));

    expect(canvas.getByText('Last action: Create')).toBeInTheDocument();
  },
};
