import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fireEvent, fn, userEvent, within } from "storybook/test";
import { Main as GitSidebarComponent } from "./GitSidebar.fs.js";
import {
  FSharpResult$2_Ok,
} from "../fable_modules/fable-library-ts.5.0.0-alpha.21/Result.ts";

const noop = () => {};
const noopWithArg = (_arg: unknown) => {};
const noopWithMessage = (_message: string) => {};
const noopWithSelection = (_request: unknown) => {};
const noopWithPaths = (_paths: string[]) => {};
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
  OnUpdateFromOnline: noop,
  OnPrimarySaveSelection: noopWithSelection,
  OnPrimarySaveAll: noopWithMessage,
  OnCommitSelection: noopWithSelection,
  OnCommitAll: noopWithMessage,
  OnDiscardSelection: noopWithPaths,
  OnConfirmPendingRemoteAction: noop,
  OnCancelPendingRemoteAction: noop,
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
    await expect(
      canvas.getByTestId("GitSidebarChangedFilesVirtualContent"),
    ).toBeInTheDocument();
  },
};

export const MarkedSelectionWithoutLastClickedHighlight: Story = {
  render: (args) => <StatefulSidebar {...args} />,
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    selectedFile: "README.md",
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks(),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await expect(canvas.getByTestId("GitSidebarChangeRow-0")).not.toHaveClass("swt:border-primary/40");
    await expect(canvas.getByTestId("GitSidebarChangeRow-0")).not.toHaveClass("swt:bg-primary/5");

    await userEvent.click(canvas.getByTestId("GitSidebarChangeRow-1"));
    await expect(canvas.getByTestId("GitSidebarChangeRow-1")).toHaveClass("swt:border-success/40");
    await expect(canvas.getByTestId("GitSidebarChangeRow-1")).toHaveClass("swt:bg-success/10");

    await userEvent.click(canvas.getByTestId("GitSidebarChangeRow-1"));
    await expect(canvas.getByTestId("GitSidebarChangeRow-1")).not.toHaveClass("swt:border-success/40");
    await expect(canvas.getByTestId("GitSidebarChangeRow-1")).not.toHaveClass("swt:bg-success/10");
  },
};

export const SlowOpenDoesNotGreyRows: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks({
      OnSelectChange: () =>
        new Promise((resolve) => {
          window.setTimeout(() => resolve(FSharpResult$2_Ok<void, string>(undefined)), 250);
        }),
    }),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    await userEvent.click(canvas.getByTestId("GitSidebarChangeRow-0"));
    await expect(canvas.getByTestId("GitSidebarChangeRow-0")).not.toHaveClass("swt:opacity-60");
    await expect(canvas.getByTestId("GitSidebarChangeRow-1")).not.toHaveClass("swt:opacity-60");
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
    await userEvent.click(canvas.getByTestId("GitSidebarAdvancedActionsButton"));
    await expect(canvas.getByTestId("GitSidebarAdvancedActionsButton")).toHaveClass("swt:btn-primary");
    await expect(canvas.getByTestId("GitSidebarAdvancedActionsDivider")).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarUpdateArcButton")).toHaveTextContent("Update ARC from Online");
    await expect(canvas.queryByTestId("GitSidebarSyncButton")).toBeNull();
    await expect(canvas.queryByTestId("GitSidebarLocalCommitButton")).toBeNull();
    await expect(canvas.getByTestId("GitSidebarFetchButton")).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarPullButton")).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarPushButton")).toBeInTheDocument();
    const downloadLargeFilesCheckbox = canvas.getByTestId("GitSidebarDownloadLargeFilesCheckbox");
    const lfsThresholdInput = canvas.getByTestId("GitSidebarLfsThresholdInput");

    await expect(downloadLargeFilesCheckbox).toBeChecked();
    await expect(lfsThresholdInput).toHaveValue(1);
    await expect(
      Boolean(
        downloadLargeFilesCheckbox.compareDocumentPosition(lfsThresholdInput) &
          Node.DOCUMENT_POSITION_FOLLOWING,
      ),
    ).toBe(true);
  },
};

