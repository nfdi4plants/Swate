import type { Meta, StoryObj } from '@storybook/react-vite';
import { screen, fn, within, expect, userEvent, waitFor, fireEvent } from 'storybook/test';
import Layout from "./Layout.fs.js";
import {LayoutBtn, LeftSidebarToggleBtn} from "./Layout.fs.js";
import React from 'react';
import { Entry as SelectorEntry } from '../ARCSelector/Selector.fs.js';

const meta = {
  title: "Components/Layout/Selector",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'fullscreen',
  },
  component: Layout,
} satisfies Meta<typeof Layout>;

export default meta;

type Story = StoryObj<typeof meta>;

export const DisplaySelector: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      < SelectorEntry maxNumber = {5} debug={true} />
    </div>
  }
}
