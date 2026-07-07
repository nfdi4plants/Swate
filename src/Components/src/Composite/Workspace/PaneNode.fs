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
    static member LeafNode(paneId: string, ?key: string) =
        let workspaceCtx = useWorkspaceCtx ()
        let paneState =
            workspaceCtx.panes
            |> Map.tryFind paneId
            |> Option.defaultValue {
                tabs = [||]
                tabOrder = [||]
                activeTabId = None
            }

        let paneCtxValue: PaneContext = {
            paneId = paneId
            tabs = paneState.tabs
            tabOrder = paneState.tabOrder
            activateTab = workspaceCtx.setActiveTabId << Some
            closeTab = workspaceCtx.closeTab
        }

        PaneCtx.Provider(
            paneCtxValue,
            Html.div [
                prop.key (defaultArg key paneId)
                prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:flex-1 swt:overflow-hidden"
                if workspaceCtx.debug then
                    prop.testId $"workspace-pane-{paneId}"
                prop.children [
                    TabBar.TabBar(paneId)
                    ContentArea.ContentArea(paneId)
                ]
            ]
        )

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member SplitNode(direction, first, second, panePath: string, ?key: string) =
        let workspaceCtx = useWorkspaceCtx ()

        let dragging = React.useRef false

        let storageKey = Keys.mkLocalStorageKey "workspace" "split" panePath
        let splitContainerRef = React.useElementRef()

        let (storedRatio, setStoredRatio) =
            React.useLocalStorage (storageKey, 0.5)


        let pointerPosition, setPointerPosition = React.useState (None: float option)
        let throttledPointerPosition = React.useThrottle(pointerPosition, 16)

        let clampedRatio =
            match throttledPointerPosition with
            | Some pos ->
                let clamped = max 0.15 (min 0.85 pos)
                Some clamped
            | None -> None

        // Combining throttle with local storage is not easy. Dragging the divider will only change the pointerPosition (throttled!). On update we recalculate the clampedRatio and if changed we update the local storage. This way we avoid updating the local storage on every pointer move event, which would be too frequent and cause performance issues.
        React.useEffect (
            (fun () ->
                match clampedRatio with
                | Some clamped ->
                    if clamped <> storedRatio then
                        setStoredRatio clamped
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
                        // depending on direction we need a different pointer position (x or y)
                        let directionalPointerPosition =
                            match direction with
                            | SplitDirection.Horizontal ->
                                let directionalPointerPosition = (e.clientX - rect.left) / rect.width
                                directionalPointerPosition
                            | SplitDirection.Vertical ->
                                let directionalPointerPosition = (e.clientY - rect.top) / rect.height
                                directionalPointerPosition
                        setPointerPosition (Some directionalPointerPosition)

            let stop (_: PointerEvent) =
                dragging.current <- false

            Browser.Dom.document.addEventListener ("pointermove", unbox onMove)
            Browser.Dom.document.addEventListener ("pointerup", unbox stop)

            FsReact.createDisposable (fun () ->
                Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                Browser.Dom.document.removeEventListener ("pointerup", unbox stop)
            )
        )

        let flexDir =
            match direction with
            | SplitDirection.Horizontal -> "swt:flex-row"
            | SplitDirection.Vertical -> "swt:flex-col"

        let size1 = storedRatio * 100.0
        let size2 = 100.0 - size1

        Html.div [
            prop.key (defaultArg key panePath)
            prop.ref splitContainerRef
            prop.className $"swt:flex {flexDir} swt:min-w-0 swt:min-h-0 swt:flex-1 swt:overflow-hidden"
            if workspaceCtx.debug then
                prop.testId $"workspace-split-{panePath}"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:overflow-hidden"
                    prop.style [ 
                        match direction with
                        | SplitDirection.Horizontal -> style.width (length.perc size1)
                        | SplitDirection.Vertical -> style.height (length.perc size1)
                    ]
                    prop.children [ PaneNode.PaneNode(first, panePath + "/first") ]
                ]
                // SplitDivider.SplitDivider(direction, ratio, onRatioChange, panePath)
                Html.div [
                    match key with
                    | Some k -> prop.key k
                    | None -> ()
                    prop.onPointerDown (fun _ -> 
                        dragging.current <- true
                    )
                    prop.className [
                        "swt:shrink-0 swt:select-none swt:transition-colors swt:bg-base-content swt:hover:bg-primary swt:z-10"
                        match direction with
                        | SplitDirection.Horizontal -> "swt:w-1 swt:cursor-col-resize swt:h-full"
                        | SplitDirection.Vertical -> "swt:h-1 swt:cursor-row-resize swt:w-full"
                    ]
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:overflow-hidden"
                    prop.style [ 
                        match direction with
                        | SplitDirection.Horizontal -> style.width (length.perc size2)
                        | SplitDirection.Vertical -> style.height (length.perc size2)
                    ]
                    prop.children [ PaneNode.PaneNode(second, panePath + "/second") ]
                ]
            ]
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member PaneNode(pane: Pane, panePath: string, ?key: string) =

        match pane with
        | Pane.Leaf paneId ->
            PaneNode.LeafNode(paneId, ?key = key)

        | Pane.Split(direction, first, second) ->
            let nextPath = panePath + "/" + unbox<string> direction
            PaneNode.SplitNode(direction, first, second, nextPath, ?key = key)

