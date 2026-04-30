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
    const breadcrumbContainer = () =>
      canvasElement.querySelector("[class*='swt:breadcrumbs']") as HTMLElement | null;
    const expectBreadcrumbContains = async (label: string) => {
      await waitFor(() => {
        const breadcrumbs = breadcrumbContainer();
        expect(breadcrumbs).toBeTruthy();
        expect(within(breadcrumbs!).getByText(label)).toBeTruthy();
      });
    };

    const container = await canvas.findByTestId("file-explorer-container");
    const scrollContainer = await canvas.findByTestId("file-explorer-scroll-container");
    expect(container).toBeTruthy();
    expect(scrollContainer).toBeTruthy();
    expect(scrollContainer.className).toContain("swt:overflow-x-auto");
    expect(getComputedStyle(scrollContainer).overflowX).toBe("auto");

    const resume = await canvas.findByText("resume.pdf");
    expect(resume).toBeTruthy();
    await userEvent.click(resume);
    await expectBreadcrumbContains("resume.pdf");

    const folderNameButton = await canvas.findByRole("button", { name: "My Files" });
    let expandFolderButton = await canvas.findByRole("button", { name: "Expand My Files" });
    expect(getComputedStyle(folderNameButton).cursor).not.toBe("pointer");
    expect(getComputedStyle(expandFolderButton).cursor).toBe("pointer");
    expect(expandFolderButton.className).toContain("swt:h-5");
    expect(expandFolderButton.className).toContain("swt:w-5");
    expect((folderNameButton.compareDocumentPosition(expandFolderButton) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0).toBe(true);
    const myFilesRowContainer = expandFolderButton.closest("div[data-file-item-id]") as HTMLElement | null;
    expect(myFilesRowContainer).toBeTruthy();
    expect(getComputedStyle(myFilesRowContainer!).cursor).not.toBe("pointer");
    const myFilesRowPaddingRight = Number.parseFloat(getComputedStyle(myFilesRowContainer!).paddingRight || "0");
    const myFilesRowRight = myFilesRowContainer!.getBoundingClientRect().right - myFilesRowPaddingRight;
    const expandButtonRight = expandFolderButton.getBoundingClientRect().right;
    expect(Math.abs(myFilesRowRight - expandButtonRight)).toBeLessThanOrEqual(2);
    const lfsFolderStatusBadge =
      myFilesRowContainer!.querySelector("[data-lfs-download-status='not-downloaded']") as HTMLElement | null;
    const lfsFolderSizeBadge = within(myFilesRowContainer!).getByText("2 KB");
    expect(lfsFolderStatusBadge).toBeTruthy();
    expect(within(lfsFolderStatusBadge!).getByText("LFS")).toBeTruthy();
    const lfsFolderPill = lfsFolderStatusBadge!.parentElement as HTMLElement | null;
    expect(lfsFolderPill).toBeTruthy();
    expect(lfsFolderStatusBadge!.className).toContain("swt:bg-info");
    expect(lfsFolderStatusBadge!.className).toContain("swt:text-info-content");
    expect(lfsFolderSizeBadge.className).toContain("swt:bg-base-200");
    expect(lfsFolderSizeBadge.className).toContain("swt:text-base-content");
    expect(lfsFolderPill!.className).toContain("swt:rounded-full");
    expect(lfsFolderPill!.className).toContain("swt:overflow-hidden");
    const lfsFolderStatusIcon = lfsFolderStatusBadge!.querySelector("i");
    expect(lfsFolderStatusIcon).toBeTruthy();
    expect(lfsFolderStatusIcon!.className).toContain("swt:fluent--cloud-arrow-down-24-regular");
    expect(getComputedStyle(lfsFolderStatusBadge!).cursor).not.toBe("pointer");
    expect(getComputedStyle(lfsFolderSizeBadge).cursor).not.toBe("pointer");
    expect((lfsFolderStatusBadge!.compareDocumentPosition(expandFolderButton) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0).toBe(true);
    await userEvent.click(lfsFolderStatusBadge!);
    await expectBreadcrumbContains("resume.pdf");
    expect(canvas.queryByText("My Files")).toBeTruthy();
    expect(canvas.queryByRole("button", { name: "Expand Empty Folder" })).toBeNull();
    expect(canvas.queryByText("Project-final.psd")).toBeNull();
    await userEvent.click(folderNameButton);
    expect(canvas.queryByText("Project-final.psd")).toBeNull();
    await expectBreadcrumbContains("My Files");
    const selectedFolderNameButton = await canvas.findByRole("button", { name: "My Files" });
    const selectedFolderLabel = selectedFolderNameButton.querySelector("span");
    expect(selectedFolderLabel).toBeTruthy();
    expect(selectedFolderLabel!.className).toContain("swt:font-semibold");
    expect(selectedFolderLabel!.className).toContain("swt:text-primary");
    const notesLabel = await canvas.findByText("notes.txt");
    const notesRow = notesLabel.closest("a[data-file-item-id]") as HTMLElement | null;
    expect(notesRow).toBeTruthy();
    expect(getComputedStyle(notesRow!).cursor).not.toBe("pointer");
    const notesLabelInitialStyle = getComputedStyle(notesLabel);
    const initialNotesLabelColor = notesLabelInitialStyle.color;
    const initialNotesLabelWeight = notesLabelInitialStyle.fontWeight;
    await userEvent.hover(notesRow!);
    await waitFor(() => {
      const hoveredNotesLabelStyle = getComputedStyle(notesLabel);
      expect(hoveredNotesLabelStyle.color).toBe(initialNotesLabelColor);
      expect(hoveredNotesLabelStyle.fontWeight).toBe(initialNotesLabelWeight);
    });
    expandFolderButton = await canvas.findByRole("button", { name: "Expand My Files" });
    expect(expandFolderButton.className).toContain("swt:bg-base-300");
    const expandIcon = expandFolderButton.querySelector("i");
    expect(expandIcon).toBeTruthy();
    expect(expandIcon!.className).toContain("swt:fluent--caret-right-24-filled");

    await userEvent.click(expandFolderButton);
    await expectBreadcrumbContains("My Files");

    const collapseFolderButton = await canvas.findByRole("button", { name: "Collapse My Files" });
    expect(collapseFolderButton.className).toContain("swt:bg-base-300");
    expect(getComputedStyle(collapseFolderButton).cursor).toBe("pointer");
    const collapseIcon = collapseFolderButton.querySelector("i");
    expect(collapseIcon).toBeTruthy();
    expect(collapseIcon!.className).toContain("swt:fluent--caret-down-24-filled");

    const nestedDownloadedFile = await canvas.findByText("Project-final.psd");
    const nestedDownloadedFileRow = nestedDownloadedFile.closest("a[data-file-item-id]") as HTMLElement | null;
    expect(nestedDownloadedFileRow).toBeTruthy();
    const lfsDownloadedStatusBadge =
      nestedDownloadedFileRow!.querySelector("[data-lfs-download-status='downloaded']") as HTMLElement | null;
    expect(lfsDownloadedStatusBadge).toBeTruthy();
    expect(within(lfsDownloadedStatusBadge!).getByText("LFS")).toBeTruthy();
    expect(lfsDownloadedStatusBadge!.className).toContain("swt:badge-success");
    const lfsDownloadedSizeBadge = within(nestedDownloadedFileRow!).getByText("6 MB");
    expect(lfsDownloadedSizeBadge).toBeTruthy();
    expect(lfsDownloadedSizeBadge.className).toContain("swt:bg-base-200");
    expect(lfsDownloadedSizeBadge.className).toContain("swt:text-base-content");
    const lfsDownloadedPill = lfsDownloadedStatusBadge!.parentElement as HTMLElement | null;
    expect(lfsDownloadedPill).toBeTruthy();
    expect(lfsDownloadedPill!.className).toContain("swt:rounded-full");
    expect(lfsDownloadedPill!.className).toContain("swt:overflow-hidden");
    const lfsDownloadedStatusIcon = lfsDownloadedStatusBadge!.querySelector("i");
    expect(lfsDownloadedStatusIcon).toBeTruthy();
    expect(lfsDownloadedStatusIcon!.className).toContain("swt:fluent--checkmark-circle-24-regular");
    expect(getComputedStyle(lfsDownloadedStatusBadge!).cursor).not.toBe("pointer");
    await userEvent.click(lfsDownloadedStatusBadge!);
    await expectBreadcrumbContains("My Files");
    await waitFor(() => {
      const breadcrumbs = breadcrumbContainer();
      expect(breadcrumbs).toBeTruthy();
      expect(within(breadcrumbs!).queryByText("Project-final.psd")).toBeNull();
    });

    const nestedFile = await canvas.findByText("Project-final.psd");
    expect(nestedFile).toBeTruthy();
    await userEvent.click(nestedFile);
    await expectBreadcrumbContains("Project-final.psd");

    const parentFolderCollapseToggle = await canvas.findByRole("button", { name: "Collapse My Files" });
    expect(parentFolderCollapseToggle.className).toContain("swt:bg-base-300");
    const parentFolderRowAfterChildSelection = parentFolderCollapseToggle.closest("div[data-file-item-id]") as HTMLElement | null;
    expect(parentFolderRowAfterChildSelection).toBeTruthy();
    expect(parentFolderRowAfterChildSelection!.className).toContain("swt:bg-base-300");

    await userEvent.click(parentFolderCollapseToggle);
    expect(within(container).queryByText("Project-final.psd")).toBeNull();
    await expectBreadcrumbContains("Project-final.psd");
  }),
};

