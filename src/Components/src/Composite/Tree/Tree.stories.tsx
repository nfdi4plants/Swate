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
import GeneratedTree from "./Tree.fs.js";
import { nativeOptionValue } from "./Types.fs.js";
import {
  bindTree,
  type TreeApi,
  type TreeDataSource,
  type TreeItem,
  type TreeProps,
  type TreeRenderProps,
  type TreeSelectionMode,
} from "./TreePublicApi";

const Tree = bindTree(GeneratedTree, nativeOptionValue);

const queryTreeItem = (element: HTMLElement, id: string) =>
  element.querySelector<HTMLElement>(`[data-tree-node-id="${CSS.escape(id)}"]`);
const getTreeItem = (element: HTMLElement, id: string) => {
  const row = queryTreeItem(element, id);
  if (!row) throw new Error(`Tree item ${id} is not mounted`);
  return row;
};

type DemoPayload = {
  badge?: string;
};

type DemoNode = TreeItem<DemoPayload>;
type DemoTreeProps = TreeProps<DemoPayload>;
type DemoDataSource = TreeDataSource<DemoPayload>;
type DemoRenderProps = TreeRenderProps<DemoPayload>;
type DemoSelectionMode = TreeSelectionMode;

const branch = (
  id: string,
  label: string,
  children?: DemoNode[],
  payload?: DemoPayload,
): DemoNode => ({
  type: "branch",
  props: { id, label, data: payload },
  ...(children !== undefined ? { children } : {}),
});

const leaf = (id: string, label: string, payload?: DemoPayload): DemoNode => ({
  type: "leaf",
  props: { id, label, data: payload },
});

type Deferred<T> = {
  promise: Promise<T>;
  resolve: (value: T) => void;
  reject: (reason: unknown) => void;
};

const createDeferred = <T,>(): Deferred<T> => {
  let resolve!: (value: T) => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise;
    reject = rejectPromise;
  });
  return { promise, resolve, reject };
};

const expectLoadingIndicator = async (canvasElement: HTMLElement) => {
  await waitFor(() =>
    expect(canvasElement.querySelector(".swt\\:loading")).toBeTruthy(),
  );
};

const baseItems: DemoNode[] = [
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
];

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
    await expect(getTreeItem(canvasElement, "arc")).toHaveAttribute(
      "aria-posinset",
      "1",
    );
    await expect(getTreeItem(canvasElement, "arc")).toHaveAttribute(
      "aria-setsize",
      "1",
    );
    await expect(getTreeItem(canvasElement, "arc/studies")).toHaveAttribute(
      "aria-posinset",
      "1",
    );
    await expect(getTreeItem(canvasElement, "arc/studies")).toHaveAttribute(
      "aria-setsize",
      "3",
    );
    await expect(getTreeItem(canvasElement, "arc/assays")).toHaveAttribute(
      "aria-posinset",
      "2",
    );
    await expect(
      getTreeItem(canvasElement, "arc/isa.investigation.xlsx"),
    ).toHaveAttribute("aria-posinset", "3");
  },
};

export const SingleSelection: Story = {
  render: () => <BasicTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("isa.study.xlsx"));

    await expect(
      getTreeItem(canvasElement, "arc/studies/study_01/isa.study.xlsx"),
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
      />
      <div data-testid="folder-selection">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const SelectingAFolderDoesNotToggleExpansion: Story = {
  render: () => <FolderSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const studiesNode = getTreeItem(canvasElement, "arc/studies");

    await expect(studiesNode).toHaveAttribute("aria-selected", "false");
    await expect(studiesNode).toHaveAttribute("aria-expanded", "false");

    await userEvent.click(studiesNode);
    await expect(studiesNode).toHaveAttribute("aria-expanded", "false");
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

    await userEvent.click(canvas.getByText("studies"));
    await expect(studiesNode).toHaveAttribute("aria-expanded", "true");
    await expect(canvas.getByText("Study 01")).toBeVisible();
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
    const studiesNode = getTreeItem(canvasElement, "arc/studies");

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

export const ChevronKeepsTreeitemFocusForKeyboardNavigation: Story = {
  render: () => <FolderSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const studiesNode = getTreeItem(canvasElement, "arc/studies");

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand studies" }),
    );
    await expect(studiesNode).toHaveFocus();

    await userEvent.keyboard("{ArrowDown}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "arc/studies/study_01")).toHaveFocus(),
    );
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

    await expect(getTreeItem(canvasElement, "alpha.txt")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    await expect(getTreeItem(canvasElement, "beta")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(getTreeItem(canvasElement, "gamma.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(getTreeItem(canvasElement, "delta")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(getTreeItem(canvasElement, "epsilon.txt")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    expect(canvas.getByTestId("select-until-selection").textContent).toBe(
      JSON.stringify(["beta", "gamma.txt", "delta"]),
    );
  },
};

export const NavigationDoesNotReplaceTheRangeAnchor: Story = {
  render: () => <SelectUntilTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("beta"));
    await userEvent.keyboard("{ArrowDown}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "gamma.txt")).toHaveFocus(),
    );

    fireEvent.click(canvas.getByText("delta"), { shiftKey: true });
    await expect(
      canvas.getByTestId("select-until-selection"),
    ).toHaveTextContent(JSON.stringify(["beta", "gamma.txt", "delta"]));
  },
};

export const KeyboardRangeSelectionUsesTheOriginalAnchor: Story = {
  render: () => <SelectUntilTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("beta"));
    await userEvent.keyboard("{ArrowDown}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "gamma.txt")).toHaveFocus(),
    );

    await userEvent.keyboard("{ArrowDown}");
    const deltaNode = getTreeItem(canvasElement, "delta");
    await waitFor(() => expect(deltaNode).toHaveFocus());
    fireEvent.keyDown(deltaNode, { key: " ", code: "Space", shiftKey: true });

    await expect(
      canvas.getByTestId("select-until-selection"),
    ).toHaveTextContent(JSON.stringify(["beta", "gamma.txt", "delta"]));
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

    expect(getTreeItem(canvasElement, "arc/studies").className).toContain(
      "swt:cursor-pointer",
    );
    expect(getTreeItem(canvasElement, "arc/studies").className).toContain(
      "swt:hover:bg-base-200",
    );
  },
};

