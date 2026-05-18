import type { Meta, StoryObj } from '@storybook/react-vite';
import { Entry as LandingEntry } from './Landing.fs.js';

const meta = {
  title: 'Page Components/Landing',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: LandingEntry,
} satisfies Meta<typeof LandingEntry>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <LandingEntry />,
};
