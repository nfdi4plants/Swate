import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, screen, expect, userEvent, waitFor } from 'storybook/test';
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

export const ModeSwitching: Story = {
  parameters: { isolated: true },
  render: () => <TextInputWithMarkdownEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const textareaSelector = 'textarea[placeholder="Write markdown..."]';
    expect(canvasElement.querySelector(textareaSelector)).not.toBeNull();

    await userEvent.click(canvas.getByRole('button', { name: 'Preview' }));

    await waitFor(() => {
      expect(canvasElement.querySelector(textareaSelector)).toBeNull();
      expect(canvas.getByText('Markdown Notes')).toBeInTheDocument();
    });

    await userEvent.click(canvas.getByRole('button', { name: 'Edit' }));

    await waitFor(() => {
      expect(canvasElement.querySelector(textareaSelector)).not.toBeNull();
    });
  },
};

export const AddStepPluginFlow: Story = {
  parameters: { isolated: true },
  render: () => <TextInputWithMarkdownEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const editor = canvas.getByPlaceholderText('Write markdown...') as HTMLTextAreaElement;

    await userEvent.click(canvas.getByRole('button', { name: 'Add Step' }));

    const modalInput = await screen.findByPlaceholderText('Step text');
    await userEvent.type(modalInput, 'Prepare samples');
    await userEvent.click(screen.getByRole('button', { name: 'Add' }));

    await waitFor(() => {
      expect(screen.queryByPlaceholderText('Step text')).not.toBeInTheDocument();
      expect(editor.value).toContain('- Prepare samples');
    });
  },
};

export const AddOntologyPluginFlow: Story = {
  parameters: { isolated: true },
  render: () => <TextInputWithMarkdownEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const editor = canvas.getByPlaceholderText('Write markdown...') as HTMLTextAreaElement;

    await userEvent.click(canvas.getByRole('button', { name: 'Add Ontology Reference' }));

    const modalInput = await screen.findByPlaceholderText('instrument model | MS:1000031');
    await userEvent.type(modalInput, 'instrument model | MS:1000031');
    await userEvent.click(screen.getByRole('button', { name: 'Add' }));

    await waitFor(() => {
      expect(screen.queryByPlaceholderText('instrument model | MS:1000031')).not.toBeInTheDocument();
      expect(editor.value).toContain('instrument model');
      expect(editor.value).toContain('MS:1000031');
    });
  },
};

export const AddImagePluginFlow: Story = {
  parameters: { isolated: true },
  render: () => <TextInputWithMarkdownEntry />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const editor = canvas.getByPlaceholderText('Write markdown...') as HTMLTextAreaElement;

    await userEvent.click(canvas.getByRole('button', { name: 'Add Image' }));

    const fileInput = await screen.findByTestId('markdown-plugin-file-input');
    expect(fileInput).toBeTruthy();

    const firstImage = new File(['fake-image-a'], 'diagram-a.png', { type: 'image/png' });
    const secondImage = new File(['fake-image-b'], 'diagram-b.png', { type: 'image/png' });

    await userEvent.upload(fileInput as HTMLInputElement, firstImage);
    await userEvent.upload(fileInput as HTMLInputElement, secondImage);

    expect(screen.getByText('diagram-a.png')).toBeInTheDocument();
    expect(screen.getByText('diagram-b.png')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Remove diagram-a.png' }));

    await waitFor(() => {
      expect(screen.queryByText('diagram-a.png')).not.toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole('button', { name: 'Insert' }));

    await waitFor(() => {
      expect(editor.value).toContain('![diagram-b.png](diagram-b.png)');
      expect(editor.value).not.toContain('![diagram-a.png](diagram-a.png)');
    });
  },
};