export const MultipleSelectionAllowsBranchSelection: Story = {
  render: () => <MultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("studies"));
    await expect(getTreeItem(canvasElement, "arc/studies")).toHaveAttribute(
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
        "arc/studies/study_01/isa.study.xlsx",
        "arc/assays/assay_01/isa.assay.xlsx",
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
      getTreeItem(canvasElement, "arc/assays/assay_01/isa.assay.xlsx"),
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
    const activeNode = getTreeItem(
      canvasElement,
      "arc/studies/study_01/isa.study.xlsx",
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
    const branchNode = getTreeItem(canvasElement, "folder");
    const leafNode = getTreeItem(canvasElement, "folder/file.txt");

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
      />
      <button
        type="button"
        className="swt:btn swt:btn-sm"
        onClick={() => setSelectionMode("single")}
      >
        Use single selection
      </button>
      <button type="button" onClick={() => setSelectionMode("multiple")}>
        Use multiple selection
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

    await expect(getTreeItem(canvasElement, "alpha.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(getTreeItem(canvasElement, "beta.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Use single selection" }),
    );
    await expect(getTreeItem(canvasElement, "alpha.txt")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    await expect(getTreeItem(canvasElement, "beta.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(canvas.getByTestId("parent-selected-ids").textContent).toBe(
      JSON.stringify(["alpha.txt", "beta.txt"]),
    );
    expect(canvas.getByTestId("selection-change-count").textContent).toBe("0");
    await userEvent.click(
      canvas.getByRole("button", { name: "Use multiple selection" }),
    );
    await expect(getTreeItem(canvasElement, "alpha.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(getTreeItem(canvasElement, "beta.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(canvas.getByTestId("selection-change-count").textContent).toBe("0");
  },
};

const UncontrolledMultiSelectionTree = () => {
  const [reportedSelection, setReportedSelection] = React.useState<string[]>(
    [],
  );
  const items = React.useMemo(
    () => [leaf("alpha.txt", "alpha.txt"), leaf("beta.txt", "beta.txt")],
    [],
  );

  return (
    <div className="swt:w-96">
      <Tree
        items={items}
        selectionMode="multiple"
        onSelectionChange={setReportedSelection}
      />
      <output data-testid="tree-selected-ids">
        {reportedSelection.join(",")}
      </output>
    </div>
  );
};

export const UncontrolledMultiSelectionUsesLatestState: Story = {
  render: () => <UncontrolledMultiSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByText("alpha.txt"));
    fireEvent.click(canvas.getByText("beta.txt"), { ctrlKey: true });

    await expect(getTreeItem(canvasElement, "alpha.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(getTreeItem(canvasElement, "beta.txt")).toHaveAttribute(
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

const UncontrolledSelectionModeTree = () => {
  const [selectionMode, setSelectionMode] =
    React.useState<TreeSelectionMode>("multiple");
  const items = React.useMemo(
    () => [leaf("alpha.txt", "alpha.txt"), leaf("beta.txt", "beta.txt")],
    [],
  );
  const [changeCount, setChangeCount] = React.useState(0);
  const [reportedSelection, setReportedSelection] = React.useState([
    "alpha.txt",
    "beta.txt",
  ]);

  return (
    <div>
      <Tree
        items={items}
        selectionMode={selectionMode}
        defaultSelectedIds={["alpha.txt", "beta.txt"]}
        onSelectionChange={(ids) => {
          setChangeCount((count) => count + 1);
          setReportedSelection(ids);
        }}
      />
      <button type="button" onClick={() => setSelectionMode("single")}>
        Uncontrolled single
      </button>
      <button type="button" onClick={() => setSelectionMode("multiple")}>
        Uncontrolled multiple
      </button>
      <output data-testid="uncontrolled-change-count">{changeCount}</output>
      <output data-testid="tree-selected-ids">
        {reportedSelection.join(",")}
      </output>
    </div>
  );
};

export const UncontrolledModeChangeCleansHiddenSelections: Story = {
  render: () => <UncontrolledSelectionModeTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(getTreeItem(canvasElement, "beta.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Uncontrolled single" }),
    );
    await expect(getTreeItem(canvasElement, "beta.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Uncontrolled multiple" }),
    );
    await expect(getTreeItem(canvasElement, "beta.txt")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(canvas.getByTestId("tree-selected-ids")).toHaveTextContent(
      "beta.txt",
    );
    await expect(getTreeItem(canvasElement, "alpha.txt")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    await expect(
      canvas.getByTestId("uncontrolled-change-count"),
    ).toHaveTextContent("1");
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
      />
      <div data-testid="disabled-selected">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const DisabledSelection: Story = {
  render: () => <DisabledSelectionTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const branchNode = getTreeItem(canvasElement, "folder");

    await expect(branchNode).not.toHaveAttribute("aria-selected");
    await expect(branchNode).toHaveAttribute("aria-expanded", "false");

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand folder" }),
    );
    await expect(branchNode).toHaveAttribute("aria-expanded", "true");
    await expect(canvas.getByText("file.txt")).toBeVisible();

    const leafNode = getTreeItem(canvasElement, "folder/file.txt");
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
  const apiRef = React.useRef<TreeApi | null>(null);

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
      <Tree items={items} dataSource={dataSource} ref={apiRef} />
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
    await expect(await canvas.findByText("Study load 2")).toBeVisible();
    await expect(canvas.queryByText("Study load 1")).not.toBeInTheDocument();
    expect(canvas.getByTestId("load-count").textContent).toBe("Loads: 2");
  },
};

const NestedLazyInvalidationTree = () => {
  const apiRef = React.useRef<TreeApi | null>(null);
  const loadCountsRef = React.useRef({ parent: 0, child: 0 });
  const [loadCounts, setLoadCounts] = React.useState({ parent: 0, child: 0 });
  const items = React.useMemo(() => [branch("parent", "parent")], []);

  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item) => {
        if (item?.props.id === "parent") {
          loadCountsRef.current.parent += 1;
          setLoadCounts({ ...loadCountsRef.current });
          return [branch("parent/child", "child")];
        }

        if (item?.props.id === "parent/child") {
          loadCountsRef.current.child += 1;
          setLoadCounts({ ...loadCountsRef.current });
          return [leaf("parent/child/result.txt", "nested result")];
        }

        return [];
      },
    }),
    [],
  );

  return (
    <div className="swt:w-96 swt:space-y-2">
      <Tree items={items} dataSource={dataSource} ref={apiRef} />
      <button
        type="button"
        onClick={() => apiRef.current?.invalidateNode("parent")}
      >
        Invalidate parent subtree
      </button>
      <output data-testid="nested-load-counts">
        Parent: {loadCounts.parent}; Child: {loadCounts.child}
      </output>
    </div>
  );
};

export const InvalidateNodeClearsDescendantCacheAndExpansion: Story = {
  render: () => <NestedLazyInvalidationTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(
      canvas.getByRole("button", { name: "Expand parent" }),
    );
    await userEvent.click(
      await canvas.findByRole("button", { name: "Expand child" }),
    );
    await expect(await canvas.findByText("nested result")).toBeVisible();

    await userEvent.click(
      canvas.getByRole("button", { name: "Invalidate parent subtree" }),
    );

    await expect(
      await canvas.findByRole("button", { name: "Expand child" }),
    ).toBeVisible();
    await expect(canvas.queryByText("nested result")).not.toBeInTheDocument();
    await expect(canvas.getByTestId("nested-load-counts")).toHaveTextContent(
      "Parent: 2; Child: 1",
    );

    await userEvent.click(canvas.getByRole("button", { name: "Expand child" }));
    await expect(await canvas.findByText("nested result")).toBeVisible();
    await expect(canvas.getByTestId("nested-load-counts")).toHaveTextContent(
      "Parent: 2; Child: 2",
    );
  },
};

const PendingRequestInvalidationTree = () => {
  const apiRef = React.useRef<TreeApi | null>(null);
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
      <Tree items={items} dataSource={dataSource} ref={apiRef} />
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
    await expectLoadingIndicator(canvasElement);
    await expect(canvas.getByTestId("pending-request-count")).toHaveTextContent(
      "Requests: 2",
    );

    await userEvent.click(
      canvas.getByRole("button", { name: "Resolve latest load" }),
    );
    await expect(await canvas.findByText("Result 2")).toBeVisible();
  },
};

