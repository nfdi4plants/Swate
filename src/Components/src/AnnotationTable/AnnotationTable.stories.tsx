import type { Meta, StoryObj } from '@storybook/react';
import { fn, within, expect, userEvent, waitFor, fireEvent } from '@storybook/test';
import { screen } from "@storybook/testing-library";
import Table from "./AnnotationTable.fs.js";
import { TIBApi } from '../Util/Api.fs.js';
import React from 'react';
import MockTable from './MockTable.js';

function renderTable(args: any) {
  const [table, setTable] = React.useState(MockTable.Copy());

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
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const contextMenu = canvas.getByTestId('annotation_table');
    expect(contextMenu).toBeVisible();
  }
}

export const ContextMenuBody: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true,
    testId: "body"
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-1');

    fireEvent.contextMenu(cell);

    await waitFor(() => {
      const contextMenu = screen.getByTestId('context_menu_body');
      expect(contextMenu).toBeVisible();
    });
  }
}

export const ContextMenuHeader: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true,
    testId: "header"
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-0-1');

    await fireEvent.contextMenu(cell);

    await waitFor(() => {
      const contextMenu = screen.getByTestId('context_menu_header');
      expect(contextMenu).toBeVisible();
    });
  }
}

export const ContextMenuIndex: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true,
    testId: "index"
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-0-0');

    await fireEvent.contextMenu(cell);

    await waitFor(() => {
      const contextMenu = screen.getByTestId('context_menu_index');
      expect(contextMenu).toBeVisible();
    });
  }
}

export const FreeTextDetails: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-1');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const detailsButton = within(contextMenu).getByRole('button', { name: /Details/d });
    expect(detailsButton).toBeVisible();

    await userEvent.click(detailsButton);

    await waitFor(() => {
      const contextMenu = screen.getByTestId('modal_Details_FreeText');
      expect(contextMenu).toBeVisible();
    });
  }
}

export const FreeText2Details: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-2');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const detailsButton = within(contextMenu).getByRole('button', { name: /Details/d });
    expect(detailsButton).toBeVisible();

    await userEvent.click(detailsButton);

    await waitFor(() => {
      const contextMenu = screen.getByTestId('modal_Details_FreeText');
      expect(contextMenu).toBeVisible();
    });
  }
}

export const TermDetails: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-3');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const detailsButton = within(contextMenu).getByRole('button', { name: /Details/d });
    expect(detailsButton).toBeVisible();

    await userEvent.click(detailsButton);

    await waitFor(() => {
      const contextMenu = screen.getByTestId('modal_Details_Term');
      expect(contextMenu).toBeVisible();
    });
  }
}

export const UnitizedDetails: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-5');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const detailsButton = within(contextMenu).getByRole('button', { name: /Details/d });
    expect(detailsButton).toBeVisible();

    await userEvent.click(detailsButton);

    await waitFor(() => {
      const contextMenu = screen.getByTestId('modal_Details_Unitized');
      expect(contextMenu).toBeVisible();
    });
  }
}

export const EditColumn: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-1');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Edit/i });
    expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const editModal = screen.getByTestId('modal_Edit');
      expect(editModal).toBeVisible();
    });
  }
}

export const CreateColumn: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-1');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Edit/d });
    expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const editModal = screen.getByTestId('modal_Edit');
      expect(editModal).toBeVisible();
    });

    const editModal = screen.getByTestId('modal_Edit');
    expect(editModal).toBeVisible();

    const createButton = await within(editModal).findByText(/Create Column/i);
    expect(createButton).toBeVisible();

    await userEvent.click(createButton);

    await waitFor(() => {
      const createTab = screen.getByTestId('Create Column');
      expect(createTab).toBeVisible();
    });
  }
}

export const UpdateColumn: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-1');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Edit/d });
    expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const editModal = screen.getByTestId('modal_Edit');
      expect(editModal).toBeVisible();
    });

    const editModal = screen.getByTestId('modal_Edit');
    expect(editModal).toBeVisible();

    const createButton = await within(editModal).findByText(/Update Column/i);
    expect(createButton).toBeVisible();

    await userEvent.click(createButton);

    await waitFor(() => {
      const createTab = screen.getByTestId('Update Column');
      expect(createTab).toBeVisible();
    });
  }
}

export const DeleteColumn: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-1');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Delete Column/d });
    expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const table = screen.getByTestId('annotation_table');
      expect(table).toBeVisible();

      const rows = within(table).getAllByRole('row');
      const headerRow = rows[0];
      const columns = within(headerRow).getAllByRole('columnheader');
      expect(columns).toHaveLength(5);
    });
  }
}

export const DeleteRow: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-1');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu_body');
    expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Delete Row/d });
    expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const table = screen.getByTestId('annotation_table');
      expect(table).toBeVisible();

      const rows = within(table).getAllByRole('row');
      expect(rows).toHaveLength(17);
    });
  }
}