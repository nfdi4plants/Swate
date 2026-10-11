import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fn, userEvent, within } from "storybook/test";
import { GitHistory } from "./GitHistory.fs.js";
import {
  GitHistoryCommit,
  GitHistoryCommitChanges,
  GitHistoryFileChange,
  GitHistoryChangeSummary,
} from "./Types.fs.js";

type Props = React.ComponentProps<typeof GitHistory>;
type ChangeKind = ConstructorParameters<typeof GitHistoryFileChange>[2];

const revisions = [
  "fb2e9a01f24061e6c8326cf8f889c76175d84be0",
  "e7416ab20d3b082328c144e0d279dfe58228704f",
  "934fb8c1a2cd4b49cb4af02cfd3a841117b17c00",
  "80d345173b56ebc12b8f98f9612bb25950f5a9c1",
  "61b0db11ba7af2c52437c7066b1342cfae54a33e",
];

const summaries = [
  new GitHistoryChangeSummary(1, 0, 2, 0, 0, 0),
  new GitHistoryChangeSummary(0, 0, 2, 0, 0, 0),
  new GitHistoryChangeSummary(0, 1, 0, 1, 1, 1),
  new GitHistoryChangeSummary(2, 0, 0, 0, 0, 0),
  new GitHistoryChangeSummary(1, 0, 0, 0, 0, 0),
];

function commit(index: number, message: string, author = "Maya Chen", merge = false) {
  const date = new Date(Date.now() - index * 86_400_000).toISOString();
  const parents = index + 1 < revisions.length ? [revisions[index + 1]] : [];
  if (merge) parents.push("cb4aef930bd02367b02b87660dcb4889e8172340");
  return new GitHistoryCommit(
    revisions[index], parents, message, author,
    `${author.toLowerCase().replaceAll(" ", ".")}@example.org`, date, date, summaries[index],
  );
}

const commits = [
  commit(0, "Document sample preparation and update study metadata\n\nAdds preparation steps and corrects organism annotations."),
  commit(1, "Merge reviewed assay annotations", "Alex Rivera", true),
  commit(2, "Rename the protocol and archive outdated notes"),
];

const olderCommits = [
  commit(3, "Add microscope images and sequencing results", "Alex Rivera"),
  commit(4, "Create the initial study structure"),
];

function file(
  path: string, kind: ChangeKind, insertions?: number, deletions?: number,
  previousPath?: string,
) {
  return new GitHistoryFileChange(path, previousPath, kind, insertions, deletions);
}

const changes = [
  new GitHistoryCommitChanges(revisions[0], [
    file("studies/s-metabolomics/protocols/sample-preparation.md", "modified", 24, 6),
    file("studies/s-metabolomics/isa.study.xlsx", "modified"),
    file("studies/s-metabolomics/resources/reagent-list.csv", "added", 18, 0),
  ], false, undefined),
  new GitHistoryCommitChanges(revisions[1], [
    file("assays/a-metabolomics/isa.assay.xlsx", "modified"),
    file("assays/a-metabolomics/README.md", "modified", 5, 2),
  ], false, undefined),
  new GitHistoryCommitChanges(revisions[2], [
    file("studies/s-metabolomics/protocols/extraction.md", "renamed", 2, 1,
      "studies/s-metabolomics/protocols/extraction-draft.md"),
    file("notes/old-measurements.md", "deleted", 0, 42),
    file("assays/a-metabolomics/protocols/extraction.md", "copied", 0, 0,
      "studies/s-metabolomics/protocols/extraction.md"),
    file("resources/instrument-config", "typeChanged", 0, 0),
  ], false, undefined),
  new GitHistoryCommitChanges(revisions[3], [
    file("studies/s-metabolomics/resources/microscope-image.png", "added"),
    file("assays/a-metabolomics/dataset/sequencing-results.csv", "added", 132, 0),
  ], false, undefined),
  new GitHistoryCommitChanges(revisions[4], [
    file("README.md", "added", 12, 0),
  ], false, undefined),
];

