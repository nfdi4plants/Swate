import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { screen, within, expect, userEvent, waitFor, fireEvent, fn } from "storybook/test";
import { FileExplorer, FileExplorerExample_Example as FileExplorerExample } from "./FileExplorer.fs.js";
import {
  contextMenuItems as fileExplorerGitLfsContextMenuItems,
  lfsPillAction as fileExplorerGitLfsPillAction,
} from "./FileExplorerGitLfsHelper.fs.js";
import {
  ContextMenuItem,
  type FileItem,
  FileItemIcon_Document$,
  FileItemIcon_Folder,
  FileTree_createFile,
  FileTree_createFolder,
} from "./Types.fs.js";
import { ofArray } from "../../fable_modules/fable-library-ts.5.0.0-alpha.21/List.ts";

const arcCreateItems = [
  { label: "Add Study", path: "studies/NewStudy/isa.study.xlsx" },
  { label: "Add Assay", path: "assays/NewAssay/isa.assay.xlsx" },
  { label: "Add Workflow", path: "workflows/NewWorkflow/isa.workflow.xlsx" },
  { label: "Add Run", path: "runs/NewRun/isa.run.xlsx" },
];

const createStableFile = (name: string, path: string, id: string): FileItem =>
  Object.assign(FileTree_createFile(name, path, FileItemIcon_Document$()), { Id: id });

const createStableFolder = (name: string, path: string, id: string, children?: FileItem[]): FileItem =>
  Object.assign(FileTree_createFolder(name, path, FileItemIcon_Folder()), {
    Id: id,
    Children: children === undefined ? undefined : ofArray(children),
  });

const InMemoryCreateFileExplorer = () => {
  const [pendingPath, setPendingPath] = React.useState<string | null>(null);
  const [savedPaths, setSavedPaths] = React.useState<string[]>([]);
  const rootFolder = React.useMemo(() => FileTree_createFolder("ARC Root", "arc", FileItemIcon_Folder()), []);

  return (
    <div className="swt:p-4 swt:space-y-4">
      <FileExplorer
        initialItems={ofArray([rootFolder])}
        onContextMenu={() =>
          ofArray(
            arcCreateItems.map(
              (item) =>
                new ContextMenuItem(
                  item.label,
                  "swt:fluent--document-add-24-regular",
                  () => setPendingPath(item.path),
                  undefined,
                ),
            ),
          )
        }
      />
      <div data-testid="pending-draft">Pending draft: {pendingPath ?? "none"}</div>
      <div data-testid="saved-count">Saved count: {savedPaths.length}</div>
      <div data-testid="saved-paths">Saved paths: {savedPaths.join(", ") || "none"}</div>
      <button
        type="button"
        disabled={!pendingPath}
        onClick={() => {
          if (pendingPath) {
            setSavedPaths((paths) => [...paths, pendingPath]);
            setPendingPath(null);
          }
        }}
      >
        Save
      </button>
    </div>
  );
};

const LazyLoadDirectoryFileExplorer = () => {
  const [lazyFolderLoaded, setLazyFolderLoaded] = React.useState(false);

  const items = React.useMemo(() => {
    const emptyFolder = createStableFolder("Empty Folder", "arc/empty-folder", "empty-folder", []);
    const lazyChild = createStableFile("Lazy Child.txt", "arc/lazy-folder/Lazy Child.txt", "lazy-child");
    const lazyFolder = createStableFolder(
      "Lazy Folder",
      "arc/lazy-folder",
      "lazy-folder",
      lazyFolderLoaded ? [lazyChild] : undefined,
    );

    return ofArray([emptyFolder, lazyFolder]);
  }, [lazyFolderLoaded]);

  return (
    <div className="swt:p-4">
      <FileExplorer
        initialItems={items}
        onDirectoryArrowToggle={(item, willExpand) => {
          if (item.Id === "lazy-folder" && willExpand) {
            setLazyFolderLoaded(true);
          }
        }}
      />
    </div>
  );
};

