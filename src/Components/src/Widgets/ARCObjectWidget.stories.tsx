import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, within } from 'storybook/test';
import { Entry as ARCObjectWidgetEntry } from './ARCObjectWidget.fs.js';

const meta = {
  title: 'Components/Widgets/ARC Object Widget',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: ARCObjectWidgetEntry,
} satisfies Meta<typeof ARCObjectWidgetEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const openButton = canvas.getByRole('button', { name: /open arc object/i });
    expect(openButton).toBeInTheDocument();

    await userEvent.click(openButton);

    expect(screen.getByText('ARC Object Tree')).toBeInTheDocument();
    expect(screen.getByText('ARC Object Explorer')).toBeInTheDocument();
    expect(screen.getByText('ARC Object Details')).toBeInTheDocument();
    expect(screen.getAllByText('PlantStressStudy').length).toBeGreaterThan(0);
    expect(screen.getByText('Selected Object')).toBeInTheDocument();
  },
};