const StaleFailureTree = () => {
  const apiRef = React.useRef<TreeApi | null>(null);
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
        ref={apiRef}
        onError={() => setErrorCount((count) => count + 1)}
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
      <Tree items={items} dataSource={dataSource} />
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

const RootDataSourceTree = () => {
  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item) =>
        item === undefined ? [leaf("root-file.txt", "root file")] : [],
    }),
    [],
  );

  return <Tree items={[]} dataSource={dataSource} />;
};

export const DataSourceCanLoadRootItems: Story = {
  render: () => <RootDataSourceTree />,
  play: async ({ canvasElement }) => {
    await expect(
      await within(canvasElement).findByText("root file"),
    ).toBeVisible();
  },
};

const ReplacedDataSourceTree = () => {
  const [version, setVersion] = React.useState(1);
  const items = React.useMemo(() => [branch("remote", "remote")], []);
  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item) =>
        item?.props.id === "remote"
          ? [leaf(`remote/v${version}.txt`, `version ${version}`)]
          : [],
    }),
    [version],
  );

  return (
    <div>
      <Tree
        items={items}
        dataSource={dataSource}
        defaultExpandedIds={["remote"]}
      />
      <button type="button" onClick={() => setVersion(2)}>
        Replace datasource
      </button>
    </div>
  );
};

export const ReplacingDataSourceClearsItsCache: Story = {
  render: () => <ReplacedDataSourceTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(await canvas.findByText("version 1")).toBeVisible();

    await userEvent.click(
      canvas.getByRole("button", { name: "Replace datasource" }),
    );
    await expect(await canvas.findByText("version 2")).toBeVisible();
    await expect(canvas.queryByText("version 1")).not.toBeInTheDocument();
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
      />
      <div data-testid="empty-folder-selection">{JSON.stringify(selected)}</div>
    </div>
  );
};

export const EmptyFolderRemainsSelectableAndExpandable: Story = {
  render: () => <EmptyFolderTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const emptyFolder = getTreeItem(canvasElement, "empty-folder");

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
  const apiRef = React.useRef<TreeApi | null>(null);
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
      <Tree items={items} dataSource={dataSource} ref={apiRef} />
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
      <Tree items={items} dataSource={dataSource} onError={onError} />
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
    await expect(queryTreeItem(canvasElement, "arc")).not.toBeInTheDocument();
  },
};

export const VirtualizationKeepsTheFocusedRowMounted: Story = {
  render: () => <VirtualizedTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const firstStudy = getTreeItem(canvasElement, "arc/studies/study_01");

    await userEvent.click(firstStudy);
    await expect(firstStudy).toHaveFocus();
    await scrollVirtualizedTreeToBottom(canvasElement);

    await expect(
      getTreeItem(canvasElement, "arc/studies/study_01"),
    ).toHaveFocus();
    await userEvent.keyboard("{ArrowDown}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "arc/studies/study_02")).toHaveFocus(),
    );
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
    await expect(getTreeItem(canvasElement, "arc")).toHaveFocus();

    await userEvent.keyboard("{End}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "arc/docs/changelog.md")).toHaveFocus(),
    );

    await userEvent.keyboard("{Home}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "arc")).toHaveFocus(),
    );
  },
};

const ContextMenuTree = () => {
  const [lastAction, setLastAction] = React.useState("none");
  const [validEvents, setValidEvents] = React.useState(false);

  return (
    <div className="swt:w-96">
      <Tree
        items={baseItems}
        defaultExpandedIds={["arc", "arc/studies", "arc/studies/study_01"]}
        onContextMenu={(_event, nodeOption) => {
          const node = nodeOption;
          return [
            {
              text: <span>Inspect {node?.props.label ?? "tree root"}</span>,
              onClick: ({ buttonEvent, spawnData }) => {
                setLastAction(spawnData.item?.props.id ?? "root");
                setValidEvents(
                  buttonEvent.nativeEvent instanceof MouseEvent &&
                    spawnData.event instanceof MouseEvent,
                );
              },
            },
          ];
        }}
      />
      <div data-testid="last-action">Last action: {lastAction}</div>
      <output data-testid="context-events-valid">{String(validEvents)}</output>
    </div>
  );
};

export const NodeAndRootContextMenu: Story = {
  render: () => <ContextMenuTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    fireEvent.contextMenu(
      getTreeItem(canvasElement, "arc/studies/study_01/isa.study.xlsx"),
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
    await expect(canvas.getByTestId("context-events-valid")).toHaveTextContent(
      "true",
    );
  },
};

const AppearanceTree = () => {
  const items = React.useMemo<DemoNode[]>(
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
      },
    ],
    [],
  );

  return (
    <div className="swt:w-96">
      <Tree items={items} />
    </div>
  );
};

export const CustomIconAndTooltip: Story = {
  render: () => <AppearanceTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByTestId("custom-tree-icon")).toBeVisible();
    await expect(
      getTreeItem(canvasElement, "arc/featured.xlsx"),
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
        const payload = props.node.props.data;
        return payload?.badge ? (
          <span data-testid="custom-trailing">{payload.badge}</span>
        ) : (
          <></>
        );
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
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(within(canvasElement).getByRole("tree")).toHaveClass(
      "swt:border-info",
    );
  },
};

