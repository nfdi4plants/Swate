import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, userEvent, waitFor, fn, fireEvent } from "storybook/test";
import { Viewer as GitMergeConflictViewerComponent } from "./GitMergeConflictViewer.fs.js";

const introLines = Array.from({ length: 10 }, (_, index) => `Shared intro line ${index + 1}`);
const currentExperimentLines = Array.from({ length: 32 }, (_, index) => `Current experiment note ${index + 1}`);
const incomingExperimentLines = Array.from({ length: 32 }, (_, index) => `Incoming experiment note ${index + 1}`);
const middleLines = Array.from({ length: 8 }, (_, index) => `Shared middle line ${index + 1}`);
const currentTemperatureLines = Array.from({ length: 10 }, (_, index) =>
  index === 4 ? "Current temperature 24 C" : `Current measurement note ${index + 1}`
);
const incomingTemperatureLines = Array.from({ length: 10 }, (_, index) =>
  index === 4 ? "Incoming temperature 20 C" : `Incoming measurement note ${index + 1}`
);
const checklistLines = Array.from({ length: 6 }, (_, index) => `Checklist line ${index + 1}`);
const currentClosingLines = ["Current closing summary", ...Array.from({ length: 8 }, (_, index) => `Current closing note ${index + 1}`)];
const incomingClosingLines = [
  "Incoming closing summary",
  "Incoming extra context",
  ...Array.from({ length: 8 }, (_, index) => `Incoming closing note ${index + 1}`),
];
const largeCurrentConflictLines = Array.from(
  { length: 240 },
  (_, index) => `Large current conflict line ${index + 1}`,
);
const largeIncomingConflictLines = Array.from(
  { length: 240 },
  (_, index) => `Large incoming conflict line ${index + 1}`,
);

const largeConflictContent = [
  "<<<<<<< HEAD",
  ...largeCurrentConflictLines,
  "=======",
  ...largeIncomingConflictLines,
  ">>>>>>> origin/main",
  "",
].join("\n");

const mergeConflictContent = [
  "# Merge plan",
  ...introLines,
  "<<<<<<< HEAD",
  ...currentExperimentLines,
  "=======",
  ...incomingExperimentLines,
  ">>>>>>> origin/main",
  ...middleLines,
  "<<<<<<< HEAD",
  ...currentTemperatureLines,
  "=======",
  ...incomingTemperatureLines,
  ">>>>>>> lab/main",
  ...checklistLines,
  "<<<<<<< HEAD",
  ...currentClosingLines,
  "=======",
  ...incomingClosingLines,
  ">>>>>>> feature/review",
  "Shared outro",
].join("\n") + "\n";

const fullyResolvedContent = [
  "# Merge plan",
  ...introLines,
  ...incomingExperimentLines,
  ...middleLines,
  ...currentTemperatureLines,
  ...checklistLines,
  ...incomingClosingLines,
  "Shared outro",
].join("\n") + "\n";

const duplicateConflictContent = [
  "Before duplicate block",
  "<<<<<<< HEAD",
  "Current duplicate line",
  "=======",
  "Incoming duplicate line",
  ">>>>>>> origin/main",
  "Between duplicate blocks",
  "<<<<<<< HEAD",
  "Current duplicate line",
  "=======",
  "Incoming duplicate line",
  ">>>>>>> origin/main",
  "After duplicate block",
].join("\n") + "\n";

type ControlledMergeHarnessProps = Omit<
  React.ComponentProps<typeof GitMergeConflictViewerComponent>,
  "resolvedContent" | "defaultResolvedContent"
> & {
  initialResolvedContent?: string;
};

type ExternalReplacementHarnessProps = ControlledMergeHarnessProps & {
  replacementResolvedContent: string;
};

function ControlledMergeHarness({
  initialResolvedContent,
  onResolvedContentChange,
  ...viewerProps
}: ControlledMergeHarnessProps) {
  const [resolvedContent, setResolvedContent] = React.useState(
    initialResolvedContent ?? viewerProps.mergeConflictContent
  );

  return (
    <GitMergeConflictViewerComponent
      {...viewerProps}
      resolvedContent={resolvedContent}
      onResolvedContentChange={(nextResolvedContent: string) => {
        setResolvedContent(nextResolvedContent);
        onResolvedContentChange?.(nextResolvedContent);
      }}
    />
  );
}

