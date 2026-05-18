import type { Meta, StoryObj } from '@storybook/react-vite';
import { Entry as ActionbarEntry } from './Actionbar.fs.js';

const meta = {
  title: "Primitive Components/Actionbar",
  tags: ["autodocs"],
  parameters: {
    // Optional parameter to center the component in the Canvas. More info: https://storybook.js.org/docs/configure/story-layout
    layout: 'fullscreen',
  },
  component: ActionbarEntry,
} satisfies Meta<typeof ActionbarEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

export const DisplayActionbar: Story = {
  args: {
    maxNumber: 3,
    debug: true,
  }
}

export const DisplayActionbarWithoutRestElementButton: Story = {
  args: {
    maxNumber: 5,
    debug: true
  }
}