export const CustomBranchStyling: Story = {
  render: () => (
    <Tree
      items={customItems}
      styleFn={(nodeOption, classes) => {
        const node = nodeOption;
        return node?.type === "branch"
          ? [...classes, "swt:text-primary"]
          : classes;
      }}
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(
      getTreeItem(canvasElement, "arc/studies/study_04"),
    ).toHaveClass("swt:text-primary");
  },
};

export const CustomLeafStyling: Story = {
  render: () => (
    <Tree
      items={customItems}
      defaultExpandedIds={customExpandedIds}
      styleFn={(nodeOption, classes) => {
        const node = nodeOption;
        return node?.type === "leaf"
          ? [...classes, "swt:text-accent"]
          : classes;
      }}
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(
      getTreeItem(canvasElement, "arc/studies/study_04/isa.study.xlsx"),
    ).toHaveClass("swt:text-accent");
  },
};

const CustomSelectTree = ({
  mode = "native",
}: {
  mode?: "native" | "synthetic" | "async";
}) => {
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
              onClick={async (event) => {
                if (mode === "async") await Promise.resolve();
                props.select(mode === "native" ? event.nativeEvent : event);
              }}
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
    await expect(
      getTreeItem(canvasElement, "arc/studies/study_04/isa.study.xlsx"),
    ).toHaveFocus();
    await userEvent.keyboard("{ArrowUp}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "arc/studies/study_04")).toHaveFocus(),
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
          ? {
              ...node,
              children: Array.from(node.children ?? []).map((child) =>
                child.props.id === "arc/assays/assay_05/datamap.tsv"
                  ? {
                      ...child,
                      props: { ...child.props, label: draftLabel },
                    }
                  : child,
              ),
            }
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
      branch("workspace/projects", "projects", [
        branch("workspace/projects/project-a", "Project A", [
          leaf("workspace/projects/project-a/alpha.txt", "alpha.txt"),
          leaf("workspace/projects/project-a/beta.txt", "beta.txt"),
        ]),
      ]),
    ]),
    leaf("stable-one.txt", "stable-one.txt"),
    leaf("stable-two.txt", "stable-two.txt"),
  ]);
  const [selected, setSelected] = React.useState<string[]>([]);
  const [renderCounts, setRenderCounts] = React.useState<
    Record<string, number>
  >({});
  const expandedIds = React.useMemo(
    () => ["workspace", "workspace/projects", "workspace/projects/project-a"],
    [],
  );

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
          ? {
              ...node,
              children: (node.children ?? []).map((projects) =>
                projects.type === "branch"
                  ? {
                      ...projects,
                      children: (projects.children ?? []).map((project) =>
                        project.type === "branch"
                          ? {
                              ...project,
                              children: (project.children ?? []).map((file) =>
                                file.props.id ===
                                "workspace/projects/project-a/beta.txt"
                                  ? {
                                      ...file,
                                      props: {
                                        ...file.props,
                                        label: "beta-renamed.txt",
                                      },
                                    }
                                  : file,
                              ),
                            }
                          : project,
                      ),
                    }
                  : projects,
              ),
            }
          : node,
      ),
    );
  }, []);

  const addGamma = React.useCallback(() => {
    setItems((current) =>
      current.map((node) =>
        node.type === "branch" && node.props.id === "workspace"
          ? {
              ...node,
              children: (node.children ?? []).map((projects) =>
                projects.type === "branch"
                  ? {
                      ...projects,
                      children: (projects.children ?? []).map((project) =>
                        project.type === "branch"
                          ? {
                              ...project,
                              children: [
                                ...(project.children ?? []),
                                leaf(
                                  "workspace/projects/project-a/gamma.txt",
                                  "gamma.txt",
                                ),
                              ],
                            }
                          : project,
                      ),
                    }
                  : projects,
              ),
            }
          : node,
      ),
    );
  }, []);

  const trackedNodeIds = [
    "workspace",
    "workspace/projects",
    "workspace/projects/project-a",
    "workspace/projects/project-a/alpha.txt",
    "workspace/projects/project-a/beta.txt",
    "workspace/projects/project-a/gamma.txt",
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
      />
      <div className="swt:flex swt:gap-2">
        <button
          type="button"
          className="swt:btn swt:btn-sm"
          onClick={() => setSelected(["workspace/projects/project-a/beta.txt"])}
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
  "workspace/projects",
  "workspace/projects/project-a",
  "workspace/projects/project-a/alpha.txt",
  "workspace/projects/project-a/beta.txt",
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
      projects: renderCount("workspace/projects"),
      project: renderCount("workspace/projects/project-a"),
      alpha: renderCount("workspace/projects/project-a/alpha.txt"),
      beta: renderCount("workspace/projects/project-a/beta.txt"),
      stableOne: renderCount("stable-one.txt"),
      stableTwo: renderCount("stable-two.txt"),
    };

    await userEvent.click(canvas.getByRole("button", { name: "Select beta" }));
    await waitFor(() =>
      expect(
        renderCount("workspace/projects/project-a/beta.txt"),
      ).toBeGreaterThan(beforeSelection.beta),
    );
    await expect(
      getTreeItem(canvasElement, "workspace/projects/project-a/beta.txt"),
    ).toHaveAttribute("aria-selected", "true");
    expect(renderCount("workspace")).toBeGreaterThan(beforeSelection.workspace);
    expect(renderCount("workspace/projects")).toBe(beforeSelection.projects);
    expect(renderCount("workspace/projects/project-a")).toBe(
      beforeSelection.project,
    );
    expect(renderCount("workspace/projects/project-a/alpha.txt")).toBe(
      beforeSelection.alpha,
    );
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
      projects: renderCount("workspace/projects"),
      project: renderCount("workspace/projects/project-a"),
      alpha: renderCount("workspace/projects/project-a/alpha.txt"),
      beta: renderCount("workspace/projects/project-a/beta.txt"),
      stableOne: renderCount("stable-one.txt"),
      stableTwo: renderCount("stable-two.txt"),
    };

    await userEvent.click(canvas.getByRole("button", { name: "Rename beta" }));
    await waitFor(() =>
      expect(
        renderCount("workspace/projects/project-a/beta.txt"),
      ).toBeGreaterThan(beforeRename.beta),
    );
    await expect(canvas.getByText("beta-renamed.txt")).toBeVisible();
    expect(renderCount("workspace/projects/project-a")).toBeGreaterThan(
      beforeRename.project,
    );
    expect(renderCount("workspace")).toBe(beforeRename.workspace);
    expect(renderCount("workspace/projects")).toBe(beforeRename.projects);
    expect(renderCount("workspace/projects/project-a/alpha.txt")).toBe(
      beforeRename.alpha,
    );
    expect(renderCount("stable-one.txt")).toBe(beforeRename.stableOne);
    expect(renderCount("stable-two.txt")).toBe(beforeRename.stableTwo);

    const beforeAdd = {
      workspace: renderCount("workspace"),
      projects: renderCount("workspace/projects"),
      project: renderCount("workspace/projects/project-a"),
      alpha: renderCount("workspace/projects/project-a/alpha.txt"),
      beta: renderCount("workspace/projects/project-a/beta.txt"),
      stableOne: renderCount("stable-one.txt"),
      stableTwo: renderCount("stable-two.txt"),
    };

    await userEvent.click(canvas.getByRole("button", { name: "Add gamma" }));
    await waitFor(() =>
      expect(
        renderCount("workspace/projects/project-a/gamma.txt"),
      ).toBeGreaterThan(0),
    );
    await expect(canvas.getByText("gamma.txt")).toBeVisible();
    expect(renderCount("workspace/projects/project-a")).toBeGreaterThan(
      beforeAdd.project,
    );
    expect(renderCount("workspace")).toBe(beforeAdd.workspace);
    expect(renderCount("workspace/projects")).toBe(beforeAdd.projects);
    expect(renderCount("workspace/projects/project-a/alpha.txt")).toBe(
      beforeAdd.alpha,
    );
    expect(renderCount("workspace/projects/project-a/beta.txt")).toBe(
      beforeAdd.beta,
    );
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
      <Tree items={items} dataSource={dataSource} />
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

    const branchB = getTreeItem(canvasElement, "branch-b");
    branchB.focus();
    await expect(branchB).toHaveFocus();

    await userEvent.click(
      canvas.getByRole("button", { name: "Resolve lazy child" }),
    );
    await expect(await canvas.findByText("Lazy child")).toBeVisible();
    await expect(branchB).toHaveFocus();

    await userEvent.keyboard("{ArrowUp}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "lazy-a/child.txt")).toHaveFocus(),
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
    await expect(getTreeItem(canvasElement, "arc")).toHaveFocus();

    await userEvent.keyboard("{ArrowDown}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "arc/studies")).toHaveFocus(),
    );

    await userEvent.keyboard("{ArrowRight}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "arc/studies/study_01")).toHaveFocus(),
    );

    await userEvent.keyboard("{ArrowRight}");
    await waitFor(() =>
      expect(
        getTreeItem(canvasElement, "arc/studies/study_01/isa.study.xlsx"),
      ).toHaveFocus(),
    );

    await userEvent.keyboard("{ArrowLeft}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "arc/studies/study_01")).toHaveFocus(),
    );

    await userEvent.keyboard("{Enter}");
    await waitFor(() =>
      expect(canvas.queryByText("isa.study.xlsx")).not.toBeInTheDocument(),
    );
    await expect(canvas.getByTestId("selected-node")).toHaveTextContent(
      "arc/studies/study_01",
    );
    await expect(
      getTreeItem(canvasElement, "arc/studies/study_01"),
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

const AnchorEdgeTree = ({
  hidden = false,
  controlled = false,
}: {
  hidden?: boolean;
  controlled?: boolean;
}) => {
  const items = React.useMemo(
    () =>
      hidden
        ? [
            branch("folder", "folder", [leaf("folder/a", "nested a")]),
            leaf("x", "x"),
            leaf("y", "y"),
            leaf("z", "z"),
          ]
        : [
            leaf("alpha", "alpha"),
            leaf("beta", "beta"),
            leaf("gamma", "gamma"),
            leaf("delta", "delta"),
          ],
    [hidden],
  );
  const [selected, setSelected] = React.useState<string[]>([]);
  return (
    <div>
      <Tree
        items={items}
        selectionMode="multiple"
        defaultSelectedIds={controlled || hidden ? [] : ["beta"]}
        defaultExpandedIds={["folder"]}
        selectedIds={controlled ? selected : undefined}
        onSelectionChange={setSelected}
      />
      <output data-testid="tree-selected-ids">{selected.join(",")}</output>
      <button type="button" onClick={() => setSelected([])}>
        Clear parent selection
      </button>
    </div>
  );
};

export const DefaultSelectionRemainsTheRangeAnchor: Story = {
  render: () => <AnchorEdgeTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    fireEvent.click(canvas.getByText("delta"), { shiftKey: true });
    await expect(canvas.getByTestId("tree-selected-ids")).toHaveTextContent(
      "beta,gamma,delta",
    );
    fireEvent.click(canvas.getByText("alpha"), { shiftKey: true });
    expect(canvas.getByTestId("tree-selected-ids").textContent).toBe(
      "alpha,beta",
    );
  },
};

export const HiddenRangeAnchorIsReplaced: Story = {
  render: () => <AnchorEdgeTree hidden />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByText("nested a"));
    await userEvent.click(
      canvas.getByRole("button", { name: "Collapse folder" }),
    );
    fireEvent.click(canvas.getByText("x"), { shiftKey: true });
    expect(canvas.getByTestId("tree-selected-ids").textContent).toBe("x");
    fireEvent.click(canvas.getByText("z"), { shiftKey: true });
    expect(canvas.getByTestId("tree-selected-ids").textContent).toBe("x,y,z");
  },
};

