import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor, fireEvent } from 'storybook/test';
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

const warningCommandLineToolYaml = `cwlVersion: v1.2
class: CommandLineTool
outputs:
  out:
    type: stdout
`;

const toLoadResponse = (yaml: string, filePath: string): LoadCwlResponse => ({
  Success: true,
  Yaml: yaml,
  ResolvedYaml: undefined,
  FilePath: filePath,
  Error: undefined,
});

type MockHostOptions = {
  openFilePath?: string;
  loadYaml?: string;
  saveFilePath?: string;
};

const createMockHost = (options: MockHostOptions = {}) => {
  const files = new Map<string, string>();

  return {
    pickOpenFile: async () => ({
      Canceled: false,
      FilePath: options.openFilePath ?? 'minimal-command-line-tool.cwl',
    }),
    loadCwlFile: async (filePath: string) =>
      toLoadResponse(options.loadYaml ?? minimalCommandLineToolYaml, filePath),
    pickSavePath: async () => ({
      Canceled: false,
      FilePath: options.saveFilePath ?? 'minimal-command-line-tool.cwl',
    }),
    saveCwlFile: async (filePath: string, yaml: string) => {
      files.set(filePath, yaml);

      return {
        Success: true,
        FilePath: filePath,
        Error: undefined,
      };
    },
    savedFiles: files,
  };
};

function createDataTransfer() {
  return {
    data: {} as Record<string, string>,
    setData(type: string, value: string) {
      this.data[type] = value;
    },
    getData(type: string) {
      return this.data[type] ?? '';
    },
    types: [] as string[],
    effectAllowed: 'all',
    dropEffect: 'move',
  };
}

async function dragTemplateTo(
  canvas: ReturnType<typeof within>,
  key: string,
  bucket: 'requirement' | 'hint'
) {
  const dataTransfer = createDataTransfer();
  const sourceEl = canvas.getByTestId(`cwl-requirement-template-${key}`);
  const dropzoneEl = canvas.getByTestId(`cwl-requirement-dropzone-${bucket}`);

  // Chromium rejects plain-object dataTransfer in the DragEvent constructor
  // (which fireEvent uses); dispatch plain Events and attach dataTransfer after.
  const dispatchDragEvent = (el: Element, type: string) => {
    const event = new Event(type, { bubbles: true, cancelable: true });
    Object.defineProperty(event, 'dataTransfer', { value: dataTransfer });
    el.dispatchEvent(event);
  };

  dispatchDragEvent(sourceEl, 'dragstart');
  dispatchDragEvent(dropzoneEl, 'dragover');
  dispatchDragEvent(dropzoneEl, 'drop');
}

async function openNewCommandLineTool(canvas: ReturnType<typeof within>) {
  await userEvent.click(canvas.getByTestId('cwl-new-command-line-tool'));
  await waitFor(() =>
    expect(
      canvas.getByTestId('cwl-command-line-tool-editor')
    ).toBeInTheDocument()
  );
}

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

export const NewWorkflowFlow: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('cwl-new-workflow'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-workflow-editor')).toBeInTheDocument()
    );
  },
};

export const NewExpressionToolFlow: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('cwl-new-expression-tool'));
    await waitFor(() =>
      expect(
        canvas.getByTestId('cwl-expression-tool-editor')
      ).toBeInTheDocument()
    );
  },
};

export const NewOperationFlow: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('cwl-new-operation'));
    await waitFor(() => {
      expect(canvas.getByTestId('cwl-operation-editor')).toBeInTheDocument();
      expect(
        canvas.getByText('Operation Editing Is Not Implemented Yet')
      ).toBeVisible();
    });
  },
};

export const LoadExistingFlow: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost({
      openFilePath: 'loaded/existing-tool.cwl',
      loadYaml: minimalCommandLineToolYaml,
    }),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('cwl-load-existing'));
    await waitFor(() => {
      expect(
        canvas.getByTestId('cwl-command-line-tool-editor')
      ).toBeInTheDocument();
      expect(canvas.getByTestId('cwl-editor-base-command')).toHaveValue('echo');
      expect(canvas.getByTestId('cwl-input-item-0')).toHaveTextContent(
        'message : string'
      );
      expect(
        canvas.getByText(/loaded\/existing-tool\.cwl \| version \d+/)
      ).toBeVisible();
    });
  },
};

