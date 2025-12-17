import type { Meta, StoryObj } from '@storybook/react-vite';
import Layout from "../Layout/Layout.fs.js";
import { Entry as SelectorEntry } from './Selector.fs.js';

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
