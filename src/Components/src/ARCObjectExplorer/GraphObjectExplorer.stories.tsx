import type { Meta, StoryObj } from '@storybook/react-vite';
import { Entry as GraphObjectExplorerEntry } from './GraphExplorer/GraphObjectExplorer.fs.js';

const meta = {
  title: 'Components/ARCObjectExplorer/GraphModel',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: GraphObjectExplorerEntry,
} satisfies Meta<typeof GraphObjectExplorerEntry>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <GraphObjectExplorerEntry />,
};