export const ClearingControlledSelectionClearsTheAnchor: Story = {
  render: () => <AnchorEdgeTree controlled />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByText("alpha"));
    await userEvent.click(
      canvas.getByRole("button", { name: "Clear parent selection" }),
    );
    fireEvent.click(canvas.getByText("gamma"), { shiftKey: true });
    expect(canvas.getByTestId("tree-selected-ids").textContent).toBe("gamma");
  },
};

const RendererFreshnessTree = () => {
  const items = React.useMemo(
    () => [leaf("alpha", "alpha"), leaf("beta", "beta")],
    [],
  );
  const [version, setVersion] = React.useState(0);
  return (
    <div>
      <Tree
        items={items}
        renderNode={(props) => (
          <span>
            {props.node.props.label} version {version}
          </span>
        )}
        leading={(props) => (
          <span>
            {props.node.props.id} leading {version}
          </span>
        )}
        trailing={(props) => (
          <span>
            {props.node.props.id} trailing {version}
          </span>
        )}
      />
      <button type="button" onClick={() => setVersion((value) => value + 1)}>
        Change renderer output
      </button>
    </div>
  );
};

export const InlineRenderersRefreshCapturedState: Story = {
  render: () => <RendererFreshnessTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(
      canvas.getByRole("button", { name: "Change renderer output" }),
    );
    for (const id of ["alpha", "beta"]) {
      await expect(canvas.getByText(`${id} version 1`)).toBeVisible();
      await expect(canvas.getByText(`${id} leading 1`)).toBeVisible();
      await expect(canvas.getByText(`${id} trailing 1`)).toBeVisible();
    }
  },
};

