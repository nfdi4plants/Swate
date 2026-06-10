import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, screen, userEvent, waitFor, within } from 'storybook/test';
import { Main as ProvenanceGrouping } from './ProvenanceGrouping.fs.js';
import {
  Exports_createSampleSession as createSampleSession,
  Exports_createInputOnlySession as createInputOnlySession,
  Exports_createOutputOnlySession as createOutputOnlySession,
  Exports_createSwitchablePropertySession as createSwitchablePropertySession,
  Exports_createTypedSampleSession as createTypedSampleSession,
  Exports_createDataOutputOnlySession as createDataOutputOnlySession,
  Exports_createRetaggedTypedSampleSession as createRetaggedTypedSampleSession,
  Exports_patchDetails as patchDetails,
} from './Types.fs.js';

type Fixture = 'sample' | 'inputOnly' | 'outputOnly' | 'switchableProperty' | 'typedSample' | 'dataOutputOnly';

function createSessionForFixture(selected: Fixture) {
  switch (selected) {
    case 'inputOnly':
      return createInputOnlySession();
    case 'outputOnly':
      return createOutputOnlySession();
    case 'switchableProperty':
      return createSwitchablePropertySession();
    case 'typedSample':
      return createTypedSampleSession();
    case 'dataOutputOnly':
      return createDataOutputOnlySession();
    default:
      return createSampleSession();
  }
}

function Harness({
  inputOnly = false,
  outputOnly = false,
  fixture = 'sample',
  debug = true,
  allowTermReplacement = false,
  allowEndpointReplacement = false,
}: {
  inputOnly?: boolean;
  outputOnly?: boolean;
  fixture?: Fixture;
  debug?: boolean;
  allowTermReplacement?: boolean;
  allowEndpointReplacement?: boolean;
}) {
  const selected = inputOnly ? 'inputOnly' : outputOnly ? 'outputOnly' : fixture;
  const [session, setSession] = React.useState(() => createSessionForFixture(selected));
  const [patches, setPatches] = React.useState<string[]>([]);

  React.useEffect(() => {
    setSession(createSessionForFixture(selected));
    setPatches([]);
  }, [selected]);

  return (
    <div className="swt:flex swt:flex-col swt:gap-4 swt:min-h-screen swt:bg-base-200 swt:p-4">
      {allowTermReplacement && (
        <button type="button" onClick={() => setSession(createRetaggedTypedSampleSession())}>
          Replace term metadata
        </button>
      )}
      {allowEndpointReplacement && (
        <button type="button" onClick={() => setSession(createOutputOnlySession())}>
          Replace endpoint context
        </button>
      )}
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
  parameters: { layout: 'fullscreen', isolated: true },
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

export const ExpandedGroupsShowMemberHoverValues: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(within(canvas.getByText('Input A').closest('article')!).queryByRole('button', { name: 'Show members' }))
      .not.toBeInTheDocument();

    await userEvent.click(canvas.getByTestId('provenance-property-Input-Species'));
    const grouped = await waitFor(() => {
      const heading = canvas
        .getAllByText('Species: Arabidopsis')
        .find((element) => element.tagName === 'H3');
      expect(heading).toBeDefined();
      return heading!.closest('article')!;
    });

    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));
    const member = within(grouped).getByTestId('provenance-group-member-Input-input-a');

    expect(within(grouped).queryByTestId('provenance-member-values-Input-input-a')).not.toBeInTheDocument();
    await userEvent.hover(member);

    await waitFor(() => {
      const details = within(grouped).getByTestId('provenance-member-values-Input-input-a');
      expect(details).toHaveTextContent('Species: Arabidopsis');
      expect(details).toHaveTextContent('Temperature: 12 C');
    });

    await userEvent.unhover(member);
  },
};

export const GroupsBothSidesFromOutputProperty: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-property-both-Output-Replicate'));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-group-Input-input:Replicate=1 | 2')).toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Output-output:Replicate=1 | 2')).toBeInTheDocument();
      expect(canvas.queryByTestId('provenance-group-Input-input:Replicate=1')).not.toBeInTheDocument();
      expect(canvas.queryByTestId('provenance-group-Output-output:Replicate=2')).not.toBeInTheDocument();
    });
  },
};

