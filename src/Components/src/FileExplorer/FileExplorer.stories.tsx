import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { screen, within, expect, userEvent, waitFor, fireEvent } from "storybook/test";
import { FileExplorer, FileExplorerExample_Example as FileExplorerExample } from "./FileExplorer.fs.js";
import {
  ContextMenuItem,
  ContextMenuItemTone_Destructive,
  FileItemIcon_Document$,
  FileItemIcon_Folder,
  FileTree_createFile,
  FileTree_createFolder,
} from "./Types.fs.js";
import { ofArray } from "../fable_modules/fable-library-ts.5.0.0-alpha.21/List.ts";

const arcCreateItems = [
  { label: "Add Study", path: "studies/NewStudy/isa.study.xlsx" },
  { label: "Add Assay", path: "assays/NewAssay/isa.assay.xlsx" },
  { label: "Add Workflow", path: "workflows/NewWorkflow/isa.workflow.xlsx" },
  { label: "Add Run", path: "runs/NewRun/isa.run.xlsx" },
];

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

const DeleteActionFileExplorer = () => {
  const [lastDeleted, setLastDeleted] = React.useState<string>("none");

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
      />
      <div data-testid="last-deleted">Last deleted: {lastDeleted}</div>
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
              ContextMenuItemTone_Destructive(),
            ),
          ])
        }
      />
      <div data-testid="context-click-count">Context clicks: {clickCount}</div>
    </div>
  );
};

const meta: Meta<typeof FileExplorerExample> = {
  title: "Components/FileExplorer",
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
    await userEvent.click(await canvas.findByRole("button", { name: "Delete Deletable Folder" }));

    await waitFor(() =>
      expect(canvas.getByTestId("last-deleted")).toHaveTextContent("Last deleted: Deletable Folder"),
    );

    const deletableFile = await canvas.findByText("Deletable File");
    await userEvent.hover(deletableFile);
    await userEvent.click(await canvas.findByRole("button", { name: "Delete Deletable File" }));

    await waitFor(() =>
      expect(canvas.getByTestId("last-deleted")).toHaveTextContent("Last deleted: Deletable File"),
    );
  },
};

export const DestructiveContextMenuItemUsesErrorTone: StoryObj<typeof DestructiveContextMenuFileExplorer> = {
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
    expect(deleteMenuItem).toHaveClass("swt:text-error");
  },
};
