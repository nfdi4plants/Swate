import React from "react";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { within, expect, waitFor, fn, fireEvent, userEvent } from "storybook/test";
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
  PagedScrollTarget,
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
import { displayRowCount } from "./GitPagedDiff.Types.fs.js";
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
  const rowCount = displayRowCount(parts.slice(0, -1));
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

const FOLD_PAGE_COUNT = 2000;
const FOLD_PAGE_ROWS = 1000;
const FOLD_WINDOW = 8;

function foldedPageRows(index: number) {
  const start = (index - 1) * FOLD_PAGE_ROWS;
  return Array.from({ length: FOLD_PAGE_ROWS }, (_, offset) =>
    new PagedRow(`fold-${index}-${offset}`, "added", undefined, makeLine(start + offset, `Page ${index} row ${offset}`)),
  );
}

// Keeps a window of eight loaded pages out of 2,000 pages of 1,000 rows, the way the app does.
// Every other page is an evicted placeholder. A replay waits for the complete button, so the
// story can look at the rows before and after it.
// With fromStart, the diff is complete and the first eight pages are the loaded ones.
function FoldedPagesHarness({ fromStart = false }: { fromStart?: boolean }) {
  const loadedParts = React.useRef(new Map<number, PagedPart_$union>());
  const evictedParts = React.useRef(new Map<number, PagedPart_$union>());
  const [model, setModel] = React.useState(() => ({
    known: fromStart ? FOLD_PAGE_COUNT : FOLD_PAGE_COUNT - 1,
    loaded: Array.from({ length: FOLD_WINDOW }, (_, offset) =>
      fromStart ? offset + 1 : FOLD_PAGE_COUNT - FOLD_WINDOW + offset,
    ),
  }));
  const [pendingReplays, setPendingReplays] = React.useState<string[]>([]);

  const load = (loaded: number[], index: number) => {
    const next = [...loaded.filter((page) => page !== index), index];
    while (next.length > FOLD_WINDOW) {
      const farthest = next.reduce((far, page) => (Math.abs(page - index) > Math.abs(far - index) ? page : far));
      next.splice(next.indexOf(farthest), 1);
    }
    return next;
  };

  const partFor = (index: number, loaded: boolean) => {
    const cache = loaded ? loadedParts.current : evictedParts.current;
    let part = cache.get(index);
    if (!part) {
      part = loaded
        ? PagedPart_HunkRows(
            `fold-hunk-${index}`,
            range((index - 1) * FOLD_PAGE_ROWS, 0),
            range((index - 1) * FOLD_PAGE_ROWS, FOLD_PAGE_ROWS),
            false,
            false,
            foldedPageRows(index),
          )
        : PagedPart_EvictedPage(`page-${index}`, FOLD_PAGE_ROWS);
      cache.set(index, part);
    }
    return part;
  };

  const parts = Array.from({ length: model.known }, (_, offset) => partFor(offset + 1, model.loaded.includes(offset + 1)));

  const requestNext = () => {
    onFoldedNext();
    setModel((current) =>
      current.known >= FOLD_PAGE_COUNT
        ? current
        : { known: current.known + 1, loaded: load(current.loaded, current.known + 1) },
    );
  };

  const requestReplay = (pageId: string) => {
    onFoldedReplay(pageId);
    setPendingReplays([pageId]);
  };

  const completeReplay = () => {
    const [pageId] = pendingReplays;
    if (!pageId) return;
    const index = Number(pageId.replace("page-", ""));
    setModel((current) => ({ ...current, loaded: load(current.loaded, index) }));
    setPendingReplays([]);
  };

  return (
    <div style={{ height: "40rem" }}>
      <button data-testid="git-paged-folded-complete" onClick={completeReplay}>Complete replay</button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(model.known, FOLD_PAGE_COUNT, model.known >= FOLD_PAGE_COUNT)}
        hasMore={model.known < FOLD_PAGE_COUNT}
        outputComplete={model.known >= FOLD_PAGE_COUNT}
        requestNext={requestNext}
        requestReplay={requestReplay}
        pendingReplays={pendingReplays}
        testIdPrefix="git-paged-folded"
      />
    </div>
  );
}

const SHIFT_PAGE_ROWS = 30;
const SHIFT_SHORT_PAGE_ROWS = 5;

function shiftPageRows(index: number, count: number) {
  return Array.from({ length: count }, (_, offset) =>
    new PagedRow(
      `shift-${index}-${offset}`,
      "added",
      undefined,
      makeLine(index * SHIFT_PAGE_ROWS + offset, `Page ${index} row ${offset}`),
    ),
  );
}

// Starts with six pages, the first two folded. A next page waits for the complete button. It
// adds a short page and folds the two earliest loaded pages, so the rows above the view
// shrink more than the rows below grow, the way the app evicts pages near the end of a diff.
function FoldOnLoadHarness() {
  const [model, setModel] = React.useState({ known: 6, loaded: [3, 4, 5, 6] });
  const [nextPending, setNextPending] = React.useState(false);

  const rowCount = (index: number) => (index === 7 ? SHIFT_SHORT_PAGE_ROWS : SHIFT_PAGE_ROWS);

  const parts = Array.from({ length: model.known }, (_, offset) => {
    const index = offset + 1;
    return model.loaded.includes(index)
      ? PagedPart_HunkRows(
          `shift-hunk-${index}`,
          range(index * SHIFT_PAGE_ROWS, 0),
          range(index * SHIFT_PAGE_ROWS, rowCount(index)),
          false,
          false,
          shiftPageRows(index, rowCount(index)),
        )
      : PagedPart_EvictedPage(`page-${index}`, rowCount(index));
  });

  const requestNext = () => {
    onShiftNext();
    setNextPending(true);
  };

  const completeNext = () => {
    setModel({ known: 7, loaded: [5, 6, 7] });
    setNextPending(false);
  };

  return (
    <div style={{ height: "40rem" }}>
      <button data-testid="git-paged-shift-complete" onClick={completeNext}>Complete next page</button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={nextPending ? PagedDiffStatus_LoadingNext() : PagedDiffStatus_Ready()}
        progress={new PagedProgress(model.known, 10, false)}
        hasMore={true}
        outputComplete={false}
        nextPageKey={`cursor-${model.known}`}
        requestNext={requestNext}
        testIdPrefix="git-paged-shift"
      />
    </div>
  );
}

const DIRECTORY_PAGE_COUNT = 24;
const DIRECTORY_PAGE_ROWS = 40;
const DIRECTORY_WINDOW = 4;
const DIRECTORY_ROW_HEIGHT = 28;
const DIRECTORY_TOTAL_ROWS = DIRECTORY_PAGE_COUNT * DIRECTORY_PAGE_ROWS;

function directoryPart(index: number) {
  const rows = Array.from({ length: DIRECTORY_PAGE_ROWS }, (_, offset) => {
    const number = index * DIRECTORY_PAGE_ROWS + offset;
    return new PagedRow(`directory-row-${number}`, "added", undefined, makeLine(number, `Directory row ${number}`));
  });
  return PagedPart_HunkRows(
    `directory-hunk-${index}`,
    range(index * DIRECTORY_PAGE_ROWS, 0),
    range(index * DIRECTORY_PAGE_ROWS, DIRECTORY_PAGE_ROWS),
    false,
    false,
    rows,
  );
}

// A diff of 24 pages with 40 rows each, of which four are loaded at a time, the way the app evicts
// the pages far from the requested one. A replay answers after a short delay and reports itself as
// running until then. With fromStart every page is known and the first four are loaded. Without it
// only the first page is known, and the continue row reads the next pages.
function DirectoryHarness({
  fromStart = false,
  initialKnown = DIRECTORY_PAGE_COUNT,
  appendButton = false,
  indexing = false,
  onReplay,
  onNext,
  maxScrollHeight,
}: {
  fromStart?: boolean;
  initialKnown?: number;
  appendButton?: boolean;
  indexing?: boolean;
  onReplay?: (pageId: string) => void;
  onNext?: () => void;
  maxScrollHeight?: number;
}) {
  const cache = React.useRef(new Map<string, PagedPart_$union>());
  const [model, setModel] = React.useState(() => ({
    known: fromStart ? initialKnown : 1,
    loaded: fromStart ? [0, 1, 2, 3] : [0],
  }));
  const [pendingReplays, setPendingReplays] = React.useState<string[]>([]);
  const running = React.useRef<string[]>([]);

  const load = (loaded: number[], index: number) => {
    const next = [...loaded.filter((page) => page !== index), index];
    while (next.length > DIRECTORY_WINDOW) {
      const farthest = next.reduce((far, page) => (Math.abs(page - index) > Math.abs(far - index) ? page : far));
      next.splice(next.indexOf(farthest), 1);
    }
    return next;
  };

  const partFor = (index: number, loaded: boolean) => {
    const key = `${loaded ? "loaded" : "evicted"}-${index}`;
    let part = cache.current.get(key);
    if (!part) {
      part = loaded ? directoryPart(index) : PagedPart_EvictedPage(`page-${index}`, DIRECTORY_PAGE_ROWS);
      cache.current.set(key, part);
    }
    return part;
  };

  const parts = Array.from({ length: model.known }, (_, index) => partFor(index, model.loaded.includes(index)));

  const requestNext = () => {
    onNext?.();
    setModel((current) =>
      current.known >= DIRECTORY_PAGE_COUNT
        ? current
        : { known: current.known + 1, loaded: load(current.loaded, current.known) },
    );
  };

  const requestReplay = (pageId: string) => {
    onReplay?.(pageId);
    running.current = [...running.current, pageId];
    setPendingReplays(running.current);

    window.setTimeout(() => {
      const index = Number(pageId.replace("page-", ""));
      setModel((current) => ({ ...current, loaded: load(current.loaded, index) }));
      running.current = running.current.filter((id) => id !== pageId);
      setPendingReplays(running.current);
    }, 20);
  };

  return (
    <div style={{ height: "40rem" }}>
      {appendButton ? (
        <button data-testid="git-paged-directory-append" onClick={requestNext}>
          Append page
        </button>
      ) : null}
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        indexing={indexing}
        progress={new PagedProgress(model.known, DIRECTORY_PAGE_COUNT, model.known >= DIRECTORY_PAGE_COUNT)}
        hasMore={model.known < DIRECTORY_PAGE_COUNT}
        outputComplete={model.known >= DIRECTORY_PAGE_COUNT}
        nextPageKey={`cursor-${model.known}`}
        requestNext={requestNext}
        requestReplay={requestReplay}
        pendingReplays={pendingReplays}
        maxScrollHeight={maxScrollHeight}
        testIdPrefix="git-paged-directory"
      />
    </div>
  );
}

