import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
import { ArcFileEditor as ArcFileEditorComponent, Entry as ArcFileEditorEntry } from './ArcFileEditor.fs.js';
import { ActiveView_Table } from './Types.fs.js';
import { TemplateWidgetServices } from '../Widgets/WidgetSupport.fs.js';
import { ArcAssay } from '../fable_modules/ARCtrl.Core.3.0.0-beta.12/ArcTypes.fs.js';
import {
  CompositeHeader_Input,
  CompositeHeader_Output,
  IOType_Sample,
  IOType_Source,
} from '../fable_modules/ARCtrl.Core.3.0.0-beta.12/Table/CompositeHeader.fs.js';
import { Organisation_DataPLANT, Template } from '../fable_modules/ARCtrl.Core.3.0.0-beta.12/Template.fs.js';
import { singleton as asyncBuilder } from '../fable_modules/fable-library-ts.5.0.0-alpha.21/AsyncBuilder.ts';
import { FSharpResult$2_Ok } from '../fable_modules/fable-library-ts.5.0.0-alpha.21/Result.ts';
import type { MutableArray } from '../fable_modules/fable-library-ts.5.0.0-alpha.21/Util.ts';
import {
  ARCtrlHelper_ArcFiles_Assay,
  ARCtrlHelper_ArcFiles__TryGetActiveTable_71136F3F,
} from '../../../Shared/ARCtrl.Helper.fs.js';
import type { ARCtrlHelper_ArcFiles_$union } from '../../../Shared/ARCtrl.Helper.fs.js';

const createAssayWithTable = () => {
  const assay = ArcAssay.init('NavbarTestAssay');
  assay.InitTable('Assay Table');
  return ARCtrlHelper_ArcFiles_Assay(assay);
};

const STORY_TEMPLATE_NAME = 'Story Import Template';

const createImportTemplate = () => {
  const template = Template.init(STORY_TEMPLATE_NAME);
  template.Organisation = Organisation_DataPLANT();
  template.Version = '1.0.0';
  template.Table.AddColumn(CompositeHeader_Input(IOType_Source()));
  template.Table.AddColumn(CompositeHeader_Output(IOType_Sample()));
  template.Table.AddRowsEmpty(1);
  return template;
};

const templateServices = new TemplateWidgetServices(() =>
  asyncBuilder.Delay(() =>
    asyncBuilder.Return(
      FSharpResult$2_Ok<MutableArray<Template>, string>([createImportTemplate()] as unknown as MutableArray<Template>),
    ),
  ),
);

const getActiveColumnCount = (arcFile: ARCtrlHelper_ArcFiles_$union) => {
  const activeTable = ARCtrlHelper_ArcFiles__TryGetActiveTable_71136F3F(arcFile, 0);
  return activeTable ? activeTable[1].ColumnCount : 0;
};

const parseColumnCount = (text: string | null) => {
  const match = text?.match(/\d+/);
  return match ? Number(match[0]) : 0;
};

const FullSizeArcEditor = () => {
  return (
    <div className='swt:size-full swt:grow'>
      <ArcFileEditorEntry />
    </div>
  );
};

const TemplateImportTestEditor = () => {
  const [arcFile, setArcFile] = React.useState<ARCtrlHelper_ArcFiles_$union>(() => createAssayWithTable());
  const activeColumnCount = getActiveColumnCount(arcFile);

  return (
    <div className='swt:size-full swt:grow swt:flex swt:flex-col'>
      <div data-testid='arc-file-editor-column-count' className='swt:px-2 swt:py-1 swt:text-sm swt:font-medium'>
        {`Column count: ${activeColumnCount}`}
      </div>
      <ArcFileEditorComponent
        arcFile={arcFile}
        setArcFile={(nextArcFile) => setArcFile(nextArcFile)}
        templateServices={templateServices}
        startingActiveView={ActiveView_Table(0)}
      />
    </div>
  );
};

const meta = {
  title: 'Components/ArcFileEditor',
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
  render: () => <TemplateImportTestEditor />,
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
