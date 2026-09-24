import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import {
  expect,
  fireEvent,
  screen,
  userEvent,
  waitFor,
  within,
} from "storybook/test";
import { Tree } from "./Tree.fs.js";
import type { TreeApi, TreeItem$1 as TreeItem } from "./Types.fs.js";

type DemoPayload = {
  badge?: string;
};

type DemoNode = TreeItem<DemoPayload>;
type DemoTreeProps = Parameters<typeof Tree<DemoPayload>>[0];
type DemoDataSource = NonNullable<DemoTreeProps["dataSource"]>;
type DemoRenderProps = Parameters<NonNullable<DemoTreeProps["renderNode"]>>[0];
type DemoSelectionMode = NonNullable<DemoTreeProps["selectionMode"]>;

const branch = (
  id: string,
  label: string,
  children?: DemoNode[],
  payload?: DemoPayload,
): DemoNode =>
  ({
    type: "branch",
    props: { id, label, data: payload },
    ...(children !== undefined ? { children } : {}),
  }) as DemoNode;

const leaf = (id: string, label: string, payload?: DemoPayload): DemoNode =>
  ({
    type: "leaf",
    props: { id, label, data: payload },
  }) as DemoNode;

type Deferred<T> = {
  promise: Promise<T>;
  resolve: (value: T) => void;
  reject: (reason: Error) => void;
};

const createDeferred = <T,>(): Deferred<T> => {
  let resolve!: (value: T) => void;
  let reject!: (reason: Error) => void;
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise;
    reject = rejectPromise;
  });
  return { promise, resolve, reject };
};

const unwrapGeneratedOption = <T,>(
  value: T | { value: T } | undefined,
): T | undefined => {
  if (value === undefined) return undefined;
  if (typeof value === "object" && value !== null && "value" in value) {
    return (value as { value: T }).value;
  }
  return value as T;
};

const expectLoadingIndicator = async (canvasElement: HTMLElement) => {
  await waitFor(() =>
    expect(canvasElement.querySelector(".swt\\:loading")).toBeTruthy(),
  );
};

const baseItems = [
  branch("arc", "Swate Demo ARC", [
    branch("arc/studies", "studies", [
      branch("arc/studies/study_01", "Study 01", [
        leaf("arc/studies/study_01/isa.study.xlsx", "isa.study.xlsx"),
        leaf("arc/studies/study_01/datamap.tsv", "datamap.tsv"),
      ]),
    ]),
    branch("arc/assays", "assays", [
      branch("arc/assays/assay_01", "Assay 01", [
        leaf("arc/assays/assay_01/isa.assay.xlsx", "isa.assay.xlsx"),
        leaf("arc/assays/assay_01/raw-data.tsv", "raw-data.tsv"),
      ]),
    ]),
    leaf("arc/isa.investigation.xlsx", "isa.investigation.xlsx"),
  ]),
] as DemoNode[];

const meta = {
  title: "Composite Components/Tree",
  tags: ["autodocs"],
  component: Tree,
  args: {
    items: [],
  },
  parameters: {
    layout: "centered",
  },
} satisfies Meta<typeof Tree>;

export default meta;

type Story = StoryObj<typeof meta>;

const BasicTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["arc", "arc/studies", "arc/studies/study_01"]}
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        debug
      />
      <div data-testid="selected-node">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const AriaSiblingMetadata: Story = {
  render: () => <BasicTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByRole("tree")).toBeVisible();
    await expect(canvas.getByTestId("tree-node-arc")).toHaveAttribute(
      "aria-posinset",
      "1",
    );
    await expect(canvas.getByTestId("tree-node-arc")).toHaveAttribute(
      "aria-setsize",
      "1",
    );
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveAttribute(
      "aria-posinset",
      "1",
    );
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveAttribute(
      "aria-setsize",
      "3",
    );
    await expect(canvas.getByTestId("tree-node-arc/assays")).toHaveAttribute(
      "aria-posinset",
      "2",
    );
    await expect(
      canvas.getByTestId("tree-node-arc/isa.investigation.xlsx"),
    ).toHaveAttribute("aria-posinset", "3");
  },
};

export const SingleSelection: Story = {
  render: () => <BasicTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("isa.study.xlsx"));

    await expect(
      canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx"),
    ).toHaveAttribute("aria-selected", "true");
    expect(canvas.getByTestId("selected-node").textContent).toBe(
      JSON.stringify(["arc/studies/study_01/isa.study.xlsx"]),
    );
  },
};

export const ChevronControlsExpansion: Story = {
  render: () => <BasicTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByText("isa.study.xlsx")).toBeVisible();

    await userEvent.click(
      canvas.getByRole("button", { name: "Collapse studies" }),
    );
    await waitFor(() =>
      expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument(),
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand studies" }),
    );
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();
  },
};

const FolderSelectionTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={baseItems}
        defaultExpandedIds={["arc"]}
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        debug
      />
      <div data-testid="folder-selection">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const SelectingAFolderDoesNotToggleExpansion: Story = {
  render: () => <FolderSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const studiesNode = canvas.getByTestId("tree-node-arc/studies");

    await expect(studiesNode).toHaveAttribute("aria-selected", "false");
    await expect(studiesNode).toHaveAttribute("aria-expanded", "false");

    await userEvent.click(studiesNode);
    await expect(studiesNode).toHaveAttribute("aria-expanded", "false");
    await expect(studiesNode).toHaveAttribute("aria-selected", "false");

    await userEvent.click(canvas.getByText("studies"));
    await expect(studiesNode).toHaveAttribute("aria-selected", "true");
    await expect(studiesNode).toHaveAttribute("aria-expanded", "false");
    expect(canvas.getByTestId("folder-selection").textContent).toBe(
      JSON.stringify(["arc/studies"]),
    );
    await expect(canvas.queryByText("Study 01")).not.toBeInTheDocument();

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand studies" }),
    );
    await expect(studiesNode).toHaveAttribute("aria-selected", "true");
    await expect(studiesNode).toHaveAttribute("aria-expanded", "true");
    await expect(canvas.getByText("Study 01")).toBeVisible();

    await userEvent.click(studiesNode);
    await expect(studiesNode).toHaveAttribute("aria-expanded", "true");
    await expect(canvas.getByText("Study 01")).toBeVisible();

    await userEvent.click(canvas.getByText("studies"));
    await expect(studiesNode).toHaveAttribute("aria-expanded", "true");
  },
};

export const EnterOpensAFolder: Story = {
  render: () => (
    <div className="swt:space-y-2">
      <FolderSelectionTree />
      <p className="swt:text-sm">
        The studies folder remains focused after the check. Press Enter to open
        it.
      </p>
    </div>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const studiesNode = canvas.getByTestId("tree-node-arc/studies");

    await userEvent.click(canvas.getByText("studies"));
    await expect(studiesNode).toHaveFocus();
    await expect(studiesNode).toHaveAttribute("aria-selected", "true");
    await expect(studiesNode).toHaveAttribute("aria-expanded", "false");

    await userEvent.keyboard("{Enter}");
    await waitFor(() =>
      expect(studiesNode).toHaveAttribute("aria-expanded", "true"),
    );
    await expect(canvas.getByText("Study 01")).toBeVisible();
    await expect(studiesNode).toHaveAttribute("aria-selected", "true");
    await expect(studiesNode).toHaveFocus();
    expect(canvas.getByTestId("folder-selection").textContent).toBe(
      JSON.stringify(["arc/studies"]),
    );

    await userEvent.keyboard("{Enter}");
    await waitFor(() =>
      expect(studiesNode).toHaveAttribute("aria-expanded", "false"),
    );
    await expect(canvas.queryByText("Study 01")).not.toBeInTheDocument();
    await expect(studiesNode).toHaveFocus();
  },
};

const SelectUntilTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);
  const items = React.useMemo(
    () => [
      leaf("alpha.txt", "alpha.txt"),
      branch("beta", "beta", []),
      leaf("gamma.txt", "gamma.txt"),
      branch("delta", "delta", []),
      leaf("epsilon.txt", "epsilon.txt"),
    ],
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        selectionMode="multiple"
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        debug
      />
      <div data-testid="select-until-selection">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const ShiftSelectsUntilClickedNode: Story = {
  render: () => <SelectUntilTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("beta"));
    fireEvent.click(canvas.getByText("delta"), { shiftKey: true });

    await expect(canvas.getByTestId("tree-node-alpha.txt")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    await expect(canvas.getByTestId("tree-node-beta")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(canvas.getByTestId("tree-node-gamma.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(canvas.getByTestId("tree-node-delta")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(canvas.getByTestId("tree-node-epsilon.txt")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    expect(canvas.getByTestId("select-until-selection").textContent).toBe(
      JSON.stringify(["beta", "delta", "gamma.txt"]),
    );
  },
};

type MultiSelectionTreeProps = {
  initialSelectedIds?: string[];
};

const MultiSelectionTree = ({
  initialSelectedIds = [],
}: MultiSelectionTreeProps) => {
  const [selected, setSelected] = React.useState<string[]>(initialSelectedIds);

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={[
          "arc",
          "arc/studies",
          "arc/studies/study_01",
          "arc/assays",
          "arc/assays/assay_01",
        ]}
        selectionMode="multiple"
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        debug
      />
      <div data-testid="multi-selected">{JSON.stringify(selected)}</div>
      <button type="button">Outside tree</button>
    </div>
  );
};

export const SelectableRowsExposePointerStyling: Story = {
  render: () => <MultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    expect(canvas.getByTestId("tree-node-arc/studies").className).toContain(
      "swt:cursor-pointer",
    );
    expect(canvas.getByTestId("tree-node-arc/studies").className).toContain(
      "swt:hover:bg-base-200",
    );
  },
};

export const MultipleSelectionAllowsBranchSelection: Story = {
  render: () => <MultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("studies"));
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(canvas.getByTestId("multi-selected").textContent).toBe(
      JSON.stringify(["arc/studies"]),
    );
  },
};

export const ControlClickAddsToMultipleSelection: Story = {
  render: () => <MultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("isa.study.xlsx"));
    fireEvent.click(canvas.getByText("isa.assay.xlsx"), { ctrlKey: true });

    expect(canvas.getByTestId("multi-selected").textContent).toBe(
      JSON.stringify([
        "arc/assays/assay_01/isa.assay.xlsx",
        "arc/studies/study_01/isa.study.xlsx",
      ]),
    );
  },
};

export const ControlClickRemovesFromMultipleSelection: Story = {
  render: () => (
    <MultiSelectionTree
      initialSelectedIds={[
        "arc/assays/assay_01/isa.assay.xlsx",
        "arc/studies/study_01/isa.study.xlsx",
      ]}
    />
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    fireEvent.click(canvas.getByText("isa.assay.xlsx"), { ctrlKey: true });

    await expect(
      canvas.getByTestId("tree-node-arc/assays/assay_01/isa.assay.xlsx"),
    ).toHaveAttribute("aria-selected", "false");
    expect(canvas.getByTestId("multi-selected").textContent).toBe(
      JSON.stringify(["arc/studies/study_01/isa.study.xlsx"]),
    );
  },
};

export const ActiveStatePersistsAfterFocusLeavesTree: Story = {
  render: () => <MultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("isa.study.xlsx"));
    const activeNode = canvas.getByTestId(
      "tree-node-arc/studies/study_01/isa.study.xlsx",
    );
    await expect(activeNode).toHaveAttribute("data-tree-active", "true");
    await expect(activeNode).toHaveFocus();
    await expect(activeNode).toHaveAttribute("data-tree-focused", "true");

    await userEvent.click(canvas.getByRole("button", { name: "Outside tree" }));
    await expect(activeNode).toHaveAttribute("data-tree-active", "true");
    await waitFor(() =>
      expect(activeNode).toHaveAttribute("data-tree-focused", "false"),
    );
    await expect(activeNode).toHaveAttribute("tabindex", "0");
  },
};

const NodeSelectabilityTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);
  const items = React.useMemo(
    () => [branch("folder", "folder", [leaf("folder/file.txt", "file.txt")])],
    [],
  );

  return (
    <div className="swt:w-96">
      <Tree
        items={items}
        defaultExpandedIds={["folder"]}
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        isNodeSelectable={(node) => node.type === "leaf"}
        debug
      />
      <div data-testid="node-selectability-selection">
        Selected: {selected.join("|") || "none"}
      </div>
    </div>
  );
};

export const IsNodeSelectableKeepsLeafSelectable: Story = {
  render: () => <NodeSelectabilityTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const branchNode = canvas.getByTestId("tree-node-folder");
    const leafNode = canvas.getByTestId("tree-node-folder/file.txt");

    await expect(branchNode).not.toHaveAttribute("aria-selected");
    await expect(leafNode).toHaveAttribute("aria-selected", "false");

    await userEvent.click(canvas.getByText("folder"));
    await expect(branchNode).not.toHaveAttribute("aria-selected");
    await expect(
      canvas.getByTestId("node-selectability-selection"),
    ).toHaveTextContent("Selected: none");

    await userEvent.click(canvas.getByText("file.txt"));
    await expect(leafNode).toHaveAttribute("aria-selected", "true");
    await expect(
      canvas.getByTestId("node-selectability-selection"),
    ).toHaveTextContent("Selected: folder/file.txt");
  },
};

const SelectionModeNormalizationTree = () => {
  const items = React.useMemo(
    () => [leaf("alpha.txt", "alpha.txt"), leaf("beta.txt", "beta.txt")],
    [],
  );
  const [selectionMode, setSelectionMode] =
    React.useState<DemoSelectionMode>("multiple");
  const [selected, setSelected] = React.useState(["alpha.txt", "beta.txt"]);
  const [selectionChangeCount, setSelectionChangeCount] = React.useState(0);

  const onSelectionChange = React.useCallback((nextSelected: string[]) => {
    setSelectionChangeCount((count) => count + 1);
    setSelected(nextSelected);
  }, []);

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        selectionMode={selectionMode}
        selectedIds={selected}
        onSelectionChange={onSelectionChange}
        debug
      />
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={() => setSelectionMode("single")}
      >
        Use single selection
      </button>
      <div data-testid="parent-selected-ids">{JSON.stringify(selected)}</div>
      <div data-testid="selection-change-count">{selectionChangeCount}</div>
    </div>
  );
};