const SelectedPathFileExplorer = () => {
  const items = React.useMemo(() => {
    const selectedFile = createStableFile("selected-report.txt", "arc/selected-parent/selected-report.txt", "selected-file");
    const parentFolder = createStableFolder("Selected Parent", "arc/selected-parent", "selected-parent", [selectedFile]);

    return ofArray([parentFolder]);
  }, []);

  return (
    <div className="swt:p-4">
      <FileExplorer initialItems={items} selectedItemId="selected-file" />
    </div>
  );
};

const InitiallyExpandedFileExplorer = () => {
  const items = React.useMemo(() => {
    const child = createStableFile("Initially Visible.txt", "arc/initially-expanded/Initially Visible.txt", "initial-child");
    const folder = Object.assign(
      createStableFolder("Initially Expanded", "arc/initially-expanded", "initial-folder", [child]),
      { IsExpanded: true },
    );

    return ofArray([folder]);
  }, []);

  return (
    <div className="swt:p-4">
      <FileExplorer initialItems={items} />
    </div>
  );
};

const ExpansionRefreshFileExplorer = () => {
  const [lazyFolderLoaded, setLazyFolderLoaded] = React.useState(false);

  const items = React.useMemo(() => {
    const selectedFile = createStableFile("selected-report.txt", "arc/selected-parent/selected-report.txt", "selected-file");
    const selectedParent = createStableFolder("Selected Parent", "arc/selected-parent", "selected-parent", [selectedFile]);
    const lazyChild = createStableFile("Lazy Child.txt", "arc/lazy-folder/Lazy Child.txt", "lazy-child");
    const lazyFolder = createStableFolder(
      "Lazy Folder",
      "arc/lazy-folder",
      "lazy-folder",
      lazyFolderLoaded ? [lazyChild] : undefined,
    );

    return ofArray([selectedParent, lazyFolder]);
  }, [lazyFolderLoaded]);

  return (
    <div className="swt:p-4">
      <FileExplorer
        initialItems={items}
        selectedItemId="selected-file"
        onDirectoryExpansionChange={(item, willExpand) => {
          if (item.Id === "lazy-folder" && willExpand) {
            setLazyFolderLoaded(true);
          }
        }}
      />
    </div>
  );
};

const DeleteActionFileExplorer = () => {
  const [lastDeleted, setLastDeleted] = React.useState<string>("none");
  const [lastAction, setLastAction] = React.useState<string>("none");

  const items = React.useMemo(
    () =>
      ofArray([
        FileTree_createFolder("Deletable Folder", "studies/FolderA", FileItemIcon_Folder()),
        FileTree_createFile("Deletable File", "studies/FolderA/isa.study.xlsx", FileItemIcon_Document$()),
      ]),
    [],
  );

  return (
    <div className="swt:p-4 swt:space-y-4">
      <FileExplorer
        initialItems={items}
        canDeleteItem={() => true}
        onDeleteItem={(item) => setLastDeleted(item.Name)}
        getItemActions={(item) =>
          ofArray([new ContextMenuItem("Mark", "swt:fluent--star-24-regular", () => setLastAction(item.Name), undefined)])
        }
      />
      <div data-testid="last-deleted">Last deleted: {lastDeleted}</div>
      <div data-testid="last-row-action">Last action: {lastAction}</div>
    </div>
  );
};

const DestructiveContextMenuFileExplorer = () => {
  const [clickCount, setClickCount] = React.useState(0);
  const items = React.useMemo(() => ofArray([FileTree_createFile("Context File", "studies/A/file.txt", FileItemIcon_Document$())]), []);

  return (
    <div className="swt:p-4">
      <FileExplorer
        initialItems={items}
        onContextMenu={() =>
          ofArray([
            new ContextMenuItem(
              "Delete Item",
              "swt:fluent--delete-24-regular",
              () => setClickCount((count) => count + 1),
              undefined,
            ),
          ])
        }
      />
      <div data-testid="context-click-count">Context clicks: {clickCount}</div>
    </div>
  );
};

