import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, waitFor, within } from 'storybook/test';
import { Entry as BOAT } from './BOAT.fs.js';

const meta: Meta<typeof BOAT> = {
  title: "Composite Components/BOAT",
  component: BOAT,
  parameters: {
    layout: "fullscreen",
  },
};

export default meta;
type Story = StoryObj<typeof BOAT>;

export const Default: Story = {
  render: () => <BOAT />,

  play: (async ({ canvasElement }: { canvasElement: HTMLElement }) => {
    const canvas = within(canvasElement);

    // Find content view
    const contentView = await canvas.findByTestId("contentView");
    expect(contentView).toBeTruthy();

  }),
};