function DelayedControlledMergeHarness({
  initialResolvedContent,
  onResolvedContentChange,
  ...viewerProps
}: ControlledMergeHarnessProps) {
  const [resolvedContent, setResolvedContent] = React.useState(
    initialResolvedContent ?? viewerProps.mergeConflictContent
  );
  const pendingUpdateRef = React.useRef<number | null>(null);

  React.useEffect(() => {
    return () => {
      if (pendingUpdateRef.current !== null) {
        window.clearTimeout(pendingUpdateRef.current);
      }
    };
  }, []);

  return (
    <GitMergeConflictViewerComponent
      {...viewerProps}
      resolvedContent={resolvedContent}
      onResolvedContentChange={(nextResolvedContent: string) => {
        if (pendingUpdateRef.current !== null) {
          window.clearTimeout(pendingUpdateRef.current);
        }

        pendingUpdateRef.current = window.setTimeout(() => {
          setResolvedContent(nextResolvedContent);
          pendingUpdateRef.current = null;
        }, 75);

        onResolvedContentChange?.(nextResolvedContent);
      }}
    />
  );
}

function ExternalReplacementHarness({
  initialResolvedContent,
  replacementResolvedContent,
  onResolvedContentChange,
  testIdPrefix,
  ...viewerProps
}: ExternalReplacementHarnessProps) {
  const [resolvedContent, setResolvedContent] = React.useState(
    initialResolvedContent ?? viewerProps.mergeConflictContent
  );

  return (
    <>
      <button
        data-testid={`${testIdPrefix}-replace-external`}
        onClick={() => {
          setResolvedContent(replacementResolvedContent);
        }}
      >
        Replace externally
      </button>
      <GitMergeConflictViewerComponent
        {...viewerProps}
        testIdPrefix={testIdPrefix}
        resolvedContent={resolvedContent}
        onResolvedContentChange={(nextResolvedContent: string) => {
          setResolvedContent(nextResolvedContent);
          onResolvedContentChange?.(nextResolvedContent);
        }}
      />
    </>
  );
}

