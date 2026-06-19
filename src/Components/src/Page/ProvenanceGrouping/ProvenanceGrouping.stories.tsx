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
  title: 'Page Components/ProvenanceGrouping',
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

    await userEvent.click(canvas.getByTestId('provenance-property-Output-Species'));
    const grouped = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));
    expect(canvas.getByTestId('provenance-group-Output-output:Species=Chlamydomonas')).toBeInTheDocument();

    // The grouping shows as an organizer tab "Category: Value" on top of the member folder.
    const tab = within(grouped).getByTestId('provenance-group-tab-Output-output:Species=Arabidopsis-0');
    expect(tab).toHaveTextContent('Species: Arabidopsis');

    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));
    await waitFor(() => expect(grouped).toHaveTextContent('Output A'));
  },
};

export const ExpandedGroupsShowMemberHoverValues: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(within(canvas.getByText('Output A').closest('article')!).queryByRole('button', { name: 'Show members' }))
      .not.toBeInTheDocument();

    await userEvent.click(canvas.getByTestId('provenance-property-Output-Species'));
    const grouped = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));

    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));
    const member = within(grouped).getByTestId('provenance-group-member-Output-output-a');

    expect(within(grouped).queryByTestId('provenance-member-values-Output-output-a')).not.toBeInTheDocument();
    await userEvent.hover(member);

    await waitFor(() => {
      const details = within(grouped).getByTestId('provenance-member-values-Output-output-a');
      expect(details).toHaveTextContent('Species: Arabidopsis');
      expect(details).toHaveTextContent('Analysis: Mass Spectrometry');
    });

    await userEvent.unhover(member);
  },
};

export const ShowsEntityTypesAndCollapsedSymbols: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-property-Output-Species'));
    const grouped = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));

    // The collapsed card previews its member types as symbols instead of a bare "×3" count.
    expect(within(grouped).getByTestId('provenance-group-symbols-Output-output:Species=Arabidopsis'))
      .toBeInTheDocument();
    expect(grouped).not.toHaveTextContent('×3');

    // Expanding shows each member with its endpoint type ("Sample") above the name.
    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));
    const member = within(grouped).getByTestId('provenance-group-member-Output-output-a');
    expect(member).toHaveTextContent('Sample');
    expect(member).toHaveTextContent('Output A');
  },
};

export const HoveringGroupTabHighlightsItAndKeepsFolderPreview: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-property-Output-Species'));
    const grouped = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));
    const tab = within(grouped).getByTestId('provenance-group-tab-Output-output:Species=Arabidopsis-0');

    expect(tab).toHaveAttribute('data-hovered', 'false');

    // Hovering the tab highlights it and the folder previews that tab's members.
    await userEvent.hover(tab);
    await waitFor(() => expect(tab).toHaveAttribute('data-hovered', 'true'));
    expect(within(grouped).getByTestId('provenance-group-symbols-Output-output:Species=Arabidopsis'))
      .toBeInTheDocument();

    await userEvent.unhover(tab);
    await waitFor(() => expect(tab).toHaveAttribute('data-hovered', 'false'));
  },
};

export const ShowsFileTypeForDataEndpoints: Story = {
  render: () => <Harness fixture="dataOutputOnly" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // A Data endpoint surfaces as a "File" type line above the single-entity card name.
    const card = await waitFor(() => canvas.getByText('Data Output A').closest('article')!);
    expect(card).toHaveTextContent('File');
  },
};

export const GroupCardsSelectFromCardSurface: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputA = canvas.getByText('Output A').closest('article')!;

    expect(within(outputA).queryByRole('button', { name: 'Select group' })).not.toBeInTheDocument();

    const selectionSurface = outputA.querySelector<HTMLElement>('[data-testid^="provenance-group-select-surface-"]')!;
    await userEvent.click(selectionSurface);
    await waitFor(() => expect(outputA).toHaveClass('swt:border-primary'));
  },
};