export const ActionTooltipsAndResponsiveLabels: Story = {
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

    await expect(canvas.getByTestId("GitSidebarUpdateArcButton")).toHaveAttribute(
      "title",
      "Update ARC from Online:\n- git fetch origin\n- git merge-tree (conflict preflight)\n- git pull origin",
    );
    await expect(canvas.getByTestId("GitSidebarUpdateArcButtonLabel")).toHaveClass("swt:truncate");
    await expect(canvas.getByTestId("GitSidebarUpdateArcButtonLabel")).toHaveClass(
      "swt:@max-3xs/gitSidebar:sr-only",
    );

    await userEvent.click(canvas.getByTestId("GitSidebarAdvancedActionsButton"));
    await expect(canvas.getByTestId("GitSidebarFetchButton")).toHaveAttribute(
      "title",
      "Check for Changes:\n- git fetch origin",
    );
    await expect(canvas.getByTestId("GitSidebarPullButton")).toHaveAttribute(
      "title",
      "Download Changes:\n- git pull origin",
    );
    await expect(canvas.getByTestId("GitSidebarPushButton")).toHaveAttribute(
      "title",
      "Upload Changes:\n- git push origin",
    );
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
    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-0")).toBeInTheDocument();
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
    await expect(
      canvas.getByTestId("GitSidebarChangedFilesVirtualContent"),
    ).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarChangeRow-0")).toBeInTheDocument();
    await expect(canvas.queryByTestId("GitSidebarChangeRow-399")).toBeNull();
    const scrollContainer = canvas.getByTestId(
      "GitSidebarChangedFilesScrollContainer",
    ) as HTMLElement;
    const maxScrollTop =
      scrollContainer.scrollHeight - scrollContainer.clientHeight;
    scrollContainer.scrollTop = maxScrollTop;
    await fireEvent.scroll(scrollContainer, {
      target: { scrollTop: maxScrollTop },
    });
    await expect(
      await canvas.findByTestId("GitSidebarChangeRow-399"),
    ).toBeInTheDocument();
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
    await expect(canvas.getByTestId("GitSidebar")).not.toHaveTextContent("git: D.");
    await expect(canvas.getByTestId("GitSidebar")).not.toHaveTextContent("Deleted");
    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-0")).toHaveClass("swt:text-error");
    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-0")).toHaveAttribute(
      "title",
      "Deleted:\n- git: D.",
    );
  },
};

export const ChangeStatusIconColors: Story = {
  args: {
    status: baseStatus,
    changedFiles: [
      {
        Path: "new-file.md",
        OriginalPath: undefined,
        IndexStatus: "A",
        WorkingTreeStatus: " ",
        IsConflicted: false,
      },
      {
        Path: "changed-file.md",
        OriginalPath: undefined,
        IndexStatus: "M",
        WorkingTreeStatus: " ",
        IsConflicted: false,
      },
      {
        Path: "deleted-file.md",
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

    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-0")).toHaveClass("swt:text-success");
    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-0")).toHaveClass(
      "swt:fluent--add-24-regular",
    );
    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-1")).toHaveClass("swt:text-warning");
    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-1")).toHaveClass(
      "swt:fluent--edit-24-regular",
    );
    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-2")).toHaveClass("swt:text-error");
    await expect(canvas.getByTestId("GitSidebarChangeStatusIcon-2")).toHaveClass(
      "swt:fluent--delete-24-regular",
    );
  },
};