// A diff that grows by one page for each click on the button.
function GestureHarness() {
  const [pages, setPages] = React.useState(1);
  const parts = React.useMemo(() => Array.from({ length: pages }, (_, index) => directoryPart(index)), [pages]);
  return (
    <div style={{ height: "30rem" }}>
      <button data-testid="git-paged-gesture-add" onClick={() => setPages((current) => current + 1)}>
        Add page
      </button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        testIdPrefix="git-paged-gesture"
      />
    </div>
  );
}

// A diff the caller indexes in the background. It starts with three pages known, and a click on
// the first button reads one more page. The second button ends the indexing.
function IndexingHarness({ maxScrollHeight }: { maxScrollHeight?: number }) {
  const [known, setKnown] = React.useState(3);
  const [complete, setComplete] = React.useState(false);
  const parts = React.useMemo(() => Array.from({ length: known }, (_, index) => directoryPart(index)), [known]);
  return (
    <div style={{ height: "30rem" }}>
      <button data-testid="git-paged-indexing-add" onClick={() => setKnown((current) => current + 1)}>
        Read page
      </button>
      <button data-testid="git-paged-indexing-finish" onClick={() => setComplete(true)}>
        Finish
      </button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(known, 10, complete)}
        hasMore={!complete}
        outputComplete={complete}
        indexing={!complete}
        maxScrollHeight={maxScrollHeight}
        nextPageKey={`cursor-${known}`}
        testIdPrefix="git-paged-indexing"
      />
    </div>
  );
}

// The first diff row in the viewport and its distance from the top of the viewport.
function firstVisibleRow(scroll: HTMLElement) {
  const top = scroll.getBoundingClientRect().top;
  const rows = Array.from(scroll.querySelectorAll<HTMLElement>("[data-paged-diff-key]"))
    .filter((row) => /^shift-\d+-\d+$/.test(row.dataset.pagedDiffKey ?? ""))
    .filter((row) => row.getBoundingClientRect().bottom > top + 1)
    .sort((left, right) => left.getBoundingClientRect().top - right.getBoundingClientRect().top);
  const row = rows[0];
  if (!row) throw new Error("No diff row is in view");
  return { key: row.dataset.pagedDiffKey, offset: row.getBoundingClientRect().top - top };
}

const LINE_START_TEXT = `${"a".repeat(20000)}CHANGED${"b".repeat(5000)}`;
const LINE_SLICE_START = 19872;

