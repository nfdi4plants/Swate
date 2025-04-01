import type { Meta, StoryObj } from '@storybook/react';
import { fn, within, expect, userEvent, waitFor, fireEvent } from '@storybook/test';
import Table from "./Table.fs.js";
import React from 'react';

const meta = {
  title: "Components/Table",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'centered',
  },
  component: Table,
} satisfies Meta<typeof Table>;

export default meta;

type Story = StoryObj<typeof meta>;


export const Default: Story = {
  // render: renderTermSearch,
  args: {
    // onTermSelect: fn((term) => console.log(term)),
    // term: undefined,
    // showDetails: true,
    // debug: true
  }
  // play: async ({ args, canvasElement }) => {
  //   const input = within(canvasElement).getByTestId('term-search-input');
  //   expect(input).toBeInTheDocument();
  //   await userEvent.type(input, "instrument model", {delay: 50});

  //   await waitFor(() => expect(args.onTermSelect).toHaveBeenCalled());
  // }
}