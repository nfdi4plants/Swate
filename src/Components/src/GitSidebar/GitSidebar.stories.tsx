import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, userEvent, within } from "storybook/test";
import { Main as GitSidebarComponent } from "./GitSidebar.fs.js";
import {
  FSharpResult$2_Error$,
  FSharpResult$2_Ok,
} from "../fable_modules/fable-library-ts.5.0.0-alpha.21/Result.ts";

const ok = () => Promise.resolve(FSharpResult$2_Ok<void, string>(undefined));
const fail = (message: string) =>
  Promise.resolve(FSharpResult$2_Error$<void, string>(message));
const okWithArg = (_arg: unknown) => ok();
const okWithMessage = (_message: string) => ok();
const okWithSelection = (_request: unknown) => ok();
const okWithBranch = (_branchName: string) => ok();
const okWithThreshold = (_thresholdMb: number) => ok();
const okWithDownloadPreference = (_downloadLargeFiles: boolean) => ok();

const baseCallbacks = {
  OnRefresh: ok,
  OnFetch: ok,
  OnPull: ok,
  OnPush: ok,
  OnSync: ok,
  OnCommitSelection: okWithSelection,
  OnCommitAll: okWithMessage,
  OnSaveDownloadLargeFiles: okWithDownloadPreference,
  OnSaveLfsAutoTrackThreshold: okWithThreshold,
  OnCreateBranch: okWithArg,
  OnSwitchBranch: okWithBranch,
  OnSelectChange: okWithArg,
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
    changedFiles: conflictedFiles.slice(),
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
    await expect(canvas.getByTestId("GitSidebar")).toHaveTextContent("Conflict");
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

export const CallbackErrorHandling: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks({
      OnFetch: () =>
        fail("Fetch failed because the remote rejected the request."),
    }),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByTestId("GitSidebarAdvancedActionsButton"));
    await userEvent.click(canvas.getByTestId("GitSidebarFetchButton"));
    await expect(canvas.getByTestId("GitSidebarErrorNotice")).toHaveTextContent(
      "Fetch failed because the remote rejected the request.",
    );
  },
};
