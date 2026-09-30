namespace Swate.Components.Page.GitComparison

open System
open System.Collections.Generic
open Browser.Types
open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Page
open Swate.Components.Page.GitComparison.GitPagedDiffTypes

module internal GitPagedDiffDisplay =

    type Content =
        | HunkHeader of string
        | AlignedRow of PagedRow * bool
        | Gap of string * PagedRange * PagedRange
        | UnalignedLabel of string * PagedRange * PagedRange
        | UnalignedLines of PagedLine option * PagedLine option
        | Evicted of string * int
        | Continue of PagedPending option

    type Row = { Key: string; Content: Content }

    type VirtualRow = {
        Key: string
        Index: int
        Start: int
        Size: int
    }

    type RowProps = {
        Row: Row
        Index: int
        Start: int
        MeasureElementRef: VirtualMeasureElementRef
        Prefix: string
        /// Gaps whose expansion is still running. Their controls stay disabled.
        ExpandingGaps: string[]
        LoadingNext: bool
        HasMore: bool
        Progress: PagedProgress option
        RequestExpand: (string -> bool -> unit) option
        RequestLineSlice: (PagedDiffSide -> float -> float -> unit) option
        PendingLineSlices: PagedLineSliceRequest[]
        /// Evicted pages whose replay is still running. Their controls stay disabled.
        PendingReplays: string[]
        RequestReplay: (string -> unit) option
        RequestNext: (unit -> unit) option
    }

    type LineProps = {
        Line: PagedLine option
        Side: PagedDiffSide
        Kind: PagedRowKind
        ForceContext: bool
        Prefix: string
        RequestLineSlice: (PagedDiffSide -> float -> float -> unit) option
        /// A slice of this line is still loading, so its control stays disabled.
        SlicePending: bool
    }

    type AnchorCandidate = { Key: string; Start: int }

    type AnchorSnapshot = {
        Keys: string[]
        Candidates: AnchorCandidate[]
    }

    /// Height of one diff row. A placeholder of an evicted page is as tall as its rows, so the
    /// scroll position stays where it was when the page is evicted or replayed.
    [<Literal>]
    let RowHeightPx = 28

    let private rangeStart (range: PagedRange) =
        if range.Count = 0.0 then range.Start else range.Start + 1.0

    let clamp (lower: int) (upper: int) (value: int) = min upper (max lower value)

    let numberText value = sprintf "%.0f" value

    let hunkHeader previous current =
        $"@@ -{numberText (rangeStart previous)},{numberText previous.Count} +{numberText (rangeStart current)},{numberText current.Count} @@"

    let buildPartRows part =
        match part with
        | PagedPart.HunkRows(hunkId, previous, current, startsHunk, _, rows) -> [|
            if startsHunk then
                {
                    Key = $"hunk:{hunkId}"
                    Content = HunkHeader(hunkHeader previous current)
                }

            for row in rows do
                {
                    Key = row.Id
                    Content = AlignedRow(row, false)
                }
          |]
        | PagedPart.UnalignedRegion(hunkId, previous, current, previousRange, currentRange) ->
            let rowCount = max previous.Length current.Length
            // A large hunk can be split into several unaligned fragments with the same hunk id.
            // The range starts differ per fragment and come from the source, so the keys stay
            // unique and survive rerenders and replays.
            let fragmentKey =
                $"unaligned:{hunkId}:{numberText previousRange.Start}:{numberText currentRange.Start}"

            [|
                {
                    Key = $"{fragmentKey}:label"
                    Content = UnalignedLabel(hunkId, previousRange, currentRange)
                }

                for index in 0 .. rowCount - 1 do
                    {
                        Key = $"{fragmentKey}:{index}"
                        Content =
                            UnalignedLines(
                                (if index < previous.Length then
                                     Some previous.[index]
                                 else
                                     None),
                                (if index < current.Length then
                                     Some current.[index]
                                 else
                                     None)
                            )
                    }
            |]
        | PagedPart.HiddenGap(gapId, previous, current) -> [|
            {
                Key = $"gap:{gapId}"
                Content = Gap(gapId, previous, current)
            }
          |]
        | PagedPart.ExpandedRows(_, rows) ->
            rows
            |> Array.map (fun row -> {
                Key = row.Id
                Content = AlignedRow(row, true)
            })
        | PagedPart.EvictedPage(pageId, rowCount) -> [|
            {
                Key = $"evicted:{pageId}"
                Content = Evicted(pageId, rowCount)
            }
          |]

    let endingText ending =
        match ending with
        | PagedLineEnding.NoEnding -> "No ending"
        | PagedLineEnding.LF -> "LF"
        | PagedLineEnding.CRLF -> "CRLF"
        | PagedLineEnding.CR -> "CR"

    let lineNumberText number = numberText (number + 1.0)

    let private changedSide side kind =
        match kind, side with
        | PagedRowKind.Added, PagedDiffSide.Current
        | PagedRowKind.Replaced, _
        | PagedRowKind.EndingChanged, PagedDiffSide.Previous -> true
        | PagedRowKind.Removed, PagedDiffSide.Previous -> true
        | PagedRowKind.EndingChanged, PagedDiffSide.Current -> true
        | _ -> false

    let rowKindClass side kind forceContext =
        if forceContext || kind = PagedRowKind.Context then
            "swt:text-base-content/45 swt:bg-base-100"
        elif changedSide side kind then
            let theme = GitTextComparisonRendering.Rendering.DiffTheme

            if side = PagedDiffSide.Previous then
                theme.LeftChanged.ChangedLineClass
            else
                theme.RightChanged.ChangedLineClass
        else
            "swt:text-base-content/45 swt:bg-base-100"

    let lineNumberClass side kind forceContext =
        if forceContext || kind = PagedRowKind.Context then
            "swt:text-base-content/45 swt:bg-base-100"
        elif changedSide side kind then
            let theme = GitTextComparisonRendering.Rendering.DiffTheme

            if side = PagedDiffSide.Previous then
                theme.LeftChanged.ChangedLineNumberClass
            else
                theme.RightChanged.ChangedLineNumberClass
        else
            "swt:text-base-content/45 swt:bg-base-100"

    let changedSegmentClass side =
        let theme = GitTextComparisonRendering.Rendering.DiffTheme

        if side = PagedDiffSide.Previous then
            theme.LeftChanged.ChangedSegmentClass
        else
            theme.RightChanged.ChangedSegmentClass

    let formatBytes bytes =
        if bytes >= 1048576.0 then
            $"{Math.Round(bytes / 1048576.0, 1)} MiB"
        elif bytes >= 1024.0 then
            $"{Math.Round(bytes / 1024.0, 1)} KiB"
        else
            $"{numberText bytes} B"

    let progressPercentage (progress: PagedProgress) =
        if progress.TotalBytes <= 0.0 then
            if progress.ScanComplete then 100 else 0
        else
            clamp 0 100 (int (Math.Round(progress.ValidatedBytes / progress.TotalBytes * 100.0)))

    /// An evicted page with rows counts as content, since its rows come back when it is replayed.
    let hasChanges (parts: PagedPart[]) =
        parts
        |> Array.exists (
            function
            | PagedPart.HunkRows _
            | PagedPart.UnalignedRegion _ -> true
            | PagedPart.EvictedPage(_, rowCount) -> rowCount > 0
            | _ -> false
        )

    let changeBadge (parts: PagedPart[]) noChanges =
        if noChanges then
            "No changes"
        else
            let kinds =
                parts
                |> Array.collect (
                    function
                    | PagedPart.HunkRows(_, _, _, _, _, rows)
                    | PagedPart.ExpandedRows(_, rows) -> rows |> Array.map (fun row -> row.Kind)
                    | _ -> [||]
                )

            let hasAdded = kinds |> Array.contains PagedRowKind.Added
            let hasRemoved = kinds |> Array.contains PagedRowKind.Removed

            match hasAdded, hasRemoved with
            | true, false -> "Added"
            | false, true -> "Deleted"
            | _ -> "Changed"

    let statusName status =
        match status with
        | PagedDiffStatus.Opening -> "opening"
        | PagedDiffStatus.Scanning -> "scanning"
        | PagedDiffStatus.EncodingChoice _ -> "encoding-choice"
        | PagedDiffStatus.Ready -> "ready"
        | PagedDiffStatus.LoadingNext -> "loading-next"
        | PagedDiffStatus.Reopening -> "reopening"
        | PagedDiffStatus.Blocked _ -> "blocked"
        | PagedDiffStatus.SourceChanged -> "source-changed"
        | PagedDiffStatus.WorkerFailed _ -> "worker-failed"
        | PagedDiffStatus.Failed _ -> "failed"

    let sideName side =
        match side with
        | PagedDiffSide.Previous -> "previous"
        | PagedDiffSide.Current -> "current"

    let kindName kind =
        match kind with
        | PagedRowKind.Context -> "context"
        | PagedRowKind.Added -> "added"
        | PagedRowKind.Removed -> "removed"
        | PagedRowKind.Replaced -> "replaced"
        | PagedRowKind.EndingChanged -> "ending-changed"

    let endingName ending =
        match ending with
        | PagedLineEnding.NoEnding -> "none"
        | PagedLineEnding.LF -> "lf"
        | PagedLineEnding.CRLF -> "crlf"
        | PagedLineEnding.CR -> "cr"

    let snippetEndName snippetEnd =
        match snippetEnd with
        | PagedSnippetEnd.Truncated -> "truncated"
        | PagedSnippetEnd.MoreTextPending -> "more-text-pending"
        | PagedSnippetEnd.LineEnd -> "line-end"
        | PagedSnippetEnd.EndOfFile -> "end-of-file"