export const SingleSelectionNormalizesControlledIds: Story = {
  render: () => <SelectionModeNormalizationTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByTestId("tree-node-alpha.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(canvas.getByTestId("tree-node-beta.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Use single selection" }),
    );
    await expect(canvas.getByTestId("tree-node-alpha.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(canvas.getByTestId("tree-node-beta.txt")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    expect(canvas.getByTestId("tree-selected-ids").textContent).toBe(
      "alpha.txt",
    );
    expect(canvas.getByTestId("parent-selected-ids").textContent).toBe(
      JSON.stringify(["alpha.txt", "beta.txt"]),
    );
    expect(canvas.getByTestId("selection-change-count").textContent).toBe("0");
  },
};

const UncontrolledMultiSelectionTree = () => {
  const items = React.useMemo(
    () => [leaf("alpha.txt", "alpha.txt"), leaf("beta.txt", "beta.txt")],
    [],
  );

  return (
    <div className="swt:w-96">
      <Tree items={items} selectionMode="multiple" debug />
    </div>
  );
};

export const UncontrolledMultiSelectionUsesLatestState: Story = {
  render: () => <UncontrolledMultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("alpha.txt"));
    fireEvent.click(canvas.getByText("beta.txt"), { ctrlKey: true });

    await expect(canvas.getByTestId("tree-node-alpha.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(canvas.getByTestId("tree-node-beta.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(canvas.getByTestId("tree-selected-ids")).toHaveTextContent(
      "alpha.txt",
    );
    await expect(canvas.getByTestId("tree-selected-ids")).toHaveTextContent(
      "beta.txt",
    );
  },
};

const DisabledSelectionTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);
  const items = React.useMemo(
    () => [branch("folder", "folder", [leaf("folder/file.txt", "file.txt")])],
    [],
  );

  return (
    <div className="swt:w-96">
      <Tree
        items={items}
        isSelectionDisabled
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        debug
      />
      <div data-testid="disabled-selected">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const DisabledSelection: Story = {
  render: () => <DisabledSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const branchNode = canvas.getByTestId("tree-node-folder");

    await expect(branchNode).not.toHaveAttribute("aria-selected");
    await expect(branchNode).toHaveAttribute("aria-expanded", "false");

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand folder" }),
    );
    await expect(branchNode).toHaveAttribute("aria-expanded", "true");
    await expect(canvas.getByText("file.txt")).toBeVisible();

    const leafNode = canvas.getByTestId("tree-node-folder/file.txt");
    await expect(leafNode).not.toHaveAttribute("aria-selected");
    await expect(leafNode).toHaveAttribute("aria-disabled", "true");
    await userEvent.click(canvas.getByText("file.txt"));
    expect(canvas.getByTestId("disabled-selected").textContent).toBe(
      JSON.stringify([]),
    );
  },
};

const LazyCacheTree = () => {
  const [loadCount, setLoadCount] = React.useState(0);
  const requestCountRef = React.useRef(0);
  const apiRef = React.useRef<TreeApi | undefined>(undefined);

  const items = React.useMemo(
    () => [branch("arc/lazy-studies", "studies", undefined)],
    [],
  );

  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        if (item?.props.id !== "arc/lazy-studies") return [];
        requestCountRef.current += 1;
        const requestNumber = requestCountRef.current;
        setLoadCount(requestNumber);
        return [
          leaf(
            `arc/lazy-studies/load-${requestNumber}.txt`,
            `Study load ${requestNumber}`,
          ),
        ];
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource} apiRef={apiRef} debug />
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={() => apiRef.current?.invalidateNode("arc/lazy-studies")}
      >
        Invalidate studies cache
      </button>
      <div data-testid="load-count">Loads: {loadCount}</div>
    </div>
  );
};

export const LazyLoadingCachesChildren: Story = {
  render: () => <LazyCacheTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand studies" }),
    );
    await expect(await canvas.findByText("Study load 1")).toBeVisible();
    expect(canvas.getByTestId("load-count").textContent).toBe("Loads: 1");

    await userEvent.click(
      canvas.getByRole("button", { name: "Collapse studies" }),
    );
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand studies" }),
    );
    await expect(canvas.getByText("Study load 1")).toBeVisible();
    expect(canvas.getByTestId("load-count").textContent).toBe("Loads: 1");
  },
};

export const InvalidateNodeClearsLoadedCache: Story = {
  render: () => <LazyCacheTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand studies" }),
    );
    await expect(await canvas.findByText("Study load 1")).toBeVisible();
    await userEvent.click(
      canvas.getByRole("button", { name: "Invalidate studies cache" }),
    );
    await expect(
      canvas.getByRole("button", { name: "Expand studies" }),
    ).toBeVisible();
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand studies" }),
    );
    await expect(await canvas.findByText("Study load 2")).toBeVisible();
    await expect(canvas.queryByText("Study load 1")).not.toBeInTheDocument();
    expect(canvas.getByTestId("load-count").textContent).toBe("Loads: 2");
  },
};

const PendingRequestInvalidationTree = () => {
  const apiRef = React.useRef<TreeApi | undefined>(undefined);
  const requestsRef = React.useRef<Deferred<DemoNode[]>[]>([]);
  const [requestCount, setRequestCount] = React.useState(0);
  const items = React.useMemo(
    () => [branch("arc/pending", "pending", undefined)],
    [],
  );

  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        if (item?.props.id !== "arc/pending") return [];
        const request = createDeferred<DemoNode[]>();
        requestsRef.current.push(request);
        setRequestCount(requestsRef.current.length);
        return request.promise;
      },
    }),
    [],
  );

  const resolveLatestRequest = React.useCallback(() => {
    const requestNumber = requestsRef.current.length;
    requestsRef.current
      .at(-1)
      ?.resolve([
        leaf(
          `arc/pending/result-${requestNumber}.txt`,
          `Result ${requestNumber}`,
        ),
      ]);
  }, []);

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource} apiRef={apiRef} debug />
      <button
        type="button"
        onClick={() => apiRef.current?.invalidateNode("arc/pending")}
      >
        Invalidate pending load
      </button>
      <button type="button" onClick={resolveLatestRequest}>
        Resolve latest load
      </button>
      <div data-testid="pending-request-count">Requests: {requestCount}</div>
    </div>
  );
};

export const InvalidateNodeSupersedesPendingRequest: Story = {
  render: () => <PendingRequestInvalidationTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand pending" }),
    );
    await expectLoadingIndicator(canvasElement);
    expect(canvas.getByTestId("pending-request-count").textContent).toBe(
      "Requests: 1",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Invalidate pending load" }),
    );
    await expect(
      canvas.getByRole("button", { name: "Expand pending" }),
    ).toBeVisible();
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand pending" }),
    );
    await expectLoadingIndicator(canvasElement);
    expect(canvas.getByTestId("pending-request-count").textContent).toBe(
      "Requests: 2",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Resolve latest load" }),
    );
    await expect(await canvas.findByText("Result 2")).toBeVisible();
  },
};

