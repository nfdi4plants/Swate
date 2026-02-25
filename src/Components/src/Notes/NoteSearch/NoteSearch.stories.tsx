import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, userEvent } from "storybook/test";
import {Entry as NoteSearch } from "./NoteSearchComponent.fs.js";

const meta: Meta<typeof NoteSearch> = {
  title: "Components/NoteSearch",
  component: NoteSearch,
  parameters: {
    layout: "fullscreen",
  },
};

export default meta;
type Story = StoryObj<typeof NoteSearch>;

export const Default: Story = {
  render: () => <NoteSearch />,

  play: (async ({ canvasElement }: { canvasElement: HTMLElement }) => {
    const canvas = within(canvasElement);

    const toggleButton = await canvas.findByText("Toggle Search");
    expect(toggleButton).toBeTruthy();

    // Click toggle to show search
    await userEvent.click(toggleButton);

    // Find search input
    const searchInput = await canvas.findByPlaceholderText("Search Notes...");
    expect(searchInput).toBeTruthy();

  }),
};
