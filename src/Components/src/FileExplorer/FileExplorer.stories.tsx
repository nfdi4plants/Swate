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
    const lfsFolderBadge = within(myFilesRowContainer!).getByText("LFS Not Downloaded");
    expect(lfsFolderBadge.className).toContain("swt:badge-warning");
    expect(getComputedStyle(lfsFolderBadge).cursor).not.toBe("pointer");
    const lfsFolderSizeBadge = within(myFilesRowContainer!).getByText("2 KB");
    expect(getComputedStyle(lfsFolderSizeBadge).cursor).not.toBe("pointer");
    expect((lfsFolderBadge.compareDocumentPosition(expandFolderButton) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0).toBe(true);
    await userEvent.click(lfsFolderBadge);
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

    const lfsDownloadedBadge = await canvas.findByText("LFS Downloaded");
    expect(lfsDownloadedBadge.className).toContain("swt:badge-success");
    expect(getComputedStyle(lfsDownloadedBadge).cursor).not.toBe("pointer");
    const lfsDownloadedSizeBadge = await canvas.findByText("6 MB");
    expect(getComputedStyle(lfsDownloadedSizeBadge).cursor).not.toBe("pointer");
    await userEvent.click(lfsDownloadedBadge);
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
