import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fireEvent, screen, userEvent, waitFor, within } from "storybook/test";
import { Tree } from "./Tree.fs.js";
import type { TreeApi, TreeItem } from "./Types.fs.js";

type DemoPayload = {
  badge?: string;
};

type DemoNode = TreeItem<DemoPayload>;

const branch = (id: string, label: string, children?: DemoNode[], payload?: DemoPayload): DemoNode =>
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

const delayed = <T,>(value: T, ms = 300) => new Promise<T>((resolve) => setTimeout(() => resolve(value), ms));

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

const expectLoadingIndicator = async (canvasElement: HTMLElement) => {
  await waitFor(() => expect(canvasElement.querySelector(".swt\\:loading")).toBeTruthy());
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
        onSelectionChange={setSelected}
        debug
      />
      <div data-testid="selected-node">{selected.join(",") || "none"}</div>
    </div>
  );
};

export const BasicExpansionAndSelection: Story = {
  render: () => <BasicTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByRole("tree")).toBeVisible();
    await expect(canvas.getByText("isa.study.xlsx")).toBeVisible();
    await expect(canvas.getByTestId("tree-node-arc")).toHaveAttribute("aria-posinset", "1");
    await expect(canvas.getByTestId("tree-node-arc")).toHaveAttribute("aria-setsize", "1");
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveAttribute("aria-posinset", "1");
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveAttribute("aria-setsize", "3");
    await expect(canvas.getByTestId("tree-node-arc/assays")).toHaveAttribute("aria-posinset", "2");
    await expect(canvas.getByTestId("tree-node-arc/isa.investigation.xlsx")).toHaveAttribute("aria-posinset", "3");

    await userEvent.click(canvas.getByText("studies"));
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("arc/studies");
    await expect(canvas.getByText("isa.study.xlsx")).toBeVisible();

    await userEvent.click(canvas.getByText("studies"));
    await expect(canvas.getByText("isa.study.xlsx")).toBeVisible();

    await userEvent.click(canvas.getByText("isa.study.xlsx"));
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("arc/studies/study_01/isa.study.xlsx");

    await userEvent.click(canvas.getByRole("button", { name: "Collapse studies" }));
    await waitFor(() => expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument());

    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();

    await userEvent.click(canvas.getByText("studies"));
    await expect(canvas.getByText("isa.study.xlsx")).toBeVisible();
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("arc/studies");
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
        onSelectionChange={setSelected}
        debug
      />
      <div data-testid="folder-selection">Selected: {selected.join("|") || "none"}</div>
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

    await userEvent.click(canvas.getByText("studies"));
    await expect(studiesNode).toHaveAttribute("aria-selected", "true");
    await expect(studiesNode).toHaveAttribute("aria-expanded", "false");
    await expect(canvas.getByTestId("folder-selection")).toHaveTextContent("Selected: arc/studies");
    await expect(canvas.queryByText("Study 01")).not.toBeInTheDocument();

    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expect(studiesNode).toHaveAttribute("aria-selected", "true");
    await expect(studiesNode).toHaveAttribute("aria-expanded", "true");
    await expect(canvas.getByText("Study 01")).toBeVisible();
  },
};

export const EnterOpensAFolder: Story = {
  render: () => <FolderSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const studiesNode = canvas.getByTestId("tree-node-arc/studies");

    studiesNode.focus();
    await expect(studiesNode).toHaveFocus();
    await expect(studiesNode).toHaveAttribute("aria-expanded", "false");

    fireEvent.keyDown(studiesNode, { key: "Enter" });
    await waitFor(() => expect(studiesNode).toHaveAttribute("aria-expanded", "true"));
    await expect(canvas.getByText("Study 01")).toBeVisible();
    await expect(studiesNode).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("folder-selection")).toHaveTextContent("Selected: arc/studies");
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
        selectionMode={"multiple" as any}
        selectedIds={selected}
        onSelectionChange={setSelected}
        debug
      />
      <div data-testid="select-until-selection">Selected: {selected.join("|") || "none"}</div>
    </div>
  );
};

export const ShiftSelectsUntilClickedNode: Story = {
  render: () => <SelectUntilTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("beta"));
    fireEvent.click(canvas.getByText("delta"), { shiftKey: true, bubbles: true });

    await expect(canvas.getByTestId("tree-node-alpha.txt")).toHaveAttribute("aria-selected", "false");
    await expect(canvas.getByTestId("tree-node-beta")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-gamma.txt")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-delta")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-epsilon.txt")).toHaveAttribute("aria-selected", "false");
    await expect(canvas.getByTestId("select-until-selection")).toHaveTextContent("beta|delta|gamma.txt");
  },
};

const MultiSelectionTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["arc", "arc/studies", "arc/studies/study_01", "arc/assays", "arc/assays/assay_01"]}
        selectionMode={"multiple" as any}
        selectedIds={selected}
        onSelectionChange={setSelected}
        debug
      />
      <div data-testid="multi-selected">{selected.join("|") || "none"}</div>
      <button type="button">Outside tree</button>
    </div>
  );
};

export const MultiSelectionWithoutCheckboxes: Story = {
  render: () => <MultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.queryByRole("checkbox")).not.toBeInTheDocument();
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveAttribute("aria-selected", "false");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx")).toHaveAttribute("aria-selected", "false");
    expect(canvas.getByTestId("tree-node-arc/studies").className).toContain("swt:cursor-pointer");
    expect(canvas.getByTestId("tree-node-arc/studies").className).toContain("swt:hover:bg-base-200");
    await userEvent.click(canvas.getByText("studies"));
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByText("isa.study.xlsx")).toBeVisible();

    await userEvent.click(canvas.getByText("isa.study.xlsx"));
    fireEvent.click(canvas.getByText("isa.assay.xlsx"), { ctrlKey: true, bubbles: true });
    await expect(canvas.getByTestId("multi-selected")).toHaveTextContent("arc/studies/study_01/isa.study.xlsx");
    await expect(canvas.getByTestId("multi-selected")).toHaveTextContent("arc/assays/assay_01/isa.assay.xlsx");

    fireEvent.click(canvas.getByText("isa.assay.xlsx"), { ctrlKey: true, bubbles: true });
    await expect(canvas.getByTestId("tree-node-arc/assays/assay_01/isa.assay.xlsx")).toHaveAttribute("aria-selected", "false");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx")).toHaveAttribute("aria-selected", "true");

    await userEvent.click(canvas.getByText("studies"));
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveFocus();
    fireEvent.click(canvas.getByText("isa.study.xlsx"), { shiftKey: true, bubbles: true });
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_01")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_01/datamap.tsv")).toHaveAttribute("aria-selected", "false");

    const activeNode = canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx");
    await expect(activeNode).toHaveAttribute("data-tree-active", "true");
    activeNode.focus();
    await waitFor(() => expect(activeNode).toHaveAttribute("data-tree-focused", "true"));
    await userEvent.click(canvas.getByRole("button", { name: "Outside tree" }));
    await expect(activeNode).toHaveAttribute("data-tree-active", "true");
    await waitFor(() => expect(activeNode).toHaveAttribute("data-tree-focused", "false"));
    await expect(activeNode).toHaveAttribute("tabindex", "0");
  },
};

const SelectionModeNormalizationTree = () => {
  const items = React.useMemo(() => [leaf("alpha.txt", "alpha.txt"), leaf("beta.txt", "beta.txt")], []);
  const [selectionMode, setSelectionMode] = React.useState<"multiple" | "single">("multiple");
  const [selected, setSelected] = React.useState(["alpha.txt", "beta.txt"]);

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        selectionMode={selectionMode as any}
        selectedIds={selected}
        onSelectionChange={setSelected}
        debug
      />
      <button type="button" className="swt:btn swt:btn-sm" onClick={() => setSelectionMode("single")}>
        Use single selection
      </button>
    </div>
  );
};

export const SingleSelectionNormalizesControlledIds: Story = {
  render: () => <SelectionModeNormalizationTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByTestId("tree-node-alpha.txt")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-beta.txt")).toHaveAttribute("aria-selected", "true");

    await userEvent.click(canvas.getByRole("button", { name: "Use single selection" }));
    await expect(canvas.getByTestId("tree-node-alpha.txt")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-beta.txt")).toHaveAttribute("aria-selected", "false");
    await expect(canvas.getByTestId("tree-selected-ids")).toHaveTextContent("alpha.txt");
    await expect(canvas.getByTestId("tree-selected-ids")).not.toHaveTextContent("beta.txt");
  },
};

const UncontrolledMultiSelectionTree = () => {
  const items = React.useMemo(() => [leaf("alpha.txt", "alpha.txt"), leaf("beta.txt", "beta.txt")], []);

  return (
    <div className="swt:w-96">
      <Tree items={items} selectionMode={"multiple" as any} debug />
    </div>
  );
};