function InteractiveHistory({ width = 320, ...props }: Props & { width?: number }) {
  const [expanded, setExpanded] = React.useState(props.expandedRevisions);
  const [selectedRevision, setSelectedRevision] = React.useState(props.selectedRevision);
  const [selectedPath, setSelectedPath] = React.useState(props.selectedPath);
  const [showOlder, setShowOlder] = React.useState(false);
  const [historyError, setHistoryError] = React.useState(props.error);
  const [fileChanges, setFileChanges] = React.useState(props.changes);
  const [scrollTop, setScrollTop] = React.useState(props.scrollTop ?? 0);

  return (
    <div style={{ width, height: 640 }} className="swt:overflow-hidden swt:border swt:border-base-300 swt:bg-base-100 swt:p-2">
      <GitHistory
        {...props}
        commits={showOlder ? [...props.commits, ...olderCommits] : props.commits}
        changes={fileChanges}
        expandedRevisions={expanded}
        selectedRevision={selectedRevision}
        selectedPath={selectedPath}
        hasMore={props.hasMore && !showOlder}
        error={historyError}
        scrollTop={scrollTop}
        onScroll={setScrollTop}
        onToggleCommit={(revision) => {
          const failed = fileChanges.find((item) => item.Revision === revision && item.Error);
          if (failed) {
            setFileChanges((current) => current.map((item) => item.Revision === revision
              ? changes.find((loaded) => loaded.Revision === revision) ?? item
              : item));
          } else {
            const loaded = changes.find((item) => item.Revision === revision);
            if (!fileChanges.some((item) => item.Revision === revision) && loaded) {
              setFileChanges((current) => [...current, loaded]);
            }
            setExpanded((current) => current.includes(revision)
              ? current.filter((item) => item !== revision)
              : [...current, revision]);
          }
          props.onToggleCommit(revision);
        }}
        onSelectFile={(savedVersion, changedFile) => {
          setSelectedRevision(savedVersion.Revision);
          setSelectedPath(changedFile.Path);
          props.onSelectFile(savedVersion, changedFile);
        }}
        onLoadMore={() => {
          setShowOlder(true);
          props.onLoadMore();
        }}
        onRefresh={() => {
          setHistoryError(undefined);
          props.onRefresh();
        }}
      />
    </div>
  );
}

const meta = {
  title: "Page Components/GitHistory",
  component: GitHistory,
  tags: ["autodocs"],
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="swt:min-h-screen swt:bg-base-200 swt:p-4">
        <Story />
      </div>
    ),
  ],
  render: (args, context) => <InteractiveHistory {...args} width={context.parameters.historyWidth ?? 320} />,
  args: {
    commits,
    changes: changes.slice(0, 3),
    expandedRevisions: [revisions[0]],
    branchName: "main",
    hasMore: true,
    loading: false,
    onToggleCommit: fn(),
    onSelectFile: fn(),
    onLoadMore: fn(),
    onRefresh: fn(),
  },
} satisfies Meta<typeof GitHistory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Sidebar: Story = {
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const fileButton = canvas.getByRole("button", {
      name: "Modified: studies/s-metabolomics/protocols/sample-preparation.md",
    });
    await userEvent.click(fileButton);
    await expect(args.onSelectFile).toHaveBeenCalledWith(commits[0], changes[0].Files[0]);
    await expect(fileButton).toHaveAttribute("aria-pressed", "true");

    const subject = commits[0].Message.split("\n")[0];
    await userEvent.click(canvas.getByRole("button", { name: "Collapse saved version: " + subject }));
    await expect(canvas.queryByRole("button", { name: "Modified: studies/s-metabolomics/protocols/sample-preparation.md" })).not.toBeInTheDocument();
    await userEvent.click(canvas.getByRole("button", { name: "Expand saved version: " + subject }));
    await expect(canvas.getByRole("button", { name: "Modified: studies/s-metabolomics/protocols/sample-preparation.md" })).toHaveAttribute("aria-pressed", "true");

    await userEvent.click(canvas.getByRole("button", { name: "Load older versions" }));
    await expect(args.onLoadMore).toHaveBeenCalledTimes(1);
    await expect(canvas.getByRole("button", { name: "Expand saved version: Create the initial study structure" })).toBeInTheDocument();
    await userEvent.click(canvas.getByRole("button", { name: "Refresh history" }));
    await expect(args.onRefresh).toHaveBeenCalledTimes(1);
  },
};

export const DenseOverview: Story = {
  args: {
    commits: Array.from({ length: 30 }, (_, index) => new GitHistoryCommit(
      ((index + 10).toString(16).padStart(2, "0") + "f2e9a01").padEnd(40, "0"),
      [],
      [
        "Update sample annotations",
        "Review study metadata",
        "Document extraction protocol",
        "Add instrument settings",
        "Correct organism annotations",
      ][index % 5],
      index % 2 ? "Alex Rivera" : "Maya Chen",
      "researcher@example.org",
      new Date(Date.now() - index * 86_400_000).toISOString(),
      new Date(Date.now() - index * 86_400_000).toISOString(),
      new GitHistoryChangeSummary(index % 3, 0, 1, 0, 0, 0),
    )),
    changes: [],
    expandedRevisions: [],
    hasMore: false,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const sidebar = canvas.getByRole("region", { name: "Git history" });
    const bottom = sidebar.getBoundingClientRect().bottom;
    const visibleCommits = canvas.getAllByRole("button", { name: /^Expand saved version:/ })
      .filter((button) => button.getBoundingClientRect().bottom <= bottom);
    await expect(visibleCommits.length).toBeGreaterThanOrEqual(10);
  },
};

