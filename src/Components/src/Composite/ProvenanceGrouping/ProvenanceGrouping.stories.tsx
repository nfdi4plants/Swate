import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, screen, userEvent, waitFor, within } from 'storybook/test';
import { Main as ProvenanceGrouping } from './ProvenanceGrouping.fs.js';
import {
  Exports_createSampleSession as createSampleSession,
  Exports_createInputOnlySession as createInputOnlySession,
  Exports_createOutputOnlySession as createOutputOnlySession,
  Exports_createTypedSampleSession as createTypedSampleSession,
  Exports_createDataOutputOnlySession as createDataOutputOnlySession,
  Exports_patchDetails as patchDetails,
} from './Types.fs.js';

type Fixture = 'sample' | 'inputOnly' | 'outputOnly' | 'typedSample' | 'dataOutputOnly';

function Harness({
  inputOnly = false,
  outputOnly = false,
  fixture = 'sample',
  debug = true,
}: {
  inputOnly?: boolean;
  outputOnly?: boolean;
  fixture?: Fixture;
  debug?: boolean;
}) {
  const [session, setSession] = React.useState(() => {
    const selected = inputOnly ? 'inputOnly' : outputOnly ? 'outputOnly' : fixture;
    switch (selected) {
      case 'inputOnly':
        return createInputOnlySession();
      case 'outputOnly':
        return createOutputOnlySession();
      case 'typedSample':
        return createTypedSampleSession();
      case 'dataOutputOnly':
        return createDataOutputOnlySession();
      default:
        return createSampleSession();
    }
  });
  const [patches, setPatches] = React.useState<string[]>([]);

  return (
    <div className="swt:flex swt:flex-col swt:gap-4 swt:min-h-screen swt:bg-base-200 swt:p-4">
      <ProvenanceGrouping
        session={session}
        height={680}
        debug={debug}
        onChange={(change: any) => {
          setSession(change.Session);
          setPatches((current) => [
            ...current,
            ...Array.from(patchDetails(change.Patches)),
          ]);
        }}
      />
      <section className="swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:p-4">
        <h3 className="swt:text-primary swt:font-semibold">Writeback patch preview</h3>
        <pre data-testid="provenance-patch-preview" className="swt:text-xs swt:whitespace-pre-wrap">
          {patches.length === 0 ? 'No patches emitted.' : patches.join('\n')}
        </pre>
      </section>
    </div>
  );
}

const meta = {
  title: 'Composite Components/ProvenanceGrouping',
  component: ProvenanceGrouping,
  tags: ['autodocs'],
  parameters: { layout: 'fullscreen' },
} satisfies Meta<typeof ProvenanceGrouping>;

export default meta;
type Story = StoryObj<typeof meta>;

export const ExampleModel: Story = {
  render: () => <Harness />,
};

export const InputOnlyModel: Story = {
  render: () => <Harness inputOnly />,
};

export const OutputOnlyModel: Story = {
  render: () => <Harness outputOnly />,
};

export const GroupsByPropertiesAndShowsMembers: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-property-Input-Species'));
    await waitFor(() => {
      expect(canvasElement).toHaveTextContent('Species: Arabidopsis');
      expect(canvasElement).toHaveTextContent('Species: Chlamydomonas');
    });

    const grouped = canvas
      .getAllByText('Species: Arabidopsis')
      .find((element) => element.tagName === 'H3')!
      .closest('article')!;
    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));
    await waitFor(() => expect(grouped).toHaveTextContent('Input A'));
  },
};

export const RegroupedValuesOpenTheirOwnDraft: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-property-Input-Species'));
    const grouped = canvas
      .getAllByText('Species: Chlamydomonas')
      .find((element) => element.tagName === 'H3')!
      .closest('article')!;

    const species = within(grouped).getByTestId('provenance-value-pv-input-d-species');
    await userEvent.click(within(species).getByTestId('popover_trigger_provenance-edit-Species'));
    expect(screen.getByRole('textbox', { name: /Species value/i })).toHaveValue('Chlamydomonas');
  },
};

export const RendersMeasuredConnections: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await waitFor(() => {
      const connector = canvas.getAllByTestId('provenance-connection')[0];
      expect(connector.getAttribute('d')).toMatch(/^M\s+\d/);
      expect(connector.getAttribute('d')).not.toContain('M 0 32');
    });
  },
};

