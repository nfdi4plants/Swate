import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
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

const getActiveOrderText = (canvas: ReturnType<typeof within>) => {
  const node = canvas.getByText((content) => content.startsWith(ACTIVE_ORDER_PREFIX));
  return node.textContent ?? '';
};

const getActiveOrderTextNormalized = (canvas: ReturnType<typeof within>) =>
  getActiveOrderText(canvas).toLowerCase();

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
    expect(getActiveOrderText(canvas)).toContain('none');
  },
};

export const OpenCloseAll: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: 'Open All' }));

    await waitFor(() => {
      const order = getActiveOrderTextNormalized(canvas);
      expect(order).toContain('buildingblock');
      expect(order).toContain('template');
      expect(order).toContain('filepicker');
      expect(order).toContain('dataannotator');
      expect(order).toContain('playground');
    });

    await userEvent.click(canvas.getByRole('button', { name: 'Close All' }));

    await waitFor(() => {
      expect(getActiveOrderText(canvas)).toContain('none');
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
      expect(getActiveOrderTextNormalized(canvas)).toContain('buildingblock > template');
    });

    await userEvent.click(canvas.getByText('BuildingBlock POC'));

    await waitFor(() => {
      expect(getActiveOrderTextNormalized(canvas)).toContain('template > buildingblock');
    });
  },
};
