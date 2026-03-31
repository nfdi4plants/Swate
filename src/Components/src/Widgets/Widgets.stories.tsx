import type { Meta, StoryObj } from '@storybook/react-vite';
import { screen, within, expect, userEvent, waitFor } from 'storybook/test';
import { Entry as WidgetsEntry } from './Widgets.fs.js';

const ACTIVE_ORDER_PREFIX = 'Active order:';

const meta = {
  title: 'Components/Widgets',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: WidgetsEntry,
} satisfies Meta<typeof WidgetsEntry>;

export default meta;

type Story = StoryObj<typeof meta>;

const getActiveOrderText = () => {
  const node = screen.getByText((content) => content.startsWith(ACTIVE_ORDER_PREFIX));
  return node.textContent ?? '';
};

const getActiveOrderTextNormalized = () => getActiveOrderText().toLowerCase();

const getWidgetToggleButton = (canvas: ReturnType<typeof within>, widgetLabelPart: string) =>
  canvas.getByRole('button', { name: new RegExp(`(Open|Close)\\s+${widgetLabelPart}`, 'i') });

export const Default: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(getWidgetToggleButton(canvas, 'Building')).toBeInTheDocument();
    expect(getWidgetToggleButton(canvas, 'Template')).toBeInTheDocument();
    expect(getWidgetToggleButton(canvas, 'File')).toBeInTheDocument();
    expect(getWidgetToggleButton(canvas, 'Data')).toBeInTheDocument();
    expect(getWidgetToggleButton(canvas, 'Playground')).toBeInTheDocument();

    expect(canvas.getByRole('button', { name: 'Open All' })).toBeInTheDocument();
    expect(canvas.getByRole('button', { name: 'Close All' })).toBeInTheDocument();
    expect(getActiveOrderText()).toContain('none');
  },
};

export const OpenCloseAll: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: 'Open All' }));

    await waitFor(() => {
      const order = getActiveOrderTextNormalized();
      expect(order).toContain('buildingblock');
      expect(order).toContain('template');
      expect(order).toContain('filepicker');
      expect(order).toContain('dataannotator');
      expect(order).toContain('playground');
    });

    await userEvent.click(canvas.getByRole('button', { name: 'Close All' }));

    await waitFor(() => {
      expect(getActiveOrderText()).toContain('none');
    });
  },
};

export const FocusReordersWidgets: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(getWidgetToggleButton(canvas, 'Building'));
    await userEvent.click(getWidgetToggleButton(canvas, 'Template'));

    await waitFor(() => {
      expect(getActiveOrderTextNormalized()).toContain('buildingblock > template');
    });

    await userEvent.click(screen.getByText('BuildingBlock POC'));

    await waitFor(() => {
      expect(getActiveOrderTextNormalized()).toContain('template > buildingblock');
    });
  },
};
