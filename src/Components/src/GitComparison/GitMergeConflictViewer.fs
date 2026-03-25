namespace Swate.Components

open System.Globalization
open Browser.Dom
open Fable.Core
open Feliz

module private GitMergeConflictViewerInternal =

    type ConflictResolutionChoice =
        | Incoming
        | Current

    type ConflictResolutionState = {
        Choice: ConflictResolutionChoice
        AppliedText: string
        StartIndex: int
    }

    type ConflictPaneState =
        | Unresolved of GitTextComparisonCore.MergeConflictBlock
        | Resolved of GitTextComparisonCore.MergeConflictBlock * ConflictResolutionState

    type ConflictPaneEntry = {
        EntryId: int
        State: ConflictPaneState
    }

    let parseConflictBlocks (content: string) =
        GitTextComparisonCore.MergeConflicts.parseMergeConflictFragments content
        |> List.choose (function
            | GitTextComparisonCore.ConflictBlock block -> Some block
            | GitTextComparisonCore.PlainText _ -> None
        )

    let parsePaneEntries (content: string) =
        parseConflictBlocks content
        |> List.mapi (fun index block ->
            {
                EntryId = index + 1
                State = Unresolved block
            }
        )

    let countUnresolvedEntries (entries: ConflictPaneEntry list) =
        entries
        |> List.sumBy (fun entry ->
            match entry.State with
            | Unresolved _ -> 1
            | Resolved _ -> 0
        )

    let shiftResolvedEntryOffsetsAfter cutoffIndex delta (entries: ConflictPaneEntry list) =
        if delta = 0 then
            entries
        else
            entries
            |> List.map (fun entry ->
                match entry.State with
                | Resolved(block, resolution) when resolution.StartIndex > cutoffIndex ->
                    {
                        entry with
                            State =
                                Resolved(
                                    block,
                                    {
                                        resolution with
                                            StartIndex = resolution.StartIndex + delta
                                    }
                                )
                    }
                | _ ->
                    entry
            )

    let refreshPaneEntries (content: string) (entries: ConflictPaneEntry list) =
        let unresolvedBlocks = parseConflictBlocks content
        let expectedUnresolvedCount = countUnresolvedEntries entries

        if unresolvedBlocks.Length <> expectedUnresolvedCount then
            parsePaneEntries content
        else
            let mutable unresolvedIndex = 0

            entries
            |> List.map (fun entry ->
                match entry.State with
                | Resolved _ ->
                    entry
                | Unresolved _ ->
                    let nextBlock = unresolvedBlocks.[unresolvedIndex]
                    unresolvedIndex <- unresolvedIndex + 1

                    {
                        entry with
                            State = Unresolved nextBlock
                    }
            )

    [<ReactComponent>]
    let VerticalSplitHandle
        (
            onPointerDown: Browser.Types.PointerEvent -> unit,
            testId: string option
        ) =
        Html.div [
            if testId.IsSome then
                prop.testId testId.Value
            prop.className
                "swt:group swt:flex swt:items-center swt:justify-center swt:cursor-row-resize swt:bg-base-200 hover:swt:bg-base-300"
            prop.onPointerDown onPointerDown
            prop.style [ style.custom ("touch-action", "none") ]
            prop.children [
                Html.div [
                    prop.className "swt:h-1 swt:w-20 swt:rounded-full swt:bg-base-content/20 group-hover:swt:bg-base-content/35"
                ]
                ]
            ]

    [<ReactComponent>]
    let VerticalSplitPane
        (
            topPane: ReactElement,
            bottomPane: ReactElement,
            splitHandleTestId: string option
        ) =
        let splitPaneRef = React.useElementRef ()
        let splitPercent, setSplitPercent = React.useState 50
        let splitPercentRef = React.useRef 50
        let isDragging = React.useRef false

        let commitSplitPercent nextSplitPercent =
            if splitPercentRef.current <> nextSplitPercent then
                splitPercentRef.current <- nextSplitPercent
                setSplitPercent nextSplitPercent

        let updateSplitPercentFromClientY (clientY: float) =
            match splitPaneRef.current with
            | Some container ->
                let rect = container.getBoundingClientRect()
                let relativeY = clientY - rect.top

                let rawPercent =
                    if rect.height <= 0. then
                        float splitPercentRef.current
                    else
                        (relativeY / rect.height) * 100.

                let clampedPercent =
                    rawPercent
                    |> max 25.
                    |> min 75.
                    |> round
                    |> int

                commitSplitPercent clampedPercent
            | None ->
                ()

        React.useEffectOnce (fun () ->
            let onMove =
                fun (moveEvent: Browser.Types.PointerEvent) ->
                    if isDragging.current then
                        updateSplitPercentFromClientY moveEvent.clientY
                    else
                        ()

            let stopDragging =
                fun (_: Browser.Types.PointerEvent) ->
                    isDragging.current <- false

            document.addEventListener ("pointermove", unbox onMove)
            document.addEventListener ("pointerup", unbox stopDragging)
            document.addEventListener ("pointercancel", unbox stopDragging)

            FsReact.createDisposable (fun () ->
                document.removeEventListener ("pointermove", unbox onMove)
                document.removeEventListener ("pointerup", unbox stopDragging)
                document.removeEventListener ("pointercancel", unbox stopDragging)
            )
        )

        let startSplitDrag (event: Browser.Types.PointerEvent) =
            event.preventDefault ()
            isDragging.current <- true
            updateSplitPercentFromClientY event.clientY

        let topFractionText = splitPercent.ToString(CultureInfo.InvariantCulture)
        let bottomFractionText = (100 - splitPercent).ToString(CultureInfo.InvariantCulture)

        Html.div [
            prop.ref splitPaneRef
            prop.className "swt:grid swt:min-h-0 swt:flex-1"
            prop.style [
                style.custom (
                    "grid-template-rows",
                    $"minmax(0, {topFractionText}fr) 0.75rem minmax(0, {bottomFractionText}fr)"
                )
            ]
            prop.children [
                topPane
                VerticalSplitHandle(startSplitDrag, splitHandleTestId)
                bottomPane
            ]
        ]

    [<ReactComponent>]
    let RenderConflictBlock
        (
            entryId: int,
            block: GitTextComparisonCore.MergeConflictBlock,
            incomingTitle: string option,
            currentTitle: string option,
            applyResolution: ConflictResolutionChoice -> int -> GitTextComparisonCore.MergeConflictBlock -> string -> unit,
            comparisonScrollTestId: string option
        ) =
        let incomingHeaderLabel, currentHeaderLabel, rows, incomingLineCount, currentLineCount =
            React.useMemo (
                (fun () ->
                    let incomingHeaderLabel =
                        GitTextComparisonCore.Metadata.resolveHeaderLabel "Incoming" incomingTitle block.IncomingLabel

                    let currentHeaderLabel =
                        GitTextComparisonCore.Metadata.resolveHeaderLabel "Current" currentTitle block.CurrentLabel

                    let rows = GitTextComparisonCore.Rows.buildRows block.IncomingContent block.CurrentContent
                    let incomingLineCount = (GitTextComparisonCore.Text.splitContentToLines block.IncomingContent).Length
                    let currentLineCount = (GitTextComparisonCore.Text.splitContentToLines block.CurrentContent).Length

                    incomingHeaderLabel, currentHeaderLabel, rows, incomingLineCount, currentLineCount
                ),
                [| box block; box incomingTitle; box currentTitle |]
            )

        let header =
            GitComparisonView.HeaderRow
                (GitComparisonView.TitleStack
                    (Html.h4 [
                        prop.className "swt:text-sm swt:font-semibold"
                        prop.text $"Merge conflict {entryId}"
                    ])
                    None
                    None)
                (Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-sm swt:btn-outline"
                            prop.text "Take incoming"
                            prop.onClick (fun _ ->
                                applyResolution ConflictResolutionChoice.Incoming entryId block block.IncomingContent
                            )
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-sm swt:btn-outline"
                            prop.text "Take current"
                            prop.onClick (fun _ ->
                                applyResolution ConflictResolutionChoice.Current entryId block block.CurrentContent
                            )
                        ]
                    ]
                ])
                (Some "swt:border-b swt:border-base-content/10 swt:bg-base-100")

        let body =
            GitTextComparisonRendering.Rendering.ComparisonGrid(
                rows,
                ("Incoming", (incomingHeaderLabel, incomingLineCount)),
                ("Current", (currentHeaderLabel, currentLineCount)),
                None,
                None,
                comparisonScrollTestId,
                Some 260,
                Some GitTextComparisonRendering.Rendering.ChoiceTheme
            )

        GitComparisonView.SectionCard
            header
            body
            (Some $"merge-conflict-{entryId}")
            (Some "swt:overflow-hidden")

    [<ReactComponent>]
    let ResolvedConflictSummary
        (
            entryId: int,
            block: GitTextComparisonCore.MergeConflictBlock,
            resolution: ConflictResolutionState,
            undoResolution: int -> GitTextComparisonCore.MergeConflictBlock -> ConflictResolutionState -> unit
        ) =
        let resolutionLabel =
            match resolution.Choice with
            | ConflictResolutionChoice.Incoming -> "incoming"
            | ConflictResolutionChoice.Current -> "current"

        let header =
            GitComparisonView.HeaderRow
                (GitComparisonView.TitleStack
                    (Html.h4 [
                        prop.className "swt:text-sm swt:font-semibold"
                        prop.text $"Merge conflict {entryId}"
                    ])
                    (Some(
                        Html.p [
                            prop.className "swt:text-xs swt:text-base-content/60"
                            prop.text $"Resolved with {resolutionLabel}. Expand again with Undo if you want to choose differently."
                        ]
                    ))
                    None)
                (Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                    prop.children [
                        Html.span [
                            prop.className "swt:badge swt:badge-ghost swt:badge-sm"
                            prop.text $"Using {resolutionLabel}"
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-sm"
                            prop.text "Undo"
                            prop.onClick (fun _ -> undoResolution entryId block resolution)
                        ]
                    ]
                ])
                None

        GitComparisonView.SectionCard
            header
            Html.none
            (Some $"merge-conflict-{entryId}")
            None

