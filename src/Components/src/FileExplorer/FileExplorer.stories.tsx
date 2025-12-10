import React from "react";
import type { Meta, StoryObj, PlayFunction } from "@storybook/react";
import { within, expect, userEvent } from "@storybook/test";
import { Example as FileExplorerExample } from "./FileExplorerExample.fs";

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
  }) as PlayFunction,
};
