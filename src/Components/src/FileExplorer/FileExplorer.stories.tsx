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
    expect((folderNameButton.compareDocumentPosition(expandFolderButton) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0).toBe(true);
    const myFilesRowContainer = expandFolderButton.closest("div[data-file-item-id]") as HTMLElement | null;
    expect(myFilesRowContainer).toBeTruthy();
    const myFilesRowPaddingRight = Number.parseFloat(getComputedStyle(myFilesRowContainer!).paddingRight || "0");
    const myFilesRowRight = myFilesRowContainer!.getBoundingClientRect().right - myFilesRowPaddingRight;
    const expandButtonRight = expandFolderButton.getBoundingClientRect().right;
    expect(Math.abs(myFilesRowRight - expandButtonRight)).toBeLessThanOrEqual(2);
    const lfsFolderButton = within(myFilesRowContainer!).getByRole("button", { name: "LFS" });
    expect((lfsFolderButton.compareDocumentPosition(expandFolderButton) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0).toBe(true);
    expect(canvas.queryByRole("button", { name: "Expand Empty Folder" })).toBeNull();
    expect(canvas.queryByText("Project-final.psd")).toBeNull();
    await userEvent.click(folderNameButton);
    expect(canvas.queryByText("Project-final.psd")).toBeNull();
    await expectBreadcrumbContains("My Files");
    expandFolderButton = await canvas.findByRole("button", { name: "Expand My Files" });
    expect(expandFolderButton.textContent).toBe(">");

    await userEvent.click(expandFolderButton);
    await expectBreadcrumbContains("My Files");

    const collapseFolderButton = await canvas.findByRole("button", { name: "Collapse My Files" });
    expect(collapseFolderButton.textContent).toBe("v");

    const nestedFile = await canvas.findByText("Project-final.psd");
    expect(nestedFile).toBeTruthy();

    await userEvent.click(collapseFolderButton);
    expect(canvas.queryByText("Project-final.psd")).toBeNull();
    await expectBreadcrumbContains("My Files");
  }),
};
