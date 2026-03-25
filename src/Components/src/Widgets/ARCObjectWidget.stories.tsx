import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, screen, userEvent, within } from 'storybook/test';
import { ARCObjectFixture_Entry as ARCObjectWidgetEntry } from '../ARCObjectExplorer/ARCObjectFixture.fs.js';

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
    expect(screen.getByText('All kinds')).toBeInTheDocument();
    const searchInput = screen.getByPlaceholderText(/search visible objects/i);
    expect(searchInput).toBeInTheDocument();
    expect(screen.getAllByText('PlantStressStudy').length).toBeGreaterThan(0);
    expect(screen.getAllByText('SoilMicrobiomeStudy').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Assays').length).toBeGreaterThan(0);
    expect(screen.getAllByText('DataMap').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Tables').length).toBeGreaterThan(0);
    expect(screen.getByText('Properties')).toBeInTheDocument();
    expect(screen.getByText('Metadata')).toBeInTheDocument();
    expect(screen.getByText('PS-2026-001')).toBeInTheDocument();

    await userEvent.type(searchInput, 'QC Injection');

    const listbox = await screen.findByRole('listbox');
    const option = within(listbox).getByRole('option', { name: /qc injection summary/i });
    expect(option).toBeInTheDocument();
    expect(listbox).toHaveTextContent('Parent:');

    await userEvent.click(option);

    expect(screen.getAllByText('QC Injection Summary').length).toBeGreaterThan(0);
    expect(screen.getByText('Workbook child')).toBeInTheDocument();
    expect(screen.getByText('assays/MetabolomicsAssay/isa.assay.xlsx -> Table 3')).toBeInTheDocument();
    expect(screen.getByText('Rows')).toBeInTheDocument();
    expect(screen.getByText('18')).toBeInTheDocument();
  },
};
