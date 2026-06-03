import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, screen, waitFor, within } from 'storybook/test';
import { Example as ContextMenuExample } from './ContextMenu.fs.js';

const meta = {
  title: 'Primitive Components/ContextMenu',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: ContextMenuExample,
} satisfies Meta<typeof ContextMenuExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <ContextMenuExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const cell = canvas.getByRole('button', { name: /example table cell/i });

    fireEvent.contextMenu(document.body, { clientX: 10, clientY: 10, bubbles: true });
    expect(screen.queryByRole('button', { name: /item 0/i })).not.toBeInTheDocument();

    await waitFor(() => {
      fireEvent.contextMenu(cell.firstChild ?? cell, { clientX: 40, clientY: 40, bubbles: true });
      expect(screen.getByRole('button', { name: /item 0/i })).toBeInTheDocument();
    });
  },
};