export const CompactSidebar: Story = {
  render: () => (
    <div style={{ width: "12rem" }}>
      <FileExplorerExample />
    </div>
  ),

  play: (async ({ canvasElement }: { canvasElement: HTMLElement }) => {
    const canvas = within(canvasElement);

    const expandFolderButton = await canvas.findByRole("button", { name: "Expand My Files" });
    const myFilesRowContainer = expandFolderButton.closest("div[data-file-item-id]") as HTMLElement | null;
    expect(myFilesRowContainer).toBeTruthy();

    const lfsNotDownloadedStatusBadge =
      myFilesRowContainer!.querySelector("[data-lfs-download-status='not-downloaded']") as HTMLElement | null;
    expect(lfsNotDownloadedStatusBadge).toBeTruthy();
    expect(within(myFilesRowContainer!).getByText("LFS")).toBeTruthy();
    const lfsFolderSizeBadge = within(myFilesRowContainer!).getByText("2 KB");
    expect(lfsFolderSizeBadge).toBeTruthy();
    expect(lfsNotDownloadedStatusBadge!.className).toContain("swt:bg-info");
    expect(lfsNotDownloadedStatusBadge!.className).toContain("swt:text-info-content");
    expect(lfsFolderSizeBadge.className).toContain("swt:bg-base-200");
    expect(lfsFolderSizeBadge.className).toContain("swt:text-base-content");

    const lfsNotDownloadedIcon = lfsNotDownloadedStatusBadge!.querySelector("i");
    expect(lfsNotDownloadedIcon).toBeTruthy();
    expect(lfsNotDownloadedIcon!.className).toContain("swt:fluent--cloud-arrow-down-24-regular");

    const lfsNotDownloadedPill = lfsNotDownloadedStatusBadge!.parentElement as HTMLElement | null;
    expect(lfsNotDownloadedPill).toBeTruthy();
    expect(lfsNotDownloadedPill!.getAttribute("aria-label")).toBe("LFS Not Downloaded - 2 KB");
    expect(lfsNotDownloadedPill!.getAttribute("title")).toBe("LFS Not Downloaded - 2 KB");

    await userEvent.click(lfsNotDownloadedStatusBadge!);
    expect(canvas.queryByText("Project-final.psd")).toBeNull();

    await userEvent.click(expandFolderButton);

    const nestedFile = await canvas.findByText("Project-final.psd");
    expect(nestedFile).toBeTruthy();
    const nestedFileRow = nestedFile.closest("a[data-file-item-id]") as HTMLElement | null;
    expect(nestedFileRow).toBeTruthy();

    expect(within(nestedFileRow!).queryByText("LFS Downloaded")).toBeNull();
    expect(within(nestedFileRow!).getByText("LFS")).toBeTruthy();
    const downloadedSizeBadge = within(nestedFileRow!).getByText("6 MB");
    expect(downloadedSizeBadge).toBeTruthy();
    expect(downloadedSizeBadge.className).toContain("swt:bg-base-200");
    expect(downloadedSizeBadge.className).toContain("swt:text-base-content");

    const lfsDownloadedStatusBadge =
      nestedFileRow!.querySelector("[data-lfs-download-status='downloaded']") as HTMLElement | null;
    expect(lfsDownloadedStatusBadge).toBeTruthy();
    expect(lfsDownloadedStatusBadge!.className).toContain("swt:badge-success");
    const lfsDownloadedIcon = lfsDownloadedStatusBadge!.querySelector("i");
    expect(lfsDownloadedIcon).toBeTruthy();
    expect(lfsDownloadedIcon!.className).toContain("swt:fluent--checkmark-circle-24-regular");

    const lfsDownloadedPill = lfsDownloadedStatusBadge!.parentElement as HTMLElement | null;
    expect(lfsDownloadedPill).toBeTruthy();
    expect(lfsDownloadedPill!.getAttribute("aria-label")).toBe("LFS Downloaded - 6 MB");
    expect(lfsDownloadedPill!.getAttribute("title")).toBe("LFS Downloaded - 6 MB");
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
