import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor, fn } from 'storybook/test';
import { Wizard as NotesWizard } from './Notes.fs.js';
import {
  Exports_createNotesDraft as createNotesDraft,
  Exports_createNotesUiState as createNotesUiState,
  Exports_createDemoExistingTargets as createDemoExistingTargets,
} from './Types.fs.js';

function renderWizard(args: any) {
  const [draft, setDraft] = React.useState(() => {
    const next = createNotesDraft();
    next.Title = 'Watering plan';
    next.MainText = 'My markdown body';
    next.DateCreated = new Date(2026, 1, 26);
    return next;
  });
  const [uiState, setUiState] = React.useState(createNotesUiState());

  return (
    <NotesWizard
      draft={draft}
      setDraft={setDraft}
      uiState={uiState}
      setUiState={setUiState}
      onSubmit={args.onSubmit}
      availableExistingTargets={createDemoExistingTargets()}
    />
  );
}

type PreviewHarnessProps = {
  createInitialDraft: () => any;
};

function createSeededPreviewDraft() {
  const next = createNotesDraft();
  next.Title = 'Preview title';
  next.MainText = 'Preview body content';
  next.DateCreated = new Date(2026, 1, 26);
  return next;
}

function createEmptyDraft() {
  return createNotesDraft();
}

function PreviewHarness({ createInitialDraft }: PreviewHarnessProps) {
  const [draft, setDraft] = React.useState(() => createInitialDraft());
  const [uiState, setUiState] = React.useState(createNotesUiState());
  const [previewPath, setPreviewPath] = React.useState<string | null>(null);
  const [previewMarkdown, setPreviewMarkdown] = React.useState<string | null>(null);

  return (
    <div className="swt:flex swt:flex-col swt:gap-4">
      <NotesWizard
        draft={draft}
        setDraft={setDraft}
        uiState={uiState}
        setUiState={setUiState}
        onSubmit={(payload: any) => {
          setPreviewPath(payload.Intent.RelativePath);
          setPreviewMarkdown(payload.Intent.Content);
        }}
        availableExistingTargets={createDemoExistingTargets()}
      />
      <div
        data-testid="notes-preview-panel"
        className="swt:mx-auto swt:w-full swt:max-w-4xl swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:space-y-2"
      >
        <h3 className="swt:text-lg swt:font-semibold">Generated Save Preview</h3>
        <p className="swt:text-sm swt:font-semibold">Relative Path</p>
        <pre
          data-testid="notes-preview-relative-path"
          className="swt:rounded swt:bg-base-200 swt:p-2 swt:text-xs swt:whitespace-pre-wrap"
        >
          {previewPath ?? 'No preview yet. Click a save-intent action above.'}
        </pre>
        <p className="swt:text-sm swt:font-semibold">Markdown Content</p>
        <pre
          data-testid="notes-preview-markdown"
          className="swt:max-h-72 swt:overflow-auto swt:rounded swt:bg-base-200 swt:p-2 swt:text-xs swt:whitespace-pre-wrap"
        >
          {previewMarkdown ?? 'No preview yet. Click a save-intent action above.'}
        </pre>
      </div>
    </div>
  );
}

const meta = {
  title: 'Components/Notes/Editor',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: NotesWizard,
} satisfies Meta<typeof NotesWizard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => <PreviewHarness createInitialDraft={createEmptyDraft} />,
};

export const ExistingTargetSubmit: Story = {
  render: renderWizard,
  args: {
    onSubmit: fn(),
  },
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('notes-add-existing-button'));

    const targetSelect = canvas.getByTestId('notes-existing-target-select') as HTMLSelectElement;
    await userEvent.selectOptions(targetSelect, 'MyStudy');

    await userEvent.click(canvas.getByTestId('notes-create-existing-button'));

    await waitFor(() => {
      expect(args.onSubmit).toHaveBeenCalledTimes(1);
    });

    const payload = args.onSubmit.mock.calls[0][0];
    expect(payload.Intent.RelativePath).toBe('notes/studies/MyStudy/26_02_2026/Watering_plan.md');
  },
};

export const NewRootNoteSubmit: Story = {
  render: renderWizard,
  args: {
    onSubmit: fn(),
  },
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByTestId('notes-create-new-button'));

    await waitFor(() => {
      expect(args.onSubmit).toHaveBeenCalledTimes(1);
    });

    const payload = args.onSubmit.mock.calls[0][0];
    expect(payload.Intent.RelativePath).toBe('notes/26_02_2026/Watering_plan.md');
  },
};

export const PreviewGeneratedMarkdownAndPath: Story = {
  render: () => <PreviewHarness createInitialDraft={createSeededPreviewDraft} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const titleField = canvas.getByTestId('notes-title-field');
    const titleInput = within(titleField).getByRole('textbox');
    await userEvent.clear(titleInput);
    await userEvent.type(titleInput, 'Preview from story');

    await userEvent.click(canvas.getByTestId('notes-create-new-button'));

    await waitFor(() => {
      expect(canvas.getByTestId('notes-preview-relative-path')).toHaveTextContent(
        'notes/26_02_2026/Preview_from_story.md'
      );
    });

    expect(canvas.getByTestId('notes-preview-markdown')).toHaveTextContent('# Preview from story');
  },
};