// Shows a slice around the change of a long line and puts the requested text in front of it,
// with the highlights moved by the length of that text, the way the app merges a slice.
function LineStartHarness() {
  const [line, setLine] = React.useState(() =>
    makeLine(0, LINE_START_TEXT.slice(LINE_SLICE_START, 20500), "lF", LINE_SLICE_START, LINE_START_TEXT.length, [
      new PagedHighlight(20000 - LINE_SLICE_START, 7, true),
    ]),
  );

  const requestLineBefore = (side: string, number: number, start: number) => {
    onRequestLineBefore(side, number, start);
    setLine((current) => {
      const offset = Math.max(0, start - 8192);
      const prefix = LINE_START_TEXT.slice(offset, start);
      return makeLine(
        current.Number,
        prefix + current.Text,
        current.Ending,
        offset,
        current.TotalUtf16 ?? null,
        current.Highlights.map((highlight) => new PagedHighlight(highlight.Start + prefix.length, highlight.Length, highlight.Changed)),
      );
    });
  };

  return (
    <div style={{ height: "30rem" }}>
      <GitPagedDiffViewerComponent
        parts={[
          PagedPart_HunkRows("line-start-hunk", range(0, 1), range(0, 1), true, true, [
            new PagedRow("line-start-row", "replaced", makeLine(0, "short previous line"), line),
          ]),
        ]}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        requestLineSlice={() => {}}
        requestLineBefore={requestLineBefore}
        testIdPrefix="git-paged-line-start"
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

const TARGET_PAGE_ROWS = 40;

function targetPage(index: number) {
  return PagedPart_HunkRows(
    `target-hunk-${index}`,
    range(index * TARGET_PAGE_ROWS, TARGET_PAGE_ROWS),
    range(index * TARGET_PAGE_ROWS, TARGET_PAGE_ROWS),
    false,
    false,
    makeAlignedRows(index * TARGET_PAGE_ROWS, TARGET_PAGE_ROWS, -1, -1),
  );
}

// Starts with four loaded pages. Landing swaps them for placeholders and one loaded page further
// down with a scroll target on one of its lines, the way a reopen lands. No row key survives.
function ScrollTargetHarness() {
  const [landed, setLanded] = React.useState(false);
  const parts = React.useMemo(
    () =>
      landed
        ? [
            ...Array.from({ length: 5 }, (_, index) => PagedPart_EvictedPage(`target-page-${index}`, TARGET_PAGE_ROWS)),
            targetPage(5),
          ]
        : Array.from({ length: 4 }, (_, index) => targetPage(index)),
    [landed],
  );
  return (
    <div style={{ height: "32rem" }}>
      <button data-testid="git-paged-target-land" onClick={() => setLanded(true)}>
        Land
      </button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        scrollTarget={landed ? new PagedScrollTarget("previous", 5 * TARGET_PAGE_ROWS + 30, 1) : undefined}
        testIdPrefix="git-paged-target"
      />
    </div>
  );
}

// Four loaded pages. The diff reopens while the user reads in the middle of the second page, and
// lands with a scroll target on the first line of that page, the way the renderer sets it.
function ReopenLandsHarness() {
  const [step, setStep] = React.useState<"ready" | "reopening" | "landed">("ready");
  const parts = React.useMemo(() => Array.from({ length: 4 }, (_, index) => targetPage(index)), []);
  return (
    <div style={{ height: "32rem" }}>
      <button data-testid="git-paged-landing-reopen" onClick={() => setStep("reopening")}>
        Reopen
      </button>
      <button data-testid="git-paged-landing-land" onClick={() => setStep("landed")}>
        Land
      </button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={step === "reopening" ? PagedDiffStatus_Reopening() : PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        scrollTarget={step === "landed" ? new PagedScrollTarget("previous", TARGET_PAGE_ROWS, 1) : undefined}
        testIdPrefix="git-paged-landing"
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
const onFoldedReplay = fn();
const onFoldedNext = fn();
const onRequestLineBefore = fn();
const onShiftNext = fn();
const onDirectoryReplay = fn();
const onDirectoryNext = fn();

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
      onFoldedReplay,
      onFoldedNext,
      onRequestLineBefore,
      onShiftNext,
      onDirectoryReplay,
      onDirectoryNext,
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

    // The first control of a gap reveals its first lines, the second its last lines. The gap takes
    // one display row.
    const startControl = canvas.getByTestId("git-paged-interactions-gap-expand-start-focus-gap");
    const endControl = canvas.getByTestId("git-paged-interactions-gap-expand-end-focus-gap");
    await expect(startControl).toHaveAttribute("data-from-start", "true");
    await expect(endControl).toHaveAttribute("data-from-start", "false");
    await expect(startControl.getBoundingClientRect().left).toBeLessThan(endControl.getBoundingClientRect().left);
    await expect(Math.abs(firstGap.getBoundingClientRect().height - 28)).toBeLessThan(1);

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

    // The evicted pages fold into one placeholder, and the continue row asks for the next page
    // as soon as it is in view.
    for (let expectedCall = 1; expectedCall <= 4; expectedCall += 1) {
      const grid = within(canvasElement).getByTestId("git-paged-evicting-grid");
      const scroll = Array.from(grid.querySelectorAll<HTMLElement>("div")).find(
        (element) => element.scrollHeight > element.clientHeight + 1,
      );
      if (scroll) await scrollToEnd(scroll);
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

    // The line stays on one row, and the content grows wider than the view.
    const scroll = canvas.getByTestId("git-paged-long-content").parentElement as HTMLElement;
    await expect(Math.abs(canvas.getByTestId("git-paged-long-row-long-row").getBoundingClientRect().height - 28)).toBeLessThan(1);
    await expect(scroll.scrollWidth).toBeGreaterThan(scroll.clientWidth);
    await expect(currentText.scrollWidth).toBeLessThanOrEqual(currentText.clientWidth + 1);
    await expect(currentCell.left).toBeGreaterThanOrEqual(previousCell.right - 1);
    await expect(loadMore.left).toBeGreaterThanOrEqual(currentCell.left - 1);
    await expect(loadMore.right).toBeLessThanOrEqual(currentCell.right + 1);
    await expect(loadMore.top).toBeGreaterThanOrEqual(currentCell.top - 1);
    await expect(loadMore.bottom).toBeLessThanOrEqual(currentCell.bottom + 1);
  },
};

// The control lies inside the client area of the scroller, and a click at its center reaches it.
async function expectControlInView(canvas: ReturnType<typeof within>, scroll: HTMLElement, testId: string) {
  const control = canvas.getByTestId(testId);
  const box = control.getBoundingClientRect();
  const view = scroll.getBoundingClientRect();
  const left = view.left + scroll.clientLeft;
  const top = view.top + scroll.clientTop;
  await expect(box.left).toBeGreaterThanOrEqual(left);
  await expect(box.right).toBeLessThanOrEqual(left + scroll.clientWidth);
  await expect(box.top).toBeGreaterThanOrEqual(top);
  await expect(box.bottom).toBeLessThanOrEqual(top + scroll.clientHeight);
  const hit = document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2);
  await expect(control.contains(hit)).toBe(true);
}

// A diff whose line is far wider than the view, with a hidden gap above it.
function WideLineGapHarness() {
  const [parts, setParts] = React.useState<PagedPart_$union[]>([
    PagedPart_HiddenGap("wide-gap", range(0, 12), range(0, 12)),
    PagedPart_HunkRows("wide-hunk", range(12, 1), range(12, 1), true, true, [
      new PagedRow("wide-row", "replaced", makeLine(12, "short previous line"), makeLine(12, "x".repeat(3000))),
    ]),
  ]);

  return (
    <div style={{ height: "30rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        requestExpand={(gapId: string, fromStart: boolean) =>
          setParts((current) => expandGap(current, gapId, fromStart))
        }
        testIdPrefix="git-paged-wide-gap"
      />
    </div>
  );
}

export const GapControlsStayInViewWithLongLines: Story = {
  render: () => <WideLineGapHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = canvas.getByTestId("git-paged-wide-gap-content").parentElement as HTMLElement;
    await expect(scroll.scrollWidth).toBeGreaterThan(scroll.clientWidth * 2);

    const expectInView = (testId: string) => expectControlInView(canvas, scroll, testId);

    const start = "git-paged-wide-gap-gap-expand-start-wide-gap";
    const end = "git-paged-wide-gap-gap-expand-end-wide-gap";
    await expectInView(start);
    await expectInView(end);

    scroll.scrollLeft = scroll.scrollWidth;
    await fireEvent.scroll(scroll);
    await expect(scroll.scrollLeft).toBeGreaterThan(scroll.clientWidth);
    await waitFor(() => expectInView(start));
    await expectInView(end);

    await userEvent.click(canvas.getByTestId(start));
    await waitFor(() => expect(canvas.queryByTestId(start)).toBeNull());
    await waitFor(() => expectInView("git-paged-wide-gap-gap-expand-start-wide-gap-right"));
  },
};

const wideStopNextSpy = fn();

// Two long lines around an evicted page. The reading stopped with a note and the host passes a
// next callback, so the continue row shows the note and the button.
function WideLineStopHarness() {
  const parts = React.useMemo(
    () => [
      PagedPart_HunkRows("wide-stop-first", range(0, 1), range(0, 1), true, false, [
        new PagedRow("wide-stop-row-0", "replaced", makeLine(0, "short previous line"), makeLine(0, "x".repeat(3000))),
      ]),
      PagedPart_EvictedPage("wide-old-page", 3),
      PagedPart_HunkRows("wide-stop-last", range(4, 1), range(4, 1), false, true, [
        new PagedRow("wide-stop-row-4", "replaced", makeLine(4, "short previous line"), makeLine(4, "x".repeat(3000))),
      ]),
    ],
    [],
  );

  return (
    <div style={{ height: "30rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(300, 1000, false)}
        hasMore={true}
        outputComplete={false}
        endNote="The reading stopped."
        requestNext={() => wideStopNextSpy()}
        requestReplay={() => {}}
        testIdPrefix="git-paged-wide-stop"
      />
    </div>
  );
}

export const ContinueAndReloadControlsStayInViewWithLongLines: Story = {
  render: () => <WideLineStopHarness />,
  play: async ({ canvasElement }) => {
    wideStopNextSpy.mockClear();
    const canvas = within(canvasElement);
    const scroll = canvas.getByTestId("git-paged-wide-stop-content").parentElement as HTMLElement;
    await expect(scroll.scrollWidth).toBeGreaterThan(scroll.clientWidth * 2);

    const expectInView = (testId: string) => expectControlInView(canvas, scroll, testId);
    const reload = "git-paged-wide-stop-folded-replay-wide-old-page";
    const next = "git-paged-wide-stop-continue-button";

    await expect(scroll.scrollLeft).toBe(0);
    await expectInView(reload);
    await expectInView(next);

    scroll.scrollLeft = scroll.scrollWidth;
    await fireEvent.scroll(scroll);
    await expect(scroll.scrollLeft).toBeGreaterThan(scroll.clientWidth);
    await waitFor(() => expectInView(reload));
    await expectInView(next);

    await expect(wideStopNextSpy).not.toHaveBeenCalled();
    await userEvent.click(canvas.getByTestId(next));
    await waitFor(() => expect(wideStopNextSpy).toHaveBeenCalledTimes(1));
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
    const placeholder = canvas.getByTestId("git-paged-replay-row-folded:between:old-page");
    const rowHeight = canvas.getByTestId("git-paged-replay-row-row-60").getBoundingClientRect().height;
    await expect(Math.abs(placeholder.getBoundingClientRect().height - PAGE_LIMIT * rowHeight)).toBeLessThan(1);
    await expect(onRequestReplay).toHaveBeenCalledWith("old-page");
    await waitFor(() => expect(canvas.getByTestId("git-paged-replay-folded-replay-old-page")).toBeDisabled());
    await expect(onRequestReplay).toHaveBeenCalledTimes(1);

    await fireEvent.click(canvas.getByTestId("git-paged-replay-complete"));
    await waitFor(() => expect(canvas.queryByTestId("git-paged-replay-row-folded:between:old-page")).toBeNull());
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

    // Replays run one after another, so the last page in view shows its rows only after all the others.
    await waitFor(() => expect(canvas.queryByTestId(`git-paged-viewport-row-row-${(pagesInView - 1) * VIEWPORT_PAGE_ROWS}`)).not.toBeNull());
    await new Promise((resolve) => window.setTimeout(resolve, 200));

    const records = onViewportReplay.mock.calls.map(([record]) => record as ReplayRecord);
    const pageIds = records.map((record) => record.pageId);

    await expect(records.every((record) => record.runningAtRequest === 0)).toBe(true);
    await expect(new Set(pageIds).size).toBe(pageIds.length);
    await expect(pageIds.length).toBeLessThanOrEqual(pagesInView);
    await expect(pageIds.every((pageId) => Number(pageId.replace("page-", "")) < pagesInView)).toBe(true);
    // The pages below the view stay unloaded, folded into one placeholder after the loaded rows.
    const later = canvas.getByTestId("git-paged-viewport-row-folded:later").firstElementChild as HTMLElement;
    await expect(later).toHaveAttribute("data-page-count", String(VIEWPORT_PAGE_COUNT - pageIds.length));
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

export const FoldedPagesKeepTheirHeightAndTheEndReachable: Story = {
  render: () => <FoldedPagesHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-folded-grid");
    const earlier = () => canvas.getByTestId("git-paged-folded-row-folded:earlier").firstElementChild as HTMLElement;
    const firstFolded = FOLD_PAGE_COUNT - 1 - FOLD_WINDOW;

    // The folded pages stand for all their rows. The diff is taller than the browser lays out, so
    // the scroll height is the cap, and the placeholder shows the part the view reaches.
    await expect(earlier()).toHaveAttribute("data-page-count", String(firstFolded));
    await expect(earlier()).toHaveAttribute("data-row-count", String(firstFolded * FOLD_PAGE_ROWS));
    await expect(Math.abs(scroll.scrollHeight - 10000000)).toBeLessThan(1);
    const height = scroll.scrollHeight;

    // The view starts at the top, so the first page of the diff replays and lands in place.
    await waitFor(() => expect(onFoldedReplay).toHaveBeenCalledTimes(1));
    await expect(onFoldedReplay).toHaveBeenLastCalledWith("page-1");
    await fireEvent.click(canvas.getByTestId("git-paged-folded-complete"));
    await waitFor(() => expect(canvas.getByTestId("git-paged-folded-row-fold-1-0")).toBeInTheDocument());
    await expect(scroll.scrollTop).toBe(0);
    await expect(Math.abs(scroll.scrollHeight - height)).toBeLessThan(1);

    // The continue row stays reachable at the end. Once it is in view it asks for the last page.
    await scrollToEnd(scroll);
    await waitFor(() => expect(onFoldedNext).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(canvas.queryByTestId("git-paged-folded-row-continue")).toBeNull());
  },
};

export const JumpingToTheTopShowsTheFirstPage: Story = {
  render: () => <FoldedPagesHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-folded-grid");
    const replayed = () => onFoldedReplay.mock.calls.map(([pageId]) => pageId);

    // The first replay answers the view at the top. Its rows stay at the top.
    await waitFor(() => expect(onFoldedReplay).toHaveBeenCalledTimes(1));
    await fireEvent.click(canvas.getByTestId("git-paged-folded-complete"));
    await waitFor(() => expect(canvas.getByTestId("git-paged-folded-row-fold-1-0")).toBeInTheDocument());

    // A jump into the middle replays the page at that offset, and no page next to the loaded rows.
    // The diff is taller than the cap, so the native position maps to the logical offset by the
    // ratio of the two ranges. The continue row adds a few pixels at most.
    const middlePage = Math.floor(FOLD_PAGE_COUNT / 2) + 1;
    const logicalTotal = (FOLD_PAGE_COUNT - 1) * FOLD_PAGE_ROWS * 28 + 28;
    const logical = (middlePage - 1) * FOLD_PAGE_ROWS * 28 + 20 * 28;
    const viewHeight = scroll.clientHeight;
    await scrollTo(scroll, (logical * (scroll.scrollHeight - viewHeight)) / (logicalTotal - viewHeight));
    await waitFor(() => expect(onFoldedReplay).toHaveBeenCalledTimes(2));
    await expect(replayed()[1]).toBe(`page-${middlePage}`);
    await fireEvent.click(canvas.getByTestId("git-paged-folded-complete"));
    await waitFor(() => expect(canvas.getByTestId(`git-paged-folded-row-fold-${middlePage}-20`)).toBeInTheDocument());

    // Dragging the thumb to the top is one jump to the top. The viewer asks for the first page of
    // the diff.
    await scrollTo(scroll, 0);
    await waitFor(() => expect(onFoldedReplay).toHaveBeenCalledTimes(3));
    await expect(replayed()[2]).toBe("page-1");
    await fireEvent.click(canvas.getByTestId("git-paged-folded-complete"));

    await waitFor(() => expect(canvas.getByTestId("git-paged-folded-row-fold-1-0")).toBeInTheDocument());
    await new Promise((resolve) => window.setTimeout(resolve, 50));
    await expect(scroll.scrollTop).toBe(0);
    const view = scroll.getBoundingClientRect();
    await expect(canvas.getByTestId("git-paged-folded-row-fold-1-0").getBoundingClientRect().top).toBeGreaterThanOrEqual(view.top - 1);
  },
};

export const JumpingToTheEndShowsTheLastPage: Story = {
  render: () => <FoldedPagesHarness fromStart />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-folded-grid");

    await scrollToEnd(scroll);
    await waitFor(() => expect(onFoldedReplay).toHaveBeenCalledTimes(1));
    await expect(onFoldedReplay).toHaveBeenLastCalledWith(`page-${FOLD_PAGE_COUNT}`);
    await fireEvent.click(canvas.getByTestId("git-paged-folded-complete"));

    const lastRow = `git-paged-folded-row-fold-${FOLD_PAGE_COUNT}-${FOLD_PAGE_ROWS - 1}`;
    await waitFor(() => expect(canvas.getByTestId(lastRow)).toBeInTheDocument());
    await new Promise((resolve) => window.setTimeout(resolve, 50));
    await expect(scroll.scrollHeight - scroll.clientHeight - scroll.scrollTop).toBeLessThan(2);
    await expect(canvas.getByTestId(lastRow).getBoundingClientRect().bottom).toBeLessThanOrEqual(scroll.getBoundingClientRect().bottom + 1);
  },
};

export const ReopenLandsAtTheLineBeingRead: Story = {
  render: () => <ReopenLandsHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-landing-grid");
    const readingRow = TARGET_PAGE_ROWS + 30;
    await scrollTo(scroll, readingRow * 28);

    await fireEvent.click(canvas.getByTestId("git-paged-landing-reopen"));
    await fireEvent.click(canvas.getByTestId("git-paged-landing-land"));
    await waitFor(() => {
      const view = scroll.getBoundingClientRect();
      const row = canvas.getByTestId(`git-paged-landing-row-row-${readingRow}`).getBoundingClientRect();
      expect(Math.abs(row.top - view.top)).toBeLessThan(2);
    });
  },
};

export const LoadingNearTheEndKeepsTheFirstVisibleRow: Story = {
  render: () => <FoldOnLoadHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-shift-grid");

    await scrollToEnd(scroll);
    await waitFor(() => expect(onShiftNext).toHaveBeenCalledTimes(1));
    const before = firstVisibleRow(scroll);

    await fireEvent.click(canvas.getByTestId("git-paged-shift-complete"));
    await waitFor(() => expect(canvas.getByTestId("git-paged-shift-row-shift-7-0")).toBeInTheDocument());
    await expect(canvas.queryByTestId("git-paged-shift-row-shift-4-0")).toBeNull();
    await expect(firstVisibleRow(scroll).key).toBe(before.key);
    await expect(Math.abs(firstVisibleRow(scroll).offset - before.offset)).toBeLessThan(2);
  },
};

export const LoadEarlierShowsTheLineStart: Story = {
  render: () => <LineStartHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const lineText = () => canvas.getByTestId("git-paged-line-start-line-text-current-0");
    const changedText = () =>
      Array.from(lineText().querySelectorAll('[data-highlight="changed"]'))
        .map((segment) => segment.textContent)
        .join("");

    await expect(lineText()).toHaveAttribute("data-offset-utf16", String(LINE_SLICE_START));
    await expect(changedText()).toBe("CHANGED");
    await expect(canvas.queryByTestId("git-paged-line-start-line-before-previous-0")).toBeNull();

    let start = LINE_SLICE_START;
    while (start > 0) {
      await fireEvent.click(canvas.getByTestId("git-paged-line-start-line-before-current-0"));
      await expect(onRequestLineBefore).toHaveBeenLastCalledWith("current", 0, start);
      start = Math.max(0, start - 8192);
      await waitFor(() => expect(lineText()).toHaveAttribute("data-offset-utf16", String(start)));
      await expect(changedText()).toBe("CHANGED");
    }

    await expect(onRequestLineBefore).toHaveBeenCalledTimes(3);
    await expect(canvas.queryByTestId("git-paged-line-start-line-before-current-0")).toBeNull();
  },
};

export const ScrollTargetShowsTheTargetRow: Story = {
  render: () => <ScrollTargetHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-target-grid");
    await scrollTo(scroll, 10 * 28);

    await fireEvent.click(canvas.getByTestId("git-paged-target-land"));
    const targetKey = `row-${5 * TARGET_PAGE_ROWS + 30}`;
    await waitFor(() => {
      const view = scroll.getBoundingClientRect();
      const row = canvas.getByTestId(`git-paged-target-row-${targetKey}`).getBoundingClientRect();
      expect(row.top).toBeGreaterThanOrEqual(view.top - 1);
      expect(row.bottom).toBeLessThanOrEqual(view.bottom + 1);
    });

    // The target applies once. Scrolling away afterwards stays where the user went.
    await scrollTo(scroll, 0);
    await new Promise((resolve) => window.setTimeout(resolve, 50));
    await expect(scroll.scrollTop).toBe(0);
  },
};