export const NarrowSidebar: Story = {
  parameters: { historyWidth: 260 },
  args: { branchName: "feature/review-annotations", expandedRevisions: revisions.slice(0, 3) },
  play: async ({ canvasElement }) => {
    const sidebar = within(canvasElement).getByRole("region", { name: "Git history" });
    await expect(sidebar.scrollWidth).toBeLessThanOrEqual(sidebar.clientWidth);
    for (const button of within(sidebar).getAllByRole("button")) {
      await expect(button.scrollWidth).toBeLessThanOrEqual(button.clientWidth);
    }
  },
};

export const CollapsedSummaries: Story = {
  args: { changes: [], expandedRevisions: [] },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByRole("img", { name: "1 added file" })).toBeInTheDocument();
    await expect(canvas.getAllByRole("img", { name: "2 modified files" })).toHaveLength(2);
    await expect(canvas.getByRole("img", { name: "1 deleted file" })).toBeInTheDocument();
    await expect(canvas.getByRole("img", { name: "1 renamed file" })).toBeInTheDocument();
    await expect(canvas.getByRole("img", { name: "1 copied file" })).toBeInTheDocument();
    await expect(canvas.getByRole("img", { name: "1 type changed file" })).toBeInTheDocument();
    await expect(canvas.queryByRole("list", { name: "Changed files" })).not.toBeInTheDocument();
    await userEvent.click(canvas.getByRole("button", { name: "Load older versions" }));
    await expect(canvas.getByRole("img", { name: "2 added files" })).toBeInTheDocument();
  },
};

export const SixKindsInNarrowSidebar: Story = {
  parameters: { historyWidth: 260 },
  args: {
    commits: [new GitHistoryCommit(
      revisions[0], [], "Review all file changes", "Maya Chen", "maya@example.org",
      new Date().toISOString(), new Date().toISOString(),
      new GitHistoryChangeSummary(12, 23, 34, 45, 56, 67),
    )],
    changes: [], expandedRevisions: [], hasMore: false,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getAllByRole("img")).toHaveLength(6);
    const button = canvas.getByRole("button", { name: "Expand saved version: Review all file changes" });
    await expect(button.scrollWidth).toBeLessThanOrEqual(button.clientWidth);
  },
};

export const EmptyAndUnavailableSummaries: Story = {
  args: {
    commits: [
      new GitHistoryCommit(revisions[0], [], "Empty saved version", "Maya Chen", "maya@example.org",
        new Date().toISOString(), new Date().toISOString(), new GitHistoryChangeSummary(0, 0, 0, 0, 0, 0)),
      new GitHistoryCommit(revisions[1], [], "Summary unavailable", "Maya Chen", "maya@example.org",
        new Date().toISOString(), new Date().toISOString(), undefined),
    ],
    changes: [], expandedRevisions: [], hasMore: false,
  },
};

export const RenamedCopiedDeletedAndTypeChanged: Story = {
  args: {
    expandedRevisions: [revisions[2]],
    selectedRevision: revisions[2],
    selectedPath: "studies/s-metabolomics/protocols/extraction.md",
  },
};

export const MergeAndBinaryFiles: Story = {
  args: { expandedRevisions: [revisions[1]], hasMore: false },
};

export const LoadingHistory: Story = {
  args: { commits: [], changes: [], expandedRevisions: [], loading: true, hasMore: false },
};

export const LoadingOlderVersions: Story = { args: { loading: true } };

export const NoSavedVersions: Story = {
  args: { commits: [], changes: [], expandedRevisions: [], hasMore: false },
};

export const HistoryError: Story = {
  args: { error: "The history request was interrupted. Your existing saved versions are still available." },
};

export const ChangedFilesError: Story = {
  args: {
    changes: [
      new GitHistoryCommitChanges(revisions[0], [], false,
        "The changed-file list could not be read. Retry to load this version again."),
      ...changes.slice(1, 3),
    ],
  },
};

export const LoadingChangedFiles: Story = {
  args: {
    changes: [new GitHistoryCommitChanges(revisions[0], [], true, undefined), ...changes.slice(1, 3)],
  },
};
