import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, screen, userEvent, waitFor, within } from 'storybook/test';
import { Main as ProvenanceGrouping } from './ProvenanceGrouping.fs.js';
import {
  Exports_createSampleSession as createSampleSession,
  Exports_createInputOnlySession as createInputOnlySession,
  Exports_createOutputOnlySession as createOutputOnlySession,
  Exports_patchLabels as patchLabels,
} from './Types.fs.js';

function Harness({ inputOnly = false, outputOnly = false }: { inputOnly?: boolean; outputOnly?: boolean }) {
  const [session, setSession] = React.useState(() =>
    inputOnly ? createInputOnlySession() : outputOnly ? createOutputOnlySession() : createSampleSession(),
  );
  const [patches, setPatches] = React.useState<string[]>([]);

  return (
    <div className="swt:flex swt:flex-col swt:gap-4 swt:min-h-screen swt:bg-base-200 swt:p-4">
      <ProvenanceGrouping
        session={session}
        height={680}
        debug={true}
        onChange={(change: any) => {
          setSession(change.Session);
          setPatches((current) => [
            ...current,
            ...Array.from(patchLabels(change.Patches)),
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

    const grouped = canvas.getByText('Species: Arabidopsis').closest('article')!;
    await userEvent.click(within(grouped).getByRole('button', { name: 'Show members' }));
    await waitFor(() => expect(grouped).toHaveTextContent('Input A'));
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

    await userEvent.click(canvas.getByTestId('provenance-edit-trigger-Analysis'));
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

    await userEvent.click(canvas.getByTestId('provenance-add-output-trigger'));
    await userEvent.type(screen.getByRole('textbox', { name: /Endpoint name/i }), 'New Output');
    await userEvent.click(screen.getByRole('button', { name: /Create endpoint/i }));

    await waitFor(() => expect(canvasElement).toHaveTextContent('New Output'));
    expect(canvas.getByTestId('provenance-patch-preview')).not.toHaveTextContent('No patches emitted.');
  },
};

async function dragByPointer(source: Element, target: Element) {
  const from = source.getBoundingClientRect();
  const to = target.getBoundingClientRect();
  fireEvent.pointerDown(source, { clientX: from.left + 4, clientY: from.top + 4, buttons: 1 });
  fireEvent.pointerMove(target, { clientX: to.left + 4, clientY: to.top + 4, buttons: 1 });
  fireEvent.pointerUp(target, { clientX: to.left + 4, clientY: to.top + 4 });
}

export const CopiesValueOntoAGroup: Story = {
  render: () => <Harness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const source = canvas.getByTestId('provenance-value-pv-output-a-analysis');
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
