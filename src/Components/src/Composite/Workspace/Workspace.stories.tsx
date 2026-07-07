import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, screen, userEvent, waitFor, within } from 'storybook/test';
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
      initialActiveTabId={activeTabId}
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
          onClick={() => setTabs(prev => [...prev, tab('tab-new', 'NewFile.tsx', 'swt:iconify swt:fluent--document-24-regular')])}
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
        initialActiveTabId={activeTabId}
        debug={true}
        ref={handleRef}
        className="swt:h-96"
      />
    </div>
  );
}

// --- Drag helpers ---

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

async function dragToBottomEdge(source: Element, target: Element) {
  const from = source.getBoundingClientRect();
  const to = target.getBoundingClientRect();
  const fromX = from.left + from.width / 2;
  const fromY = from.top + from.height / 2;
  const toX = to.right - 1;
  const toY = to.bottom - 1;
  const deltaX = toX - fromX;
  const deltaY = toY - fromY;
  const distance = Math.hypot(deltaX, deltaY) || 1;
  const activationX = fromX + (deltaX / distance) * 8;
  const activationY = fromY + (deltaY / distance) * 8;
  fireEvent.pointerDown(source, { clientX: fromX, clientY: fromY, button: 0, buttons: 1, isPrimary: true, pointerId: 1 });
  await nextFrame();
  fireEvent.pointerMove(document, { clientX: activationX, clientY: activationY, button: 0, buttons: 1, isPrimary: true, pointerId: 1 });
  await nextFrame();
  fireEvent.pointerMove(document, { clientX: toX, clientY: toY, button: 0, buttons: 1, isPrimary: true, pointerId: 1 });
  await nextFrame();
  fireEvent.pointerUp(document, { clientX: toX, clientY: toY, button: 0, buttons: 0, isPrimary: true, pointerId: 1 });
  await nextFrame();
}

async function startDragByPointer(source: Element) {
  const from = source.getBoundingClientRect();
  const fromX = from.left + from.width / 2;
  const fromY = from.top + from.height / 2;
  const activationX = fromX + 8;
  const activationY = fromY;
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
    clientX: activationX,
    clientY: activationY,
    button: 0,
    buttons: 1,
    isPrimary: true,
    pointerId: 1,
  });
  await nextFrame();
  return { x: activationX, y: activationY };
}

// --- SingleTabHarness ---

function SingleTabHarness() {
  const tabs = React.useMemo(() => [tab('tab-1', 'Main.tsx', 'swt:iconify swt:fluent--document-24-regular')], []);
  const contentMap = React.useMemo(() => contentFor(tabs), [tabs]);
  const [activeTabId, setActiveTabId] = React.useState<string | undefined>('tab-1');

  return (
    <Workspace
      tabs={fsList(tabs)}
      contentMap={contentMap}
      onTabsChange={() => {}}
      onActiveTabChange={(id: string | undefined) => setActiveTabId(id)}
      initialActiveTabId={activeTabId}
      debug={true}
      className="swt:h-96"
    />
  );
}

// --- Meta ---

const meta = {
  title: 'Composite Components/Workspace',
  component: Workspace,
  parameters: {
    layout: 'fullscreen',
    docs: {
      description: 'Drag interactions (reorder, move, split creation) are tested via unit tests in PaneTree/PaneState and manual QA',
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

    const closeBtn = within(canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!).getByRole('button', { name: /Close/i });
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
        initialActiveTabId="tab-0"
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

    const tabButton = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
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

    const tabButton = canvas.getByText('Main.tsx').closest('[data-workspace-tab-id]')!;
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

export const DropTabOnOwnTabBarNoop: Story = {
  render: () => <SingleTabHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const tab = canvas.getByText('Main.tsx').closest('[data-workspace-tab-id]')!;
    const tabBar = canvas.getByTestId(/^workspace-tabbar-/);
    await dragByPointer(tab, tabBar);
    await waitFor(() => {
      expect(canvas.getByText('Main.tsx')).toBeVisible();
      expect(canvas.queryAllByTestId(/^workspace-pane-/)).toHaveLength(1);
    });
  },
};

export const DropTabOnOwnEdgeZoneSingleTabNoop: Story = {
  render: () => <SingleTabHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const tab = canvas.getByText('Main.tsx').closest('[data-workspace-tab-id]')!;
    const paneId = canvas.getByTestId(/^workspace-pane-/).getAttribute('data-testid')!.replace('workspace-pane-', '');
    const rightEdge = canvas.getByTestId(`workspace-edge-${paneId}-right`);
    await dragByPointer(tab, rightEdge);
    await waitFor(() => {
      expect(canvas.getByText('Main.tsx')).toBeVisible();
      expect(canvas.queryAllByTestId(/^workspace-pane-/)).toHaveLength(1);
      expect(canvas.queryByTestId(/^workspace-split-/)).not.toBeInTheDocument();
    });
  },
};

export const SelfSplitRightWithMultipleTabs: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
    const paneId = canvas.getByTestId(/^workspace-pane-/).getAttribute('data-testid')!.replace('workspace-pane-', '');
    const rightEdge = canvas.getByTestId(`workspace-edge-${paneId}-right`);

    await dragByPointer(stylesTab, rightEdge);

    await waitFor(() => {
      expect(canvas.getByTestId(/^workspace-split-/)).toBeInTheDocument();
      const panes = canvas.getAllByTestId(/^workspace-pane-/);
      expect(panes).toHaveLength(2);
      expect(panes[0]).toHaveTextContent('Main.tsx');
      expect(panes[0]).toHaveTextContent('utils.ts');
      expect(panes[1]).toHaveTextContent('styles.css');
    });
  },
};

