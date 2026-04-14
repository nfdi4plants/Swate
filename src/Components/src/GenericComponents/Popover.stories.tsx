import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, screen, userEvent, waitFor, within } from "storybook/test";
import React, { useState } from "react";
import {
  Popover,
  Trigger,
  TriggerRender,
  Content,
  Heading,
  Description,
  Close,
} from "./Popover.fs.js";
// NOTE: Fable dev mode (--lang ts) generates `.fs.ts` files colocated with
// source. The `.fs.js` extension in the import is resolved by Vite's
// TypeScript handling — no literal `.fs.js` file is created during dev.

const TESTID_CUSTOM_TRIGGER = "popover_custom_trigger";

const BasicPopoverExample = () => (
  <Popover debug="basic">
    <Trigger>Open details</Trigger>
    <Content>
      <Heading>Dataset actions</Heading>
      <Description>Choose what should happen to this dataset.</Description>
      <button className="swt:btn swt:btn-sm">Keep open</button>
      <Close>Close</Close>
    </Content>
  </Popover>
);

const ControlledPopoverExample = () => {
  const [open, setOpen] = useState(false);

  return (
    <div className="swt:flex swt:flex-col swt:items-start swt:gap-3">
      <button className="swt:btn swt:btn-primary" onClick={() => setOpen(true)}>
        Open from outside
      </button>
      <Popover open={open} onOpenChange={setOpen} debug="controlled">
        <Trigger>Toggle controlled</Trigger>
        <Content>
          <Heading>Controlled popover</Heading>
          <Description>The host owns the open state.</Description>
          <Close>Done</Close>
        </Content>
      </Popover>
    </div>
  );
};

const CustomTriggerExample = () => (
  <Popover debug="custom-trigger">
    <TriggerRender
      render={({ isOpen, setReference, referenceProps }) => (
        <button
          ref={setReference}
          data-state={isOpen ? "open" : "closed"}
          data-testid={TESTID_CUSTOM_TRIGGER}
          className="swt:btn swt:btn-secondary"
          {...referenceProps}
        >
          Custom trigger
        </button>
      )}
    />
    <Content>
      <Heading>Custom trigger popover</Heading>
      <Description>The trigger applied the Floating UI props itself.</Description>
      <Close>Close custom</Close>
    </Content>
  </Popover>
);

const NonModalPopoverExample = () => (
  <div className="swt:flex swt:items-center swt:gap-4">
    <Popover modal={false} debug="non-modal">
      <Trigger>Open non-modal</Trigger>
      <Content>
        <Heading>Non-modal popover</Heading>
        <Description>Focus can move outside this popover.</Description>
        <button className="swt:btn swt:btn-sm">Focusable content</button>
      </Content>
    </Popover>
    <button className="swt:btn" type="button">
      Outside target
    </button>
  </div>
);

const meta = {
  title: "Components/GenericComponents/Popover",
  tags: ["autodocs"],
  parameters: {
    layout: "centered",
    viewport: { defaultViewport: "responsive" },
  },
  component: BasicPopoverExample,
} satisfies Meta<typeof BasicPopoverExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <BasicPopoverExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByRole("button", { name: /open details/i }));

    const dialog = await screen.findByRole("dialog", { name: /dataset actions/i });
    expect(dialog).toHaveAttribute("data-state", "open");
    expect(screen.getByText(/choose what should happen/i)).toBeInTheDocument();

    await userEvent.keyboard("{Escape}");
    await waitFor(() =>
      expect(screen.queryByRole("dialog", { name: /dataset actions/i })).not.toBeInTheDocument(),
    );
  },
};

export const Controlled: Story = {
  render: () => <ControlledPopoverExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByRole("button", { name: /open from outside/i }));

    const dialog = await screen.findByRole("dialog", { name: /controlled popover/i });
    expect(dialog).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: /done/i }));
    await waitFor(() =>
      expect(screen.queryByRole("dialog", { name: /controlled popover/i })).not.toBeInTheDocument(),
    );
  },
};

export const CustomTrigger: Story = {
  render: () => <CustomTriggerExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const trigger = canvas.getByTestId(TESTID_CUSTOM_TRIGGER);

    expect(trigger).toHaveAttribute("data-state", "closed");
    await userEvent.click(trigger);
    expect(trigger).toHaveAttribute("data-state", "open");
    expect(await screen.findByRole("dialog", { name: /custom trigger popover/i })).toBeInTheDocument();
  },
};

export const NonModal: Story = {
  render: () => <NonModalPopoverExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByRole("button", { name: /open non-modal/i }));
    expect(await screen.findByRole("dialog", { name: /non-modal popover/i })).toBeInTheDocument();

    await userEvent.click(canvas.getByRole("button", { name: /outside target/i }));
    await waitFor(() =>
      expect(screen.queryByRole("dialog", { name: /non-modal popover/i })).not.toBeInTheDocument(),
    );
  },
};