export const UncontrolledMultiSelectionUsesLatestState: Story = {
  render: () => <UncontrolledMultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("alpha.txt"));
    fireEvent.click(canvas.getByText("beta.txt"), { ctrlKey: true, bubbles: true });

    await expect(canvas.getByTestId("tree-node-alpha.txt")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-node-beta.txt")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByTestId("tree-selected-ids")).toHaveTextContent("alpha.txt");
    await expect(canvas.getByTestId("tree-selected-ids")).toHaveTextContent("beta.txt");
  },
};

const DisabledSelectionTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["arc", "arc/studies", "arc/studies/study_01"]}
        isSelectionDisabled
        selectedIds={selected}
        onSelectionChange={setSelected}
        debug
      />
      <div data-testid="disabled-selected">{selected.join(",") || "none"}</div>
    </div>
  );
};

export const DisabledSelection: Story = {
  render: () => <DisabledSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("isa.study.xlsx"));
    await expect(canvas.getByTestId("disabled-selected")).toHaveTextContent("none");
  },
};

const LazyTree = () => {
  const [loadCount, setLoadCount] = React.useState(0);
  const apiRef = React.useRef<TreeApi | null>(null);

  const items = React.useMemo(() => [branch("arc/lazy-studies", "studies", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        setLoadCount((count) => count + 1);
        return delayed(
          item?.props.id === "arc/lazy-studies"
            ? [branch("arc/lazy-studies/study_02", "Study 02", [leaf("arc/lazy-studies/study_02/isa.study.xlsx", "isa.study.xlsx")])]
            : [],
        );
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource as any} apiRef={apiRef as any} debug />
      <button type="button" className="swt:btn swt:btn-sm" onClick={() => apiRef.current?.invalidateNode("arc/lazy-studies")}>
        Invalidate studies cache
      </button>
      <div data-testid="load-count">Loads: {loadCount}</div>
    </div>
  );
};

export const LazyLoadingCachesChildren: Story = {
  render: () => <LazyTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expectLoadingIndicator(canvasElement);
    await userEvent.click(canvas.getByRole("button", { name: "Invalidate studies cache" }));
    await expect(canvas.getByRole("button", { name: "Expand studies" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expectLoadingIndicator(canvasElement);
    await expect(await canvas.findByText("Study 02")).toBeVisible();
    await expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 2");

    await userEvent.click(canvas.getByRole("button", { name: "Collapse studies" }));
    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 2");

    await userEvent.click(canvas.getByRole("button", { name: "Invalidate studies cache" }));
    await expect(canvas.getByRole("button", { name: "Expand studies" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expectLoadingIndicator(canvasElement);
    await expect(await canvas.findByText("Study 02")).toBeVisible();
    await expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 3");
  },
};

const StaleFailureTree = () => {
  const apiRef = React.useRef<TreeApi | null>(null);
  const requestsRef = React.useRef<Deferred<DemoNode[]>[]>([]);
  const [requestCount, setRequestCount] = React.useState(0);
  const [requestSettlements, setRequestSettlements] = React.useState<string[]>([]);
  const [errorCount, setErrorCount] = React.useState(0);
  const items = React.useMemo(() => [branch("arc/concurrent", "concurrent", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        if (item?.props.id !== "arc/concurrent") return [];
        const request = createDeferred<DemoNode[]>();
        requestsRef.current.push(request);
        const requestNumber = requestsRef.current.length;
        setRequestCount(requestNumber);

        try {
          const children = await request.promise;
          setRequestSettlements((current) => [...current, `request-${requestNumber}:resolved`]);
          return children;
        } catch (error) {
          setRequestSettlements((current) => [...current, `request-${requestNumber}:rejected`]);
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
        dataSource={dataSource as any}
        apiRef={apiRef as any}
        onError={() => setErrorCount((count) => count + 1)}
        debug
      />
      <button type="button" className="swt:btn swt:btn-sm" onClick={() => apiRef.current?.invalidateNode("arc/concurrent")}>
        Invalidate pending request
      </button>
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={() => requestsRef.current[1]?.resolve([leaf("arc/concurrent/fresh.txt", "fresh.txt")])}
      >
        Resolve second request
      </button>
      <button type="button" className="swt:btn swt:btn-sm" onClick={() => requestsRef.current[0]?.reject(new Error("stale failure"))}>
        Reject first request
      </button>
      <div data-testid="stale-request-count">Requests: {requestCount}</div>
      <div data-testid="stale-request-settlements">Settled: {requestSettlements.join("|") || "none"}</div>
      <div data-testid="stale-error-count">Errors: {errorCount}</div>
    </div>
  );
};

export const StaleLazyFailureDoesNotCollapseNewerResult: Story = {
  render: () => <StaleFailureTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Expand concurrent" }));
    await expectLoadingIndicator(canvasElement);
    await expect(canvas.getByTestId("stale-request-count")).toHaveTextContent("Requests: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Invalidate pending request" }));
    await expect(canvas.getByRole("button", { name: "Expand concurrent" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand concurrent" }));
    await expectLoadingIndicator(canvasElement);
    await expect(canvas.getByTestId("stale-request-count")).toHaveTextContent("Requests: 2");

    await userEvent.click(canvas.getByRole("button", { name: "Resolve second request" }));
    await expect(await canvas.findByText("fresh.txt")).toBeVisible();
    await expect(canvas.getByTestId("stale-request-settlements")).toHaveTextContent("request-2:resolved");
    await expect(canvas.getByRole("button", { name: "Collapse concurrent" })).toBeVisible();

    await userEvent.click(canvas.getByRole("button", { name: "Reject first request" }));
    await waitFor(() =>
      expect(canvas.getByTestId("stale-request-settlements")).toHaveTextContent("request-1:rejected"),
    );
    await waitFor(() => {
      expect(canvas.getByText("fresh.txt")).toBeVisible();
      expect(canvas.getByRole("button", { name: "Collapse concurrent" })).toBeVisible();
      expect(canvas.getByTestId("stale-error-count")).toHaveTextContent("Errors: 0");
      expect(canvas.queryByText("Error")).not.toBeInTheDocument();
    });
  },
};

const ParentAwareDataSourceTree = () => {
  const [loadLog, setLoadLog] = React.useState<string[]>([]);
  const items = React.useMemo(() => [branch("remote/arc", "Remote Swate ARC", undefined)], []);

  const dataSource = React.useMemo(
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
              leaf("remote/arc/isa.investigation.xlsx", "isa.investigation.xlsx"),
            ];
          case "remote/arc/studies":
            return [branch("remote/arc/studies/study_03", "Study 03", [leaf("remote/arc/studies/study_03/isa.study.xlsx", "isa.study.xlsx")])];
          case "remote/arc/runs":
            return [branch("remote/arc/runs/run_01", "Run 01", [leaf("remote/arc/runs/run_01/isa.run.xlsx", "isa.run.xlsx")])];
          default:
            return [];
        }
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource as any} debug />
      <div data-testid="datasource-load-log">Loaded: {loadLog.join("|") || "none"}</div>
    </div>
  );
};

export const DataSourceLoadsChildrenForExpandedBranch: Story = {
  render: () => <ParentAwareDataSourceTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Expand Remote Swate ARC" }));
    await expect(await canvas.findByText("isa.investigation.xlsx")).toBeVisible();
    await expect(canvas.getByText("studies")).toBeVisible();
    await expect(canvas.getByText("runs")).toBeVisible();
    await expect(canvas.getByRole("button", { name: "Expand runs" })).toBeVisible();
    await expect(canvas.getByText("empty folder")).toBeVisible();
    await expect(canvas.getByRole("button", { name: "Expand empty folder" })).toBeVisible();
    await expect(canvas.getByTestId("datasource-load-log")).toHaveTextContent("remote/arc");

    await userEvent.click(canvas.getByText("empty folder"));
    await expect(canvas.getByTestId("tree-node-remote/arc/empty-folder")).toHaveAttribute("aria-selected", "true");
    await expect(canvas.getByRole("button", { name: "Expand empty folder" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand empty folder" }));
    await expect(canvas.getByRole("button", { name: "Collapse empty folder" })).toBeVisible();

    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expect(await canvas.findByText("Study 03")).toBeVisible();
    await expect(canvas.getByTestId("datasource-load-log")).toHaveTextContent("remote/arc|remote/arc/studies");

    await userEvent.click(canvas.getByRole("button", { name: "Expand runs" }));
    await expect(await canvas.findByText("Run 01")).toBeVisible();
    await expect(canvas.getByTestId("datasource-load-log")).toHaveTextContent("remote/arc|remote/arc/studies|remote/arc/runs");
  },
};

const DataSourceInvalidateAllTree = () => {
  const [loadCount, setLoadCount] = React.useState(0);
  const versionRef = React.useRef(1);
  const apiRef = React.useRef<TreeApi | null>(null);
  const items = React.useMemo(() => [branch("arc/workflows", "workflows", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      getTreeItems: async (item: DemoNode | null | undefined) => {
        const version = versionRef.current;
        setLoadCount((count) => count + 1);
        return delayed(
          item?.props.id === "arc/workflows"
            ? [branch(`arc/workflows/workflow_${version}`, `Workflow ${version}`, [leaf(`arc/workflows/workflow_${version}/workflow.xlsx`, "workflow.xlsx")])]
            : [],
        );
      },
    }),
    [],
  );

  const invalidateAll = React.useCallback(() => {
    versionRef.current += 1;
    apiRef.current?.invalidateAll();
  }, []);

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource as any} apiRef={apiRef as any} debug />
      <button type="button" className="swt:btn swt:btn-sm" onClick={invalidateAll}>
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

    await userEvent.click(canvas.getByRole("button", { name: "Expand workflows" }));
    await expectLoadingIndicator(canvasElement);
    await userEvent.click(canvas.getByRole("button", { name: "Invalidate all datasource cache" }));
    await expect(canvas.getByRole("button", { name: "Expand workflows" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand workflows" }));
    await expectLoadingIndicator(canvasElement);
    await expect(await canvas.findByText("Workflow 2")).toBeVisible();
    await expect(canvas.queryByText("Workflow 1")).not.toBeInTheDocument();
    await expect(canvas.getByTestId("datasource-invalidate-loads")).toHaveTextContent("Loads: 2");
  },
};

const LazyErrorTree = () => {
  const [errorMessage, setErrorMessage] = React.useState("none");
  const [isReady, setIsReady] = React.useState(false);
  const originalConsoleError = React.useRef(console.error);
  const items = React.useMemo(() => [branch("arc/runs", "runs", undefined)], []);

  React.useEffect(() => {
    console.error = (error: unknown) => setErrorMessage(error instanceof Error ? error.message : String(error));
    setIsReady(true);

    return () => {
      console.error = originalConsoleError.current;
    };
  }, []);

  const dataSource = React.useMemo(
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
      {isReady ? <Tree items={items} dataSource={dataSource as any} debug /> : null}
      <div data-testid="lazy-error-message">Error: {errorMessage}</div>
    </div>
  );
};

export const LazyLoadingErrorState: Story = {
  render: () => <LazyErrorTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Expand runs" }));
    await expect(await canvas.findByText("Error")).toBeVisible();
    await expect(canvas.getByRole("button", { name: "Expand runs" })).toBeVisible();
    await expect(canvas.queryByRole("button", { name: "Collapse runs" })).not.toBeInTheDocument();
    await expect(canvas.getByTestId("lazy-error-message")).toHaveTextContent("Run metadata could not be loaded");
  },
};

const VirtualizedTree = () => {
  const numberedDirectories = React.useCallback(
    (parentId: string, namePrefix: string, labelPrefix: string, count: number) =>
      Array.from({ length: count }, (_, index) => {
        const number = (index + 1).toString().padStart(2, "0");
        return branch(`${parentId}/${namePrefix}_${number}`, `${labelPrefix} ${number}`, [
          leaf(`${parentId}/${namePrefix}_${number}/metadata.xlsx`, "metadata.xlsx"),
        ]);
      }),
    [],
  );

  const items = React.useMemo(
    () => [
      branch("arc", "Swate Demo ARC", [
        branch("arc/studies", "studies", numberedDirectories("arc/studies", "study", "Study", 24)),
        branch("arc/assays", "assays", numberedDirectories("arc/assays", "assay", "Assay", 24)),
        branch("arc/runs", "runs", numberedDirectories("arc/runs", "run", "Run", 16)),
        branch("arc/workflows", "workflows", numberedDirectories("arc/workflows", "workflow", "Workflow", 16)),
        branch("arc/docs", "docs", [
          leaf("arc/docs/README.md", "README.md"),
          leaf("arc/docs/changelog.md", "changelog.md"),
        ]),
      ]),
    ],
    [numberedDirectories],
  );

  return (
    <div className="swt:w-96">
      <Tree
        items={items}
        defaultExpandedIds={["arc", "arc/studies", "arc/assays", "arc/runs", "arc/workflows", "arc/docs"]}
        enableVirtualization
        estimateNodeHeight={34}
        debug
      />
    </div>
  );
};

export const VirtualizedRows: Story = {
  render: () => <VirtualizedTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByRole("tree")).toHaveAttribute("data-tree-root", "true");
    await expect(canvas.getByText("Swate Demo ARC")).toBeVisible();
    await expect(canvas.getByText("Study 01")).toBeVisible();
    const virtualizedViewport = canvasElement.querySelector("[data-tree-virtualized='true']") as HTMLElement;
    await expect(virtualizedViewport).toBeTruthy();

    virtualizedViewport.scrollTop = virtualizedViewport.scrollHeight;
    fireEvent.scroll(virtualizedViewport);

    await waitFor(() => expect(canvas.getByText("Workflow 16")).toBeVisible());
    await expect(canvas.queryByText("Study 01")).not.toBeInTheDocument();

    const workflowNode = canvas.getByTestId("tree-node-arc/workflows/workflow_16");
    workflowNode.focus();
    fireEvent.keyDown(workflowNode, { key: "End" });
    await waitFor(() => expect(canvas.getByTestId("tree-node-arc/docs/changelog.md")).toHaveFocus());

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/docs/changelog.md"), { key: "Home" });
    await waitFor(() => expect(canvas.getByTestId("tree-node-arc")).toHaveFocus());
  },
};

const ContextMenuTree = () => {
  const [lastAction, setLastAction] = React.useState("none");

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["arc", "arc/studies", "arc/studies/study_01"]}
        onContextMenu={(_event, node) => [
          {
            text: <span>Inspect {node?.props.label ?? "tree root"}</span>,
            onClick: () => setLastAction(node?.props.id ?? "root"),
          },
        ] as any}
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

    fireEvent.contextMenu(canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx"), {
      clientX: 20,
      clientY: 20,
      bubbles: true,
    });
    await userEvent.click(await screen.findByText("Inspect isa.study.xlsx"));
    await expect(canvas.getByTestId("last-action")).toHaveTextContent("arc/studies/study_01/isa.study.xlsx");

    fireEvent.contextMenu(canvas.getByRole("tree"), { clientX: 20, clientY: 20, bubbles: true });
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
          icon: <i data-testid="custom-tree-icon" className="swt:iconify swt:fluent--document-star-24-filled swt:size-4" />,
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
    await expect(canvas.getByTestId("tree-node-arc/featured.xlsx")).toHaveAttribute("title", "Featured ARC spreadsheet");
  },
};

const CustomTree = () => {
  const items = [branch("arc/studies/study_04", "Study 04", [leaf("arc/studies/study_04/isa.study.xlsx", "isa.study.xlsx", { badge: "ISA" })])];

  return (
    <div className="swt:w-96">
      <Tree
        items={items}
        defaultExpandedIds={["arc/studies/study_04"]}
        leading={(props) => (
          <span className="swt:badge swt:badge-xs">
            {props.node.type === "branch" ? (props.isExpanded ? "open" : "closed") : `depth-${props.depth}`}
          </span>
        )}
        trailing={(props) =>
          props.node.props.data?.badge ? (
            <button type="button" className="swt:badge swt:badge-primary swt:badge-sm" onClick={(event) => props.select(event as any)}>
              {props.isSelected ? "Selected" : props.node.props.data.badge}
            </button>
          ) : null
        }
        renderNode={(props) => (
          <span className="swt:flex swt:items-center swt:gap-2">
            <strong>{props.isFocused ? `${props.node.props.label} focused` : props.node.props.label}</strong>
            {props.node.type === "branch" ? (
              <button
                type="button"
                className="swt:btn swt:btn-ghost swt:btn-xs"
                onClick={(event) => {
                  event.preventDefault();
                  event.stopPropagation();
                  props.toggle();
                }}
              >
                Custom toggle {props.node.props.label}
              </button>
            ) : null}
          </span>
        )}
        styleFn={(node, classes) => {
          if (!node) return [...classes, "swt:border", "swt:border-info"];
          if (node?.props.id === "arc/studies/study_04") return [...classes, "swt:text-primary"];
          if (node?.props.id === "arc/studies/study_04/isa.study.xlsx") return [...classes, "swt:text-accent"];
          return classes;
        }}
        debug
      />
    </div>
  );
};

export const CustomRenderingAndStyling: Story = {
  render: () => <CustomTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByText("isa.study.xlsx")).toBeVisible();
    await expect(canvas.getByText("ISA")).toBeVisible();
    await expect(canvas.getByText("open")).toBeVisible();
    await expect(canvas.getByText("depth-1")).toBeVisible();
    await expect(canvas.getByTestId("generic-tree")).toHaveClass("swt:border-info");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_04")).toHaveClass("swt:text-primary");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_04/isa.study.xlsx")).toHaveClass("swt:text-accent");

    await userEvent.click(canvas.getByRole("button", { name: "ISA" }));
    await expect(canvas.getByText("Selected")).toBeVisible();

    canvas.getByTestId("tree-node-arc/studies/study_04/isa.study.xlsx").focus();
    await waitFor(() => expect(canvas.getByText("isa.study.xlsx focused")).toBeVisible());

    await userEvent.click(canvas.getByRole("button", { name: "Custom toggle Study 04" }));
    await waitFor(() => expect(canvas.queryByText("isa.study.xlsx focused")).not.toBeInTheDocument());
    await expect(canvas.getByText("closed")).toBeVisible();
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
              children: node.children?.map((child) =>
                child.props.id === "arc/assays/assay_05/datamap.tsv"
                  ? ({ ...child, props: { ...child.props, label: draftLabel } } as DemoNode)
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
        onSelectionChange={setSelected}
        debug
      />
      <input
        aria-label="Datamap file name"
        className="swt:input swt:input-sm swt:input-bordered"
        value={draftLabel}
        onChange={(event) => setDraftLabel(event.currentTarget.value)}
      />
      <button type="button" className="swt:btn swt:btn-sm" onClick={renameDatamap}>
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
    await userEvent.clear(canvas.getByRole("textbox", { name: "Datamap file name" }));
    await userEvent.type(canvas.getByRole("textbox", { name: "Datamap file name" }), "datamap-updated.tsv");
    await userEvent.click(canvas.getByRole("button", { name: "Apply datamap rename" }));
    await expect(canvas.getByText("datamap-updated.tsv")).toBeVisible();
    await expect(canvas.queryByText("datamap.tsv")).not.toBeInTheDocument();

    await userEvent.click(canvas.getByText("datamap-updated.tsv"));
    await expect(canvas.getByTestId("rename-selected")).toHaveTextContent("arc/assays/assay_05/datamap.tsv");
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
    branch("workspace", "Workspace", [leaf("workspace/alpha.txt", "alpha.txt"), leaf("workspace/beta.txt", "beta.txt")]),
    leaf("stable-one.txt", "stable-one.txt"),
    leaf("stable-two.txt", "stable-two.txt"),
  ]);
  const [selected, setSelected] = React.useState<string[]>([]);
  const [renderCounts, setRenderCounts] = React.useState<Record<string, number>>({});
  const expandedIds = React.useMemo(() => ["workspace"], []);

  const reportRender = React.useCallback((nodeId: string) => {
    setRenderCounts((current) => ({ ...current, [nodeId]: (current[nodeId] ?? 0) + 1 }));
  }, []);

  const renderNode = React.useCallback(
    (props: { node: DemoNode }) => <RenderCountNode node={props.node} reportRender={reportRender} />,
    [reportRender],
  );

  const renameBeta = React.useCallback(() => {
    setItems((current) =>
      current.map((node) =>
        node.type === "branch" && node.props.id === "workspace"
          ? ({
              ...node,
              children: node.children?.map((child) =>
                child.props.id === "workspace/beta.txt"
                  ? ({ ...child, props: { ...child.props, label: "beta-renamed.txt" } } as DemoNode)
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
              children: [...(node.children ?? []), leaf("workspace/gamma.txt", "gamma.txt")],
            } as DemoNode)
          : node,
      ),
    );
  }, []);

  const trackedNodeIds = ["workspace", "workspace/alpha.txt", "workspace/beta.txt", "workspace/gamma.txt", "stable-one.txt", "stable-two.txt"];

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        defaultExpandedIds={expandedIds}
        selectedIds={selected}
        onSelectionChange={setSelected}
        renderNode={renderNode as any}
        debug
      />
      <div className="swt:flex swt:gap-2">
        <button type="button" className="swt:btn swt:btn-sm" onClick={() => setSelected(["workspace/beta.txt"])}>
          Select beta
        </button>
        <button type="button" className="swt:btn swt:btn-sm" onClick={renameBeta}>
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

export const OnlyAffectedNodesRerender: Story = {
  render: () => <SelectiveRenderingTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const renderCount = (nodeId: string) => Number(canvas.getByTestId(`render-count-${nodeId}`).textContent);

    await waitFor(() => expect(renderCount("workspace/beta.txt")).toBeGreaterThan(0));
    const beforeSelection = {
      workspace: renderCount("workspace"),
      alpha: renderCount("workspace/alpha.txt"),
      beta: renderCount("workspace/beta.txt"),
      stableOne: renderCount("stable-one.txt"),
      stableTwo: renderCount("stable-two.txt"),
    };

    await userEvent.click(canvas.getByRole("button", { name: "Select beta" }));
    await waitFor(() => expect(renderCount("workspace/beta.txt")).toBeGreaterThan(beforeSelection.beta));
    await expect(canvas.getByTestId("tree-node-workspace/beta.txt")).toHaveAttribute("aria-selected", "true");
    expect(renderCount("workspace")).toBeGreaterThan(beforeSelection.workspace);
    expect(renderCount("workspace/alpha.txt")).toBe(beforeSelection.alpha);
    expect(renderCount("stable-one.txt")).toBe(beforeSelection.stableOne);
    expect(renderCount("stable-two.txt")).toBe(beforeSelection.stableTwo);

    const beforeRename = {
      workspace: renderCount("workspace"),
      alpha: renderCount("workspace/alpha.txt"),
      beta: renderCount("workspace/beta.txt"),
      stableOne: renderCount("stable-one.txt"),
      stableTwo: renderCount("stable-two.txt"),
    };

    await userEvent.click(canvas.getByRole("button", { name: "Rename beta" }));
    await waitFor(() => expect(renderCount("workspace/beta.txt")).toBeGreaterThan(beforeRename.beta));
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
    await waitFor(() => expect(renderCount("workspace/gamma.txt")).toBeGreaterThan(0));
    await expect(canvas.getByText("gamma.txt")).toBeVisible();
    expect(renderCount("workspace")).toBeGreaterThan(beforeAdd.workspace);
    expect(renderCount("workspace/alpha.txt")).toBe(beforeAdd.alpha);
    expect(renderCount("workspace/beta.txt")).toBe(beforeAdd.beta);
    expect(renderCount("stable-one.txt")).toBe(beforeAdd.stableOne);
    expect(renderCount("stable-two.txt")).toBe(beforeAdd.stableTwo);
  },
};

