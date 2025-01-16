import type { Meta, StoryObj } from '@storybook/react';
import { fn, within, expect, userEvent, waitFor, fireEvent } from '@storybook/test';
import TermSearch from "./TermSearchV2.fs.js";

const meta = {
  title: "Components/TermSearch",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'centered',
  },
  component: TermSearch,
} satisfies Meta<typeof TermSearch>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    onTermSelect: fn((term) => console.log(term)),
    term: undefined,
    parentId: "test:xx",
    showDetails: true,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const input = within(canvasElement).getByTestId('term-search-input');
    expect(input).toBeInTheDocument();
    userEvent.type(input, "instrument model", {delay: 50});

    await waitFor(() => expect(args.onTermSelect).toHaveBeenCalled());
  }
}

const advancedSearch = {
  input: "test",
  search: fn(() => Promise.resolve([{name: "test", id: "TST:00001", description: "Test Term", isObsolete: true}])),
  form: (controller: { startSearch: (() => void), cancel: (() => void) }) => (
    <input
      data-testid="advanced-search-input"
      type="text"
      onKeyDown={(e) => e.code === "Enter" ? controller.startSearch() : null}
    />
  ),
};

export const AdvancedSearch: Story = {
  args: {
    term: undefined,
    onTermSelect: fn((term) => console.log(term)),
    advancedSearch: advancedSearch,
    showDetails: false,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    const indicator = canvas.getByTestId("advanced-search-indicator");
    expect(indicator).toBeInTheDocument();
    userEvent.click(indicator);

    const modal = await waitFor(() => canvas.getByTestId("advanced-search-modal"));
    expect(modal).toBeInTheDocument();

    const input = canvas.getByTestId("advanced-search-input");
    expect(input).toBeInTheDocument();

    await userEvent.type(input, "test", {delay: 50});
    await fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

    await expect(args.advancedSearch.search).toHaveBeenCalled()
    await expect(args.advancedSearch.search).toHaveBeenCalledWith("test")
  }
}