export const ScrollbarPositionsMapToPages: Story = {
  render: () => <DirectoryHarness onReplay={onDirectoryReplay} onNext={onDirectoryNext} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-directory-grid");
    const rowTestId = (index: number) => `git-paged-directory-row-directory-row-${index}`;

    // Read every page. The viewer evicts and folds the pages far from the end on the way.
    for (let call = 1; call < DIRECTORY_PAGE_COUNT; call += 1) {
      await scrollToEnd(scroll);
      await waitFor(() => expect(onDirectoryNext.mock.calls.length).toBeGreaterThanOrEqual(call));
    }
    await waitFor(() => expect(canvas.queryByTestId("git-paged-directory-row-continue")).toBeNull());
    await new Promise((resolve) => window.setTimeout(resolve, 100));

    // The scroll height is the height of all rows, and it stays that way.
    const totalHeight = DIRECTORY_TOTAL_ROWS * DIRECTORY_ROW_HEIGHT;
    await expect(Math.abs(scroll.scrollHeight - totalHeight)).toBeLessThan(1);

    const jumpTo = async (top: number, firstPage: number) => {
      onDirectoryReplay.mockClear();
      await scrollTo(scroll, top);
      const viewTop = scroll.scrollTop;
      const firstRow = Math.floor(viewTop / DIRECTORY_ROW_HEIGHT);
      const lastRow = Math.min(
        DIRECTORY_TOTAL_ROWS - 1,
        Math.ceil((viewTop + scroll.clientHeight) / DIRECTORY_ROW_HEIGHT) - 1,
      );

      // The page at the requested offset replays, and the pages next to the loaded rows do not.
      await waitFor(() => expect(onDirectoryReplay).toHaveBeenCalled());
      await expect(onDirectoryReplay.mock.calls[0][0]).toBe(`page-${firstPage}`);
      await expect(Math.abs(scroll.scrollHeight - totalHeight)).toBeLessThan(1);

      // The rows land where the placeholder was, so the view shows the rows at that offset.
      await waitFor(() => {
        const view = scroll.getBoundingClientRect();
        for (const index of [firstRow, lastRow]) {
          const row = canvas.getByTestId(rowTestId(index)).getBoundingClientRect();
          expect(row.bottom).toBeGreaterThan(view.top);
          expect(row.top).toBeLessThan(view.bottom);
        }
      });
      await new Promise((resolve) => window.setTimeout(resolve, 50));
      await expect(Math.abs(scroll.scrollTop - viewTop)).toBeLessThan(1);
      await expect(Math.abs(scroll.scrollHeight - totalHeight)).toBeLessThan(1);
    };

    const middle = Math.floor(scroll.scrollHeight / 2);
    await jumpTo(middle, Math.floor(middle / DIRECTORY_ROW_HEIGHT / DIRECTORY_PAGE_ROWS));
    await jumpTo(0, 0);
    await jumpTo(scroll.scrollHeight, DIRECTORY_PAGE_COUNT - 1);
  },
};

export const SlowDragRendersEveryRow: Story = {
  render: () => <DirectoryHarness fromStart onReplay={onDirectoryReplay} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-directory-grid");
    const totalHeight = DIRECTORY_TOTAL_ROWS * DIRECTORY_ROW_HEIGHT;
    const maxTop = scroll.scrollHeight - scroll.clientHeight;
    const step = Math.floor(scroll.clientHeight / 2);
    const seen = new Set<number>();

    // The thumb moves in steps shorter than the view. Only four of the 24 pages are loaded at a
    // time, so pages fold and replay all along the way.
    for (let top = 0; ; top = Math.min(top + step, maxTop)) {
      await scrollTo(scroll, top);
      const firstRow = Math.floor(scroll.scrollTop / DIRECTORY_ROW_HEIGHT);
      const lastRow = Math.min(
        DIRECTORY_TOTAL_ROWS - 1,
        Math.ceil((scroll.scrollTop + scroll.clientHeight) / DIRECTORY_ROW_HEIGHT) - 1,
      );

      await waitFor(() => {
        for (let index = firstRow; index <= lastRow; index += 1) {
          expect(canvas.getByTestId(`git-paged-directory-row-directory-row-${index}`)).toBeInTheDocument();
        }
      });
      for (let index = firstRow; index <= lastRow; index += 1) seen.add(index);
      await expect(Math.abs(scroll.scrollHeight - totalHeight)).toBeLessThan(1);
      if (top >= maxTop) break;
    }

    await expect(seen.size).toBe(DIRECTORY_TOTAL_ROWS);
    await expect(onDirectoryReplay.mock.calls.length).toBeGreaterThan(DIRECTORY_PAGE_COUNT - DIRECTORY_WINDOW - 1);
  },
};

export const GeometryChangesWaitForTheScrollbarGesture: Story = {
  render: () => <GestureHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-gesture-grid");
    const before = scroll.scrollHeight;

    // A press on the scrollbar targets the scroll element itself. While it lasts, a new page does
    // not change the scroll height, so the thumb keeps its meaning.
    await fireEvent.pointerDown(scroll);
    await fireEvent.click(canvas.getByTestId("git-paged-gesture-add"));
    await new Promise((resolve) => window.setTimeout(resolve, 100));
    await expect(scroll.scrollHeight).toBe(before);

    await fireEvent.pointerUp(window);
    await waitFor(() => expect(scroll.scrollHeight).toBeGreaterThan(before));
  },
};

