import type { Meta, StoryObj } from '@storybook/react-vite';
import { screen, fn, within, expect, userEvent, waitFor, fireEvent } from 'storybook/test';
import {Entry as TermSearchConfigSetter} from "./TermSearchConfigSetter.fs.js";
import {TIBQueryProvider as TermSearchConfigProvider} from "./TermSearchConfigProvider.fs.js"
import React from 'react';

const SETTER_DEBUG_TESTID = "term-search-config-setter"

const SETTER_TRIGGER_TESTID = "term-search-config-setter-tib-trigger"

const meta = {
  title: "Components/TermSearch/TermSearchConfigSetter",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'centered',
  },
  async beforeEach() {
    localStorage.clear();
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

export const Default: Story = {
  play: async ({ args, canvasElement }) => {

    // const input = within(canvasElement).getByTestId(SETTER_TRIGGER_TESTID);
    const debug = within(canvasElement).getByTestId(SETTER_DEBUG_TESTID);


    // read the attributes (strings!)
    const activeKeysCount = parseInt(debug.getAttribute('data-activekeyscount') || '0', 10);
    const disableDefault = debug.getAttribute('data-defaultdisables') === 'true';
    const activeKeys = debug.getAttribute('data-activekeys') || "";

    // assertions
    expect(activeKeysCount).toBe(1);
    expect(disableDefault).toBe(false);
    expect(activeKeys).toBe("TIB_DataPLANT");
  }
}



export const SetMultipleTIBQueries: Story = {
  play: async ({ args, canvasElement }) => {

    const debug = within(canvasElement).getByTestId(SETTER_DEBUG_TESTID);
    const trigger = within(canvasElement).getByTestId(SETTER_TRIGGER_TESTID);
    trigger.click();

    await waitFor(async () => {
      const box = screen.getByRole('listbox');
      expect(box).toBeInTheDocument()
      const option = box.querySelector('[data-selectoption="TIB_NFDI4CHEM"]')
      expect(option).toBeInTheDocument()
      expect(option).toBeTruthy()
      await userEvent.click(option as HTMLElement);
      await userEvent.keyboard('{Escape}');
    }, { timeout: 5000 })


    // read the attributes (strings!)
    const activeKeysCount = parseInt(debug.getAttribute('data-activekeyscount') || '0', 10);
    const disableDefault = debug.getAttribute('data-defaultdisables') === 'true';
    const activeKeys = debug.getAttribute('data-activekeys') || "";

    // assertions
    expect(activeKeysCount).toBe(2);
    expect(disableDefault).toBe(false);
    expect(activeKeys).toBe("TIB_DataPLANT; TIB_NFDI4CHEM");
  }
}