export const GroupsBothSidesFromOutputProperty: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.hover(canvas.getByTestId('provenance-property-Output-Replicate'));
    await userEvent.click(canvas.getByTestId('provenance-property-both-Output-Replicate'));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-group-Input-input:Replicate=1 | 2')).toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Output-output:Replicate=1 | 2')).toBeInTheDocument();
      expect(canvas.queryByTestId('provenance-group-Input-input:Replicate=1')).not.toBeInTheDocument();
      expect(canvas.queryByTestId('provenance-group-Output-output:Replicate=2')).not.toBeInTheDocument();
    });
  },
};

export const MissingSecondGroupingKeyKeepsAvailableGroupingKeys: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.hover(canvas.getByTestId('provenance-property-Output-Species'));
    await userEvent.click(canvas.getByTestId('provenance-property-both-Output-Species'));
    await userEvent.hover(canvas.getByTestId('provenance-property-Output-Temperature'));
    await userEvent.click(canvas.getByTestId('provenance-property-both-Output-Temperature'));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-group-Input-input:Species=Arabidopsis|Temperature=12 C'))
        .toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Input-input:Species=Arabidopsis|Temperature=24 C'))
        .toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Input-input:Species=Chlamydomonas')).toBeInTheDocument();
      expect(canvas.queryByTestId('provenance-group-Input-input:input-d')).not.toBeInTheDocument();
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

    expect(inputRail.getByTestId('provenance-property-Input-Previous Treatment')).toBeInTheDocument();
    expect(inputRail.queryByTestId('provenance-property-Input-Species')).not.toBeInTheDocument();
    expect(inputRail.queryByTestId('provenance-property-Input-Temperature')).not.toBeInTheDocument();
    expect(inputRail.queryByTestId('provenance-property-Input-Analysis')).not.toBeInTheDocument();
    expect(inputRail.queryByTestId('provenance-property-Input-Replicate')).not.toBeInTheDocument();

    expect(outputRail.getByTestId('provenance-property-Output-Analysis')).toBeInTheDocument();
    expect(outputRail.getByTestId('provenance-property-Output-Replicate')).toBeInTheDocument();
    expect(outputRail.getByTestId('provenance-property-Output-Species')).toBeInTheDocument();
    expect(outputRail.getByTestId('provenance-property-Output-Temperature')).toBeInTheDocument();
    expect(outputRail.queryByTestId('provenance-property-Output-Previous Treatment')).not.toBeInTheDocument();
  },
};

export const ToolbarUsesSinglePropertySortAndOriginButtons: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const toolbar = within(canvas.getByTestId('provenance-filter-toolbar'));

    expect(toolbar.getByPlaceholderText('Search properties & values...')).toBeInTheDocument();

    await userEvent.click(toolbar.getByRole('button', { name: /^Sort By$/i }));
    expect(toolbar.getByRole('button', { name: /^Property Value Count$/i })).toBeInTheDocument();
    expect(toolbar.getByRole('button', { name: /^Name$/i })).toBeInTheDocument();
    expect(toolbar.getAllByRole('button', { name: /^Connection Count$/i })).toHaveLength(1);

    expect(toolbar.getByRole('button', { name: /^Show upstream properties$/i }).querySelector('[class*="fluent--arrow-up-20"]'))
      .toBeInTheDocument();
    expect(toolbar.getByRole('button', { name: /^Show current properties$/i }).querySelector('[class*="fluent--circle-20-filled"]'))
      .toBeInTheDocument();
    const both = toolbar.getByRole('button', { name: /^Show current and upstream properties$/i });
    expect(both.querySelector('[class*="fluent--arrow-up-20"]')).toBeInTheDocument();
    expect(both.querySelector('[class*="fluent--circle-20-filled"]')).toBeInTheDocument();
  },
};