const StaleFailureTree = () => {
  const apiRef = React.useRef<TreeApi | undefined>(undefined);
  const requestsRef = React.useRef<Deferred<DemoNode[]>[]>([]);
  const [requestCount, setRequestCount] = React.useState(0);
  const [requestSettlements, setRequestSettlements] = React.useState<string[]>(
    [],
  );
  const [errorCount, setErrorCount] = React.useState(0);
  const items = React.useMemo(
    () => [branch("arc/concurrent", "concurrent", undefined)],
    [],
  );

  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        if (item?.props.id !== "arc/concurrent") return [];
        const request = createDeferred<DemoNode[]>();
        requestsRef.current.push(request);
        const requestNumber = requestsRef.current.length;
        setRequestCount(requestNumber);

        try {
          const children = await request.promise;
          setRequestSettlements((current) => [
            ...current,
            `request-${requestNumber}:resolved`,
          ]);
          return children;
        } catch (error) {
          setRequestSettlements((current) => [
            ...current,
            `request-${requestNumber}:rejected`,
          ]);
          throw error;
        }
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        dataSource={dataSource}
        apiRef={apiRef}
        onError={() => setErrorCount((count) => count + 1)}
        debug
      />
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={() => apiRef.current?.invalidateNode("arc/concurrent")}
      >
        Invalidate pending request
      </button>
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={() =>
          requestsRef.current[1]?.resolve([
            leaf("arc/concurrent/fresh.txt", "fresh.txt"),
          ])
        }
      >
        Resolve second request
      </button>
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={() =>
          requestsRef.current[0]?.reject(new Error("stale failure"))
        }
      >
        Reject first request
      </button>
      <div data-testid="stale-request-count">Requests: {requestCount}</div>
      <div data-testid="stale-request-settlements">
        Settled: {requestSettlements.join("|") || "none"}
      </div>
      <div data-testid="stale-error-count">Errors: {errorCount}</div>
    </div>
  );
};

export const StaleLazyFailureDoesNotCollapseNewerResult: Story = {
  render: () => <StaleFailureTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand concurrent" }),
    );
    await expectLoadingIndicator(canvasElement);
    await expect(canvas.getByTestId("stale-request-count")).toHaveTextContent(
      "Requests: 1",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Invalidate pending request" }),
    );
    await expect(
      canvas.getByRole("button", { name: "Expand concurrent" }),
    ).toBeVisible();
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand concurrent" }),
    );
    await expectLoadingIndicator(canvasElement);
    await expect(canvas.getByTestId("stale-request-count")).toHaveTextContent(
      "Requests: 2",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Resolve second request" }),
    );
    await expect(await canvas.findByText("fresh.txt")).toBeVisible();
    await expect(
      canvas.getByTestId("stale-request-settlements"),
    ).toHaveTextContent("request-2:resolved");
    await expect(
      canvas.getByRole("button", { name: "Collapse concurrent" }),
    ).toBeVisible();

    await userEvent.click(
      canvas.getByRole("button", { name: "Reject first request" }),
    );
    await waitFor(() =>
      expect(canvas.getByTestId("stale-request-settlements")).toHaveTextContent(
        "request-1:rejected",
      ),
    );
    await waitFor(() => {
      expect(canvas.getByText("fresh.txt")).toBeVisible();
      expect(
        canvas.getByRole("button", { name: "Collapse concurrent" }),
      ).toBeVisible();
      expect(canvas.getByTestId("stale-error-count")).toHaveTextContent(
        "Errors: 0",
      );
      expect(canvas.queryByText("Error")).not.toBeInTheDocument();
    });
  },
};

const ParentAwareDataSourceTree = () => {
  const [loadLog, setLoadLog] = React.useState<string[]>([]);
  const items = React.useMemo(
    () => [branch("remote/arc", "Remote Swate ARC", undefined)],
    [],
  );

  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        const parentId = item?.props.id ?? "root";
        setLoadLog((current) => [...current, parentId]);

        switch (parentId) {
          case "remote/arc":
            return [
              branch("remote/arc/studies", "studies", undefined),
              branch("remote/arc/runs", "runs", undefined),
              branch("remote/arc/empty-folder", "empty folder", []),
              leaf(
                "remote/arc/isa.investigation.xlsx",
                "isa.investigation.xlsx",
              ),
            ];
          case "remote/arc/studies":
            return [
              branch("remote/arc/studies/study_03", "Study 03", [
                leaf(
                  "remote/arc/studies/study_03/isa.study.xlsx",
                  "isa.study.xlsx",
                ),
              ]),
            ];
          case "remote/arc/runs":
            return [
              branch("remote/arc/runs/run_01", "Run 01", [
                leaf("remote/arc/runs/run_01/isa.run.xlsx", "isa.run.xlsx"),
              ]),
            ];
          default:
            return [];
        }
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource} debug />
      <div data-testid="datasource-load-log">{JSON.stringify(loadLog)}</div>
    </div>
  );
};

export const DataSourceLoadsChildrenForExpandedBranch: Story = {
  render: () => <ParentAwareDataSourceTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand Remote Swate ARC" }),
    );
    await expect(
      await canvas.findByText("isa.investigation.xlsx"),
    ).toBeVisible();
    await expect(canvas.getByText("studies")).toBeVisible();
    await expect(canvas.getByText("runs")).toBeVisible();
    await expect(
      canvas.getByRole("button", { name: "Expand runs" }),
    ).toBeVisible();
    await expect(canvas.getByText("empty folder")).toBeVisible();
    await expect(
      canvas.getByRole("button", { name: "Expand empty folder" }),
    ).toBeVisible();
    expect(canvas.getByTestId("datasource-load-log").textContent).toBe(
      JSON.stringify(["remote/arc"]),
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand studies" }),
    );
    await expect(await canvas.findByText("Study 03")).toBeVisible();
    expect(canvas.getByTestId("datasource-load-log").textContent).toBe(
      JSON.stringify(["remote/arc", "remote/arc/studies"]),
    );

    await userEvent.click(canvas.getByRole("button", { name: "Expand runs" }));
    await expect(await canvas.findByText("Run 01")).toBeVisible();
    expect(canvas.getByTestId("datasource-load-log").textContent).toBe(
      JSON.stringify(["remote/arc", "remote/arc/studies", "remote/arc/runs"]),
    );
  },
};

const EmptyFolderTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);
  const items = React.useMemo(
    () => [branch("empty-folder", "empty folder", [])],
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        debug
      />
      <div data-testid="empty-folder-selection">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const EmptyFolderRemainsSelectableAndExpandable: Story = {
  render: () => <EmptyFolderTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const emptyFolder = canvas.getByTestId("tree-node-empty-folder");

    await userEvent.click(canvas.getByText("empty folder"));
    await expect(emptyFolder).toHaveAttribute("aria-selected", "true");
    await expect(emptyFolder).toHaveAttribute("aria-expanded", "false");
    expect(canvas.getByTestId("empty-folder-selection").textContent).toBe(
      JSON.stringify(["empty-folder"]),
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand empty folder" }),
    );
    await expect(emptyFolder).toHaveAttribute("aria-expanded", "true");
    await expect(
      canvas.getByRole("button", { name: "Collapse empty folder" }),
    ).toBeVisible();
  },
};

const DataSourceInvalidateAllTree = () => {
  const [loadCount, setLoadCount] = React.useState(0);
  const loadCountRef = React.useRef(0);
  const apiRef = React.useRef<TreeApi | undefined>(undefined);
  const items = React.useMemo(
    () => [branch("arc/workflows", "workflows", undefined)],
    [],
  );

  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        if (item?.props.id !== "arc/workflows") return [];
        loadCountRef.current += 1;
        const version = loadCountRef.current;
        setLoadCount(version);
        return [
          branch(`arc/workflows/workflow_${version}`, `Workflow ${version}`, [
            leaf(
              `arc/workflows/workflow_${version}/workflow.xlsx`,
              "workflow.xlsx",
            ),
          ]),
        ];
      },
    }),
    [],
  );

  const invalidateAll = React.useCallback(() => {
    apiRef.current?.invalidateAll();
  }, []);

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource} apiRef={apiRef} debug />
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={invalidateAll}
      >
        Invalidate all datasource cache
      </button>
      <div data-testid="datasource-invalidate-loads">Loads: {loadCount}</div>
    </div>
  );
};

