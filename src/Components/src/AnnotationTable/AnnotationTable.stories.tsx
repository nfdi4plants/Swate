import type { Meta, StoryObj } from '@storybook/react-vite';
import { fn, within, expect, userEvent, waitFor, fireEvent } from 'storybook/test';
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
    await expect(contextMenu).toBeVisible();
  }
}

export const ContextMenu: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-1');

    fireEvent.contextMenu(cell);

    await waitFor(() => {
      const contextMenu = screen.getByTestId('context_menu');
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const detailsButton = within(contextMenu).getByRole('button', { name: /Details/d });
    await expect(detailsButton).toBeVisible();

    await userEvent.click(detailsButton);

    await waitFor(() => {
      const modal = screen.getByTestId('modal_Details_FreeText');
      expect(modal).toBeVisible();
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const detailsButton = within(contextMenu).getByRole('button', { name: /Details/d });
    await expect(detailsButton).toBeVisible();

    await userEvent.click(detailsButton);

    await waitFor(() => {
      const modal = screen.getByTestId('modal_Details_FreeText');
      expect(modal).toBeVisible();
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const detailsButton = within(contextMenu).getByRole('button', { name: /Details/d });
    await expect(detailsButton).toBeVisible();

    await userEvent.click(detailsButton);

    await waitFor(() => {
      const modal = screen.getByTestId('modal_Details_Term');
      expect(modal).toBeVisible();
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const detailsButton = within(contextMenu).getByRole('button', { name: /Details/d });
    await expect(detailsButton).toBeVisible();

    await userEvent.click(detailsButton);

    await waitFor(() => {
      const modal = screen.getByTestId('modal_Details_Unitized');
      expect(modal).toBeVisible();
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Edit/i });
    await expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const modal = screen.getByTestId('modal_Edit');
      expect(modal).toBeVisible();
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Edit/d });
    await expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const editModal = screen.getByTestId('modal_Edit');
      expect(editModal).toBeVisible();
    });

    const editModal = screen.getByTestId('modal_Edit');
    await expect(editModal).toBeVisible();

    const createButton = await within(editModal).findByText(/Create Column/i);
    await expect(createButton).toBeVisible();

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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Edit/d });
    await expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const editModal = screen.getByTestId('modal_Edit');
      expect(editModal).toBeVisible();
    });

    const editModal = screen.getByTestId('modal_Edit');
    await expect(editModal).toBeVisible();

    const createButton = await within(editModal).findByText(/Update Column/i);
    await expect(createButton).toBeVisible();

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

    const table = screen.getByTestId('annotation_table');
    await expect(table).toBeVisible();

    const oldColumnLength = table.getAttribute("data-columnCount")
    await expect(oldColumnLength).toBe("6");

    const cell = await canvas.findByTestId('cell-1-1');

    await fireEvent.contextMenu(cell);

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const deleteButton = within(contextMenu).getByRole('button', { name: /Delete Column/d });
    await expect(deleteButton).toBeVisible();
    await userEvent.click(deleteButton);
    const newTable = screen.getByTestId('annotation_table');
    await expect(newTable).toBeVisible();

    const newColumnLength = newTable.getAttribute("data-columnCount")
    await expect(newColumnLength).toBe("5");
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    const editButton = within(contextMenu).getByRole('button', { name: /Delete Row/d });
    await expect(editButton).toBeVisible();

    await userEvent.click(editButton);

    await waitFor(() => {
      const table = screen.getByTestId('annotation_table');
      expect(table).toBeVisible();

      const rows = within(table).getAllByRole('row');
      expect(rows).toHaveLength(17);
    });
  }
}

export const ActivateTermSearchContainer: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-3');

    await userEvent.click(cell);
    await userEvent.keyboard('T');

    const input = within(canvasElement).getByTestId('term-search-input');

    await userEvent.clear(input);
    await userEvent.type(input, 'Temperature', {delay: 50});

    await waitFor(() => {
      const termDropdown = screen.getByTestId('term_dropdown');
      expect(termDropdown).toBeVisible();
    }, { timeout: 5000 });
  }
}

export const TermDetailsKeyboard: Story = {
  render: renderTable,
  args: {
    height: 600,
    witdth: 1000,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);

    const cell = await canvas.findByTestId('cell-1-3');

    await userEvent.click(cell);

    await userEvent.keyboard('{Control>}{Enter}{/Control}');

    await waitFor(() => {
      const modal = screen.getByTestId('modal_Details_Term');
      expect(modal).toBeVisible();
    });
  }
}

export const FreeTextDetailsKeyboardActivation: Story = {
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    await userEvent.keyboard('d');
    await userEvent.keyboard('{Enter}');

    await waitFor(() => {
      const modal = screen.getByTestId('modal_Details_FreeText');
      expect(modal).toBeVisible();
    });
  }
}

export const EditColumnKeyboardActivation: Story = {
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

    const contextMenu = screen.getByTestId('context_menu');
    await expect(contextMenu).toBeVisible();

    await userEvent.keyboard('e');
    await userEvent.keyboard('{Enter}');

    await waitFor(() => {
      const modal = screen.getByTestId('modal_Edit');
      expect(modal).toBeVisible();
    });
  }
}