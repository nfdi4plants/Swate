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

export const EscapesClippingOwner: Story = {
  render: () => <ContextMenuExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const cell = canvas.getByRole('button', { name: /example table cell/i });
    const owner = cell.parentElement!;

    owner.style.width = '12rem';
    owner.style.height = '5rem';
    owner.style.overflow = 'hidden';

    const ownerRect = owner.getBoundingClientRect();
    fireEvent.contextMenu(cell, {
      clientX: ownerRect.right - 4,
      clientY: ownerRect.bottom - 4,
      bubbles: true,
    });

    const firstItem = await screen.findByRole('button', { name: /item 0/i });
    const menu = firstItem.parentElement!;
    const menuRect = menu.getBoundingClientRect();
    const candidatePoints = [
      [menuRect.left + 2, menuRect.top + 2],
      [menuRect.right - 2, menuRect.top + 2],
      [menuRect.left + 2, menuRect.bottom - 2],
      [menuRect.right - 2, menuRect.bottom - 2],
    ];
    const pointOutsideOwner = candidatePoints.find(([x, y]) => (
      x < ownerRect.left || x > ownerRect.right || y < ownerRect.top || y > ownerRect.bottom
    ));

    expect(pointOutsideOwner).toBeDefined();
    const [x, y] = pointOutsideOwner!;
    expect(document.elementFromPoint(x, y)?.closest('[role="menu"]')).toBe(menu);
  },
};
