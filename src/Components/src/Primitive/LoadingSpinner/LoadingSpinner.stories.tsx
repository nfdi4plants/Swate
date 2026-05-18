import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, within } from 'storybook/test';
import LoadingSpinner from './LoadingSpinner.fs.js';

const LoadingSpinnerExample = () => <LoadingSpinner text="Loading data" />;

const meta = {
  title: 'Primitive Components/LoadingSpinner',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: LoadingSpinnerExample,
} satisfies Meta<typeof LoadingSpinnerExample>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  render: () => <LoadingSpinnerExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.getByText(/loading data/i)).toBeInTheDocument();
    expect(canvasElement.querySelector('[class*="swt:loading"]')).not.toBeNull();
  },
};
