import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
import React from 'react';
import CwlEditor from './CwlEditor.fs.js';
import type { LoadCwlResponse } from '../../../../Shared/Cwl/HostTypes.fs.js';

const START_SCREEN_TESTIDS = [
  'cwl-new-command-line-tool',
  'cwl-new-workflow',
  'cwl-new-expression-tool',
  'cwl-new-operation',
  'cwl-load-existing',
] as const;

const minimalCommandLineToolYaml = `cwlVersion: v1.2
class: CommandLineTool
baseCommand: echo
inputs:
  message:
    type: string
    inputBinding:
      position: 1
outputs:
  out:
    type: stdout
`;

const minimalWorkflowYaml = `cwlVersion: v1.2
class: Workflow
inputs:
  input_file:
    type: File
outputs:
  output_file:
    type: File
    outputSource: step1/output
steps:
  step1:
    run: tool.cwl
    in:
      input_file: input_file
    out: [output]
`;

const invalidExpressionToolYaml = "cwlVersion: v1.2\nclass: ExpressionTool\nrequirements:\n  - class: InlineJavascriptRequirement\ninputs:\n  input_val:\n    type: int\noutputs:\n  output_val:\n    type: int\nexpression: ''\n";

const toLoadResponse = (yaml: string, filePath: string): LoadCwlResponse => ({
  Success: true,
  Yaml: yaml,
  ResolvedYaml: undefined,
  FilePath: filePath,
  Error: undefined,
});

const createMockHost = () => {
  const files = new Map<string, string>();

  return {
    pickOpenFile: async () => ({
      Canceled: false,
      FilePath: 'minimal-command-line-tool.cwl',
    }),
    loadCwlFile: async (filePath: string) =>
      toLoadResponse(minimalCommandLineToolYaml, filePath),
    pickSavePath: async () => ({
      Canceled: false,
      FilePath: 'minimal-command-line-tool.cwl',
    }),
    saveCwlFile: async (filePath: string, yaml: string) => {
      files.set(filePath, yaml);

      return {
        Success: true,
        FilePath: filePath,
        Error: undefined,
      };
    },
  };
};

function renderCwlEditor(args: any) {
  return (
    <div style={{ height: '100vh', width: '100%' }}>
      <CwlEditor {...args} />
    </div>
  );
}

const meta = {
  title: 'Page Components/CwlEditor',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: CwlEditor,
} satisfies Meta<typeof CwlEditor>;

export default meta;

type Story = StoryObj<typeof meta>;

export const StartScreen: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    for (const testId of START_SCREEN_TESTIDS) {
      expect(canvas.getByTestId(testId)).toBeVisible();
    }
  },
};

export const CommandLineToolFlow: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('cwl-new-command-line-tool'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-command-line-tool-editor')).toBeInTheDocument()
    );

    const baseCommand = canvas.getByTestId('cwl-editor-base-command');
    await userEvent.clear(baseCommand);
    await userEvent.type(baseCommand, 'cat');

    await userEvent.click(canvas.getByTestId('cwl-editor-preview'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-preview-close')).toBeInTheDocument()
    );

    const previewText = canvasElement.querySelector('pre');
    expect(previewText).not.toBeNull();
    expect(previewText).toHaveTextContent(/baseCommand[\s\S]*cat/);

    await userEvent.click(canvas.getByTestId('cwl-preview-close'));
    expect(canvas.queryByTestId('cwl-preview-close')).not.toBeInTheDocument();

    await userEvent.click(canvas.getByTestId('cwl-editor-back-to-start'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-discard-confirm')).toBeInTheDocument()
    );

    await userEvent.click(canvas.getByTestId('cwl-discard-cancel'));
    expect(canvas.getByTestId('cwl-command-line-tool-editor')).toBeInTheDocument();
    expect(canvas.queryByTestId('cwl-discard-confirm')).not.toBeInTheDocument();

    await userEvent.click(canvas.getByTestId('cwl-editor-back-to-start'));
    await userEvent.click(canvas.getByTestId('cwl-discard-confirm'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-new-command-line-tool')).toBeInTheDocument()
    );
  },
};

export const LoadedWorkflow: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
    initialFile: toLoadResponse(minimalWorkflowYaml, 'workflows/main.cwl'),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.getByTestId('cwl-workflow-editor')).toBeInTheDocument();
    expect(canvas.getByTestId('cwl-workflow-canvas')).toBeInTheDocument();
  },
};

export const ValidationBlocksSave: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
    initialFile: toLoadResponse(
      invalidExpressionToolYaml,
      'minimal-expression-tool.cwl'
    ),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('cwl-editor-save'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-editor-error')).toHaveTextContent(/^Save blocked:/)
    );
  },
};
