import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, userEvent, waitFor, within } from 'storybook/test';
import { Workspace } from './Workspace.fs.js';
import type { WorkspaceTab, IWorkspaceHandle } from './Types.fs.js';
import { ofArray, type FSharpList } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/List.ts';
import { comparePrimitives } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/Util.ts';
import { ofArray as mapOfArray, type FSharpMap } from '../../fable_modules/fable-library-ts.5.0.0-alpha.21/Map.ts';

const nextFrame = () => new Promise((resolve) => requestAnimationFrame(resolve));

// --- Test data ---

function tab(id: string, label: string, icon?: string): WorkspaceTab {
  return { Id: id, Label: label, Icon: icon ?? null } as WorkspaceTab;
}

function fsList<T>(items: T[]): FSharpList<T> {
  return ofArray(items);
}

function contentFor(tabs: WorkspaceTab[]): FSharpMap<string, React.ReactElement> {
  const entries: [string, React.ReactElement][] = tabs.map((t) => [
    t.Id,
    <div
      data-testid={`content-${t.Id}`}
      style={{ padding: '16px', height: '100%', background: 'var(--color-base-200)' }}
    >
      {`${t.Label} content`}
    </div>,
  ]);
  return mapOfArray(entries, {
    Compare: (x: string, y: string) => comparePrimitives(x, y),
  });
}

function defaultTabs() {
  return [tab('tab-1', 'Main.tsx', 'swt:iconify swt:fluent--document-24-regular'),
          tab('tab-2', 'utils.ts', 'swt:iconify swt:fluent--document-24-regular'),
          tab('tab-3', 'styles.css', 'swt:iconify swt:fluent--document-css-24-regular')];
}

// --- Harness components ---

function SimpleHarness() {
  const [tabs, setTabs] = React.useState(() => defaultTabs());
  const [activeTabId, setActiveTabId] = React.useState<string | undefined>('tab-1');
  const contentMap = React.useMemo(() => contentFor(tabs), [tabs]);

  return (
    <Workspace
      tabs={fsList(tabs)}
      contentMap={contentMap}
      onTabsChange={(newTabs: FSharpList<WorkspaceTab>) => setTabs(Array.from(newTabs))}
      onActiveTabChange={(id: string | undefined) => setActiveTabId(id)}
      activeTabId={activeTabId}
      debug={true}
      className="swt:h-96"
    />
  );
}

function ImperativeHarness() {
  const handleRef = React.useRef<IWorkspaceHandle>(null);
  const [tabs, setTabs] = React.useState(() => defaultTabs());
  const [activeTabId, setActiveTabId] = React.useState<string | undefined>('tab-1');
  const contentMap = React.useMemo(() => contentFor(tabs), [tabs]);

  return (
    <div className="swt:flex swt:flex-col swt:gap-2">
      <div className="swt:flex swt:gap-2">
        <button
          data-testid="add-tab-btn"
          className="swt:btn swt:btn-xs swt:btn-outline"
          onClick={() => handleRef.current?.openTab(tab('tab-new', 'NewFile.tsx', 'swt:iconify swt:fluent--document-24-regular'))}
        >
          Add Tab
        </button>
        <button
          data-testid="close-active-btn"
          className="swt:btn swt:btn-xs swt:btn-outline"
          onClick={() => { if (activeTabId) handleRef.current?.closeTab(activeTabId); }}
        >
          Close Active
        </button>
      </div>
      <Workspace
        tabs={fsList(tabs)}
        contentMap={contentMap}
        onTabsChange={(newTabs: FSharpList<WorkspaceTab>) => setTabs(Array.from(newTabs))}
        onActiveTabChange={(id: string | undefined) => setActiveTabId(id)}
        activeTabId={activeTabId}
        debug={true}
        ref={handleRef}
        className="swt:h-96"
      />
    </div>
  );
}

// --- Meta ---

