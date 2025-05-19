import type { Meta, StoryObj } from '@storybook/react';
import { fn, within, expect, userEvent, waitFor, fireEvent } from '@storybook/test';
import TermSearch from "./TermSearch.fs.js";
import { TIBApi } from '../Util/Api.fs.js';
import React from 'react';

function renderTermSearch(args: any) {
  const [term, setTerm] = React.useState(undefined as Term | undefined);

  return (
    <div className='container mx-auto flex flex-col p-2 gap-4 h-[400px]'>
      <TermSearch
        {...args}
        term={term}
        onTermSelect={(selectedTerm) => {
          setTerm(selectedTerm);
          args.onTermSelect(selectedTerm); // Call mock or external handler
        }}
      />
    </div>
  );
};

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
  render: renderTermSearch,
  args: {
    onTermSelect: fn((term) => console.log(term)),
    term: undefined,
    showDetails: true,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const input = within(canvasElement).getByTestId('term-search-input');
    expect(input).toBeInTheDocument();
    await userEvent.type(input, "instrument model", {delay: 50});

    await waitFor(() => expect(args.onTermSelect).toHaveBeenCalled());
  }
}

export const ParentSearch: Story = {
  render: renderTermSearch,
  parameters: {isolated: true},
  args: {
    onTermSelect: fn((term) => console.log(term)),
    term: undefined,
    parentId: "MS:1000031",
    showDetails: true,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByTestId('term-search-input');
    expect(input).toBeInTheDocument();

    await userEvent.type(input, "SCIEX", {delay: 50});

    await waitFor(() => expect(args.onTermSelect).toHaveBeenCalled());

    await waitFor(() => { // await api call response
      const directedOutput = canvas.getByText("SCIEX instrument model");
      expect(directedOutput).toBeInTheDocument();
      const expectedIcons = canvas.getAllByTitle("Directed Search");
      expect(expectedIcons.length).toBeGreaterThan(0);
    }, { timeout: 3000 });
  }
}


const DefaultAdvancedSearch: Story = {
  render: renderTermSearch,
  parameters: {isolated: true},
  args: {
    onTermSelect: fn((term) => console.log(term)),
    term: undefined,
    parentId: "MS:1000031",
    showDetails: true,
    advancedSearch: true,
    debug: true
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    const indicator = canvas.getByTestId("advanced-search-indicator");
    expect(indicator).toBeInTheDocument();
    userEvent.click(indicator);

    const modal = await waitFor(() => canvas.getByTestId("modal_advanced-search-modal"));
    expect(modal).toBeInTheDocument();

    const input = canvas.getByTestId("advanced-search-term-name-input");
    expect(input).toBeInTheDocument();

    await userEvent.type(input, "instrument", {delay: 50});
    await fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

    await waitFor(() => {
      const r = canvas.getByText("Instrument Model")
      expect(r).toBeInTheDocument()
    });
  }
}

function customAdvancedSearch (input: string, setInput: (value: string) => void) {
  return {
    search: () =>
      Promise.resolve([
        {
          name: input,
          id: "TST:00001",
          description: "Test Term",
          isObsolete: true,
          data: { test1: "Hello", test2: "World" },
        },
      ]),
    form: (controller: { startSearch: () => void; cancel: () => void }) => (
      <input
        className="input input-bordered"
        data-testid="advanced-search-input"
        type="text"
        onChange={(e) => setInput(e.target.value)}
        onKeyDown={(e) => (e.code === "Enter" ? controller.startSearch() : null)}
      />
    ),
  }
};

function renderCustomAdvancedTermSearch(args: any) {
  const [term, setTerm] = React.useState(undefined as Term | undefined);
  const [input, setInput] = React.useState("");

  // Create advancedSearch dynamically
  const advancedSearchInstance = customAdvancedSearch(input, setInput);

  return (
    <div className='container mx-auto flex flex-col p-2 gap-4 h-[400px]'>
      <TermSearch
        {...args}
        term={term}
        onTermSelect={(selectedTerm) => {
          setTerm(selectedTerm);
          args.onTermSelect(selectedTerm); // Call mock or external handler
        }}
        advancedSearch={advancedSearchInstance}
      />
    </div>
  );
};


