import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, userEvent, waitFor, within } from 'storybook/test';
import { Sample, CustomRowSample, EmptySample } from './SortableList.sample.fs.js';

const meta = {
  title: 'Composite Components/SortableList',
  component: Sample,
  tags: ['autodocs'],
  parameters: { layout: 'padded' },
} satisfies Meta<typeof Sample>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <Sample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByTestId('sortable-list-up-alpha')).toBeDisabled();
    expect(canvas.getByTestId('sortable-list-down-gamma')).toBeDisabled();

    await userEvent.click(canvas.getByTestId('sortable-list-down-alpha'));
    expect(canvas.getAllByTestId(/^sortable-list-row-/).map(row => row.dataset.testid))
      .toEqual(['sortable-list-row-beta', 'sortable-list-row-alpha', 'sortable-list-row-gamma']);

    await userEvent.click(canvas.getByTestId('sortable-list-remove-beta'));
    expect(canvas.queryByTestId('sortable-list-row-beta')).not.toBeInTheDocument();
    expect(canvas.getByTestId('sortable-list-up-alpha')).toBeDisabled();
  },
};

export const CustomRows: Story = {
  render: () => <CustomRowSample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByTestId('sortable-list-custom-alpha')).toHaveTextContent('First sample');
    expect(canvas.getByTestId('sortable-list-custom-gamma')).toHaveTextContent('No extra data');
    await userEvent.click(canvas.getByTestId('sortable-list-up-beta'));
    expect(canvas.getByTestId('sortable-list-up-beta')).toBeDisabled();
    await userEvent.click(canvas.getByTestId('sortable-list-remove-alpha'));
    expect(canvas.queryByTestId('sortable-list-custom-alpha')).not.toBeInTheDocument();
  },
};

export const Empty: Story = {
  render: () => <EmptySample />,
};

export const DragAndDrop: Story = {
  render: () => <Sample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const handle = canvas.getByTestId('sortable-list-drag-alpha');
    const source = handle.getBoundingClientRect();
    const target = canvas.getByTestId('sortable-list-row-gamma').getBoundingClientRect();
    const pointer = { button: 0, buttons: 1, pointerId: 1, isPrimary: true, pointerType: 'mouse' };
    const x = source.left + source.width / 2;
    const y = source.top + source.height / 2;

    fireEvent.pointerDown(handle, { ...pointer, clientX: x, clientY: y });
    fireEvent.pointerMove(document, { ...pointer, clientX: x, clientY: y + 10 });
    await waitFor(() => expect(handle).toHaveAttribute('aria-pressed', 'true'));

    const targetX = target.left + target.width / 2;
    const targetY = target.top + target.height / 2;
    fireEvent.pointerMove(document, { ...pointer, clientX: targetX, clientY: targetY });
    // Let collision detection observe the final pointer position before dropping.
    await new Promise(resolve => requestAnimationFrame(resolve));
    fireEvent.pointerUp(document, { ...pointer, buttons: 0, clientX: targetX, clientY: targetY });

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^sortable-list-row-/).map(row => row.dataset.testid))
        .toEqual(['sortable-list-row-beta', 'sortable-list-row-gamma', 'sortable-list-row-alpha']);
    });
    // PointerSensor removes its document-wide click guard 50ms after dropping.
    // Let that cleanup finish before the next story activates a button.
    await new Promise(resolve => setTimeout(resolve, 100));
  },
};

export const KeyboardReorder: Story = {
  render: () => <Sample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const down = canvas.getByTestId('sortable-list-down-alpha');
    const keyboardUser = userEvent.setup({ document: canvasElement.ownerDocument });
    down.focus();
    await expect(down).toHaveFocus();
    await keyboardUser.keyboard('{Enter}');
    await waitFor(() => {
      expect(canvas.getAllByTestId(/^sortable-list-row-/).map(row => row.dataset.testid))
        .toEqual(['sortable-list-row-beta', 'sortable-list-row-alpha', 'sortable-list-row-gamma']);
    });
  },
};
