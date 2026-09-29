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

    type VirtualRow = { Key: string; Index: int; Start: int }

    type RowProps = {
        Row: Row
        Index: int
        Start: int
        MeasureElementRef: VirtualMeasureElementRef
        Prefix: string
        ExpandingGap: string option
        LoadingNext: bool
        HasMore: bool
        Progress: PagedProgress option
        RequestExpand: (string -> bool -> unit) option
        RequestLineSlice: (PagedDiffSide -> float -> float -> unit) option
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
    }

    type AnchorCandidate = { Key: string; Start: int }

    type AnchorSnapshot = {
        Keys: string[]
        Candidates: AnchorCandidate[]
    }

    let private rangeStart (range: PagedRange) =
        if range.Count = 0.0 then range.Start else range.Start + 1.0

    let clamp (lower: int) (upper: int) (value: int) = min upper (max lower value)

    let numberText value = sprintf "%.0f" value

    let private hunkHeader previous current =
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

    let hasChanges (parts: PagedPart[]) =
        parts
        |> Array.exists (
            function
            | PagedPart.HunkRows _
            | PagedPart.UnalignedRegion _ -> true
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
        | PagedDiffStatus.Expanding _ -> "expanding"
        | PagedDiffStatus.Blocked _ -> "blocked"
        | PagedDiffStatus.SourceChanged -> "source-changed"
        | PagedDiffStatus.WorkerFailed _ -> "worker-failed"
        | PagedDiffStatus.Closed -> "closed"
        | PagedDiffStatus.Failed _ -> "failed"

[<Erase; Mangle(false)>]
type GitPagedDiffViewer =

    [<ReactComponent>]
    static member private LineContent(props: GitPagedDiffDisplay.LineProps) =
        match props.Line with
        | None ->
            Html.div [
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

            Html.div [
                prop.className "swt:grid swt:grid-cols-[3.5rem_minmax(0,1fr)] swt:min-w-0"
                prop.children [
                    Html.div [
                        prop.className [
                            "swt:px-3 swt:py-1 swt:text-right swt:text-xs swt:leading-5 swt:select-none swt:border-r swt:border-base-content/10"
                            GitPagedDiffDisplay.lineNumberClass props.Side props.Kind props.ForceContext
                        ]
                        prop.text (GitPagedDiffDisplay.lineNumberText line.Number)
                    ]
                    Html.div [
                        prop.className [
                            "swt:px-3 swt:py-1 swt:min-w-0 swt:font-mono swt:text-xs swt:leading-5"
                            lineStyle
                        ]
                        prop.style [ style.whitespace.pre; style.overflowWrap.anywhere ]
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

                            if props.Kind = PagedRowKind.EndingChanged && not props.ForceContext then
                                Html.span [
                                    prop.className "swt:badge swt:badge-outline swt:badge-xs swt:ml-2"
                                    prop.text (GitPagedDiffDisplay.endingText line.Ending)
                                ]

                            if hasMore then
                                Html.button [
                                    prop.testId
                                        $"{props.Prefix}-line-more-{props.Side}-{GitPagedDiffDisplay.numberText line.Number}"
                                    prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:ml-2"
                                    prop.disabled props.RequestLineSlice.IsNone
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
                    | PagedPendingSide.Snippet(line, _, text, endState) ->
                        let marker =
                            mismatchOffset
                            |> Option.map (fun offset -> GitPagedDiffDisplay.clamp 0 text.Length offset)

                        let textWithMarker =
                            match marker with
                            | Some offset -> [
                                Html.span [ prop.text (text.Substring(0, offset)) ]
                                Html.span [
                                    prop.testId $"{prefix}-pending-mismatch-{sideTestId}"
                                    prop.className "swt:text-error swt:font-bold"
                                    prop.text "│"
                                ]
                                Html.span [ prop.text (text.Substring(offset)) ]
                              ]
                            | None -> [ Html.span [ prop.text text ] ]

                        Html.div [
                            prop.className "swt:flex swt:min-w-0 swt:flex-col swt:gap-1"
                            prop.children [
                                Html.span [
                                    prop.className "swt:text-xs swt:text-base-content/60"
                                    prop.text $"Line {GitPagedDiffDisplay.lineNumberText line}"
                                ]
                                Html.code [
                                    prop.className
                                        "swt:min-w-0 swt:whitespace-pre-wrap swt:break-all swt:font-mono swt:text-xs"
                                    prop.children textWithMarker
                                ]
                                Html.span [
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
                let isExpanding = props.ExpandingGap = Some gapId

                fullWidth "swt:justify-center swt:bg-base-200/45 swt:text-xs swt:text-base-content/65" [
                    Html.button [
                        prop.testId $"{props.Prefix}-gap-expand-up-{gapId}"
                        prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                        prop.disabled (isExpanding || props.RequestExpand.IsNone)
                        prop.onClick (fun _ -> props.RequestExpand |> Option.iter (fun callback -> callback gapId true))
                        prop.text "Expand up"
                    ]
                    Html.span [
                        prop.className "swt:whitespace-nowrap"
                        prop.text $"{GitPagedDiffDisplay.numberText previous.Count} hidden lines"
                    ]
                    Html.button [
                        prop.testId $"{props.Prefix}-gap-expand-down-{gapId}"
                        prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                        prop.disabled (isExpanding || props.RequestExpand.IsNone)
                        prop.onClick (fun _ ->
                            props.RequestExpand |> Option.iter (fun callback -> callback gapId false)
                        )
                        prop.text "Expand down"
                    ]
                ]
            | GitPagedDiffDisplay.UnalignedLabel(hunkId, previous, current) ->
                fullWidth
                    "swt:border-y swt:border-base-content/10 swt:bg-warning/10 swt:text-xs swt:text-base-content/70"
                    [
                        Html.span [
                            prop.className "swt:font-semibold"
                            prop.text $"Unaligned region {hunkId}"
                        ]
                        Html.span [
                            prop.text
                                $"Previous {GitPagedDiffDisplay.numberText previous.Count}, current {GitPagedDiffDisplay.numberText current.Count}"
                        ]
                    ]
            | GitPagedDiffDisplay.UnalignedLines(previous, current) ->
                React.Fragment [
                    cell PagedDiffSide.Previous PagedRowKind.Context false previous
                    cell PagedDiffSide.Current PagedRowKind.Context false current
                ]
            | GitPagedDiffDisplay.Evicted(pageId, rowCount) ->
                fullWidth "swt:justify-center swt:bg-base-200/60 swt:text-xs swt:text-base-content/65" [
                    Html.span [ prop.text $"{rowCount} rows unloaded" ]
                    Html.button [
                        prop.testId $"{props.Prefix}-evicted-{pageId}"
                        prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                        prop.disabled props.RequestReplay.IsNone
                        prop.onClick (fun _ -> props.RequestReplay |> Option.iter (fun callback -> callback pageId))
                        prop.text "Reload rows"
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
            previousTitle: string,
            currentTitle: string,
            prefix: string,
            partsLength: int,
            hasMore: bool,
            progress: PagedProgress option,
            status: PagedDiffStatus,
            requestExpand: (string -> bool -> unit) option,
            requestLineSlice: (PagedDiffSide -> float -> float -> unit) option,
            requestReplay: (string -> unit) option,
            requestNext: (unit -> unit) option
        ) =
        let rowEstimatePx = 28
        let headerScrollRef: IRefValue<HTMLElement option> = React.useElementRef ()
        let bodyScrollRef: IRefValue<HTMLElement option> = React.useElementRef ()
        let bodyContentRef: IRefValue<HTMLElement option> = React.useElementRef ()
        let contentMeasureRef, contentRect = React.useMeasure<Element> ()
        let previousLayout = React.useRef<GitPagedDiffDisplay.AnchorSnapshot option> None

        let expandingGap =
            match status with
            | PagedDiffStatus.Expanding gapId -> Some gapId
            | _ -> None

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
                estimateSize = (fun _ -> rowEstimatePx),
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
                            Start = index * rowEstimatePx
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
                    }
                    : GitPagedDiffDisplay.VirtualRow)
                )

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
                            rowVirtualizer.getMeasurements ()
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

        let requestedReplay = React.useRef (HashSet<string>())
        let requestedNext = React.useRef (HashSet<int>())

        React.useEffect (
            (fun () ->
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

                for item in virtualItems do
                    match rows.[item.Index].Content with
                    | GitPagedDiffDisplay.Evicted(pageId, _) ->
                        if requestReplay.IsSome && requestedReplay.current.Add pageId then
                            requestReplay.Value pageId
                    | GitPagedDiffDisplay.Continue _ ->
                        let canRequest =
                            match status with
                            | PagedDiffStatus.LoadingNext -> false
                            | _ -> true

                        if canRequest && requestNext.IsSome && requestedNext.current.Add partsLength then
                            requestNext.Value()
                    | _ -> ()

                FsReact.createDisposable (fun () -> ())
            ),
            [|
                box renderedKeys
                box status
                box requestReplay
                box requestNext
                box partsLength
            |]
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
                                                ExpandingGap = expandingGap
                                                LoadingNext = status = PagedDiffStatus.LoadingNext
                                                HasMore = hasMore
                                                Progress = progress
                                                RequestExpand = requestExpand
                                                RequestLineSlice = requestLineSlice
                                                RequestReplay = requestReplay
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
            candidates: PagedEncodingCandidate[],
            chooseEncoding: (PagedDiffSide -> string -> unit) option
        ) =
        Html.div [
            prop.testId $"{prefix}-state-encoding-choice"
            prop.className "swt:flex swt:min-h-0 swt:flex-1 swt:flex-col swt:gap-3 swt:p-4"
            prop.children [
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
            ?requestLineSlice: (PagedDiffSide -> float -> float -> unit),
            ?requestReplay: (string -> unit),
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
        let requestedReplay = requestReplay
        let requestedNext = requestNext

        let rows = ResizeArray<GitPagedDiffDisplay.Row>()
        let nextCache = ResizeArray<obj * GitPagedDiffDisplay.Row[]>()

        for part in parts do
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

        if hasMore then
            rows.Add {
                Key = "continue"
                Content = GitPagedDiffDisplay.Continue pending
            }
        elif pending.IsSome then
            rows.Add {
                Key = "pending"
                Content = GitPagedDiffDisplay.Continue pending
            }

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
            | PagedDiffStatus.EncodingChoice(side, candidates) ->
                GitPagedDiffViewer.EncodingState(prefix, side, candidates, chooseEncoding)
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
            | PagedDiffStatus.Closed -> GitPagedDiffViewer.MessageState(prefix, "closed", "The diff is closed", None)
            | PagedDiffStatus.Failed message ->
                GitPagedDiffViewer.MessageState(prefix, "failed", "The diff could not be opened", Some message)
            | PagedDiffStatus.Ready
            | PagedDiffStatus.LoadingNext
            | PagedDiffStatus.Expanding _ ->
                if noChanges then
                    Html.div [
                        prop.testId $"{prefix}-no-changes"
                        prop.className
                            "swt:flex swt:min-h-0 swt:flex-1 swt:items-center swt:justify-center swt:p-8 swt:text-sm swt:text-base-content/60"
                        prop.text "No changes"
                    ]
                else
                    GitPagedDiffViewer.Grid(
                        rows.ToArray(),
                        previousTitle,
                        currentTitle,
                        prefix,
                        parts.Length,
                        hasMore,
                        progress,
                        status,
                        requestExpand,
                        requestLineSlice,
                        requestReplay,
                        requestNext
                    )

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