export const SortsPropertiesByNameAndConnectionCount: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const toolbar = within(canvas.getByTestId('provenance-filter-toolbar'));

    await userEvent.click(toolbar.getByRole('button', { name: /^Sort By$/i }));
    await userEvent.click(toolbar.getByRole('button', { name: /^Name$/i }));

    await waitFor(() => {
      expect(propertyOrder(canvas, 'Output').slice(0, 4)).toEqual([
        'Analysis',
        'Replicate',
        'Species',
        'Temperature',
      ]);
    });

    await userEvent.click(toolbar.getByRole('button', { name: /^Sort By$/i }));
    await userEvent.click(toolbar.getByRole('button', { name: /^Connection Count$/i }));

    await waitFor(() => {
      expect(propertyOrder(canvas, 'Output').slice(0, 4)).toEqual([
        'Species',
        'Analysis',
        'Temperature',
        'Replicate',
      ]);
    });
  },
};

export const AddedRailPropertiesAreCurrentAndPinnedToTheirSide: Story = {
  render: () => <Harness inputOnly />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputRail = within(canvas.getByTestId('provenance-property-rail-Input'));
    const outputRail = within(canvas.getByTestId('provenance-property-rail-Output'));

    const source = await addRailProperty(canvas, 'Input', 'Treatment', 'Drought');
    expect(inputRail.getByTestId('provenance-property-Input-Treatment')).toBeInTheDocument();
    expect(within(inputRail.getByTestId('provenance-property-Input-Treatment')).getByTitle('Current')).toBeInTheDocument();
    expect(outputRail.queryByTestId('provenance-property-Output-Treatment')).not.toBeInTheDocument();

    await userEvent.click(within(canvas.getByTestId('provenance-filter-toolbar')).getByRole('button', { name: /^Show current properties$/i }));
    await waitFor(() => expect(inputRail.getByTestId('provenance-property-Input-Treatment')).toBeInTheDocument());
    expect(outputRail.queryByTestId('provenance-property-Output-Treatment')).not.toBeInTheDocument();

    const target = canvas.getByText('Input Only A').closest('article')!;
    await dragByPointer(source, target);

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue'),
    );
  },
};

export const LayerFocusDoesNotResortInitializedRails: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const initialOutputOrder = propertyOrder(canvas, 'Output').slice(0, 4);

    await selectGroup(canvas.getByText('Output A').closest('article')!);
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));
    await waitFor(() => expect(canvas.getByTestId('provenance-layer-layer-2')).toHaveClass('swt:btn-primary'));

    const toolbar = within(canvas.getByTestId('provenance-filter-toolbar'));
    await userEvent.click(toolbar.getByRole('button', { name: /^Sort By$/i }));
    await userEvent.click(toolbar.getByRole('button', { name: /^Connection Count$/i }));

    await userEvent.click(canvas.getByTestId('provenance-layer-layer-1'));
    await waitFor(() => expect(canvas.getByTestId('provenance-layer-layer-1')).toHaveClass('swt:btn-primary'));
    expect(propertyOrder(canvas, 'Output').slice(0, 4)).toEqual(initialOutputOrder);
  },
};

export const PropertyRailExpandsValuesAndAddControls: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputRail = within(canvas.getByTestId('provenance-property-rail-Output'));

    expect(outputRail.queryByText('Arabidopsis')).not.toBeInTheDocument();
    expect(outputRail.getByText('Add property')).toBeInTheDocument();

    const panel = await expandProperty(canvas, 'Output', 'Species');
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

    await expandProperty(canvas, 'Output', 'Species');

    expect(canvas.getByTestId('provenance-group-Output-output:output-a')).toBeInTheDocument();
    expect(canvas.queryByTestId('provenance-group-Output-output:Species=Arabidopsis')).not.toBeInTheDocument();
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

    await userEvent.click(canvas.getByTestId('provenance-property-Output-Batch'));
    await waitFor(() => {
      expect(canvas.getByTestId('provenance-group-Output-output:Batch=A')).toBeInTheDocument();
      expect(canvas.queryByTestId('provenance-group-Input-input:Batch=A')).not.toBeInTheDocument();
    });

    await dragByPointer(
      canvas.getByTestId('provenance-property-Output-Batch'),
      canvas.getByTestId('provenance-property-rail-Input'),
    );

    await waitFor(() => {
      expect(canvas.queryByTestId('provenance-group-Output-output:Batch=A')).not.toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Output-output:output-a')).toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Input-input:Batch=A')).toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Input-input:input-b')).toBeInTheDocument();
    });
  },
};

