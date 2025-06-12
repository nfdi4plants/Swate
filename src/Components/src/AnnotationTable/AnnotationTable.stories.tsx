import type { Meta, StoryObj } from '@storybook/react';
import { fn, within, expect, userEvent, waitFor, fireEvent } from '@storybook/test';
import Table from "./AnnotationTable.fs.js";
import { TIBApi } from '../Util/Api.fs.js';
import React from 'react';
import MockTable from './MockTable.js';

function renderTable(args: any) {
  const [table, setTable] = React.useState(MockTable);

  return (
    <div className='swt:h-[600px]'>
      <Table
        {...args}
        arcTable={table}
        setArcTable={setTable}
      />
    </div>
  );
};


const meta = {
  title: "Components/AnnotationTable",
  tags: ["autodocs"],
  component: Table,
} satisfies Meta<typeof Table>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: renderTable,
  args: {
    height: 600
  },
  play: async ({ args, canvasElement }) => {
    // const input = within(canvasElement).getByTestId('term-search-input');
    // expect(input).toBeInTheDocument();
    // await userEvent.type(input, "instrument model", {delay: 50});

    // await waitFor(() => expect(args.onTermSelect).toHaveBeenCalled());
  }
}