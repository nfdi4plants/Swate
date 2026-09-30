import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, waitFor, fn, fireEvent } from "storybook/test";
import { Viewer as GitPagedDiffViewerComponent } from "./GitPagedDiffViewer.fs.js";
import {
  PagedEncodingCandidate,
  PagedHighlight,
  PagedLine,
  PagedLineSliceRequest,
  PagedPending,
  PagedPendingSide_Exhausted,
  PagedPendingSide_NoActiveLine,
  PagedPendingSide_Snippet,
  PagedProgress,
  PagedRange,
  PagedRow,
  PagedPart_EvictedPage,
  PagedPart_ExpandedRows,
  PagedPart_HiddenGap,
  PagedPart_HunkRows,
  PagedPart_UnalignedRegion,
  PagedDiffStatus_Blocked,
  PagedDiffStatus_EncodingChoice,
  PagedDiffStatus_Failed,
  PagedDiffStatus_LoadingNext,
  PagedDiffStatus_Opening,
  PagedDiffStatus_Ready,
  PagedDiffStatus_Reopening,
  PagedDiffStatus_Scanning,
  PagedDiffStatus_SourceChanged,
  PagedDiffStatus_WorkerFailed,
} from "./GitPagedDiff.Types.fs.js";
import type { PagedPart_$union } from "./GitPagedDiff.Types.fs.js";

const PAGE_LIMIT = 20;
const EXPANSION_LIMIT = 5;
const GENERATED_LINE_COUNT = 120;

const previousText = Array.from(
  { length: GENERATED_LINE_COUNT },
  (_, index) => `Previous source line ${index + 1}`,
);
const currentText = Array.from(
  { length: GENERATED_LINE_COUNT },
  (_, index) => `Current source line ${index + 1}`,
);

function range(start: number, count: number) {
  return new PagedRange(start, count);
}

function makeLine(
  number: number,
  text: string,
  ending: "noEnding" | "lF" | "cRLF" | "cR" = "lF",
  offset = 0,
  total: number | null = text.length,
  highlights: PagedHighlight[] = [],
) {
  return new PagedLine(number, ending, offset, total ?? undefined, text, highlights);
}

function makeAlignedRows(start: number, count: number, longLine = 15, endingLine = 16) {
  return Array.from({ length: count }, (_, offset) => {
    const number = start + offset;
    let kind: "context" | "added" | "removed" | "replaced" | "endingChanged" = "context";
    let previous: PagedLine | undefined = makeLine(number, previousText[number] ?? `Previous source line ${number + 1}`);
    let current: PagedLine | undefined = makeLine(number, currentText[number] ?? `Current source line ${number + 1}`);

    if (number === longLine) {
      const text = `Current source line ${number + 1}: first slice`;
      kind = "replaced";
      current = makeLine(number, text, "lF", 0, null, [new PagedHighlight(0, 7, true)]);
    } else if (number === endingLine) {
      kind = "endingChanged";
      previous = makeLine(number, previousText[number] ?? `Previous source line ${number + 1}`, "cRLF");
      current = makeLine(number, currentText[number] ?? `Current source line ${number + 1}`, "lF");
    } else if (number === longLine + 2) {
      kind = "removed";
      current = undefined;
    } else if (number === longLine + 3) {
      kind = "added";
      previous = undefined;
    }

    return new PagedRow(`row-${number}`, kind, previous, current);
  });
}

function makeContextRows(gapId: string, start: number, count: number) {
  return Array.from({ length: count }, (_, offset) => {
    const number = start + offset;
    return new PagedRow(
      `context-${gapId}-${number}`,
      "context",
      makeLine(number, previousText[number] ?? `Previous source line ${number + 1}`),
      makeLine(number, currentText[number] ?? `Current source line ${number + 1}`),
    );
  });
}

function makeUnalignedPart() {
  const previous = Array.from({ length: 5 }, (_, index) =>
    makeLine(75 + index, `Previous unaligned line ${index + 1}`),
  );
  const current = Array.from({ length: 7 }, (_, index) =>
    makeLine(75 + index, `Current unaligned line ${index + 1}`),
  );
  return PagedPart_UnalignedRegion("fake-mismatch", previous, current, range(75, 5), range(75, 7));
}

function expandGap(parts: PagedPart_$union[], gapId: string, fromStart: boolean) {
  return parts.flatMap((part): PagedPart_$union[] => {
    if (part.tag !== 2) return [part];
    const [currentGapId, previousRange, currentRange] = part.fields as [string, PagedRange, PagedRange];
    if (currentGapId !== gapId) return [part];

    const expandedCount = Math.min(EXPANSION_LIMIT, previousRange.Count);
    const remainingCount = previousRange.Count - expandedCount;
    const expandedStart = fromStart
      ? previousRange.Start
      : previousRange.Start + previousRange.Count - expandedCount;
    const expanded = PagedPart_ExpandedRows(
      gapId,
      makeContextRows(gapId, expandedStart, expandedCount),
    );

    if (remainingCount <= 0) return [expanded];

    if (fromStart) {
      const remaining = PagedPart_HiddenGap(
        `${gapId}-right`,
        range(previousRange.Start + expandedCount, remainingCount),
        range(currentRange.Start + expandedCount, remainingCount),
      );
      return [expanded, remaining];
    }

    const remaining = PagedPart_HiddenGap(
      `${gapId}-left`,
      range(previousRange.Start, remainingCount),
      range(currentRange.Start, remainingCount),
    );
    return [remaining, expanded];
  });
}

