import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, userEvent, waitFor } from "storybook/test";
import { FileExplorerExample_Example as FileExplorerExample } from "./FileExplorer.fs.js";

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
    expect(container).toBeTruthy();

    const resume = await canvas.findByText("resume.pdf");
    expect(resume).toBeTruthy();
    await userEvent.click(resume);
    await expectBreadcrumbContains("resume.pdf");

    const folderNameButton = await canvas.findByRole("button", { name: "My Files" });
    let expandFolderButton = await canvas.findByRole("button", { name: "Expand My Files" });
    expect(getComputedStyle(folderNameButton).cursor).not.toBe("pointer");
    expect(getComputedStyle(expandFolderButton).cursor).not.toBe("pointer");
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
    expect(lfsFolderStatusBadge!.className).toContain("swt:badge-info");
    expect(lfsFolderSizeBadge.className).toContain("swt:bg-base-200");
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
    expect(within(nestedDownloadedFileRow!).queryByText("6 MB")).toBeNull();
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
    expect(lfsNotDownloadedStatusBadge!.className).toContain("swt:badge-info");

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
    expect(within(nestedFileRow!).queryByText("6 MB")).toBeNull();

    const lfsDownloadedStatusBadge =
      nestedFileRow!.querySelector("[data-lfs-download-status='downloaded']") as HTMLElement | null;
    expect(lfsDownloadedStatusBadge).toBeTruthy();
    expect(lfsDownloadedStatusBadge!.className).toContain("swt:badge-success");
    const lfsDownloadedIcon = lfsDownloadedStatusBadge!.querySelector("i");
    expect(lfsDownloadedIcon).toBeTruthy();
    expect(lfsDownloadedIcon!.className).toContain("swt:fluent--checkmark-circle-24-regular");

    const lfsDownloadedPill = lfsDownloadedStatusBadge!.parentElement as HTMLElement | null;
    expect(lfsDownloadedPill).toBeTruthy();
    expect(lfsDownloadedPill!.getAttribute("aria-label")).toBe("LFS Downloaded");
    expect(lfsDownloadedPill!.getAttribute("title")).toBe("LFS Downloaded");
  }),
};
