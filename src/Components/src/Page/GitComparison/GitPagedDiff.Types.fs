module Swate.Components.Page.GitComparison.GitPagedDiffTypes

open Fable.Core

[<StringEnum; RequireQualifiedAccess>]
type GitDiffChangeKind =
    | Added
    | Deleted
    | Modified

[<StringEnum; RequireQualifiedAccess>]
type PagedDiffSide =
    | Previous
    | Current

[<StringEnum; RequireQualifiedAccess>]
type PagedLineEnding =
    | NoEnding
    | LF
    | CRLF
    | CR

type PagedHighlight = {
    Start: int
    Length: int
    Changed: bool
}

type PagedLine = {
    Number: float
    Ending: PagedLineEnding
    OffsetUtf16: float
    TotalUtf16: float option
    Text: string
    Highlights: PagedHighlight[]
}

[<StringEnum; RequireQualifiedAccess>]
type PagedRowKind =
    | Context
    | Added
    | Removed
    | Replaced
    | EndingChanged

type PagedRow = {
    Id: string
    Kind: PagedRowKind
    Previous: PagedLine option
    Current: PagedLine option
}

type PagedRange = { Start: float; Count: float }

type PagedPart =
    | HunkRows of
        hunkId: string *
        previous: PagedRange *
        current: PagedRange *
        startsHunk: bool *
        endsHunk: bool *
        rows: PagedRow[]
    | UnalignedRegion of
        hunkId: string *
        previous: PagedLine[] *
        current: PagedLine[] *
        previousRange: PagedRange *
        currentRange: PagedRange
    | HiddenGap of gapId: string * previous: PagedRange * current: PagedRange
    | ExpandedRows of gapId: string * rows: PagedRow[]
    /// A page whose rows are not loaded. The row count is the number of display rows the page has
    /// when it is loaded: one for each hunk header, unaligned label and hidden gap, plus the line
    /// rows. The placeholder is as tall as those rows. The page has no extent when the count is 0.
    | EvictedPage of pageId: string * rowCount: int

/// The display rows of the parts, which is the count an evicted page keeps. The viewer lays out
/// the same rows, so a placeholder and the replayed page have the same height.
let displayRowCount (parts: PagedPart[]) =
    parts
    |> Array.sumBy (
        function
        | HunkRows(_, _, _, startsHunk, _, rows) -> (if startsHunk then 1 else 0) + rows.Length
        | ExpandedRows(_, rows) -> rows.Length
        | UnalignedRegion(_, previous, current, _, _) -> 1 + max previous.Length current.Length
        | HiddenGap _ -> 1
        | EvictedPage(_, count) -> count
    )

type PagedProgress = {
    ValidatedBytes: float
    TotalBytes: float
    ScanComplete: bool
}

[<StringEnum; RequireQualifiedAccess>]
type PagedSnippetEnd =
    | Truncated
    | MoreTextPending
    | LineEnd
    | EndOfFile

type PagedPendingSide =
    | NoActiveLine
    | Snippet of line: float * offsetUtf16: float * text: string * endState: PagedSnippetEnd
    | Exhausted of lineCount: float

type PagedPending = {
    Previous: PagedPendingSide
    Current: PagedPendingSide
    Mismatch: (float * float) option
}

type PagedEncodingCandidate = { Encoding: string; Preview: string }

/// A line slice the viewer asked for that has not been answered yet.
type PagedLineSliceRequest = {
    Side: PagedDiffSide
    Line: float
    OffsetUtf16: float
}

/// A source line the viewer scrolls to once a row shows it or a later line of the same side.
/// The viewer scrolls once per token, so a new token asks for the scroll again.
type PagedScrollTarget = {
    Side: PagedDiffSide
    Line: float
    Token: int
}

type PagedDiffStatus =
    | Opening
    | Scanning
    | EncodingChoice of side: PagedDiffSide * candidates: PagedEncodingCandidate[]
    | Ready
    | LoadingNext
    /// The diff opens again after its session ended. The rows already shown stay on screen
    /// and take no requests until the diff is ready again.
    | Reopening
    | Blocked of side: PagedDiffSide option * reason: string
    | SourceChanged
    | WorkerFailed of message: string
    | Failed of message: string
