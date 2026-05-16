import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, waitFor, within } from 'storybook/test';
import Layout from "../Layout/Layout.fs.js";
import { Entry } from './Selector.fs.js';

const meta = {
  title: "Components/ArcSelector",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'fullscreen',
  },
  component: Layout,
} satisfies Meta<typeof Layout>;

export default meta;

type Story = StoryObj<typeof meta>;

export const DisplaySelector: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      < Entry debug={true} />
    </div>
  }
};

export const ClickingArcPointerUpdatesActiveArc: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      <Entry debug={true} />
    </div>
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const selectorToggle = await canvas.findByTestId('selector-test');
    await userEvent.click(selectorToggle);

    const secondArcPointer = await canvas.findByTestId('selector-arc-item-1');
    await userEvent.click(secondArcPointer);

    await waitFor(() => {
      expect(selectorToggle).toHaveTextContent('Test 2');
    });
  },
};

export const ClickingArcPointerClosesDropdown: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      <Entry debug={true} />
    </div>
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const selectorToggle = await canvas.findByTestId('selector-test');
    await userEvent.click(selectorToggle);

    await waitFor(() => {
      expect(canvas.getByTestId('selector-dropdown-content')).toBeVisible();
    });

    const firstArcPointer = await canvas.findByTestId('selector-arc-item-0');
    await userEvent.click(firstArcPointer);

    await waitFor(() => {
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument();
    });
  },
};

export const ClickingActionbarButtonClosesDropdown: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      <Entry maxNumberActionbar={3} debug={true} />
    </div>
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const selectorToggle = await canvas.findByTestId('selector-test');
    await userEvent.click(selectorToggle);

    await waitFor(() => {
      expect(canvas.getByTestId('selector-dropdown-content')).toBeVisible();
    });

    const actionbarButtons = await canvas.findAllByTestId('button-test');

    await userEvent.click(actionbarButtons[0]);

    await waitFor(() => {
      expect(canvas.queryByTestId('selector-dropdown-content')).not.toBeInTheDocument();
    });
  },
};

export const RestButtonShowsOptionsAndOptionsClickable: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      <Entry maxNumberActionbar={3} debug={true} />
    </div>
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const selectorToggle = await canvas.findByTestId('selector-test');
    await userEvent.click(selectorToggle);

    const restButton = await canvas.findByTestId('actionbar-rest-button');
    await userEvent.click(restButton);

    const menu = await screen.findByTestId('context_menu');
    expect(menu).toBeVisible();

    const menuItem = within(menu).getByText('Create a new ARC');
    await userEvent.click(menuItem);

    await waitFor(() => {
      expect(screen.queryByTestId('context_menu')).not.toBeInTheDocument();
    });
  },
};