const LfsContextMenuFileExplorer = () => {
  const [downloadCount, setDownloadCount] = React.useState(0);
  const [freeCount, setFreeCount] = React.useState(0);

  const items = React.useMemo(
    () =>
      ofArray([
        Object.assign(
          FileTree_createFile("Downloaded LFS", "data/downloaded.bin", FileItemIcon_Document$()),
          { IsLFS: true, Downloaded: true, IsLFSPointer: false, SizeFormatted: "14 MB" },
        ),
        Object.assign(
          FileTree_createFile("Pointer LFS", "data/pointer.bin", FileItemIcon_Document$()),
          { IsLFS: true, Downloaded: false, IsLFSPointer: true, SizeFormatted: "42 MB" },
        ),
        Object.assign(
          FileTree_createFile("Plain File", "data/plain.txt", FileItemIcon_Document$()),
          { IsLFS: false, Downloaded: false, IsLFSPointer: false },
        ),
      ]),
    [],
  );

  const downloadLfsFile = React.useCallback(() => setDownloadCount((count) => count + 1), []);
  const freeLocalLfsCopy = React.useCallback(() => setFreeCount((count) => count + 1), []);

  return (
    <div className="swt:p-4">
      <FileExplorer
        initialItems={items}
        onContextMenu={(item) =>
          fileExplorerGitLfsContextMenuItems(
            item,
            () => {},
            downloadLfsFile,
            freeLocalLfsCopy,
          )
        }
        getItemStatusAction={(item) => fileExplorerGitLfsPillAction(item, downloadLfsFile, freeLocalLfsCopy)}
      />
      <div data-testid="lfs-download-count">Downloads: {downloadCount}</div>
      <div data-testid="lfs-free-count">Freed: {freeCount}</div>
    </div>
  );
};

const CopyPathDefaultFileExplorer = () => {
  const items = React.useMemo(
    () => ofArray([FileTree_createFile("Relative File", "studies/A/file.txt", FileItemIcon_Document$())]),
    [],
  );

  return (
    <div className="swt:p-4">
      <FileExplorer initialItems={items} />
    </div>
  );
};

const CopyPathResolverFileExplorer = () => {
  const items = React.useMemo(
    () => ofArray([FileTree_createFile("Absolute File", "studies/A/file.txt", FileItemIcon_Document$())]),
    [],
  );

  return (
    <div className="swt:p-4">
      <FileExplorer
        initialItems={items}
        getCopyPath={(item) => (item.Path ? `C:/arc/${item.Path}` : undefined)}
        getCopyRelativePath={(item) => item.Path ?? undefined}
      />
    </div>
  );
};

const veryLongFolderName =
  "Assay folder with an exceptionally long generated identifier that should be clipped before row controls";

const veryLongFileName =
  "measurement-export-with-an-exceptionally-long-generated-name-that-should-not-hide-controls.tsv";

const TruncatedOverflowFileExplorer = () => {
  const items = React.useMemo(() => {
    const childFile = createStableFile("child.txt", "assays/long-folder/child.txt", "truncate-child");
    const longFolder = createStableFolder(
      veryLongFolderName,
      "assays/long-folder",
      "truncate-folder",
      [childFile],
    );
    const longFile = createStableFile(veryLongFileName, "assays/long-file.tsv", "truncate-file");

    return ofArray([longFolder, longFile]);
  }, []);

  const rowAction = React.useCallback(
    (item: FileItem) =>
      ofArray([
        new ContextMenuItem(
          "Mark",
          "swt:fluent--star-24-regular",
          () => {
            void item;
          },
          undefined,
        ),
      ]),
    [],
  );

  const statusAction = React.useCallback(
    (item: FileItem) =>
      new ContextMenuItem(
        "Status",
        "swt:fluent--info-24-regular",
        () => {
          void item;
        },
        undefined,
      ),
    [],
  );

  return (
    <div
      data-testid="truncated-file-explorer-viewport"
      className="swt:p-2"
      style={{ width: 280, overflow: "hidden" }}
    >
      <FileExplorer
        initialItems={items}
        getItemActions={rowAction}
        getItemStatusAction={statusAction}
        truncateOverflowingItemNames={true}
      />
    </div>
  );
};