export const InputsEditing: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await openNewCommandLineTool(canvas);
    await userEvent.click(canvas.getByTestId('cwl-input-add'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-input-item-0')).toHaveTextContent(
        'input_1 : string'
      )
    );

    const inputName = canvas.getByTestId('cwl-input-name-0');
    await userEvent.clear(inputName);
    await userEvent.type(inputName, 'renamed_input');
    await userEvent.tab();
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-input-item-0')).toHaveTextContent(
        'renamed_input : string'
      )
    );

    await userEvent.click(canvas.getByTestId('cwl-input-add'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-input-item-1')).toHaveTextContent(
        'input_1 : string'
      )
    );

    await userEvent.click(canvas.getByTestId('cwl-input-move-up'));
    await waitFor(() => {
      expect(canvas.getByTestId('cwl-input-item-0')).toHaveTextContent(
        'input_1 : string'
      );
      expect(canvas.getByTestId('cwl-input-item-1')).toHaveTextContent(
        'renamed_input : string'
      );
    });

    await userEvent.click(canvas.getByTestId('cwl-input-move-down'));
    await waitFor(() => {
      expect(canvas.getByTestId('cwl-input-item-0')).toHaveTextContent(
        'renamed_input : string'
      );
      expect(canvas.getByTestId('cwl-input-item-1')).toHaveTextContent(
        'input_1 : string'
      );
    });

    await userEvent.click(canvas.getByTestId('cwl-input-remove'));
    await waitFor(() => {
      expect(canvas.getByTestId('cwl-input-item-0')).toHaveTextContent(
        'renamed_input : string'
      );
      expect(canvas.queryByTestId('cwl-input-item-1')).not.toBeInTheDocument();
    });
  },
};

export const OutputsEditing: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await openNewCommandLineTool(canvas);
    await userEvent.click(canvas.getByTestId('cwl-output-add'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-output-item-0')).toHaveTextContent(
        'output_1 : file'
      )
    );

    const outputName = canvas.getByTestId('cwl-output-name-0');
    await userEvent.clear(outputName);
    await userEvent.type(outputName, 'renamed_output');
    await userEvent.tab();
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-output-item-0')).toHaveTextContent(
        'renamed_output : file'
      )
    );

    await userEvent.click(canvas.getByTestId('cwl-output-add'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-output-item-1')).toHaveTextContent(
        'output_1 : file'
      )
    );

    await userEvent.click(canvas.getByTestId('cwl-output-move-up'));
    await waitFor(() => {
      expect(canvas.getByTestId('cwl-output-item-0')).toHaveTextContent(
        'output_1 : file'
      );
      expect(canvas.getByTestId('cwl-output-item-1')).toHaveTextContent(
        'renamed_output : file'
      );
    });

    await userEvent.click(canvas.getByTestId('cwl-output-move-down'));
    await waitFor(() => {
      expect(canvas.getByTestId('cwl-output-item-0')).toHaveTextContent(
        'renamed_output : file'
      );
      expect(canvas.getByTestId('cwl-output-item-1')).toHaveTextContent(
        'output_1 : file'
      );
    });

    await userEvent.click(canvas.getByTestId('cwl-output-remove'));
    await waitFor(() => {
      expect(canvas.getByTestId('cwl-output-item-0')).toHaveTextContent(
        'renamed_output : file'
      );
      expect(canvas.queryByTestId('cwl-output-item-1')).not.toBeInTheDocument();
    });
  },
};

export const RequirementEditing: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await openNewCommandLineTool(canvas);
    await dragTemplateTo(canvas, 'docker', 'requirement');
    await waitFor(() => {
      expect(
        canvas.getByTestId('cwl-requirement-item-requirement-0')
      ).toHaveTextContent('DockerRequirement');
      expect(
        canvas.getByTestId('cwl-requirement-field-dockerpull')
      ).toBeInTheDocument();
    });

    const dockerPull = canvas.getByTestId('cwl-requirement-field-dockerpull');
    await userEvent.clear(dockerPull);
    await userEvent.type(dockerPull, 'ubuntu:24.04');
    await userEvent.tab();
    await waitFor(() =>
      expect(
        canvas.getByTestId('cwl-requirement-field-dockerpull')
      ).toHaveValue('ubuntu:24.04')
    );

    await dragTemplateTo(canvas, 'resource', 'requirement');
    await waitFor(() =>
      expect(
        canvas.getByTestId('cwl-requirement-item-requirement-1')
      ).toHaveTextContent('ResourceRequirement')
    );

    await userEvent.click(
      canvas.getByTestId('cwl-requirement-item-requirement-0')
    );
    await waitFor(() =>
      expect(
        canvas.getByTestId('cwl-requirement-field-dockerpull')
      ).toHaveValue('ubuntu:24.04')
    );

    await userEvent.click(
      canvas.getByTestId('cwl-requirement-item-requirement-1')
    );
    await waitFor(() =>
      expect(
        canvas.getByTestId('cwl-requirement-field-coresmin')
      ).toBeInTheDocument()
    );

    const coresMin = canvas.getByTestId('cwl-requirement-field-coresmin');
    await userEvent.type(coresMin, '4');
    await userEvent.tab();
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-requirement-field-coresmin')).toHaveValue(
        '4'
      )
    );

    await userEvent.click(canvas.getByTestId('cwl-editor-preview'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-preview-close')).toBeInTheDocument()
    );

    const previewText = canvasElement.querySelector('pre');
    expect(previewText).not.toBeNull();
    expect(previewText).toHaveTextContent(/coresMin[\s\S]*4/);
  },
};

