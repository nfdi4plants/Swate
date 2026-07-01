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
    id,
    label,
    kind: "branch",
    ...(children ? { children, childrenCount: children.length } : {}),
    data: payload,
  }) as DemoNode;

const leaf = (id: string, label: string, payload?: DemoPayload): DemoNode =>
  ({
    id,
    label,
    kind: "leaf",
    data: payload,
  }) as DemoNode;

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

const menuItem = (label: string, onClick: () => void) =>
  ({
    text: <span>{label}</span>,
    icon: <i className="swt:iconify swt:fluent--document-24-regular swt:size-4" />,
    onClick,
  }) as any;

const meta = {
  title: "Primitive Components/Tree",
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

    await userEvent.click(canvas.getByText("studies"));
    await waitFor(() => expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument());
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("arc/studies");

    await userEvent.click(canvas.getByText("studies"));
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();

    await userEvent.click(canvas.getByText("isa.study.xlsx"));
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("arc/studies/study_01/isa.study.xlsx");

    await userEvent.click(canvas.getByRole("button", { name: "Collapse studies" }));
    await waitFor(() => expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument());

    await userEvent.click(canvas.getByText("studies"));
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("arc/studies");
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
        isNodeSelectable={(node) => node.kind === ("leaf" as any)}
        debug
      />
      <div data-testid="multi-selected">{selected.join("|") || "none"}</div>
    </div>
  );
};