const StickyParentsFileExplorer = () => {
  const items = React.useMemo(() => {
    const measurements = Array.from({ length: 36 }, (_, index) =>
      createStableFile(
        `measurement-${String(index + 1).padStart(2, "0")}.tsv`,
        `studies/Study A/assays/Assay A/data/measurement-${index + 1}.tsv`,
        `sticky-measurement-${index + 1}`,
      ),
    );

    const dataFolder = Object.assign(
      createStableFolder("data", "studies/Study A/assays/Assay A/data", "sticky-data", measurements),
      { IsExpanded: true },
    );
    const assayFolder = Object.assign(
      createStableFolder("Assay A", "studies/Study A/assays/Assay A", "sticky-assay", [dataFolder]),
      { IsExpanded: true },
    );
    const studyFolder = Object.assign(
      createStableFolder("Study A", "studies/Study A", "sticky-study", [assayFolder]),
      { IsExpanded: true },
    );

    return ofArray([studyFolder]);
  }, []);

  return (
    <div
      data-testid="sticky-file-explorer-viewport"
      className="swt:h-40 swt:overflow-y-auto swt:overflow-x-auto"
    >
      <FileExplorer
        initialItems={items}
        delegateHorizontalScrollToParent={true}
        truncateOverflowingItemNames={true}
      />
    </div>
  );
};

const installClipboardMock = () => {
  const writeText = fn(async () => undefined);
  Object.defineProperty(navigator, "clipboard", {
    value: { writeText },
    configurable: true,
    writable: true,
  });

  return writeText;
};

const expectStickyParentRowsToStayVisible = async (canvasElement: HTMLElement) => {
  const canvas = within(canvasElement);
  const viewport = await canvas.findByTestId("sticky-file-explorer-viewport");
  const parentToggles = await Promise.all(
    ["Collapse Study A", "Collapse Assay A", "Collapse data"].map((name) =>
      canvas.findByRole("button", { name }),
    ),
  );

  for (const toggle of parentToggles) {
    const parentRow = toggle.closest("[data-file-item-id]");

    if (!parentRow) {
      throw new Error(`Expected sticky parent row for ${toggle.getAttribute("aria-label")}.`);
    }

    await expect(parentRow).toHaveClass(/swt:bg-base-100/);
    await expect(parentRow).toHaveClass(/swt:border-b/);
    await expect(parentRow).toHaveClass(/swt:shadow-sm/);
  }

  viewport.scrollTop = 360;
  fireEvent.scroll(viewport);

  await waitFor(() => {
    const viewportRect = viewport.getBoundingClientRect();

    for (const toggle of parentToggles) {
      const rect = toggle.getBoundingClientRect();

      expect(rect.top).toBeGreaterThanOrEqual(viewportRect.top - 1);
      expect(rect.bottom).toBeLessThanOrEqual(viewportRect.bottom + 1);
    }
  });
};

const expectExpandedNonStickyDirectoryRowsToStayUnframed = async (canvasElement: HTMLElement) => {
  const canvas = within(canvasElement);
  const folderLabel = await canvas.findByText("Initially Expanded");
  const folderRow = folderLabel.closest("[data-file-item-id]");

  await waitFor(() => expect(folderRow).toBeTruthy());

  if (!folderRow) {
    throw new Error("Expected expanded directory row.");
  }

  await expect(folderRow).not.toHaveClass(/swt:bg-base-100/);
  await expect(folderRow).not.toHaveClass(/swt:border-b/);
  await expect(folderRow).not.toHaveClass(/swt:shadow-sm/);
  expect(window.getComputedStyle(folderRow).position).not.toBe("sticky");
};