export const SwitchesInheritedPropertyGroupingToInputSide: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputRail = within(canvas.getByTestId('provenance-property-rail-Input'));
    const outputRail = within(canvas.getByTestId('provenance-property-rail-Output'));

    expect(canvas.getByTestId('provenance-property-drag-Output-Species')).not.toBeDisabled();
    await dragByPointer(
      canvas.getByTestId('provenance-property-Output-Species'),
      canvas.getByTestId('provenance-property-rail-Input'),
    );

    await waitFor(() => {
      expect(canvas.queryByTestId('provenance-group-Output-output:Species=Arabidopsis')).not.toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Input-input:Species=Arabidopsis')).toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Input-input:Species=Chlamydomonas')).toBeInTheDocument();
      expect(inputRail.getByTestId('provenance-property-Input-Species')).toBeInTheDocument();
      expect(outputRail.queryByTestId('provenance-property-Output-Species')).not.toBeInTheDocument();
    });
  },
};

export const ClicksSwapHandleToSwitchGroupingSide: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputRail = within(canvas.getByTestId('provenance-property-rail-Input'));
    const outputRail = within(canvas.getByTestId('provenance-property-rail-Output'));

    await userEvent.hover(canvas.getByTestId('provenance-property-Output-Species'));
    await userEvent.click(canvas.getByTestId('provenance-property-drag-Output-Species'));

    await waitFor(() => {
      expect(canvas.queryByTestId('provenance-group-Output-output:Species=Arabidopsis')).not.toBeInTheDocument();
      expect(canvas.getByTestId('provenance-group-Input-input:Species=Arabidopsis')).toBeInTheDocument();
      expect(inputRail.getByTestId('provenance-property-Input-Species')).toBeInTheDocument();
      expect(outputRail.queryByTestId('provenance-property-Output-Species')).not.toBeInTheDocument();
    });
  },
};

export const RegroupedValuesAreReadOnlyOnCards: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('provenance-property-Output-Species'));
    const grouped = await waitFor(
      () => canvas.getByTestId('provenance-group-Output-output:Species=Chlamydomonas'),
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
    fireEvent.click(canvas.getByTestId('provenance-property-Output-Species'));

    const grouped = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));

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
    await waitFor(() => expect(canvas.queryByTestId('provenance-live-connection')).not.toBeInTheDocument());
  },
};

export const ExpandedGroupsRenderMemberLevelConnections: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await groupByProperty(canvas, 'Output', 'Species');

    const grouped = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));

    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));

    await waitFor(() => {
      const paths = canvas.getAllByTestId('provenance-member-connection');
      expect(paths.length).toBeGreaterThan(0);
      expect(paths.every((path) => path.getAttribute('d')?.startsWith('M '))).toBe(true);
    });
  },
};

export const ExpandedGroupsHideGroupConnectionAnchors: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await groupByProperty(canvas, 'Output', 'Species');

    const groupId = 'output:Species=Arabidopsis';
    const grouped = await waitFor(() => canvas.getByTestId(`provenance-group-Output-${groupId}`));
    expect(within(grouped).getByTestId('provenance-connection-handle-Output-GroupCard')).toBeInTheDocument();
    expect(connectionKeys(canvas.getAllByTestId('provenance-connection')).some((key) => key.includes(groupId))).toBe(true);

    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));

    await waitFor(() => {
      const expanded = canvas.getByTestId(`provenance-group-Output-${groupId}`);
      expect(within(expanded).queryByTestId('provenance-connection-handle-Output-GroupCard')).not.toBeInTheDocument();
      expect(within(expanded).getAllByTestId('provenance-connection-handle-Output-GroupMember').length).toBeGreaterThan(0);
      expect(connectionKeys(canvas.queryAllByTestId('provenance-connection')).some((key) => key.includes(groupId))).toBe(false);
      expect(connectionKeys(canvas.getAllByTestId('provenance-member-connection')).some((key) => key.includes(groupId))).toBe(true);
    });
  },
};