export const ConnectedOutputsKeepPropertiesInRailsAndConnections: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputA = canvas.getByText('Output A').closest('article')!;

    expect(outputA).toHaveTextContent('Output A');
    expect(outputA).not.toHaveTextContent('Analysis: Mass Spectrometry');
    expect(outputA).not.toHaveTextContent('Species: Arabidopsis');
    expect(outputA).not.toHaveTextContent('Temperature: 12 C');
    expect(canvas.getAllByTestId('provenance-connection').length).toBeGreaterThan(0);
  },
};

export const GroupingPropertiesStayOnOwningSide: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputRail = within(canvas.getByTestId('provenance-property-rail-Input'));
    const outputRail = within(canvas.getByTestId('provenance-property-rail-Output'));

    expect(inputRail.getByTestId('provenance-property-Input-Species')).toBeInTheDocument();
    expect(inputRail.getByTestId('provenance-property-Input-Temperature')).toBeInTheDocument();
    expect(inputRail.queryByTestId('provenance-property-Input-Analysis')).not.toBeInTheDocument();
    expect(inputRail.queryByTestId('provenance-property-Input-Replicate')).not.toBeInTheDocument();

    expect(outputRail.getByTestId('provenance-property-Output-Analysis')).toBeInTheDocument();
    expect(outputRail.getByTestId('provenance-property-Output-Replicate')).toBeInTheDocument();
    expect(outputRail.queryByTestId('provenance-property-Output-Species')).not.toBeInTheDocument();
    expect(outputRail.queryByTestId('provenance-property-Output-Temperature')).not.toBeInTheDocument();
  },
};

export const PropertyRailExpandsValuesAndAddControls: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputRail = within(canvas.getByTestId('provenance-property-rail-Input'));

    expect(inputRail.queryByText('Arabidopsis')).not.toBeInTheDocument();
    expect(inputRail.getByText('Add property')).toBeInTheDocument();

    const panel = await expandProperty(canvas, 'Input', 'Species');
    const arabidopsis = panel.getByText('Arabidopsis').closest('button, div')!;
    expect(arabidopsis).toBeInTheDocument();
    expect(arabidopsis).toHaveClass('swt:btn');
    expect(arabidopsis).toHaveClass('swt:btn-primary');
    expect(arabidopsis).toHaveClass('swt:w-fit');
    expect(arabidopsis).toHaveClass('swt:cursor-grab');
    expect(arabidopsis.querySelector('[class*="re-order-dots"]')).not.toBeInTheDocument();
    expect(panel.getByText('Chlamydomonas')).toBeInTheDocument();
    const addValue = panel.getByText('Add value').closest('button')!;
    expect(addValue).toHaveClass('swt:btn');
    expect(addValue).toHaveClass('swt:btn-outline');
    expect(addValue).toHaveClass('swt:w-fit');
    expect(addValue.querySelector('[class*="fluent--add-20-regular"]')).toBeInTheDocument();
  },
};

export const ExpandsPropertyValuesWithoutGrouping: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expandProperty(canvas, 'Input', 'Species');

    expect(canvas.getByTestId('provenance-group-Input-input:input-a')).toBeInTheDocument();
    expect(canvas.queryByTestId('provenance-group-Input-input:Species=Arabidopsis')).not.toBeInTheDocument();
  },
};

export const RailValueShowsDragIndicatorWhileDragging: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await railValue(canvas, 'Output', 'Analysis', 'Mass Spectrometry');

    await startDragByPointer(source);

    await waitFor(() => expect(source).toHaveClass('swt:ring-2'));
    await waitFor(() => expect(screen.getByTestId('provenance-drag-overlay-value')).toHaveTextContent('Mass Spectrometry'));
    fireEvent.pointerUp(document, {
      clientX: source.getBoundingClientRect().left + 12,
      clientY: source.getBoundingClientRect().top + 12,
      button: 0,
      buttons: 0,
      isPrimary: true,
      pointerId: 1,
    });
  },
};

export const SingleSidedPropertiesCannotSwitchSides: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.getByTestId('provenance-property-drag-Output-Analysis')).toBeDisabled();
    expect(canvas.getByTestId('provenance-property-drag-Output-Replicate')).toBeDisabled();
  },
};