const expectContextMenuCopy = async (
  canvasElement: HTMLElement,
  itemLabel: string,
  menuLabel: string,
  expectedText: string,
) => {
  const writeText = installClipboardMock();
  const canvas = within(canvasElement);
  const targetFile = await canvas.findByText(itemLabel);
  const fileItem = targetFile.closest("[data-file-item-id]");

  await waitFor(() => expect(fileItem).toBeTruthy());

  if (!fileItem) {
    throw new Error(`Expected file item element for ${itemLabel}.`);
  }

  fireEvent.contextMenu(fileItem, { clientX: 30, clientY: 30, bubbles: true });
  await userEvent.click(await screen.findByText(menuLabel));

  await waitFor(() => expect(writeText).toHaveBeenCalledWith(expectedText));
};

const expectLongNameControlsToStayVisible = async (canvasElement: HTMLElement, itemName: string) => {
  const canvas = within(canvasElement);
  const viewport = await canvas.findByTestId("truncated-file-explorer-viewport");
  const label = await canvas.findByText(itemName);
  const markButton = await canvas.findByRole("button", { name: `Mark ${itemName}` });
  const statusButton = await canvas.findByRole("button", { name: `Status ${itemName}` });

  await waitFor(() => {
    const viewportRect = viewport.getBoundingClientRect();
    const labelRect = label.getBoundingClientRect();
    const markRect = markButton.getBoundingClientRect();
    const statusRect = statusButton.getBoundingClientRect();

    expect(markRect.right).toBeLessThanOrEqual(viewportRect.right + 1);
    expect(statusRect.right).toBeLessThanOrEqual(viewportRect.right + 1);
    expect(labelRect.right).toBeLessThanOrEqual(markRect.left + 1);
    expect(label.clientWidth).toBeLessThan(label.scrollWidth);
  });
};

const meta: Meta<typeof FileExplorerExample> = {
  title: "Page Components/FileExplorer",
  component: FileExplorerExample,
  parameters: {
    layout: "fullscreen",
  },
};

export default meta;
type Story = StoryObj<typeof FileExplorerExample>;

export const Default: Story = {
  render: () => <FileExplorerExample />,

  play: (async ({ canvasElement }: { canvasElement: HTMLElement }) => {
    const canvas = within(canvasElement);

    const container = await canvas.findByTestId("file-explorer-container");
    expect(container).toBeTruthy();

    const resume = await canvas.findByText("resume.pdf");
    expect(resume).toBeTruthy();

    const folder = await canvas.findByText("My Files");
    await userEvent.click(folder);
  }),
};

export const DirectoryArrowsReflectLoadability: StoryObj<typeof LazyLoadDirectoryFileExplorer> = {
  render: () => <LazyLoadDirectoryFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await canvas.findByText("Empty Folder");
    await expect(canvas.queryByRole("button", { name: "Expand Empty Folder" })).toBeNull();

    const lazyFolderToggle = await canvas.findByRole("button", { name: "Expand Lazy Folder" });
    await expect(canvas.queryByText("Lazy Child.txt")).toBeNull();

    await userEvent.click(lazyFolderToggle);

    await waitFor(() => {
      expect(canvas.getByText("Lazy Child.txt")).toBeInTheDocument();
    });
  },
};

export const SelectingLazyDirectoryMaterializesChildren: StoryObj<typeof LazyLoadDirectoryFileExplorer> = {
  render: () => <LazyLoadDirectoryFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.queryByText("Lazy Child.txt")).toBeNull();
    await userEvent.click(await canvas.findByText("Lazy Folder"));

    await waitFor(() => expect(canvas.getByText("Lazy Child.txt")).toBeInTheDocument());
    await expect(canvas.getByRole("button", { name: "Collapse Lazy Folder" })).toBeInTheDocument();

    await userEvent.click(await canvas.findByText("Lazy Folder"));

    await waitFor(() => expect(canvas.queryByText("Lazy Child.txt")).toBeNull());
    await expect(canvas.getByRole("button", { name: "Expand Lazy Folder" })).toBeInTheDocument();
  },
};