[<Erase; Mangle(false)>]
type GitMergeConflictViewer =

    [<ReactComponent>]
    static member Viewer
        (
            mergeConflictContent: string,
            ?resolvedContent: string,
            ?defaultResolvedContent: string,
            ?onResolvedContentChange: (string -> unit),
            ?onConfirmMerge: (string -> unit),
            ?incomingTitle: string,
            ?currentTitle: string,
            ?resolvedTitle: string,
            ?heightPx: int,
            ?minHeightPx: int,
            ?className: string,
            ?testIdPrefix: string
        ) =
        let normalizedConflictContent = GitTextComparisonCore.Text.normalizeLineEndings mergeConflictContent
        let normalizedControlledResolvedText = resolvedContent |> Option.map GitTextComparisonCore.Text.normalizeLineEndings
        let normalizedDefaultResolvedText = defaultResolvedContent |> Option.map GitTextComparisonCore.Text.normalizeLineEndings

        match normalizedControlledResolvedText, normalizedDefaultResolvedText, onResolvedContentChange with
        | Some _, Some _, _ ->
            failwith
                "GitMergeConflictViewer: `resolvedContent` and `defaultResolvedContent` cannot be used together."
        | Some _, None, None ->
            failwith
                "GitMergeConflictViewer: `resolvedContent` requires `onResolvedContentChange`."
        | _ ->
            ()

        let isControlled = normalizedControlledResolvedText.IsSome

        let initialUncontrolledResolvedText =
            normalizedDefaultResolvedText
            |> Option.defaultValue normalizedConflictContent

        let internalResolvedText, setInternalResolvedText = React.useState initialUncontrolledResolvedText

        let initialActiveResolvedText =
            normalizedControlledResolvedText
            |> Option.defaultValue initialUncontrolledResolvedText

        let paneEntries, setPaneEntries =
            React.useState (GitMergeConflictViewerInternal.parsePaneEntries initialActiveResolvedText)

        let skipPaneSync = React.useRef false

        React.useEffect (
            (fun () ->
                if not isControlled then
                    let nextResolvedText =
                        normalizedDefaultResolvedText
                        |> Option.defaultValue normalizedConflictContent

                    setInternalResolvedText nextResolvedText),
            [| box normalizedConflictContent; box normalizedDefaultResolvedText; box isControlled |]
        )

        let activeResolvedText =
            normalizedControlledResolvedText
            |> Option.defaultValue internalResolvedText

        React.useEffect (
            (fun () ->
                if skipPaneSync.current then
                    skipPaneSync.current <- false
                else
                    setPaneEntries (GitMergeConflictViewerInternal.parsePaneEntries activeResolvedText)),
            [| box activeResolvedText |]
        )

        let updateResolvedText nextValue =
            let normalizedValue = GitTextComparisonCore.Text.normalizeLineEndings nextValue

            if isControlled then
                onResolvedContentChange.Value normalizedValue
            else
                setInternalResolvedText normalizedValue
                onResolvedContentChange |> Option.iter (fun notifyResolvedTextChanged -> notifyResolvedTextChanged normalizedValue)

        let handleResolvedTextChange nextValue =
            skipPaneSync.current <- false
            updateResolvedText nextValue

        let rootTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-root")

        let resolvedEditorTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-resolved-editor")

        let confirmMergeButtonTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-confirm-merge")

        let conflictsPaneTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-conflicts-pane")

        let splitHandleTestId =
            testIdPrefix |> Option.map (fun prefix -> prefix + "-split-handle")

        let resetPaneEntriesFromActiveText () =
            setPaneEntries (GitMergeConflictViewerInternal.parsePaneEntries activeResolvedText)

        let applyResolution
            (choice: GitMergeConflictViewerInternal.ConflictResolutionChoice)
            (entryId: int)
            (block: GitTextComparisonCore.MergeConflictBlock)
            (replacementText: string)
            =
            let normalizedReplacementText = GitTextComparisonCore.Text.normalizeLineEndings replacementText

            match
                GitTextComparisonCore.MergeConflicts.tryReplaceExactTextAt
                    block.StartIndex
                    block.RawContent
                    normalizedReplacementText
                    activeResolvedText
            with
            | Some nextResolvedText ->
                let resolution: GitMergeConflictViewerInternal.ConflictResolutionState = {
                    Choice = choice
                    AppliedText = normalizedReplacementText
                    StartIndex = block.StartIndex
                }

                let delta = normalizedReplacementText.Length - block.RawContent.Length

                let nextPaneEntries =
                    paneEntries
                    |> List.map (fun entry ->
                        if entry.EntryId = entryId then
                            {
                                entry with
                                    State = GitMergeConflictViewerInternal.Resolved(block, resolution)
                            }
                        else
                            entry
                    )
                    |> GitMergeConflictViewerInternal.shiftResolvedEntryOffsetsAfter block.StartIndex delta
                    |> GitMergeConflictViewerInternal.refreshPaneEntries nextResolvedText

                skipPaneSync.current <- true
                setPaneEntries nextPaneEntries
                updateResolvedText nextResolvedText
            | None ->
                skipPaneSync.current <- false
                resetPaneEntriesFromActiveText ()

        let undoResolution
            (entryId: int)
            (block: GitTextComparisonCore.MergeConflictBlock)
            (resolution: GitMergeConflictViewerInternal.ConflictResolutionState)
            =
            match
                GitTextComparisonCore.MergeConflicts.tryReplaceExactTextAt
                    resolution.StartIndex
                    resolution.AppliedText
                    block.RawContent
                    activeResolvedText
            with
            | Some nextResolvedText ->
                let delta = block.RawContent.Length - resolution.AppliedText.Length

                let nextPaneEntries =
                    paneEntries
                    |> List.map (fun entry ->
                        if entry.EntryId = entryId then
                            {
                                entry with
                                    State = GitMergeConflictViewerInternal.Unresolved block
                            }
                        else
                            entry
                    )
                    |> GitMergeConflictViewerInternal.shiftResolvedEntryOffsetsAfter resolution.StartIndex delta
                    |> GitMergeConflictViewerInternal.refreshPaneEntries nextResolvedText

                skipPaneSync.current <- true
                setPaneEntries nextPaneEntries
                updateResolvedText nextResolvedText
            | None ->
                skipPaneSync.current <- false
                resetPaneEntriesFromActiveText ()

        let hasRemainingConflictMarkers =
            React.useMemo (
                (fun () -> GitTextComparisonCore.MergeConflicts.containsConflictMarkers activeResolvedText),
                [| box activeResolvedText |]
            )

        let unresolvedConflictCount =
            React.useMemo (
                (fun () -> GitMergeConflictViewerInternal.countUnresolvedEntries paneEntries),
                [| box paneEntries |]
            )

        let resolvedLineCount =
            React.useMemo (
                (fun () -> (GitTextComparisonCore.Text.splitContentToLines activeResolvedText).Length),
                [| box activeResolvedText |]
            )

        let canConfirmMerge =
            onConfirmMerge.IsSome && not hasRemainingConflictMarkers

        let header =
            GitComparisonView.HeaderRow
                (GitComparisonView.TitleStack
                    (Html.h3 [
                        prop.className "swt:text-sm swt:font-semibold"
                        prop.text "Merge Conflicts"
                    ])
                    None
                    None)
                (Html.span [
                    prop.className "swt:badge swt:badge-outline swt:badge-sm"
                    prop.text $"{unresolvedConflictCount} conflicts"
                ])
                (Some "swt:border-b swt:border-base-content/10 swt:bg-base-100")

        let resolvedEditorHeader =
            GitComparisonView.HeaderRow
                (GitComparisonView.TitleStack
                    (Html.h4 [
                        prop.className "swt:text-sm swt:font-semibold"
                        prop.text (defaultArg resolvedTitle "Resolved file view")
                    ])
                    None
                    (Some "swt:gap-0.5"))
                (Html.span [
                    prop.className "swt:badge swt:badge-ghost swt:badge-sm"
                    prop.text $"{resolvedLineCount} lines"
                ])
                None

        let conflictsPane =
            Html.div [
                if conflictsPaneTestId.IsSome then
                    prop.testId conflictsPaneTestId.Value
                prop.className "swt:min-h-0 swt:border-b swt:border-base-content/10"
                prop.children [
                    if List.isEmpty paneEntries then
                        Html.div [
                            prop.className "swt:px-4 swt:py-3 swt:text-sm swt:text-base-content/70"
                            prop.text "No merge conflict markers were detected in the supplied content."
                        ]
                    else
                        Html.div [
                            prop.className "swt:flex swt:h-full swt:min-h-0 swt:flex-col"
                            prop.children [
                                Html.div [
                                    prop.className
                                        "swt:min-h-0 swt:flex-1 swt:overflow-y-auto swt:scrollbar-fade swt:p-4 swt:space-y-4"
                                    prop.children (
                                        paneEntries
                                        |> List.map (fun entry ->
                                            match entry.State with
                                            | GitMergeConflictViewerInternal.Resolved(block, resolution) ->
                                                GitMergeConflictViewerInternal.ResolvedConflictSummary(
                                                    entry.EntryId,
                                                    block,
                                                    resolution,
                                                    undoResolution
                                                )
                                            | GitMergeConflictViewerInternal.Unresolved block ->
                                                let comparisonScrollTestId =
                                                    testIdPrefix
                                                    |> Option.map (fun prefix -> $"{prefix}-conflict-{entry.EntryId}-scroll")

                                                GitMergeConflictViewerInternal.RenderConflictBlock(
                                                    entry.EntryId,
                                                    block,
                                                    incomingTitle,
                                                    currentTitle,
                                                    applyResolution,
                                                    comparisonScrollTestId
                                                )
                                        )
                                    )
                                ]
                            ]
                        ]
                ]
            ]

        let resolvedEditorPane =
            Html.div [
                prop.className "swt:flex swt:min-h-0 swt:flex-col swt:px-4 swt:py-3"
                prop.children [
                    resolvedEditorHeader
                    Html.textarea [
                        if resolvedEditorTestId.IsSome then
                            prop.testId resolvedEditorTestId.Value
                        prop.className
                            "swt:mt-2 swt:min-h-0 swt:flex-1 swt:w-full swt:rounded-box swt:border swt:border-base-300 swt:bg-base-100 swt:px-4 swt:py-3 swt:font-mono swt:text-xs swt:leading-6 swt:resize-none swt:focus:outline-hidden"
                        prop.value activeResolvedText
                        prop.onChange handleResolvedTextChange
                    ]
                    Html.div [
                        prop.className "swt:mt-3 swt:flex swt:justify-end"
                        prop.children [
                            Html.button [
                                if confirmMergeButtonTestId.IsSome then
                                    prop.testId confirmMergeButtonTestId.Value
                                prop.className "swt:btn swt:btn-primary"
                                prop.disabled (not canConfirmMerge)
                                prop.text "Confirm Merge"
                                prop.onClick (fun _ ->
                                    if canConfirmMerge then
                                        onConfirmMerge
                                        |> Option.iter (fun confirm -> confirm activeResolvedText)
                                )
                            ]
                        ]
                    ]
                ]
            ]

        let splitPane =
            GitMergeConflictViewerInternal.VerticalSplitPane(conflictsPane, resolvedEditorPane, splitHandleTestId)

        GitComparisonView.PanelShell
            (React.Fragment [
                header
                splitPane
            ])
            rootTestId
            (Some(
                match className with
                | Some value -> $"swt:flex swt:h-full swt:min-h-0 swt:flex-col {value}"
                | None -> "swt:flex swt:h-full swt:min-h-0 swt:flex-col"
            ))
            (Some [
                style.minHeight 0

                if heightPx.IsSome then
                    style.height heightPx.Value

                if minHeightPx.IsSome then
                    style.minHeight minHeightPx.Value
            ])
        