const connectionKeys = (paths: HTMLElement[]) =>
  paths.map((path) => path.getAttribute('data-provenance-connection-key') ?? '');

export const ExpandedPropertyValuesConnectValueChipsToMatchingGroups: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(() => {
      const headerKeys = connectionKeys(canvas.getAllByTestId('provenance-property-connection'));
      expect(headerKeys.some((key) => key.includes('Species'))).toBe(true);
    });

    const panel = await expandProperty(canvas, 'Output', 'Species');

    await waitFor(() => {
      const headerKeys = connectionKeys(canvas.queryAllByTestId('provenance-property-connection'));
      expect(headerKeys.some((key) => key.includes('Species'))).toBe(false);

      const valueKeys = connectionKeys(canvas.getAllByTestId('provenance-value-connection'));
      expect(valueKeys.some((key) => key.includes('Species') && key.includes('Arabidopsis'))).toBe(true);
      expect(valueKeys.some((key) => key.includes('Species') && key.includes('Chlamydomonas'))).toBe(true);
      expect(canvas.getAllByTestId('provenance-value-connection').every((path) => path.getAttribute('d')?.startsWith('M '))).toBe(true);
    });

    expect(panel.queryByTestId('provenance-connection-handle-Output-PropertyValue')).not.toBeInTheDocument();

    // Collapsing again must restore header connectors and drop value connectors,
    // so the expanded-header filter cannot become a one-way switch.
    await userEvent.hover(canvas.getByTestId('provenance-property-Output-Species'));
    await userEvent.click(canvas.getByTestId('provenance-property-expand-Output-Species'));

    await waitFor(() => {
      expect(canvas.queryByTestId('provenance-property-values-Output-Species')).not.toBeInTheDocument();
      const headerKeys = connectionKeys(canvas.getAllByTestId('provenance-property-connection'));
      expect(headerKeys.some((key) => key.includes('Species'))).toBe(true);
      expect(canvas.queryByTestId('provenance-value-connection')).not.toBeInTheDocument();
    });
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

export const RailValuesAssignByDragWithoutConnectionHandles: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await railValue(canvas, 'Output', 'Analysis', 'Mass Spectrometry');

    expect(within(source as HTMLElement).queryByTestId('provenance-connection-handle-Output-PropertyValue')).not.toBeInTheDocument();

    const target = canvas.getByText('Output D').closest('article')!;
    await dragByPointer(source, target);

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedPropertyValue');
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
    expect(within(details).queryByRole('button', { name: /remove connection/i })).not.toBeInTheDocument();
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

export const RemovesConnectionFromContextMenu: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const initialCount = (await waitFor(() => {
      const connectors = canvas.getAllByTestId('provenance-connection');
      expect(connectors.length).toBeGreaterThan(0);
      return connectors;
    })).length;

    const connector = canvas.getAllByTestId('provenance-connection')[0];
    fireEvent.contextMenu(connector, { clientX: 320, clientY: 240, bubbles: true });
    const menu = await screen.findByTestId('context_menu');
    await userEvent.click(within(menu).getByRole('button', { name: /delete/i }));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('RemoveLoadedConnection');
      expect(canvas.queryAllByTestId('provenance-connection').length).toBeLessThan(initialCount);
    });
    expect(canvas.queryByTestId('provenance-connection-details')).not.toBeInTheDocument();
  },
};

