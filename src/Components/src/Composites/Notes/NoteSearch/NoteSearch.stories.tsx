import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, userEvent } from "storybook/test";
import {Entry as NoteSearch } from "./NoteSearch.fs.js";

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

    // Find search input
    const searchInput = await canvas.findByPlaceholderText("Search Notes...");
    expect(searchInput).toBeTruthy();

  }),
};
