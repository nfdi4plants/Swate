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
  tags: ['skip-test'],
  play: async ({ args, canvasElement }) => {

    const canvas = within(canvasElement);
    const debug = await canvas.findByTestId(SETTER_DEBUG_TESTID, {}, { timeout: 10000 });
    const trigger = await canvas.findByTestId(SETTER_TRIGGER_TESTID, {}, { timeout: 10000 });

    await waitFor(() => {
      expect(getComputedStyle(trigger).pointerEvents).not.toBe("none");
    }, { timeout: 10000 });

    await userEvent.click(trigger);

    const box = await screen.findByRole('listbox', {}, { timeout: 10000 });
    const option = await waitFor(() => {
      const currentOption = box.querySelector('[data-selectoption="TIB_NFDI4CHEM"]');

      if (!(currentOption instanceof HTMLElement)) {
        throw new Error("Expected TIB_NFDI4CHEM option to be rendered.");
      }

      return currentOption;
    }, { timeout: 10000 });

    await userEvent.click(option);
    await userEvent.keyboard('{Escape}');

    await waitFor(() => {
      const activeKeysCount = parseInt(debug.getAttribute('data-activekeyscount') || '0', 10);
      const disableDefault = debug.getAttribute('data-defaultdisables') === 'true';
      const activeKeys = debug.getAttribute('data-activekeys') || "";

      expect(activeKeysCount).toBe(2);
      expect(disableDefault).toBe(false);
      expect(activeKeys).toBe("TIB_DataPLANT; TIB_NFDI4CHEM");
    }, { timeout: 10000 });
  }
}