const meta = {
  title: 'Composite Components/Workspace',
  component: Workspace,
  parameters: {
    layout: 'fullscreen',
    docs: {
      description: 'Drag interactions (reorder, move, split creation) are tested via unit tests in PaneTree/PaneState and manual QA, as @dnd-kit pointer event simulation is unreliable in browser-based story testing.',
    },
  },
} satisfies Meta<typeof Workspace>;

export default meta;
type Story = StoryObj<typeof meta>;

// --- Stories ---

export const SinglePaneWithTabs: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement, step }) => {
    const canvas = within(canvasElement);
    await step('renders all tabs', () => {
      expect(canvas.getByText('Main.tsx')).toBeVisible();
      expect(canvas.getByText('utils.ts')).toBeVisible();
      expect(canvas.getByText('styles.css')).toBeVisible();
    });
    await step('shows active tab content', () => {
      expect(canvas.getByTestId('content-tab-1')).toBeVisible();
    });
    await step('clicking another tab activates it', async () => {
      await userEvent.click(canvas.getByText('utils.ts'));
      await waitFor(() => {
        expect(canvas.getByTestId('content-tab-2')).toBeVisible();
      });
    });
  },
};

export const CloseTab: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText('Main.tsx')).toBeVisible();
    expect(canvas.getByText('styles.css')).toBeVisible();

    const closeBtn = within(canvas.getByText('styles.css').closest('button')!).getByRole('button', { name: /Close/i });
    await userEvent.click(closeBtn);
    await waitFor(() => {
      expect(canvas.queryByText('styles.css')).not.toBeInTheDocument();
    });
  },
};

export const ImperativeHandle: Story = {
  render: () => <ImperativeHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText('Main.tsx')).toBeVisible();
    expect(canvas.queryByText('NewFile.tsx')).not.toBeInTheDocument();

    await userEvent.click(canvas.getByTestId('add-tab-btn'));
    await waitFor(() => {
      expect(canvas.getByText('NewFile.tsx')).toBeVisible();
    });

    await userEvent.click(canvas.getByTestId('close-active-btn'));
    await waitFor(() => {
      expect(canvas.queryByText('NewFile.tsx')).not.toBeInTheDocument();
    });
  },
};

export const OverflowTabs: Story = {
  render: () => {
    const manyTabs = React.useMemo(() =>
      Array.from({ length: 20 }, (_, i) => tab(`tab-${i}`, `File${i}.tsx`)),
    []);
    const contentMap = React.useMemo(() => contentFor(manyTabs), [manyTabs]);

    return (
      <Workspace
        tabs={fsList(manyTabs)}
        contentMap={contentMap}
        onTabsChange={() => {}}
        onActiveTabChange={() => {}}
        activeTabId="tab-0"
        debug={true}
        className="swt:h-96 swt:w-64"
      />
    );
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText('File0.tsx')).toBeVisible();
    expect(canvas.getByText('File19.tsx')).toBeVisible();
  },
};

export const ContextMenuClose: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText('styles.css')).toBeVisible();

    const tabButton = canvas.getByText('styles.css').closest('button')!;
    fireEvent.contextMenu(tabButton);
    await nextFrame();

    const body = within(document.body);
    await waitFor(() => {
      expect(body.getByText('Close')).toBeVisible();
    });

    await userEvent.click(body.getByText('Close'));
    await waitFor(() => {
      expect(canvas.queryByText('styles.css')).not.toBeInTheDocument();
    });
  },
};

export const ContextMenuCloseAll: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getByText('Main.tsx')).toBeVisible();

    const tabButton = canvas.getByText('Main.tsx').closest('button')!;
    fireEvent.contextMenu(tabButton);
    await nextFrame();

    const body = within(document.body);
    await waitFor(() => {
      expect(body.getByText('Close All')).toBeVisible();
    });

    await userEvent.click(body.getByText('Close All'));
    await waitFor(() => {
      expect(canvas.queryByText('Main.tsx')).not.toBeInTheDocument();
      expect(canvas.queryByText('utils.ts')).not.toBeInTheDocument();
      expect(canvas.queryByText('styles.css')).not.toBeInTheDocument();
    });
  },
};