export const SwitchesPropertyGroupingSideByDrag: Story = {
  render: () => <Harness fixture="switchableProperty" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-property-Input-Batch'));
    await waitFor(() => {
      expect(canvas.getByTestId('provenance-group-Input-input:Batch=A')).toBeInTheDocument();
      expect(canvas.queryByTestId('provenance-group-Output-output:Batch=A')).not.toBeInTheDocument();
    });

    await dragByPointer(
      canvas.getByTestId('provenance-property-Input-Batch'),
      canvas.getByTestId('provenance-property-rail-Output'),
    );

    await waitFor(() => {
      expect(canvas.queryByTestId('provenance-group-Input-input:Batch=A')).not.toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Input-input:input-a')).toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Output-output:Batch=A')).toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Output-output:Batch=B')).toBeInTheDocument();
    });
  },
};

export const SwitchesInheritedInputPropertyGroupingToOutputSide: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputRail = within(canvas.getByTestId('provenance-property-rail-Input'));
    const outputRail = within(canvas.getByTestId('provenance-property-rail-Output'));

    expect(canvas.getByTestId('provenance-property-drag-Input-Species')).not.toBeDisabled();
    await dragByPointer(
      canvas.getByTestId('provenance-property-Input-Species'),
      canvas.getByTestId('provenance-property-rail-Output'),
    );

    await waitFor(() => {
      expect(canvas.queryByTestId('provenance-group-Input-input:Species=Arabidopsis')).not.toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis')).toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Output-output:Species=Chlamydomonas')).toBeInTheDocument();
      expect(outputRail.getByTestId('provenance-property-Output-Species')).toBeInTheDocument();
      expect(inputRail.queryByTestId('provenance-property-Input-Species')).not.toBeInTheDocument();
    });
  },
};

export const ClicksSwapHandleToSwitchGroupingSide: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputRail = within(canvas.getByTestId('provenance-property-rail-Input'));
    const outputRail = within(canvas.getByTestId('provenance-property-rail-Output'));

    await userEvent.click(canvas.getByTestId('provenance-property-drag-Input-Species'));

    await waitFor(() => {
      expect(canvas.queryByTestId('provenance-group-Input-input:Species=Arabidopsis')).not.toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis')).toBeInTheDocument();
      expect(outputRail.getByTestId('provenance-property-Output-Species')).toBeInTheDocument();
      expect(inputRail.queryByTestId('provenance-property-Input-Species')).not.toBeInTheDocument();
    });
  },
};

export const RegroupedValuesAreReadOnlyOnCards: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-property-Input-Species'));
    const grouped = await waitFor(
      () => {
        const heading = canvas
          .getAllByText('Species: Chlamydomonas')
          .find((element) => element.tagName === 'H3');
        expect(heading).toBeDefined();
        return heading!.closest('article')!;
      },
      { timeout: 3000 },
    );

    const species = within(grouped).queryByTestId('provenance-value-pv-input-d-species');
    expect(species).not.toBeInTheDocument();
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
    fireEvent.click(canvas.getByTestId('provenance-property-Input-Species'));

    const grouped = await waitFor(() => {
      const heading = canvas
        .getAllByText('Species: Arabidopsis')
        .find((element) => element.tagName === 'H3');
      expect(heading).toBeDefined();
      return heading!.closest('article')!;
    });

    const before = await waitFor(() => {
      const paths = canvas.getAllByTestId('provenance-connection').map((connector) => connector.getAttribute('d'));
      expect(paths.length).toBeGreaterThan(0);
      return paths;
    });

    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));

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
    const source = await addRailValue(canvas, 'Output', 'Analysis', "Farmer's field");
    await groupByProperty(canvas, 'Output', 'Analysis');
    const outputD = canvas.getByText('Output D').closest('article')!;

    await dragByPointer(source, outputD);

    await waitFor(() => {
      const connectors = canvas.getAllByTestId('provenance-connection');
      expect(connectors).toHaveLength(4);
      expect(connectors.every((connector) => connector.getAttribute('d')?.startsWith('M '))).toBe(true);
    });
  },
};

