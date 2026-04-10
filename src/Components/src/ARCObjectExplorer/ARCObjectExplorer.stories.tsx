import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent } from 'storybook/test';
import { ARCObjectFixture_Entry as ARCObjectExplorerEntry } from './ARCObjectFixture.fs.js';

const meta = {
  title: 'Components/ARCObjectExplorer',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: ARCObjectExplorerEntry,
} satisfies Meta<typeof ARCObjectExplorerEntry>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <ARCObjectExplorerEntry />,
};

export const OpensFixture: Story = {
  render: () => <ARCObjectExplorerEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: 'Open ARC Object' }));
    await expect(canvas.getByText('ARC Object Widget')).toBeInTheDocument();
  },
};
