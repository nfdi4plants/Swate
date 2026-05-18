import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, within } from 'storybook/test';
import { Entry as NavbarEntry, Main as NavbarMain } from './Navbar.fs.js';

const NavbarSlotsExample = () => (
  <NavbarMain
    left={<span>Left slot</span>}
    middle={<span>Center slot</span>}
    right={
      <button type="button" className="swt:btn swt:btn-sm">
        Right action
      </button>
    }
  />
);

const meta = {
  title: 'Primitive Components/Navbar',
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    viewport: { defaultViewport: 'responsive' },
  },
  component: NavbarEntry,
} satisfies Meta<typeof NavbarEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Basic: Story = {
  args: {
    debug: true,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.getByRole('navigation', { name: /arc navigation/i })).toBeInTheDocument();
  },
};

export const WithSlots: Story = {
  render: () => <NavbarSlotsExample />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.getByText('Left slot')).toBeInTheDocument();
    expect(canvas.getByText('Center slot')).toBeInTheDocument();
    expect(canvas.getByRole('button', { name: /right action/i })).toBeInTheDocument();
  },
};
