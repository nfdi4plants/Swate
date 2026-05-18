import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, within } from 'storybook/test';
import { Entry as SelectEntry } from './Select.fs.js';

const meta = {
  title: 'Primitive Components/Select',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: SelectEntry,
} satisfies Meta<typeof SelectEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <SelectEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const triggerContainer = canvasElement.querySelector('div[tabindex="0"]') as HTMLElement | null;

    if (!triggerContainer) {
      throw new Error('Could not find Select trigger container.');
    }

    await userEvent.click(triggerContainer);

    const option = await screen.findByText('Kevin Frey');
    await userEvent.click(option);

    expect(canvas.getByText('Kevin Frey')).toBeInTheDocument();
  },
};