export const RemeasuresConnectionsAfterGroupExpansion: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const before = await waitFor(() => {
      const paths = canvas.getAllByTestId('provenance-connection').map((connector) => connector.getAttribute('d'));
      expect(paths.length).toBeGreaterThan(0);
      return paths;
    });

    const inputA = canvas.getByText('Input A').closest('article')!;
    await userEvent.click(within(inputA).getByRole('button', { name: 'Show members' }));

    await waitFor(() => {
      const after = canvas.getAllByTestId('provenance-connection').map((connector) => connector.getAttribute('d'));
      expect(after).not.toEqual(before);
    });
  },
};

export const RendersConnectionsForQuotedGroupingValues: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputD = canvas.getByText('Output D').closest('article')!;

    await userEvent.click(within(outputD).getByText('Add Analysis value'));
    await userEvent.type(screen.getByRole('textbox', { name: /Analysis value/i }), "Farmer's field");
    await userEvent.click(screen.getByRole('button', { name: /Add value/i }));
    await userEvent.click(canvas.getByTestId('provenance-property-Output-Analysis'));

    await waitFor(() => {
      const connectors = canvas.getAllByTestId('provenance-connection');
      expect(connectors).toHaveLength(4);
      expect(connectors.every((connector) => connector.getAttribute('d')?.startsWith('M '))).toBe(true);
    });
  },
};

export const CreatesPropertyValueWithoutDebug: Story = {
  render: () => <Harness debug={false} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputA = canvas.getByText('Output A').closest('article')!;

    await userEvent.click(within(outputA).getByText('Add Analysis value'));
    await userEvent.type(screen.getByRole('textbox', { name: /Analysis value/i }), 'Imaging');
    await userEvent.click(screen.getByRole('button', { name: /Add value/i }));

    await waitFor(() =>
      expect(canvas.getByText('Output A').closest('article')!).toHaveTextContent('Analysis: Imaging'),
    );
  },
};

export const CreatesOutputPropertyOnConnection: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const connector = await waitFor(() => canvas.getAllByTestId('provenance-connection')[0]);
    await userEvent.click(connector);

    const details = canvas.getByTestId('provenance-connection-details');
    expect(details).toHaveTextContent('Connection IDs');
    await userEvent.click(within(details).getByText('Add Analysis value'));
    await userEvent.type(screen.getByRole('textbox', { name: /Analysis value/i }), 'Linked analysis');
    await userEvent.click(screen.getByRole('button', { name: /Add value/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue'),
    );
  },
};

export const EditsNumericValueWithoutLosingUnit: Story = {
  render: () => <Harness fixture="typedSample" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const temperature = canvas.getByTestId('provenance-value-pv-input-a-temperature');
    expect(temperature).toHaveTextContent('Temperature: 12 degree Celsius');

    await userEvent.click(within(temperature).getByTestId('popover_trigger_provenance-edit-Temperature'));
    const value = screen.getByRole('textbox', { name: /Temperature value/i });
    await userEvent.clear(value);
    await userEvent.type(value, '13.5');
    await userEvent.click(screen.getByRole('button', { name: /Apply value/i }));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-value-pv-input-a-temperature')).toHaveTextContent(
        'Temperature: 13.5 degree Celsius',
      );
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent(
        'UpdatePropertyValue:Float:degree Celsius',
      );
    });
  },
};

export const EditsOntologyTermWithoutFlatteningIt: Story = {
  render: () => <Harness fixture="typedSample" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const instrument = canvas.getByTestId('provenance-value-pv-output-a-instrument');

    await userEvent.click(within(instrument).getByTestId('popover_trigger_provenance-edit-Instrument'));
    await userEvent.click(screen.getByRole('button', { name: /Apply value/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('UpdatePropertyValue:Term:none'),
    );
  },
};

export const CreatesNumericPropertyValue: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputD = canvas.getByText('Output D').closest('article')!;

    await userEvent.click(within(outputD).getByText('Add Analysis value'));
    await userEvent.selectOptions(screen.getByRole('combobox', { name: /Value type/i }), 'Float');
    await userEvent.type(screen.getByRole('textbox', { name: /Analysis value/i }), '1.5');
    await userEvent.click(screen.getByRole('button', { name: /Add value/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue:Float:none'),
    );
  },
};

export const RejectsNonFiniteNumericPropertyValue: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputD = canvas.getByText('Output D').closest('article')!;

    await userEvent.click(within(outputD).getByText('Add Analysis value'));
    await userEvent.selectOptions(screen.getByRole('combobox', { name: /Value type/i }), 'Float');
    await userEvent.type(screen.getByRole('textbox', { name: /Analysis value/i }), 'Infinity');

    expect(screen.getByRole('button', { name: /Add value/i })).toBeDisabled();
    expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('No patches emitted.');
  },
};