export const DataSourceInvalidateAllCache: Story = {
  render: () => <DataSourceInvalidateAllTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand workflows" }),
    );
    await expect(await canvas.findByText("Workflow 1")).toBeVisible();
    expect(canvas.getByTestId("datasource-invalidate-loads").textContent).toBe(
      "Loads: 1",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Collapse workflows" }),
    );
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand workflows" }),
    );
    await expect(canvas.getByText("Workflow 1")).toBeVisible();
    expect(canvas.getByTestId("datasource-invalidate-loads").textContent).toBe(
      "Loads: 1",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Invalidate all datasource cache" }),
    );
    await expect(
      canvas.getByRole("button", { name: "Expand workflows" }),
    ).toBeVisible();
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand workflows" }),
    );
    await expect(await canvas.findByText("Workflow 2")).toBeVisible();
    await expect(canvas.queryByText("Workflow 1")).not.toBeInTheDocument();
    await expect(
      canvas.getByTestId("datasource-invalidate-loads"),
    ).toHaveTextContent("Loads: 2");
  },
};

const LazyErrorTree = () => {
  const [errorMessage, setErrorMessage] = React.useState("none");
  const [errorCount, setErrorCount] = React.useState(0);
  const items = React.useMemo(
    () => [branch("arc/runs", "runs", undefined)],
    [],
  );

  const onError = React.useCallback((error: unknown) => {
    setErrorMessage(error instanceof Error ? error.message : String(error));
    setErrorCount((count) => count + 1);
  }, []);

  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        if (item?.props.id !== "arc/runs") return [];
        throw new Error("Run metadata could not be loaded");
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource} onError={onError} debug />
      <div data-testid="lazy-error-message">Error: {errorMessage}</div>
      <div data-testid="lazy-error-count">Errors: {errorCount}</div>
    </div>
  );
};

export const LazyLoadingErrorState: Story = {
  render: () => <LazyErrorTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Expand runs" }));
    await expect(await canvas.findByText("Error")).toBeVisible();
    await expect(
      canvas.getByRole("button", { name: "Expand runs" }),
    ).toBeVisible();
    await expect(
      canvas.queryByRole("button", { name: "Collapse runs" }),
    ).not.toBeInTheDocument();
    await expect(canvas.getByTestId("lazy-error-message")).toHaveTextContent(
      "Run metadata could not be loaded",
    );
    await expect(canvas.getByTestId("lazy-error-count")).toHaveTextContent(
      "Errors: 1",
    );
  },
};

const VirtualizedTree = () => {
  const numberedDirectories = React.useCallback(
    (
      parentId: string,
      namePrefix: string,
      labelPrefix: string,
      count: number,
    ) =>
      Array.from({ length: count }, (_, index) => {
        const number = (index + 1).toString().padStart(2, "0");
        return branch(
          `${parentId}/${namePrefix}_${number}`,
          `${labelPrefix} ${number}`,
          [
            leaf(
              `${parentId}/${namePrefix}_${number}/metadata.xlsx`,
              "metadata.xlsx",
            ),
          ],
        );
      }),
    [],
  );

  const items = React.useMemo(
    () => [
      branch("arc", "Swate Demo ARC", [
        branch(
          "arc/studies",
          "studies",
          numberedDirectories("arc/studies", "study", "Study", 24),
        ),
        branch(
          "arc/assays",
          "assays",
          numberedDirectories("arc/assays", "assay", "Assay", 24),
        ),
        branch(
          "arc/runs",
          "runs",
          numberedDirectories("arc/runs", "run", "Run", 16),
        ),
        branch(
          "arc/workflows",
          "workflows",
          numberedDirectories("arc/workflows", "workflow", "Workflow", 16),
        ),
        branch("arc/docs", "docs", [
          leaf("arc/docs/README.md", "README.md"),
          leaf("arc/docs/changelog.md", "changelog.md"),
        ]),
      ]),
    ],
    [numberedDirectories],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <button type="button" className="swt:btn swt:btn-sm">
        Before tree
      </button>
      <Tree
        items={items}
        defaultExpandedIds={[
          "arc",
          "arc/studies",
          "arc/assays",
          "arc/runs",
          "arc/workflows",
          "arc/docs",
        ]}
        enableVirtualization
        estimateNodeHeight={34}
        debug
      />
    </div>
  );
};

const getVirtualizedViewport = (canvasElement: HTMLElement) => {
  const viewport = canvasElement.querySelector<HTMLElement>(
    "[data-tree-virtualized='true']",
  );
  expect(viewport).not.toBeNull();
  return viewport!;
};

const scrollVirtualizedTreeToBottom = async (canvasElement: HTMLElement) => {
  const viewport = getVirtualizedViewport(canvasElement);
  viewport.scrollTop = viewport.scrollHeight;
  fireEvent.scroll(viewport);
  await waitFor(() =>
    expect(within(canvasElement).getByText("Workflow 16")).toBeVisible(),
  );
  return viewport;
};

export const VirtualizationUnmountsOffscreenRows: Story = {
  render: () => <VirtualizedTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByRole("tree")).toHaveAttribute(
      "data-tree-root",
      "true",
    );
    await expect(canvas.getByText("Swate Demo ARC")).toBeVisible();
    await expect(canvas.getByText("Study 01")).toBeVisible();

    await scrollVirtualizedTreeToBottom(canvasElement);

    await expect(canvas.queryByText("Study 01")).not.toBeInTheDocument();
    await expect(canvas.queryByTestId("tree-node-arc")).not.toBeInTheDocument();
  },
};

export const VirtualizationKeepsAMountedTabStop: Story = {
  render: () => <VirtualizedTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const virtualizedViewport =
      await scrollVirtualizedTreeToBottom(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Before tree" }));
    await userEvent.tab();
    const mountedTabStop = virtualizedViewport.querySelector(
      "[role='treeitem'][tabindex='0']",
    );
    await expect(mountedTabStop).not.toBeNull();
    await expect(mountedTabStop).toHaveFocus();
  },
};

export const VirtualizedHomeAndEndNavigation: Story = {
  render: () => <VirtualizedTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Before tree" }));
    await userEvent.tab();
    await expect(canvas.getByTestId("tree-node-arc")).toHaveFocus();

    await userEvent.keyboard("{End}");
    await waitFor(() =>
      expect(
        canvas.getByTestId("tree-node-arc/docs/changelog.md"),
      ).toHaveFocus(),
    );

    await userEvent.keyboard("{Home}");
    await waitFor(() =>
      expect(canvas.getByTestId("tree-node-arc")).toHaveFocus(),
    );
  },
};

const ContextMenuTree = () => {
  const [lastAction, setLastAction] = React.useState("none");

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["arc", "arc/studies", "arc/studies/study_01"]}
        onContextMenu={(_event, nodeOption) => {
          const node = unwrapGeneratedOption(nodeOption);
          return [
            {
              text: <span>Inspect {node?.props.label ?? "tree root"}</span>,
              onClick: () => setLastAction(node?.props.id ?? "root"),
            },
          ];
        }}
        debug
      />
      <div data-testid="last-action">Last action: {lastAction}</div>
    </div>
  );
};

