import { describe, expect, test, vi } from "vitest";
import { act, render, screen, fireEvent, waitFor } from "@testing-library/react";
import { Fragment } from "react/jsx-runtime";
import TermSearch from './TermSearchV2.fs.js'

describe('TermSearch', () => {

  test('renders TermSearch with advancedSearch input and responds to changes', async () => {
    const setTermMock = vi.fn();
    const term = undefined;
    render(
      <TermSearch
        onTermSelect={setTermMock}
        term={term}
        parentId="test:xx"
        showDetails={true}
        debug={true}
      />
    );

    const input = screen.getByTestId("term-search-input");
    expect(input).toBeInTheDocument();

    fireEvent.change(input, { target: { value: "test" } });
    fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

    expect(setTermMock).toHaveBeenCalled();
  });

  test("calls the custom advanced search function with the correct input", async () => {
    const searchMock = vi.fn(() => Promise.resolve([])); // Mock the Search function
    const advancedSearch = {
      input: "test",
      search: searchMock,
      form: (controller: { startSearch: (() => void), cancel: (() => void) }) => (
        <input
          data-testid="advanced-search-input"
          type="text"
          onKeyDown={(e) => e.code === "Enter" ? controller.startSearch() : null}
        />
      ),
    };

    render(
      <TermSearch
        term={undefined}
        onTermSelect={() => {}}
        advancedSearch={advancedSearch}
        showDetails={false}
        debug={false}
      />
    );

    const indicator = screen.getByTestId("advanced-search-indicator");
    expect(indicator).toBeInTheDocument();

    fireEvent.click(indicator); // Open the Advanced Search

    const modal = await waitFor(() => screen.getByTestId("advanced-search-modal"));
    expect(modal).toBeInTheDocument();

    const input = screen.getByTestId("advanced-search-input");

    await act(async () => { // this will do internal react updates so we must wrap in "act"
      fireEvent.keyDown(input, { key: "Enter", code: "Enter" }); // Simulate Enter key
    });

    expect(searchMock).toHaveBeenCalledTimes(1); // Assert Search was called
    expect(searchMock).toHaveBeenCalledWith("test"); // Assert it was called with correct input
  });
});