const meta = {
  title: "Components/GitComparison/GitMergeConflictViewer",
  component: GitMergeConflictViewerComponent,
  tags: ["autodocs"],
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story) => (
      <div className="swt:h-[80vh] swt:min-h-[48rem] swt:bg-base-200 swt:p-6">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof GitMergeConflictViewerComponent>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  parameters: { isolated: true },
  args: {
    mergeConflictContent,
    onConfirmMerge: fn(),
    testIdPrefix: "git-merge-story",
  },
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);
    const hasExactText = (value: string) => (_content: string, element: Element | null) =>
      element?.textContent === value;
    const root = canvas.getByTestId("git-merge-story-root");
    const firstConflictScroll = canvas.getByTestId("git-merge-story-conflict-1-scroll") as HTMLElement;
    const conflictsPane = canvas.getByTestId("git-merge-story-conflicts-pane");
    const splitHandle = canvas.getByTestId("git-merge-story-split-handle");
    const confirmMergeButton = canvas.getByTestId("git-merge-story-confirm-merge");

    await expect(canvas.getByText("Merge conflict 1")).toBeInTheDocument();
    await expect(canvas.getByText("Merge conflict 2")).toBeInTheDocument();
    await expect(canvas.getByText("Merge conflict 3")).toBeInTheDocument();
    await expect(
      canvas.getByTestId("git-merge-story-conflict-1-scroll-virtual-content"),
    ).toBeInTheDocument();
    await expect(canvas.getByText(hasExactText("Incoming experiment note 1"))).toBeInTheDocument();
    await expect(root).toHaveTextContent("3 conflicts");
    await expect(confirmMergeButton).toBeDisabled();

    await waitFor(() => {
      expect(firstConflictScroll.scrollHeight).toBeGreaterThan(firstConflictScroll.clientHeight);
    });

    const firstConflictMaxScrollTop =
      firstConflictScroll.scrollHeight - firstConflictScroll.clientHeight;
    firstConflictScroll.scrollTop = firstConflictMaxScrollTop;
    await fireEvent.scroll(firstConflictScroll, {
      target: { scrollTop: firstConflictMaxScrollTop },
    });

    await expect(
      await canvas.findByText(hasExactText("Current experiment note 32")),
    ).toBeInTheDocument();

    const handleRect = splitHandle.getBoundingClientRect();
    const pointerX = handleRect.left + handleRect.width / 2;
    const pointerY = handleRect.top + handleRect.height / 2;

    splitHandle.dispatchEvent(new PointerEvent("pointerdown", { bubbles: true, clientX: pointerX, clientY: pointerY }));
    document.dispatchEvent(new PointerEvent("pointermove", { bubbles: true, clientX: pointerX, clientY: pointerY + 120 }));
    document.dispatchEvent(new PointerEvent("pointerup", { bubbles: true, clientX: pointerX, clientY: pointerY + 120 }));

    await expect(conflictsPane).toBeInTheDocument();

    const takeIncomingButtons = canvas.getAllByRole("button", { name: "Take incoming" });

    await userEvent.click(takeIncomingButtons[0]);

    await waitFor(() => {
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(1);
    });

    const remainingTakeCurrentButtons = canvas.getAllByRole("button", { name: "Take current" });
    await userEvent.click(remainingTakeCurrentButtons[0]);

    await expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(2);
    await expect(root).toHaveTextContent("Using incoming");
    await expect(root).toHaveTextContent("Using current");

    const resolvedEditor = canvas.getByTestId("git-merge-story-resolved-editor") as HTMLTextAreaElement;

    await waitFor(() => {
      expect(resolvedEditor.value).toContain("Incoming experiment note 1");
      expect(resolvedEditor.value).toContain("Current temperature 24 C");
      expect(resolvedEditor.value).toContain("<<<<<<< HEAD");
    });

    await expect(confirmMergeButton).toBeDisabled();

    const undoButtonsBeforeManualEdit = canvas.getAllByRole("button", { name: "Undo" });
    await userEvent.click(undoButtonsBeforeManualEdit[0]);

    await waitFor(() => {
      expect(canvas.getByText("Merge conflict 1")).toBeInTheDocument();
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(1);
      expect(root).toHaveTextContent("2 conflicts");
      expect(resolvedEditor.value).toContain("<<<<<<< HEAD");
    });

    await userEvent.type(resolvedEditor, "\nManual final line");

    await waitFor(() => {
      expect(resolvedEditor.value).toContain("Manual final line");
      expect(resolvedEditor.value).toContain("Incoming extra context");
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(1);
    });

    await expect(confirmMergeButton).toBeDisabled();

    const valueSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value")?.set;
    if (!valueSetter) {
      throw new Error("Textarea value setter is unavailable.");
    }

    valueSetter.call(
      resolvedEditor,
      resolvedEditor.value.replace("Current temperature 24 C", "Edited temperature 25 C")
    );
    resolvedEditor.dispatchEvent(new Event("input", { bubbles: true }));

    await waitFor(() => {
      expect(resolvedEditor.value).toContain("Edited temperature 25 C");
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(1);
    });

    await userEvent.click(canvas.getAllByRole("button", { name: "Undo" })[0]);

    await waitFor(() => {
      expect(root).toHaveTextContent("3 conflicts");
      expect(resolvedEditor.value).not.toContain("Edited temperature 25 C");
      expect(resolvedEditor.value).toContain("Incoming temperature 20 C");
      expect(canvas.queryByRole("button", { name: "Undo" })).not.toBeInTheDocument();
    });

    valueSetter.call(resolvedEditor, fullyResolvedContent);
    resolvedEditor.dispatchEvent(new Event("input", { bubbles: true }));

    await waitFor(() => {
      expect(confirmMergeButton).toBeEnabled();
      expect(resolvedEditor.value).toBe(fullyResolvedContent);
    });

    await expect(root).toHaveTextContent("0 conflicts");
    await expect(canvas.queryByText("Merge conflict 1")).not.toBeInTheDocument();
    await expect(canvas.queryByText("Merge conflict 2")).not.toBeInTheDocument();
    await expect(canvas.queryByText("Merge conflict 3")).not.toBeInTheDocument();

    await userEvent.click(confirmMergeButton);

    await waitFor(() => {
      expect(args.onConfirmMerge).toHaveBeenCalledTimes(1);
    });

    expect(args.onConfirmMerge.mock.calls[0][0]).toBe(fullyResolvedContent);
  },
};