export const ShowsLiveConnectionPreviewWhileDraggingHandle: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByText('Input C').closest('article')!;
    const handle = within(input).getByTestId('provenance-connection-handle-Input-GroupCard');

    await startDragByPointer(handle);

    await waitFor(() => {
      const preview = canvas.getByTestId('provenance-live-connection');
      expect(preview.getAttribute('d')).toMatch(/^M\s+\d/);
    });

    fireEvent.pointerUp(document, { button: 0, buttons: 0, isPrimary: true, pointerId: 1 });
  },
};

export const ExpandedGroupsRenderMemberLevelConnections: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    fireEvent.click(canvas.getByTestId('provenance-property-Input-Species'));

    const grouped = await waitFor(() => {
      const heading = canvas
        .getAllByText('Species: Arabidopsis')
        .find((element) => element.tagName === 'H3');
      expect(heading).toBeDefined();
      return heading!.closest('article')!;
    });

    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));

    await waitFor(() => {
      const paths = canvas.getAllByTestId('provenance-member-connection');
      expect(paths.length).toBeGreaterThan(0);
      expect(paths.every((path) => path.getAttribute('d')?.startsWith('M '))).toBe(true);
    });
  },
};

const connectionKeys = (paths: HTMLElement[]) =>
  paths.map((path) => path.getAttribute('data-provenance-connection-key') ?? '');

export const ExpandedPropertyValuesConnectToEveryMatchingGroup: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expandProperty(canvas, 'Input', 'Species');

    await waitFor(() => {
      const paths = canvas.getAllByTestId('provenance-value-connection');
      // Arabidopsis chip -> Input A/B/C, Chlamydomonas chip -> Input D.
      expect(paths).toHaveLength(4);
      expect(paths.every((path) => path.getAttribute('d')?.startsWith('M '))).toBe(true);
    });

    // Expanding Species swaps its header lines for the per-value lines above.
    const headerKeys = connectionKeys(canvas.getAllByTestId('provenance-property-connection'));
    expect(headerKeys.some((key) => key.includes('Species'))).toBe(false);
  },
};

export const CollapsedPropertiesConnectToMatchingGroupsAutomatically: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(() => {
      const paths = canvas.getAllByTestId('provenance-property-connection');
      expect(paths.every((path) => path.getAttribute('d')?.startsWith('M '))).toBe(true);
      // Species has Arabidopsis on Input A/B/C and Chlamydomonas on Input D; lines to
      // groups closer than the minimum connector distance are intentionally skipped.
      const speciesLines = connectionKeys(paths).filter((key) => key.includes('Species'));
      expect(speciesLines.length).toBeGreaterThanOrEqual(3);
    });

    // Property headers expose no draggable connection handles; their connectors derive
    // from the values they contain.
    expect(canvas.queryByTestId('provenance-connection-handle-Input-PropertyHeader')).not.toBeInTheDocument();
    expect(canvas.queryByTestId('provenance-connection-handle-Output-PropertyHeader')).not.toBeInTheDocument();
  },
};

export const AssignedValueConnectionsFollowModelData: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await railValue(canvas, 'Output', 'Analysis', 'Mass Spectrometry');

    await waitFor(() => {
      // Mass Spectrometry -> Output B at least; Output A may sit below the minimum
      // connector distance. Output D has no Analysis value yet, so no line reaches it.
      const keys = connectionKeys(canvas.getAllByTestId('provenance-value-connection'));
      expect(keys.length).toBeGreaterThanOrEqual(2);
      expect(keys.some((key) => key.includes('output-d'))).toBe(false);
    });

    const handle = within(source as HTMLElement).getByTestId('provenance-connection-handle-Output-PropertyValue');
    const target = canvas.getByText('Output D').closest('article')!;
    await dragByPointer(handle, target);

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue');
      // The committed value immediately yields a Mass Spectrometry line to Output D.
      const keys = connectionKeys(canvas.getAllByTestId('provenance-value-connection'));
      expect(keys.some((key) => key.includes('output-d'))).toBe(true);
    });
  },
};

export const CreatesPropertyValueFromRail: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await addRailValue(canvas, 'Output', 'Analysis', 'Imaging');
    await groupByProperty(canvas, 'Output', 'Analysis');
    const outputD = canvas.getByText('Output D').closest('article')!;

    await dragByPointer(source, outputD);

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue');
      expect(canvas.getByTestId('provenance-group-Output-output:Analysis=Imaging')).toBeInTheDocument();
    });
  },
};

