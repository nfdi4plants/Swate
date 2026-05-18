import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
import { Entry as ArcFileEditor } from './ArcFileEditor.fs.js';

const STORY_TEMPLATE_NAME = 'Story Import Template';

const parseColumnCount = (text: string | null) => {
  const match = text?.match(/\d+/);
  return match ? Number(match[0]) : 0;
};

const FullSizeArcEditor = () => {
  return (
    <div className='swt:flex swt:flex-col swt:gap-4 swt:h-screen swt:w-screen swt:overflow-hidden'>
      <ArcFileEditor debug/>
    </div>
  );
};


const meta = {
  title: 'Page Components/ArcFileEditor',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: FullSizeArcEditor,
} satisfies Meta<typeof FullSizeArcEditor>;

export default meta;

type Story = StoryObj<typeof meta>;

const getWidgetButton = (canvas: ReturnType<typeof within>, label: string) =>
  canvas.getByRole('button', { name: new RegExp(label, 'i') });

export const IntegratedNavbar: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const navbar = canvas.getByRole('navigation', { name: 'arc navigation' });
    expect(navbar).toBeInTheDocument();

    expect(getWidgetButton(canvas, 'Open Add Building Block')).toBeEnabled();
    expect(getWidgetButton(canvas, 'Open Add Template')).toBeEnabled();
    expect(getWidgetButton(canvas, 'Open File Picker')).toBeEnabled();
    expect(getWidgetButton(canvas, 'Open Data Annotator')).toBeEnabled();
  },
};

export const NavbarWidgetToggle: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(getWidgetButton(canvas, 'Open Add Building Block'));

    await waitFor(() => {
      expect(getWidgetButton(canvas, 'Close Add Building Block')).toBeInTheDocument();
    });

    await userEvent.click(getWidgetButton(canvas, 'Close Add Building Block'));

    await waitFor(() => {
      expect(getWidgetButton(canvas, 'Open Add Building Block')).toBeInTheDocument();
    });
  },
};

export const AddTemplateWidget: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const portal = within(canvasElement.ownerDocument.body);

    const initialColumnCount = parseColumnCount(canvas.getByTestId('arc-file-editor-column-count').textContent);

    await userEvent.click(getWidgetButton(canvas, 'Open Add Template'));

    await waitFor(() => {
      expect(getWidgetButton(canvas, 'Close Add Template')).toBeInTheDocument();
      expect(canvas.getByText(STORY_TEMPLATE_NAME)).toBeInTheDocument();
    });

    await userEvent.click(canvas.getByText(STORY_TEMPLATE_NAME));

    await waitFor(() => {
      expect(canvas.getByText(/1 selected/i)).toBeInTheDocument();
    });

    await userEvent.click(canvas.getByRole('button', { name: /^Import$/i }));

    const importDialog = await portal.findByRole('dialog', { name: /Import templates/i });
    await userEvent.click(within(importDialog).getByRole('button', { name: /^Import$/i }));

    await waitFor(() => {
      const nextColumnCount = parseColumnCount(canvas.getByTestId('arc-file-editor-column-count').textContent);
      expect(nextColumnCount).toBeGreaterThan(initialColumnCount);
    });
  },
};