export const NodeAndRootContextMenu: Story = {
  render: () => <ContextMenuTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    fireEvent.contextMenu(
      canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx"),
      {
        clientX: 20,
        clientY: 20,
        bubbles: true,
      },
    );
    await userEvent.click(await screen.findByText("Inspect isa.study.xlsx"));
    await expect(canvas.getByTestId("last-action")).toHaveTextContent(
      "arc/studies/study_01/isa.study.xlsx",
    );

    fireEvent.contextMenu(canvas.getByRole("tree"), {
      clientX: 20,
      clientY: 20,
      bubbles: true,
    });
    await userEvent.click(await screen.findByText("Inspect tree root"));
    await expect(canvas.getByTestId("last-action")).toHaveTextContent("root");
  },
};

const AppearanceTree = () => {
  const items = React.useMemo(
    () => [
      {
        type: "leaf",
        props: {
          id: "arc/featured.xlsx",
          label: "featured.xlsx",
          icon: (
            <i
              data-testid="custom-tree-icon"
              className="swt:iconify swt:fluent--document-star-24-filled swt:size-4"
            />
          ),
          tooltip: "Featured ARC spreadsheet",
        },
      } as DemoNode,
    ],
    [],
  );

  return (
    <div className="swt:w-96">
      <Tree items={items} debug />
    </div>
  );
};

export const CustomIconAndTooltip: Story = {
  render: () => <AppearanceTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByTestId("custom-tree-icon")).toBeVisible();
    await expect(
      canvas.getByTestId("tree-node-arc/featured.xlsx"),
    ).toHaveAttribute("title", "Featured ARC spreadsheet");
  },
};

const customItems = [
  branch("arc/studies/study_04", "Study 04", [
    leaf("arc/studies/study_04/isa.study.xlsx", "isa.study.xlsx", {
      badge: "ISA",
    }),
  ]),
];

const customExpandedIds = ["arc/studies/study_04"];

export const CustomLeadingRenderer: Story = {
  render: () => (
    <Tree
      items={customItems}
      defaultExpandedIds={customExpandedIds}
      leading={(props: DemoRenderProps) => (
        <span data-testid={`custom-leading-${props.node.props.id}`}>
          {props.node.type === "branch"
            ? "folder-leading"
            : `depth-${props.depth}`}
        </span>
      )}
    />
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(
      canvas.getByTestId("custom-leading-arc/studies/study_04"),
    ).toHaveTextContent("folder-leading");
    await expect(
      canvas.getByTestId("custom-leading-arc/studies/study_04/isa.study.xlsx"),
    ).toHaveTextContent("depth-1");
  },
};

export const CustomTrailingRenderer: Story = {
  render: () => (
    <Tree
      items={customItems}
      defaultExpandedIds={customExpandedIds}
      trailing={(props: DemoRenderProps) => {
        const payload = unwrapGeneratedOption(props.node.props.data);
        return payload?.badge ? (
          <span data-testid="custom-trailing">{payload.badge}</span>
        ) : null;
      }}
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(
      within(canvasElement).getByTestId("custom-trailing"),
    ).toHaveTextContent("ISA");
  },
};

export const CustomNodeRenderer: Story = {
  render: () => (
    <Tree
      items={customItems}
      defaultExpandedIds={customExpandedIds}
      renderNode={(props: DemoRenderProps) => (
        <strong data-testid={`custom-node-${props.node.props.id}`}>
          Rendered {props.node.props.label}
        </strong>
      )}
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(
      within(canvasElement).getByTestId(
        "custom-node-arc/studies/study_04/isa.study.xlsx",
      ),
    ).toHaveTextContent("Rendered isa.study.xlsx");
  },
};

export const CustomRootStyling: Story = {
  render: () => (
    <Tree
      items={customItems}
      styleFn={(node, classes) =>
        !node ? [...classes, "swt:border", "swt:border-info"] : classes
      }
      debug
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(within(canvasElement).getByTestId("generic-tree")).toHaveClass(
      "swt:border-info",
    );
  },
};

export const CustomBranchStyling: Story = {
  render: () => (
    <Tree
      items={customItems}
      styleFn={(nodeOption, classes) => {
        const node = unwrapGeneratedOption(nodeOption);
        return node?.type === "branch"
          ? [...classes, "swt:text-primary"]
          : classes;
      }}
      debug
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(
      within(canvasElement).getByTestId("tree-node-arc/studies/study_04"),
    ).toHaveClass("swt:text-primary");
  },
};

export const CustomLeafStyling: Story = {
  render: () => (
    <Tree
      items={customItems}
      defaultExpandedIds={customExpandedIds}
      styleFn={(nodeOption, classes) => {
        const node = unwrapGeneratedOption(nodeOption);
        return node?.type === "leaf"
          ? [...classes, "swt:text-accent"]
          : classes;
      }}
      debug
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(
      within(canvasElement).getByTestId(
        "tree-node-arc/studies/study_04/isa.study.xlsx",
      ),
    ).toHaveClass("swt:text-accent");
  },
};

const CustomSelectTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);

  return (
    <div>
      <Tree
        items={customItems}
        defaultExpandedIds={customExpandedIds}
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        renderNode={(props: DemoRenderProps) =>
          props.node.type === "leaf" ? (
            <button
              type="button"
              onClick={(event) => props.select(event.nativeEvent)}
            >
              Select rendered leaf
            </button>
          ) : (
            <span>{props.node.props.label}</span>
          )
        }
      />
      <div data-testid="custom-select-value">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const CustomSelectCallbackUsesNativeMouseEvent: Story = {
  render: () => <CustomSelectTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(
      canvas.getByRole("button", { name: "Select rendered leaf" }),
    );
    expect(canvas.getByTestId("custom-select-value").textContent).toBe(
      JSON.stringify(["arc/studies/study_04/isa.study.xlsx"]),
    );
  },
};

export const CustomRendererReceivesFocusState: Story = {
  render: () => (
    <Tree
      items={customItems}
      defaultExpandedIds={customExpandedIds}
      renderNode={(props: DemoRenderProps) => (
        <span>
          {props.isFocused
            ? `${props.node.props.label} focused`
            : props.node.props.label}
        </span>
      )}
      debug
    />
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByText("isa.study.xlsx"));
    await expect(canvas.getByText("isa.study.xlsx focused")).toBeVisible();
  },
};

export const CustomToggleCallback: Story = {
  render: () => (
    <Tree
      items={customItems}
      defaultExpandedIds={customExpandedIds}
      renderNode={(props: DemoRenderProps) => (
        <span>
          {props.node.props.label}
          {props.node.type === "branch" ? (
            <button
              type="button"
              onClick={(event) => {
                event.preventDefault();
                event.stopPropagation();
                props.toggle();
              }}
            >
              Toggle rendered branch
            </button>
          ) : null}
        </span>
      )}
    />
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByText("isa.study.xlsx")).toBeVisible();
    await userEvent.click(
      canvas.getByRole("button", { name: "Toggle rendered branch" }),
    );
    await expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument();
  },
};