export const ConnectionDetailsDoNotExposePropertyCreation: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const connector = await waitFor(() => canvas.getAllByTestId('provenance-connection')[0]);
    await userEvent.click(connector);

    const details = await waitFor(() => canvas.getByTestId('provenance-connection-details'));
    expect(details).toHaveTextContent('Connection IDs');
    expect(within(details).queryByText(/Add value/i)).not.toBeInTheDocument();
    expect(within(details).queryByText(/Add property/i)).not.toBeInTheDocument();
  },
};

export const SelectsConnectionWithKeyboard: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const connector = await waitFor(() => canvas.getAllByTestId('provenance-connection')[0]);

    expect(connector).toHaveAttribute('role', 'button');
    expect(connector).toHaveAttribute('aria-label');
    expect(connector).toHaveAttribute('tabindex', '0');
    connector.focus();
    await userEvent.keyboard('{Enter}');

    await waitFor(() => expect(canvas.getByTestId('provenance-connection-details')).toBeInTheDocument());

    const secondConnector = canvas.getAllByTestId('provenance-connection')[1];
    const secondLabel = secondConnector.getAttribute('aria-label')!.replace('Select connection ', '');
    secondConnector.focus();
    await userEvent.keyboard(' ');

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-connection-details')).toHaveTextContent(`Connection: ${secondLabel}`),
    );
  },
};

export const WarnsBeforeOverwritingSingleValueFromRail: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await railValue(canvas, 'Input', 'Species', 'Arabidopsis');
    await groupByProperty(canvas, 'Input', 'Species');
    const target = canvas
      .getAllByText('Species: Chlamydomonas')
      .find((element) => element.tagName === 'H3')!
      .closest('article')!;

    await dragByPointer(source, target);

    await waitFor(() => expect(canvas.getByTestId('provenance-overwrite-warning')).toBeInTheDocument());
    expect(canvas.getByTestId('provenance-overwrite-warning')).toHaveTextContent('Overwrite Species value?');
    await userEvent.click(canvas.getByTestId('provenance-confirm-overwrite'));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('UpdatePropertyValue:Text:none');
    });
  },
};

export const RejectsOverwriteWhenTargetHasMultipleValues: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await railValue(canvas, 'Output', 'Replicate', '1');
    await groupByProperty(canvas, 'Output', 'Replicate');
    const target = canvas
      .getAllByText('Replicate: 1 | 2')
      .find((element) => element.tagName === 'H3')!
      .closest('article')!;

    await dragByPointer(source, target);

    await waitFor(() => expect(canvas.getByText(/Cannot overwrite Replicate/i)).toBeInTheDocument());
    expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('No patches emitted.');
  },
};

export const RefreshesRailTermValueAfterControlledMetadataReplacement: Story = {
  render: () => <Harness fixture="typedSample" allowTermReplacement />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /Replace term metadata/i }));
    const source = await railValue(canvas, 'Output', 'Instrument', 'mass spectrometer');
    expect(source).toBeInTheDocument();
  },
};

export const CreatesNumericPropertyValue: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await addRailValue(canvas, 'Output', 'Analysis', '1.5', 'Float');
    const outputD = canvas.getByText('Output D').closest('article')!;

    await dragByPointer(source, outputD);

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue:Float:none'),
    );
  },
};

export const RejectsNonFiniteNumericPropertyValue: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const panel = await expandProperty(canvas, 'Output', 'Analysis');

    await userEvent.click(panel.getByText('Add value'));
    await userEvent.selectOptions(screen.getByRole('combobox', { name: /Value type/i }), 'Float');
    await userEvent.type(screen.getByRole('textbox', { name: /Analysis value/i }), 'Infinity');

    const submit = screen
      .getAllByRole('button', { name: /^Add value$/i })
      .find((button) => button.getAttribute('type') === 'submit')!;
    expect(submit).toBeDisabled();
    expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('No patches emitted.');
  },
};

export const CreatesMatchingDataEndpointForOneSidedModel: Story = {
  render: () => <Harness fixture="dataOutputOnly" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-input'));
    expect(screen.getByRole('textbox', { name: /Endpoint kind/i })).toHaveValue('Data');
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'New Input');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedSet:fixture:endpoint:data:Data'),
    );
  },
};