export const LongWrappedFile: Story = {
  args: {
    status: baseStatus,
    changedFiles: [
      {
        Path: "src/very/long/path/that/wraps/in/the/sidebar/and/needs/a/fixed/status/icon.txt",
        OriginalPath: undefined,
        IndexStatus: "M",
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
    await expect(canvas.getByTestId("GitSidebarChangeRow-0")).toHaveClass("swt:items-center");
    await expect(canvas.getByTestId("GitSidebarChangeStatusSlot-0")).toHaveClass("swt:shrink-0");
    await expect(canvas.getByTestId("GitSidebarChangeStatusSlot-0")).toHaveClass("swt:self-center");
  },
};

const discardSelectionSpy = fn();

export const HoverDiscardChange: Story = {
  args: {
    status: baseStatus,
    changedFiles: changedFiles.slice(),
    branchOptions: branchOptions.slice(),
    callbacks: buildCallbacks({
      OnDiscardSelection: discardSelectionSpy,
    }),
    downloadLargeFiles: true,
    lfsAutoTrackThresholdMb: 1,
  },
  play: async ({ canvasElement }) => {
    discardSelectionSpy.mockClear();
    const canvas = within(canvasElement);

    const row = canvas.getByTestId("GitSidebarChangeRow-0");
    await expect(row).toHaveClass("swt:items-center");
    await expect(row).toHaveClass("swt:min-h-6");

    await userEvent.hover(row);
    await expect(canvas.getByTestId("GitSidebarDiscardChangeButton-0")).toBeInTheDocument();
    await expect(canvas.getByTestId("GitSidebarDiscardChangeButton-0")).toHaveClass("swt:opacity-0");
    await expect(canvas.getByTestId("GitSidebarDiscardChangeButton-0")).toHaveClass(
      "swt:group-hover:opacity-100",
    );

    await userEvent.click(canvas.getByTestId("GitSidebarDiscardChangeButton-0"));
    await expect(discardSelectionSpy).toHaveBeenCalledWith(["README.md"]);
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
    await expect(modal.getByTestId("GitSidebarSwitchBranchSelect")).toHaveTextContent(
      "origin/main",
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
    const modal = within(document.body);
    const legacyCard = canvas.getByTestId("GitSidebarCommitSection").firstElementChild as HTMLElement;
    await expect(legacyCard).not.toHaveClass("swt:rounded-box");
    await expect(legacyCard).not.toHaveClass("swt:border");
    await userEvent.click(canvas.getByTestId("GitSidebarChangeRow-0"));
    await expect(canvas.getByTestId("GitSidebarPrimarySaveButton")).toHaveTextContent("Save Selected Changes");
    await userEvent.click(canvas.getByTestId("GitSidebarSaveOptionsButton"));
    await expect(canvas.getByTestId("GitSidebarLocalCommitButton")).toHaveTextContent(
      "Add and commit selected Changes",
    );
    const sidebarBounds = canvas.getByTestId("GitSidebar").getBoundingClientRect();
    const menuBounds = canvas.getByTestId("GitSidebarSaveOptionsMenu").getBoundingClientRect();
    expect(menuBounds.left).toBeGreaterThanOrEqual(sidebarBounds.left);
    expect(menuBounds.right).toBeLessThanOrEqual(sidebarBounds.right);
    await expect(canvas.queryByTestId("GitSidebarCommitSelectionButton")).toBeNull();
    await expect(canvas.queryByTestId("GitSidebarCommitSelectionCheckbox-README.md")).toBeNull();
    // Click again to deselect – button text should switch back to "Save All Changes"
    await userEvent.click(canvas.getByTestId("GitSidebarChangeRow-0"));
    await expect(canvas.getByTestId("GitSidebarPrimarySaveButton")).toHaveTextContent("Save All Changes");
    await userEvent.click(canvas.getByTestId("GitSidebarSaveOptionsButton"));
    await expect(canvas.getByTestId("GitSidebarLocalCommitButton")).toHaveTextContent(
      "Add and commit all Changes",
    );
    await userEvent.click(canvas.getByTestId("GitSidebarSaveOptionsHelpButton"));
    await expect(await modal.findByTestId("popover_content_GitSidebarSaveOptionsHelp")).toHaveTextContent(
      "Save changes commits locally",
    );
    await expect(modal.getByTestId("popover_content_GitSidebarSaveOptionsHelp")).toHaveTextContent(
      "Add and commit changes only writes the local Git commit",
    );
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
      "Sign in to a DataHub account to use fetch, pull, push, or update.",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("GitSidebarUpdateArcButton")).toBeDisabled();
    await expect(
      canvas.getByTestId("GitSidebarRemoteAuthWarning"),
    ).toHaveTextContent(
      "Sign in to a DataHub account to use fetch, pull, push, or update.",
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