export const IndexingKeepsTheTailProvisional: Story = {
  render: () => <IndexingHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-indexing-grid");
    const content = canvas.getByTestId("git-paged-indexing-content");
    const pageHeight = DIRECTORY_PAGE_ROWS * DIRECTORY_ROW_HEIGHT;
    const distanceToEnd = () => scroll.scrollHeight - scroll.clientHeight - scroll.scrollTop;

    // While pages are read, the range below the read pages is provisional and shows the progress.
    await expect(content).toHaveAttribute("data-provisional", "true");

    // A view at the end shows the progress below the read pages and follows the end as the diff grows.
    await scrollToEnd(scroll);
    await expect(await canvas.findByTestId("git-paged-indexing-indexing")).toBeInTheDocument();
    await expect(canvas.queryByTestId("git-paged-indexing-continue-button")).toBeNull();
    await scrollToEnd(scroll);
    const height = scroll.scrollHeight;
    await fireEvent.click(canvas.getByTestId("git-paged-indexing-add"));
    await waitFor(() => expect(scroll.scrollHeight).toBeGreaterThanOrEqual(height + pageHeight));
    await waitFor(() => expect(distanceToEnd()).toBeLessThan(2));

    // A view the user moved away from stays where it is.
    await scrollTo(scroll, 200);
    await fireEvent.click(canvas.getByTestId("git-paged-indexing-add"));
    await new Promise((resolve) => window.setTimeout(resolve, 100));
    await expect(scroll.scrollTop).toBe(200);

    await fireEvent.click(canvas.getByTestId("git-paged-indexing-finish"));
    await waitFor(() => expect(content).toHaveAttribute("data-provisional", "false"));
    await expect(canvas.queryByTestId("git-paged-indexing-indexing")).toBeNull();
    await expect(canvas.queryByTestId("git-paged-indexing-row-continue")).toBeNull();
  },
};

const CAP_TEST_HEIGHT = 8000;

export const CappedScrollRangeMapsToPages: Story = {
  render: () => <DirectoryHarness fromStart maxScrollHeight={CAP_TEST_HEIGHT} onReplay={onDirectoryReplay} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-directory-grid");
    const total = DIRECTORY_TOTAL_ROWS * DIRECTORY_ROW_HEIGHT;
    const rowTestId = (index: number) => `git-paged-directory-row-directory-row-${index}`;

    // The diff is taller than the cap, so the native range is the cap and rows keep their size.
    await expect(Math.abs(scroll.scrollHeight - CAP_TEST_HEIGHT)).toBeLessThan(1);
    const ratio = () => (total - scroll.clientHeight) / (scroll.scrollHeight - scroll.clientHeight);
    await expect(ratio()).toBeGreaterThan(2);

    // Waits until the rows at the logical offset are drawn at their distance from the viewport top.
    const expectRowsAt = async (logical: number) => {
      const firstRow = Math.floor(logical / DIRECTORY_ROW_HEIGHT);
      const lastRow = Math.min(
        DIRECTORY_TOTAL_ROWS - 1,
        Math.ceil((logical + scroll.clientHeight) / DIRECTORY_ROW_HEIGHT) - 1,
      );
      await waitFor(() => {
        const view = scroll.getBoundingClientRect();
        for (const index of [firstRow, lastRow]) {
          const row = canvas.getByTestId(rowTestId(index)).getBoundingClientRect();
          expect(Math.abs(row.top - (view.top + index * DIRECTORY_ROW_HEIGHT - logical))).toBeLessThan(2);
          expect(Math.abs(row.height - DIRECTORY_ROW_HEIGHT)).toBeLessThan(1);
        }
      });
    };

    const jumpTo = async (physical: number, firstPage: number) => {
      onDirectoryReplay.mockClear();
      await scrollTo(scroll, physical);
      const logical = scroll.scrollTop * ratio();
      if (firstPage >= 0) {
        await waitFor(() => expect(onDirectoryReplay).toHaveBeenCalled());
        await expect(onDirectoryReplay.mock.calls[0][0]).toBe(`page-${firstPage}`);
      }
      await expectRowsAt(logical);
      await expect(Math.abs(scroll.scrollHeight - CAP_TEST_HEIGHT)).toBeLessThan(1);
      return logical;
    };

    // The first window is loaded, so the top needs no replay.
    await jumpTo(0, -1);

    const middle = Math.floor(scroll.scrollHeight / 2);
    const middleLogical = middle * ratio();
    const middleLogicalRow = Math.floor(middleLogical / DIRECTORY_ROW_HEIGHT);
    const logical = await jumpTo(middle, Math.floor(middleLogicalRow / DIRECTORY_PAGE_ROWS));

    // A wheel step moves by its own number of logical pixels, and a key press by its usual amount.
    const wheel = (deltaY: number) =>
      scroll.dispatchEvent(new WheelEvent("wheel", { deltaY, bubbles: true, cancelable: true }));
    const press = (key: string) => scroll.dispatchEvent(new KeyboardEvent("keydown", { key, bubbles: true, cancelable: true }));
    let expected = logical;
    for (let step = 0; step < 3; step += 1) {
      wheel(100);
      expected += 100;
      await expectRowsAt(expected);
    }
    wheel(-60);
    expected -= 60;
    await expectRowsAt(expected);
    press("ArrowDown");
    expected += 40;
    await expectRowsAt(expected);
    press("PageDown");
    expected += scroll.clientHeight * 0.875;
    await expectRowsAt(expected);

    await jumpTo(0, 0);
    await jumpTo(scroll.scrollHeight, DIRECTORY_PAGE_COUNT - 1);
    await expect(scroll.scrollTop * ratio()).toBeGreaterThan(total - scroll.clientHeight - 2 * ratio());
    await expect(canvas.getByTestId(rowTestId(DIRECTORY_TOTAL_ROWS - 1))).toBeInTheDocument();
  },
};

export const CappedScrollRangeRendersEveryRow: Story = {
  render: () => <DirectoryHarness fromStart maxScrollHeight={CAP_TEST_HEIGHT} onReplay={onDirectoryReplay} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-directory-grid");
    const total = DIRECTORY_TOTAL_ROWS * DIRECTORY_ROW_HEIGHT;
    const ratio = (total - scroll.clientHeight) / (scroll.scrollHeight - scroll.clientHeight);
    const maxTop = scroll.scrollHeight - scroll.clientHeight;
    // The thumb moves by steps that stand for less than half a view of rows.
    const step = Math.floor(scroll.clientHeight / 2 / ratio);
    const seen = new Set<number>();

    for (let top = 0; ; top = Math.min(top + step, maxTop)) {
      await scrollTo(scroll, top);
      const logical = scroll.scrollTop * ratio;
      const firstRow = Math.floor(logical / DIRECTORY_ROW_HEIGHT);
      const lastRow = Math.min(
        DIRECTORY_TOTAL_ROWS - 1,
        Math.ceil((logical + scroll.clientHeight) / DIRECTORY_ROW_HEIGHT) - 1,
      );

      await waitFor(() => {
        for (let index = firstRow; index <= lastRow; index += 1) {
          expect(canvas.getByTestId(`git-paged-directory-row-directory-row-${index}`)).toBeInTheDocument();
        }
      });
      for (let index = firstRow; index <= lastRow; index += 1) seen.add(index);
      if (top >= maxTop) break;
    }

    await expect(seen.size).toBe(DIRECTORY_TOTAL_ROWS);
    await expect(onDirectoryReplay.mock.calls.length).toBeGreaterThan(DIRECTORY_PAGE_COUNT - DIRECTORY_WINDOW - 1);
  },
};

// A scroll event renders the rows for the new native position before the browser paints. The
// rectangles are read right after the event, and once more a frame later.
async function expectAfterScrollEvent(scroll: HTMLElement, top: number, check: () => void) {
  scroll.scrollTop = top;
  scroll.dispatchEvent(new Event("scroll"));
  check();
  await new Promise((resolve) => requestAnimationFrame(() => resolve(null)));
  check();
}

export const NativeScrollInsideAFoldKeepsThePlaceholderInView: Story = {
  render: () => <FoldedPagesHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-folded-grid");
    await waitFor(() => expect(onFoldedReplay).toHaveBeenCalledTimes(1));

    // The placeholder is far taller than the native range, so every position here lies inside
    // it. The virtualizer's range stays the same, and the placeholder still has to follow the
    // native position.
    for (const top of [20000, 40000, 60000]) {
      await expectAfterScrollEvent(scroll, top, () => {
        const view = scroll.getBoundingClientRect();
        const box = canvas.getByTestId("git-paged-folded-row-folded:earlier").getBoundingClientRect();
        expect(Math.abs(box.top - view.top)).toBeLessThan(1);
        expect(box.height).toBeGreaterThan(scroll.clientHeight - 1);
      });
    }
  },
};

export const NativeScrollKeepsLoadedRowsInPlaceInACappedRange: Story = {
  render: () => <DirectoryHarness fromStart maxScrollHeight={CAP_TEST_HEIGHT} onReplay={onDirectoryReplay} />,
  play: async ({ canvasElement }) => {
    const scroll = scrollElementFor(canvasElement, "git-paged-directory-grid");
    const total = DIRECTORY_TOTAL_ROWS * DIRECTORY_ROW_HEIGHT;
    const ratio = (total - scroll.clientHeight) / (scroll.scrollHeight - scroll.clientHeight);
    await new Promise((resolve) => window.setTimeout(resolve, 300));

    // The steps are small, so the rows in view stay the same while the native position moves.
    // Each row has to sit at its distance from the viewport top for the new position.
    for (const top of [100, 103, 106, 109, 112]) {
      await expectAfterScrollEvent(scroll, top, () => {
        const view = scroll.getBoundingClientRect();
        const logical = scroll.scrollTop * ratio;
        let checked = 0;
        for (const row of scroll.querySelectorAll<HTMLElement>('[data-paged-diff-key^="directory-row-"]')) {
          const index = Number((row.dataset.pagedDiffKey ?? "").replace("directory-row-", ""));
          const box = row.getBoundingClientRect();
          if (box.bottom <= view.top || box.top >= view.bottom) continue;
          expect(Math.abs(box.top - (view.top + index * DIRECTORY_ROW_HEIGHT - logical))).toBeLessThan(1);
          checked += 1;
        }
        expect(checked).toBeGreaterThan(10);
      });
    }
  },
};