export const InteractiveAncestorsDoNotBlockRowSelection: Story = {
  render: () => (
    <div role="button">
      <Tree
        items={[branch("folder", "folder", [leaf("folder/file", "file")])]}
        defaultExpandedIds={["folder"]}
      />
    </div>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByText("file"));
    await expect(getTreeItem(canvasElement, "folder/file")).toHaveAttribute(
      "aria-selected",
      "true",
    );
  },
};

type RemovalMode = "items" | "node" | "all" | "source" | "root";

const FocusRemovalTree = ({
  mode,
  moveFocusOutside = false,
}: {
  mode: RemovalMode;
  moveFocusOutside?: boolean;
}) => {
  const apiRef = React.useRef<TreeApi | null>(null);
  const [generation, setGeneration] = React.useState(0);
  const initialItems = React.useMemo(
    () => (mode === "root" ? [] : [branch("folder", "folder")]),
    [mode],
  );
  const items = React.useMemo(
    () =>
      mode === "items" && generation > 0
        ? [leaf("fallback", "fallback"), leaf("next", "next")]
        : mode === "items"
          ? [
              branch("folder", "folder", [
                branch("nested", "nested", [leaf("file", "file")]),
              ]),
              leaf("next", "next"),
            ]
          : initialItems,
    [mode, generation, initialItems],
  );
  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item) =>
        !item
          ? [branch("folder", "folder")]
          : item.props.id === "folder"
            ? [branch("nested", "nested", [leaf("file", `file ${generation}`)])]
            : [],
    }),
    [generation],
  );
  const refresh = () => {
    if (mode === "node") apiRef.current?.invalidateNode("folder");
    else if (mode === "all" || mode === "root") apiRef.current?.invalidateAll();
    else setGeneration((value) => value + 1);
  };
  return (
    <div>
      <Tree
        items={items}
        dataSource={mode === "items" ? undefined : dataSource}
        defaultExpandedIds={["folder", "nested"]}
        ref={apiRef}
      />
      <button
        type="button"
        onMouseDown={(event) => {
          if (!moveFocusOutside) event.preventDefault();
        }}
        onClick={refresh}
      >
        Refresh focused tree
      </button>
    </div>
  );
};

const checkRemovalFocus = async (
  canvasElement: HTMLElement,
  fallback: string,
) => {
  const canvas = within(canvasElement);
  await userEvent.click(
    await waitFor(() => getTreeItem(canvasElement, "file")),
  );
  await userEvent.click(
    canvas.getByRole("button", { name: "Refresh focused tree" }),
  );
  await waitFor(() =>
    expect(getTreeItem(canvasElement, fallback)).toHaveFocus(),
  );
  await userEvent.keyboard("{ArrowDown}");
  await waitFor(() =>
    expect(
      getTreeItem(canvasElement, fallback === "fallback" ? "next" : "nested"),
    ).toHaveFocus(),
  );
};

export const ItemsRemovalRestoresTreeFocus: Story = {
  render: () => <FocusRemovalTree mode="items" />,
  play: ({ canvasElement }) => checkRemovalFocus(canvasElement, "fallback"),
};
export const AncestorInvalidationRestoresTreeFocus: Story = {
  render: () => <FocusRemovalTree mode="node" />,
  play: ({ canvasElement }) => checkRemovalFocus(canvasElement, "folder"),
};
export const InvalidateAllRestoresTreeFocus: Story = {
  render: () => <FocusRemovalTree mode="all" />,
  play: ({ canvasElement }) => checkRemovalFocus(canvasElement, "folder"),
};
export const SourceReplacementRestoresTreeFocus: Story = {
  render: () => <FocusRemovalTree mode="source" />,
  play: ({ canvasElement }) => checkRemovalFocus(canvasElement, "folder"),
};
export const LazyRootRefreshRestoresTreeFocus: Story = {
  render: () => <FocusRemovalTree mode="root" />,
  play: ({ canvasElement }) => checkRemovalFocus(canvasElement, "folder"),
};
export const RefreshDoesNotStealOutsideFocus: Story = {
  render: () => <FocusRemovalTree mode="items" moveFocusOutside />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(
      await waitFor(() => getTreeItem(canvasElement, "file")),
    );
    const refresh = canvas.getByRole("button", {
      name: "Refresh focused tree",
    });
    await userEvent.click(refresh);
    await expect(await canvas.findByText("fallback")).toBeVisible();
    await expect(refresh).toHaveFocus();
  },
};

const DefaultExpansionTree = ({ lazyRoot = false }: { lazyRoot?: boolean }) => {
  const apiRef = React.useRef<TreeApi | null>(null);
  const [loads, setLoads] = React.useState(0);
  const items = React.useMemo(
    () => (lazyRoot ? [] : [branch("folder", "folder")]),
    [lazyRoot],
  );
  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: async (item) => {
        setLoads((value) => value + 1);
        return !item
          ? [branch("folder", "folder")]
          : item.props.id === "folder"
            ? [branch("nested", "nested")]
            : [leaf("file", "loaded file")];
      },
    }),
    [],
  );
  return (
    <div>
      <Tree
        items={items}
        dataSource={dataSource}
        defaultExpandedIds={["folder", "nested"]}
        ref={apiRef}
      />
      <button type="button" onClick={() => apiRef.current?.invalidateAll()}>
        Invalidate default expansion
      </button>
      <output data-testid="default-load-count">{loads}</output>
    </div>
  );
};
const checkDefaultExpansion = async (
  canvasElement: HTMLElement,
  expected: number,
) => {
  const canvas = within(canvasElement);
  await expect(await canvas.findByText("loaded file")).toBeVisible();
  await userEvent.click(
    canvas.getByRole("button", { name: "Invalidate default expansion" }),
  );
  await waitFor(() =>
    expect(canvas.getByTestId("default-load-count")).toHaveTextContent(
      String(expected),
    ),
  );
  await expect(await canvas.findByText("loaded file")).toBeVisible();
  await expect(getTreeItem(canvasElement, "folder")).toHaveAttribute(
    "aria-expanded",
    "true",
  );
  await expect(getTreeItem(canvasElement, "nested")).toHaveAttribute(
    "aria-expanded",
    "true",
  );
};
export const InvalidateAllPreservesDefaultLazyExpansion: Story = {
  render: () => <DefaultExpansionTree />,
  play: ({ canvasElement }) => checkDefaultExpansion(canvasElement, 4),
};
export const InvalidateAllPreservesLazyRootExpansion: Story = {
  render: () => <DefaultExpansionTree lazyRoot />,
  play: ({ canvasElement }) => checkDefaultExpansion(canvasElement, 6),
};

