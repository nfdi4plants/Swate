import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, waitFor, fn, fireEvent } from "storybook/test";
import { Viewer as GitPagedDiffViewerComponent } from "./GitPagedDiffViewer.fs.js";
import {
  PagedEncodingCandidate,
  PagedHighlight,
  PagedLine,
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
  PagedDiffStatus_Closed,
  PagedDiffStatus_EncodingChoice,
  PagedDiffStatus_Expanding,
  PagedDiffStatus_Failed,
  PagedDiffStatus_LoadingNext,
  PagedDiffStatus_Opening,
  PagedDiffStatus_Ready,
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
    const nextText = "continued segment";
    setParts((current) =>
      replaceHunkRows(current, (rows) =>
        rows.map((row) => {
          if (row.Id !== "row-15") return row;
          const nextLine = makeLine(number, nextText, "lF", offset, offset + nextText.length);
          return new PagedRow(row.Id, row.Kind, row.Previous, nextLine);
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

function FakePagedSource({ onRequestNext }: { onRequestNext?: () => void }) {
  const [model, setModel] = React.useState(() => ({
    parts: [
      PagedPart_HiddenGap("lead-gap", range(0, 35), range(0, 35)),
      PagedPart_HunkRows("generated-hunk", range(35, PAGE_LIMIT), range(35, PAGE_LIMIT), true, false, makeAlignedRows(35, PAGE_LIMIT, 38, 40)),
    ] as PagedPart_$union[],
    nextStart: 55,
    hasMore: true,
    outputComplete: false,
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
      return {
        parts: [...current.parts, part],
        nextStart,
        hasMore: !complete,
        outputComplete: complete,
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
        testIdPrefix="git-paged-continue"
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
  const replayCount = React.useRef(0);

  const requestReplay = (pageId: string) => {
    onRequestReplay?.(pageId);
    replayCount.current += 1;
    if (replayCount.current < 2) return;
    setParts((current) =>
      current.flatMap((part) =>
        part.tag === 4
          ? [PagedPart_HunkRows("replayed-page", range(40, PAGE_LIMIT), range(40, PAGE_LIMIT), false, false, makeAlignedRows(40, PAGE_LIMIT))]
          : [part],
      ),
    );
  };

  return (
    <div style={{ height: "40rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(500, 1200, false)}
        hasMore={false}
        outputComplete={false}
        requestReplay={requestReplay}
        testIdPrefix="git-paged-replay"
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

const meta = {
  title: "Page Components/GitComparison/GitPagedDiffViewer",
  component: GitPagedDiffViewerComponent,
  tags: ["autodocs"],
  parameters: { layout: "fullscreen" },
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

const onRequestExpand = fn();
const onRequestLineSlice = fn();

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

    await fireEvent.click(canvas.getByTestId("git-paged-interactions-gap-expand-up-focus-gap"));
    await waitFor(() => expect(canvas.getByTestId("git-paged-interactions-gap-expand-down-focus-gap-right")).toBeEnabled());
    await expect(onRequestExpand).toHaveBeenNthCalledWith(1, "focus-gap", true);

    await fireEvent.click(canvas.getByTestId("git-paged-interactions-gap-expand-down-focus-gap-right"));
    await waitFor(() => expect(canvas.getByTestId("git-paged-interactions-gap-expand-up-focus-gap-right-left")).toBeEnabled());
    await expect(onRequestExpand).toHaveBeenNthCalledWith(2, "focus-gap-right", false);

    await fireEvent.click(canvas.getByTestId("git-paged-interactions-gap-expand-up-focus-gap-right-left"));
    await waitFor(() => expect(canvas.queryByTestId("git-paged-interactions-gap-expand-up-focus-gap-right-left")).toBeNull());
    await expect(onRequestExpand).toHaveBeenNthCalledWith(3, "focus-gap-right-left", true);

    const scroll = scrollElementFor(canvasElement, "git-paged-interactions-grid");
    scroll.scrollTop = 0;
    await fireEvent.scroll(scroll, { target: { scrollTop: 0 } });

    for (let number = 1; number <= 12; number += 1) {
      await expect(canvas.getAllByText(`Previous source line ${number}`, { exact: true })).toHaveLength(1);
      await expect(canvas.getAllByText(`Current source line ${number}`, { exact: true })).toHaveLength(1);
    }

    const endingRow = canvas.getByTestId("git-paged-interactions-row-row-16");
    await expect(endingRow).toHaveTextContent("CRLF");
    await expect(endingRow).toHaveTextContent("LF");

    await fireEvent.click(canvas.getByTestId("git-paged-interactions-line-more-current-15"));
    await expect(onRequestLineSlice).toHaveBeenCalledWith("current", 15, `Current source line 16: first slice`.length);
    await waitFor(() => expect(root).toHaveTextContent("continued segment"));

    await scrollToEnd(scroll);
    await expect(await canvas.findByTestId("git-paged-interactions-row-unaligned:tail:label")).toBeInTheDocument();
    await expect(root).toHaveTextContent("Previous unaligned line 1");
    await expect(root).toHaveTextContent("Current unaligned line 1");
  },
};

const onRequestNext = fn();

export const ContinueLoadsPagedSource: Story = {
  render: () => <FakePagedSource onRequestNext={onRequestNext} />,
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

    scroll.scrollTop = 42 * 28;
    await fireEvent.scroll(scroll, { target: { scrollTop: scroll.scrollTop } });
    await expect(await canvas.findByTestId("git-paged-continue-row-unaligned:fake-mismatch:label")).toBeInTheDocument();
    await expect(root).toHaveTextContent("Current unaligned line 7");

    scroll.scrollTop = 0;
    await fireEvent.scroll(scroll, { target: { scrollTop: 0 } });
    const endingRow = await canvas.findByTestId("git-paged-continue-row-row-40");
    await expect(endingRow).toHaveTextContent("CRLF");
    await expect(endingRow).toHaveTextContent("LF");
  },
};

const onRequestReplay = fn();

export const EvictedPageReplaysWhenVisible: Story = {
  render: () => <EvictedHarness onRequestReplay={onRequestReplay} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-replay-grid");
    await scrollToEnd(scroll);
    await waitFor(() => expect(onRequestReplay).toHaveBeenCalledTimes(1));
    await expect(onRequestReplay).toHaveBeenCalledWith("old-page");
    await fireEvent.click(canvas.getByTestId("git-paged-replay-evicted-old-page"));
    await expect(onRequestReplay).toHaveBeenCalledTimes(2);
    await expect(canvas.getByTestId("git-paged-replay-row-row-40")).toBeInTheDocument();
    await expect(canvas.queryByTestId("git-paged-replay-row-evicted:old-page")).toBeNull();
  },
};

function PendingAndStateSamples({ onChooseEncoding }: { onChooseEncoding?: (side: string, encoding: string) => void }) {
  const mismatch = new PagedPending(
    PagedPendingSide_Snippet(6, 0, "previous pending snippet", "moreTextPending"),
    PagedPendingSide_Snippet(6, 0, "current pending snippet", "truncated"),
    [3, 4],
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
      <div style={{ height: "16rem" }}>
        <GitPagedDiffViewerComponent parts={[]} status={PagedDiffStatus_Closed()} progress={undefined} hasMore={false} outputComplete={false} testIdPrefix="git-paged-closed" />
      </div>
    </div>
  );
}

const onChooseEncoding = fn();

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
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-mismatch-previous")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-pending-mismatch-pending-mismatch-current")).toBeInTheDocument();
    await expect(pending).toHaveTextContent("Still reading");
    await expect(pending).toHaveTextContent("…");
    await expect(canvas.getByTestId("git-paged-pending-exhausted-pending-previous")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-pending-exhausted-pending-current")).toHaveTextContent("9");
    await expect(canvas.getByTestId("git-paged-pending-endings-pending-previous")).toHaveTextContent("Line end");
    await expect(canvas.getByTestId("git-paged-pending-endings-pending-current")).toHaveTextContent("End of file");

    await expect(canvas.getByTestId("git-paged-encoding-state-encoding-choice")).toBeInTheDocument();
    await fireEvent.click(canvas.getByTestId("git-paged-encoding-encoding-choice-utf-8"));
    await expect(onChooseEncoding).toHaveBeenCalledWith("current", "utf-8");

    await expect(canvas.getByTestId("git-paged-opening-state-opening")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-blocked-state-blocked")).toHaveTextContent("binary bytes");
    await expect(canvas.getByTestId("git-paged-source-changed-state-source-changed")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-worker-failed-state-worker-failed")).toHaveTextContent("worker exited");
    await expect(canvas.getByTestId("git-paged-failed-state-failed")).toHaveTextContent("open failed");
    await expect(canvas.getByTestId("git-paged-closed-state-closed")).toBeInTheDocument();
  },
};

function ControlStates() {
  const parts = [PagedPart_HiddenGap("busy-gap", range(4, 12), range(4, 12))];
  return (
    <div className="swt:flex swt:flex-col swt:gap-4">
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent parts={parts} status={PagedDiffStatus_Expanding("busy-gap")} progress={undefined} hasMore={false} outputComplete={false} requestExpand={() => {}} testIdPrefix="git-paged-expanding" />
      </div>
      <div style={{ height: "20rem" }}>
        <GitPagedDiffViewerComponent parts={parts} status={PagedDiffStatus_LoadingNext()} progress={new PagedProgress(40, 100, false)} hasMore={true} outputComplete={false} requestNext={() => {}} testIdPrefix="git-paged-loading" />
      </div>
    </div>
  );
}

export const ActiveRequestsDisableControls: Story = {
  render: () => <ControlStates />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByTestId("git-paged-expanding-gap-expand-up-busy-gap")).toBeDisabled();
    await expect(canvas.getByTestId("git-paged-expanding-gap-expand-down-busy-gap")).toBeDisabled();
    await expect(canvas.getByTestId("git-paged-loading-continue-button")).toBeDisabled();
    await expect(canvas.getByTestId("git-paged-loading-continue")).toHaveTextContent("40%");
    await expect(canvas.getByTestId("git-paged-loading-continue")).toHaveTextContent("40 B");
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

const onAnchorExpand = fn();

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

    await fireEvent.click(canvas.getByTestId("git-paged-anchor-gap-expand-down-anchor-gap"));
    await expect(onAnchorExpand).toHaveBeenCalledWith("anchor-gap", false);
    await waitFor(() => {
      const updatedAnchor = canvas.getByTestId("git-paged-anchor-row-anchor-target");
      expect(Math.abs(updatedAnchor.getBoundingClientRect().top - beforeTop)).toBeLessThan(2);
    });
  },
};
