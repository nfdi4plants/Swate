import type { Meta, StoryObj } from '@storybook/react-vite';
import { screen, fn, within, expect, userEvent, waitFor, fireEvent } from 'storybook/test';
import TermSearch from "./TermSearch.fs.js";
import {TIBQueryProvider as TermSearchConfigProvider} from "./TermSearchConfigProvider.fs.js"
import React from 'react';

const TERMSEARCH_INPUT_TESTID = 'term-search-input'

const meta = {
  title: "Components/TermSearch/TermSearchConfigProvider",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'padded',
  },
  async beforeEach() {
    localStorage.clear()
  },
  component: TermSearchConfigProvider,
  subcomponents: {TermSearch},
} satisfies Meta<typeof TermSearchConfigProvider>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => (
    <TermSearchConfigProvider {...args}>
      <TermSearch
        onTermChange={fn((term) => console.log(term))}
        disableDefaultSearch={true}
        disableDefaultParentSearch={true}
        disableDefaultAllChildrenSearch={true}
      />
    </TermSearchConfigProvider>
  ),
  play: async ({ args, canvasElement }) => {

    const input = within(canvasElement).getByTestId(TERMSEARCH_INPUT_TESTID);
    expect(input).toBeInTheDocument();

    await userEvent.type(input, "instrument model", {delay: 50});

    waitFor(() => {
      const debugValue = input.getAttribute("data-debugresultcount")
      expect(debugValue ? parseInt(debugValue, 10) : 0).toBeGreaterThan(0);
    })

  }
}