export const IndexingFollowsTheEndInACappedRange: Story = {
  render: () => <IndexingHarness maxScrollHeight={2500} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-indexing-grid");
    const content = canvas.getByTestId("git-paged-indexing-content");
    const lastRowOf = (pages: number) => `git-paged-indexing-row-directory-row-${pages * DIRECTORY_PAGE_ROWS - 1}`;

    // Three pages are 3,360 logical pixels, above the cap of 2,500.
    await scrollToEnd(scroll);
    await expect(await canvas.findByTestId("git-paged-indexing-indexing")).toBeInTheDocument();
    await scrollToEnd(scroll);
    await expect(Math.abs(scroll.scrollHeight - 2500)).toBeLessThan(1);

    // The view stays at the end while pages arrive, and the native range stays at the cap.
    for (const pages of [4, 5]) {
      await fireEvent.click(canvas.getByTestId("git-paged-indexing-add"));
      await waitFor(() => {
        const view = scroll.getBoundingClientRect();
        const row = canvas.getByTestId(lastRowOf(pages)).getBoundingClientRect();
        expect(row.bottom).toBeGreaterThan(view.top);
        expect(row.top).toBeLessThan(view.bottom);
      });
      await expect(Math.abs(scroll.scrollHeight - 2500)).toBeLessThan(1);
    }

    // A view the user moved away from keeps its rows in place while the diff grows, and the thumb
    // keeps the fraction of the track that the logical offset has in the larger diff.
    await scrollTo(scroll, 700);
    const visibleRow = () => {
      const view = scroll.getBoundingClientRect();
      const row = Array.from(scroll.querySelectorAll<HTMLElement>('[data-paged-diff-key^="directory-row-"]'))
        .filter((element) => {
          const box = element.getBoundingClientRect();
          return box.bottom > view.top + 1 && box.top < view.bottom - 1;
        })
        .sort((left, right) => left.getBoundingClientRect().top - right.getBoundingClientRect().top)[0];
      const key = row.dataset.pagedDiffKey ?? "";
      return { key, index: Number(key.replace("directory-row-", "")), top: row.getBoundingClientRect().top - view.top };
    };
    await new Promise((resolve) => window.setTimeout(resolve, 50));
    const before = visibleRow();
    for (const pages of [6, 7, 8]) {
      await fireEvent.click(canvas.getByTestId("git-paged-indexing-add"));
      await new Promise((resolve) => window.setTimeout(resolve, 50));
      await expect(canvas.queryByTestId(lastRowOf(pages))).toBeNull();
    }
    const after = visibleRow();
    await expect(after.key).toBe(before.key);
    await expect(Math.abs(after.top - before.top)).toBeLessThan(2);
    const logical = after.index * DIRECTORY_ROW_HEIGHT - after.top;
    const total = 8 * DIRECTORY_PAGE_ROWS * DIRECTORY_ROW_HEIGHT + 60;
    const fraction = scroll.scrollTop / (scroll.scrollHeight - scroll.clientHeight);
    await expect(Math.abs(fraction - logical / (total - scroll.clientHeight))).toBeLessThan(0.05);
    await expect(content).toHaveAttribute("data-provisional", "true");
  },
};

export const ReplayedRowsShowWhileTheScrollbarIsHeld: Story = {
  render: () => <DirectoryHarness fromStart initialKnown={20} appendButton indexing onReplay={onDirectoryReplay} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-directory-grid");
    const before = scroll.scrollHeight;

    // A page arrives while the scrollbar is held. The scroll height stays, and the rows of a
    // replay still show where the thumb is.
    await fireEvent.pointerDown(scroll);
    await fireEvent.click(canvas.getByTestId("git-paged-directory-append"));
    const row = 10 * DIRECTORY_PAGE_ROWS + 5;
    await scrollTo(scroll, row * DIRECTORY_ROW_HEIGHT);
    await waitFor(() => expect(onDirectoryReplay).toHaveBeenCalledWith("page-10"));
    await waitFor(() => {
      const view = scroll.getBoundingClientRect();
      const box = canvas.getByTestId(`git-paged-directory-row-directory-row-${row}`).getBoundingClientRect();
      expect(box.bottom).toBeGreaterThan(view.top);
      expect(box.top).toBeLessThan(view.bottom);
    });
    await expect(Math.abs(scroll.scrollHeight - before)).toBeLessThan(1);

    await fireEvent.pointerUp(window);
    await waitFor(() => expect(scroll.scrollHeight).toBeGreaterThan(before));
  },
};

const ANCHOR_PAGE_COUNT = 20;
const ANCHOR_PAGE_ROWS = 100;
const ANCHOR_SHRINK_ROWS = 60;

function anchorPart(prefix: string, start: number, count: number) {
  const rows = Array.from({ length: count }, (_, offset) =>
    new PagedRow(`${prefix}-${offset}`, "added", undefined, makeLine(start + offset, `Row ${prefix} ${offset}`)),
  );
  return PagedPart_HunkRows(`hunk-${prefix}`, range(start, 0), range(start, count), false, false, rows);
}

// A loaded page of 100 rows, 20 evicted pages and a loaded page. The button evicts the first page,
// which had arrived with 40 rows and grew to 100 by expansions, so everything below it moves up.
// With replayed, the button also loads the eleventh evicted page in the same update.
function ShrinkHarness({
  replayed = false,
  middlePages = ANCHOR_PAGE_COUNT,
  maxScrollHeight,
}: {
  replayed?: boolean;
  middlePages?: number;
  maxScrollHeight?: number;
}) {
  const [evicted, setEvicted] = React.useState(false);
  const parts = React.useMemo(
    () => [
      evicted ? PagedPart_EvictedPage("first", ANCHOR_PAGE_ROWS - ANCHOR_SHRINK_ROWS) : anchorPart("first", 0, ANCHOR_PAGE_ROWS),
      ...Array.from({ length: middlePages }, (_, index) =>
        evicted && replayed && index === Math.min(10, middlePages - 1)
          ? anchorPart("replayed", 2000, ANCHOR_PAGE_ROWS)
          : PagedPart_EvictedPage(`middle-${index}`, ANCHOR_PAGE_ROWS),
      ),
      anchorPart("last", 5000, 20),
    ],
    [evicted],
  );
  return (
    <div style={{ height: "30rem" }}>
      <button data-testid="git-paged-shrink-evict" onClick={() => setEvicted(true)}>
        Evict
      </button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        requestReplay={replayed ? () => {} : undefined}
        maxScrollHeight={maxScrollHeight}
        testIdPrefix="git-paged-shrink"
      />
    </div>
  );
}

export const EvictingAPageAboveKeepsThePageInView: Story = {
  render: () => <ShrinkHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-shrink-grid");

    // The view sits 20 rows into the eleventh evicted page.
    await scrollTo(scroll, (ANCHOR_PAGE_ROWS + 10 * ANCHOR_PAGE_ROWS + 20) * 28);
    const before = scroll.scrollTop;
    await fireEvent.click(canvas.getByTestId("git-paged-shrink-evict"));

    // The rows above shrink, and the view stays on the same offset of the same page.
    await waitFor(() => expect(Math.abs(before - scroll.scrollTop - ANCHOR_SHRINK_ROWS * 28)).toBeLessThan(2));
  },
};

const ROW_COUNT_PAGE = () => [
  PagedPart_HunkRows("count-hunk", range(0, 3), range(0, 3), true, false, makeAlignedRows(0, 3, -1, -1)),
  PagedPart_HiddenGap("count-gap", range(3, 10), range(3, 10)),
  PagedPart_UnalignedRegion(
    "count-split",
    Array.from({ length: 3 }, (_, index) => makeLine(13 + index, `Previous split ${index}`)),
    Array.from({ length: 5 }, (_, index) => makeLine(13 + index, `Current split ${index}`)),
    range(13, 3),
    range(13, 5),
  ),
  PagedPart_UnalignedRegion(
    "count-split",
    Array.from({ length: 2 }, (_, index) => makeLine(16 + index, `Previous tail ${index}`)),
    Array.from({ length: 1 }, (_, index) => makeLine(18 + index, `Current tail ${index}`)),
    range(16, 2),
    range(18, 1),
  ),
];

function RowCountHarness() {
  const [loaded, setLoaded] = React.useState(true);
  const page = React.useMemo(ROW_COUNT_PAGE, []);
  const parts = [
    PagedPart_HunkRows("count-before", range(100, 4), range(100, 4), false, false, makeAlignedRows(100, 4, -1, -1)),
    ...(loaded ? page : [PagedPart_EvictedPage("counted", displayRowCount(page))]),
    PagedPart_HunkRows("count-after", range(200, 4), range(200, 4), false, false, makeAlignedRows(200, 4, -1, -1)),
  ];
  return (
    <div style={{ height: "40rem" }}>
      <button data-testid="git-paged-count-toggle" onClick={() => setLoaded((current) => !current)}>
        Toggle
      </button>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        testIdPrefix="git-paged-count"
      />
    </div>
  );
}

export const EvictedPageIsAsTallAsItsReplay: Story = {
  render: () => <RowCountHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const content = canvas.getByTestId("git-paged-count-content");

    // A hunk header, a gap and a split unaligned region have rows of their own. The evicted page
    // is as tall as the loaded page, so the page after it keeps its place.
    const loadedHeight = content.getBoundingClientRect().height;
    const lastRow = () => canvas.getByTestId("git-paged-count-row-row-203").getBoundingClientRect().top;
    const loadedTop = lastRow();
    await expect(loadedHeight).toBe((4 + displayRowCount(ROW_COUNT_PAGE()) + 4) * 28);

    await fireEvent.click(canvas.getByTestId("git-paged-count-toggle"));
    await waitFor(() => expect(canvas.queryByTestId("git-paged-count-row-folded:between:counted")).not.toBeNull());
    await expect(content.getBoundingClientRect().height).toBe(loadedHeight);
    await expect(Math.abs(lastRow() - loadedTop)).toBeLessThan(1);
  },
};

export const ReplayWithEvictionKeepsTheRequestedOffset: Story = {
  render: () => <ShrinkHarness replayed />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-shrink-grid");

    // The view sits 20 rows into the eleventh evicted page. That page loads in the update that
    // evicts the first page, so the rows of the page replace the placeholder the view anchored to.
    await scrollTo(scroll, (ANCHOR_PAGE_ROWS + 10 * ANCHOR_PAGE_ROWS + 20) * 28);
    await fireEvent.click(canvas.getByTestId("git-paged-shrink-evict"));

    await waitFor(() => {
      const view = scroll.getBoundingClientRect();
      const row = canvas.getByTestId("git-paged-shrink-row-replayed-20").getBoundingClientRect();
      expect(Math.abs(row.top - view.top)).toBeLessThan(2);
    });
  },
};

