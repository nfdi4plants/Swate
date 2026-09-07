import { useRef, useState } from 'react';
import { ofArray } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/List.ts';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, screen, userEvent, waitFor, within } from 'storybook/test';
import { Example as ContextMenuExample, ContextMenu } from './ContextMenu.fs.js';

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

function ChangingMenu() {
  const owner = useRef<HTMLDivElement | undefined>(undefined);
  const [reversed, setReversed] = useState(false);
  const [activationCount, setActivationCount] = useState(0);
  const labels = reversed ? ['Beta', 'Alpha'] : ['Alpha', 'Beta'];
  return <>
    <div ref={node => { owner.current = node ?? undefined; }} data-testid="changing-menu-owner">
      Open menu here
      <ContextMenu
        ref={owner}
        childInfo={() => ofArray(labels.map(label => ({
          text: <span>{label}</span>,
          kbdbutton: { element: <span />, label },
          isDivider: false,
          onClick: () => {
            setActivationCount(count => count + 1);
            setReversed(true);
          },
        })))}
      />
    </div>
    <output data-testid="activation-count">{activationCount}</output>
  </>;
}

export const TypeaheadUsesCurrentItemsAfterReopening: Story = {
  render: () => <ChangingMenu />,
  play: async ({ canvasElement }) => {
    const owner = within(canvasElement).getByTestId('changing-menu-owner');
    fireEvent.contextMenu(owner, { clientX: 40, clientY: 40, bubbles: true });
    await userEvent.click(await screen.findByRole('button', { name: 'Alpha' }));
    await waitFor(() => expect(screen.queryByRole('menu')).not.toBeInTheDocument());
    fireEvent.contextMenu(owner, { clientX: 40, clientY: 40, bubbles: true });
    const menu = await screen.findByRole('menu');
    menu.focus();
    await userEvent.keyboard('a');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Alpha' })).toHaveFocus());
  },
};

export const OpeningMouseReleaseDoesNotActivateAnItem: Story = {
  render: () => <ChangingMenu />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const owner = canvas.getByTestId('changing-menu-owner');
    fireEvent.contextMenu(owner, { clientX: 40, clientY: 40, bubbles: true });
    const alpha = await screen.findByRole('button', { name: 'Alpha' });

    // Releasing the right button over an item must not select it.
    fireEvent.mouseUp(alpha, { button: 2, bubbles: true });
    expect(screen.getByRole('menu')).toBeInTheDocument();
    expect(canvas.getByTestId('activation-count')).toHaveTextContent('0');

    await userEvent.click(alpha);
    await waitFor(() => {
      expect(screen.queryByRole('menu')).not.toBeInTheDocument();
      expect(canvas.getByTestId('activation-count')).toHaveTextContent('1');
    });
  },
};
