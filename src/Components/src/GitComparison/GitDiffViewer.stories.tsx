import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, waitFor, fireEvent } from "storybook/test";
import { Viewer as GitDiffViewerComponent } from "./GitDiffViewer.fs.js";

const introLines = Array.from({ length: 14 }, (_, index) => `Context line ${index + 1}`);
const checkpointLines = Array.from({ length: 16 }, (_, index) => `Checkpoint item ${index + 1}`);
const followUpLines = Array.from({ length: 14 }, (_, index) => `Follow-up note ${index + 1}`);

const previousLines = [
  "# Protocol Overview",
  ...introLines,
  "Step A",
  "Remove me",
  ...checkpointLines,
  "Temperature 20 C",
  "Observation pending",
  ...followUpLines,
  "Tail note",
  "Footer",
];

const currentLines = [
  "# Protocol Overview",
  ...introLines,
  "Step A updated",
  ...checkpointLines,
  "Temperature 24 C",
  "Observation pending",
  "Added after shared",
  ...followUpLines,
  "Tail note refined",
  "Footer",
];

const previousContent = `${previousLines.join("\n")}\n`;
const currentContent = `${currentLines.join("\n")}\n`;

const stepOldStart = previousLines.indexOf("Step A") + 1;
const stepNewStart = currentLines.indexOf("Step A updated") + 1;
const temperatureOldStart = previousLines.indexOf("Temperature 20 C") + 1;
const temperatureNewStart = currentLines.indexOf("Temperature 24 C") + 1;
const insertAnchor = previousLines.indexOf("Observation pending") + 1;
const insertNewStart = currentLines.indexOf("Added after shared") + 1;
const tailOldStart = previousLines.indexOf("Tail note") + 1;
const tailNewStart = currentLines.indexOf("Tail note refined") + 1;

const wordDiffText = `diff --git a/notes/protocol.md b/notes/protocol.md
index 1111111..3333333 100644
--- a/notes/protocol.md
+++ b/notes/protocol.md
@@ -${stepOldStart},2 +${stepNewStart} @@
 Step A 
-Remove me
+updated
~
@@ -${temperatureOldStart} +${temperatureNewStart} @@
-Temperature 20
+Temperature 24
 C
~
@@ -${insertAnchor},0 +${insertNewStart} @@ Observation pending
+Added after shared
~
@@ -${tailOldStart} +${tailNewStart} @@
-Tail note
+Tail note refined
~
`;

function buildAddedFileWordDiffText(path: string, lines: string[]) {
  return [
    "new file mode 100644",
    "--- /dev/null",
    `+++ b/${path}`,
    `@@ -0,0 +1,${lines.length} @@`,
    ...lines.map((line) => `+${line}`),
    "~",
    "",
  ].join("\n");
}

const largeDiffLines = Array.from(
  { length: 600 },
  (_, index) => `Generated large diff line ${index + 1}`,
);
const largeDiffPath = "notes/large-diff.txt";
const largeDiffContent = `${largeDiffLines.join("\n")}\n`;
const largeDiffWordDiffText = buildAddedFileWordDiffText(
  largeDiffPath,
  largeDiffLines,
);

const meta = {
  title: "Components/GitComparison/GitDiffViewer",
  component: GitDiffViewerComponent,
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen",
    docs: {
      description: {
        component:
          "This component expects the machine-readable word diff string together with the full previous/current file contents, because the diff alone still omits unchanged regions outside the hunks.",
      },
    },
  },
  decorators: [
    (Story) => (
      <div className="swt:h-[80vh] swt:min-h-[48rem] swt:bg-base-200 swt:p-6">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof GitDiffViewerComponent>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    wordDiffText,
    previousContent,
    currentContent,
    testIdPrefix: "git-diff-story",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const root = canvas.getByTestId("git-diff-story-root");
    const comparisonScroll = canvas.getByTestId("git-diff-story-comparison-scroll") as HTMLElement;

    await expect(canvas.getByTestId("git-diff-story-previous-header")).toHaveTextContent("notes/protocol.md");
    await expect(canvas.getByTestId("git-diff-story-current-header")).toHaveTextContent("notes/protocol.md");
    await expect(
      canvas.getByTestId("git-diff-story-comparison-scroll-virtual-content"),
    ).toBeInTheDocument();
    await expect(root).toHaveTextContent("Protocol Overview");
    await expect(root).toHaveTextContent("Context line 14");
    await expect(root).toHaveTextContent("Checkpoint item 16");
    await expect(root).toHaveTextContent("Temperature 24 C");
    await expect(root).toHaveTextContent("Step A updated");
    await expect(root).toHaveTextContent("Remove me");
    await expect(root).toHaveTextContent("Added after shared");

    await waitFor(() => {
      expect(comparisonScroll.clientHeight).toBeGreaterThan(0);
      expect(comparisonScroll.scrollHeight).toBeGreaterThan(comparisonScroll.clientHeight);
    });

    const maxScrollTop = comparisonScroll.scrollHeight - comparisonScroll.clientHeight;
    comparisonScroll.scrollTop = maxScrollTop;
    await fireEvent.scroll(comparisonScroll, {
      target: { scrollTop: maxScrollTop },
    });

    await expect(await canvas.findByText("Tail note refined")).toBeInTheDocument();
  },
};