function replaceHunkRows(
  parts: PagedPart_$union[],
  update: (rows: PagedRow[]) => PagedRow[],
) {
  return parts.map((part) => {
    if (part.tag !== 0) return part;
    const [id, previousRange, currentRange, startsHunk, endsHunk, rows] = part.fields as [
      string,
      PagedRange,
      PagedRange,
      boolean,
      boolean,
      PagedRow[],
    ];
    return PagedPart_HunkRows(
      id,
      previousRange,
      currentRange,
      startsHunk,
      endsHunk,
      update(rows),
    );
  });
}

// Appends the slice text after the displayed text, the way the app merges a line slice.
function mergeSlice(line: PagedLine, sliceText: string) {
  return makeLine(
    line.Number,
    line.Text + sliceText,
    line.Ending,
    line.OffsetUtf16,
    line.OffsetUtf16 + line.Text.length + sliceText.length,
    line.Highlights,
  );
}

function InteractionHarness({
  onRequestExpand,
  onRequestLineSlice,
}: {
  onRequestExpand?: (gapId: string, fromStart: boolean) => void;
  onRequestLineSlice?: (side: string, line: number, offset: number) => void;
}) {
  const [parts, setParts] = React.useState<PagedPart_$union[]>([
    PagedPart_HiddenGap("focus-gap", range(0, 12), range(0, 12)),
    PagedPart_HunkRows("focus-hunk", range(12, PAGE_LIMIT), range(12, PAGE_LIMIT), true, true, makeAlignedRows(12, PAGE_LIMIT)),
    PagedPart_UnalignedRegion(
      "tail",
      Array.from({ length: 5 }, (_, index) => makeLine(32 + index, `Previous unaligned line ${index + 1}`)),
      Array.from({ length: 6 }, (_, index) => makeLine(32 + index, `Current unaligned line ${index + 1}`)),
      range(32, 5),
      range(32, 6),
    ),
  ]);

  const requestExpand = (gapId: string, fromStart: boolean) => {
    onRequestExpand?.(gapId, fromStart);
    setParts((current) => expandGap(current, gapId, fromStart));
  };

  const requestLineSlice = (side: "previous" | "current", number: number, offset: number) => {
    onRequestLineSlice?.(side, number, offset);
    if (side !== "current" || number !== 15) return;
    setParts((current) =>
      replaceHunkRows(current, (rows) =>
        rows.map((row) => {
          if (row.Id !== "row-15" || !row.Current) return row;
          return new PagedRow(row.Id, row.Kind, row.Previous, mergeSlice(row.Current, "continued segment"));
        }),
      ),
    );
  };

  return (
    <div style={{ height: "48rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(800, 1200, true)}
        hasMore={false}
        outputComplete={true}
        requestExpand={requestExpand}
        requestLineSlice={requestLineSlice}
        previousTitle="Generated previous text"
        currentTitle="Generated current text"
        testIdPrefix="git-paged-interactions"
        changeKind="modified"
      />
    </div>
  );
}

// Collects every earlier page into one placeholder whenever a page is added, the way the app
// evicts pages far from the end. The part count then stays the same while pages keep coming.
function collapseOlderParts(parts: PagedPart_$union[], evictions: number) {
  if (parts.length < 2) return parts;
  const older = parts.slice(0, -1);
  const rowCount = older.reduce((total, part) => {
    if (part.tag === 0) return total + (part.fields[5] as PagedRow[]).length;
    if (part.tag === 1) {
      const [, previous, current] = part.fields as [string, PagedLine[], PagedLine[]];
      return total + Math.max(previous.length, current.length);
    }
    if (part.tag === 4) return total + (part.fields[1] as number);
    return total;
  }, 0);
  return [PagedPart_EvictedPage(`evicted-${evictions}`, rowCount), parts[parts.length - 1]];
}

function FakePagedSource({
  onRequestNext,
  evictOlderPages = false,
  testIdPrefix,
}: {
  onRequestNext?: () => void;
  evictOlderPages?: boolean;
  testIdPrefix: string;
}) {
  const [model, setModel] = React.useState(() => ({
    parts: [
      PagedPart_HiddenGap("lead-gap", range(0, 35), range(0, 35)),
      PagedPart_HunkRows("generated-hunk", range(35, PAGE_LIMIT), range(35, PAGE_LIMIT), true, false, makeAlignedRows(35, PAGE_LIMIT, 38, 40)),
    ] as PagedPart_$union[],
    nextStart: 55,
    hasMore: true,
    outputComplete: false,
    evictions: 0,
    progress: new PagedProgress(350, GENERATED_LINE_COUNT * 10, false),
  }));

  const requestNext = () => {
    onRequestNext?.();
    setModel((current) => {
      if (!current.hasMore) return current;

      let part: PagedPart_$union;
      let nextStart: number;

      if (current.nextStart === 75) {
        part = makeUnalignedPart();
        nextStart = 82;
      } else {
        const count = Math.min(PAGE_LIMIT, GENERATED_LINE_COUNT - current.nextStart);
        part = PagedPart_HunkRows(
          "generated-hunk",
          range(current.nextStart, count),
          range(current.nextStart, count),
          false,
          current.nextStart + count >= GENERATED_LINE_COUNT,
          makeAlignedRows(current.nextStart, count, 38, 40),
        );
        nextStart = current.nextStart + count;
      }

      const complete = nextStart >= GENERATED_LINE_COUNT;
      const appended = [...current.parts, part];
      return {
        parts: evictOlderPages ? collapseOlderParts(appended, current.evictions + 1) : appended,
        nextStart,
        hasMore: !complete,
        outputComplete: complete,
        evictions: current.evictions + 1,
        progress: new PagedProgress(nextStart * 10, GENERATED_LINE_COUNT * 10, complete),
      };
    });
  };

  return (
    <div style={{ height: "48rem" }}>
      <GitPagedDiffViewerComponent
        parts={model.parts}
        status={PagedDiffStatus_Ready()}
        progress={model.progress}
        hasMore={model.hasMore}
        outputComplete={model.outputComplete}
        requestNext={requestNext}
        previousTitle="Generated previous text"
        currentTitle="Generated current text"
        testIdPrefix={testIdPrefix}
      />
    </div>
  );
}

function EvictedHarness({ onRequestReplay }: { onRequestReplay?: (pageId: string) => void }) {
  const [parts, setParts] = React.useState<PagedPart_$union[]>([
    PagedPart_HunkRows("before-replay", range(0, PAGE_LIMIT), range(0, PAGE_LIMIT), true, false, makeAlignedRows(0, PAGE_LIMIT)),
    PagedPart_HunkRows("before-replay-next", range(20, PAGE_LIMIT), range(20, PAGE_LIMIT), false, false, makeAlignedRows(20, PAGE_LIMIT)),
    PagedPart_EvictedPage("old-page", PAGE_LIMIT),
    PagedPart_HunkRows("after-replay", range(60, 8), range(60, 8), true, true, makeAlignedRows(60, 8)),
  ]);
  const [pendingReplays, setPendingReplays] = React.useState<string[]>([]);

  const requestReplay = (pageId: string) => {
    onRequestReplay?.(pageId);
    setPendingReplays([pageId]);
  };

  const completeReplay = () => {
    setParts((current) =>
      current.flatMap((part) =>
        part.tag === 4
          ? [PagedPart_HunkRows("replayed-page", range(40, PAGE_LIMIT), range(40, PAGE_LIMIT), false, false, makeAlignedRows(40, PAGE_LIMIT))]
          : [part],
      ),
    );
    setPendingReplays([]);
  };

  return (
    <div style={{ height: "40rem" }}>
      <button data-testid="git-paged-replay-complete" onClick={completeReplay}>Complete replay</button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(500, 1200, false)}
        hasMore={false}
        outputComplete={false}
        requestReplay={requestReplay}
        pendingReplays={pendingReplays}
        testIdPrefix="git-paged-replay"
      />
    </div>
  );
}

const VIEWPORT_PAGE_COUNT = 40;
const VIEWPORT_PAGE_ROWS = 3;

type ReplayRecord = { pageId: string; runningAtRequest: number };

// Every page starts evicted. A replay answers after a short delay and reports itself as
// running until then, like the loader does.
function ViewportReplayHarness({ onReplay }: { onReplay: (record: ReplayRecord) => void }) {
  const [parts, setParts] = React.useState<PagedPart_$union[]>(() =>
    Array.from({ length: VIEWPORT_PAGE_COUNT }, (_, index) => PagedPart_EvictedPage(`page-${index}`, VIEWPORT_PAGE_ROWS)),
  );
  const [pendingReplays, setPendingReplays] = React.useState<string[]>([]);
  const running = React.useRef<string[]>([]);

  const requestReplay = (pageId: string) => {
    onReplay({ pageId, runningAtRequest: running.current.length });
    running.current = [...running.current, pageId];
    setPendingReplays(running.current);

    window.setTimeout(() => {
      const index = Number(pageId.replace("page-", ""));
      setParts((current) =>
        current.map((part) =>
          part.tag === 4 && part.fields[0] === pageId
            ? PagedPart_HunkRows(
                `hunk-${index}`,
                range(index * VIEWPORT_PAGE_ROWS, VIEWPORT_PAGE_ROWS),
                range(index * VIEWPORT_PAGE_ROWS, VIEWPORT_PAGE_ROWS),
                false,
                false,
                makeAlignedRows(index * VIEWPORT_PAGE_ROWS, VIEWPORT_PAGE_ROWS, -1, -1),
              )
            : part,
        ),
      );
      running.current = running.current.filter((id) => id !== pageId);
      setPendingReplays(running.current);
    }, 20);
  };

  return (
    <div style={{ height: "40rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        requestReplay={requestReplay}
        pendingReplays={pendingReplays}
        testIdPrefix="git-paged-viewport"
      />
    </div>
  );
}

function ReopenHarness() {
  const [reopening, setReopening] = React.useState(false);
  const parts = [
    PagedPart_HunkRows("reopen-hunk", range(0, 60), range(0, 60), true, false, makeAlignedRows(0, 60, -1, -1)),
    PagedPart_HiddenGap("reopen-gap", range(60, 30), range(60, 30)),
  ];
  return (
    <div style={{ height: "32rem" }}>
      <button data-testid="git-paged-reopen-toggle" onClick={() => setReopening((current) => !current)}>
        Toggle reopen
      </button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={reopening ? PagedDiffStatus_Reopening() : PagedDiffStatus_Ready()}
        progress={new PagedProgress(30, 100, false)}
        hasMore={false}
        outputComplete={false}
        requestExpand={() => {}}
        testIdPrefix="git-paged-reopen"
      />
    </div>
  );
}

function scrollElementFor(container: HTMLElement, gridTestId: string) {
  const grid = within(container).getByTestId(gridTestId);
  const scrollElement = Array.from(grid.querySelectorAll<HTMLElement>("div")).find(
    (element) => element.scrollHeight > element.clientHeight + 1,
  );
  if (!scrollElement) throw new Error("The diff body has no scroll element");
  return scrollElement;
}

async function scrollToEnd(element: HTMLElement) {
  const top = element.scrollHeight - element.clientHeight;
  element.scrollTop = top;
  await fireEvent.scroll(element, { target: { scrollTop: top } });
}

async function scrollTo(element: HTMLElement, top: number) {
  element.scrollTop = top;
  await fireEvent.scroll(element, { target: { scrollTop: top } });
}

const onRequestExpand = fn();
const onRequestLineSlice = fn();
const onRequestNext = fn();
const onRequestNextAfterEvictions = fn();
const onRequestReplay = fn();
const onViewportReplay = fn();
const onChooseEncoding = fn();
const onAnchorExpand = fn();

const meta = {
  title: "Page Components/GitComparison/GitPagedDiffViewer",
  component: GitPagedDiffViewerComponent,
  tags: ["autodocs"],
  parameters: { layout: "fullscreen" },
  beforeEach: () => {
    for (const mock of [
      onRequestExpand,
      onRequestLineSlice,
      onRequestNext,
      onRequestNextAfterEvictions,
      onRequestReplay,
      onViewportReplay,
      onChooseEncoding,
      onAnchorExpand,
    ]) {
      mock.mockClear();
    }
  },
  decorators: [
    (Story) => (
      <div className="swt:h-[80vh] swt:min-h-192 swt:bg-base-200 swt:p-6">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof GitPagedDiffViewerComponent>;

export default meta;
type Story = StoryObj<typeof meta>;

export const PagedSourceInteractions: Story = {
  render: () => (
    <InteractionHarness
      onRequestExpand={onRequestExpand}
      onRequestLineSlice={onRequestLineSlice}
    />
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const root = canvas.getByTestId("git-paged-interactions-root");
    const firstGap = canvas.getByTestId("git-paged-interactions-row-gap:focus-gap");
    await expect(firstGap).toHaveTextContent("12");
    await expect(root).toHaveTextContent("@@ -13,20 +13,20 @@");

    // The control at the top of a gap reveals its first lines, the one at the bottom its last lines.
    const startControl = canvas.getByTestId("git-paged-interactions-gap-expand-start-focus-gap");
    const endControl = canvas.getByTestId("git-paged-interactions-gap-expand-end-focus-gap");
    await expect(startControl).toHaveAttribute("data-from-start", "true");
    await expect(endControl).toHaveAttribute("data-from-start", "false");
    await expect(startControl.getBoundingClientRect().top).toBeLessThan(endControl.getBoundingClientRect().top);

    await fireEvent.click(startControl);
    await waitFor(() => expect(canvas.getByTestId("git-paged-interactions-gap-expand-end-focus-gap-right")).toBeEnabled());
    await expect(onRequestExpand).toHaveBeenNthCalledWith(1, "focus-gap", true);

    await fireEvent.click(canvas.getByTestId("git-paged-interactions-gap-expand-end-focus-gap-right"));
    await waitFor(() => expect(canvas.getByTestId("git-paged-interactions-gap-expand-start-focus-gap-right-left")).toBeEnabled());
    await expect(onRequestExpand).toHaveBeenNthCalledWith(2, "focus-gap-right", false);

    await fireEvent.click(canvas.getByTestId("git-paged-interactions-gap-expand-start-focus-gap-right-left"));
    await waitFor(() => expect(canvas.queryByTestId("git-paged-interactions-gap-expand-start-focus-gap-right-left")).toBeNull());
    await expect(onRequestExpand).toHaveBeenNthCalledWith(3, "focus-gap-right-left", true);

    const scroll = scrollElementFor(canvasElement, "git-paged-interactions-grid");
    await scrollTo(scroll, 0);

    for (let number = 1; number <= 12; number += 1) {
      await expect(canvas.getAllByText(`Previous source line ${number}`, { exact: true })).toHaveLength(1);
      await expect(canvas.getAllByText(`Current source line ${number}`, { exact: true })).toHaveLength(1);
    }

    await expect(canvas.getByTestId("git-paged-interactions-ending-previous-16")).toHaveAttribute("data-ending", "crlf");
    await expect(canvas.getByTestId("git-paged-interactions-ending-current-16")).toHaveAttribute("data-ending", "lf");

    const highlighted = canvas.getByTestId("git-paged-interactions-line-text-current-15");
    const changedSegments = Array.from(highlighted.querySelectorAll('[data-highlight="changed"]'));
    await expect(changedSegments.map((segment) => segment.textContent).join("")).toBe("Current");
    const unchangedLine = canvas.getByTestId("git-paged-interactions-line-text-current-14");
    await expect(unchangedLine.querySelectorAll('[data-highlight="changed"]')).toHaveLength(0);

    await fireEvent.click(canvas.getByTestId("git-paged-interactions-line-more-current-15"));
    await expect(onRequestLineSlice).toHaveBeenCalledWith("current", 15, `Current source line 16: first slice`.length);
    await waitFor(() =>
      expect(canvas.getByTestId("git-paged-interactions-line-text-current-15")).toHaveTextContent(
        "Current source line 16: first slicecontinued segment",
      ),
    );
    await expect(canvas.queryByTestId("git-paged-interactions-line-more-current-15")).toBeNull();

    await scrollToEnd(scroll);
    await expect(await canvas.findByTestId("git-paged-interactions-row-unaligned:tail:32:32:label")).toBeInTheDocument();
    await expect(root).toHaveTextContent("Previous unaligned line 1");
    await expect(root).toHaveTextContent("Current unaligned line 1");
  },
};

export const ContinueLoadsPagedSource: Story = {
  render: () => <FakePagedSource onRequestNext={onRequestNext} testIdPrefix="git-paged-continue" />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-continue-grid");

    for (let expectedCall = 1; expectedCall <= 4; expectedCall += 1) {
      await scrollToEnd(scroll);
      await waitFor(() => expect(onRequestNext.mock.calls.length).toBeGreaterThanOrEqual(expectedCall));
    }
    await expect(onRequestNext).toHaveBeenCalledTimes(4);

    await expect(canvas.queryByTestId("git-paged-continue-row-continue")).toBeNull();
    await expect(canvas.queryByTestId("git-paged-continue-continue-button")).toBeNull();

    const root = canvas.getByTestId("git-paged-continue-root");
    await expect(root).toHaveTextContent("Current source line 120");

    await scrollTo(scroll, 42 * 28);
    await expect(await canvas.findByTestId("git-paged-continue-row-unaligned:fake-mismatch:75:75:label")).toBeInTheDocument();
    await expect(root).toHaveTextContent("Current unaligned line 7");

    await scrollTo(scroll, 0);
    await expect(await canvas.findByTestId("git-paged-continue-ending-previous-40")).toHaveAttribute("data-ending", "crlf");
    await expect(canvas.getByTestId("git-paged-continue-ending-current-40")).toHaveAttribute("data-ending", "lf");
  },
};

export const ContinueKeepsLoadingAfterEvictions: Story = {
  render: () => (
    <FakePagedSource
      onRequestNext={onRequestNextAfterEvictions}
      evictOlderPages={true}
      testIdPrefix="git-paged-evicting"
    />
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-evicting-grid");

    for (let expectedCall = 1; expectedCall <= 4; expectedCall += 1) {
      await scrollToEnd(scroll);
      await waitFor(() => expect(onRequestNextAfterEvictions.mock.calls.length).toBeGreaterThanOrEqual(expectedCall));
    }
    await expect(onRequestNextAfterEvictions).toHaveBeenCalledTimes(4);
    await expect(canvas.queryByTestId("git-paged-evicting-row-continue")).toBeNull();
  },
};

function makeSplitUnalignedParts() {
  const fragment = (label: string, previousStart: number, previousCount: number, currentStart: number, currentCount: number) =>
    PagedPart_UnalignedRegion(
      "split-hunk",
      Array.from({ length: previousCount }, (_, index) =>
        makeLine(previousStart + index, `Previous ${label} line ${index + 1}`),
      ),
      Array.from({ length: currentCount }, (_, index) =>
        makeLine(currentStart + index, `Current ${label} line ${index + 1}`),
      ),
      range(previousStart, previousCount),
      range(currentStart, currentCount),
    );

  return [fragment("first", 0, 3, 0, 4), fragment("second", 3, 2, 4, 3)];
}

export const SplitUnalignedHunkRendersEveryFragment: Story = {
  render: () => (
    <div style={{ height: "40rem" }}>
      <GitPagedDiffViewerComponent
        parts={makeSplitUnalignedParts()}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        testIdPrefix="git-paged-split"
      />
    </div>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("git-paged-split-row-unaligned:split-hunk:0:0:label")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-split-row-unaligned:split-hunk:3:4:label")).toBeInTheDocument();

    const expectedLines = [
      ...Array.from({ length: 3 }, (_, index) => `Previous first line ${index + 1}`),
      ...Array.from({ length: 4 }, (_, index) => `Current first line ${index + 1}`),
      ...Array.from({ length: 2 }, (_, index) => `Previous second line ${index + 1}`),
      ...Array.from({ length: 3 }, (_, index) => `Current second line ${index + 1}`),
    ];

    for (const text of expectedLines) {
      await expect(canvas.getAllByText(text, { exact: true })).toHaveLength(1);
    }

    // The previous lane lists removed lines and the current lane added lines.
    await expect(canvas.getByTestId("git-paged-split-line-previous-0")).toHaveAttribute("data-kind", "removed");
    await expect(canvas.getByTestId("git-paged-split-line-current-0")).toHaveAttribute("data-kind", "added");
    await expect(canvas.getByTestId("git-paged-split-line-current-6")).toHaveAttribute("data-kind", "added");
  },
};

export const LongLineStaysInItsColumn: Story = {
  render: () => (
    <div style={{ height: "30rem" }}>
      <GitPagedDiffViewerComponent
        parts={[
          PagedPart_HunkRows("long-hunk", range(0, 1), range(0, 1), true, true, [
            new PagedRow(
              "long-row",
              "replaced",
              makeLine(0, "short previous line"),
              makeLine(0, "x".repeat(3000), "lF", 0, 10000),
            ),
          ]),
        ]}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        requestLineSlice={() => {}}
        testIdPrefix="git-paged-long"
      />
    </div>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const previousCell = canvas.getByTestId("git-paged-long-line-text-previous-0").getBoundingClientRect();
    const currentText = canvas.getByTestId("git-paged-long-line-text-current-0");
    const currentCell = currentText.getBoundingClientRect();
    const loadMore = canvas.getByTestId("git-paged-long-line-more-current-0").getBoundingClientRect();

    await expect(currentText.scrollWidth).toBeLessThanOrEqual(currentText.clientWidth + 1);
    await expect(currentCell.left).toBeGreaterThanOrEqual(previousCell.right - 1);
    await expect(loadMore.left).toBeGreaterThanOrEqual(currentCell.left - 1);
    await expect(loadMore.right).toBeLessThanOrEqual(currentCell.right + 1);
    await expect(loadMore.top).toBeGreaterThanOrEqual(currentCell.top - 1);
    await expect(loadMore.bottom).toBeLessThanOrEqual(currentCell.bottom + 1);
  },
};

export const EvictedPageReplaysWhenVisible: Story = {
  render: () => <EvictedHarness onRequestReplay={onRequestReplay} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-replay-grid");

    await scrollToEnd(scroll);
    await waitFor(() => expect(onRequestReplay).toHaveBeenCalledTimes(1));

    // The placeholder is as tall as the rows it stands for.
    const placeholder = canvas.getByTestId("git-paged-replay-row-evicted:old-page");
    const rowHeight = canvas.getByTestId("git-paged-replay-row-row-60").getBoundingClientRect().height;
    await expect(Math.abs(placeholder.getBoundingClientRect().height - PAGE_LIMIT * rowHeight)).toBeLessThan(1);
    await expect(onRequestReplay).toHaveBeenCalledWith("old-page");
    await waitFor(() => expect(canvas.getByTestId("git-paged-replay-evicted-old-page")).toBeDisabled());
    await expect(onRequestReplay).toHaveBeenCalledTimes(1);

    await fireEvent.click(canvas.getByTestId("git-paged-replay-complete"));
    await waitFor(() => expect(canvas.queryByTestId("git-paged-replay-row-evicted:old-page")).toBeNull());
    await expect(canvas.getByTestId("git-paged-replay-row-row-59")).toBeInTheDocument();
    await expect(onRequestReplay).toHaveBeenCalledTimes(1);
  },
};

export const ReplaysOnlyPagesInView: Story = {
  render: () => <ViewportReplayHarness onReplay={onViewportReplay} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-viewport-grid");
    const pageHeight = VIEWPORT_PAGE_ROWS * 28;
    const pagesInView = Math.ceil(scroll.clientHeight / pageHeight);

    await waitFor(() => expect(canvas.queryByTestId("git-paged-viewport-row-evicted:page-0")).toBeNull());
    await waitFor(() => expect(canvas.queryByTestId(`git-paged-viewport-row-evicted:page-${pagesInView - 1}`)).toBeNull());
    await new Promise((resolve) => window.setTimeout(resolve, 200));

    const records = onViewportReplay.mock.calls.map(([record]) => record as ReplayRecord);
    const pageIds = records.map((record) => record.pageId);

    await expect(records.every((record) => record.runningAtRequest === 0)).toBe(true);
    await expect(new Set(pageIds).size).toBe(pageIds.length);
    await expect(pageIds.length).toBeLessThanOrEqual(pagesInView);
    await expect(pageIds.every((pageId) => Number(pageId.replace("page-", "")) < pagesInView)).toBe(true);
    await expect(canvas.getByTestId(`git-paged-viewport-row-evicted:page-${pagesInView + 2}`)).toBeInTheDocument();
  },
};

export const ReopeningKeepsTheGridInPlace: Story = {
  render: () => <ReopenHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const grid = canvas.getByTestId("git-paged-reopen-grid");
    const scroll = scrollElementFor(canvasElement, "git-paged-reopen-grid");
    await scrollTo(scroll, 20 * 28);
    const scrollTop = scroll.scrollTop;
    await expect(scrollTop).toBeGreaterThan(0);

    await fireEvent.click(canvas.getByTestId("git-paged-reopen-toggle"));
    await expect(await canvas.findByTestId("git-paged-reopen-reopening")).toHaveAttribute("data-progress", "30");
    await expect(canvas.getByTestId("git-paged-reopen-grid")).toBe(grid);
    await expect(scroll.scrollTop).toBe(scrollTop);

    await scrollToEnd(scroll);
    await expect(await canvas.findByTestId("git-paged-reopen-gap-expand-start-reopen-gap")).toBeDisabled();
    await expect(canvas.getByTestId("git-paged-reopen-gap-expand-end-reopen-gap")).toBeDisabled();
    await scrollTo(scroll, scrollTop);

    await fireEvent.click(canvas.getByTestId("git-paged-reopen-toggle"));
    await waitFor(() => expect(canvas.queryByTestId("git-paged-reopen-reopening")).toBeNull());
    await expect(canvas.getByTestId("git-paged-reopen-grid")).toBe(grid);
    await expect(scroll.scrollTop).toBe(scrollTop);
  },
};

function PendingAndStateSamples({ onChooseEncoding }: { onChooseEncoding?: (side: string, encoding: string) => void }) {
  const mismatch = new PagedPending(
    PagedPendingSide_Snippet(6, 0, "previous pending snippet", "moreTextPending"),
    PagedPendingSide_Snippet(6, 0, "current pending snippet", "truncated"),
    [3, 4],
  );
  // The snippets start 300 units into the line, and the mismatch offsets count from the line start.
  const midLine = new PagedPending(
    PagedPendingSide_Snippet(8, 300, "previous snippet from the middle", "moreTextPending"),
    PagedPendingSide_Snippet(8, 300, "current snippet from the middle", "moreTextPending"),
    [305, 307],
  );
  const exhausted = new PagedPending(
    PagedPendingSide_NoActiveLine(),
    PagedPendingSide_Exhausted(9),
    undefined,
  );
  const ended = new PagedPending(
    PagedPendingSide_Snippet(10, 0, "previous final line", "lineEnd"),
    PagedPendingSide_Snippet(10, 0, "current final line", "endOfFile"),
    undefined,
  );
  const candidates = [
    new PagedEncodingCandidate("utf-8", "preview with ascii text"),
    new PagedEncodingCandidate("windows-1252", "preview with accented text"),
  ];

  return (
    <div className="swt:flex swt:h-full swt:flex-col swt:gap-4 swt:overflow-auto">
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent
          parts={[]}
          status={PagedDiffStatus_Scanning()}
          progress={new PagedProgress(40, 100, false)}
          hasMore={true}
          outputComplete={false}
          pending={mismatch}
          testIdPrefix="git-paged-pending-mismatch"
        />
      </div>
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent
          parts={[]}
          status={PagedDiffStatus_Scanning()}
          progress={new PagedProgress(50, 100, false)}
          hasMore={true}
          outputComplete={false}
          pending={midLine}
          testIdPrefix="git-paged-pending-mid-line"
        />
      </div>
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent
          parts={[]}
          status={PagedDiffStatus_Scanning()}
          progress={new PagedProgress(90, 100, false)}
          hasMore={true}
          outputComplete={false}
          pending={exhausted}
          testIdPrefix="git-paged-pending-exhausted"
        />
      </div>
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent
          parts={[]}
          status={PagedDiffStatus_Scanning()}
          progress={new PagedProgress(100, 100, false)}
          hasMore={true}
          outputComplete={false}
          pending={ended}
          testIdPrefix="git-paged-pending-endings"
        />
      </div>
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent
          parts={[]}
          status={PagedDiffStatus_EncodingChoice("current", candidates)}
          progress={undefined}
          hasMore={false}
          outputComplete={false}
          chooseEncoding={onChooseEncoding}
          testIdPrefix="git-paged-encoding"
        />
      </div>
      <div style={{ height: "16rem" }}>
        <GitPagedDiffViewerComponent parts={[]} status={PagedDiffStatus_Opening()} progress={new PagedProgress(10, 100, false)} hasMore={false} outputComplete={false} testIdPrefix="git-paged-opening" />
      </div>
      <div style={{ height: "16rem" }}>
        <GitPagedDiffViewerComponent parts={[]} status={PagedDiffStatus_Blocked("current", "binary bytes") } progress={undefined} hasMore={false} outputComplete={false} testIdPrefix="git-paged-blocked" />
      </div>
      <div style={{ height: "16rem" }}>
        <GitPagedDiffViewerComponent parts={[]} status={PagedDiffStatus_SourceChanged()} progress={undefined} hasMore={false} outputComplete={false} testIdPrefix="git-paged-source-changed" />
      </div>
      <div style={{ height: "16rem" }}>
        <GitPagedDiffViewerComponent parts={[]} status={PagedDiffStatus_WorkerFailed("worker exited") } progress={undefined} hasMore={false} outputComplete={false} testIdPrefix="git-paged-worker-failed" />
      </div>
      <div style={{ height: "16rem" }}>
        <GitPagedDiffViewerComponent parts={[]} status={PagedDiffStatus_Failed("open failed") } progress={undefined} hasMore={false} outputComplete={false} testIdPrefix="git-paged-failed" />
      </div>
    </div>
  );
}

export const PendingEncodingAndStates: Story = {
  render: () => <PendingAndStateSamples onChooseEncoding={onChooseEncoding} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const pending = canvas.getByTestId("git-paged-pending-mismatch-pending");
    await expect(canvas.getByTestId("git-paged-pending-mismatch-state-scanning")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-pending-mismatch-state-scanning")).toHaveTextContent("40%");
    await expect(pending).toHaveTextContent("7");
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-previous")).toHaveTextContent("pending snippet");
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-current")).toHaveTextContent("pending snippet");
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-mismatch-previous")).toHaveAttribute("data-offset", "3");
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-mismatch-current")).toHaveAttribute("data-offset", "4");
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-snippet-previous")).toHaveAttribute("data-starts-mid-line", "false");
    await expect(canvas.queryByTestId("git-paged-pending-mismatch-pending-leading-previous")).toBeNull();
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-end-previous")).toHaveAttribute("data-end", "more-text-pending");
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-end-current")).toHaveAttribute("data-end", "truncated");

    await expect(canvas.getByTestId("git-paged-pending-mid-line-pending-mismatch-previous")).toHaveAttribute("data-offset", "5");
    await expect(canvas.getByTestId("git-paged-pending-mid-line-pending-mismatch-current")).toHaveAttribute("data-offset", "7");
    await expect(canvas.getByTestId("git-paged-pending-mid-line-pending-snippet-previous")).toHaveAttribute("data-starts-mid-line", "true");
    await expect(canvas.getByTestId("git-paged-pending-mid-line-pending-leading-previous")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-pending-mid-line-pending-leading-current")).toBeInTheDocument();

    await expect(canvas.getByTestId("git-paged-pending-exhausted-pending-previous")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-pending-exhausted-pending-current")).toHaveTextContent("9");
    await expect(canvas.getByTestId("git-paged-pending-endings-pending-end-previous")).toHaveAttribute("data-end", "line-end");
    await expect(canvas.getByTestId("git-paged-pending-endings-pending-end-current")).toHaveAttribute("data-end", "end-of-file");

    await expect(canvas.getByTestId("git-paged-encoding-state-encoding-choice")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-encoding-encoding-side")).toHaveAttribute("data-side", "current");
    await fireEvent.click(canvas.getByTestId("git-paged-encoding-encoding-choice-utf-8"));
    await expect(onChooseEncoding).toHaveBeenCalledWith("current", "utf-8");

    await expect(canvas.getByTestId("git-paged-opening-state-opening")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-blocked-state-blocked")).toHaveTextContent("binary bytes");
    await expect(canvas.getByTestId("git-paged-source-changed-state-source-changed")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-worker-failed-state-worker-failed")).toHaveTextContent("worker exited");
    await expect(canvas.getByTestId("git-paged-failed-state-failed")).toHaveTextContent("open failed");
  },
};

function ControlStates() {
  const parts = [
    PagedPart_HiddenGap("busy-gap", range(4, 12), range(4, 12)),
    PagedPart_HunkRows("between-gaps", range(16, 2), range(16, 2), true, true, makeAlignedRows(16, 2, -1, -1)),
    PagedPart_HiddenGap("second-busy-gap", range(18, 12), range(18, 12)),
    PagedPart_HiddenGap("idle-gap", range(30, 12), range(30, 12)),
  ];
  const sliceParts = [
    PagedPart_HunkRows("slice-hunk", range(12, PAGE_LIMIT), range(12, PAGE_LIMIT), true, true, makeAlignedRows(12, PAGE_LIMIT)),
  ];
  const pendingSlice = new PagedLineSliceRequest("current", 15, `Current source line 16: first slice`.length);
  return (
    <div className="swt:flex swt:flex-col swt:gap-4">
      <div style={{ height: "24rem" }}>
        <GitPagedDiffViewerComponent parts={parts} status={PagedDiffStatus_Ready()} progress={undefined} hasMore={false} outputComplete={false} requestExpand={() => {}} expandingGaps={["busy-gap", "second-busy-gap"]} testIdPrefix="git-paged-expanding" />
      </div>
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent parts={parts} status={PagedDiffStatus_LoadingNext()} progress={new PagedProgress(40, 100, false)} hasMore={true} outputComplete={false} requestNext={() => {}} testIdPrefix="git-paged-loading" />
      </div>
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent parts={sliceParts} status={PagedDiffStatus_Ready()} progress={new PagedProgress(100, 100, true)} hasMore={false} outputComplete={true} requestLineSlice={() => {}} pendingLineSlices={[pendingSlice]} testIdPrefix="git-paged-slice" />
      </div>
    </div>
  );
}

export const ActiveRequestsDisableControls: Story = {
  render: () => <ControlStates />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    for (const gapId of ["busy-gap", "second-busy-gap"]) {
      await expect(canvas.getByTestId(`git-paged-expanding-gap-expand-start-${gapId}`)).toBeDisabled();
      await expect(canvas.getByTestId(`git-paged-expanding-gap-expand-end-${gapId}`)).toBeDisabled();
    }
    await expect(canvas.getByTestId("git-paged-expanding-gap-expand-start-idle-gap")).toBeEnabled();
    await expect(canvas.getByTestId("git-paged-loading-continue-button")).toBeDisabled();
    await expect(canvas.getByTestId("git-paged-loading-continue")).toHaveTextContent("40%");
    await expect(canvas.getByTestId("git-paged-loading-continue")).toHaveTextContent("40 B");
    await expect(canvas.getByTestId("git-paged-slice-line-more-current-15")).toBeDisabled();
  },
};

function EmptyDiffHarness() {
  const [scanComplete, setScanComplete] = React.useState(false);
  const [outputComplete, setOutputComplete] = React.useState(false);
  return (
    <div style={{ height: "32rem" }}>
      <button data-testid="git-paged-empty-scan-complete" onClick={() => setScanComplete(true)}>Mark scan complete</button>
      <button data-testid="git-paged-empty-output-complete" onClick={() => setOutputComplete(true)}>Mark output complete</button>
      <GitPagedDiffViewerComponent
        parts={[]}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(scanComplete ? 100 : 80, 100, scanComplete)}
        hasMore={false}
        outputComplete={outputComplete}
        testIdPrefix="git-paged-empty"
      />
    </div>
  );
}

export const EmptyDiffWaitsForCompletion: Story = {
  render: () => <EmptyDiffHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.queryByTestId("git-paged-empty-no-changes")).toBeNull();
    await fireEvent.click(canvas.getByTestId("git-paged-empty-scan-complete"));
    await expect(canvas.queryByTestId("git-paged-empty-no-changes")).toBeNull();
    await fireEvent.click(canvas.getByTestId("git-paged-empty-output-complete"));
    await expect(canvas.getByTestId("git-paged-empty-no-changes")).toBeInTheDocument();
  },
};

function AnchorHarness() {
  const afterRows = makeAlignedRows(25, 10, -1, -1).map((row, index) =>
    index === 0 ? new PagedRow("anchor-target", row.Kind, row.Previous, row.Current) : row,
  );
  const [parts, setParts] = React.useState<PagedPart_$union[]>([
    PagedPart_HunkRows("before", range(0, 5), range(0, 5), true, true, makeAlignedRows(0, 5, -1, -1)),
    PagedPart_HiddenGap("anchor-gap", range(5, 20), range(5, 20)),
    PagedPart_HunkRows("after", range(25, 10), range(25, 10), true, true, afterRows),
  ]);
  return (
    <div style={{ height: "24rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        requestExpand={(gapId, fromStart) => {
          onAnchorExpand(gapId, fromStart);
          setParts((current) => expandGap(current, gapId, fromStart));
        }}
        testIdPrefix="git-paged-anchor"
      />
    </div>
  );
}

export const ExpansionKeepsTheVisibleRowInPlace: Story = {
  render: () => <AnchorHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-anchor-grid");
    const anchor = canvas.getByTestId("git-paged-anchor-row-anchor-target");
    scroll.scrollTop += anchor.getBoundingClientRect().top - scroll.getBoundingClientRect().top;
    await fireEvent.scroll(scroll, { target: { scrollTop: scroll.scrollTop } });
    await waitFor(() => expect(Math.abs(anchor.getBoundingClientRect().top - scroll.getBoundingClientRect().top)).toBeLessThan(2));
    const beforeTop = anchor.getBoundingClientRect().top;

    await fireEvent.click(canvas.getByTestId("git-paged-anchor-gap-expand-end-anchor-gap"));
    await expect(onAnchorExpand).toHaveBeenCalledWith("anchor-gap", false);
    await waitFor(() => {
      const updatedAnchor = canvas.getByTestId("git-paged-anchor-row-anchor-target");
      expect(Math.abs(updatedAnchor.getBoundingClientRect().top - beforeTop)).toBeLessThan(2);
    });
  },
};
