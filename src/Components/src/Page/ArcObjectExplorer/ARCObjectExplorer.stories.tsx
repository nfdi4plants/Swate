import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent } from 'storybook/test';
import { ARCObjectExplorer } from './ARCObjectExplorer.fs.js';

const meta = {
  title: 'Page Components/ARCObjectExplorer',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: ARCObjectExplorer,
} satisfies Meta<typeof ARCObjectExplorer>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <ARCObjectExplorer>
      <div>ARC Object Widget</div>
    </ARCObjectExplorer>
  ),
};

export const OpensFixture: Story = {
  render: () => (
    <ARCObjectExplorer>
      <div>ARC Object Widget</div>
    </ARCObjectExplorer>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: 'Open ARC Object' }));
    await expect(canvas.getByText('ARC Object Widget')).toBeInTheDocument();
  },
};
