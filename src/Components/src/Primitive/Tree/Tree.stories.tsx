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
  branch("project", "Project", [
    branch("project/schemas", "Schemas", [leaf("project/schemas/person", "Person schema")]),
    leaf("project/readme", "Readme"),
  ]),
  branch("data", "Data", [leaf("data/raw", "Raw data"), leaf("data/processed", "Processed data")]),
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
        defaultExpandedIds={["project", "project/schemas"]}
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
    await expect(canvas.getByText("Person schema")).toBeVisible();

    await userEvent.click(canvas.getByText("Project"));
    await waitFor(() => expect(canvas.queryByText("Readme")).not.toBeInTheDocument());
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("project");

    await userEvent.click(canvas.getByText("Project"));
    await expect(await canvas.findByText("Readme")).toBeVisible();

    await userEvent.click(canvas.getByText("Readme"));
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("project/readme");

    await userEvent.click(canvas.getByRole("button", { name: "Collapse Project" }));
    await waitFor(() => expect(canvas.queryByText("Readme")).not.toBeInTheDocument());

    await userEvent.click(canvas.getByText("Project"));
    await expect(await canvas.findByText("Readme")).toBeVisible();
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent("project");
  },
};

const MultiSelectionTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["project", "data"]}
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
    await userEvent.click(canvas.getByText("Project"));
    await expect(canvas.getByTestId("multi-selected")).toHaveTextContent("none");
    await waitFor(() => expect(canvas.queryByText("Readme")).not.toBeInTheDocument());

    await userEvent.click(canvas.getByText("Project"));
    await expect(await canvas.findByText("Readme")).toBeVisible();

    await userEvent.click(canvas.getByText("Readme"));
    await userEvent.click(canvas.getByText("Raw data"));
    await expect(canvas.getByTestId("multi-selected")).toHaveTextContent("project/readme");
    await expect(canvas.getByTestId("multi-selected")).toHaveTextContent("data/raw");
    await expect(canvas.getByTestId("tree-node-data/raw")).toHaveFocus();
  },
};

const DisabledSelectionTree = () => {
  const [selected, setSelected] = React.useState<string[]>([]);

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["project"]}
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

    await userEvent.click(canvas.getByText("Readme"));
    await expect(canvas.getByTestId("disabled-selected")).toHaveTextContent("none");
  },
};