export const RemovesExpandedMemberConnectionFromContextMenu: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await groupByProperty(canvas, 'Output', 'Species');

    const grouped = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));
    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));

    const connector = await waitFor(() => {
      const memberConnector = canvas
        .getAllByTestId('provenance-member-connection')
        .find((path) => path.getAttribute('data-provenance-connection-key')?.includes('output:Species=Arabidopsis'));
      expect(memberConnector).toBeTruthy();
      expect(memberConnector).toHaveAttribute('role', 'button');
      return memberConnector!;
    });

    const removedKey = connector.getAttribute('data-provenance-connection-key');
    await userEvent.click(connector);

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-connection-details')).toBeInTheDocument();
      expect(within(grouped).getAllByTestId('provenance-connection-handle-Output-GroupMember').length).toBeGreaterThan(0);
    });

    const selectedConnector = canvas
      .getAllByTestId('provenance-member-connection')
      .find((path) => path.getAttribute('data-provenance-connection-key') === removedKey);
    expect(selectedConnector).toBeTruthy();
    fireEvent.contextMenu(selectedConnector!, { clientX: 360, clientY: 280, bubbles: true });
    const menu = await screen.findByTestId('context_menu');
    await userEvent.click(within(menu).getByRole('button', { name: /delete/i }));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('RemoveLoadedConnection');
      expect(connectionKeys(canvas.queryAllByTestId('provenance-member-connection'))).not.toContain(removedKey);
      expect(within(grouped).getAllByTestId('provenance-connection-handle-Output-GroupMember').length).toBeGreaterThan(0);
    });
  },
};

export const RemovesConnectionWithDeleteKey: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const connector = await waitFor(() => canvas.getAllByTestId('provenance-connection')[0]);
    const initialCount = canvas.getAllByTestId('provenance-connection').length;

    connector.focus();
    await userEvent.keyboard('{Delete}');

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('RemoveLoadedConnection');
      expect(canvas.queryAllByTestId('provenance-connection').length).toBeLessThan(initialCount);
    });
  },
};

export const WarnsBeforeOverwritingSingleValueFromRail: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = await railValue(canvas, 'Output', 'Species', 'Arabidopsis');
    await groupByProperty(canvas, 'Output', 'Species');
    const target = canvas.getByTestId('provenance-group-Output-output:Species=Chlamydomonas');

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
    const target = canvas.getByTestId('provenance-group-Output-output:Replicate=1 | 2');

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

export const CreatesDataEndpointFromAvailableKindList: Story = {
  render: () => <Harness fixture="dataOutputOnly" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-input'));
    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: /Endpoint kind/i }),
      'arc-isa:endpoint:data',
    );
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'New Input');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedSet:arc-isa:endpoint:data:Data'),
    );
  },
};

export const KeepsEndpointKindListIndependentOfSessionReplacement: Story = {
  render: () => <Harness fixture="dataOutputOnly" allowEndpointReplacement />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole('button', { name: /Replace endpoint context/i }));
    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-input'));

    expect(screen.getByRole('combobox', { name: /Endpoint kind/i })).toHaveValue('arc-isa:endpoint:source');
  },
};

export const CreatesEndpointFromSelectedAvailableKind: Story = {
  render: () => <Harness inputOnly />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-output'));
    await userEvent.selectOptions(
      screen.getByRole('combobox', { name: /Endpoint kind/i }),
      'arc-isa:endpoint:material',
    );
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'Custom Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    await waitFor(() =>
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent(
        'AddLoadedSet:arc-isa:endpoint:material:Material',
      ),
    );
  },
};

export const CreatesNextLayerAndKeepsBoundaryEditsSynchronized: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const outputA = canvas.getByText('Output A').closest('article')!;
    await selectGroup(outputA);
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-layer-layer-2')).toHaveClass('swt:btn-primary');
      expect(canvasElement).toHaveTextContent('Output A');
    });

    const source = await addRailValue(canvas, 'Input', 'Analysis', 'Imaging');
    await groupByProperty(canvas, 'Input', 'Analysis');
    const carried = canvas.getByTestId('provenance-group-Input-input:Analysis=Mass Spectrometry');
    await dragByPointer(source, carried);
    await userEvent.click(canvas.getByTestId('provenance-confirm-overwrite'));

    await userEvent.click(canvas.getByTestId('provenance-layer-layer-1'));
    await waitFor(() => expect(canvasElement).toHaveTextContent('Imaging'));
    expect(canvas.getByTestId('provenance-patch-preview')).not.toHaveTextContent('No patches emitted.');
  },
};