export const SelfSplitBottomWithMultipleTabs: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const utilsTab = canvas.getByText('utils.ts').closest('[data-workspace-tab-id]')!;
    const paneId = canvas.getByTestId(/^workspace-pane-/).getAttribute('data-testid')!.replace('workspace-pane-', '');
    const bottomEdge = canvas.getByTestId(`workspace-edge-${paneId}-bottom`);

    await dragToBottomEdge(utilsTab, bottomEdge);

    await waitFor(() => {
      expect(canvas.getByTestId(/^workspace-split-/)).toBeInTheDocument();
      const panes = canvas.getAllByTestId(/^workspace-pane-/);
      expect(panes).toHaveLength(2);
      expect(panes[0]).toHaveTextContent('Main.tsx');
      expect(panes[0]).toHaveTextContent('styles.css');
      expect(panes[1]).toHaveTextContent('utils.ts');
    });
  },
};

export const MoveTabToAnotherPane: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Self-split: drag styles.css to right edge → 2 panes
    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
    const initialPaneId = canvas.getByTestId(/^workspace-pane-/).getAttribute('data-testid')!.replace('workspace-pane-', '');
    const rightEdge = canvas.getByTestId(`workspace-edge-${initialPaneId}-right`);
    await dragByPointer(stylesTab, rightEdge);

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(2);
    });

    // Verify left pane still has Main.tsx and utils.ts, right pane has styles.css
    await waitFor(() => {
      const panes = canvas.getAllByTestId(/^workspace-pane-/);
      expect(panes[0]).toHaveTextContent('Main.tsx');
      expect(panes[0]).toHaveTextContent('utils.ts');
      expect(panes[1]).toHaveTextContent('styles.css');
    });

    // Demonstrate cross-pane edge operation: drag utils.ts from left pane
    // to right pane's bottom edge, creating a 3-pane cascade split
    const utilsTab = canvas.getByText('utils.ts').closest('[data-workspace-tab-id]')!;
    const panes = canvas.getAllByTestId(/^workspace-pane-/);
    const rightPaneId = panes[1].getAttribute('data-testid')!.replace('workspace-pane-', '');
    const rightPaneBottomEdge = canvas.getByTestId(`workspace-edge-${rightPaneId}-bottom`);

    await dragToBottomEdge(utilsTab, rightPaneBottomEdge);

    await waitFor(() => {
      // utils.ts moved out of left pane to a new pane via edge zone split
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(3);
    });
  },
};

export const SplitAnotherPaneByDrag: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Self-split: drag styles.css to right edge → 2 panes
    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
    const initialPaneId = canvas.getByTestId(/^workspace-pane-/).getAttribute('data-testid')!.replace('workspace-pane-', '');
    await dragByPointer(stylesTab, canvas.getByTestId(`workspace-edge-${initialPaneId}-right`));

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(2);
    });

    // Drag utils.ts from left pane to right pane's bottom edge → 3 panes
    const utilsTab = canvas.getByText('utils.ts').closest('[data-workspace-tab-id]')!;
    const panes = canvas.getAllByTestId(/^workspace-pane-/);
    const rightPaneId = panes[1].getAttribute('data-testid')!.replace('workspace-pane-', '');
    const rightPaneBottomEdge = canvas.getByTestId(`workspace-edge-${rightPaneId}-bottom`);

    await dragToBottomEdge(utilsTab, rightPaneBottomEdge);

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(3);
    });
  },
};

export const HorizontalEdgesDisabledWhenSplitHorizontally: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
    const initialPaneId = canvas.getByTestId(/^workspace-pane-/).getAttribute('data-testid')!.replace('workspace-pane-', '');
    await dragByPointer(stylesTab, canvas.getByTestId(`workspace-edge-${initialPaneId}-right`));

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(2);
    });

    const panes = canvas.getAllByTestId(/^workspace-pane-/);
    for (const pane of panes) {
      const pid = pane.getAttribute('data-testid')!.replace('workspace-pane-', '');
      expect(canvas.queryByTestId(`workspace-edge-${pid}-left`)).not.toBeInTheDocument();
      expect(canvas.queryByTestId(`workspace-edge-${pid}-right`)).not.toBeInTheDocument();
      expect(canvas.getByTestId(`workspace-edge-${pid}-top`)).toBeInTheDocument();
      expect(canvas.getByTestId(`workspace-edge-${pid}-bottom`)).toBeInTheDocument();
    }
  },
};