export const ContextMenuExpansionMaterializesChildren: StoryObj<typeof LazyLoadDirectoryFileExplorer> = {
  render: () => <LazyLoadDirectoryFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const lazyFolder = await canvas.findByText("Lazy Folder");
    const lazyFolderItem = lazyFolder.closest("[data-file-item-id]");

    await waitFor(() => expect(lazyFolderItem).toBeTruthy());

    if (!lazyFolderItem) {
      throw new Error("Expected lazy folder item for context menu test.");
    }

    fireEvent.contextMenu(lazyFolderItem, { clientX: 24, clientY: 24, bubbles: true });
    await userEvent.click(await screen.findByText("Expand"));

    await waitFor(() => expect(canvas.getByText("Lazy Child.txt")).toBeInTheDocument());
  },
};

export const InitialExpandedHintIsApplied: StoryObj<typeof InitiallyExpandedFileExplorer> = {
  render: () => <InitiallyExpandedFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(await canvas.findByText("Initially Visible.txt")).toBeInTheDocument();
    await expect(canvas.getByRole("button", { name: "Collapse Initially Expanded" })).toBeInTheDocument();
    await expectExpandedNonStickyDirectoryRowsToStayUnframed(canvasElement);
  },
};

export const SelectedFileHighlightPersistsAfterParentReopen: StoryObj<typeof SelectedPathFileExplorer> = {
  render: () => <SelectedPathFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const selectedFileLabel = await canvas.findByText("selected-report.txt");
    await expect(selectedFileLabel).toHaveClass(/swt:font-semibold/);
    await expect(selectedFileLabel).toHaveClass(/swt:text-primary/);

    const collapseParent = await canvas.findByRole("button", { name: "Collapse Selected Parent" });
    await expect(collapseParent).toHaveClass(/swt:bg-base-300/);
    await userEvent.click(collapseParent);

    await waitFor(() => {
      expect(canvas.queryByText("selected-report.txt")).toBeNull();
    });

    await userEvent.click(await canvas.findByRole("button", { name: "Expand Selected Parent" }));

    const selectedFileLabelAfterReopen = await canvas.findByText("selected-report.txt");
    await expect(selectedFileLabelAfterReopen).toHaveClass(/swt:font-semibold/);
    await expect(selectedFileLabelAfterReopen).toHaveClass(/swt:text-primary/);
  },
};

export const CollapsedDirectoryStaysClosedAfterLazySiblingLoads: StoryObj<typeof ExpansionRefreshFileExplorer> = {
  render: () => <ExpansionRefreshFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(await canvas.findByRole("button", { name: "Collapse Selected Parent" }));
    await waitFor(() => expect(canvas.queryByText("selected-report.txt")).toBeNull());

    await userEvent.click(await canvas.findByRole("button", { name: "Expand Lazy Folder" }));
    await waitFor(() => expect(canvas.getByText("Lazy Child.txt")).toBeInTheDocument());

    await expect(canvas.queryByText("selected-report.txt")).toBeNull();
    await expect(canvas.getByRole("button", { name: "Expand Selected Parent" })).toBeInTheDocument();
  },
};