export const LargeAddedFileVirtualized: Story = {
  args: {
    wordDiffText: largeDiffWordDiffText,
    previousContent: "",
    currentContent: largeDiffContent,
    testIdPrefix: "git-diff-large-story",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const comparisonScroll = canvas.getByTestId(
      "git-diff-large-story-comparison-scroll",
    ) as HTMLElement;

    await expect(
      canvas.getByTestId("git-diff-large-story-comparison-scroll-virtual-content"),
    ).toBeInTheDocument();
    await expect(
      canvas.getByTestId("git-diff-large-story-comparison-scroll-row-0"),
    ).toBeInTheDocument();
    await expect(
      canvas.queryByTestId("git-diff-large-story-comparison-scroll-row-599"),
    ).toBeNull();

    const maxScrollTop = comparisonScroll.scrollHeight - comparisonScroll.clientHeight;
    comparisonScroll.scrollTop = maxScrollTop;
    await fireEvent.scroll(comparisonScroll, {
      target: { scrollTop: maxScrollTop },
    });

    await expect(
      await canvas.findByTestId("git-diff-large-story-comparison-scroll-row-599"),
    ).toBeInTheDocument();
    await expect(
      await canvas.findByText("Generated large diff line 600"),
    ).toBeInTheDocument();
  },
};

const quotedPath = "notes/my protocol.md";
const quotedPreviousContent = "Quoted previous line\n";
const quotedCurrentContent = "Quoted current line\n";
const quotedPathWordDiffText = `diff --git "a/${quotedPath}" "b/${quotedPath}"
index 2222222..3333333 100644
--- "a/${quotedPath}"
+++ "b/${quotedPath}"
@@ -1 +1 @@
-Quoted previous line
+Quoted current line
~
`;

const createdFilePath = "notes/new file.md";
const createdFileWordDiffText = `new file mode 100644
--- /dev/null
+++ b/${createdFilePath}
@@ -0,0 +1 @@
+Fresh content
~
`;

const deletedFilePath = "notes/obsolete file.md";
const deletedFileWordDiffText = `deleted file mode 100644
--- a/${deletedFilePath}
+++ /dev/null
@@ -1 +0,0 @@
-Remove obsolete content
~
`;

const renamedPreviousPath = "notes/old name.md";
const renamedCurrentPath = "notes/new name.md";
const renamedFileWordDiffText = `diff --git "a/${renamedPreviousPath}" "b/${renamedCurrentPath}"
similarity index 100%
rename from ${renamedPreviousPath}
rename to ${renamedCurrentPath}
`;

function MetadataVariants() {
  return (
    <div className="swt:flex swt:flex-col swt:gap-6">
      <GitDiffViewerComponent
        wordDiffText={quotedPathWordDiffText}
        previousContent={quotedPreviousContent}
        currentContent={quotedCurrentContent}
        testIdPrefix="git-diff-quoted"
      />
      <GitDiffViewerComponent
        wordDiffText={createdFileWordDiffText}
        previousContent=""
        currentContent={"Fresh content\n"}
        testIdPrefix="git-diff-created"
      />
      <GitDiffViewerComponent
        wordDiffText={deletedFileWordDiffText}
        previousContent={"Remove obsolete content\n"}
        currentContent=""
        testIdPrefix="git-diff-deleted"
      />
      <GitDiffViewerComponent
        wordDiffText={renamedFileWordDiffText}
        previousContent={"Rename only content\n"}
        currentContent={"Rename only content\n"}
        testIdPrefix="git-diff-renamed"
      />
    </div>
  );
}

export const MetadataEdgeCases: Story = {
  render: () => <MetadataVariants />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByTestId("git-diff-quoted-previous-header")).toHaveTextContent(quotedPath);
    await expect(canvas.getByTestId("git-diff-quoted-current-header")).toHaveTextContent(quotedPath);

    await expect(canvas.getByTestId("git-diff-created-previous-header")).toHaveTextContent("Previous version");
    await expect(canvas.getByTestId("git-diff-created-current-header")).toHaveTextContent(createdFilePath);

    await expect(canvas.getByTestId("git-diff-deleted-previous-header")).toHaveTextContent(deletedFilePath);
    await expect(canvas.getByTestId("git-diff-deleted-current-header")).toHaveTextContent("Current version");

    await expect(canvas.getByTestId("git-diff-renamed-previous-header")).toHaveTextContent(renamedPreviousPath);
    await expect(canvas.getByTestId("git-diff-renamed-current-header")).toHaveTextContent(renamedCurrentPath);
  },
};
