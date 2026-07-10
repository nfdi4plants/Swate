import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, fireEvent, userEvent, waitFor, within } from 'storybook/test';
import { Workspace, WorkspaceProvider } from './Workspace.fs.js';
import { Tab } from './Types.fs.js';
import type { Tab as TabType } from './Types.fs.js';

const nextFrame = () => new Promise((resolve) => requestAnimationFrame(resolve));

function createTab(id: string, label: string, payload: string = '{}'): TabType<string> {
  return new Tab<string>(id, label, payload);
}

function defaultTabs() {
  return [createTab('tab-1', 'Main.tsx'), createTab('tab-2', 'utils.ts'), createTab('tab-3', 'styles.css')];
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
    clientX: fromX, clientY: fromY, button: 0, buttons: 1, isPrimary: true, pointerId: 1,
  });
  await nextFrame();
  fireEvent.pointerMove(target, {
    clientX: activationX, clientY: activationY, button: 0, buttons: 1, isPrimary: true, pointerId: 1,
  });
  await nextFrame();
  fireEvent.pointerMove(document, {
    clientX: toX, clientY: toY, button: 0, buttons: 1, isPrimary: true, pointerId: 1,
  });
  await nextFrame();
  fireEvent.pointerUp(target, {
    clientX: toX, clientY: toY, button: 0, buttons: 0, isPrimary: true, pointerId: 1,
  });
  await nextFrame();
}

function getPaneContentRect(canvas: ReturnType<typeof within>) {
  const pane = canvas.getByTestId(/^workspace-pane-/);
  const tabBar = canvas.getByTestId(/^workspace-tabbar-/);
  const paneRect = pane.getBoundingClientRect();
  const tabBarRect = tabBar.getBoundingClientRect();
  const contentLeft = paneRect.left;
  const contentTop = tabBarRect.bottom;
  const contentWidth = paneRect.width;
  const contentHeight = paneRect.bottom - tabBarRect.bottom;
  return { left: contentLeft, top: contentTop, width: contentWidth, height: contentHeight };
}

async function dragToPaneEdge(source: Element, canvas: ReturnType<typeof within>, edge: 'right' | 'bottom') {
  const from = source.getBoundingClientRect();
  const fromX = from.left + from.width / 2;
  const fromY = from.top + from.height / 2;
  const contentRect = getPaneContentRect(canvas);
  let toX: number, toY: number;
  if (edge === 'right') {
    toX = contentRect.left + contentRect.width * 0.875;
    toY = contentRect.top + contentRect.height / 2;
  } else {
    toX = contentRect.left + contentRect.width / 2;
    toY = contentRect.top + contentRect.height * 0.8;
  }
  const deltaX = toX - fromX;
  const deltaY = toY - fromY;
  const distance = Math.hypot(deltaX, deltaY) || 1;
  const activationX = fromX + (deltaX / distance) * 8;
  const activationY = fromY + (deltaY / distance) * 8;
  const paneEl = canvas.getByTestId(/^workspace-pane-/);
  fireEvent.pointerDown(source, {
    clientX: fromX, clientY: fromY, button: 0, buttons: 1, isPrimary: true, pointerId: 1,
  });
  await new Promise(resolve => setTimeout(resolve, 500));
  await nextFrame();
  fireEvent.pointerMove(paneEl, {
    clientX: activationX, clientY: activationY, button: 0, buttons: 1, isPrimary: true, pointerId: 1,
  });
  await new Promise(resolve => setTimeout(resolve, 500));
  await nextFrame();
  await nextFrame();
  fireEvent.pointerMove(document, {
    clientX: toX, clientY: toY, button: 0, buttons: 1, isPrimary: true, pointerId: 1,
  });
  await new Promise(resolve => setTimeout(resolve, 500));
  await nextFrame();
  await nextFrame();
  fireEvent.pointerUp(document, {
    clientX: toX, clientY: toY, button: 0, buttons: 0, isPrimary: true, pointerId: 1,
  });
  await new Promise(resolve => setTimeout(resolve, 500));
  await nextFrame();
}

// --- Render helpers ---

function renderTabContent(tab: TabType<string>): React.ReactElement {
  return (
    <div data-testid={`content-${tab.Id}`} style={{ padding: '16px', height: '100%', background: 'var(--color-base-200)' }}>
      {`${tab.Label} content`}
    </div>
  );
}

function renderTab(tab: TabType<string>): React.ReactElement {
  return <span>{tab.Label}</span>;
}

// --- Harnesses ---

function SimpleHarness() {
  const tabs = React.useMemo(() => defaultTabs(), []);
  return (
    <WorkspaceProvider<string> renderTabContent={renderTabContent} renderTab={renderTab} initialTabs={tabs} debug={true}>
      <Workspace className="swt:h-96" />
    </WorkspaceProvider>
  );
}

function SingleTabHarness() {
  const tabs = React.useMemo(() => [createTab('tab-1', 'Main.tsx')], []);
  return (
    <WorkspaceProvider<string> renderTabContent={renderTabContent} renderTab={renderTab} initialTabs={tabs} debug={true}>
      <Workspace className="swt:h-96" />
    </WorkspaceProvider>
  );
}

