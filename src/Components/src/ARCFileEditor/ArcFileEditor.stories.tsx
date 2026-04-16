import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { within, expect, userEvent, waitFor } from 'storybook/test';
import { ArcFileEditor } from './ArcFileEditor.fs.js';
import { ArcFileEditorWidgetServices } from './Types.fs.js';
import type { ActiveView_$union } from './Types.fs.js';
import {
  DataAnnotatorWidgetServices,
  FilePickerWidgetServices,
  TemplateWidgetServices,
} from '../Widgets/WidgetSupport.fs.js';
import type { ImportedTextFile } from '../../../Shared/Types.fs.js';
import { ArcAssay } from '../fable_modules/ARCtrl.Core.3.0.0-beta.12/ArcTypes.fs.js';
import type { Template } from '../fable_modules/ARCtrl.Core.3.0.0-beta.12/Template.fs.js';
import { singleton as asyncBuilder } from '../fable_modules/fable-library-ts.5.0.0-alpha.21/AsyncBuilder.ts';
import { FSharpResult$2_Ok } from '../fable_modules/fable-library-ts.5.0.0-alpha.21/Result.ts';
import type { MutableArray } from '../fable_modules/fable-library-ts.5.0.0-alpha.21/Util.ts';
import { ARCtrlHelper_ArcFiles_Assay } from '../../../Shared/ARCtrl.Helper.fs.js';
import type { ARCtrlHelper_ArcFiles_$union } from '../../../Shared/ARCtrl.Helper.fs.js';

const emptyMutableArray = <T,>() => [] as unknown as MutableArray<T>;

const templateServices = new TemplateWidgetServices(() =>
  asyncBuilder.Delay(() => asyncBuilder.Return(FSharpResult$2_Ok<MutableArray<Template>, string>(emptyMutableArray()))),
);

const widgetServices = new ArcFileEditorWidgetServices(
  new FilePickerWidgetServices(() => Promise.resolve(FSharpResult$2_Ok<MutableArray<string>, string>(emptyMutableArray()))),
  new DataAnnotatorWidgetServices(() =>
    Promise.resolve(FSharpResult$2_Ok<MutableArray<ImportedTextFile>, string>(emptyMutableArray())),
  ),
);

const createAssayWithTable = () => {
  const assay = ArcAssay.init('NavbarTestAssay');
  assay.InitTable('Assay Table');
  return ARCtrlHelper_ArcFiles_Assay(assay);
};

const activeViewLabel = (activeView: ActiveView_$union) => {
  switch (activeView.tag) {
    case 0:
      return `Table ${Number(activeView.fields[0]) + 1}`;
    case 1:
      return 'DataMap';
    default:
      return 'Metadata';
  }
};

const NavbarWithWidgets = () => {
  const [arcFile, setArcFile] = React.useState<ARCtrlHelper_ArcFiles_$union>(createAssayWithTable);

  return (
    <ArcFileEditor
      arcFile={arcFile}
      setArcFile={(nextArcFile) => setArcFile(nextArcFile)}
      templateServices={templateServices}
      widgetServices={widgetServices}
      header={(props) => (
        <div
          data-testid="arc-file-editor-story-header"
          className="swt:flex swt:items-center swt:px-2 swt:font-semibold"
        >
          {`Story header: ${activeViewLabel(props.activeView)}`}
        </div>
      )}
    />
  );
};

const meta = {
  title: 'Components/ARCFileEditor/Navbar',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
  component: NavbarWithWidgets,
} satisfies Meta<typeof NavbarWithWidgets>;

export default meta;

type Story = StoryObj<typeof meta>;

const getWidgetButton = (canvas: ReturnType<typeof within>, label: string) =>
  canvas.getByRole('button', { name: new RegExp(label, 'i') });

const getDisabledWidgetButtons = (canvas: ReturnType<typeof within>) =>
  canvas.getAllByRole('button', { name: /Select a table to open widgets/i });

export const IntegratedNavbar: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const navbar = canvas.getByRole('navigation', { name: 'arc navigation' });
    expect(navbar).toBeInTheDocument();
    expect(canvas.getByTestId('arc-file-editor-story-header')).toHaveTextContent('Story header: Metadata');

    const disabledButtons = getDisabledWidgetButtons(canvas);
    expect(disabledButtons).toHaveLength(4);
    disabledButtons.forEach((button: HTMLElement) => expect(button).toBeDisabled());

    await userEvent.click(canvas.getByRole('button', { name: /Assay Table/i }));

    await waitFor(() => {
      expect(canvas.getByTestId('arc-file-editor-story-header')).toHaveTextContent('Story header: Table 1');
      expect(getWidgetButton(canvas, 'Open Add Building Block')).toBeEnabled();
      expect(getWidgetButton(canvas, 'Open Add Template')).toBeEnabled();
      expect(getWidgetButton(canvas, 'Open File Picker')).toBeEnabled();
      expect(getWidgetButton(canvas, 'Open Data Annotator')).toBeEnabled();
    });
  },
};

export const NavbarWidgetToggle: Story = {
  parameters: { isolated: true },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /Assay Table/i }));
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