[<Erase; Mangle(false)>]
type GitPagedDiffViewer =

    [<ReactComponent>]
    static member private LineContent(props: GitPagedDiffDisplay.LineProps) =
        let kind =
            if props.ForceContext then
                PagedRowKind.Context
            else
                props.Kind

        match props.Line with
        | None ->
            Html.div [
                prop.custom ("data-side", GitPagedDiffDisplay.sideName props.Side)
                prop.custom ("data-kind", GitPagedDiffDisplay.kindName kind)
                prop.className "swt:grid swt:grid-cols-[3.5rem_minmax(0,1fr)] swt:min-w-0"
            ]
        | Some line ->
            let textLength = line.Text.Length
            let highlights = line.Highlights |> Array.sortBy (fun highlight -> highlight.Start)

            let segments = ResizeArray<ReactElement>()
            let mutable cursor = 0

            for index, highlight in highlights |> Array.indexed do
                let startIndex = GitPagedDiffDisplay.clamp cursor textLength highlight.Start

                let endIndex =
                    GitPagedDiffDisplay.clamp startIndex textLength (highlight.Start + highlight.Length)

                if startIndex > cursor then
                    segments.Add(
                        Html.span [
                            prop.key $"text-{index}"
                            prop.text (line.Text.Substring(cursor, startIndex - cursor))
                        ]
                    )

                if endIndex > startIndex then
                    segments.Add(
                        Html.span [
                            prop.key $"highlight-{index}"
                            prop.custom ("data-highlight", (if highlight.Changed then "changed" else "unchanged"))
                            if highlight.Changed then
                                prop.className (GitPagedDiffDisplay.changedSegmentClass props.Side)
                            prop.text (line.Text.Substring(startIndex, endIndex - startIndex))
                        ]
                    )

                cursor <- endIndex

            if cursor < textLength then
                segments.Add(Html.span [ prop.key "tail"; prop.text (line.Text.Substring(cursor)) ])

            let hasMore =
                line.TotalUtf16
                |> Option.map (fun total -> total > line.OffsetUtf16 + float textLength)
                |> Option.defaultValue true

            let lineStyle =
                GitPagedDiffDisplay.rowKindClass props.Side props.Kind props.ForceContext

            let lineNumber = GitPagedDiffDisplay.numberText line.Number
            let side = GitPagedDiffDisplay.sideName props.Side

            Html.div [
                prop.testId $"{props.Prefix}-line-{side}-{lineNumber}"
                prop.custom ("data-side", side)
                prop.custom ("data-kind", GitPagedDiffDisplay.kindName kind)
                prop.className "swt:grid swt:grid-cols-[3.5rem_minmax(0,1fr)] swt:min-w-0"
                prop.children [
                    Html.div [
                        prop.className [
                            "swt:px-3 swt:py-1 swt:text-right swt:text-xs swt:leading-5 swt:select-none swt:border-r swt:border-base-content/10"
                            GitPagedDiffDisplay.lineNumberClass props.Side props.Kind props.ForceContext
                        ]
                        prop.text (GitPagedDiffDisplay.lineNumberText line.Number)
                    ]
                    // The text wraps inside its column, and the controls sit after it outside the
                    // text flow, so a long line neither covers the other column nor hides them.
                    Html.div [
                        prop.testId $"{props.Prefix}-line-text-{side}-{lineNumber}"
                        prop.className [
                            "swt:flex swt:min-w-0 swt:items-start swt:gap-2 swt:px-3 swt:py-1 swt:font-mono swt:text-xs swt:leading-5"
                            lineStyle
                        ]
                        prop.children [
                            Html.span [
                                prop.className "swt:min-w-0 swt:flex-1"
                                prop.style [ style.whitespace.prewrap; style.overflowWrap.anywhere ]
                                prop.children [
                                    if line.OffsetUtf16 > 0.0 then
                                        Html.span [
                                            prop.className "swt:mr-1 swt:text-base-content/45"
                                            prop.text "…"
                                        ]

                                    if segments.Count = 0 then
                                        Html.span [ prop.text " " ]
                                    else
                                        React.Fragment(List.ofSeq segments)
                                ]
                            ]

                            if props.Kind = PagedRowKind.EndingChanged && not props.ForceContext then
                                Html.span [
                                    prop.testId $"{props.Prefix}-ending-{side}-{lineNumber}"
                                    prop.custom ("data-ending", GitPagedDiffDisplay.endingName line.Ending)
                                    prop.className "swt:badge swt:badge-outline swt:badge-xs swt:shrink-0"
                                    prop.text (GitPagedDiffDisplay.endingText line.Ending)
                                ]

                            if hasMore then
                                Html.button [
                                    prop.testId $"{props.Prefix}-line-more-{side}-{lineNumber}"
                                    prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:shrink-0"
                                    prop.disabled (props.SlicePending || props.RequestLineSlice.IsNone)
                                    prop.onClick (fun _ ->
                                        props.RequestLineSlice
                                        |> Option.iter (fun callback ->
                                            callback props.Side line.Number (line.OffsetUtf16 + float textLength)
                                        )
                                    )
                                    prop.text "Load more"
                                ]
                        ]
                    ]
                ]
            ]

    [<ReactComponent>]
    static member private PendingPanel(prefix: string, pending: PagedPending) =
        let mismatchFor side =
            pending.Mismatch
            |> Option.map (fun (previous, current) -> if side = PagedDiffSide.Previous then previous else current)

        let renderSide (sideTestId: string) (label: string) (side: PagedPendingSide) (mismatchOffset: float option) =
            let mismatchOffset = mismatchOffset |> Option.map int

            Html.div [
                prop.testId $"{prefix}-pending-{sideTestId}"
                prop.className "swt:min-w-0 swt:rounded-box swt:border swt:border-base-content/10 swt:p-3"
                prop.children [
                    Html.div [
                        prop.className
                            "swt:mb-2 swt:text-[11px] swt:font-semibold swt:uppercase swt:tracking-wide swt:text-base-content/60"
                        prop.text label
                    ]
                    match side with
                    | PagedPendingSide.NoActiveLine ->
                        Html.span [
                            prop.className "swt:text-xs swt:text-base-content/55"
                            prop.text "No active line"
                        ]
                    | PagedPendingSide.Exhausted lineCount ->
                        Html.span [
                            prop.className "swt:text-xs swt:text-base-content/55"
                            prop.text $"No further line, {GitPagedDiffDisplay.numberText lineCount} lines read"
                        ]
                    | PagedPendingSide.Snippet(line, snippetOffset, text, endState) ->
                        // The mismatch offset counts from the start of the line, and the snippet
                        // can start further into the line.
                        let marker =
                            mismatchOffset
                            |> Option.map (fun offset ->
                                GitPagedDiffDisplay.clamp 0 text.Length (offset - int snippetOffset)
                            )

                        let startsMidLine = snippetOffset > 0.0

                        let textWithMarker = [
                            if startsMidLine then
                                Html.span [
                                    prop.testId $"{prefix}-pending-leading-{sideTestId}"
                                    prop.className "swt:text-base-content/45"
                                    prop.text "…"
                                ]
                            match marker with
                            | Some offset ->
                                Html.span [ prop.text (text.Substring(0, offset)) ]

                                Html.span [
                                    prop.testId $"{prefix}-pending-mismatch-{sideTestId}"
                                    prop.custom ("data-offset", offset)
                                    prop.className "swt:text-error swt:font-bold"
                                    prop.text "│"
                                ]

                                Html.span [ prop.text (text.Substring(offset)) ]
                            | None -> Html.span [ prop.text text ]
                        ]

                        Html.div [
                            prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-1"
                            prop.children [
                                Html.span [
                                    prop.className "swt:text-xs swt:text-base-content/60"
                                    prop.text $"Line {GitPagedDiffDisplay.lineNumberText line}"
                                ]
                                Html.code [
                                    prop.testId $"{prefix}-pending-snippet-{sideTestId}"
                                    prop.custom ("data-starts-mid-line", (if startsMidLine then "true" else "false"))
                                    prop.className
                                        "swt:min-w-0 swt:whitespace-pre-wrap swt:break-all swt:font-mono swt:text-xs"
                                    prop.children textWithMarker
                                ]
                                Html.span [
                                    prop.testId $"{prefix}-pending-end-{sideTestId}"
                                    prop.custom ("data-end", GitPagedDiffDisplay.snippetEndName endState)
                                    prop.className "swt:text-[11px] swt:text-base-content/55"
                                    prop.text (
                                        match endState with
                                        | PagedSnippetEnd.MoreTextPending -> "Still reading"
                                        | PagedSnippetEnd.LineEnd -> "Line end"
                                        | PagedSnippetEnd.EndOfFile -> "End of file"
                                        | PagedSnippetEnd.Truncated -> "…"
                                    )
                                ]
                            ]
                        ]
                ]
            ]

        Html.div [
            prop.testId $"{prefix}-pending"
            prop.className
                "swt:col-span-2 swt:grid swt:grid-cols-2 swt:gap-3 swt:border-t swt:border-base-content/10 swt:p-3"
            prop.children [
                Html.div [
                    prop.className "swt:col-span-2 swt:text-xs swt:text-base-content/60"
                    prop.text "Alignment is pending"
                ]
                renderSide "previous" "Previous" pending.Previous (mismatchFor PagedDiffSide.Previous)
                renderSide "current" "Current" pending.Current (mismatchFor PagedDiffSide.Current)
            ]
        ]

    [<ReactComponent>]
    static member private RowView(props: GitPagedDiffDisplay.RowProps) =
        let fullWidth (className: string) (children: ReactElement list) : ReactElement =
            Html.div [
                prop.className $"swt:col-span-2 swt:flex swt:items-center swt:gap-3 swt:px-4 swt:py-2 {className}"
                prop.children children
            ]

        let cell (side: PagedDiffSide) (kind: PagedRowKind) (forceContext: bool) (line: PagedLine option) =
            GitPagedDiffViewer.LineContent {
                Line = line
                Side = side
                Kind = kind
                ForceContext = forceContext
                Prefix = props.Prefix
                RequestLineSlice = props.RequestLineSlice
                SlicePending =
                    line
                    |> Option.exists (fun shown ->
                        props.PendingLineSlices
                        |> Array.exists (fun request -> request.Side = side && request.Line = shown.Number)
                    )
            }

        let content =
            match props.Row.Content with
            | GitPagedDiffDisplay.HunkHeader header ->
                fullWidth
                    "swt:border-y swt:border-base-content/10 swt:bg-base-200/80 swt:font-mono swt:text-xs swt:text-base-content/70"
                    [ Html.span [ prop.text header ] ]
            | GitPagedDiffDisplay.AlignedRow(row, forceContext) ->
                React.Fragment [
                    cell PagedDiffSide.Previous row.Kind forceContext row.Previous
                    cell PagedDiffSide.Current row.Kind forceContext row.Current
                ]
            | GitPagedDiffDisplay.Gap(gapId, previous, _) ->
                let isExpanding = props.ExpandingGaps |> Array.contains gapId

                // The control at the top of the gap sits below the previous hunk and reveals the
                // first hidden lines. The control at the bottom sits above the next hunk and
                // reveals the last hidden lines.
                let expandButton (fromStart: bool) =
                    let position, icon, label =
                        if fromStart then
                            "start", "swt:fluent--arrow-down-24-regular", "Show the first hidden lines"
                        else
                            "end", "swt:fluent--arrow-up-24-regular", "Show the last hidden lines"

                    Html.button [
                        prop.testId $"{props.Prefix}-gap-expand-{position}-{gapId}"
                        prop.custom ("data-from-start", (if fromStart then "true" else "false"))
                        prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:gap-1"
                        prop.title label
                        prop.ariaLabel label
                        prop.disabled (isExpanding || props.RequestExpand.IsNone)
                        prop.onClick (fun _ ->
                            props.RequestExpand |> Option.iter (fun callback -> callback gapId fromStart)
                        )
                        prop.children [
                            Html.span [ prop.className [ "swt:iconify"; icon; "swt:size-4" ] ]
                        ]
                    ]

                Html.div [
                    prop.className
                        "swt:col-span-2 swt:flex swt:flex-col swt:items-center swt:gap-0.5 swt:bg-base-200/45 swt:px-4 swt:py-1 swt:text-xs swt:text-base-content/65"
                    prop.children [
                        expandButton true
                        Html.span [
                            prop.className "swt:whitespace-nowrap"
                            prop.text $"{GitPagedDiffDisplay.numberText previous.Count} hidden lines"
                        ]
                        expandButton false
                    ]
                ]
            | GitPagedDiffDisplay.UnalignedLabel(_, previous, current) ->
                fullWidth
                    "swt:border-y swt:border-base-content/10 swt:bg-warning/10 swt:font-mono swt:text-xs swt:text-base-content/70"
                    [
                        Html.span [
                            prop.text (GitPagedDiffDisplay.hunkHeader previous current)
                        ]
                    ]
            | GitPagedDiffDisplay.UnalignedLines(previous, current) ->
                React.Fragment [
                    cell PagedDiffSide.Previous PagedRowKind.Removed false previous
                    cell PagedDiffSide.Current PagedRowKind.Added false current
                ]
            | GitPagedDiffDisplay.Evicted(pageId, rowCount) ->
                let replaying = props.PendingReplays |> Array.contains pageId

                Html.div [
                    prop.className
                        "swt:col-span-2 swt:flex swt:items-center swt:justify-center swt:gap-3 swt:px-4 swt:bg-base-200/60 swt:text-xs swt:text-base-content/65"
                    prop.custom ("data-row-count", rowCount)
                    prop.style [
                        style.height (GitPagedDiffDisplay.RowHeightPx * max 1 rowCount)
                    ]
                    prop.children [
                        Html.span [ prop.text $"{rowCount} rows unloaded" ]
                        Html.button [
                            prop.testId $"{props.Prefix}-evicted-{pageId}"
                            prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                            prop.disabled (replaying || props.RequestReplay.IsNone)
                            prop.onClick (fun _ -> props.RequestReplay |> Option.iter (fun callback -> callback pageId))
                            prop.text "Reload rows"
                        ]
                    ]
                ]
            | GitPagedDiffDisplay.Continue pending ->
                let busy = props.LoadingNext

                fullWidth
                    "swt:flex-col swt:items-stretch swt:gap-2 swt:border-t swt:border-base-content/10 swt:bg-base-200/35 swt:py-3"
                    [
                        match pending with
                        | Some value -> GitPagedDiffViewer.PendingPanel(props.Prefix, value)
                        | None -> ()

                        if props.HasMore then
                            Html.div [
                                prop.testId $"{props.Prefix}-continue"
                                prop.className "swt:flex swt:flex-wrap swt:items-center swt:justify-between swt:gap-3"
                                prop.children [
                                    match props.Progress with
                                    | Some value ->
                                        Html.span [
                                            prop.className "swt:text-xs swt:text-base-content/60"
                                            prop.text
                                                $"{GitPagedDiffDisplay.progressPercentage value}%% ({GitPagedDiffDisplay.formatBytes value.ValidatedBytes} / {GitPagedDiffDisplay.formatBytes value.TotalBytes})"
                                        ]
                                    | None -> ()
                                    Html.span [
                                        prop.className "swt:text-xs swt:text-base-content/65"
                                        prop.text "More diff content is available"
                                    ]
                                    Html.button [
                                        prop.testId $"{props.Prefix}-continue-button"
                                        prop.className "swt:btn swt:btn-primary swt:btn-xs"
                                        prop.disabled (busy || props.RequestNext.IsNone)
                                        prop.onClick (fun _ ->
                                            props.RequestNext |> Option.iter (fun callback -> callback ())
                                        )
                                        prop.text (if busy then "Loading" else "Continue loading diff")
                                    ]
                                ]
                            ]
                    ]

        Html.div [
            prop.testId $"{props.Prefix}-row-{props.Row.Key}"
            prop.custom ("data-index", props.Index)
            prop.custom ("data-paged-diff-key", props.Row.Key)
            prop.ref (fun element -> props.MeasureElementRef(Option.ofObj element))
            prop.className
                "swt:absolute swt:left-0 swt:grid swt:w-full swt:min-w-232 swt:grid-cols-2 swt:divide-x swt:divide-base-content/10"
            prop.style [
                style.top 0
                style.left 0
                style.custom ("transform", $"translateY({props.Start}px)")
            ]
            prop.children [ content ]
        ]

    [<ReactComponent>]
    static member private Grid
        (
            rows: GitPagedDiffDisplay.Row[],
            rowParts: int[],
            previousTitle: string,
            currentTitle: string,
            prefix: string,
            lastPart: obj,
            hasMore: bool,
            progress: PagedProgress option,
            status: PagedDiffStatus,
            expandingGaps: string[],
            requestExpand: (string -> bool -> unit) option,
            requestLineSlice: (PagedDiffSide -> float -> float -> unit) option,
            pendingLineSlices: PagedLineSliceRequest[],
            pendingReplays: string[],
            requestReplay: (string -> int -> int -> unit) option,
            requestNext: (unit -> unit) option
        ) =
        let rowHeight = GitPagedDiffDisplay.RowHeightPx
        let headerScrollRef: IRefValue<HTMLElement option> = React.useElementRef ()
        let bodyScrollRef: IRefValue<HTMLElement option> = React.useElementRef ()
        let bodyContentRef: IRefValue<HTMLElement option> = React.useElementRef ()
        let contentMeasureRef, contentRect = React.useMeasure<Element> ()
        let previousLayout = React.useRef<GitPagedDiffDisplay.AnchorSnapshot option> None

        // While the diff reopens, the rows stay on screen and take no requests.
        let interactive = status <> PagedDiffStatus.Reopening
        let requestExpand = requestExpand |> Option.filter (fun _ -> interactive)
        let requestLineSlice = requestLineSlice |> Option.filter (fun _ -> interactive)
        let requestReplay = requestReplay |> Option.filter (fun _ -> interactive)
        let requestNext = requestNext |> Option.filter (fun _ -> interactive)

        React.useEffect (
            (fun () ->
                contentMeasureRef (bodyContentRef.current |> Option.map unbox<Element>)
                FsReact.createDisposable (fun () -> contentMeasureRef None)
            ),
            [| box contentMeasureRef |]
        )

        React.useEffect (
            (fun () ->
                match headerScrollRef.current, bodyScrollRef.current with
                | Some headerScroll, Some bodyScroll ->
                    let syncHeaderToBody (_: Event) =
                        headerScroll.scrollLeft <- bodyScroll.scrollLeft

                    bodyScroll.addEventListener ("scroll", syncHeaderToBody)
                    syncHeaderToBody (unbox null)
                    FsReact.createDisposable (fun () -> bodyScroll.removeEventListener ("scroll", syncHeaderToBody))
                | _ -> FsReact.createDisposable (fun () -> ())
            ),
            [||]
        )

        let contentWidth = contentRect.width |> Option.map int |> Option.defaultValue 0

        let headerContentWidth =
            if contentWidth > 0 then
                $"max(58rem, {contentWidth}px)"
            else
                "max(58rem, 100%)"

        let rowVirtualizer =
            Virtual.useVirtualizer (
                count = rows.Length,
                getScrollElement = (fun () -> bodyScrollRef.current),
                estimateSize =
                    (fun index ->
                        match rows.[index].Content with
                        | GitPagedDiffDisplay.Evicted(_, rowCount) -> rowHeight * max 1 rowCount
                        | _ -> rowHeight
                    ),
                getItemKey = (fun index -> rows.[index].Key),
                overscan = 8,
                gap = 0
            )

        let visibleItems = rowVirtualizer.getVirtualItems ()

        let virtualItems: GitPagedDiffDisplay.VirtualRow[] =
            if visibleItems.Length = 0 && rows.Length > 0 then
                let initialPosition =
                    bodyScrollRef.current
                    |> Option.map (fun element -> element.scrollTop = 0.0)
                    |> Option.defaultValue true

                if initialPosition then
                    [| 0 .. min (rows.Length - 1) 8 |]
                    |> Array.map (fun index ->
                        ({
                            Key = rows.[index].Key
                            Index = index
                            Start = index * rowHeight
                            Size = rowHeight
                        }
                        : GitPagedDiffDisplay.VirtualRow)
                    )
                else
                    [||]
            else
                visibleItems
                |> Array.map (fun item ->
                    ({
                        Key = item.key
                        Index = item.index
                        Start = item.start
                        Size = item.size
                    }
                    : GitPagedDiffDisplay.VirtualRow)
                )

        // The rendered rows include the overscan. Replays only go to rows the user can see.
        let viewportItems () =
            match bodyScrollRef.current with
            | Some element ->
                let top = int element.scrollTop
                let bottom = top + int element.clientHeight

                virtualItems
                |> Array.filter (fun item -> item.Start < bottom && item.Start + item.Size > top)
            | None -> [||]

        // The first and the last part with a row in the viewport, or -1 for both without one.
        let visiblePartRange (items: GitPagedDiffDisplay.VirtualRow[]) =
            let indices =
                items
                |> Array.map (fun item -> rowParts.[item.Index])
                |> Array.filter (fun index -> index >= 0)

            if indices.Length = 0 then
                -1, -1
            else
                Array.min indices, Array.max indices

        let captureAnchor () : GitPagedDiffDisplay.AnchorSnapshot =
            let scrollTop =
                bodyScrollRef.current
                |> Option.map (fun element -> element.scrollTop)
                |> Option.defaultValue 0.0

            let visibleStart = int scrollTop

            let visibleEnd =
                bodyScrollRef.current
                |> Option.map (fun element -> int (scrollTop + element.clientHeight))
                |> Option.defaultValue visibleStart

            let candidates =
                rowVirtualizer.getVirtualItems ()
                |> Array.filter (fun item -> item.``end`` > visibleStart && item.start < visibleEnd)
                |> Array.sortBy (fun item -> item.start)
                |> Array.map (fun item -> ({ Key = item.key; Start = item.start }: GitPagedDiffDisplay.AnchorCandidate))

            ({
                Keys = rows |> Array.map (fun row -> row.Key)
                Candidates = candidates
            }
            : GitPagedDiffDisplay.AnchorSnapshot)

        let rowsSignature = rows |> Array.map (fun row -> row.Key) |> String.concat "\u001f"

        React.useLayoutEffect (
            (fun () ->
                match previousLayout.current with
                | Some previous when previous.Keys <> (rows |> Array.map (fun row -> row.Key)) ->
                    let surviving =
                        previous.Candidates
                        |> Array.tryPick (fun candidate ->
                            if rows |> Array.exists (fun row -> row.Key = candidate.Key) then
                                Some candidate
                            else
                                None
                        )

                    match surviving, bodyScrollRef.current with
                    | Some anchor, Some scrollElement ->
                        let nextStart =
                            rowVirtualizer.measurementsCache
                            |> Array.tryFind (fun item -> item.key = anchor.Key)
                            |> Option.map (fun item -> item.start)

                        nextStart
                        |> Option.iter (fun start ->
                            let previousScrollTop = scrollElement.scrollTop
                            scrollElement.scrollTop <- previousScrollTop + float (start - anchor.Start)
                        )
                    | _ -> ()
                | _ -> ()

                previousLayout.current <- Some(captureAnchor ())
            ),
            [| box rowsSignature |]
        )

        let renderedKeys =
            virtualItems
            |> Array.map (fun item -> rows.[item.Index].Key)
            |> String.concat "\u001f"

        let viewportKeys =
            viewportItems ()
            |> Array.map (fun item -> rows.[item.Index].Key)
            |> String.concat "\u001f"

        let replaySignature = pendingReplays |> String.concat "\u001f"
        let requestedReplay = React.useRef (HashSet<string>())
        let lastReplaySignature = React.useRef replaySignature
        // Holds the last part at the time of the last next page request. It changes when a page
        // is added or the last page is replaced. Evicting earlier pages leaves it alone.
        let requestedNext = React.useRef<obj> (obj ())

        React.useEffect (
            (fun () ->
                // A replay the loader dropped while another one ran is asked for again once the
                // running replays change.
                if lastReplaySignature.current <> replaySignature then
                    lastReplaySignature.current <- replaySignature
                    requestedReplay.current.Clear()

                // A replayed page can be evicted again later. Forgetting pages that are no longer
                // placeholders lets the viewer ask for them once more.
                let evictedIds =
                    rows
                    |> Array.choose (fun row ->
                        match row.Content with
                        | GitPagedDiffDisplay.Evicted(pageId, _) -> Some pageId
                        | _ -> None
                    )
                    |> Set.ofArray

                requestedReplay.current <- HashSet<string>(requestedReplay.current |> Seq.filter evictedIds.Contains)

                // One replay at a time, for the first placeholder the user can see.
                match requestReplay with
                | Some callback when pendingReplays.Length = 0 ->
                    let viewport = viewportItems ()

                    viewport
                    |> Array.tryPick (fun item ->
                        match rows.[item.Index].Content with
                        | GitPagedDiffDisplay.Evicted(pageId, _) when not (requestedReplay.current.Contains pageId) ->
                            Some pageId
                        | _ -> None
                    )
                    |> Option.iter (fun pageId ->
                        requestedReplay.current.Add pageId |> ignore
                        let first, last = visiblePartRange viewport
                        callback pageId first last
                    )
                | _ -> ()

                let continueRendered =
                    virtualItems
                    |> Array.exists (fun item ->
                        match rows.[item.Index].Content with
                        | GitPagedDiffDisplay.Continue _ -> true
                        | _ -> false
                    )

                match requestNext with
                | Some callback when
                    continueRendered
                    && status <> PagedDiffStatus.LoadingNext
                    && not (Object.ReferenceEquals(requestedNext.current, lastPart))
                    ->
                    requestedNext.current <- lastPart
                    callback ()
                | _ -> ()

                FsReact.createDisposable (fun () -> ())
            ),
            [|
                box renderedKeys
                box viewportKeys
                box status
                box requestReplay
                box requestNext
                box replaySignature
                lastPart
            |]
        )

        let replayVisible =
            requestReplay
            |> Option.map (fun callback ->
                fun pageId ->
                    let first, last = visiblePartRange (viewportItems ())
                    callback pageId first last
            )

        Html.div [
            prop.testId $"{prefix}-grid"
            prop.className "swt:flex swt:min-h-0 swt:flex-1 swt:flex-col"
            prop.children [
                Html.div [
                    prop.ref headerScrollRef
                    prop.className "swt:overflow-hidden"
                    prop.children [
                        Html.div [
                            prop.className "swt:min-w-232"
                            prop.style [ style.custom ("width", headerContentWidth) ]
                            prop.children [
                                Html.div [
                                    prop.className "swt:grid swt:grid-cols-2 swt:divide-x swt:divide-base-content/10"
                                    prop.children [
                                        for label, title in [ "Previous", previousTitle; "Current", currentTitle ] do
                                            Html.div [
                                                prop.className
                                                    "swt:flex swt:items-start swt:justify-between swt:gap-3 swt:px-4 swt:py-3 swt:bg-base-200 swt:border-b swt:border-base-content/10"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "swt:min-w-0 swt:flex swt:flex-col swt:gap-0.5"
                                                        prop.children [
                                                            Html.span [
                                                                prop.className
                                                                    "swt:text-[11px] swt:uppercase swt:tracking-wide swt:text-base-content/60"
                                                                prop.text label
                                                            ]
                                                            Html.span [
                                                                prop.className
                                                                    "swt:truncate swt:text-sm swt:font-semibold"
                                                                prop.text title
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.ref bodyScrollRef
                    prop.className "swt:min-h-0 swt:flex-1 swt:overflow-auto swt:scrollbar-fade"
                    prop.onScroll (fun _ -> previousLayout.current <- Some(captureAnchor ()))
                    prop.children [
                        Html.div [
                            prop.ref bodyContentRef
                            prop.className "swt:relative swt:w-full swt:min-w-232"
                            prop.style [ style.height (rowVirtualizer.getTotalSize ()) ]
                            prop.children [
                                for item in virtualItems do
                                    React.KeyedFragment(
                                        item.Key,
                                        [
                                            GitPagedDiffViewer.RowView {
                                                Row = rows.[item.Index]
                                                Index = item.Index
                                                Start = item.Start
                                                MeasureElementRef = rowVirtualizer.measureElement
                                                Prefix = prefix
                                                ExpandingGaps = expandingGaps
                                                LoadingNext = status = PagedDiffStatus.LoadingNext
                                                HasMore = hasMore
                                                Progress = progress
                                                RequestExpand = requestExpand
                                                RequestLineSlice = requestLineSlice
                                                PendingLineSlices = pendingLineSlices
                                                PendingReplays = pendingReplays
                                                RequestReplay = replayVisible
                                                RequestNext = requestNext
                                            }
                                        ]
                                    )
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private ProgressState
        (prefix: string, stateName: string, progress: PagedProgress option, pending: PagedPending option)
        =
        let progressContent =
            match progress with
            | None ->
                Html.span [
                    prop.className "swt:text-xs swt:text-base-content/60"
                    prop.text ""
                ]
            | Some value ->
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.div [
                            prop.className
                                "swt:flex swt:items-center swt:justify-between swt:gap-3 swt:text-xs swt:text-base-content/70"
                            prop.children [
                                Html.span [
                                    prop.text $"{GitPagedDiffDisplay.progressPercentage value}%%"
                                ]
                                Html.span [
                                    prop.text
                                        $"{GitPagedDiffDisplay.formatBytes value.ValidatedBytes} / {GitPagedDiffDisplay.formatBytes value.TotalBytes}"
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:h-1.5 swt:overflow-hidden swt:rounded-full swt:bg-base-300"
                            prop.children [
                                Html.div [
                                    prop.className "swt:h-full swt:bg-primary"
                                    prop.style [
                                        style.width (length.percent (GitPagedDiffDisplay.progressPercentage value))
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]

        Html.div [
            prop.testId $"{prefix}-state-{stateName}"
            prop.className "swt:flex swt:min-h-0 swt:flex-1 swt:flex-col swt:gap-3 swt:p-4"
            prop.children [
                progressContent
                match pending with
                | Some value -> GitPagedDiffViewer.PendingPanel(prefix, value)
                | None -> ()
            ]
        ]

    [<ReactComponent>]
    static member private MessageState(prefix: string, stateName: string, heading: string, detail: string option) =
        Html.div [
            prop.testId $"{prefix}-state-{stateName}"
            prop.className
                "swt:flex swt:min-h-0 swt:flex-1 swt:flex-col swt:items-center swt:justify-center swt:gap-2 swt:p-8 swt:text-center"
            prop.children [
                Html.h4 [
                    prop.className "swt:text-sm swt:font-semibold"
                    prop.text heading
                ]
                match detail with
                | Some value ->
                    Html.p [
                        prop.className "swt:max-w-prose swt:text-xs swt:text-base-content/65"
                        prop.text value
                    ]
                | None -> ()
            ]
        ]

    [<ReactComponent>]
    static member private EncodingState
        (
            prefix: string,
            side: PagedDiffSide,
            sideTitle: string,
            candidates: PagedEncodingCandidate[],
            chooseEncoding: (PagedDiffSide -> string -> unit) option
        ) =
        let sideName =
            match side with
            | PagedDiffSide.Previous -> "previous"
            | PagedDiffSide.Current -> "current"

        Html.div [
            prop.testId $"{prefix}-state-encoding-choice"
            prop.className "swt:flex swt:min-h-0 swt:flex-1 swt:flex-col swt:gap-3 swt:p-4"
            prop.children [
                Html.div [
                    prop.testId $"{prefix}-encoding-side"
                    prop.custom ("data-side", sideName)
                    prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-0.5"
                    prop.children [
                        Html.span [
                            prop.className "swt:text-sm swt:font-semibold"
                            prop.text $"Choose the encoding of the {sideName} version"
                        ]
                        Html.span [
                            prop.className "swt:truncate swt:text-xs swt:text-base-content/60"
                            prop.text sideTitle
                        ]
                    ]
                ]
                for candidate in candidates do
                    Html.div [
                        prop.className
                            "swt:flex swt:flex-col swt:gap-2 swt:rounded-box swt:border swt:border-base-300 swt:p-3"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:items-center swt:justify-between swt:gap-3"
                                prop.children [
                                    Html.span [
                                        prop.className "swt:text-sm swt:font-semibold"
                                        prop.text candidate.Encoding
                                    ]
                                    Html.button [
                                        prop.testId $"{prefix}-encoding-choice-{candidate.Encoding}"
                                        prop.className "swt:btn swt:btn-primary swt:btn-xs"
                                        prop.disabled chooseEncoding.IsNone
                                        prop.onClick (fun _ ->
                                            chooseEncoding
                                            |> Option.iter (fun callback -> callback side candidate.Encoding)
                                        )
                                        prop.text "Choose encoding"
                                    ]
                                ]
                            ]
                            Html.pre [
                                prop.className
                                    "swt:max-h-48 swt:overflow-auto swt:whitespace-pre-wrap swt:break-all swt:rounded-box swt:bg-base-200/60 swt:p-2 swt:font-mono swt:text-xs"
                                prop.text candidate.Preview
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private ReopeningBar(prefix: string, progress: PagedProgress option) =
        let percentage =
            progress
            |> Option.map GitPagedDiffDisplay.progressPercentage
            |> Option.defaultValue 0

        Html.div [
            prop.testId $"{prefix}-reopening"
            prop.custom ("data-progress", percentage)
            prop.className "swt:h-1 swt:w-full swt:shrink-0 swt:overflow-hidden swt:bg-base-300"
            prop.children [
                Html.div [
                    prop.className "swt:h-full swt:bg-primary"
                    prop.style [ style.width (length.percent percentage) ]
                ]
            ]
        ]

    [<ReactComponent(true)>]
    static member Viewer
        (
            parts: PagedPart[],
            status: PagedDiffStatus,
            progress: PagedProgress option,
            hasMore: bool,
            outputComplete: bool,
            ?pending: PagedPending,
            ?requestNext: unit -> unit,
            ?requestExpand: (string -> bool -> unit),
            ?expandingGaps: string[],
            ?requestLineSlice: (PagedDiffSide -> float -> float -> unit),
            ?pendingLineSlices: PagedLineSliceRequest[],
            // Asks for the rows of an evicted page. The two numbers are the indices of the first
            // and the last part with a row in the viewport, or -1 for both when no part is visible.
            ?requestReplay: (string -> int -> int -> unit),
            ?pendingReplays: string[],
            ?chooseEncoding: (PagedDiffSide -> string -> unit),
            ?previousTitle: string,
            ?currentTitle: string,
            ?changeKind: GitDiffChangeKind,
            ?testIdPrefix: string
        ) =
        let prefix = defaultArg testIdPrefix "git-paged-diff"
        let previousTitle = defaultArg previousTitle "Previous version"
        let currentTitle = defaultArg currentTitle "Current version"
        let pending = pending
        let partRowCache = React.useRef<(obj * GitPagedDiffDisplay.Row[]) list> []

        let rows = ResizeArray<GitPagedDiffDisplay.Row>()
        // The index of the part each row belongs to, -1 for rows outside the parts.
        let rowParts = ResizeArray<int>()
        let nextCache = ResizeArray<obj * GitPagedDiffDisplay.Row[]>()

        for partIndex, part in parts |> Array.indexed do
            let identity = box part

            let cached =
                partRowCache.current
                |> List.tryPick (fun (cachedIdentity, cachedRows) ->
                    if Object.ReferenceEquals(identity, cachedIdentity) then
                        Some cachedRows
                    else
                        None
                )

            let partRows =
                cached |> Option.defaultWith (fun () -> GitPagedDiffDisplay.buildPartRows part)

            nextCache.Add(identity, partRows)
            rows.AddRange partRows

            for _ in partRows do
                rowParts.Add partIndex

        if hasMore then
            rows.Add {
                Key = "continue"
                Content = GitPagedDiffDisplay.Continue pending
            }

            rowParts.Add -1
        elif pending.IsSome then
            rows.Add {
                Key = "pending"
                Content = GitPagedDiffDisplay.Continue pending
            }

            rowParts.Add -1

        partRowCache.current <- List.ofSeq nextCache

        let noChanges =
            progress |> Option.exists (fun value -> value.ScanComplete)
            && outputComplete
            && not (GitPagedDiffDisplay.hasChanges parts)

        let changeBadge =
            match changeKind with
            | Some GitDiffChangeKind.Added -> "Added"
            | Some GitDiffChangeKind.Deleted -> "Deleted"
            | Some GitDiffChangeKind.Modified -> "Changed"
            | None -> GitPagedDiffDisplay.changeBadge parts noChanges

        let content =
            match status with
            | PagedDiffStatus.Opening ->
                GitPagedDiffViewer.ProgressState(prefix, GitPagedDiffDisplay.statusName status, progress, pending)
            | PagedDiffStatus.Scanning ->
                GitPagedDiffViewer.ProgressState(prefix, GitPagedDiffDisplay.statusName status, progress, pending)
            | PagedDiffStatus.Reopening when parts.Length = 0 ->
                GitPagedDiffViewer.ProgressState(prefix, GitPagedDiffDisplay.statusName status, progress, pending)
            | PagedDiffStatus.EncodingChoice(side, candidates) ->
                let sideTitle =
                    match side with
                    | PagedDiffSide.Previous -> previousTitle
                    | PagedDiffSide.Current -> currentTitle

                GitPagedDiffViewer.EncodingState(prefix, side, sideTitle, candidates, chooseEncoding)
            | PagedDiffStatus.Blocked(side, reason) ->
                let sideText =
                    side
                    |> Option.map (
                        function
                        | PagedDiffSide.Previous -> "Previous"
                        | PagedDiffSide.Current -> "Current"
                    )

                let detail =
                    sideText
                    |> Option.map (fun value -> $"{value}: {reason}")
                    |> Option.orElse (Some reason)

                GitPagedDiffViewer.MessageState(prefix, "blocked", "Content is blocked", detail)
            | PagedDiffStatus.SourceChanged ->
                GitPagedDiffViewer.MessageState(prefix, "source-changed", "The source changed", None)
            | PagedDiffStatus.WorkerFailed message ->
                GitPagedDiffViewer.MessageState(prefix, "worker-failed", "The diff worker failed", Some message)
            | PagedDiffStatus.Failed message ->
                GitPagedDiffViewer.MessageState(prefix, "failed", "The diff could not be opened", Some message)
            | PagedDiffStatus.Ready
            | PagedDiffStatus.LoadingNext
            | PagedDiffStatus.Reopening ->
                if noChanges && status <> PagedDiffStatus.Reopening then
                    Html.div [
                        prop.testId $"{prefix}-no-changes"
                        prop.className
                            "swt:flex swt:min-h-0 swt:flex-1 swt:items-center swt:justify-center swt:p-8 swt:text-sm swt:text-base-content/60"
                        prop.text "No changes"
                    ]
                else
                    // Both statuses render the same two children, so the grid keeps its scroll
                    // position when a reopen starts or ends.
                    React.Fragment [
                        if status = PagedDiffStatus.Reopening then
                            GitPagedDiffViewer.ReopeningBar(prefix, progress)
                        else
                            Html.none
                        GitPagedDiffViewer.Grid(
                            rows.ToArray(),
                            rowParts.ToArray(),
                            previousTitle,
                            currentTitle,
                            prefix,
                            (parts |> Array.tryLast |> Option.map box |> Option.defaultValue null),
                            hasMore,
                            progress,
                            status,
                            defaultArg expandingGaps [||],
                            requestExpand,
                            requestLineSlice,
                            defaultArg pendingLineSlices [||],
                            defaultArg pendingReplays [||],
                            requestReplay,
                            requestNext
                        )
                    ]

        GitComparisonView.PanelShell
            (React.Fragment [
                GitComparisonView.HeaderRow
                    (GitComparisonView.TitleStack
                        (Html.h3 [
                            prop.className "swt:text-sm swt:font-semibold"
                            prop.text "Git Diff"
                        ])
                        None
                        None)
                    (Html.span [
                        prop.className "swt:badge swt:badge-outline swt:badge-sm"
                        prop.text changeBadge
                    ])
                    (Some "swt:border-b swt:border-base-content/10 swt:bg-base-100")
                content
            ])
            (Some $"{prefix}-root")
            (Some "swt:flex swt:h-full swt:w-full swt:min-h-0 swt:min-w-0 swt:flex-col")
            None
