import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, userEvent, waitFor } from "storybook/test";
import { FileExplorerExample_Example as FileExplorerExample } from "./FileExplorer.fs.js";
import { FileExplorerTestHarness_ARCSelectionHarness as ARCSelectionHarness } from "./FileExplorer.fs.js";

const meta: Meta<typeof FileExplorerExample> = {
  title: "Components/FileExplorer",
  component: FileExplorerExample,
  parameters: {
    layout: "fullscreen",
  },
};

export default meta;
type Story = StoryObj<typeof FileExplorerExample>;

export const Default: Story = {
  render: () => <FileExplorerExample />,

  play: (async ({ canvasElement }: { canvasElement: HTMLElement }) => {
    const canvas = within(canvasElement);

    const container = await canvas.findByTestId("file-explorer-container");
    expect(container).toBeTruthy();

    const resume = await canvas.findByText("resume.pdf");
    expect(resume).toBeTruthy();

    const folder = await canvas.findByText("My Files");
    await userEvent.click(folder);
  }),
};

export const CollapsesUnrelatedFolder: Story = {
  parameters: { isolated: true },
  render: () => <ARCSelectionHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const selected = await canvas.findByTestId("selected-item-id");
    expect(selected).toHaveTextContent("note:protocol");
    await waitFor(() => {
      expect(canvas.getByTestId("file-item-toggle-study:plant")).toBeDisabled();
    });

    expect(canvas.getByTestId("file-item-children-group:runs")).toBeInTheDocument();

    await userEvent.click(canvas.getByTestId("file-item-toggle-group:runs"));

    await waitFor(() => {
      expect(canvas.queryByTestId("file-item-children-group:runs")).not.toBeInTheDocument();
    });

    expect(selected).toHaveTextContent("note:protocol");
  },
};

export const KeepsActiveBranchOpen: Story = {
  parameters: { isolated: true },
  render: () => <ARCSelectionHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const selected = await canvas.findByTestId("selected-item-id");

    await waitFor(() => {
      expect(selected).toHaveTextContent("note:protocol");
      expect(canvas.getByTestId("file-item-toggle-study:plant")).toBeDisabled();
    });

    expect(canvas.getByTestId("file-item-children-study:plant")).toBeInTheDocument();
    expect(canvas.getByTestId("file-item-row-note:protocol")).toBeInTheDocument();
    expect(selected).toHaveTextContent("note:protocol");
  },
};

export const SelectableFolderCanBecomeActive: Story = {
  parameters: { isolated: true },
  render: () => <ARCSelectionHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(() => {
      expect(canvas.getByTestId("file-item-toggle-study:plant")).toBeDisabled();
    });

    await userEvent.click(canvas.getByTestId("file-item-row-run:2026"));

    const selected = await canvas.findByTestId("selected-item-id");

    await waitFor(() => {
      expect(selected).toHaveTextContent("run:2026");
      expect(canvas.getByTestId("file-item-toggle-run:2026")).toBeDisabled();
    });

    const runToggle = canvas.getByTestId("file-item-toggle-run:2026");
    expect(canvas.getByText("Run Result")).toBeInTheDocument();
    expect(runToggle).toBeDisabled();
  },
};

export const NonSelectableGroupDoesNotChangeSelection: Story = {
  parameters: { isolated: true },
  render: () => <ARCSelectionHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const selected = await canvas.findByTestId("selected-item-id");
    expect(selected).toHaveTextContent("note:protocol");

    await userEvent.click(await canvas.findByTestId("file-item-row-group:runs"));

    expect(selected).toHaveTextContent("note:protocol");
  },
};