export const ContextMenuCreatesInMemoryUntilSave: StoryObj<typeof InMemoryCreateFileExplorer> = {
  render: () => <InMemoryCreateFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const rootFolder = await canvas.findByText("ARC Root");

    await waitFor(() => expect(rootFolder.closest("[data-file-item-id]")).toBeTruthy());
    fireEvent.contextMenu(rootFolder, { clientX: 24, clientY: 24, bubbles: true });

    const addMenuItems = await Promise.all(
      arcCreateItems.map((item) => screen.findByText(item.label)),
    );

    expect(addMenuItems.map((item) => item.textContent)).toEqual(arcCreateItems.map((item) => item.label));

    await userEvent.click(addMenuItems[1]);

    await waitFor(() => {
      expect(canvas.getByTestId("pending-draft")).toHaveTextContent("Pending draft: assays/NewAssay/isa.assay.xlsx");
      expect(canvas.getByTestId("saved-count")).toHaveTextContent("Saved count: 0");
      expect(canvas.getByTestId("saved-paths")).toHaveTextContent("Saved paths: none");
    });

    await userEvent.click(canvas.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(canvas.getByTestId("pending-draft")).toHaveTextContent("Pending draft: none");
      expect(canvas.getByTestId("saved-count")).toHaveTextContent("Saved count: 1");
      expect(canvas.getByTestId("saved-paths")).toHaveTextContent("Saved paths: assays/NewAssay/isa.assay.xlsx");
    });
  },
};

export const InlineDeleteDispatchesForFileAndDirectory: StoryObj<typeof DeleteActionFileExplorer> = {
  render: () => <DeleteActionFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const deletableDirectory = await canvas.findByText("Deletable Folder");
    await userEvent.hover(deletableDirectory);
    await userEvent.click(await canvas.findByRole("button", { name: "Mark Deletable Folder" }));
    await waitFor(() => expect(canvas.getByTestId("last-row-action")).toHaveTextContent("Last action: Deletable Folder"));
    await userEvent.click(await canvas.findByRole("button", { name: "Delete Deletable Folder" }));
    await waitFor(() => expect(canvas.getByTestId("last-deleted")).toHaveTextContent("Last deleted: Deletable Folder"));

    const deletableFile = await canvas.findByText("Deletable File");
    await userEvent.hover(deletableFile);
    await userEvent.click(await canvas.findByRole("button", { name: "Mark Deletable File" }));
    await waitFor(() => expect(canvas.getByTestId("last-row-action")).toHaveTextContent("Last action: Deletable File"));
    await userEvent.click(await canvas.findByRole("button", { name: "Delete Deletable File" }));
    await waitFor(() => expect(canvas.getByTestId("last-deleted")).toHaveTextContent("Last deleted: Deletable File"));
  },
};

export const ContextMenuItemDispatchesAction: StoryObj<typeof DestructiveContextMenuFileExplorer> = {
  render: () => <DestructiveContextMenuFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const targetFile = await canvas.findByText("Context File");
    const fileItem = targetFile.closest("[data-file-item-id]");

    await waitFor(() => expect(fileItem).toBeTruthy());

    if (!fileItem) {
      throw new Error("Expected file item element for context menu test.");
    }

    fireEvent.contextMenu(fileItem, { clientX: 30, clientY: 30, bubbles: true });
    const deleteMenuItem = await screen.findByText("Delete Item");
    await userEvent.click(deleteMenuItem);
    await waitFor(() =>
      expect(canvas.getByTestId("context-click-count")).toHaveTextContent("Context clicks: 1"),
    );
  },
};

