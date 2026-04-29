import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { screen, within, expect, userEvent, waitFor, fireEvent } from "storybook/test";
import { FileExplorer, FileExplorerExample_Example as FileExplorerExample } from "./FileExplorer.fs.js";
import {
  ContextMenuItem,
  FileItemIcon_Folder,
  FileTree_createFolder,
} from "./FileTreeDataStructures.fs.js";
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
