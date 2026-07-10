namespace Swate.Components.Composite.Workspace

open Fable.Core
open Browser.Types
open Feliz
open Swate.Components
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context

[<Erase; Mangle(false)>]
type PaneNode =

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member LeafNode(paneId: PaneId, ?key: string) =
        let paneStateCtx = useWorkspacePaneStateCtx ()
        let paneIdKey = paneId.Value.ToString("N")

        let pane =
            paneStateCtx.panesMap
            |> Map.tryFind paneId
            |> Option.defaultValue {
                Id = paneId
                Tabs = []
                FocusedTab = None
            }

        let paneCtxValue: PaneContext = {
            paneId = paneId
            tabs = pane.Tabs |> Array.ofList
            focusedTab = pane.FocusedTab
        }

        PaneCtx.Provider(
            paneCtxValue,
            Html.div [
                prop.key (defaultArg key paneIdKey)
                prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:flex-1 swt:overflow-hidden"
                prop.custom ("data-workspace-pane", paneIdKey)
                if paneStateCtx.debug then
                    prop.testId $"workspace-pane-{paneIdKey}"
                prop.children [
                    TabBar.TabBar(paneId)
                    ContentArea.ContentArea(paneId)
                ]
            ]
        )

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member PaneNode(layout: Layout, ?key: string) =

        match layout with
        | Layout.Single leaf -> PaneNode.LeafNode(leaf, ?key = key)

        | Layout.Split(splitId, dir, ratio, l1, l2) ->
            let dispatchCtx = useWorkspaceDispatchCtx ()
            let paneStateCtx = useWorkspacePaneStateCtx ()

            let dragging = React.useRef false

            let splitContainerRef = React.useElementRef ()

            let pointerPosition, setPointerPosition = React.useState (None: float option)
            let throttledPointerPosition = React.useThrottle (pointerPosition, 16)

            let clampedRatio =
                match throttledPointerPosition with
                | Some pos -> Some(max 0.15 (min 0.85 pos))
                | None -> None

            React.useEffect (
                (fun () ->
                    match clampedRatio with
                    | Some clamped ->
                        dispatchCtx.dispatch (box (SetSplitRatio(splitId, clamped)))
                    | None -> ()
                ),
                [| box clampedRatio |]
            )

            React.useEffectOnce (fun () ->

                let onMove (e: PointerEvent) =
                    if dragging.current then
                        match splitContainerRef.current with
                        | None
                        | Some null -> ()
                        | Some splitContainer ->
                            let rect = splitContainer.getBoundingClientRect ()

                            let directionalPointerPosition =
                                match dir with
                                | SplitDirection.Horizontal -> (e.clientX - rect.left) / rect.width
                                | SplitDirection.Vertical -> (e.clientY - rect.top) / rect.height

                            setPointerPosition (Some directionalPointerPosition)

                let stop (_: PointerEvent) = dragging.current <- false

                Browser.Dom.document.addEventListener ("pointermove", unbox onMove)
                Browser.Dom.document.addEventListener ("pointerup", unbox stop)

                FsReact.createDisposable (fun () ->
                    Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                    Browser.Dom.document.removeEventListener ("pointerup", unbox stop)
                )
            )

            let flexDir =
                match dir with
                | SplitDirection.Horizontal -> "swt:flex-row"
                | SplitDirection.Vertical -> "swt:flex-col"

            let size1 = ratio * 100.0
            let size2 = 100.0 - size1

            let splitIdKey = splitId.Value.ToString("N")

            Html.div [
                prop.key (defaultArg key splitIdKey)
                prop.ref splitContainerRef
                prop.className $"swt:flex {flexDir} swt:min-w-0 swt:min-h-0 swt:flex-1 swt:overflow-hidden"
                if paneStateCtx.debug then
                    prop.testId $"workspace-split-{splitIdKey}"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:overflow-hidden"
                        prop.style [
                            match dir with
                            | SplitDirection.Horizontal -> style.width (length.perc size1)
                            | SplitDirection.Vertical -> style.height (length.perc size1)
                        ]
                        prop.children [ PaneNode.PaneNode l1 ]
                    ]
                    Html.div [
                        prop.onPointerDown (fun _ -> dragging.current <- true)
                        prop.className [
                            "swt:shrink-0 swt:select-none swt:transition-colors swt:bg-base-content swt:hover:bg-primary swt:z-10"
                            match dir with
                            | SplitDirection.Horizontal -> "swt:w-1 swt:cursor-col-resize swt:h-full"
                            | SplitDirection.Vertical -> "swt:h-1 swt:cursor-row-resize swt:w-full"
                        ]
                    ]
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:overflow-hidden"
                        prop.style [
                            match dir with
                            | SplitDirection.Horizontal -> style.width (length.perc size2)
                            | SplitDirection.Vertical -> style.height (length.perc size2)
                        ]
                        prop.children [ PaneNode.PaneNode l2 ]
                    ]
                ]
            ]
