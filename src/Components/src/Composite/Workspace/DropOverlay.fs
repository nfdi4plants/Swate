namespace Swate.Components.Composite.Workspace

open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components
open Swate.Components.JsBindings
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context
open Swate.Components.Composite.Workspace.Helper.DndId
open Swate.Components.Composite.Workspace.Helper.DropTarget

[<Erase; Mangle(false)>]
type DropOverlay =

    [<ReactComponent>]
    static member DropOverlay(workspaceRef: IRefValue<HTMLElement option>) =
        let dispatchCtx = useWorkspaceDispatchCtx ()
        let paneStateCtx = useWorkspacePaneStateCtx ()

        let isDragging, setIsDragging = React.useState false
        let isDraggingRef = React.useRef false
        let dragInfo, setDragInfo = React.useState (None: (string * string) option)
        let dropTarget, setDropTarget = React.useState (None: DropTarget option)

        let targetRect, setTargetRect =
            React.useState (
                None: {| left: float
                         top: float
                         width: float
                         height: float |} option
            )

        let dropTargetRef =
            React.useRef (None: DropTarget option)

        let targetRectRef =
            React.useRef (
                None: {| left: float
                         top: float
                         width: float
                         height: float |} option
            )

        let resolveTarget (x: float) (y: float) =
            match workspaceRef.current, dragInfo with
            | Some workspaceEl, Some(sourcePaneId, _) ->
                let el : HTMLElement option =
                    let fromPoint = Browser.Dom.document.elementFromPoint (x, y)

                    let foundFrom el : HTMLElement option =
                        let mutable found : HTMLElement option = None
                        let mutable current : HTMLElement option = Some el

                        while current.IsSome do
                            let c = current.Value

                            if obj.ReferenceEquals(c, workspaceEl) then
                                current <- None
                            elif not (isNull (c.getAttribute "data-workspace-pane")) then
                                found <- Some c
                                current <- None
                            elif not (isNull (c.getAttribute "data-workspace-tabbar")) then
                                found <- Some c
                                current <- None
                            else
                                current <- Option.ofObj c.parentElement

                        found

                    match fromPoint with
                    | :? HTMLElement as e when not (isNull e) ->
                        match foundFrom e with
                        | Some _ as result -> result
                        | None ->
                            let elements : Element[] = (!!Browser.Dom.document)?elementsFromPoint (x, y)

                            elements
                            |> Array.tryPick (fun el ->
                                match el with
                                | :? HTMLElement as h when workspaceEl.contains (h) -> foundFrom h
                                | _ -> None
                            )
                    | _ -> None

                match el with
                | Some targetEl ->
                    let target = resolveDropTarget targetEl x y workspaceEl sourcePaneId
                    setDropTarget target
                    dropTargetRef.current <- target

                    match target with
                    | Some(TabBarDrop paneId) ->
                        let found = workspaceEl.querySelector ($"[data-workspace-tabbar=\"{paneId}\"]")

                        match found with
                        | :? HTMLElement as tabBarEl ->
                            let r = tabBarEl.getBoundingClientRect()
                            let wr = workspaceEl.getBoundingClientRect()

                            let rect =
                                Some {|
                                    left = r.left - wr.left
                                    top = r.top - wr.top
                                    width = r.width
                                    height = r.height
                                |}

                            setTargetRect rect
                            targetRectRef.current <- rect
                        | _ ->
                            setTargetRect None
                            targetRectRef.current <- None

                    | Some(EdgeDrop(paneId, dir)) ->
                        let found = workspaceEl.querySelector ($"[data-workspace-pane=\"{paneId}\"]")

                        match found with
                        | :? HTMLElement as paneEl ->
                            let r = paneEl.getBoundingClientRect()
                            let wr = workspaceEl.getBoundingClientRect()

                            let tabBarEl =
                                workspaceEl.querySelector ($"[data-workspace-tabbar=\"{paneId}\"]")

                            let tabBarHeight =
                                match tabBarEl with
                                | :? HTMLElement as tb -> tb.getBoundingClientRect().height
                                | _ -> 0.0

                            let contentTop = r.top + tabBarHeight
                            let contentHeight = max 0.0 (r.bottom - contentTop)
                            let relativeLeft = r.left - wr.left
                            let relativeContentTop = contentTop - wr.top

                            let edgeRect =
                                match dir with
                                | EdgeDirection.Top -> {|
                                    left = relativeLeft
                                    top = relativeContentTop
                                    width = r.width
                                    height = contentHeight * 0.25
                                  |}
                                | EdgeDirection.Bottom -> {|
                                    left = relativeLeft
                                    top = relativeContentTop + contentHeight * 0.75
                                    width = r.width
                                    height = contentHeight * 0.25
                                  |}
                                | EdgeDirection.Left -> {|
                                    left = relativeLeft
                                    top = relativeContentTop
                                    width = r.width * 0.25
                                    height = contentHeight
                                  |}
                                | EdgeDirection.Right -> {|
                                    left = relativeLeft + r.width * 0.75
                                    top = relativeContentTop
                                    width = r.width * 0.25
                                    height = contentHeight
                                  |}

                            setTargetRect (Some edgeRect)
                            targetRectRef.current <- Some edgeRect
                        | _ ->
                            setTargetRect None
                            targetRectRef.current <- None
                    | None ->
                        setTargetRect None
                        targetRectRef.current <- None
                | None ->
                    setDropTarget None
                    setTargetRect None
                    dropTargetRef.current <- None
                    targetRectRef.current <- None
            | _ -> ()

        DndKit.useDndMonitor (
            {|
                onDragStart =
                    fun (event: DndKit.IDndKitEvent) ->
                        if not (isNull event.active) then
                            let activeId = string event.active.id

                            match DndId.read activeId with
                            | Some(Tab(sourcePaneId, tabId)) ->
                                setDragInfo (Some(sourcePaneId, tabId))
                                setIsDragging true
                                isDraggingRef.current <- true
                            | _ -> ()

                onDragEnd =
                    fun (_: DndKit.IDndKitEvent) ->
                        match dragInfo, dropTargetRef.current with
                        | Some(_, tabId), Some(TabBarDrop targetPaneId) ->
                            dispatchCtx.dispatch (
                                box (
                                    MoveTab(TabId tabId, PaneId(System.Guid.Parse targetPaneId))
                                )
                            )
                        | Some(_, tabId), Some(EdgeDrop(targetPaneId, dir)) ->
                            dispatchCtx.dispatch (
                                box (
                                    SplitPaneByTabMove(
                                        TabId tabId,
                                        PaneId(System.Guid.Parse targetPaneId),
                                        dir
                                    )
                                )
                            )
                        | _ -> ()

                        isDraggingRef.current <- false
                        setIsDragging false
                        setDragInfo None
                        setDropTarget None
                        setTargetRect None
                        dropTargetRef.current <- None
                        targetRectRef.current <- None

                onDragCancel =
                    fun (_: DndKit.IDndKitEvent) ->
                        isDraggingRef.current <- false
                        setIsDragging false
                        setDragInfo None
                        setDropTarget None
                        setTargetRect None
                        dropTargetRef.current <- None
                        targetRectRef.current <- None
            |}
        )

        React.useEffect (
            (fun () ->
                if not isDragging then
                    FsReact.createDisposable (fun () -> ())
                else
                    let mutable handlerRef : (PointerEvent -> unit) option = None

                    let handler (e: PointerEvent) =
                        if isDraggingRef.current then
                            resolveTarget e.clientX e.clientY

                    handlerRef <- Some handler
                    Browser.Dom.document.addEventListener ("pointermove", unbox handler)

                    FsReact.createDisposable (fun () ->
                        match handlerRef with
                        | Some h -> Browser.Dom.document.removeEventListener ("pointermove", unbox h)
                        | None -> ()
                    )
            ),
            [| box isDragging; box dragInfo |]
        )

        match dropTarget, targetRect with
        | Some target, Some rect ->
            let transitionStyle =
                "top 100ms ease-out, left 100ms ease-out, width 100ms ease-out, height 100ms ease-out"

            match target with
            | TabBarDrop _ ->
                Html.div [
                    prop.className
                        "swt:absolute swt:bg-primary/10 swt:pointer-events-none swt:z-30 swt:rounded"
                    prop.style [
                        style.left (length.px rect.left)
                        style.top (length.px rect.top)
                        style.width (length.px rect.width)
                        style.height (length.px rect.height)
                        style.custom ("transition", transitionStyle)
                    ]
                ]
            | EdgeDrop(_, dir) ->
                Html.div [
                    prop.className "swt:absolute swt:bg-primary/20 swt:pointer-events-none swt:z-30 swt:rounded"
                    prop.style [
                        style.left (length.px rect.left)
                        style.top (length.px rect.top)
                        style.width (length.px rect.width)
                        style.height (length.px rect.height)
                        match dir with
                        | EdgeDirection.Top -> style.borderBottomWidth (length.px 4)
                        | EdgeDirection.Bottom -> style.borderTopWidth (length.px 4)
                        | EdgeDirection.Left -> style.borderRightWidth (length.px 4)
                        | EdgeDirection.Right -> style.borderLeftWidth (length.px 4)
                        style.custom ("borderColor", "oklch(var(--p))")
                        style.custom ("transition", transitionStyle)
                    ]
                ]
        | _ -> Html.none