function CappedButtonHarness({ onExpand }: { onExpand: () => void }) {
  const parts = React.useMemo(
    () => [
      PagedPart_HiddenGap("capped-gap", range(0, 5), range(0, 5)),
      ...Array.from({ length: 3 }, (_, index) => directoryPart(index)),
    ],
    [],
  );
  return (
    <div style={{ height: "30rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        requestExpand={onExpand}
        maxScrollHeight={2500}
        testIdPrefix="git-paged-capped-button"
      />
    </div>
  );
}

export const ScrollKeysLeaveTheButtonsOfARowAlone: Story = {
  render: () => <CappedButtonHarness onExpand={() => {}} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-capped-button-grid");
    await expect(Math.abs(scroll.scrollHeight - 2500)).toBeLessThan(1);

    // Arrow keys on a focused button move the view by the usual 40 logical pixels.
    const button = canvas.getByTestId("git-paged-capped-button-gap-expand-start-capped-gap");
    const rowTop = () => canvas.getByTestId("git-paged-capped-button-row-directory-row-5").getBoundingClientRect().top;
    const topBefore = rowTop();
    button.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true, cancelable: true }));
    await waitFor(() => expect(Math.abs(topBefore - rowTop() - 40)).toBeLessThan(1.5));

    // Space on a focused button belongs to the button, so the viewer leaves the event alone.
    const onButton = new KeyboardEvent("keydown", { key: " ", bubbles: true, cancelable: true });
    button.dispatchEvent(onButton);
    await expect(onButton.defaultPrevented).toBe(false);

    // The same key on the scroll element moves the view by a page of logical pixels.
    const onScroller = new KeyboardEvent("keydown", { key: " ", bubbles: true, cancelable: true });
    scroll.dispatchEvent(onScroller);
    await expect(onScroller.defaultPrevented).toBe(true);
    await waitFor(() => expect(scroll.scrollTop).toBeGreaterThan(0));
  },
};

export const ClickingARowGivesTheScrollKeysTheFocus: Story = {
  render: () => <CappedButtonHarness onExpand={() => {}} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-capped-button-grid");
    const rowTop = () => canvas.getByTestId("git-paged-capped-button-row-directory-row-10").getBoundingClientRect().top;

    // A click on a row text moves the focus to the scroll element, and PageDown then moves the view
    // by the logical page step, not by the native step of the mapped range.
    await userEvent.click(canvas.getByTestId("git-paged-capped-button-line-text-current-10"));
    await expect(document.activeElement).toBe(scroll);
    const before = rowTop();
    await userEvent.keyboard("{PageDown}");
    await waitFor(() => expect(Math.abs(before - rowTop() - scroll.clientHeight * 0.875)).toBeLessThan(2));
  },
};

export const OptionArrowMovesOneLogicalPageInACappedRange: Story = {
  render: () => <CappedButtonHarness onExpand={() => {}} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-capped-button-grid");
    const rowTop = () => canvas.getByTestId("git-paged-capped-button-row-directory-row-10").getBoundingClientRect().top;

    // Chromium on macOS scrolls a page for Option+Arrow. In a capped diff that native step would skip
    // rows, so the viewer moves the logical page step of PageDown instead.
    await userEvent.click(canvas.getByTestId("git-paged-capped-button-line-text-current-10"));
    await expect(document.activeElement).toBe(scroll);
    const before = rowTop();
    await userEvent.keyboard("{Alt>}{ArrowDown}{/Alt}");
    await waitFor(() => expect(Math.abs(before - rowTop() - scroll.clientHeight * 0.875)).toBeLessThan(2));
    await userEvent.keyboard("{Alt>}{ArrowUp}{/Alt}");
    await waitFor(() => expect(Math.abs(before - rowTop())).toBeLessThan(2));
  },
};

export const CtrlWheelLeavesACappedRangeAlone: Story = {
  render: () => <CappedButtonHarness onExpand={() => {}} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-capped-button-grid");
    const rowTop = () => canvas.getByTestId("git-paged-capped-button-row-directory-row-10").getBoundingClientRect().top;
    const wheel = (ctrlKey: boolean) => {
      const event = new WheelEvent("wheel", { deltaY: 100, ctrlKey, bubbles: true, cancelable: true });
      scroll.dispatchEvent(event);
      return event;
    };

    // A plain wheel step moves the view, so the viewer handles wheel events here.
    const before = rowTop();
    await expect(wheel(false).defaultPrevented).toBe(true);
    await waitFor(() => expect(Math.abs(before - rowTop() - 100)).toBeLessThan(1.5));

    // Chromium sends a trackpad pinch and Ctrl+wheel as wheel events with ctrlKey set. They keep the
    // default handling and do not move the view.
    const settled = rowTop();
    await expect(wheel(true).defaultPrevented).toBe(false);
    await new Promise((resolve) => window.setTimeout(resolve, 100));
    await expect(Math.abs(rowTop() - settled)).toBeLessThan(1);
  },
};

export const ReplayOfOnePageWithEvictionKeepsTheRequestedOffset: Story = {
  render: () => <ShrinkHarness replayed middlePages={1} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-shrink-grid");

    // The view lies inside the only evicted page. The loaded rows around it anchor the view when
    // that page loads in the update that evicts the first page.
    await scrollTo(scroll, (ANCHOR_PAGE_ROWS + 20) * 28);
    await fireEvent.click(canvas.getByTestId("git-paged-shrink-evict"));

    await waitFor(() => {
      const view = scroll.getBoundingClientRect();
      const row = canvas.getByTestId("git-paged-shrink-row-replayed-20").getBoundingClientRect();
      expect(Math.abs(row.top - view.top)).toBeLessThan(2);
    });
  },
};