export const MultiSelectionWithoutCheckboxes: Story = {
  render: () => <MultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.queryByRole("checkbox")).not.toBeInTheDocument();
    await userEvent.click(canvas.getByText("studies"));
    await expect(canvas.getByTestId("multi-selected")).toHaveTextContent("none");
    await waitFor(() => expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument());

    await userEvent.click(canvas.getByText("studies"));
    await expect(await canvas.findByText("isa.study.xlsx")).toBeVisible();

    await userEvent.click(canvas.getByText("isa.study.xlsx"));
    await userEvent.click(canvas.getByText("isa.assay.xlsx"));
    await expect(canvas.getByTestId("multi-selected")).toHaveTextContent("arc/studies/study_01/isa.study.xlsx");
    await expect(canvas.getByTestId("multi-selected")).toHaveTextContent("arc/assays/assay_01/isa.assay.xlsx");
    await expect(canvas.getByTestId("tree-node-arc/assays/assay_01/isa.assay.xlsx")).toHaveFocus();
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
      GetChildrenCount: () => 1,
      GetTreeItems: async () => {
        setLoadCount((count) => count + 1);
        return [branch("arc/lazy-studies/study_02", "Study 02", [leaf("arc/lazy-studies/study_02/isa.study.xlsx", "isa.study.xlsx")])];
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource as any} apiRef={apiRef as any} debug />
      <button type="button" className="swt:btn swt:btn-sm" onClick={() => apiRef.current?.InvalidateNode("arc/lazy-studies")}>
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
    await waitFor(() => expect(canvas.getByText("Study 02")).toBeVisible());
    await expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Collapse studies" }));
    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Invalidate studies cache" }));
    await expect(canvas.getByRole("button", { name: "Expand studies" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await waitFor(() => expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 2"));
  },
};

const UnknownCountBranchTree = () => {
  const [loadLog, setLoadLog] = React.useState<string[]>([]);
  const items = React.useMemo(() => [branch("arc/assays", "assays", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      GetChildrenCount: () => 0,
      GetTreeItems: async (item: DemoNode | null | undefined) => {
        setLoadLog((current) => [...current, item?.id ?? "root"]);
        return [branch("arc/assays/assay_02", "Assay 02", [leaf("arc/assays/assay_02/isa.assay.xlsx", "isa.assay.xlsx")])];
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource as any} debug />
      <div data-testid="unknown-count-load-log">Loaded: {loadLog.join("|") || "none"}</div>
    </div>
  );
};

export const DataSourceBranchClickExpandsUnknownChildren: Story = {
  render: () => <UnknownCountBranchTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("assays"));
    await expect(await canvas.findByText("Assay 02")).toBeVisible();
    await expect(canvas.getByTestId("unknown-count-load-log")).toHaveTextContent("arc/assays");
  },
};

const ParentAwareDataSourceTree = () => {
  const [loadLog, setLoadLog] = React.useState<string[]>([]);
  const items = React.useMemo(() => [branch("remote/arc", "Remote Swate ARC", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      GetChildrenCount: (item: DemoNode | null | undefined) => {
        switch (item?.id) {
          case "remote/arc":
            return 2;
          case "remote/arc/studies":
            return 1;
          default:
            return 0;
        }
      },
      GetTreeItems: async (item: DemoNode | null | undefined) => {
        const parentId = item?.id ?? "root";
        setLoadLog((current) => [...current, parentId]);

        switch (parentId) {
          case "remote/arc":
            return [branch("remote/arc/studies", "studies", undefined), leaf("remote/arc/isa.investigation.xlsx", "isa.investigation.xlsx")];
          case "remote/arc/studies":
            return [branch("remote/arc/studies/study_03", "Study 03", [leaf("remote/arc/studies/study_03/isa.study.xlsx", "isa.study.xlsx")])];
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
    await expect(canvas.getByTestId("datasource-load-log")).toHaveTextContent("remote/arc");

    await userEvent.click(canvas.getByRole("button", { name: "Expand studies" }));
    await expect(await canvas.findByText("Study 03")).toBeVisible();
    await expect(canvas.getByTestId("datasource-load-log")).toHaveTextContent("remote/arc|remote/arc/studies");
  },
};

const DataSourceInvalidateAllTree = () => {
  const [loadCount, setLoadCount] = React.useState(0);
  const versionRef = React.useRef(1);
  const apiRef = React.useRef<TreeApi | null>(null);
  const items = React.useMemo(() => [branch("arc/workflows", "workflows", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      GetChildrenCount: () => 1,
      GetTreeItems: async () => {
        const version = versionRef.current;
        setLoadCount((count) => count + 1);
        return [branch(`arc/workflows/workflow_${version}`, `Workflow ${version}`, [leaf(`arc/workflows/workflow_${version}/workflow.xlsx`, "workflow.xlsx")])];
      },
    }),
    [],
  );

  const invalidateAll = React.useCallback(() => {
    versionRef.current += 1;
    apiRef.current?.InvalidateAll();
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
    await expect(await canvas.findByText("Workflow 1")).toBeVisible();
    await expect(canvas.getByTestId("datasource-invalidate-loads")).toHaveTextContent("Loads: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Collapse workflows" }));
    await userEvent.click(canvas.getByRole("button", { name: "Expand workflows" }));
    await expect(canvas.getByTestId("datasource-invalidate-loads")).toHaveTextContent("Loads: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Invalidate all datasource cache" }));
    await expect(canvas.getByRole("button", { name: "Expand workflows" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand workflows" }));
    await expect(await canvas.findByText("Workflow 2")).toBeVisible();
    await expect(canvas.getByTestId("datasource-invalidate-loads")).toHaveTextContent("Loads: 2");
  },
};

const LazyErrorTree = () => {
  const [errorMessage, setErrorMessage] = React.useState("none");
  const items = React.useMemo(() => [branch("arc/runs", "runs", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      GetChildrenCount: () => 1,
      GetTreeItems: async () => {
        throw new Error("Run metadata could not be loaded");
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource as any} onError={(error) => setErrorMessage(error.message)} debug />
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
    await expect(canvasElement.querySelector("[data-tree-virtualized='true']")).toBeTruthy();
  },
};

const ContextMenuTree = () => {
  const [lastAction, setLastAction] = React.useState("none");

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["arc", "arc/studies", "arc/studies/study_01"]}
        contextMenuItems={(node) => [
          menuItem(node ? `Inspect ${node.label}` : "Inspect ARC root", () => setLastAction(node?.id ?? "root")),
        ]}
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

    await waitFor(() => {
      fireEvent.contextMenu(canvas.getByTestId("tree-node-arc/studies/study_01/isa.study.xlsx"), { clientX: 20, clientY: 20, bubbles: true });
      expect(screen.getByRole("button", { name: /inspect isa.study.xlsx/i })).toBeInTheDocument();
    });
    await userEvent.click(screen.getByRole("button", { name: /inspect isa.study.xlsx/i }));
    await expect(canvas.getByTestId("last-action")).toHaveTextContent("arc/studies/study_01/isa.study.xlsx");

    await waitFor(() => {
      fireEvent.contextMenu(canvas.getByRole("tree"), { clientX: 20, clientY: 20, bubbles: true });
      expect(screen.getByRole("button", { name: /inspect arc root/i })).toBeInTheDocument();
    });
    await userEvent.click(screen.getByRole("button", { name: /inspect arc root/i }));
    await expect(canvas.getByTestId("last-action")).toHaveTextContent("root");
  },
};

const CustomTree = () => {
  const items = [branch("arc/studies/study_04", "Study 04", [leaf("arc/studies/study_04/isa.study.xlsx", "isa.study.xlsx", { badge: "ISA" })])];

  return (
    <div className="swt:w-96">
      <Tree
        items={items}
        defaultExpandedIds={["arc/studies/study_04"]}
        leading={(props) => <span className="swt:badge swt:badge-xs">{props.Depth}</span>}
        trailing={(props) =>
          props.Node.data?.badge ? <span className="swt:badge swt:badge-primary swt:badge-sm">{props.Node.data.badge}</span> : null
        }
        renderNode={(props) => <strong>{props.Node.label}</strong>}
        styleFn={(kind, node, classes) => {
          if (!kind) return [...classes, "swt:border", "swt:border-info"];
          if (node?.id === "arc/studies/study_04") return [...classes, "swt:text-primary"];
          if (node?.id === "arc/studies/study_04/isa.study.xlsx") return [...classes, "swt:text-accent"];
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
    await expect(canvas.getByTestId("generic-tree")).toHaveClass("swt:border-info");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_04")).toHaveClass("swt:text-primary");
    await expect(canvas.getByTestId("tree-node-arc/studies/study_04/isa.study.xlsx")).toHaveClass("swt:text-accent");
  },
};

const RenderCounterLabel = ({ label, onRender }: { label: string; onRender: () => void }) => {
  React.useEffect(() => {
    onRender();
  });

  return <span>{label}</span>;
};

const RenderCounterTree = () => {
  const countRef = React.useRef(0);
  const countElementRef = React.useRef<HTMLDivElement | null>(null);
  const [items, setItems] = React.useState<DemoNode[]>(() => [
    branch("arc/assays/assay_05", "Assay 05", [
      leaf("arc/assays/assay_05/isa.assay.xlsx", "isa.assay.xlsx"),
      leaf("arc/assays/assay_05/datamap.tsv", "datamap.tsv"),
      leaf("arc/assays/assay_05/raw-data.tsv", "raw-data.tsv"),
    ]),
  ]);

  const onRender = React.useCallback(() => {
    countRef.current += 1;

    if (countElementRef.current) {
      countElementRef.current.textContent = `Renders: ${countRef.current}`;
    }
  }, []);

  const renameDatamap = React.useCallback(() => {
    setItems((current) =>
      current.map((node) =>
        node.id === "arc/assays/assay_05"
          ? ({
              ...node,
              children: node.children?.map((child) =>
                child.id === "arc/assays/assay_05/datamap.tsv" ? ({ ...child, label: "datamap-updated.tsv" } as DemoNode) : child,
              ),
            } as DemoNode)
          : node,
      ),
    );
  }, []);

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree
        items={items}
        defaultExpandedIds={["arc/assays/assay_05"]}
        renderNode={(props) => <RenderCounterLabel label={props.Node.label} onRender={onRender} />}
        debug
      />
      <button type="button" className="swt:btn swt:btn-sm" onClick={renameDatamap}>
        Rename datamap
      </button>
      <div data-testid="render-count" ref={countElementRef}>
        Renders: 0
      </div>
    </div>
  );
};

const readRenderCount = (canvas: ReturnType<typeof within>) =>
  Number(canvas.getByTestId("render-count").textContent?.replace("Renders: ", "") ?? "0");

export const EfficientRenameRerendersAffectedNodes: Story = {
  render: () => <RenderCounterTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await waitFor(() => expect(readRenderCount(canvas)).toBeGreaterThan(0));
    const beforeRename = readRenderCount(canvas);

    await userEvent.click(canvas.getByRole("button", { name: "Rename datamap" }));
    await expect(canvas.getByText("datamap-updated.tsv")).toBeVisible();
    await waitFor(() => expect(readRenderCount(canvas)).toBeGreaterThan(beforeRename));

    const rerenderedLabels = readRenderCount(canvas) - beforeRename;
    expect(rerenderedLabels).toBeLessThanOrEqual(4);
  },
};

export const KeyboardNavigation: Story = {
  render: () => <BasicTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const tree = canvas.getByRole("tree");

    await userEvent.tab();
    fireEvent.keyDown(tree.querySelector("[data-tree-node-id='arc']")!, { key: "ArrowDown" });
    await expect(canvas.getByTestId("tree-node-arc/studies")).toHaveFocus();

    fireEvent.keyDown(canvas.getByTestId("tree-node-arc/studies"), { key: "ArrowRight" });
    await waitFor(() => expect(canvas.getByText("isa.study.xlsx")).toBeVisible());
  },
};