/**
 * Advanced search with a custom form
 *
 * ```tsx
 * const advancedSearch = (input: string, setInput: (value: string) => void) => ({
 *   search: () =>
 *     Promise.resolve([
 *       {
 *         name: input,
 *         id: "TST:00001",
 *         description: "Test Term",
 *         isObsolete: true,
 *         data: { test1: "Hello", test2: "World" },
 *       },
 *     ]),
 *   form: (controller: { startSearch: () => void; cancel: () => void }) => (
 *     <input
 *       className="input input-bordered"
 *       data-testid="advanced-search-input"
 *       type="text"
 *       onChange={(e) => setInput(e.target.value)}
 *       onKeyDown={(e) => (e.code === "Enter" ? controller.startSearch() : null)}
 *     />
 *   ),
 * });
 *
 * function renderAdvancedTermSearch(args: any) {
 *   const [term, setTerm] = React.useState(undefined as Term | undefined);
 *   const [input, setInput] = React.useState("");
 *
 *   // Create advancedSearch dynamically
 *   const advancedSearchInstance = advancedSearch(input, setInput);
 *
 *   return (
 *     <div className='container mx-auto flex flex-col p-2 gap-4 h-[400px]'>
 *       <TermSearch
 *         {...args}
 *         term={term}
 *         onTermSelect={(selectedTerm) => {
 *           setTerm(selectedTerm);
 *           args.onTermSelect(selectedTerm); // Call mock or external handler
 *         }}
 *         advancedSearch={advancedSearchInstance}
 *       />
 *     </div>
 *   );
 * };
 * ```
 **/
export const CustomAdvancedSearch: Story = {
  render: renderCustomAdvancedTermSearch,
  args: {
    term: undefined,
    onTermSelect: fn((term) => console.log(term)),
    showDetails: false,
    debug: true
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const indicator = canvas.getByTestId("advanced-search-indicator");
    expect(indicator).toBeInTheDocument();
    userEvent.click(indicator);

    const modal = await waitFor(() => canvas.getByTestId("modal_advanced-search-modal"));
    expect(modal).toBeInTheDocument();

    const input = canvas.getByTestId("advanced-search-input");
    expect(input).toBeInTheDocument();

    await userEvent.type(input, "test input", {delay: 50});
    await fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

    await waitFor(() => {
      const r = canvas.getByText("test input")
      expect(r).toHaveClass("line-through")
      expect(r).toBeInTheDocument()
    });
  }
}

export const TIBSearch: Story = {
  render: renderTermSearch,
  args: {
    term: undefined,
    parentId: "MS:1000031",
    onTermSelect: fn((term) => console.log(term)),
    debug: true,
    disableDefaultSearch: true,
    disableDefaultParentSearch: true,
    disableDefaultAllChildrenSearch: true,
    termSearchQueries: [
      ["tib_search", (query) => TIBApi.defaultSearch(query, 10, "DataPLANT")]
    ],
    parentSearchQueries: [
      ["tib_search", ([parentId, query]) => TIBApi.searchChildrenOf(query, parentId, 10, "DataPLANT")]
    ],
    allChildrenSearchQueries: [
      ["tib_search", (parentId) => TIBApi.searchAllChildrenOf(parentId, 500, "DataPLANT")]
    ]
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByTestId('term-search-input');
    expect(input).toBeInTheDocument();

    await userEvent.type(input, "SCIEX", {delay: 50});

    await waitFor(() => expect(args.onTermSelect).toHaveBeenCalled());

    await waitFor(() => { // await api call response
      const directedOutput = canvas.getByText("SCIEX instrument model");
      expect(directedOutput).toBeInTheDocument();
      const expectedIcons = canvas.getAllByTitle("Directed Search");
      expect(expectedIcons.length).toBeGreaterThan(0);
    }, { timeout: 3000 });

    await userEvent.clear(input);

    await userEvent.dblClick(input);

    await waitFor(() => { // await api call response
      const debugContainer = canvas.getByTestId("term-search-container");
      expect(debugContainer).toBeInTheDocument();
      const debugValue = debugContainer.getAttribute("data-debug-searchresults")

      try {
        const parsedData = JSON.parse(debugValue!);
        expect(Array.isArray(parsedData)).toBe(true); // f# discriminated unions are serialized as arrays
        const searchResults = parsedData[1]
        expect(Array.isArray(searchResults)).toBe(true); // element should be result array
        expect(searchResults.length).toBeGreaterThan(100);
      } catch (error) {
        throw new Error(`Failed to parse data-debug-searchresults: ${debugValue}`);
      }

    }, { timeout: 3000 });
  }
}