import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fn, screen, userEvent, waitFor, within } from "storybook/test";
import { Entry as TermSearchConfigSetter } from "./ConfigSetter.fs.js";
import { DefaultQueryProvider as TermSearchConfigProvider } from "./ConfigProvider.fs.js";
import React from "react";

const SETTER_DEBUG_TESTID = "term-search-config-setter";
const TIB_TRIGGER_TESTID = "term-search-config-setter-TIB-trigger";
const OLS_TRIGGER_TESTID = "term-search-config-setter-OLS-trigger";
const TIB_DEFAULT_KEY = "TIB_DataPLANT";
const TIB_CHEM_KEY = "TIB_NFDI4CHEM";
const OLS_DEFAULT_KEY = "OLS_DataPLANT Project";
const OLS_PLANTS_KEY = "OLS_NFDI4Plants";

const TIB_COLLECTIONS = {
  content: ["DataPLANT", "NFDI4CHEM"],
  numberOfElements: 2,
};

const OLS_COLLECTIONS = [
  { id: "dataplant-id", label: "DataPLANT Project", isPublic: true },
  { id: "nfdi4plants-id", label: "NFDI4Plants", isPublic: true },
];

const meta = {
  title: "Composite Components/TermSearch/TermSearchConfigSetter",
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
  },
  async beforeEach(context) {
    localStorage.clear();
    const originalFetch = globalThis.fetch;

    globalThis.fetch = fn(async (input: RequestInfo | URL) => {
      const url = String(input);

      if (url.includes("/ontologies/schemavalues")) {
        if (context.parameters.tibCollectionRequestFails) {
          throw new Error("TIB collection discovery unavailable");
        }

        return Response.json(TIB_COLLECTIONS);
      }

      if (url.endsWith("/collections/")) {
        return Response.json(OLS_COLLECTIONS);
      }

      throw new Error(`Unexpected request: ${url}`);
    }) as typeof fetch;

    return () => {
      globalThis.fetch = originalFetch;
    };
  },
  component: TermSearchConfigSetter,
  decorators: [
    (Story) => (
      <TermSearchConfigProvider>
        <Story />
      </TermSearchConfigProvider>
    ),
  ],
} satisfies Meta<typeof TermSearchConfigSetter>;

export default meta;

type Story = StoryObj<typeof meta>;

async function openCollectionSelector(trigger: HTMLElement) {
  const selector = trigger.parentElement;
  expect(selector).not.toBeNull();
  await userEvent.click(selector!);
  return screen.findByRole("listbox");
}

function getOption(listbox: HTMLElement, key: string) {
  const option = listbox.querySelector(`[data-selectoption="${key}"]`);

  if (!(option instanceof HTMLElement)) {
    throw new Error(`Expected ${key} option to be rendered.`);
  }

  return option;
}

async function waitForOptions(listbox: HTMLElement, ...keys: string[]) {
  await waitFor(() => {
    keys.forEach((key) => expect(getOption(listbox, key)).toBeInTheDocument());
  });
}

function expectActiveKeys(debug: HTMLElement, ...keys: string[]) {
  expect(debug).toHaveAttribute("data-activekeys", keys.sort().join("; "));
}

export const Default: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const debug = canvas.getByTestId(SETTER_DEBUG_TESTID);
    const tibTrigger = canvas.getByTestId(TIB_TRIGGER_TESTID);
    const olsTrigger = canvas.getByTestId(OLS_TRIGGER_TESTID);

    expect(debug).toHaveAttribute("data-activekeyscount", "2");
    expect(debug).toHaveAttribute("data-defaultdisables", "false");
    expectActiveKeys(debug, OLS_DEFAULT_KEY, TIB_DEFAULT_KEY);
    expect(tibTrigger).toHaveTextContent(TIB_DEFAULT_KEY);
    expect(tibTrigger).not.toHaveTextContent("OLS");
    expect(olsTrigger).toHaveTextContent(OLS_DEFAULT_KEY);
  },
};

export const OLSLoadsIndependentlyFromTIB: Story = {
  parameters: {
    tibCollectionRequestFails: true,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const trigger = canvas.getByTestId(OLS_TRIGGER_TESTID);
    const listbox = await openCollectionSelector(trigger);

    await waitForOptions(listbox, OLS_DEFAULT_KEY, OLS_PLANTS_KEY);
  },
};

export const SelectOLSCollection: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const debug = canvas.getByTestId(SETTER_DEBUG_TESTID);
    const trigger = canvas.getByTestId(OLS_TRIGGER_TESTID);
    const listbox = await openCollectionSelector(trigger);

    await waitForOptions(listbox, OLS_DEFAULT_KEY, OLS_PLANTS_KEY);
    await userEvent.click(getOption(listbox, OLS_PLANTS_KEY));
    await userEvent.keyboard("{Escape}");

    await waitFor(() => {
      expectActiveKeys(debug, OLS_DEFAULT_KEY, OLS_PLANTS_KEY, TIB_DEFAULT_KEY);
      expect(trigger).toHaveTextContent(`${OLS_DEFAULT_KEY}, ${OLS_PLANTS_KEY}`);
    });
  },
};

export const SetMultipleTIBQueries: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const debug = canvas.getByTestId(SETTER_DEBUG_TESTID);
    const trigger = canvas.getByTestId(TIB_TRIGGER_TESTID);
    const listbox = await openCollectionSelector(trigger);

    await waitForOptions(listbox, TIB_CHEM_KEY);
    await userEvent.click(getOption(listbox, TIB_CHEM_KEY));
    await userEvent.keyboard("{Escape}");

    await waitFor(() => {
      expectActiveKeys(debug, OLS_DEFAULT_KEY, TIB_DEFAULT_KEY, TIB_CHEM_KEY);
    });
  },
};