export const CreatesMatchingDataEndpointForOneSidedModel: Story = {
  render: () => <Harness fixture="dataOutputOnly" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-input'));
    expect(screen.getByRole('combobox', { name: 'Kind' })).toHaveValue('Data');
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'New Input');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedSet:Data'),
    );
  },
};

export const CreatesFreeTextEndpointHeader: Story = {
  render: () => <Harness inputOnly />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-output'));
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Kind' }), 'FreeText');
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint header/i }), 'Derived data');
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'Custom Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedSet:FreeText:Derived data'),
    );
  },
};

export const CreatesNextLayerAndKeepsBoundaryEditsSynchronized: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const outputA = canvas.getByText('Output A').closest('article')!;
    await userEvent.click(within(outputA).getByRole('button', { name: 'Select group' }));
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-pair-pair-2')).toHaveClass('swt:btn-primary');
      expect(canvasElement).toHaveTextContent('Output A');
    });

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-edit-Analysis'));
    const value = screen.getByRole('textbox', { name: /Analysis value/i });
    await userEvent.clear(value);
    await userEvent.type(value, 'Imaging');
    await userEvent.click(screen.getByRole('button', { name: /Apply value/i }));

    await userEvent.click(canvas.getByTestId('provenance-pair-pair-1'));
    await waitFor(() => expect(canvasElement).toHaveTextContent('Imaging'));
    expect(canvas.getByTestId('provenance-patch-preview')).not.toHaveTextContent('No patches emitted.');
  },
};

export const CompletesAnInputOnlyPair: Story = {
  render: () => <Harness inputOnly />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvasElement).toHaveTextContent('Input Only A');
    expect(canvasElement).toHaveTextContent('No entries in this layer');

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-output'));
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'New Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    await waitFor(() => expect(canvasElement).toHaveTextContent('New Output'));
    expect(canvas.getByTestId('provenance-patch-preview')).not.toHaveTextContent('No patches emitted.');
  },
};

export const AddsExistingPropertyToCreatedEmptySide: Story = {
  render: () => <Harness inputOnly debug={false} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText('Add output'));
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'New Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    const output = await waitFor(() => canvas.getByText('New Output').closest('article')!);
    await userEvent.click(within(output).getByText('Add Species value'));
    await userEvent.type(screen.getByRole('textbox', { name: /Species value/i }), 'Arabidopsis');
    await userEvent.click(screen.getByRole('button', { name: /Add value/i }));

    await waitFor(() =>
      expect(canvas.getByText('New Output').closest('article')!).toHaveTextContent('Species: Arabidopsis'),
    );
  },
};

async function dragByPointer(source: Element, target: Element) {
  const from = source.getBoundingClientRect();
  const to = target.getBoundingClientRect();
  fireEvent.pointerDown(source, {
    clientX: from.left + 4,
    clientY: from.top + 4,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });
  fireEvent.pointerMove(document, {
    clientX: to.left + 4,
    clientY: to.top + 4,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });
  fireEvent.pointerUp(document, {
    clientX: to.left + 4,
    clientY: to.top + 4,
    button: 0,
    buttons: 0,
    isPrimary: true,
    pointerId: 1,
  });
}

export const CopiesValueOntoAGroup: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = canvas.getByTestId('provenance-drag-value-pv-output-a-analysis');
    const target = canvas.getByText('Output D').closest('article')!;

    await dragByPointer(source, target);

    await waitFor(() => {
      expect(target).toHaveTextContent('Analysis: Mass Spectrometry');
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue');
    });
  },
};

export const ConnectsGroups: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByText('Input C').closest('article')!;
    const output = canvas.getByText('Output E').closest('article')!;
    const initialLines = canvas.queryAllByTestId('provenance-connection').length;

    await dragByPointer(within(input).getByRole('button', { name: 'Connect group' }), output);

    await waitFor(() => {
      expect(canvas.getAllByTestId('provenance-connection').length).toBe(initialLines + 1);
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedConnection');
    });
  },
};

export const AddsLayerFromMixedSelection: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputA = canvas.getByText('Input A').closest('article')!;
    const outputB = canvas.getByText('Output B').closest('article')!;

    await userEvent.click(within(inputA).getByRole('button', { name: 'Select group' }));
    await userEvent.click(within(outputB).getByRole('button', { name: 'Select group' }));
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-pair-pair-2')).toHaveClass('swt:btn-primary');
      expect(canvasElement).toHaveTextContent('Selection 3');
      expect(canvasElement).toHaveTextContent('Input A');
      expect(canvasElement).toHaveTextContent('Output B');
    });
  },
};