export const RefreshesEndpointKindAfterControlledSessionReplacement: Story = {
  render: () => <Harness fixture="dataOutputOnly" allowEndpointReplacement />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /Replace endpoint context/i }));
    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-input'));

    expect(screen.getByRole('textbox', { name: /Endpoint kind/i })).toHaveValue('Sample');
  },
};

export const CreatesFreeTextEndpointHeader: Story = {
  render: () => <Harness inputOnly />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-output'));
    await userEvent.clear(screen.getByRole('textbox', { name: /Endpoint kind/i }));
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint kind/i }), 'Derived data');
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'Custom Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent(
        'AddLoadedSet:editor:endpoint:Derived%20data:Derived data',
      ),
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

    const source = await addRailValue(canvas, 'Input', 'Analysis', 'Imaging');
    await groupByProperty(canvas, 'Input', 'Analysis');
    const carried = canvas
      .getAllByText('Analysis: Mass Spectrometry')
      .find((element) => element.tagName === 'H3')!
      .closest('article')!;
    await dragByPointer(source, carried);
    await userEvent.click(canvas.getByTestId('provenance-confirm-overwrite'));

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
  render: () => <Harness inputOnly />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-add-output-trigger'));
    const endpoint = await waitFor(() => screen.getByRole('textbox', { name: /Endpoint name/i }));
    await userEvent.type(endpoint, 'New Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    const output = await waitFor(() => canvas.getByText('New Output').closest('article')!);
    const source = await railValue(canvas, 'Input', 'Species', 'Arabidopsis');
    await dragByPointer(source, output);

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue'),
    );
    expect(canvas.getByText('New Output').closest('article')!).not.toHaveTextContent('Species: Arabidopsis');
  },
};

export const AddsNewPropertyFromRail: Story = {
  render: () => <Harness inputOnly />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const target = canvas.getByText('Input Only A').closest('article')!;
    const source = await addRailProperty(canvas, 'Input', 'Treatment', 'Drought');
    await dragByPointer(source, target);

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue'),
    );
    expect(target).not.toHaveTextContent('Treatment: Drought');
  },
};

async function dragByPointer(source: Element, target: Element) {
  const from = source.getBoundingClientRect();
  const to = target.getBoundingClientRect();
  const fromX = from.left + from.width / 2;
  const fromY = from.top + from.height / 2;
  const toX = to.left + to.width / 2;
  const toY = to.top + to.height / 2;
  const deltaX = toX - fromX;
  const deltaY = toY - fromY;
  const distance = Math.hypot(deltaX, deltaY) || 1;
  const activationX = fromX + (deltaX / distance) * 8;
  const activationY = fromY + (deltaY / distance) * 8;
  const nextFrame = () => new Promise((resolve) => requestAnimationFrame(resolve));
  fireEvent.pointerDown(source, {
    clientX: fromX,
    clientY: fromY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });
  await nextFrame();
  fireEvent.pointerMove(target, {
    clientX: activationX,
    clientY: activationY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });
  await nextFrame();
  fireEvent.pointerMove(document, {
    clientX: toX,
    clientY: toY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });
  await nextFrame();
  fireEvent.pointerUp(target, {
    clientX: toX,
    clientY: toY,
    button: 0,
    buttons: 0,
    isPrimary: true,
    pointerId: 1,
  });
  await nextFrame();
}

async function startDragByPointer(source: Element) {
  const from = source.getBoundingClientRect();
  const fromX = from.left + from.width / 2;
  const fromY = from.top + from.height / 2;
  const nextFrame = () => new Promise((resolve) => requestAnimationFrame(resolve));
  fireEvent.pointerDown(source, {
    clientX: fromX,
    clientY: fromY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });
  await nextFrame();
  fireEvent.pointerMove(document, {
    clientX: fromX + 8,
    clientY: fromY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });
  await nextFrame();
}

