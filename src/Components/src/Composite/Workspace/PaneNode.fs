namespace Swate.Components.Composite.Workspace

open Fable.Core
open Browser.Types
open Feliz
open Swate.Components
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context
open Swate.Components.Composite.Widgets.Types

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

        let paneCtxValue: PaneContext<_> = {
            paneId = paneId
            tabs = pane.Tabs |> Array.ofList
            focusedTab = pane.FocusedTab
            isFocusedPane = paneStateCtx.focusedPane = paneId
        }

        PaneCtx.Provider(
            box paneCtxValue,
            Html.div [
                prop.key (defaultArg key paneIdKey)
                prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:flex-1 swt:overflow-hidden swt:border-l swt:border-t swt:border-base-content/20"
                prop.custom ("data-workspace-pane", paneIdKey)
                if paneStateCtx.debug then
                    prop.testId $"workspace-pane-{paneIdKey}"
                prop.children [ TabBar.TabBar(paneId); ContentArea.ContentArea(paneId) ]
            ]
        )

    [<ReactComponent>]
    static member SplitNodeResizer(splitId, splitContainerRef: IRefValue<option<HTMLElement>>, dir: SplitDirection) =

        let dispatchCtx = useWorkspaceDispatchCtx ()
        let draggingRef = React.useRef false
        let draggingUi, setIsDraggingUi = React.useState false
        let pointerPosition, setPointerPosition = React.useState(None: Rect option)
        let throttledPointerPosition = React.useThrottle (pointerPosition, 20)

        // Using effect to watch over throttledPointerPosition. This is the only dependency outputting a float, so no chance to enter loops or unexpected behavior.
        React.useEffect (
            (fun () ->
                match splitContainerRef.current, pointerPosition with
                | None, _
                | Some null, _
                | _, None -> ()
                | Some splitContainer, Some pointerPos ->
                    console.log $"Pointer position: {pointerPos.X}, {pointerPos.Y}"
                    let rect = splitContainer.getBoundingClientRect ()

                    let directionalPointerPosition =
                        match dir with
                        | SplitDirection.Horizontal -> (float pointerPos.X - rect.left) / rect.width
                        | SplitDirection.Vertical -> (float pointerPos.Y - rect.top) / rect.height

                    let clampedRatio =
                        match throttledPointerPosition with
                        | Some pos -> Some(max 0.15 (min 0.85 directionalPointerPosition))
                        | None -> None

                    match clampedRatio with
                    | Some clamped -> dispatchCtx.dispatch (SetSplitRatio(splitId, clamped))
                    | None -> ()
            ),
            [| box throttledPointerPosition |]
        )

        // Register the drag handler event
        // This is not throttled, therefore minimal logic inside the event listener
        React.useEffectOnce (
            (fun () ->
                let onMove (e: PointerEvent) =
                    if draggingRef.current then
                        setPointerPosition (Some { X = int e.clientX; Y = int e.clientY })

                let stop (_: PointerEvent) = 
                    draggingRef.current <- false
                    setIsDraggingUi false

                Browser.Dom.document.addEventListener ("pointermove", unbox onMove)
                Browser.Dom.document.addEventListener ("pointerup", unbox stop)

                FsReact.createDisposable (fun () ->
                    Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                    Browser.Dom.document.removeEventListener ("pointerup", unbox stop)
                )
            )
        )

        Html.div [
            prop.onPointerDown (fun _ -> 
                draggingRef.current <- true
                setIsDraggingUi true
            )
            prop.data("active", 
                if draggingUi then "true" else "false")
            prop.className [
                "swt:shrink-0 swt:select-none swt:transition-colors swt:bg-transparent swt:hover:bg-primary swt:z-10 swt:absolute swt:data-[active=true]:bg-primary"
                match dir with
                | SplitDirection.Horizontal -> "swt:w-1.5 swt:cursor-col-resize swt:h-full swt:top-0 swt:bottom-0 swt:-left-1"
                | SplitDirection.Vertical -> "swt:h-1.5 swt:cursor-row-resize swt:w-full"
            ]
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member SplitNode(splitId: SplitId, dir: SplitDirection, ratio: float, l1: Layout, l2: Layout, ?key: string) =
        let paneStateCtx = useWorkspacePaneStateCtx ()

        let splitContainerRef = React.useElementRef ()

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
                    prop.className "swt:flex swt:min-w-0 swt:min-h-0 swt:relative"
                    prop.style [
                        match dir with
                        | SplitDirection.Horizontal -> style.width (length.perc size2)
                        | SplitDirection.Vertical -> style.height (length.perc size2)
                    ]
                    prop.children [
                        PaneNode.SplitNodeResizer(splitId, splitContainerRef, dir)
                        Html.div [
                            prop.className "swt:overflow-hidden swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:flex-1 swt:grow"
                            prop.children [
                                PaneNode.PaneNode l2 
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member PaneNode(layout: Layout, ?key: string) =

        match layout with
        | Layout.Single leaf -> PaneNode.LeafNode(leaf, ?key = key)

        | Layout.Split(splitId, dir, ratio, l1, l2) ->
            PaneNode.SplitNode(splitId, dir, ratio, l1, l2, ?key = key)
