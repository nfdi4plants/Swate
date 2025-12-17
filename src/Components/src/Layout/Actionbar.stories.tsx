import type { Meta, StoryObj } from '@storybook/react-vite';
import { screen, fn, within, expect, userEvent, waitFor, fireEvent } from 'storybook/test';
import Layout from "./Layout.fs.js";
import {LayoutBtn, LeftSidebarToggleBtn} from "./Layout.fs.js";
import React from 'react';
import { ActionbarInSelectorEntry } from '../ARCSelector/Selector.fs.js';
import { Entry as ActionbarEntry } from '../GenericComponents/Actionbar.fs.js';

const meta = {
  title: "Components/Layout/Actionbar",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'fullscreen',
  },
  component: Layout,
} satisfies Meta<typeof Layout>;

export default meta;

type Story = StoryObj<typeof meta>;

export const DisplayActionbar: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      < ActionbarEntry maxNumber = {3} debug={true} />
    </div>
  }
}

export const DisplayActionbarInSelector: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      < ActionbarInSelectorEntry maxNumber = {5} maxNumberActionbar = {3} debug={true} />
    </div>
  }
}

export const DisplayActionbarWithoutRestElementButton: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      < ActionbarEntry maxNumber = {5} debug={true} />
    </div>
  }
}

export const DisplayActionbarWithoutRestElementButtonInSelector: Story = {
  args: {
    children: <div className="swt:flex swt:items-center swt:justify-center swt:h-full">
      < ActionbarInSelectorEntry maxNumber = {5} maxNumberActionbar = {5} debug={true} />
    </div>
  }
}
