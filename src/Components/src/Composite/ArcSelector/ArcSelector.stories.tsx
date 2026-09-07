import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, userEvent, waitFor, within } from 'storybook/test';
import { Entry as ArcSelectorEntry } from './ArcSelector.fs.js';

const meta = {
  title: 'Composite Components/ArcSelector',
  tags: ['autodocs'],
  parameters: { layout: 'fullscreen' },
  component: ArcSelectorEntry,
  args: { debug: true },
} satisfies Meta<typeof ArcSelectorEntry>;

export default meta;
type Story = StoryObj<typeof meta>;

export const DisplaySelector: Story = {};

export const ClickingArcPointerUpdatesActiveArc: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const selectorToggle = await canvas.findByTestId('selector-test');
    await userEvent.click(selectorToggle);
    await userEvent.click(await canvas.findByTestId('selector-arc-item-1'));
    await waitFor(() => expect(selectorToggle).toHaveTextContent('Test 2'));
  },
};

export const LongArcNamesAreTruncatedWithoutResizing: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const selectorToggle = await canvas.findByTestId('selector-test');
    const initialWidth = selectorToggle.getBoundingClientRect().width;

    await userEvent.click(selectorToggle);
    await userEvent.click(await canvas.findByTestId('selector-arc-item-3'));

    await waitFor(() => {
      const currentName = canvas.getByTestId('selector-current-arc-name');
      expect(selectorToggle.getBoundingClientRect().width).toBe(initialWidth);
      expect(currentName.scrollWidth).toBeGreaterThan(currentName.clientWidth);
    });
  },
};

export const ClickingArcPointerClosesDropdown: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('selector-test'));
    await userEvent.click(await canvas.findByTestId('selector-arc-item-0'));
    await waitFor(() =>
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument(),
    );
  },
};

export const ClickingActionClosesDropdown: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('selector-test'));
    await userEvent.click(await canvas.findByTestId('button-test'));
    await waitFor(() =>
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument(),
    );
  },
};

export const RestButtonShowsOptionsAndOptionsClickable: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(await canvas.findByTestId('selector-test'));
    await userEvent.click(await canvas.findByTestId('actionbar-rest-button'));

    expect(canvas.getByTestId('selector-dropdown-content')).toBeInTheDocument();

    const contextMenu = await within(document.body).findByTestId('context_menu');
    await userEvent.click(within(contextMenu).getByRole('button', { name: /open an existing arc/i }));

    await waitFor(() =>
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument(),
    );
  },
};