const StaticInvalidationTree = () => {
  const apiRef = React.useRef<TreeApi | null>(null);
  const items = React.useMemo(
    () => [branch("folder", "folder", [leaf("file", "static file")])],
    [],
  );
  const firstHandle = React.useRef<TreeApi | null>(null);
  const [stable, setStable] = React.useState(true);
  const check = () => {
    firstHandle.current ??= apiRef.current;
    apiRef.current?.invalidateNode("folder");
    setStable(firstHandle.current === apiRef.current);
  };
  return (
    <div>
      <Tree items={items} defaultExpandedIds={["folder"]} ref={apiRef} />
      <button type="button" onClick={check}>
        Invalidate static folder
      </button>
      <output data-testid="stable-handle">{String(stable)}</output>
    </div>
  );
};
export const StaticInvalidationIsANoop: Story = {
  render: () => <StaticInvalidationTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(
      canvas.getByRole("button", { name: "Invalidate static folder" }),
    );
    await userEvent.click(canvas.getByText("static file"));
    await userEvent.click(
      canvas.getByRole("button", { name: "Invalidate static folder" }),
    );
    await expect(getTreeItem(canvasElement, "folder")).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    await expect(canvas.getByText("static file")).toBeVisible();
    await expect(canvas.getByTestId("stable-handle")).toHaveTextContent("true");
  },
};

const RecreatedSourceTree = ({ inline = false }: { inline?: boolean }) => {
  const items = React.useMemo(() => [branch("folder", "folder")], []);
  const [loads, setLoads] = React.useState(0);
  const [renders, setRenders] = React.useState(0);
  const [selected, setSelected] = React.useState<string[]>([]);
  const getTreeItems = React.useCallback(async (item?: DemoNode) => {
    setLoads((value) => value + 1);
    if (item?.props.id === "folder") return [branch("nested", "nested")];
    return [leaf("file", "cached file")];
  }, []);
  const dataSource = inline
    ? {
        cacheKey: "inline-source",
        getTreeItems: async (item?: DemoNode) => getTreeItems(item),
      }
    : { getTreeItems };
  return (
    <div>
      <Tree
        items={items}
        dataSource={dataSource}
        defaultExpandedIds={["folder", "nested"]}
        selectedIds={selected}
        onSelectionChange={setSelected}
      />
      <button type="button" onClick={() => setRenders((value) => value + 1)}>
        Rerender datasource wrapper
      </button>
      <output data-testid="recreated-loads">{loads}</output>
      <output>{renders}</output>
    </div>
  );
};
const checkRecreatedSource = async (canvasElement: HTMLElement) => {
  const canvas = within(canvasElement);
  await expect(await canvas.findByText("cached file")).toBeVisible();
  await userEvent.click(canvas.getByText("cached file"));
  await userEvent.click(
    canvas.getByRole("button", { name: "Rerender datasource wrapper" }),
  );
  await expect(canvas.getByText("cached file")).toBeVisible();
  expect(canvas.getByTestId("recreated-loads").textContent).toBe("2");
  await expect(getTreeItem(canvasElement, "nested")).toHaveAttribute(
    "aria-expanded",
    "true",
  );
};
export const RecreatedDatasourceWrapperKeepsCache: Story = {
  render: () => <RecreatedSourceTree />,
  play: ({ canvasElement }) => checkRecreatedSource(canvasElement),
};
export const InlineDatasourceWithStableKeyKeepsCache: Story = {
  render: () => <RecreatedSourceTree inline />,
  play: ({ canvasElement }) => checkRecreatedSource(canvasElement),
};

const checkCustomSelectFocus = async (canvasElement: HTMLElement) => {
  const canvas = within(canvasElement);
  await userEvent.click(
    canvas.getByRole("button", { name: "Select rendered leaf" }),
  );
  await waitFor(() =>
    expect(
      getTreeItem(canvasElement, "arc/studies/study_04/isa.study.xlsx"),
    ).toHaveFocus(),
  );
  await userEvent.keyboard("{ArrowUp}");
  await waitFor(() =>
    expect(getTreeItem(canvasElement, "arc/studies/study_04")).toHaveFocus(),
  );
};
export const CustomSelectAcceptsSyntheticEvents: Story = {
  render: () => <CustomSelectTree mode="synthetic" />,
  play: ({ canvasElement }) => checkCustomSelectFocus(canvasElement),
};
export const CustomSelectAfterAwaitRestoresRowFocus: Story = {
  render: () => <CustomSelectTree mode="async" />,
  play: ({ canvasElement }) => checkCustomSelectFocus(canvasElement),
};

const RootItemsReplacementTree = () => {
  const [supplied, setSupplied] = React.useState(false);
  const dataSource = React.useMemo<DemoDataSource>(
    () => ({ getTreeItems: async () => [leaf("cached", "cached root")] }),
    [],
  );
  return (
    <div>
      <Tree
        items={supplied ? [leaf("supplied", "supplied root")] : []}
        dataSource={dataSource}
      />
      <button onClick={() => setSupplied(true)}>Supply real root items</button>
    </div>
  );
};
export const SuppliedRootItemsReplaceTheLazyRoot: Story = {
  render: () => <RootItemsReplacementTree />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(await canvas.findByText("cached root")).toBeVisible();
    await userEvent.click(
      canvas.getByRole("button", { name: "Supply real root items" }),
    );
    await expect(canvas.getByText("supplied root")).toBeVisible();
    expect(canvas.queryByText("cached root")).not.toBeInTheDocument();
  },
};

