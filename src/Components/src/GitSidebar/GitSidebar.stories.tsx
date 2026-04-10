import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, userEvent, within } from "storybook/test";
import { Main as GitSidebarComponent } from "./GitSidebar.fs.js";
import {
  FSharpResult$2_Ok,
} from "../fable_modules/fable-library-ts.5.0.0-alpha.21/Result.ts";

const noop = () => {};
const noopWithArg = (_arg: unknown) => {};
const noopWithMessage = (_message: string) => {};
const noopWithSelection = (_request: unknown) => {};
const noopWithBranch = (_branchName: string) => {};
const noopWithThreshold = (_thresholdMb: number) => {};
const noopWithDownloadPreference = (_downloadLargeFiles: boolean) => {};
const noopSelectChange = (_change: unknown) =>
  Promise.resolve(FSharpResult$2_Ok<void, string>(undefined));

const baseCallbacks = {
  OnRefresh: noop,
  OnFetch: noop,
  OnPull: noop,
  OnPush: noop,
  OnSync: noop,
  OnCommitSelection: noopWithSelection,
  OnCommitAll: noopWithMessage,
  OnSaveDownloadLargeFiles: noopWithDownloadPreference,
  OnSaveLfsAutoTrackThreshold: noopWithThreshold,
  OnCreateBranch: noopWithArg,
  OnSwitchBranch: noopWithBranch,
  OnSelectChange: noopSelectChange,
};

const buildCallbacks = (
  overrides: Partial<typeof baseCallbacks> = {},
) => ({
  ...baseCallbacks,
  ...overrides,
});

const buildBusyRunStatus = (notice: string) => ({
  // Fable DU: GitSidebarRunStatus.Busy
  tag: 1,
  fields: [notice],
});

const buildProgressRunStatus = (progress: {
  Method: string;
  Stage: string;
  ProgressPercent: number;
}) => ({
  // Fable DU: GitSidebarRunStatus.Progress
  tag: 2,
  fields: [progress],
});

const baseStatus = {
  CurrentBranch: "feature/git-sidebar",
  TrackingBranch: "origin/feature/git-sidebar",
  Ahead: 2,
  Behind: 1,
  IsClean: false,
  IsMergeInProgress: false,
};

const branchOptions = [
  {
    RefName: "feature/git-sidebar",
    DisplayLabel: "feature/git-sidebar",
    Kind: "Local",
    IsCurrent: true,
    IsTracking: false,
  },
  {
    RefName: "main",
    DisplayLabel: "main",
    Kind: "Local",
    IsCurrent: false,
    IsTracking: false,
  },
  {
    RefName: "origin/main",
    DisplayLabel: "origin/main",
    Kind: "Remote",
    IsCurrent: false,
    IsTracking: true,
  },
] as const;

const changedFiles = [
  {
    Path: "README.md",
    OriginalPath: undefined,
    IndexStatus: "M",
    WorkingTreeStatus: " ",
    IsConflicted: false,
  },
  {
    Path: "assays/isa.assay.xlsx",
    OriginalPath: undefined,
    IndexStatus: "?",
    WorkingTreeStatus: "?",
    IsConflicted: false,
  },
  {
    Path: "notes/protocol.md",
    OriginalPath: "notes/protocol-draft.md",
    IndexStatus: "R",
    WorkingTreeStatus: "M",
    IsConflicted: false,
  },
] as const;

const conflictedFiles = [
  ...changedFiles,
  {
    Path: "studies/s-study-01/protocol.md",
    OriginalPath: undefined,
    IndexStatus: "U",
    WorkingTreeStatus: "U",
    IsConflicted: true,
  },
] as const;

const largeChangedFiles = Array.from({ length: 400 }, (_, index) => ({
  Path: `src/file-${String(index).padStart(3, "0")}.txt`,
  OriginalPath: undefined,
  IndexStatus: "M",
  WorkingTreeStatus: " ",
  IsConflicted: false,
}));

function StatefulSidebar(
  props: React.ComponentProps<typeof GitSidebarComponent>,
) {
  const [selectedFile, setSelectedFile] = React.useState<string | undefined>(
    props.selectedFile,
  );

  return (
    <GitSidebarComponent
      {...props}
      selectedFile={selectedFile}
      callbacks={{
        ...props.callbacks,
        OnSelectChange: (change) => {
          setSelectedFile(change.Path);
          return props.callbacks.OnSelectChange(change);
        },
      }}
    />
  );
}

const meta = {
  title: "Components/GitSidebar/GitSidebar",
  component: GitSidebarComponent,
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="swt:h-[760px] swt:w-[340px] swt:bg-base-200 swt:p-4">
        <Story />
      </div>
    ),
  ],
  render: (args) => <StatefulSidebar {...args} />,
} satisfies Meta<typeof GitSidebarComponent>;

export default meta;
type Story = StoryObj<typeof meta>;

export const CleanRepo: Story = {
  args: {
    status: {
      ...baseStatus,
      IsClean: true,
      Ahead: 0,
      Behind: 0,
    },
    changedFiles: [],
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebar")).toHaveTextContent(
      "No changed files. Your repository is in sync.",
    );
  },
};