export const LargeConflictBlockVirtualized: Story = {
  parameters: { isolated: true },
  args: {
    mergeConflictContent: largeConflictContent,
    testIdPrefix: "git-merge-large-story",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const hasExactText = (value: string) => (_content: string, element: Element | null) =>
      element?.textContent === value;
    const firstConflictScroll = canvas.getByTestId(
      "git-merge-large-story-conflict-1-scroll",
    ) as HTMLElement;

    await expect(
      canvas.getByTestId("git-merge-large-story-conflict-1-scroll-virtual-content"),
    ).toBeInTheDocument();
    await expect(
      canvas.getByTestId("git-merge-large-story-conflict-1-scroll-row-0"),
    ).toBeInTheDocument();
    await expect(
      canvas.queryByTestId("git-merge-large-story-conflict-1-scroll-row-239"),
    ).toBeNull();

    await waitFor(() => {
      expect(firstConflictScroll.clientHeight).toBeGreaterThan(0);
      expect(firstConflictScroll.scrollHeight).toBeGreaterThan(firstConflictScroll.clientHeight);
    });

    const maxScrollTop = firstConflictScroll.scrollHeight - firstConflictScroll.clientHeight;
    firstConflictScroll.scrollTop = maxScrollTop;
    await fireEvent.scroll(firstConflictScroll, {
      target: { scrollTop: maxScrollTop },
    });

    await expect(
      await canvas.findByTestId("git-merge-large-story-conflict-1-scroll-row-239"),
    ).toBeInTheDocument();
    await expect(
      await canvas.findByText(hasExactText("Large current conflict line 240")),
    ).toBeInTheDocument();
  },
};

export const DuplicateConflictBodies: Story = {
  parameters: { isolated: true },
  args: {
    mergeConflictContent: duplicateConflictContent,
    testIdPrefix: "git-merge-duplicate-story",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const resolvedEditor = canvas.getByTestId("git-merge-duplicate-story-resolved-editor") as HTMLTextAreaElement;
    const takeIncomingButtons = canvas.getAllByRole("button", { name: "Take incoming" });

    await userEvent.click(takeIncomingButtons[1]);

    await waitFor(() => {
      expect(resolvedEditor.value).toContain("Between duplicate blocks\nIncoming duplicate line\nAfter duplicate block");
      expect(resolvedEditor.value).toContain("Before duplicate block\n<<<<<<< HEAD");
      expect(resolvedEditor.value).toContain("Between duplicate blocks\nIncoming duplicate line");
    });
  },
};

export const DefaultResolvedContentRemainsEditable: Story = {
  parameters: { isolated: true },
  args: {
    mergeConflictContent,
    defaultResolvedContent: fullyResolvedContent,
    onResolvedContentChange: fn(),
    onConfirmMerge: fn(),
    testIdPrefix: "git-merge-default-resolved-story",
  },
  play: async ({ canvasElement, args }: { canvasElement: HTMLElement; args: any }) => {
    const canvas = within(canvasElement);
    const root = canvas.getByTestId("git-merge-default-resolved-story-root");
    const resolvedEditor = canvas.getByTestId("git-merge-default-resolved-story-resolved-editor") as HTMLTextAreaElement;
    const confirmMergeButton = canvas.getByTestId("git-merge-default-resolved-story-confirm-merge");

    await expect(root).toHaveTextContent("0 conflicts");
    await expect(confirmMergeButton).toBeEnabled();

    await userEvent.type(resolvedEditor, "\nEdited from default value");

    await waitFor(() => {
      expect(resolvedEditor.value).toContain("Edited from default value");
      expect(root).toHaveTextContent("0 conflicts");
      expect(args.onResolvedContentChange).toHaveBeenCalled();
    });

    await userEvent.click(confirmMergeButton);

    await waitFor(() => {
      expect(args.onConfirmMerge).toHaveBeenCalledTimes(1);
    });

    expect(args.onConfirmMerge.mock.calls[0][0]).toContain("Edited from default value");
  },
};