const LazyTree = () => {
  const [loadCount, setLoadCount] = React.useState(0);
  const apiRef = React.useRef<TreeApi | null>(null);

  const items = React.useMemo(() => [branch("lazy", "Lazy branch", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      GetChildrenCount: () => 1,
      GetTreeItems: async () => {
        setLoadCount((count) => count + 1);
        return [leaf("lazy/child", "Loaded child")];
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource as any} apiRef={apiRef as any} debug />
      <button type="button" className="swt:btn swt:btn-sm" onClick={() => apiRef.current?.InvalidateNode("lazy")}>
        Invalidate lazy branch
      </button>
      <div data-testid="load-count">Loads: {loadCount}</div>
    </div>
  );
};

export const LazyLoadingCachesChildren: Story = {
  render: () => <LazyTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByRole("button", { name: "Expand Lazy branch" }));
    await waitFor(() => expect(canvas.getByText("Loaded child")).toBeVisible());
    await expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Collapse Lazy branch" }));
    await userEvent.click(canvas.getByRole("button", { name: "Expand Lazy branch" }));
    await expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Invalidate lazy branch" }));
    await expect(canvas.getByRole("button", { name: "Expand Lazy branch" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand Lazy branch" }));
    await waitFor(() => expect(canvas.getByTestId("load-count")).toHaveTextContent("Loads: 2"));
  },
};

const UnknownCountBranchTree = () => {
  const [loadLog, setLoadLog] = React.useState<string[]>([]);
  const items = React.useMemo(() => [branch("unknown-count", "Unknown count branch", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      GetChildrenCount: () => 0,
      GetTreeItems: async (item: DemoNode | null | undefined) => {
        setLoadLog((current) => [...current, item?.id ?? "root"]);
        return [leaf("unknown-count/child", "Child discovered on expand")];
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

    await userEvent.click(canvas.getByText("Unknown count branch"));
    await expect(await canvas.findByText("Child discovered on expand")).toBeVisible();
    await expect(canvas.getByTestId("unknown-count-load-log")).toHaveTextContent("unknown-count");
  },
};

const ParentAwareDataSourceTree = () => {
  const [loadLog, setLoadLog] = React.useState<string[]>([]);
  const items = React.useMemo(() => [branch("remote/arc", "Remote ARC", undefined)], []);

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
            return [branch("remote/arc/studies", "Studies", undefined), leaf("remote/arc/readme", "Readme")];
          case "remote/arc/studies":
            return [leaf("remote/arc/studies/study-1", "Study One")];
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

    await userEvent.click(canvas.getByRole("button", { name: "Expand Remote ARC" }));
    await expect(await canvas.findByText("Readme")).toBeVisible();
    await expect(canvas.getByText("Studies")).toBeVisible();
    await expect(canvas.getByTestId("datasource-load-log")).toHaveTextContent("remote/arc");

    await userEvent.click(canvas.getByRole("button", { name: "Expand Studies" }));
    await expect(await canvas.findByText("Study One")).toBeVisible();
    await expect(canvas.getByTestId("datasource-load-log")).toHaveTextContent("remote/arc|remote/arc/studies");
  },
};

const DataSourceInvalidateAllTree = () => {
  const [loadCount, setLoadCount] = React.useState(0);
  const versionRef = React.useRef(1);
  const apiRef = React.useRef<TreeApi | null>(null);
  const items = React.useMemo(() => [branch("invalidate/all", "Invalidate-all branch", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      GetChildrenCount: () => 1,
      GetTreeItems: async () => {
        const version = versionRef.current;
        setLoadCount((count) => count + 1);
        return [leaf(`invalidate/all/child-${version}`, `Loaded version ${version}`)];
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

    await userEvent.click(canvas.getByRole("button", { name: "Expand Invalidate-all branch" }));
    await expect(await canvas.findByText("Loaded version 1")).toBeVisible();
    await expect(canvas.getByTestId("datasource-invalidate-loads")).toHaveTextContent("Loads: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Collapse Invalidate-all branch" }));
    await userEvent.click(canvas.getByRole("button", { name: "Expand Invalidate-all branch" }));
    await expect(canvas.getByTestId("datasource-invalidate-loads")).toHaveTextContent("Loads: 1");

    await userEvent.click(canvas.getByRole("button", { name: "Invalidate all datasource cache" }));
    await expect(canvas.getByRole("button", { name: "Expand Invalidate-all branch" })).toBeVisible();
    await userEvent.click(canvas.getByRole("button", { name: "Expand Invalidate-all branch" }));
    await expect(await canvas.findByText("Loaded version 2")).toBeVisible();
    await expect(canvas.getByTestId("datasource-invalidate-loads")).toHaveTextContent("Loads: 2");
  },
};

const LazyErrorTree = () => {
  const [errorMessage, setErrorMessage] = React.useState("none");
  const items = React.useMemo(() => [branch("lazy-error", "Broken lazy branch", undefined)], []);

  const dataSource = React.useMemo(
    () => ({
      GetChildrenCount: () => 1,
      GetTreeItems: async () => {
        throw new Error("Lazy load failed");
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

    await userEvent.click(canvas.getByRole("button", { name: "Expand Broken lazy branch" }));
    await expect(await canvas.findByText("Error")).toBeVisible();
    await expect(canvas.getByTestId("lazy-error-message")).toHaveTextContent("Lazy load failed");
  },
};

const VirtualizedTree = () => {
  const items = React.useMemo(
    () =>
      Array.from({ length: 80 }, (_, index) =>
        leaf(`node-${index}`, `Virtual node ${index.toString().padStart(2, "0")}`),
      ),
    [],
  );

  return (
    <div className="swt:w-96">
      <Tree items={items} enableVirtualization estimateNodeHeight={34} debug />
    </div>
  );
};

export const VirtualizedRows: Story = {
  render: () => <VirtualizedTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByRole("tree")).toHaveAttribute("data-tree-root", "true");
    await expect(canvas.getByText("Virtual node 00")).toBeVisible();
    await expect(canvasElement.querySelector("[data-tree-virtualized='true']")).toBeTruthy();
  },
};

const ContextMenuTree = () => {
  const [lastAction, setLastAction] = React.useState("none");

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["project", "project/schemas"]}
        contextMenuItems={(node) => [
          menuItem(node ? `Inspect ${node.label}` : "Inspect root", () => setLastAction(node?.id ?? "root")),
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
      fireEvent.contextMenu(canvas.getByTestId("tree-node-project/readme"), { clientX: 20, clientY: 20, bubbles: true });
      expect(screen.getByRole("button", { name: /inspect readme/i })).toBeInTheDocument();
    });
    await userEvent.click(screen.getByRole("button", { name: /inspect readme/i }));
    await expect(canvas.getByTestId("last-action")).toHaveTextContent("project/readme");

    await waitFor(() => {
      fireEvent.contextMenu(canvas.getByRole("tree"), { clientX: 20, clientY: 20, bubbles: true });
      expect(screen.getByRole("button", { name: /inspect root/i })).toBeInTheDocument();
    });
    await userEvent.click(screen.getByRole("button", { name: /inspect root/i }));
    await expect(canvas.getByTestId("last-action")).toHaveTextContent("root");
  },
};

const CustomTree = () => {
  const items = [branch("custom", "Custom branch", [leaf("custom/a", "Alpha", { badge: "A" })])];

  return (
    <div className="swt:w-96">
      <Tree
        items={items}
        defaultExpandedIds={["custom"]}
        leading={(props) => <span className="swt:badge swt:badge-xs">{props.Depth}</span>}
        trailing={(props) =>
          props.Node.data?.badge ? <span className="swt:badge swt:badge-primary swt:badge-sm">{props.Node.data.badge}</span> : null
        }
        renderNode={(props) => <strong>{props.Node.label}</strong>}
        styleFn={(kind, node, classes) => {
          if (!kind) return [...classes, "swt:border", "swt:border-info"];
          if (node?.id === "custom") return [...classes, "swt:text-primary"];
          if (node?.id === "custom/a") return [...classes, "swt:text-accent"];
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
    await expect(canvas.getByText("Alpha")).toBeVisible();
    await expect(canvas.getByText("A")).toBeVisible();
    await expect(canvas.getByTestId("generic-tree")).toHaveClass("swt:border-info");
    await expect(canvas.getByTestId("tree-node-custom")).toHaveClass("swt:text-primary");
    await expect(canvas.getByTestId("tree-node-custom/a")).toHaveClass("swt:text-accent");
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
    branch("memo/root", "Root", [leaf("memo/alpha", "Alpha"), leaf("memo/beta", "Beta"), leaf("memo/gamma", "Gamma")]),
  ]);

  const onRender = React.useCallback(() => {
    countRef.current += 1;

    if (countElementRef.current) {
      countElementRef.current.textContent = `Renders: ${countRef.current}`;
    }
  }, []);

  const renameBeta = React.useCallback(() => {
    setItems((current) =>
      current.map((node) =>
        node.id === "memo/root"
          ? ({
              ...node,
              children: node.children?.map((child) =>
                child.id === "memo/beta" ? ({ ...child, label: "Beta renamed" } as DemoNode) : child,
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
        defaultExpandedIds={["memo/root"]}
        renderNode={(props) => <RenderCounterLabel label={props.Node.label} onRender={onRender} />}
        debug
      />
      <button type="button" className="swt:btn swt:btn-sm" onClick={renameBeta}>
        Rename Beta
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

    await userEvent.click(canvas.getByRole("button", { name: "Rename Beta" }));
    await expect(canvas.getByText("Beta renamed")).toBeVisible();
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
    fireEvent.keyDown(tree.querySelector("[data-tree-node-id='project']")!, { key: "ArrowDown" });
    await expect(canvas.getByTestId("tree-node-project/schemas")).toHaveFocus();

    fireEvent.keyDown(canvas.getByTestId("tree-node-project/schemas"), { key: "ArrowRight" });
    await waitFor(() => expect(canvas.getByText("Person schema")).toBeVisible());
  },
};