const LatestKeyboardNavigationTree = () => {
  const requestRef = React.useRef<Deferred<DemoNode[]> | null>(null);
  const items = React.useMemo(() => [branch("lazy-a", "Lazy A"), leaf("branch-b", "Branch B")], []);

  const dataSource = React.useMemo(
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
      <Tree items={items} dataSource={dataSource as any} debug />
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={() => requestRef.current?.resolve([leaf("lazy-a/child.txt", "Lazy child")])}
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

    await userEvent.click(canvas.getByRole("button", { name: "Expand Lazy A" }));
    await expectLoadingIndicator(canvasElement);

    const branchB = canvas.getByTestId("tree-node-branch-b");
    branchB.focus();
    await expect(branchB).toHaveFocus();

    await userEvent.click(canvas.getByRole("button", { name: "Resolve lazy child" }));
    await expect(await canvas.findByText("Lazy child")).toBeVisible();

    await userEvent.click(branchB);
    fireEvent.keyDown(branchB, { key: "ArrowUp" });
    await waitFor(() => expect(canvas.getByTestId("tree-node-lazy-a/child.txt")).toHaveFocus());
  },
};

const DescendantKeyboardTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);
  const items = React.useMemo(() => [branch("interactive", "interactive", [leaf("interactive/child.txt", "child.txt")])], []);

  const renderNode = React.useCallback(
    (props: { node: DemoNode }) =>
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
        onSelectionChange={setSelected}
        renderNode={renderNode as any}
        debug
      />
      <div data-testid="descendant-key-selected">{selected.join(",") || "none"}</div>
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
    await expect(canvas.getByTestId("descendant-key-selected")).toHaveTextContent("none");
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
    await expect(canvas.getByTestId("descendant-key-selected")).toHaveTextContent("none");
  },
};

export const KeyboardNavigation: Story = {
  render: () => <BasicTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const tree = canvas.getByRole("tree");

    await userEvent.tab();
    fireEvent.keyDown(tree.querySelector("[data-tree-node-id='arc']")!, { key: "ArrowDown" });
    await waitFor(() => expect(canvas.getByTestId("tree-node-arc/studies")).toHaveFocus());

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/studies"), { key: "ArrowRight" });
    await waitFor(() => expect(canvas.getByTestId("tree-node-arc/studies/study_01")).toHaveFocus());

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/studies/study_01"), { key: "ArrowRight" });
    await waitFor(() => expect(canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx")).toHaveFocus());

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx"), { key: "ArrowLeft" });
    await waitFor(() => expect(canvas.getByTestId("tree-node-arc/studies/study_01")).toHaveFocus());

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/studies/study_01"), { key: "Enter" });
    await waitFor(() => expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument());
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("arc/studies/study_01");

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/studies/study_01"), { key: "Enter" });
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/studies/study_01"), { key: "ArrowLeft" });
    await waitFor(() => expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument());

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/studies/study_01"), { key: "ArrowRight" });
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();
  },
};