// A capped diff whose gap expands after a short delay, the way the app answers an expansion. The
// control stays disabled while the request runs, and the rows replace the gap when it ends.
function CappedExpandHarness() {
  const [parts, setParts] = React.useState<PagedPart_$union[]>(() => [
    PagedPart_HiddenGap("expand-gap", range(0, 5), range(0, 5)),
    ...Array.from({ length: 3 }, (_, index) => directoryPart(index)),
  ]);
  const [expanding, setExpanding] = React.useState<string[]>([]);
  const requestExpand = (gapId: string, fromStart: boolean) => {
    setExpanding([gapId]);
    window.setTimeout(() => {
      setParts((current) => expandGap(current, gapId, fromStart));
      setExpanding([]);
    }, 50);
  };
  return (
    <div style={{ height: "30rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        requestExpand={requestExpand}
        expandingGaps={expanding}
        maxScrollHeight={2500}
        testIdPrefix="git-paged-capped-expand"
      />
    </div>
  );
}

export const ExpandingAGapKeepsTheScrollKeysWorking: Story = {
  render: () => <CappedExpandHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-capped-expand-grid");
    await expect(Math.abs(scroll.scrollHeight - 2500)).toBeLessThan(1);

    // The click gives the button the focus, and the expansion removes the button. The focus then
    // moves to the scroll element, so PageDown moves one logical page.
    const buttonId = "git-paged-capped-expand-gap-expand-start-expand-gap";
    await userEvent.click(canvas.getByTestId(buttonId));
    await waitFor(() => expect(canvas.queryByTestId(buttonId)).toBeNull());
    await waitFor(() => expect(document.activeElement).toBe(scroll));

    const rowTop = () => canvas.getByTestId("git-paged-capped-expand-row-directory-row-10").getBoundingClientRect().top;
    const before = rowTop();
    await userEvent.keyboard("{PageDown}");
    await waitFor(() => expect(Math.abs(before - rowTop() - scroll.clientHeight * 0.875)).toBeLessThan(2));
  },
};

// Four viewers with a continue row. The first shows the indexing status itself, the second leaves
// it to the host while the indexing runs, the third leaves it to the host with a failed read, and
// the fourth shows the row of a diff that is not indexed.
function StatusOptionHarness() {
  const parts = React.useMemo(
    () => [PagedPart_HunkRows("status-hunk", range(0, 0), range(0, 5), false, false, makeAlignedRows(0, 5))],
    [],
  );
  const viewer = (prefix: string, hideIndexingStatus: boolean, indexing: boolean, nextFailed: boolean) => (
    <div style={{ height: "18rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(300, 1000, false)}
        hasMore={true}
        outputComplete={false}
        indexing={indexing}
        nextFailed={nextFailed}
        hideIndexingStatus={hideIndexingStatus}
        requestNext={() => {}}
        nextPageKey="status-cursor"
        testIdPrefix={prefix}
      />
    </div>
  );
  return (
    <div>
      {viewer("git-paged-status-shown", false, true, false)}
      {viewer("git-paged-status-hidden", true, true, false)}
      {viewer("git-paged-status-failed", true, false, true)}
      {viewer("git-paged-status-idle", false, false, false)}
    </div>
  );
}

export const HostStatusOptionHidesTheIndexingNoteAndTheProgress: Story = {
  render: () => <StatusOptionHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // The viewer shows the indexing note and the progress of the reading by default.
    await expect(await canvas.findByTestId("git-paged-status-shown-indexing")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-status-shown-continue-progress")).toBeInTheDocument();

    // With the option, the continue row stays and shows neither of them.
    await expect(await canvas.findByTestId("git-paged-status-hidden-continue")).toBeInTheDocument();
    await expect(canvas.queryByTestId("git-paged-status-hidden-indexing")).toBeNull();
    await expect(canvas.queryByTestId("git-paged-status-hidden-continue-progress")).toBeNull();

    // The button and its failure state stay, and the label of the row is left out.
    const button = await canvas.findByTestId("git-paged-status-failed-continue-button");
    await expect(button).toHaveAttribute("data-failed", "true");
    await expect(canvas.queryByTestId("git-paged-status-failed-continue-progress")).toBeNull();
    await expect(canvas.queryByTestId("git-paged-status-failed-continue-label")).toBeNull();

    // Without the option the same row names the content that is available and offers the button.
    await expect(await canvas.findByTestId("git-paged-status-idle-continue-label")).toBeInTheDocument();
    await expect(canvas.getByTestId("git-paged-status-idle-continue-button")).toBeInTheDocument();
  },
};

const endNoteWithNextSpy = fn();
const endNoteWithoutNextSpy = fn();
const noEndNoteSpy = fn();

// Three viewers with a continue row. The first has an end note and a next callback, the second an end
// note without a callback, and the third no note, which asks for the next page by itself.
function EndNoteHarness() {
  const parts = React.useMemo(
    () => [PagedPart_HunkRows("end-note-hunk", range(0, 0), range(0, 40), false, false, makeAlignedRows(0, 40))],
    [],
  );
  const viewer = (prefix: string, endNote: string | undefined, requestNext: (() => void) | undefined) => (
    <div style={{ height: "18rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(300, 1000, false)}
        hasMore={true}
        outputComplete={false}
        endNote={endNote}
        requestNext={requestNext}
        nextPageKey="end-note-cursor"
        testIdPrefix={prefix}
      />
    </div>
  );
  return (
    <div>
      {viewer("git-paged-end-note-retry", "The reading stopped.", () => endNoteWithNextSpy())}
      {viewer("git-paged-end-note-final", "The reading ended.", undefined)}
      {viewer("git-paged-end-note-none", undefined, () => noEndNoteSpy())}
    </div>
  );
}

export const EndNoteStopsTheAutomaticRequest: Story = {
  render: () => <EndNoteHarness />,
  play: async ({ canvasElement }) => {
    endNoteWithNextSpy.mockClear();
    endNoteWithoutNextSpy.mockClear();
    noEndNoteSpy.mockClear();
    const canvas = within(canvasElement);

    // Every viewer scrolls to its end row.
    for (const prefix of ["retry", "final", "none"]) {
      await scrollToEnd(scrollElementFor(canvasElement, `git-paged-end-note-${prefix}-grid`));
    }

    // Without a note the viewer asks for the next page once the end row is in view.
    await waitFor(() => expect(noEndNoteSpy).toHaveBeenCalledTimes(1));
    await expect(canvas.queryByTestId("git-paged-end-note-none-continue-note")).toBeNull();

    // With a note it never asks by itself, and the note replaces the label of the row.
    await expect(await canvas.findByTestId("git-paged-end-note-retry-continue-note")).toBeInTheDocument();
    await expect(canvas.queryByTestId("git-paged-end-note-retry-continue-label")).toBeNull();
    await expect(endNoteWithNextSpy).not.toHaveBeenCalled();

    // The button shows when the host passes a callback, and it asks once for each click.
    const button = canvas.getByTestId("git-paged-end-note-retry-continue-button");
    await userEvent.click(button);
    await expect(endNoteWithNextSpy).toHaveBeenCalledTimes(1);

    // Without a callback the note stands alone.
    await expect(await canvas.findByTestId("git-paged-end-note-final-continue-note")).toBeInTheDocument();
    await expect(canvas.queryByTestId("git-paged-end-note-final-continue-button")).toBeNull();
  },
};

const LARGE_NUMBER_START = 2168270;

// Rows with line numbers of seven digits, context rows and changed rows, next to rows with small
// numbers, in two viewers.
function LineNumberHarness() {
  const parts = (start: number, prefix: string) => [
    PagedPart_HunkRows(
      `${prefix}-hunk`,
      range(start, 8),
      range(start, 8),
      false,
      false,
      Array.from({ length: 8 }, (_, offset) => {
        const number = start + offset;
        return new PagedRow(
          `${prefix}-${number}`,
          offset % 2 === 0 ? "context" : "replaced",
          makeLine(number, `Previous ${number}`),
          makeLine(number, `Current ${number}`),
        );
      }),
    ),
  ];
  const viewer = (start: number, prefix: string) => (
    <div style={{ height: "18rem" }}>
      <GitPagedDiffViewerComponent
        parts={parts(start, prefix)}
        status={PagedDiffStatus_Ready()}
        progress={new PagedProgress(100, 100, true)}
        hasMore={false}
        outputComplete={true}
        testIdPrefix={`git-paged-${prefix}`}
      />
    </div>
  );
  return (
    <div>
      {viewer(LARGE_NUMBER_START, "large")}
      {viewer(10, "small")}
    </div>
  );
}

export const LineNumbersOfSevenDigitsFitTheirColumn: Story = {
  render: () => <LineNumberHarness />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await canvas.findByTestId(`git-paged-large-line-previous-${LARGE_NUMBER_START}`);

    // The cell of a number is the first child of the cell of its line.
    const numberCells = (prefix: string, start: number) =>
      ["previous", "current"].flatMap((side) =>
        Array.from({ length: 8 }, (_, offset) => {
          const line = canvas.getByTestId(`git-paged-${prefix}-line-${side}-${start + offset}`);
          return line.firstElementChild as HTMLElement;
        }),
      );

    // No number loses a digit, and the column has one width for both sides and every row.
    const large = numberCells("large", LARGE_NUMBER_START);
    for (const cell of large) await expect(cell.scrollWidth).toBeLessThanOrEqual(cell.clientWidth);
    await expect(new Set(large.map((cell) => Math.round(cell.getBoundingClientRect().width))).size).toBe(1);

    // Small numbers keep the minimum width of 3.5rem, which is narrower than the large column.
    const remPixels = parseFloat(getComputedStyle(document.documentElement).fontSize);
    const small = numberCells("small", 10);
    await expect(new Set(small.map((cell) => Math.round(cell.getBoundingClientRect().width))).size).toBe(1);
    await expect(Math.abs(small[0].getBoundingClientRect().width - 3.5 * remPixels)).toBeLessThan(1);
    await expect(large[0].getBoundingClientRect().width).toBeGreaterThan(small[0].getBoundingClientRect().width);
  },
};

export const HomeAndEndJumpAtOnceInAnUncappedDiff: Story = {
  render: () => <DirectoryHarness fromStart onReplay={onDirectoryReplay} />,
  play: async ({ canvasElement }) => {
    const scroll = scrollElementFor(canvasElement, "git-paged-directory-grid");
    const press = (key: string, modifiers: KeyboardEventInit = {}) => {
      const event = new KeyboardEvent("keydown", { key, bubbles: true, cancelable: true, ...modifiers });
      scroll.dispatchEvent(event);
      return event;
    };

    // The diff is not capped, so the native scroll height is the height of all rows.
    await expect(Math.abs(scroll.scrollHeight - DIRECTORY_TOTAL_ROWS * DIRECTORY_ROW_HEIGHT)).toBeLessThan(2);
    await new Promise((resolve) => window.setTimeout(resolve, 100));
    await expect(onDirectoryReplay).not.toHaveBeenCalled();

    // A key with a modifier keeps its default. Shift+End selects, and Ctrl+End belongs to the browser.
    await expect(press("End", { shiftKey: true }).defaultPrevented).toBe(false);
    await expect(press("End", { ctrlKey: true }).defaultPrevented).toBe(false);

    // End jumps to the end in the same task. A smooth scroll would pass the pages in between, and
    // each of them would be replayed.
    const end = press("End");
    await expect(end.defaultPrevented).toBe(true);
    await expect(Math.abs(scroll.scrollTop - (scroll.scrollHeight - scroll.clientHeight))).toBeLessThan(1);
    await waitFor(() => expect(onDirectoryReplay).toHaveBeenCalledWith(`page-${DIRECTORY_PAGE_COUNT - 1}`));
    await new Promise((resolve) => window.setTimeout(resolve, 300));
    await expect(onDirectoryReplay.mock.calls.map((call) => call[0])).toEqual([`page-${DIRECTORY_PAGE_COUNT - 1}`]);

    // Home does the same at the top. Only the first page is replayed, since the window moved to the end.
    const home = press("Home");
    await expect(home.defaultPrevented).toBe(true);
    await expect(scroll.scrollTop).toBe(0);
    await waitFor(() => expect(onDirectoryReplay).toHaveBeenCalledTimes(2));
    await new Promise((resolve) => window.setTimeout(resolve, 300));
    await expect(onDirectoryReplay.mock.calls.map((call) => call[0])).toEqual([
      `page-${DIRECTORY_PAGE_COUNT - 1}`,
      "page-0",
    ]);
  },
};

export const SmallWheelStepsInACappedRangeKeepTheRowsInPlaceWhenRowsAboveShrink: Story = {
  render: () => <ShrinkHarness maxScrollHeight={2000} />,
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const scroll = scrollElementFor(canvasElement, "git-paged-shrink-grid");
    const total = (ANCHOR_PAGE_ROWS + ANCHOR_PAGE_COUNT * ANCHOR_PAGE_ROWS + 20) * 28;
    const ratio = (total - scroll.clientHeight) / (scroll.scrollHeight - scroll.clientHeight);
    await expect(ratio).toBeGreaterThan(30);

    // The view shows the first rows of the last page.
    await scrollTo(scroll, (total - scroll.clientHeight - 700) / ratio);
    const firstLastRow = () => {
      const view = scroll.getBoundingClientRect();
      const row = Array.from(scroll.querySelectorAll<HTMLElement>('[data-paged-diff-key^="last-"]'))
        .filter((element) => element.getBoundingClientRect().bottom > view.top + 1)
        .sort((left, right) => left.getBoundingClientRect().top - right.getBoundingClientRect().top)[0];
      if (!row) throw new Error("No row of the last page is in view");
      return { key: row.dataset.pagedDiffKey, top: row.getBoundingClientRect().top - view.top };
    };
    await waitFor(() => firstLastRow());

    // A wheel step of 4 logical pixels is a fraction of a native pixel, so the native position
    // and the scroll events stay as they are while the view moves.
    const nativeBefore = scroll.scrollTop;
    const topBefore = firstLastRow().top;
    for (let step = 0; step < 3; step += 1) {
      scroll.dispatchEvent(new WheelEvent("wheel", { deltaY: 4, bubbles: true, cancelable: true }));
    }
    await waitFor(() => expect(Math.abs(topBefore - firstLastRow().top - 12)).toBeLessThan(2));
    await expect(scroll.scrollTop).toBe(nativeBefore);
    const before = firstLastRow();

    // The first page shrinks. The rows of the last page keep the place they have in the view.
    await fireEvent.click(canvas.getByTestId("git-paged-shrink-evict"));
    await new Promise((resolve) => window.setTimeout(resolve, 100));
    const after = firstLastRow();
    await expect(after.key).toBe(before.key);
    await expect(Math.abs(after.top - before.top)).toBeLessThan(2);
  },
};