export const ControlledResolvedContentRemainsEditable: Story = {
  parameters: { isolated: true },
  render: (args: any) => (
    <ControlledMergeHarness
      {...args}
      initialResolvedContent={fullyResolvedContent}
    />
  ),
  args: {
    mergeConflictContent,
    onResolvedContentChange: fn(),
    onConfirmMerge: fn(),
    testIdPrefix: "git-merge-controlled-story",
  },
  play: async ({ canvasElement, args }: { canvasElement: HTMLElement; args: any }) => {
    const canvas = within(canvasElement);
    const root = canvas.getByTestId("git-merge-controlled-story-root");
    const resolvedEditor = canvas.getByTestId("git-merge-controlled-story-resolved-editor") as HTMLTextAreaElement;
    const confirmMergeButton = canvas.getByTestId("git-merge-controlled-story-confirm-merge");

    await expect(root).toHaveTextContent("0 conflicts");
    await expect(confirmMergeButton).toBeEnabled();

    await userEvent.type(resolvedEditor, "\nEdited in controlled mode");

    await waitFor(() => {
      expect(resolvedEditor.value).toContain("Edited in controlled mode");
      expect(root).toHaveTextContent("0 conflicts");
      expect(args.onResolvedContentChange).toHaveBeenCalled();
    });

    await userEvent.click(confirmMergeButton);

    await waitFor(() => {
      expect(args.onConfirmMerge).toHaveBeenCalledTimes(1);
    });

    expect(args.onConfirmMerge.mock.calls[0][0]).toContain("Edited in controlled mode");
  },
};

export const ControlledResolvedContentPreservesSessionDuringLag: Story = {
  parameters: { isolated: true },
  render: (args: any) => (
    <DelayedControlledMergeHarness
      {...args}
      initialResolvedContent={mergeConflictContent}
    />
  ),
  args: {
    mergeConflictContent,
    onResolvedContentChange: fn(),
    testIdPrefix: "git-merge-controlled-lag-story",
  },
  play: async ({ canvasElement, args }: { canvasElement: HTMLElement; args: any }) => {
    const canvas = within(canvasElement);
    const root = canvas.getByTestId("git-merge-controlled-lag-story-root");
    const resolvedEditor = canvas.getByTestId("git-merge-controlled-lag-story-resolved-editor") as HTMLTextAreaElement;

    await expect(root).toHaveTextContent("3 conflicts");

    await userEvent.click(canvas.getAllByRole("button", { name: "Take incoming" })[0]);

    await waitFor(() => {
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(1);
    });

    await userEvent.click(canvas.getAllByRole("button", { name: "Take current" })[0]);

    await waitFor(() => {
      expect(args.onResolvedContentChange).toHaveBeenCalledTimes(2);
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(2);
      expect(root).toHaveTextContent("1 conflicts");
      expect(resolvedEditor.value).toContain("Incoming experiment note 1");
      expect(resolvedEditor.value).toContain("Current temperature 24 C");
      expect((resolvedEditor.value.match(/<<<<<<< HEAD/g) ?? [])).toHaveLength(1);
    });
  },
};

export const BoundaryCrossingInvalidatesOnlyTouchedUndo: Story = {
  parameters: { isolated: true },
  args: {
    mergeConflictContent,
    testIdPrefix: "git-merge-boundary-story",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const root = canvas.getByTestId("git-merge-boundary-story-root");
    const resolvedEditor = canvas.getByTestId("git-merge-boundary-story-resolved-editor") as HTMLTextAreaElement;

    await userEvent.click(canvas.getAllByRole("button", { name: "Take current" })[1]);

    await waitFor(() => {
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(1);
    });

    await userEvent.click(canvas.getAllByRole("button", { name: "Take incoming" })[1]);

    await waitFor(() => {
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(2);
      expect(root).toHaveTextContent("1 conflicts");
    });

    const valueSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value")?.set;
    if (!valueSetter) {
      throw new Error("Textarea value setter is unavailable.");
    }

    valueSetter.call(
      resolvedEditor,
      resolvedEditor.value.replace(
        "Shared middle line 8\nCurrent measurement note 1",
        "Shared middle line 8 adjusted\nEdited measurement note 1"
      )
    );
    resolvedEditor.dispatchEvent(new Event("input", { bubbles: true }));

    await waitFor(() => {
      const undoButtons = canvas.getAllByRole("button", { name: "Undo" });

      expect(root).toHaveTextContent("1 conflicts");
      expect(canvas.getByText("Undo unavailable")).toBeInTheDocument();
      expect(undoButtons).toHaveLength(2);
      expect(undoButtons[0]).toBeDisabled();
      expect(undoButtons[1]).toBeEnabled();
    });

    await userEvent.click(canvas.getAllByRole("button", { name: "Undo" })[1]);

    await waitFor(() => {
      const undoButtons = canvas.getAllByRole("button", { name: "Undo" });

      expect(root).toHaveTextContent("2 conflicts");
      expect(undoButtons).toHaveLength(1);
      expect(undoButtons[0]).toBeDisabled();
    });
  },
};

