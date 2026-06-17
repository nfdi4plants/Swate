import React, { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, within } from 'storybook/test';
import { ResetTableConfirmationModal } from './ResetTableConfirmationModal.fs.js';

const NamedTableConfirmation = () => {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <button onClick={() => setIsOpen(true)}>Open named confirmation</button>
      <ResetTableConfirmationModal
        isOpen={isOpen}
        setIsOpen={setIsOpen}
        onDelete={() => undefined}
        tableName="Measurements"
      />
    </>
  );
};

const LegacyResetConfirmation = () => {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <button onClick={() => setIsOpen(true)}>Open legacy confirmation</button>
      <ResetTableConfirmationModal
        isOpen={isOpen}
        setIsOpen={setIsOpen}
        onDelete={() => undefined}
      />
    </>
  );
};

const meta = {
  title: 'Composite Components/AnnotationTable/ResetTableConfirmationModal',
  tags: ['autodocs'],
  component: NamedTableConfirmation,
} satisfies Meta<typeof NamedTableConfirmation>;

export default meta;

type Story = StoryObj<typeof meta>;

export const NamedTable: Story = {
  render: () => <NamedTableConfirmation />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /open named confirmation/i }));

    const dialog = await screen.findByRole('dialog', { name: /attention/i });
    const modal = within(dialog);

    expect(
      modal.getByText('You are about to delete table ‘Measurements’. This action cannot be undone.'),
    ).toBeInTheDocument();
    expect(modal.getByRole('button', { name: 'Delete Table: Measurements' })).toBeInTheDocument();
    expect(modal.queryByText(/right-click the sheet at the bottom/i)).not.toBeInTheDocument();

    await userEvent.click(modal.getByRole('button', { name: /^back$/i }));
  },
};

export const LegacyResetAll: Story = {
  render: () => <LegacyResetConfirmation />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /open legacy confirmation/i }));

    const dialog = await screen.findByRole('dialog', { name: /attention/i });
    const modal = within(dialog);

    expect(modal.getAllByText('all')).toHaveLength(2);
    expect(modal.getByText(/there is no option to recover any information/i)).toBeInTheDocument();
    expect(modal.getByText(/right-click the sheet at the bottom/i)).toBeInTheDocument();
    expect(modal.getByRole('button', { name: /^delete$/i })).toBeInTheDocument();

    await userEvent.click(modal.getByRole('button', { name: /^back$/i }));
  },
};
