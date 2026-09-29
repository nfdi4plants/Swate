module Swate.Components.Page.GitComparison.GitPagedDiffTypes

open Fable.Core

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
    | EvictedPage of pageId: string * rowCount: int

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

type PagedDiffStatus =
    | Opening
    | Scanning
    | EncodingChoice of side: PagedDiffSide * candidates: PagedEncodingCandidate[]
    | Ready
    | LoadingNext
    | Expanding of gapId: string
    | Blocked of side: PagedDiffSide option * reason: string
    | SourceChanged
    | WorkerFailed of message: string
    | Closed
    | Failed of message: string