const RenameTree = () => {
  const [draftLabel, setDraftLabel] = React.useState("datamap-updated.tsv");
  const [selected, setSelected] = React.useState<string[]>([]);
  const [items, setItems] = React.useState<DemoNode[]>(() => [
    branch("arc/assays/assay_05", "Assay 05", [
      leaf("arc/assays/assay_05/isa.assay.xlsx", "isa.assay.xlsx"),
      leaf("arc/assays/assay_05/datamap.tsv", "datamap.tsv"),
      leaf("arc/assays/assay_05/raw-data.tsv", "raw-data.tsv"),
    ]),
  ]);

  const renameDatamap = React.useCallback(() => {
    setItems((current) =>
      current.map((node) =>
        node.type === "branch" && node.props.id === "arc/assays/assay_05"
          ? ({
              ...node,
              children: Array.from(node.children ?? []).map((child) =>
                child.props.id === "arc/assays/assay_05/datamap.tsv"
                  ? ({
                      ...child,
                      props: { ...child.props, label: draftLabel },
                    } as DemoNode)
                  : child,
              ),
            } as DemoNode)
          : node,
      ),
    );
  }, [draftLabel]);

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        defaultExpandedIds={["arc/assays/assay_05"]}
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        debug
      />
      <input
        aria-label="Datamap file name"
        className="swt:input swt:input-sm swt:input-bordered"
        value={draftLabel}
        onChange={(event) => setDraftLabel(event.currentTarget.value)}
      />
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={renameDatamap}
      >
        Apply datamap rename
      </button>
      <div data-testid="rename-selected">{selected.join(",") || "none"}</div>
    </div>
  );
};

export const RenameUpdatesVisibleNodeLabel: Story = {
  render: () => <RenameTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByText("datamap.tsv")).toBeVisible();
    await userEvent.clear(
      canvas.getByRole("textbox", { name: "Datamap file name" }),
    );
    await userEvent.type(
      canvas.getByRole("textbox", { name: "Datamap file name" }),
      "datamap-updated.tsv",
    );
    await userEvent.click(
      canvas.getByRole("button", { name: "Apply datamap rename" }),
    );
    await expect(canvas.getByText("datamap-updated.tsv")).toBeVisible();
    await expect(canvas.queryByText("datamap.tsv")).not.toBeInTheDocument();

    await userEvent.click(canvas.getByText("datamap-updated.tsv"));
    await expect(canvas.getByTestId("rename-selected")).toHaveTextContent(
      "arc/assays/assay_05/datamap.tsv",
    );
  },
};

type RenderCountNodeProps = {
  node: DemoNode;
  reportRender: (nodeId: string) => void;
};

const RenderCountNode = ({ node, reportRender }: RenderCountNodeProps) => {
  React.useEffect(() => reportRender(node.props.id));
  return <span>{node.props.label}</span>;
};

const SelectiveRenderingTree = () => {
  const [items, setItems] = React.useState<DemoNode[]>(() => [
    branch("workspace", "Workspace", [
      leaf("workspace/alpha.txt", "alpha.txt"),
      leaf("workspace/beta.txt", "beta.txt"),
    ]),
    leaf("stable-one.txt", "stable-one.txt"),
    leaf("stable-two.txt", "stable-two.txt"),
  ]);
  const [selected, setSelected] = React.useState<string[]>([]);
  const [renderCounts, setRenderCounts] = React.useState<
    Record<string, number>
  >({});
  const expandedIds = React.useMemo(() => ["workspace"], []);

  const reportRender = React.useCallback((nodeId: string) => {
    setRenderCounts((current) => ({
      ...current,
      [nodeId]: (current[nodeId] ?? 0) + 1,
    }));
  }, []);

  const renderNode = React.useCallback(
    (props: DemoRenderProps) => (
      <RenderCountNode node={props.node} reportRender={reportRender} />
    ),
    [reportRender],
  );

  const renameBeta = React.useCallback(() => {
    setItems((current) =>
      current.map((node) =>
        node.type === "branch" && node.props.id === "workspace"
          ? ({
              ...node,
              children: Array.from(node.children ?? []).map((child) =>
                child.props.id === "workspace/beta.txt"
                  ? ({
                      ...child,
                      props: { ...child.props, label: "beta-renamed.txt" },
                    } as DemoNode)
                  : child,
              ),
            } as DemoNode)
          : node,
      ),
    );
  }, []);

  const addGamma = React.useCallback(() => {
    setItems((current) =>
      current.map((node) =>
        node.type === "branch" && node.props.id === "workspace"
          ? ({
              ...node,
              children: [
                ...(node.children ?? []),
                leaf("workspace/gamma.txt", "gamma.txt"),
              ],
            } as DemoNode)
          : node,
      ),
    );
  }, []);

  const trackedNodeIds = [
    "workspace",
    "workspace/alpha.txt",
    "workspace/beta.txt",
    "workspace/gamma.txt",
    "stable-one.txt",
    "stable-two.txt",
  ];

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        defaultExpandedIds={expandedIds}
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        renderNode={renderNode}
        debug
      />
      <div className="swt:flex swt:gap-2">
        <button
          type="button"
          className="swt:btn swt:btn-sm"
          onClick={() => setSelected(["workspace/beta.txt"])}
        >
          Select beta
        </button>
        <button
          type="button"
          className="swt:btn swt:btn-sm"
          onClick={renameBeta}
        >
          Rename beta
        </button>
        <button type="button" className="swt:btn swt:btn-sm" onClick={addGamma}>
          Add gamma
        </button>
      </div>
      {trackedNodeIds.map((nodeId) => (
        <output key={nodeId} data-testid={`render-count-${nodeId}`}>
          {renderCounts[nodeId] ?? 0}
        </output>
      ))}
    </div>
  );
};

const initiallyRenderedNodeIds = [
  "workspace",
  "workspace/alpha.txt",
  "workspace/beta.txt",
  "stable-one.txt",
  "stable-two.txt",
];

export const SelectionRerendersOnlyAffectedRows: Story = {
  render: () => <SelectiveRenderingTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const renderCount = (nodeId: string) =>
      Number(canvas.getByTestId(`render-count-${nodeId}`).textContent);

    await waitFor(() =>
      expect(
        initiallyRenderedNodeIds.every((nodeId) => renderCount(nodeId) > 0),
      ).toBe(true),
    );
    const beforeSelection = {
      workspace: renderCount("workspace"),
      alpha: renderCount("workspace/alpha.txt"),
      beta: renderCount("workspace/beta.txt"),
      stableOne: renderCount("stable-one.txt"),
      stableTwo: renderCount("stable-two.txt"),
    };

    await userEvent.click(canvas.getByRole("button", { name: "Select beta" }));
    await waitFor(() =>
      expect(renderCount("workspace/beta.txt")).toBeGreaterThan(
        beforeSelection.beta,
      ),
    );
    await expect(
      canvas.getByTestId("tree-node-workspace/beta.txt"),
    ).toHaveAttribute("aria-selected", "true");
    expect(renderCount("workspace")).toBeGreaterThan(beforeSelection.workspace);
    expect(renderCount("workspace/alpha.txt")).toBe(beforeSelection.alpha);
    expect(renderCount("stable-one.txt")).toBe(beforeSelection.stableOne);
    expect(renderCount("stable-two.txt")).toBe(beforeSelection.stableTwo);
  },
};