const RejectionTree = ({
  reason,
  root = false,
}: {
  reason: unknown;
  root?: boolean;
}) => {
  const first = React.useMemo(() => createDeferred<DemoNode[]>(), []);
  const attempts = React.useRef(0);
  const [error, setError] = React.useState<Error>();
  const dataSource = React.useMemo<DemoDataSource>(
    () => ({
      getTreeItems: () =>
        ++attempts.current === 1
          ? first.promise
          : Promise.resolve([leaf("recovered", "recovered file")]),
    }),
    [first],
  );
  return (
    <div>
      <Tree
        items={root ? [] : [branch("folder", "folder")]}
        dataSource={dataSource}
        onError={setError}
      />
      <button onClick={() => first.reject(reason)}>Reject request</button>
      <output data-testid="normalized-error">
        {error instanceof Error ? error.message : "not reported"}
      </output>
    </div>
  );
};
const checkRejection = async (canvasElement: HTMLElement, message: string) => {
  const canvas = within(canvasElement);
  await userEvent.click(canvas.getByRole("button", { name: "Expand folder" }));
  await expectLoadingIndicator(canvasElement);
  await userEvent.click(canvas.getByRole("button", { name: "Reject request" }));
  await expect(canvas.getByTestId("normalized-error")).toHaveTextContent(
    message,
  );
  expect(canvasElement.querySelector(".swt\\:loading")).not.toBeInTheDocument();
  expect(getTreeItem(canvasElement, "folder")).toHaveAttribute(
    "aria-expanded",
    "false",
  );
  await expect(canvas.getByTitle(message)).toBeVisible();
  await userEvent.click(canvas.getByRole("button", { name: "Expand folder" }));
  await expect(await canvas.findByText("recovered file")).toBeVisible();
};
export const UndefinedRejectionsBecomeErrors: Story = {
  render: () => <RejectionTree reason={undefined} />,
  play: ({ canvasElement }) =>
    checkRejection(canvasElement, "Failed to load tree items"),
};
export const NullRejectionsBecomeErrors: Story = {
  render: () => <RejectionTree reason={null} />,
  play: ({ canvasElement }) =>
    checkRejection(canvasElement, "Failed to load tree items"),
};
export const StringRejectionsKeepTheirMessage: Story = {
  render: () => <RejectionTree reason="some string" />,
  play: ({ canvasElement }) => checkRejection(canvasElement, "some string"),
};
export const LazyRootShowsLoadingErrorAndRetry: Story = {
  render: () => <RejectionTree root reason={null} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(
      await within(canvas.getByRole("tree")).findByRole("status"),
    ).toHaveTextContent("Loading tree");
    await expect(canvas.getByRole("tree")).toHaveAttribute("aria-busy", "true");
    await userEvent.click(
      canvas.getByRole("button", { name: "Reject request" }),
    );
    await expect(await canvas.findByRole("alert")).toHaveTextContent(
      "Failed to load tree items",
    );
    await expect(canvas.getByRole("tree")).toHaveAttribute(
      "aria-busy",
      "false",
    );
    await userEvent.click(
      canvas.getByRole("button", { name: "Retry tree loading" }),
    );
    await expect(await canvas.findByText("recovered file")).toBeVisible();
    expect(canvas.queryByRole("alert")).not.toBeInTheDocument();
  },
};

export const VirtualViewportHeightIsCustomizable: Story = {
  render: () => (
    <div className="swt:w-96">
      <style>{".custom-tree-viewport { height: 120px; }"}</style>
      <Tree
        items={Array.from({ length: 200 }, (_, index) =>
          leaf(`item-${index}`, `item ${index}`),
        )}
        enableVirtualization
        viewportClassName="custom-tree-viewport"
      />
    </div>
  ),
  play: async ({ canvasElement }) => {
    const viewport = canvasElement.querySelector<HTMLElement>(
      "[data-tree-virtualized=true]",
    )!;
    expect(viewport.getBoundingClientRect().height).toBe(120);
    await userEvent.click(getTreeItem(canvasElement, "item-0"));
    await userEvent.keyboard("{End}");
    await waitFor(() =>
      expect(getTreeItem(canvasElement, "item-199")).toHaveFocus(),
    );
  },
};

const AncestorSelectionTree = ({
  editable = false,
}: {
  editable?: boolean;
}) => {
  const tree = (
    <Tree items={[branch("folder", "folder", [leaf("file", "file")])]} />
  );
  return editable ? (
    <div contentEditable suppressContentEditableWarning>
      {tree}
    </div>
  ) : (
    <a href="#tree-selection">{tree}</a>
  );
};
const checkAncestorSelection = async (canvasElement: HTMLElement) => {
  await userEvent.click(within(canvasElement).getByText("folder"));
  await expect(getTreeItem(canvasElement, "folder")).toHaveAttribute(
    "aria-selected",
    "true",
  );
  await expect(getTreeItem(canvasElement, "folder")).toHaveAttribute(
    "aria-expanded",
    "false",
  );
};
export const LinkAncestorsDoNotBlockRowSelection: Story = {
  render: () => <AncestorSelectionTree />,
  play: ({ canvasElement }) => checkAncestorSelection(canvasElement),
};

export const SingleModeUsesTheLastControlledOccurrence: Story = {
  render: () => (
    <Tree
      items={[leaf("alpha", "alpha"), leaf("beta", "beta")]}
      selectedIds={["beta", "alpha", "beta"]}
    />
  ),
  play: async ({ canvasElement }) => {
    await expect(getTreeItem(canvasElement, "alpha")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    await expect(getTreeItem(canvasElement, "beta")).toHaveAttribute(
      "aria-selected",
      "true",
    );
  },
};
export const EditableAncestorsDoNotBlockRowSelection: Story = {
  render: () => <AncestorSelectionTree editable />,
  play: ({ canvasElement }) => checkAncestorSelection(canvasElement),
};
export const IndentAndTrailingClicksSelectWithoutToggling: Story = {
  render: () => (
    <Tree
      items={[
        branch("root", "root", [
          {
            type: "branch",
            props: {
              id: "folder",
              label: "folder",
              trailing: <span data-testid="folder-badge">badge</span>,
            },
            children: [leaf("file", "file")],
          },
        ]),
      ]}
      defaultExpandedIds={["root"]}
    />
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const row = getTreeItem(canvasElement, "folder");
    await userEvent.click(row);
    await expect(row).toHaveAttribute("aria-selected", "true");
    await expect(row).toHaveAttribute("aria-expanded", "false");
    await userEvent.click(canvas.getByText("root"));
    await userEvent.click(canvas.getByTestId("folder-badge"));
    await expect(row).toHaveAttribute("aria-selected", "true");
    await expect(row).toHaveAttribute("aria-expanded", "false");
    await userEvent.click(
      canvas.getByRole("button", { name: "Expand folder" }),
    );
    await userEvent.click(canvas.getByTestId("folder-badge"));
    await expect(row).toHaveAttribute("aria-expanded", "true");
    await expect(canvas.getByText("file")).toBeVisible();
  },
};
