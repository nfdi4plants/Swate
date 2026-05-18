import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, within } from 'storybook/test';
import { Entry as ComboBoxEntry } from './ComboBox.fs.js';

const meta = {
  title: 'Primitive Components/ComboBox',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: ComboBoxEntry,
} satisfies Meta<typeof ComboBoxEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <ComboBoxEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /focus combobox/i }));
    const input = canvas.getByPlaceholderText(/search/i);
    await userEvent.type(input, 'app');

    expect(await screen.findByText('Apple')).toBeInTheDocument();
  },
};