export const RenameAndAddRerenderOnlyAffectedRows: Story = {
  render: () => <SelectiveRenderingTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const renderCount = (nodeId: string) =>
      Number(canvas.getByTestId(`render-count-${nodeId}`).textContent);

    await waitFor(() =>
      expect(
        initiallyRenderedNodeIds.every((nodeId) => renderCount(nodeId) > 0),
      ).toBe(true),
    );
    const beforeRename = {
      workspace: renderCount("workspace"),
      alpha: renderCount("workspace/alpha.txt"),
      beta: renderCount("workspace/beta.txt"),
      stableOne: renderCount("stable-one.txt"),
      stableTwo: renderCount("stable-two.txt"),
    };

    await userEvent.click(canvas.getByRole("button", { name: "Rename beta" }));
    await waitFor(() =>
      expect(renderCount("workspace/beta.txt")).toBeGreaterThan(
        beforeRename.beta,
      ),
    );
    await expect(canvas.getByText("beta-renamed.txt")).toBeVisible();
    expect(renderCount("workspace")).toBeGreaterThan(beforeRename.workspace);
    expect(renderCount("workspace/alpha.txt")).toBe(beforeRename.alpha);
    expect(renderCount("stable-one.txt")).toBe(beforeRename.stableOne);
    expect(renderCount("stable-two.txt")).toBe(beforeRename.stableTwo);

    const beforeAdd = {
      workspace: renderCount("workspace"),
      alpha: renderCount("workspace/alpha.txt"),
      beta: renderCount("workspace/beta.txt"),
      stableOne: renderCount("stable-one.txt"),
      stableTwo: renderCount("stable-two.txt"),
    };

    await userEvent.click(canvas.getByRole("button", { name: "Add gamma" }));
    await waitFor(() =>
      expect(renderCount("workspace/gamma.txt")).toBeGreaterThan(0),
    );
    await expect(canvas.getByText("gamma.txt")).toBeVisible();
    expect(renderCount("workspace")).toBeGreaterThan(beforeAdd.workspace);
    expect(renderCount("workspace/alpha.txt")).toBe(beforeAdd.alpha);
    expect(renderCount("workspace/beta.txt")).toBe(beforeAdd.beta);
    expect(renderCount("stable-one.txt")).toBe(beforeAdd.stableOne);
    expect(renderCount("stable-two.txt")).toBe(beforeAdd.stableTwo);
  },
};

const LatestKeyboardNavigationTree = () => {
  const requestRef = React.useRef<Deferred<DemoNode[]> | undefined>(undefined);
  const items = React.useMemo(
    () => [branch("lazy-a", "Lazy A"), leaf("branch-b", "Branch B")],
    [],
  );

  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        if (item?.props.id !== "lazy-a") return [];
        const request = createDeferred<DemoNode[]>();
        requestRef.current = request;
        return request.promise;
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource} debug />
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onMouseDown={(event) => event.preventDefault()}
        onClick={() =>
          requestRef.current?.resolve([leaf("lazy-a/child.txt", "Lazy child")])
        }
      >
        Resolve lazy child
      </button>
    </div>
  );
};

export const KeyboardNavigationUsesLatestVisibleRows: Story = {
  render: () => <LatestKeyboardNavigationTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand Lazy A" }),
    );
    await expectLoadingIndicator(canvasElement);

    const branchB = canvas.getByTestId("tree-node-branch-b");
    branchB.focus();
    await expect(branchB).toHaveFocus();

    await userEvent.click(
      canvas.getByRole("button", { name: "Resolve lazy child" }),
    );
    await expect(await canvas.findByText("Lazy child")).toBeVisible();
    await expect(branchB).toHaveFocus();

    await userEvent.keyboard("{ArrowUp}");
    await waitFor(() =>
      expect(canvas.getByTestId("tree-node-lazy-a/child.txt")).toHaveFocus(),
    );
  },
};

const DescendantKeyboardTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);
  const items = React.useMemo(
    () => [
      branch("interactive", "interactive", [
        leaf("interactive/child.txt", "child.txt"),
      ]),
    ],
    [],
  );

  const renderNode = React.useCallback(
    (props: DemoRenderProps) =>
      props.node.props.id === "interactive" ? (
        <input
          aria-label="Tree node editor"
          className="swt:input swt:input-sm swt:input-bordered"
        />
      ) : (
        <span>{props.node.props.label}</span>
      ),
    [],
  );

  return (
    <div className="swt:w-96">
      <Tree
        items={items}
        defaultExpandedIds={["interactive"]}
        selectedIds={selected}
        onSelectionChange={(nextSelected) =>
          setSelected(Array.from(nextSelected))
        }
        renderNode={renderNode}
        debug
      />
      <div data-testid="descendant-key-selected">
        {selected.join(",") || "none"}
      </div>
    </div>
  );
};

export const DescendantInteractiveClicksKeepDefaultBehavior: Story = {
  render: () => <DescendantKeyboardTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const editor = canvas.getByRole("textbox", { name: "Tree node editor" });

    await userEvent.click(editor);
    await expect(editor).toHaveFocus();
    await expect(canvas.getByText("child.txt")).toBeVisible();
    await expect(
      canvas.getByTestId("descendant-key-selected"),
    ).toHaveTextContent("none");
  },
};

export const DescendantKeyboardEventsKeepDefaultBehavior: Story = {
  render: () => <DescendantKeyboardTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const editor = canvas.getByRole("textbox", { name: "Tree node editor" });

    editor.focus();
    expect(fireEvent.keyDown(editor, { key: "ArrowLeft" })).toBe(true);
    await expect(editor).toHaveFocus();
    await expect(canvas.getByText("child.txt")).toBeVisible();

    await userEvent.type(editor, "alpha beta", { skipClick: true });
    await expect(editor).toHaveValue("alpha beta");
    await expect(canvas.getByText("child.txt")).toBeVisible();
    await expect(
      canvas.getByTestId("descendant-key-selected"),
    ).toHaveTextContent("none");
  },
};

export const KeyboardNavigation: Story = {
  render: () => <BasicTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.tab();
    await expect(canvas.getByTestId("tree-node-arc")).toHaveFocus();

    await userEvent.keyboard("{ArrowDown}");
    await waitFor(() =>
      expect(canvas.getByTestId("tree-node-arc/studies")).toHaveFocus(),
    );

    await userEvent.keyboard("{ArrowRight}");
    await waitFor(() =>
      expect(
        canvas.getByTestId("tree-node-arc/studies/study_01"),
      ).toHaveFocus(),
    );

    await userEvent.keyboard("{ArrowRight}");
    await waitFor(() =>
      expect(
        canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx"),
      ).toHaveFocus(),
    );

    await userEvent.keyboard("{ArrowLeft}");
    await waitFor(() =>
      expect(
        canvas.getByTestId("tree-node-arc/studies/study_01"),
      ).toHaveFocus(),
    );

    await userEvent.keyboard("{Enter}");
    await waitFor(() =>
      expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument(),
    );
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent(
      "arc/studies/study_01",
    );
    await expect(
      canvas.getByTestId("tree-node-arc/studies/study_01"),
    ).toHaveFocus();

    await userEvent.keyboard("{Enter}");
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();

    await userEvent.keyboard("{ArrowLeft}");
    await waitFor(() =>
      expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument(),
    );

    await userEvent.keyboard("{ArrowRight}");
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();
  },
};