// --- Meta ---

const meta = {
  title: 'Composite Components/Workspace',
  component: Workspace,
  parameters: {
    layout: 'fullscreen',
    docs: {
      description: 'Drag interactions are tested via unit tests in WorkspaceModel.test.ts and manual QA',
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
      }, { timeout: 10_000 });
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
    }, { timeout: 10_000 });
  },
};

export const OverflowTabs: Story = {
  render: () => {
    const manyTabs = React.useMemo(() =>
      Array.from({ length: 20 }, (_, i) => createTab(`tab-${i}`, `File${i}.tsx`)),
    []);
    return (
      <WorkspaceProvider<string> renderTabContent={renderTabContent} renderTab={renderTab} initialTabs={manyTabs} debug={true}>
        <Workspace className="swt:h-96 swt:w-64" />
      </WorkspaceProvider>
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
    }, { timeout: 10_000 });

    await userEvent.click(body.getByText('Close'));
    await waitFor(() => {
      expect(canvas.queryByText('styles.css')).not.toBeInTheDocument();
    }, { timeout: 10_000 });
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
    }, { timeout: 10_000 });
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
    }, { timeout: 10_000 });
  },
};

export const DropTabOnOwnEdgeZoneSingleTabNoop: Story = {
  render: () => <SingleTabHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const tab = canvas.getByText('Main.tsx').closest('[data-workspace-tab-id]')!;
    await dragToPaneEdge(tab, canvas, 'right');
    await waitFor(() => {
      expect(canvas.getByText('Main.tsx')).toBeVisible();
      expect(canvas.queryAllByTestId(/^workspace-pane-/)).toHaveLength(1);
      expect(canvas.queryByTestId(/^workspace-split-/)).not.toBeInTheDocument();
    }, { timeout: 10_000 });
  },
};

export const SelfSplitRightWithMultipleTabs: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;

    await dragToPaneEdge(stylesTab, canvas, 'right');

    await waitFor(() => {
      expect(canvas.getByTestId(/^workspace-split-/)).toBeInTheDocument();
      const panes = canvas.getAllByTestId(/^workspace-pane-/);
      expect(panes).toHaveLength(2);
      expect(panes[0]).toHaveTextContent('Main.tsx');
      expect(panes[0]).toHaveTextContent('utils.ts');
      expect(panes[1]).toHaveTextContent('styles.css');
    }, { timeout: 10_000 });
  },
};

export const SelfSplitBottomWithMultipleTabs: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const utilsTab = canvas.getByText('utils.ts').closest('[data-workspace-tab-id]')!;

    await dragToPaneEdge(utilsTab, canvas, 'bottom');

    await waitFor(() => {
      expect(canvas.getByTestId(/^workspace-split-/)).toBeInTheDocument();
      const panes = canvas.getAllByTestId(/^workspace-pane-/);
      expect(panes).toHaveLength(2);
      expect(panes[0]).toHaveTextContent('Main.tsx');
      expect(panes[0]).toHaveTextContent('styles.css');
      expect(panes[1]).toHaveTextContent('utils.ts');
    }, { timeout: 10_000 });
  },
};

export const ClosePaneViaLastTab: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
    await dragToPaneEdge(stylesTab, canvas, 'right');

    await waitFor(() => {
      expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(2);
    }, { timeout: 10_000 });
    
    const newPane = canvas.getAllByTestId(/^workspace-pane-/)[1];
    expect(within(newPane).getByText('styles.css')).toBeVisible();
    expect(within(newPane).queryByText('Main.tsx')).not.toBeInTheDocument();
    expect(within(newPane).queryByText('utils.ts')).not.toBeInTheDocument();
  },
};

export const ActiveTabContentAfterSplit: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const stylesTab = canvas.getByText('styles.css').closest('[data-workspace-tab-id]')!;
    await dragToPaneEdge(stylesTab, canvas, 'right');

    await waitFor(() => {
      const panes = canvas.getAllByTestId(/^workspace-pane-/);
      expect(panes).toHaveLength(2);
      expect(panes[0]).toHaveTextContent('Main.tsx');
      expect(panes[0]).toHaveTextContent('utils.ts');
      expect(panes[1]).toHaveTextContent('styles.css');
    }, { timeout: 10_000 });

    expect(canvas.getByTestId('content-tab-1')).toBeVisible();
  },
};

export const ContextMenuCloseOthers: Story = {
  render: () => <SimpleHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    expect(canvas.getAllByTestId(/^workspace-pane-/)).toHaveLength(1);

    const tabButton = canvas.getByText('Main.tsx').closest('[data-workspace-tab-id]')!;
    fireEvent.contextMenu(tabButton);
    await nextFrame();

    const body = within(document.body);
    await waitFor(() => {
      expect(body.getByText('Close Others')).toBeVisible();
    }, { timeout: 10_000 });

    await userEvent.click(body.getByText('Close Others'));
    await waitFor(() => {
      expect(canvas.queryByText('utils.ts')).not.toBeInTheDocument();
      expect(canvas.queryByText('styles.css')).not.toBeInTheDocument();
      expect(canvas.getByText('Main.tsx')).toBeVisible();
    }, { timeout: 10_000 });
  },
};