async function expandProperty(canvas: ReturnType<typeof within>, side: 'Input' | 'Output', propertyName: string) {
  const panelId = `provenance-property-values-${side}-${propertyName}`;
  const triggerId = `provenance-property-expand-${side}-${propertyName}`;
  for (let attempt = 0; attempt < 3 && !canvas.queryByTestId(panelId); attempt += 1) {
    await userEvent.click(canvas.getByTestId(triggerId));
    await waitFor(() => expect(canvas.getByTestId(panelId)).toBeInTheDocument(), { timeout: 1000 }).catch(() => {
      if (!canvas.queryByTestId(panelId)) {
        fireEvent.click(canvas.getByTestId(triggerId));
      }
    });
  }
  await waitFor(() => expect(canvas.getByTestId(panelId)).toBeInTheDocument(), { timeout: 3000 });
  return within(canvas.getByTestId(panelId));
}

async function groupByProperty(canvas: ReturnType<typeof within>, side: 'Input' | 'Output', propertyName: string) {
  await userEvent.click(canvas.getByTestId(`provenance-property-${side}-${propertyName}`));
  await waitFor(
    () => {
      const groupedHeading = canvas
        .getAllByText((content, element) => element?.tagName === 'H3' && content.startsWith(`${propertyName}:`))
        .at(0);
      expect(groupedHeading).toBeDefined();
    },
    { timeout: 3000 },
  );
}

async function railValue(
  canvas: ReturnType<typeof within>,
  side: 'Input' | 'Output',
  propertyName: string,
  valueText: string,
) {
  const panel = await expandProperty(canvas, side, propertyName);
  const value = await waitFor(() => panel.getByText(valueText).closest('button, [role="button"]'));
  expect(value).toBeTruthy();
  return value!;
}

async function addRailValue(
  canvas: ReturnType<typeof within>,
  side: 'Input' | 'Output',
  propertyName: string,
  valueText: string,
  valueType = 'Text',
) {
  const panel = await expandProperty(canvas, side, propertyName);
  await userEvent.click(panel.getByText('Add value'));
  if (valueType !== 'Text') {
    await userEvent.selectOptions(screen.getByRole('combobox', { name: /Value type/i }), valueType);
  }
  await userEvent.type(screen.getByRole('textbox', { name: new RegExp(`${propertyName} value`, 'i') }), valueText);
  const submit = screen
    .getAllByRole('button', { name: /^Add value$/i })
    .find((button) => button.getAttribute('type') === 'submit')!;
  await userEvent.click(submit);
  await userEvent.keyboard('{Escape}');
  return railValue(canvas, side, propertyName, valueText);
}

async function addRailProperty(
  canvas: ReturnType<typeof within>,
  side: 'Input' | 'Output',
  propertyName: string,
  valueText: string,
) {
  const rail = within(canvas.getByTestId(`provenance-property-rail-${side}`));
  const addPropertyTrigger = within(rail.getByTestId('popover_trigger_provenance-add-value-Property'))
    .getByText('Add property')
    .closest('button')!;
  fireEvent.click(addPropertyTrigger);
  const category = await waitFor(() => screen.getAllByTestId('term-search-input')[0]).catch(async () => {
    fireEvent.click(addPropertyTrigger);
    return waitFor(() => screen.getAllByTestId('term-search-input')[0]);
  });
  await userEvent.type(category, propertyName);
  await userEvent.keyboard('{Escape}');
  await userEvent.type(screen.getByRole('textbox', { name: new RegExp(`${propertyName} value`, 'i') }), valueText);
  const submit = screen
    .getAllByRole('button', { name: /^Add property$/i })
    .find((button) => button.getAttribute('type') === 'submit')!;
  await userEvent.click(submit);
  await userEvent.keyboard('{Escape}');
  return railValue(canvas, side, propertyName, valueText);
}

export const CopiesValueOntoAGroup: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await railValue(canvas, 'Output', 'Analysis', 'Mass Spectrometry');
    const target = canvas.getByText('Output D').closest('article')!;

    await dragByPointer(source, target);

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue');
    });
  },
};