export const FullDocumentReplacementClearsSession: Story = {
  parameters: { isolated: true },
  args: {
    mergeConflictContent,
    testIdPrefix: "git-merge-document-replacement-story",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const root = canvas.getByTestId("git-merge-document-replacement-story-root");
    const resolvedEditor = canvas.getByTestId("git-merge-document-replacement-story-resolved-editor") as HTMLTextAreaElement;

    await userEvent.click(canvas.getAllByRole("button", { name: "Take incoming" })[0]);

    await waitFor(() => {
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(1);
      expect(root).toHaveTextContent("2 conflicts");
    });

    const replacementDocument = ["Replacement document", "No conflicts remain here.", ""].join("\n");
    const valueSetter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value")?.set;
    if (!valueSetter) {
      throw new Error("Textarea value setter is unavailable.");
    }

    resolvedEditor.focus();
    resolvedEditor.setSelectionRange(0, resolvedEditor.value.length);
    resolvedEditor.dispatchEvent(new Event("select", { bubbles: true }));
    resolvedEditor.dispatchEvent(new KeyboardEvent("keyup", { bubbles: true, key: "a" }));

    valueSetter.call(resolvedEditor, replacementDocument);
    resolvedEditor.dispatchEvent(new Event("input", { bubbles: true }));

    await waitFor(() => {
      expect(root).toHaveTextContent("0 conflicts");
      expect(resolvedEditor.value).toBe(replacementDocument);
      expect(canvas.queryByRole("button", { name: "Undo" })).not.toBeInTheDocument();
      expect(canvas.queryByText("Undo unavailable")).not.toBeInTheDocument();
      expect(canvas.queryByText("Merge conflict 1")).not.toBeInTheDocument();
    });
  },
};

export const ExternalResolvedContentReplacementClearsSession: Story = {
  parameters: { isolated: true },
  render: (args: any) => (
    <ExternalReplacementHarness
      {...args}
      initialResolvedContent={mergeConflictContent}
      replacementResolvedContent={["Externally replaced", "No conflicts remain here.", ""].join("\n")}
    />
  ),
  args: {
    mergeConflictContent,
    testIdPrefix: "git-merge-external-replacement-story",
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const root = canvas.getByTestId("git-merge-external-replacement-story-root");
    const resolvedEditor = canvas.getByTestId("git-merge-external-replacement-story-resolved-editor") as HTMLTextAreaElement;

    await userEvent.click(canvas.getAllByRole("button", { name: "Take incoming" })[0]);

    await waitFor(() => {
      expect(canvas.getAllByRole("button", { name: "Undo" })).toHaveLength(1);
      expect(root).toHaveTextContent("2 conflicts");
    });

    await userEvent.click(canvas.getByTestId("git-merge-external-replacement-story-replace-external"));

    await waitFor(() => {
      expect(root).toHaveTextContent("0 conflicts");
      expect(resolvedEditor.value).toBe("Externally replaced\nNo conflicts remain here.\n");
      expect(canvas.queryByRole("button", { name: "Undo" })).not.toBeInTheDocument();
      expect(canvas.queryByText("Undo unavailable")).not.toBeInTheDocument();
      expect(canvas.queryByText("Merge conflict 1")).not.toBeInTheDocument();
    });
  },
};