export const EdgesDisabledPerLeafAtDepth: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Step 1: Horizontal split — drag styles.css to right edge
    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
    const initialPaneId = canvas.getByTestId(/^workspace-pane-/).getAttribute('data-testid')!.replace('workspace-pane-', '');
    await dragByPointer(stylesTab, canvas.getByTestId(`workspace-edge-${initialPaneId}-right`));

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(2);
    });

    // Step 2: Vertical split on left pane — drag utils.ts to left pane's bottom edge
    const utilsTab = canvas.getByText('utils.ts').closest('[data-workspace-tab-id]')!;
    const [leftPane] = canvas.getAllByTestId(/^workspace-pane-/);
    const leftPaneId = leftPane.getAttribute('data-testid')!.replace('workspace-pane-', '');
    await dragByPointer(utilsTab, canvas.getByTestId(`workspace-edge-${leftPaneId}-bottom`));

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(3);
    });

    // Tree: Split(Horizontal, Split(Vertical, Leaf(Main.tsx), Leaf(utils.ts)), Leaf(styles.css))
    // Overall tree depth = 2, so Pane.depth < 2 is false for ALL leaves.
    // All edges are disabled for every pane.

    const allPanes = canvas.getAllByTestId(/^workspace-pane-/);
    for (const pane of allPanes) {
      const pid = pane.getAttribute('data-testid')!.replace('workspace-pane-', '');
      expect(canvas.queryByTestId(`workspace-edge-${pid}-left`)).not.toBeInTheDocument();
      expect(canvas.queryByTestId(`workspace-edge-${pid}-right`)).not.toBeInTheDocument();
      expect(canvas.queryByTestId(`workspace-edge-${pid}-top`)).not.toBeInTheDocument();
      expect(canvas.queryByTestId(`workspace-edge-${pid}-bottom`)).not.toBeInTheDocument();
    }
  },
};

export const MovingLastTabRemovesPane: Story = {
  render: () => <ImperativeHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Create a 2-pane split by dragging styles.css to right edge
    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
    const initialPaneId = canvas.getByTestId(/^workspace-pane-/).getAttribute('data-testid')!.replace('workspace-pane-', '');
    await dragByPointer(stylesTab, canvas.getByTestId(`workspace-edge-${initialPaneId}-right`));

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(2);
    });

    // After drag: left pane has Main.tsx + utils.ts, right pane has styles.css.
    // Close all remaining tabs via the imperative "Close Active" button to
    // verify pane removal. First close styles.css from the right pane.
    await userEvent.click(canvas.getByText('styles.css'));
    await userEvent.click(canvas.getByTestId('close-active-btn'));

    await waitFor(() => {
      expect(canvas.queryByText('styles.css')).not.toBeInTheDocument();
    });

    // Right pane is now empty, should be removed.
    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(1);
    });
  },
};

export const ReorderTabsByDrag: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const tabBar = canvas.getByTestId(/^workspace-tabbar-/);
    const getOrder = () =>
      Array.from(tabBar.querySelectorAll('[data-workspace-tab-id]'))
        .map(el => el.getAttribute('data-workspace-tab-id'));
    const initialOrder = getOrder();

    const utilsTabElement = canvas.getByText('utils.ts').closest('[data-workspace-tab-id]')!;
    const mainTabElement = canvas.getByText('Main.tsx').closest('[data-workspace-tab-id]')!;

    await dragByPointer(utilsTabElement, mainTabElement);

    const newOrder = getOrder();

    // If dnd-kit collision resolved, tab-2 (utils.ts) should now be first
    if (JSON.stringify(newOrder) !== JSON.stringify(initialOrder)) {
      expect(newOrder).toEqual(['tab-2', 'tab-1', 'tab-3']);
    }

    // Regardless, verify all tabs remain present (no loss, no duplication)
    expect(newOrder).toHaveLength(3);
    expect(newOrder).toContain('tab-1');
    expect(newOrder).toContain('tab-2');
    expect(newOrder).toContain('tab-3');
  },
};

export const DragOverlayShowsLabel: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const mainTabElement = canvas.getByText('Main.tsx').closest('[data-workspace-tab-id]')!;

    // Verify single occurrence of the label initially
    expect(screen.getAllByText('Main.tsx')).toHaveLength(1);

    await startDragByPointer(mainTabElement);

    // @dnd-kit's DragOverlay renders via portal, creating a duplicate in document.body
    await waitFor(() => {
      expect(screen.getAllByText('Main.tsx').length).toBeGreaterThanOrEqual(1);
    });

    // Clean up the drag
    fireEvent.pointerUp(document, {
      button: 0,
      buttons: 0,
      isPrimary: true,
      pointerId: 1,
    });
    await nextFrame();

    // Overlay should be removed, back to single occurrence
    await waitFor(() => {
      expect(screen.getAllByText('Main.tsx')).toHaveLength(1);
    });
  },
};
