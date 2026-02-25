import type { Meta, StoryObj } from '@storybook/react-vite';
import { Entry as TextInputWithMarkdownEntry } from './TextInputWithMarkdown.fs.js';

const meta = {
  title: 'Components/TextInputWithMarkdown',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
    docs: {
      canvas: {
        withToolbar: false,
        sourceState: 'none',
      },
    },
  },
  component: TextInputWithMarkdownEntry,
} satisfies Meta<typeof TextInputWithMarkdownEntry>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <TextInputWithMarkdownEntry />,
};