export const CompletesAnInputOnlyLayer: Story = {
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
  // The row controls only enter the layout while the row is hovered or focused.
  await userEvent.hover(canvas.getByTestId(`provenance-property-${side}-${propertyName}`));
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
  const groupedPattern = new RegExp(`^provenance-group-${side}-${side.toLowerCase()}:.*${propertyName}=`);

  for (let attempt = 0; attempt < 3 && canvas.queryAllByTestId(groupedPattern).length === 0; attempt += 1) {
    fireEvent.click(canvas.getByTestId(`provenance-property-${side}-${propertyName}`));
    await waitFor(() => expect(canvas.queryAllByTestId(groupedPattern).length).toBeGreaterThan(0), {
      timeout: 1000,
    }).catch(() => undefined);
  }

  await waitFor(
    () => {
      // Grouped cards carry their grouping in the card test id, e.g.
      // provenance-group-Input-input:Species=Arabidopsis.
      const grouped = canvas.getAllByTestId(groupedPattern);
      expect(grouped.length).toBeGreaterThan(0);
    },
    { timeout: 3000 },
  );
}

async function selectGroup(groupCard: HTMLElement) {
  for (let attempt = 0; attempt < 3 && !groupCard.classList.contains('swt:border-primary'); attempt += 1) {
    const selectionSurface = groupCard.querySelector<HTMLElement>('[data-testid^="provenance-group-select-surface-"]');
    await userEvent.click(selectionSurface ?? groupCard);
    await waitFor(() => expect(groupCard).toHaveClass('swt:border-primary'), { timeout: 1000 }).catch(() => undefined);
  }

  await waitFor(() => expect(groupCard).toHaveClass('swt:border-primary'), { timeout: 3000 });
}

function propertyOrder(canvas: ReturnType<typeof within>, side: 'Input' | 'Output') {
  const prefix = `provenance-property-${side}-`;
  const rail = canvas.getByTestId(`provenance-property-rail-${side}`);
  return Array.from(rail.querySelectorAll<HTMLElement>(`[data-testid^="${prefix}"]`))
    .map((element) => element.getAttribute('data-testid')!.slice(prefix.length));
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
      within(output).getByTestId('provenance-connection-handle-Output-GroupCard'),
    );

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('AddLoadedConnection');
    });
    expect(canvas.queryByTestId('provenance-live-connection')).not.toBeInTheDocument();
  },
};

export const IgnoresConnectionHandleDroppedOnCardBody: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByText('Input C').closest('article')!;
    const output = canvas.getByText('Output E').closest('article')!;
    const initialLineCount = await waitFor(() => {
      const lines = canvas.queryAllByTestId('provenance-connection');
      expect(lines.length).toBeGreaterThan(0);
      return lines.length;
    });

    await dragByPointer(
      within(input).getByTestId('provenance-connection-handle-Input-GroupCard'),
      output,
    );

    await waitFor(() => expect(canvas.queryByTestId('provenance-live-connection')).not.toBeInTheDocument());
    expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('No patches emitted.');
    expect(canvas.queryAllByTestId('provenance-connection')).toHaveLength(initialLineCount);
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
    await groupByProperty(canvas, 'Output', 'Species');

    const inputGroup = canvas.getByText('Input D').closest('article')!;
    const outputGroup = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));
    const outputHandle = within(outputGroup).getByTestId('provenance-connection-handle-Output-GroupCard');

    await dragByPointer(
      within(inputGroup).getByTestId('provenance-connection-handle-Input-GroupCard'),
      outputHandle,
    );

    await waitFor(() => expect(canvas.getByTestId('provenance-member-resolution-prompt')).toBeInTheDocument());
    expect(canvas.getByTestId('provenance-member-resolution-prompt')).toHaveTextContent('1 input member');
    expect(canvas.getByTestId('provenance-member-resolution-prompt')).toHaveTextContent('3 output members');
  },
};

