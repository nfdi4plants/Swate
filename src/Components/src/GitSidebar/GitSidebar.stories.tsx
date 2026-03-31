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
      onSelectChange={(change) => {
        setSelectedFile(change.Path);
        return props.onSelectChange(change);
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
    onRefresh: ok,
    onFetch: ok,
    onPull: ok,
    onPush: ok,
    onSync: ok,
    onCommitSelection: okWithSelection,
    onCommitAll: okWithMessage,
    onCreateBranch: okWithArg,
    onSwitchBranch: okWithBranch,
    onSelectChange: okWithArg,
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
    onRefresh: ok,
    onFetch: ok,
    onPull: ok,
    onPush: ok,
    onSync: ok,
    onCommitSelection: okWithSelection,
    onCommitAll: okWithMessage,
    onCreateBranch: okWithArg,
    onSwitchBranch: okWithBranch,
    onSelectChange: okWithArg,
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

export const ConflictsPresent: Story = {
  args: {
    status: {
      ...baseStatus,
      IsMergeInProgress: true,
    },
    changedFiles: conflictedFiles.slice(),
    branchOptions: branchOptions.slice(),
    onRefresh: ok,
    onFetch: ok,
    onPull: ok,
    onPush: ok,
    onSync: ok,
    onCommitSelection: okWithSelection,
    onCommitAll: okWithMessage,
    onCreateBranch: okWithArg,
    onSwitchBranch: okWithBranch,
    onSelectChange: okWithArg,
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
    busyNotice: "Pulling from origin/main",
    currentProgress: {
      Method: "pull",
      Stage: "Receiving objects",
      ProgressPercent: 54,
    },
    onRefresh: ok,
    onFetch: ok,
    onPull: ok,
    onPush: ok,
    onSync: ok,
    onCommitSelection: okWithSelection,
    onCommitAll: okWithMessage,
    onCreateBranch: okWithArg,
    onSwitchBranch: okWithBranch,
    onSelectChange: okWithArg,
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
    onRefresh: ok,
    onFetch: ok,
    onPull: ok,
    onPush: ok,
    onSync: ok,
    onCommitSelection: okWithSelection,
    onCommitAll: okWithMessage,
    onCreateBranch: okWithArg,
    onSwitchBranch: okWithBranch,
    onSelectChange: okWithArg,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const modal = within(document.body);
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
    onRefresh: ok,
    onFetch: ok,
    onPull: ok,
    onPush: ok,
    onSync: ok,
    onCommitSelection: okWithSelection,
    onCommitAll: okWithMessage,
    onCreateBranch: okWithArg,
    onSwitchBranch: okWithBranch,
    onSelectChange: okWithArg,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const modal = within(document.body);
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
    onRefresh: ok,
    onFetch: ok,
    onPull: ok,
    onPush: ok,
    onSync: ok,
    onCommitSelection: okWithSelection,
    onCommitAll: okWithMessage,
    onCreateBranch: okWithArg,
    onSwitchBranch: okWithBranch,
    onSelectChange: okWithArg,
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
    onRefresh: ok,
    onFetch: () =>
      fail("Fetch failed because the remote rejected the request."),
    onPull: ok,
    onPush: ok,
    onSync: ok,
    onCommitSelection: okWithSelection,
    onCommitAll: okWithMessage,
    onCreateBranch: okWithArg,
    onSwitchBranch: okWithBranch,
    onSelectChange: okWithArg,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await userEvent.click(canvas.getByTestId("GitSidebarFetchButton"));
    await expect(canvas.getByTestId("GitSidebarErrorNotice")).toHaveTextContent(
      "Fetch failed because the remote rejected the request.",
    );
  },
};