export const HintToggle: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await openNewCommandLineTool(canvas);
    await dragTemplateTo(canvas, 'docker', 'hint');
    await waitFor(() =>
      expect(
        canvas.getByTestId('cwl-requirement-hint-item-0')
      ).toHaveTextContent('DockerRequirement')
    );

    await userEvent.click(canvas.getByTestId('cwl-requirement-remove'));
    await waitFor(() => {
      expect(
        canvas.queryByTestId('cwl-requirement-hint-item-0')
      ).not.toBeInTheDocument();
      expect(
        canvas.getByTestId('cwl-requirement-empty-hint')
      ).toBeInTheDocument();
    });
  },
};

export const ValidationPanelListing: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
    initialFile: toLoadResponse(warningCommandLineToolYaml, 'warning-tool.cwl'),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(() => {
      expect(
        canvas.getByText('No validation messages.', { exact: true })
      ).toBeVisible();
      expect(
        canvas.getByText('No blocking errors.', { exact: true })
      ).toBeVisible();
    });

    await userEvent.click(canvas.getByTestId('cwl-editor-back-to-start'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-new-expression-tool')).toBeInTheDocument()
    );

    await userEvent.click(canvas.getByTestId('cwl-new-expression-tool'));
    await waitFor(() =>
      expect(
        canvas.getByTestId('cwl-expression-tool-editor')
      ).toBeInTheDocument()
    );

    const expression = canvas.getByTestId('cwl-editor-expression');
    await userEvent.clear(expression);
    await userEvent.tab();
    await waitFor(() =>
      expect(
        canvas.getByText(
          '[EXP.001] ExpressionTool must have a non-empty expression.'
        )
      ).toBeVisible()
    );
  },
};

export const SaveSuccessFlow: Story = {
  render: renderCwlEditor,
  args: (() => {
    const host = createMockHost({ saveFilePath: 'tools/echo.cwl' });

    return {
      host,
      initialFile: toLoadResponse(
        minimalCommandLineToolYaml,
        'tools/echo.cwl'
      ),
    };
  })(),
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    const host = args.host as ReturnType<typeof createMockHost>;

    await userEvent.click(canvas.getByTestId('cwl-editor-save'));
    await waitFor(() => {
      expect(canvas.getByTestId('cwl-editor-info')).toHaveTextContent(
        /^Saved to tools\/echo\.cwl$/
      );
      // The ARCtrl 3.2.0 emitter may write baseCommand in list form; assert
      // form-agnostically like the .NET roundtrip tests do.
      expect(host.savedFiles.get('tools/echo.cwl')).toContain('baseCommand');
      expect(host.savedFiles.get('tools/echo.cwl')).toContain('echo');
    });
  },
};

const dirtyChanges: boolean[] = [];

export const DirtyStateCallback: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
    initialFile: toLoadResponse(minimalCommandLineToolYaml, 'dirty-tool.cwl'),
    onDirtyChange: (isDirty: boolean) => dirtyChanges.push(isDirty),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    dirtyChanges.length = 0;

    const baseCommand = canvas.getByTestId('cwl-editor-base-command');
    await userEvent.clear(baseCommand);
    await userEvent.type(baseCommand, 'printf');
    await userEvent.tab();

    await waitFor(() => expect(dirtyChanges).toContain(true));
  },
};

export const InitialFileLoadError: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
    initialFile: toLoadResponse('this is not valid CWL: [', 'broken.cwl'),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(() =>
      expect(canvas.getByTestId('cwl-editor-initial-load-error')).toBeVisible()
    );
    expect(
      canvas.queryByTestId('cwl-new-command-line-tool')
    ).not.toBeInTheDocument();
  },
};

export const ExpressionToolEditing: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('cwl-new-expression-tool'));
    await waitFor(() =>
      expect(
        canvas.getByTestId('cwl-expression-tool-editor')
      ).toBeInTheDocument()
    );

    const expression = canvas.getByTestId('cwl-editor-expression');
    await userEvent.clear(expression);
    await userEvent.type(expression, 'return 42');
    await userEvent.tab();
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-editor-expression')).toHaveValue(
        'return 42'
      )
    );

    await userEvent.click(canvas.getByTestId('cwl-editor-preview'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-preview-close')).toBeInTheDocument()
    );

    const previewText = canvasElement.querySelector('pre');
    expect(previewText).not.toBeNull();
    expect(previewText).toHaveTextContent(/expression[\s\S]*return 42/);
  },
};

export const CwlVersionSelect: Story = {
  render: renderCwlEditor,
  args: {
    host: createMockHost(),
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await openNewCommandLineTool(canvas);
    const version = canvas.getByTestId('cwl-editor-cwl-version');
    await userEvent.selectOptions(version, 'v1.1');
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-editor-cwl-version')).toHaveValue('v1.1')
    );

    await userEvent.click(canvas.getByTestId('cwl-editor-preview'));
    await waitFor(() =>
      expect(canvas.getByTestId('cwl-preview-close')).toBeInTheDocument()
    );

    const previewText = canvasElement.querySelector('pre');
    expect(previewText).not.toBeNull();
    expect(previewText).toHaveTextContent(/cwlVersion: v1.1/);
  },
};