export const LfsContextMenuStates: StoryObj<typeof LfsContextMenuFileExplorer> = {
  render: () => <LfsContextMenuFileExplorer />,

  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const pointerPill = await canvas.findByLabelText("Download LFS file Pointer LFS. LFS Pointer - 42 MB");
    await userEvent.click(pointerPill);
    await waitFor(() => expect(canvas.getByTestId("lfs-download-count")).toHaveTextContent("Downloads: 1"));

    const downloadedPill = await canvas.findByLabelText("Free local LFS copy Downloaded LFS. LFS Downloaded - 14 MB");
    await userEvent.click(downloadedPill);
    await waitFor(() => expect(canvas.getByTestId("lfs-free-count")).toHaveTextContent("Freed: 1"));

    const downloadedFile = await canvas.findByText("Downloaded LFS");
    const downloadedItem = downloadedFile.closest("[data-file-item-id]");
    await waitFor(() => expect(downloadedItem).toBeTruthy());

    if (!downloadedItem) {
      throw new Error("Expected downloaded LFS file item.");
    }

    fireEvent.contextMenu(downloadedItem, { clientX: 30, clientY: 30, bubbles: true });
    const enabledMenuItem = await screen.findByText("Free local LFS copy");
    const disabledDownloadMenuItem = await screen.findByText("Download LFS file");
    await expect(enabledMenuItem).toBeVisible();
    await expect(enabledMenuItem).not.toHaveClass("swt:opacity-50");
    await expect(disabledDownloadMenuItem).toHaveClass("swt:opacity-50");

    await userEvent.click(document.body);

    const pointerFile = await canvas.findByText("Pointer LFS");
    const pointerItem = pointerFile.closest("[data-file-item-id]");
    await waitFor(() => expect(pointerItem).toBeTruthy());

    if (!pointerItem) {
      throw new Error("Expected pointer LFS file item.");
    }

    fireEvent.contextMenu(pointerItem, { clientX: 30, clientY: 30, bubbles: true });
    const enabledDownloadMenuItem = await screen.findByText("Download LFS file");
    const disabledMenuItem = await screen.findByText("Free local LFS copy");
    await expect(enabledDownloadMenuItem).toBeVisible();
    await expect(enabledDownloadMenuItem).not.toHaveClass("swt:opacity-50");
    await expect(disabledMenuItem).toBeVisible();
    await expect(disabledMenuItem).toHaveClass("swt:opacity-50");

    await userEvent.click(document.body);

    const plainFile = await canvas.findByText("Plain File");
    const plainItem = plainFile.closest("[data-file-item-id]");
    await waitFor(() => expect(plainItem).toBeTruthy());

    if (!plainItem) {
      throw new Error("Expected plain file item.");
    }

    fireEvent.contextMenu(plainItem, { clientX: 30, clientY: 30, bubbles: true });
    await expect(screen.queryByText("Free local LFS copy")).toBeNull();
  },
};

export const CopyPathUsesRelativePathByDefault: StoryObj<typeof CopyPathDefaultFileExplorer> = {
  render: () => <CopyPathDefaultFileExplorer />,

  play: async ({ canvasElement }) => {
    await expectContextMenuCopy(canvasElement, "Relative File", "Copy Path", "studies/A/file.txt");
  },
};

export const CopyPathUsesProvidedResolver: StoryObj<typeof CopyPathResolverFileExplorer> = {
  render: () => <CopyPathResolverFileExplorer />,

  play: async ({ canvasElement }) => {
    await expectContextMenuCopy(canvasElement, "Absolute File", "Copy Path", "C:/arc/studies/A/file.txt");
  },
};

export const CopyRelativePathUsesProvidedResolver: StoryObj<typeof CopyPathResolverFileExplorer> = {
  render: () => <CopyPathResolverFileExplorer />,

  play: async ({ canvasElement }) => {
    await expectContextMenuCopy(canvasElement, "Absolute File", "Copy Relative Path", "studies/A/file.txt");
  },
};

export const LongNamesKeepInlineControlsVisible: StoryObj<typeof TruncatedOverflowFileExplorer> = {
  render: () => <TruncatedOverflowFileExplorer />,

  play: async ({ canvasElement }) => {
    await expectLongNameControlsToStayVisible(canvasElement, veryLongFolderName);
    await expectLongNameControlsToStayVisible(canvasElement, veryLongFileName);
  },
};

export const ExpandedDirectoryParentsStayVisibleWhileScrolling: StoryObj<typeof StickyParentsFileExplorer> = {
  render: () => <StickyParentsFileExplorer />,

  play: async ({ canvasElement }) => {
    await expectStickyParentRowsToStayVisible(canvasElement);
  },
};