export const ChangedFiles: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    selectedFile: "README.md",
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebar")).toHaveTextContent(
      "assays/isa.assay.xlsx",
    );
    await expect(canvas.getByTestId("GitSidebar")).toHaveTextContent(
      "Renamed from notes/protocol-draft.md",
    );
  },
};

export const AdvancedActions: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebarDownloadLargeFilesCheckbox")).toBeChecked();
    await userEvent.click(canvas.getByTestId("GitSidebarAdvancedActionsButton"));
    await expect(canvas.getByTestId("GitSidebarAdvancedActionsButton")).toHaveClass("swt:btn-primary");
    await expect(canvas.getByTestId("GitSidebarAdvancedActionsDivider")).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarFetchButton")).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarPullButton")).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarPushButton")).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarLfsThresholdInput")).toHaveValue(1);
  },
};

export const ConflictsPresent: Story = {
  args: {
    status: {
      ...baseStatus,
      IsMergeInProgress: true,
    },
    changedFiles: [
      conflictedFiles[3],
      conflictedFiles[0],
      conflictedFiles[1],
      conflictedFiles[2],
    ],
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebarMergeBanner")).toHaveTextContent(
      "Resolve all conflicted files before pushing.",
    );
    await expect(canvasElement.querySelectorAll("[data-testid^='GitSidebarChangeRow-']")).toHaveLength(4);
    await expect(canvas.getByTestId("GitSidebar")).toHaveTextContent("Conflict");
  },
};

export const LargeChangedSet: Story = {
  args: {
    status: baseStatus,
    changedFiles: largeChangedFiles,
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebar")).toHaveTextContent("400 files");
    await expect(canvas.getByTestId("GitSidebarChangeRow-0")).toBeInTheDocument();
    await expect(canvas.queryByTestId("GitSidebarChangeRow-399")).toBeNull();
  },
};

export const DeletedFile: Story = {
  args: {
    status: baseStatus,
    changedFiles: [
      {
        Path: "obsolete.md",
        OriginalPath: undefined,
        IndexStatus: "D",
        WorkingTreeStatus: " ",
        IsConflicted: false,
      },
    ],
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebar")).toHaveTextContent("Deleted");
    await expect(canvas.getByTestId("GitSidebar")).toHaveTextContent("git: D.");
  },
};

export const BusyProgressState: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    runStatus: buildProgressRunStatus({
      Method: "pull",
      Stage: "Receiving objects",
      ProgressPercent: 54,
    }),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebarProgressNotice")).toHaveTextContent(
      "pull | Receiving objects | 54%",
    );
  },
};

export const BusyNoticeState: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    runStatus: buildBusyRunStatus("Pulling from origin/main"),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebarProgressNotice")).toHaveTextContent(
      "Pulling from origin/main",
    );
  },
};

export const CreateBranchModal: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const modal = within(document.body);
    await userEvent.click(canvas.getByTestId("GitSidebarAdvancedActionsButton"));
    await userEvent.click(canvas.getByTestId("GitSidebarCreateBranchButton"));
    const branchNameInput = await modal.findByTestId(
      "GitSidebarBranchNameInput",
    );
    await userEvent.type(
      branchNameInput,
      "feature/new-branch",
    );
    await expect(modal.getByTestId("GitSidebarStartPointSelect")).toHaveTextContent(
      "origin/main",
    );
  },
};

export const SwitchBranchModal: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const modal = within(document.body);
    await userEvent.click(canvas.getByTestId("GitSidebarAdvancedActionsButton"));
    await userEvent.click(canvas.getByTestId("GitSidebarSwitchBranchButton"));
    await expect(modal.getByTestId("GitSidebarSwitchBranchSelect")).toHaveTextContent(
      "main",
    );
    await userEvent.click(modal.getByTestId("GitSidebarSwitchBranchSubmit"));
  },
};

export const CommitComposer: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const commitMessageInput = canvas.getByTestId("GitSidebarCommitMessageInput");
    await userEvent.type(commitMessageInput, "Add sidebar commit action");
    await userEvent.click(
      canvas.getByTestId("GitSidebarCommitSelectionCheckbox-README.md"),
    );
    await userEvent.click(canvas.getByTestId("GitSidebarCommitSelectionButton"));
    await expect(canvas.getByTestId("GitSidebarCommitMessageInput")).toHaveValue("");
  },
};

export const RemoteActionsDisabled: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
    remoteActionsEnabled: false,
    remoteActionsWarning:
      "Sign in to a DataHub account to use fetch, pull, push, or sync.",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebarSyncButton")).toBeDisabled();
    await expect(
      canvas.getByTestId("GitSidebarRemoteAuthWarning"),
    ).toHaveTextContent(
      "Sign in to a DataHub account to use fetch, pull, push, or sync.",
    );
  },
};

export const GlobalErrorState: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    errorNotice: "Fetch failed because the remote rejected the request.",
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
};