export const ManualMismatchResolutionExpandsMembersWithoutPatches: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await groupByProperty(canvas, 'Output', 'Species');

    const inputGroup = canvas.getByText('Input D').closest('article')!;
    const outputGroup = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));
    const outputHandle = within(outputGroup).getByTestId('provenance-connection-handle-Output-GroupCard');

    await dragByPointer(
      within(inputGroup).getByTestId('provenance-connection-handle-Input-GroupCard'),
      outputHandle,
    );
    await waitFor(() => expect(canvas.getByTestId('provenance-member-resolution-prompt')).toBeInTheDocument());
    await userEvent.click(canvas.getByTestId('provenance-member-resolution-manual'));

    await waitFor(() => {
      const currentInputGroup = canvas.getByTestId('provenance-group-Input-input:input-d');
      const currentOutputGroup = canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis');
      expect(within(currentInputGroup).queryByTestId('provenance-connection-handle-Input-GroupMember')).not.toBeInTheDocument();
      expect(within(currentOutputGroup).getAllByTestId('provenance-connection-handle-Output-GroupMember').length).toBeGreaterThan(0);
    });
    expect(canvas.queryByTestId('provenance-member-resolution-prompt')).not.toBeInTheDocument();
    expect(canvas.getByTestId('provenance-patch-preview')).toHaveTextContent('No patches emitted.');
  },
};

export const ExpandedGroupedCardsDoNotExpandConnectedSingleCards: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await groupByProperty(canvas, 'Output', 'Species');

    const inputA = canvas.getByText('Input A').closest('article')!;
    const outputGroup = await waitFor(() => canvas.getByTestId('provenance-group-Output-output:Species=Arabidopsis'));

    await userEvent.click(within(outputGroup).getByRole('button', { name: 'Show members' }));

    await waitFor(() => {
      expect(within(outputGroup).getAllByTestId('provenance-connection-handle-Output-GroupMember').length).toBeGreaterThan(0);
      expect(within(inputA).queryByTestId('provenance-group-member-Input-input-a')).not.toBeInTheDocument();
      expect(within(inputA).queryByTestId('provenance-connection-handle-Input-GroupMember')).not.toBeInTheDocument();
    });
  },
};

export const LayerTabsUseConceptualLayerColorsAndSideRails: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const layer1 = canvas.getByTestId('provenance-layer-layer-1');
    const layerColor = layer1.getAttribute('data-provenance-layer-color') ?? '';

    expect(layer1).toHaveClass('swt:btn-primary');
    expect(layerColor).toMatch(/^#/);
    expect(layerColor).not.toContain('|');

    expect(canvas.getByTestId('provenance-property-rail-Input')).toHaveAttribute(
      'data-provenance-side-id',
      'layer-1-input',
    );
    expect(canvas.getByTestId('provenance-property-rail-Output')).toHaveAttribute(
      'data-provenance-side-id',
      'layer-1-output',
    );
  },
};

export const AddsLayerFromMixedSelection: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const inputA = canvas.getByText('Input A').closest('article')!;
    const outputB = canvas.getByText('Output B').closest('article')!;

    await selectGroup(inputA);
    await selectGroup(outputB);
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));

    await waitFor(() => {
      expect(canvas.getByTestId('provenance-layer-layer-2')).toHaveClass('swt:btn-primary');
      expect(canvasElement).toHaveTextContent('Input A');
      expect(canvasElement).toHaveTextContent('Output B');
    });
  },
};

export const DoesNotReuseSelectionForEqualGroupIdsInDifferentLayers: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const outputA = canvas.getByText('Output A').closest('article')!;

    await selectGroup(outputA);
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-output'));
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'Layer 2 Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));
    const layer2Output = await waitFor(() => canvas.getByText('Layer 2 Output').closest('article')!);

    await selectGroup(layer2Output);
    await userEvent.click(canvas.getByTestId('provenance-add-layer'));

    await userEvent.click(canvas.getByTestId('popover_trigger_provenance-add-output'));
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'Layer 3 Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));
    const layer3Output = await waitFor(() => canvas.getByText('Layer 3 Output').closest('article')!);

    expect(layer3Output).not.toHaveClass('swt:border-primary');
  },
};
