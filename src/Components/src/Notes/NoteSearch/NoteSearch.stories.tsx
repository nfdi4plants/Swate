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

    const searchInput = await canvas.findByPlaceholderText("Search Notes...");
    expect(searchInput).toBeTruthy();
    await userEvent.click(searchInput);
    await userEvent.type(searchInput, "Grocery Planning");

    const planningTag = await canvas.findByText("Planning");
    expect(planningTag).toBeVisible();

    const renderedTags = await canvas.findAllByTestId("notes-search-tag");
    expect(renderedTags.length).toBeGreaterThan(0);

    for (const tag of renderedTags) {
      expect((tag.textContent ?? "").trim().length).toBeGreaterThan(0);
    }

  }),
};
