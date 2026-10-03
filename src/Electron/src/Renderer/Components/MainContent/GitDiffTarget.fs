module Renderer.Components.MainContent.GitDiffTarget

open Fable.Core
open Feliz
open Renderer.Types
open Renderer.Context.GitWorkflow
open Swate.Components.Page.GitComparison

module Presentation = Renderer.GitDiffPresentation

[<AllowNullLiteral>]
type private IPerformance =
    /// Removes every User Timing measure of the page.
    abstract clearMeasures: unit -> unit

[<Global("performance")>]
let private performance: IPerformance = jsNative

let private clearPerformanceMeasures () : unit = performance.clearMeasures ()

[<ReactComponent>]
let Main (page: GitDiffPageData) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()
    let send = gitStateCtx.sendDiffMsg
    let generation = page.Generation

    // The development build of React records a User Timing measure for every component render
    // whose props changed, with the changed props copied into the measure. The browser keeps
    // these measures until they are cleared, outside the JavaScript heap. With thousands of diff
    // rows as props, each loaded page added megabytes that never came back. Clearing them when
    // the pages change and when the diff closes keeps that memory bounded. The production build
    // records no measures, and Swate reads none.
    React.useEffect ((fun () -> FsReact.createDisposable clearPerformanceMeasures), [| box page.Pages |])

    // Once the first page is ready, the loader reads every other page in the background, so the
    // viewer knows the extent of the whole diff.
    React.useEffect (
        (fun () -> GitDiffPageLoader.indexingRequest page |> Option.iter send),
        [|
            box generation
            box page.Indexing
            box page.NextFailed
            box page.NextCursor
            box page.Status
        |]
    )

    // The viewer rebuilds the rows of the parts only when the parts array changes.
    // The parts are collected only when the pages change.
    let parts =
        React.useMemo ((fun () -> page.Pages |> Array.collect _.Parts), [| box page.Pages |])

    // The page of each part, so the part range the viewer reports maps back to page ids.
    let partPages =
        React.useMemo (
            (fun () ->
                page.Pages
                |> Array.collect (fun windowPage -> windowPage.Parts |> Array.map (fun _ -> windowPage.PageId))
            ),
            [| box page.Pages |]
        )

    let visiblePages (firstPart: int) (lastPart: int) =
        if firstPart < 0 || lastPart < firstPart then
            []
        else
            [
                for index in firstPart .. min lastPart (partPages.Length - 1) -> partPages.[index]
            ]
            |> List.distinct

    let previousTitle, currentTitle =
        match page.SourceInfos with
        | Some infos -> Presentation.sourceTitle infos.Previous, Presentation.sourceTitle infos.Current
        | None -> page.PreviousPath |> Option.defaultValue page.Path, page.Path

    Html.div [
        prop.className "swt:flex swt:h-full swt:w-full swt:min-h-0 swt:min-w-0 swt:flex-col"
        prop.children [
            Html.div [
                prop.className
                    "swt:flex swt:items-center swt:justify-end swt:border-b swt:border-base-content/10 swt:bg-base-100 swt:px-4 swt:py-2"
                prop.children [
                    Html.button [
                        prop.testId "renderer-git-diff-close"
                        prop.className "swt:btn swt:btn-ghost swt:btn-sm swt:gap-2 swt:normal-case"
                        prop.onClick (fun _ -> pageStateCtx.setState None)
                        prop.children [
                            Html.span [
                                prop.className "swt:iconify swt:fluent--dismiss-24-regular swt:size-4"
                            ]
                            Html.span "Close"
                        ]
                    ]
                ]
            ]
            Html.div [
                prop.className "swt:min-h-0 swt:min-w-0 swt:flex-1 swt:p-4"
                prop.children [
                    GitPagedDiffViewer.Viewer(
                        parts = parts,
                        status = Presentation.status page.Status,
                        progress = (page.Progress |> Option.map Presentation.progress),
                        hasMore = page.NextCursor.IsSome,
                        indexing = (page.Indexing && not page.NextFailed),
                        nextFailed = page.NextFailed,
                        outputComplete = page.OutputComplete,
                        ?pending = (page.Pending |> Option.map Presentation.pending),
                        requestNext = (fun () -> send (GitDiffMsg.LoadNext generation)),
                        requestExpand = (fun gapId fromStart -> send (GitDiffMsg.Expand(generation, gapId, fromStart))),
                        expandingGaps = Array.ofList page.ExpandingGaps,
                        requestLineSlice =
                            (fun side line offsetUtf16 ->
                                send (
                                    GitDiffMsg.LoadLineSlice(generation, Presentation.sideDto side, line, offsetUtf16)
                                )
                            ),
                        requestLineBefore =
                            (fun side line displayedStart ->
                                send (
                                    GitDiffMsg.LoadLineBefore(
                                        generation,
                                        Presentation.sideDto side,
                                        line,
                                        displayedStart
                                    )
                                )
                            ),
                        pendingLineSlices = Array.ofList page.PendingLineSlices,
                        requestReplay =
                            (fun pageId firstPart lastPart ->
                                send (GitDiffMsg.Replay(generation, pageId, visiblePages firstPart lastPart))
                            ),
                        pendingReplays = Option.toArray page.PendingReplay,
                        chooseEncoding =
                            (fun side encoding ->
                                send (GitDiffMsg.ChooseEncoding(generation, Presentation.sideDto side, encoding))
                            ),
                        previousTitle = previousTitle,
                        currentTitle = currentTitle,
                        ?changeKind = page.ChangeKind,
                        testIdPrefix = "renderer-git-diff",
                        // Evicting or replaying the last page changes the last part but not the
                        // cursor, so the cursor keeps the continue row from reading again.
                        ?nextPageKey = page.NextCursor,
                        failedGaps = Array.ofList page.FailedGaps,
                        failedLineSlices = Array.ofList page.FailedLineSlices,
                        failedReplays = Array.ofList page.FailedReplays,
                        ?scrollTarget = page.ScrollTarget
                    )
                ]
            ]
        ]
    ]