export const ResizesThreePanelLayout: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const surface = canvas.getByTestId('provenance-surface');
    const leftSplitter = canvas.getByTestId('provenance-left-splitter');
    const before = surface.getAttribute('style');
    const surfaceRect = surface.getBoundingClientRect();
    const splitterRect = leftSplitter.getBoundingClientRect();

    fireEvent.pointerDown(leftSplitter, {
      clientX: splitterRect.left + 2,
      clientY: splitterRect.top + 8,
      button: 0,
      buttons: 1,
      isPrimary: true,
      pointerId: 11,
    });
    fireEvent.pointerMove(document, {
      clientX: surfaceRect.left + surfaceRect.width * 0.32,
      clientY: splitterRect.top + 8,
      button: 0,
      buttons: 1,
      isPrimary: true,
      pointerId: 11,
    });
    fireEvent.pointerUp(document, {
      button: 0,
      buttons: 0,
      isPrimary: true,
      pointerId: 11,
    });

    await waitFor(() => expect(surface.getAttribute('style')).not.toEqual(before));
    expect(surface.getAttribute('style')).toContain('grid-template-columns');
  },
};

export const ConnectsGroups: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByText('Input C').closest('article')!;
    const output = canvas.getByText('Output E').closest('article')!;

    await dragByPointer(
      within(input).getByTestId('provenance-connection-handle-Input-GroupCard'),
      output,
    );

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedConnection');
    });
  },
};

export const InvalidSameSideConnectionDropIsIgnored: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputA = canvas.getByText('Input A').closest('article')!;
    const inputB = canvas.getByText('Input B').closest('article')!;
    const initialLines = canvas.queryAllByTestId('provenance-connection').length;

    await dragByPointer(
      within(inputA).getByTestId('provenance-connection-handle-Input-GroupCard'),
      inputB,
    );

    await waitFor(() => expect(canvas.queryAllByTestId('provenance-connection')).toHaveLength(initialLines));
    expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('No patches emitted.');
  },
};

export const MismatchedGroupConnectionPromptsForResolution: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    fireEvent.click(canvas.getByTestId('provenance-property-Input-Species'));

    const inputGroup = await waitFor(
      () => canvas.getAllByText('Species: Arabidopsis').find((element) => element.tagName === 'H3')!.closest('article')!,
    );
    const outputGroup = canvas.getByText('Output E').closest('article')!;

    await dragByPointer(
      within(inputGroup).getByTestId('provenance-connection-handle-Input-GroupCard'),
      outputGroup,
    );

    await waitFor(() => expect(canvas.getByTestId('provenance-member-resolution-prompt')).toBeInTheDocument());
    expect(canvas.getByTestId('provenance-member-resolution-prompt')).toHaveTextContent('3 input members');
    expect(canvas.getByTestId('provenance-member-resolution-prompt')).toHaveTextContent('1 output member');
  },
};

export const ManualMismatchResolutionExpandsMembersWithoutPatches: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await groupByProperty(canvas, 'Input', 'Species');

    const inputGroup = await waitFor(() => canvas.getByTestId('provenance-group-Input-input:Species=Arabidopsis'));
    const outputGroup = canvas.getByText('Output E').closest('article')!;

    await dragByPointer(
      within(inputGroup).getByTestId('provenance-connection-handle-Input-GroupCard'),
      outputGroup,
    );
    await waitFor(() => expect(canvas.getByTestId('provenance-member-resolution-prompt')).toBeInTheDocument());
    await userEvent.click(canvas.getByTestId('provenance-member-resolution-manual'));

    await waitFor(() => {
      const currentInputGroup = canvas.getByTestId('provenance-group-Input-input:Species=Arabidopsis');
      const currentOutputGroup = canvas.getByTestId('provenance-group-Output-output:output-e');
      expect(within(currentInputGroup).getAllByTestId('provenance-connection-handle-Input-GroupMember').length).toBeGreaterThan(0);
      expect(within(currentOutputGroup).getAllByTestId('provenance-connection-handle-Output-GroupMember').length).toBeGreaterThan(0);
    });
    expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('No patches emitted.');
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

export const DoesNotReuseSelectionForEqualGroupIdsInDifferentPairs: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputA = canvas.getByText('Output A').closest('article')!;

    await userEvent.click(within(outputA).getByRole('button', { name: 'Select group' }));
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-output'));
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'Pair 2 Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));
    const pair2Output = await waitFor(() => canvas.getByText('Pair 2 Output').closest('article')!);

    await userEvent.click(within(pair2Output).getByRole('button', { name: 'Select group' }));
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-output'));
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'Pair 3 Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));
    const pair3Output = await waitFor(() => canvas.getByText('Pair 3 Output').closest('article')!);

    expect(pair3Output).not.toHaveClass('swt:border-primary');
  },
};
