namespace Swate.Components.Page.ProvenanceGrouping

open System
open System.Globalization
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.FolderedDraggableList
open Swate.Components.Composite.FolderedDraggableList.Types
open Swate.Components.JsBindings
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Page.ProvenanceGrouping.Types

[<Erase; Mangle(false)>]
type ProvenanceGrouping =

    [<ReactComponent>]
    static member Main
        (session: ProvenanceSession, onChange: ProvenanceEditorChange -> unit, ?height: int, ?debug: bool)
        =
        let debug = defaultArg debug false
        let rawUiState, setUiState = React.useState (State.init session)
        let activeDrag, setActiveDrag = React.useState<ActiveDrag option> None
        let surfaceRef = React.useElementRef ()
        let splitDrag = React.useRef (None: Splitter.SplitterSide option)
        let rootRef = React.useElementRef ()
        let tier, setTier = React.useState LayoutTier.Wide
        let openRail, setOpenRail = React.useState<ProvenanceSide option> None
        let density, setDensity = React.useState Density.EditorDensity.Comfortable
        let isPropertyShelfExpanded, setIsPropertyShelfExpanded = React.useState true

        let propertyShelfFolderExpansion, setPropertyShelfFolderExpansion =
            React.useState<(ProvenanceLayerId * Set<string>) option> None

        let showPropertyHeaderConnectors, setShowPropertyHeaderConnectors =
            React.useState true

        let liveDragStore = React.useRef (LiveDrag.create ())
        let hoverStore = React.useRef (HoverHighlight.create ())
        let isValueChipDragging, setIsValueChipDragging = React.useState false

        // Click-to-connect: a tapped handle stays armed until a target handle is
        // tapped, Escape is pressed, or the pointer goes down elsewhere.
        let armedHandle, setArmedHandle = React.useState<ConnectionHandleRef option> None
        let latestArmedHandle = React.useRef armedHandle
        latestArmedHandle.current <- armedHandle
        let latestDragContext = React.useRef (None: DragContext option)

        React.useEffectOnce (fun () ->
            let onPointerDown =
                fun (event: Browser.Types.Event) ->
                    if latestArmedHandle.current.IsSome then
                        let onHandle =
                            not (isNull event.target)
                            && not (isNull (Motion.closest event.target "[data-provenance-connection-drop-id]"))

                        if not onHandle then
                            setArmedHandle None

            let onKeyDown =
                fun (event: Browser.Types.KeyboardEvent) ->
                    if event.key = "Escape" && latestArmedHandle.current.IsSome then
                        setArmedHandle None

            Browser.Dom.document.addEventListener ("pointerdown", unbox onPointerDown)
            Browser.Dom.document.addEventListener ("keydown", unbox onKeyDown)

            FsReact.createDisposable (fun () ->
                Browser.Dom.document.removeEventListener ("pointerdown", unbox onPointerDown)
                Browser.Dom.document.removeEventListener ("keydown", unbox onKeyDown)
            )
        )

        let handleTap (handle: ConnectionHandleRef) =
            match latestArmedHandle.current with
            | Some armed when armed = handle -> setArmedHandle None
            | Some armed ->
                match ConnectionRouting.action armed handle with
                | Some _ ->
                    latestDragContext.current
                    |> Option.iter (fun context -> DragHandlers.routeConnectionHandle context armed handle)

                    setArmedHandle None
                // A tap on an incompatible handle re-arms from there instead.
                | None -> setArmedHandle (Some handle)
            | None -> setArmedHandle (Some handle)

        React.useEffectOnce (fun () ->
            let applyTier () =
                match rootRef.current with
                | Some root -> setTier (LayoutTier.forWidth root.clientWidth)
                | None -> ()

            applyTier ()
            let observer = TierObserver.create applyTier

            match rootRef.current with
            | Some root -> TierObserver.observe observer root
            | None -> ()

            FsReact.createDisposable (fun () -> TierObserver.disconnect observer)
        )

        let uiState =
            React.useMemo ((fun () -> State.Sides.ensure session rawUiState), [| box session; box rawUiState |])

        let layer, inputGroups, outputGroups, connections =
            React.useMemo ((fun () -> Display.displayLayer session uiState), [| box session; box uiState.SideStates |])

        // Event handlers below read these refs instead of closing over render values,
        // so their identities stay stable and memoized subtrees never act on stale
        // sessions or UI state.
        let latestUiState = React.useRef uiState
        let latestSession = React.useRef session
        let latestLayer = React.useRef layer
        let latestOnChange = React.useRef onChange
        let latestConnections = React.useRef connections
        latestUiState.current <- uiState
        latestSession.current <- session
        latestLayer.current <- layer
        latestOnChange.current <- onChange
        latestConnections.current <- connections

        // Marks the cards connected to the hovered card with a data attribute, styled
        // by the cards' CSS; imperative on purpose so hovering never re-renders the
        // editor tree.
        React.useEffectOnce (fun () ->
            let markRelated () =
                match surfaceRef.current with
                | None -> ()
                | Some surface ->
                    Motion.queryAll surface "[data-provenance-related]"
                    |> Array.iter (fun node -> node.removeAttribute "data-provenance-related")

                    match hoverStore.current.Current with
                    | None -> ()
                    | Some target ->
                        let related =
                            latestConnections.current
                            |> List.choose (fun connection ->
                                match target.Side with
                                | ProvenanceSide.Input when connection.SourceGroupId = target.GroupId ->
                                    Some(ProvenanceSide.Output, connection.TargetGroupId)
                                | ProvenanceSide.Output when connection.TargetGroupId = target.GroupId ->
                                    Some(ProvenanceSide.Input, connection.SourceGroupId)
                                | _ -> None
                            )
                            |> List.distinct

                        for relatedSide, groupId in related do
                            let node: Browser.Types.HTMLElement =
                                !!
                                    surface.querySelector
                                    ($"[data-provenance-group-node='{DragDrop.groupNodeId relatedSide groupId}']")

                            if not (isNull node) then
                                node.setAttribute ("data-provenance-related", "true")

            let unsubscribe = hoverStore.current |> HoverHighlight.subscribe markRelated
            FsReact.createDisposable unsubscribe
        )

        let commitUiState =
            React.useCallback (
                (fun next ->
                    latestUiState.current <- next
                    setUiState next
                ),
                [||]
            )

        let applyUiState: (UiState -> UiState) -> unit =
            React.useCallback ((fun update -> commitUiState (update latestUiState.current)), [||])

        let panelRatios = State.PanelLayout.get layer.Id uiState

        let endpointKinds =
            React.useMemo ((fun () -> Endpoints.endpointKindOptions ()), [||])

        let existingEndpointNames =
            React.useMemo (
                (fun () -> [
                    yield!
                        inputGroups
                        |> List.collect (fun group -> group.Members |> List.map (fun member' -> member'.Name))
                    yield!
                        outputGroups
                        |> List.collect (fun group -> group.Members |> List.map (fun member' -> member'.Name))
                ]),
                [| box inputGroups; box outputGroups |]
            )

        let lookups =
            React.useMemo (
                (fun () -> EditorLookups.create layer uiState inputGroups outputGroups),
                [|
                    box layer
                    box uiState.PaletteValues
                    box inputGroups
                    box outputGroups
                |]
            )

        let inputRailProjection =
            React.useMemo (
                (fun () ->
                    PropertyProjection.railProjectionWithFilters
                        session
                        layer.Id
                        ProvenanceSide.Input
                        layer.Model
                        uiState
                ),
                [|
                    box session
                    box layer.Id
                    box layer.Model
                    box uiState.PropertyRailPlacements
                    box uiState.PropertyRailOrders
                    box uiState.ExpandedProperties
                    box uiState.PaletteValues
                    box uiState.PropertyColors
                    box uiState.Filters
                |]
            )

        let outputRailProjection =
            React.useMemo (
                (fun () ->
                    PropertyProjection.railProjectionWithFilters
                        session
                        layer.Id
                        ProvenanceSide.Output
                        layer.Model
                        uiState
                ),
                [|
                    box session
                    box layer.Id
                    box layer.Model
                    box uiState.PropertyRailPlacements
                    box uiState.PropertyRailOrders
                    box uiState.ExpandedProperties
                    box uiState.PaletteValues
                    box uiState.PropertyColors
                    box uiState.Filters
                |]
            )

        let inputSideState = State.Sides.get layer.InputSideId uiState
        let outputSideState = State.Sides.get layer.OutputSideId uiState

        let activeInputRailProjection =
            React.useMemo (
                (fun () ->
                    EditorSurface.dropZoneProjection
                        layer.Id
                        ProvenanceSide.Input
                        uiState
                        inputSideState.GroupingAssignments
                        inputRailProjection
                ),
                [|
                    box layer.Id
                    box uiState.PropertyRailPlacements
                    box inputSideState
                    box inputRailProjection
                |]
            )

        let activeOutputRailProjection =
            React.useMemo (
                (fun () ->
                    EditorSurface.dropZoneProjection
                        layer.Id
                        ProvenanceSide.Output
                        uiState
                        outputSideState.GroupingAssignments
                        outputRailProjection
                ),
                [|
                    box layer.Id
                    box uiState.PropertyRailPlacements
                    box outputSideState
                    box outputRailProjection
                |]
            )

        let propertyShelfFolders =
            React.useMemo (
                (fun () -> PropertyShelf.folders session layer uiState inputRailProjection outputRailProjection),
                [|
                    box session
                    box layer
                    box uiState.PropertyRailPlacements
                    box uiState.PropertyColors
                    box uiState.SideStates
                    box inputRailProjection
                    box outputRailProjection
                |]
            )

        let defaultPropertyShelfFolderIds =
            React.useMemo (
                (fun () ->
                    propertyShelfFolders
                    |> List.tryHead
                    |> Option.map (fun folder -> Set.singleton folder.Id)
                    |> Option.defaultValue Set.empty
                ),
                [| box propertyShelfFolders |]
            )

        let propertyShelfExpandedFolderIds =
            match propertyShelfFolderExpansion with
            | Some(expandedLayerId, folderIds) when expandedLayerId = layer.Id -> folderIds
            | _ -> defaultPropertyShelfFolderIds

        let setPropertyShelfExpandedFolderIds folderIds =
            setPropertyShelfFolderExpansion (Some(latestLayer.current.Id, folderIds))

        let setPropertyShelfFolderColor folderId color =
            applyUiState (PropertyShelf.setFolderColor latestSession.current folderId color)

        let propertyShelf =
            React.useMemo (
                (fun () ->
                    Html.section [
                        prop.className
                            "swt:flex swt:min-w-0 swt:flex-col swt:gap-3 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100/80 swt:p-3 swt:shadow-sm"
                        if debug then
                            prop.testId "provenance-property-shelf"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:min-w-0 swt:items-center swt:justify-between swt:gap-2"
                                prop.children [
                                    Html.div [
                                        prop.className
                                            "swt:flex swt:min-w-0 swt:items-center swt:gap-2 swt:text-sm swt:font-medium"
                                        prop.children [
                                            Html.i [
                                                prop.className [
                                                    "swt:iconify swt:size-5 swt:shrink-0"
                                                    if isPropertyShelfExpanded then
                                                        "swt:fluent--folder-open-24-regular"
                                                    else
                                                        "swt:fluent--folder-24-regular"
                                                ]
                                            ]
                                            Html.span [
                                                prop.className "swt:min-w-0 swt:truncate"
                                                prop.text "Available properties"
                                            ]
                                        ]
                                    ]
                                    Html.button [
                                        prop.title (
                                            if isPropertyShelfExpanded then
                                                "Minimize property folders"
                                            else
                                                "Expand property folders"
                                        )
                                        prop.type'.button
                                        prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:size-8 swt:p-0"
                                        prop.custom ("aria-expanded", isPropertyShelfExpanded)
                                        prop.ariaLabel (
                                            if isPropertyShelfExpanded then
                                                "Minimize property folders"
                                            else
                                                "Expand property folders"
                                        )
                                        if debug then
                                            prop.testId "provenance-property-shelf-toggle"
                                        prop.onClick (fun _ ->
                                            setIsPropertyShelfExpanded (not isPropertyShelfExpanded)
                                        )
                                        prop.children [
                                            Html.i [
                                                prop.className [
                                                    "swt:iconify swt:size-4"
                                                    if isPropertyShelfExpanded then
                                                        "swt:fluent--chevron-up-20-regular"
                                                    else
                                                        "swt:fluent--chevron-down-20-regular"
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                            if isPropertyShelfExpanded then
                                FolderedDraggableList.FolderedDraggableList<PropertyShelfItemPayload>(
                                    propertyShelfFolders,
                                    (fun _ item ->
                                        DragDrop.folderPropertyDragId item.Payload.SourceSide item.Payload.Header
                                    ),
                                    expandedFolderIds = propertyShelfExpandedFolderIds,
                                    onExpandedFolderIdsChange = setPropertyShelfExpandedFolderIds,
                                    onSetFolderColor = setPropertyShelfFolderColor,
                                    className = "swt:min-w-0 swt:motion-pop-in",
                                    debug = debug
                                )
                        ]
                    ]
                ),
                [|
                    box propertyShelfFolders
                    box isPropertyShelfExpanded
                    box propertyShelfExpandedFolderIds
                |]
            )

        React.useEffect (
            (fun () ->
                let next =
                    latestUiState.current
                    |> State.RailOrder.ensure layer.Id ProvenanceSide.Input inputRailProjection.Headers
                    |> State.RailOrder.ensure layer.Id ProvenanceSide.Output outputRailProjection.Headers

                if next <> latestUiState.current then
                    commitUiState next
            ),
            [|
                box layer.Id
                box inputRailProjection.Headers
                box outputRailProjection.Headers
            |]
        )

        // Splitter drags write the grid template straight to the DOM per animation
        // frame and commit the ratios to state once on release; committing per
        // pointermove re-rendered the entire editor for every mouse step.
        let liveSplitRatios = React.useRef (None: PanelRatios option)
        let splitFrame = React.useRef (None: float option)
        let activeSplit, setActiveSplit = React.useState<Splitter.SplitterSide option> None

        let applyLiveSplitTemplate =
            React.useCallback (
                (fun () ->
                    splitFrame.current <- None

                    match surfaceRef.current, liveSplitRatios.current with
                    | Some surface, Some ratios -> Splitter.applyTemplate surface (Splitter.template ratios)
                    | _ -> ()
                ),
                [||]
            )

        let updateLiveSplit =
            React.useCallback (
                (fun (side, clientX) ->
                    match surfaceRef.current with
                    | None -> ()
                    | Some surface ->
                        let rect = surface.getBoundingClientRect ()

                        let rawPercent =
                            if rect.width <= 0. then
                                0.
                            else
                                ((clientX - rect.left) / rect.width) * 100.

                        let splitPercent = rawPercent |> max 0. |> min 100. |> round |> int

                        let current =
                            liveSplitRatios.current
                            |> Option.defaultWith (fun () ->
                                State.PanelLayout.get latestLayer.current.Id latestUiState.current
                            )

                        let next =
                            match side with
                            | Splitter.Left -> State.PanelLayout.clamped splitPercent current.Right
                            | Splitter.Right -> State.PanelLayout.clamped current.Left (100 - splitPercent)

                        liveSplitRatios.current <- Some next

                        match splitFrame.current with
                        | Some _ -> ()
                        | None -> splitFrame.current <- Some(AnimationFrame.request applyLiveSplitTemplate)
                ),
                [||]
            )

        let finishSplitDrag =
            React.useCallback (
                (fun () ->
                    match splitDrag.current with
                    | None -> ()
                    | Some _ ->
                        splitDrag.current <- None

                        match splitFrame.current with
                        | Some handle ->
                            AnimationFrame.cancel handle
                            splitFrame.current <- None
                        | None -> ()

                        match liveSplitRatios.current with
                        | Some ratios -> applyUiState (State.PanelLayout.set latestLayer.current.Id ratios)
                        | None -> ()

                        liveSplitRatios.current <- None
                        setActiveSplit None
                ),
                [||]
            )

        React.useEffectOnce (fun () ->
            let onMove =
                fun (event: Browser.Types.PointerEvent) ->
                    match splitDrag.current with
                    | Some side -> updateLiveSplit (side, event.clientX)
                    | None -> ()

            let stopDragging = fun (_: Browser.Types.PointerEvent) -> finishSplitDrag ()

            Browser.Dom.document.addEventListener ("pointermove", unbox onMove)
            Browser.Dom.document.addEventListener ("pointerup", unbox stopDragging)
            Browser.Dom.document.addEventListener ("pointercancel", unbox stopDragging)

            FsReact.createDisposable (fun () ->
                Browser.Dom.document.removeEventListener ("pointermove", unbox onMove)
                Browser.Dom.document.removeEventListener ("pointerup", unbox stopDragging)
                Browser.Dom.document.removeEventListener ("pointercancel", unbox stopDragging)
            )
        )

        let startPanelDrag side (event: Browser.Types.PointerEvent) =
            event.preventDefault ()
            splitDrag.current <- Some side
            liveSplitRatios.current <- Some(State.PanelLayout.get latestLayer.current.Id latestUiState.current)
            setActiveSplit (Some side)
            updateLiveSplit (side, event.clientX)

        let nudgeSplit side delta =
            applyUiState (fun state ->
                let layerId = latestLayer.current.Id
                let current = State.PanelLayout.get layerId state

                match side with
                | Splitter.Left -> State.PanelLayout.setLeft layerId (current.Left + delta) state
                | Splitter.Right -> State.PanelLayout.setRight layerId (current.Right - delta) state
            )

        let resetSplit () =
            applyUiState (State.PanelLayout.set latestLayer.current.Id State.PanelLayout.defaultRatios)

        let pointerSensor =
            DndKit.useSensor (
                DndKit.PointerSensor,
                {|
                    activationConstraint = {| distance = 6 |}
                |}
            )

        let sensors = DndKit.useSensors [| pointerSensor |]

        // publish and the actions below read session/layer/UI state through the
        // latest refs so that memoized subtrees never fire an action against a
        // session that has since been replaced.
        let publish =
            React.useCallback (
                (fun (result: SessionResult) ->
                    match result with
                    | Ok(next, patches) ->
                        LiveDrag.clear liveDragStore.current

                        let nextUiState = latestUiState.current |> State.Sides.ensure next

                        commitUiState {
                            nextUiState with
                                Error = None
                                PendingAssignmentBatch = None
                                PendingMemberResolution = None
                        }

                        latestOnChange.current { Session = next; Patches = patches }
                    | Error error ->
                        LiveDrag.clear liveDragStore.current

                        commitUiState {
                            latestUiState.current with
                                Error = Some(string error)
                        }
                ),
                [||]
            )

        let createSet =
            React.useCallback ((fun command -> EditorActions.createSet latestSession.current publish command), [||])

        let addPaletteValue side header value unit =
            let layer = latestLayer.current
            applyUiState (State.Palette.addValue layer.Id layer.Model.Source side header value unit)

        let toggleSideGrouping sideId side header =
            applyUiState (State.GroupingAssignments.toggleSide sideId side header)

        let togglePropertyExpanded side header =
            applyUiState (State.PropertyExpansion.toggle latestLayer.current.Id side header)

        let toggleSelection side groupId =
            applyUiState (State.Selection.toggle latestLayer.current.Id side groupId)

        let toggleGroupDetail side groupId =
            applyUiState (State.Detail.toggleGroup side groupId)

        let sourceInfoForValue value =
            Session.propertyValueSourceInfo layer value

        let setPropertyColor header color =
            let colorContext =
                State.PropertyColors.visibleColorContextForLayer latestSession.current latestLayer.current

            let update =
                match color with
                | Some selectedColor -> State.PropertyColors.setColor colorContext.Id header selectedColor
                | None -> State.PropertyColors.clearColor colorContext.Id header

            applyUiState update

        let setSourceColor sourceId color =
            let update =
                match color with
                | Some selectedColor -> State.PropertyColors.setSourceColor sourceId selectedColor
                | None -> State.PropertyColors.clearSourceColor sourceId

            applyUiState update

        let setPropertySort sort =
            let sortedInputHeaders =
                PropertyProjection.sortHeaders
                    sort
                    inputRailProjection.StatsByHeader
                    inputRailProjection.ConnectionCountByHeader
                    inputRailProjection.Headers

            let sortedOutputHeaders =
                PropertyProjection.sortHeaders
                    sort
                    outputRailProjection.StatsByHeader
                    outputRailProjection.ConnectionCountByHeader
                    outputRailProjection.Headers

            applyUiState (
                State.Filters.setPropertySort sort
                >> State.RailOrder.reorderVisible layer.Id ProvenanceSide.Input sortedInputHeaders
                >> State.RailOrder.reorderVisible layer.Id ProvenanceSide.Output sortedOutputHeaders
            )

        let confirmBatch (pending: PendingAssignmentBatch) =
            EditorActions.applyAssignmentBatch latestSession.current publish pending.Batch

        let connectSetPairs pairs =
            EditorActions.connectSetPairs latestSession.current publish pairs

        let removeDisplayConnection =
            React.useCallback (
                (fun (connection: DisplayConnection) ->
                    match Session.removeConnections connection.ConnectionIds latestSession.current with
                    | Ok(next, patches) ->
                        LiveDrag.clear liveDragStore.current

                        let nextUiState = latestUiState.current |> State.Sides.ensure next

                        commitUiState {
                            nextUiState with
                                Error = None
                                PendingAssignmentBatch = None
                                PendingMemberResolution = None
                                Detail = None
                        }

                        latestOnChange.current { Session = next; Patches = patches }
                    | Error error ->
                        LiveDrag.clear liveDragStore.current

                        commitUiState {
                            latestUiState.current with
                                Error = Some(string error)
                        }
                ),
                [||]
            )

        let resolveAllToAll (pending: PendingMemberResolution) =
            match
                lookups.FindGroup ProvenanceSide.Input pending.InputGroupId,
                lookups.FindGroup ProvenanceSide.Output pending.OutputGroupId
            with
            | Some inputGroup, Some outputGroup ->
                EditorActions.allMemberPairs inputGroup outputGroup |> connectSetPairs
            | _ -> applyUiState State.MemberResolution.clearPending

        let isGroupedCard side groupId =
            lookups.FindGroup side groupId
            |> Option.exists (fun group -> group.GroupingValues |> List.isEmpty |> not)

        let isConnectedToExpanded side groupId =
            isGroupedCard side groupId
            && (connections
                |> List.exists (fun connection ->
                    match side with
                    | ProvenanceSide.Input ->
                        connection.SourceGroupId = groupId
                        && State.Detail.isGroupExpanded ProvenanceSide.Output connection.TargetGroupId uiState
                    | ProvenanceSide.Output ->
                        connection.TargetGroupId = groupId
                        && State.Detail.isGroupExpanded ProvenanceSide.Input connection.SourceGroupId uiState
                ))

        let isGroupExpanded side groupId =
            State.Detail.isGroupExpanded side groupId uiState
            || isConnectedToExpanded side groupId

        let dragContext = {
            Session = session
            Layer = layer
            UiState = uiState
            GetUiState = fun () -> latestUiState.current
            Publish = publish
            SetUiState = commitUiState
            Lookups = lookups
            ConnectSetPairs = connectSetPairs
        }

        latestDragContext.current <- Some dragContext

        let railSideLabel side =
            match side with
            | ProvenanceSide.Input -> "input"
            | ProvenanceSide.Output -> "output"

        let toggleRail side =
            setOpenRail (if openRail = Some side then None else Some side)

        let isRejectedPropertyRailDrop targetSide =
            let headerCannotSwitch sourceSide headerId =
                sourceSide <> targetSide
                && (lookups.FindHeader headerId
                    |> Option.exists (fun header -> PropertyRails.canSwitchHeader header layer.Model |> not))

            match activeDrag with
            | Some {
                       Payload = DragDrop.Payload.FolderPropertyHeader(sourceSide, headerId)
                   }
            | Some {
                       Payload = DragDrop.Payload.PropertyHeader(sourceSide, headerId)
                   } -> headerCannotSwitch sourceSide headerId
            | _ -> false

        let isPropertyDragActive =
            match activeDrag with
            | Some {
                       Payload = DragDrop.Payload.FolderPropertyHeader _
                   }
            | Some {
                       Payload = DragDrop.Payload.PropertyHeader _
                   } -> true
            | _ -> false

        let connectionDragSide =
            match activeDrag with
            | Some {
                       Payload = DragDrop.Payload.ConnectionHandle handle
                   } -> Some handle.Side
            | _ -> None

        let connectionInteraction: ConnectionDragHints.Interaction =
            React.useMemo (
                (fun () -> {
                    SourceSide =
                        connectionDragSide
                        |> Option.orElse (armedHandle |> Option.map (fun handle -> handle.Side))
                    Armed = armedHandle
                    OnHandleTap = handleTap
                }),
                [| box connectionDragSide; box armedHandle |]
            )

        let inputRailDropRejected = isRejectedPropertyRailDrop ProvenanceSide.Input
        let outputRailDropRejected = isRejectedPropertyRailDrop ProvenanceSide.Output

        let buildRailPanel side isDropRejected =
            let sideId, oppositeSideId, targetSide =
                match side with
                | ProvenanceSide.Input -> layer.InputSideId, layer.OutputSideId, ProvenanceSide.Output
                | ProvenanceSide.Output -> layer.OutputSideId, layer.InputSideId, ProvenanceSide.Input

            EditorSurface.propertyRail
                side
                layer.Model.Source.Id
                sideId
                (match side with
                 | ProvenanceSide.Input -> activeInputRailProjection
                 | ProvenanceSide.Output -> activeOutputRailProjection)
                (match side with
                 | ProvenanceSide.Input -> inputSideState.GroupingAssignments
                 | ProvenanceSide.Output -> outputSideState.GroupingAssignments)
                (fun header -> toggleSideGrouping sideId side header)
                (fun header ->
                    applyUiState (State.GroupingAssignments.toggleBoth layer.InputSideId layer.OutputSideId header)
                )
                (fun header ->
                    applyUiState (State.GroupingAssignments.move layer.Id sideId oppositeSideId targetSide header)
                )
                (fun header -> togglePropertyExpanded side header)
                (fun header value unit -> addPaletteValue side header value unit)
                setPropertyColor
                sourceInfoForValue
                isDropRejected
                (isPropertyDragActive && not isDropRejected)
                debug
                setIsValueChipDragging

        let inputRailPanel =
            React.useMemo (
                (fun () -> buildRailPanel ProvenanceSide.Input inputRailDropRejected),
                [|
                    box layer
                    box activeInputRailProjection
                    box inputSideState
                    box inputRailDropRejected
                    box isPropertyDragActive
                |]
            )

        let outputRailPanel =
            React.useMemo (
                (fun () -> buildRailPanel ProvenanceSide.Output outputRailDropRejected),
                [|
                    box layer
                    box activeOutputRailProjection
                    box outputSideState
                    box outputRailDropRejected
                    box isPropertyDragActive
                |]
            )

        let railPanel side =
            match side with
            | ProvenanceSide.Input -> inputRailPanel
            | ProvenanceSide.Output -> outputRailPanel

        let railColumn side =
            Html.div [
                prop.className "swt:@container/provenancePanel swt:min-w-0 swt:overflow-hidden"
                prop.children [ railPanel side ]
            ]

        let connectionCounts =
            React.useMemo (
                (fun () ->
                    connections
                    |> List.collect (fun connection -> [
                        (ProvenanceSide.Input, connection.SourceGroupId), connection.ConnectionIds.Length
                        (ProvenanceSide.Output, connection.TargetGroupId), connection.ConnectionIds.Length
                    ])
                    |> List.groupBy fst
                    |> List.map (fun (key, grouped) -> key, grouped |> List.sumBy snd)
                    |> Map.ofList
                ),
                [| box connections |]
            )

        let connectionCountFor side groupId =
            connectionCounts |> Map.tryFind (side, groupId) |> Option.defaultValue 0

        let sortedInputGroups =
            React.useMemo (
                (fun () -> Display.sortGroups uiState.Filters.GroupSort connections inputGroups),
                [| box uiState.Filters; box connections; box inputGroups |]
            )

        let sortedOutputGroups =
            React.useMemo (
                (fun () -> Display.sortGroups uiState.Filters.GroupSort connections outputGroups),
                [| box uiState.Filters; box connections; box outputGroups |]
            )

        // Group columns carry one card per display group; memoizing the rendered
        // columns keeps unrelated state changes (search text, rail expansion, panel
        // toggles) from reconciling hundreds of cards on large models.
        let withConnectionBadges = tier = LayoutTier.Narrow

        let buildGroupColumn side groups =
            let counts groupId =
                if withConnectionBadges then
                    Some(connectionCountFor side groupId)
                else
                    None

            EditorSurface.groupColumn
                side
                layer
                layer.Model
                groups
                endpointKinds
                existingEndpointNames
                createSet
                uiState
                isGroupExpanded
                toggleSelection
                toggleGroupDetail
                counts
                sourceInfoForValue
                debug
                isValueChipDragging

        let inputGroupColumn =
            React.useMemo (
                (fun () -> buildGroupColumn ProvenanceSide.Input sortedInputGroups),
                [|
                    box layer
                    box sortedInputGroups
                    box existingEndpointNames
                    box uiState.SelectedInputs
                    box uiState.ExpandedGroup
                    box connections
                    box lookups
                    box connectionCounts
                    box withConnectionBadges
                    box isValueChipDragging
                |]
            )

        let outputGroupColumn =
            React.useMemo (
                (fun () -> buildGroupColumn ProvenanceSide.Output sortedOutputGroups),
                [|
                    box layer
                    box sortedOutputGroups
                    box existingEndpointNames
                    box uiState.SelectedOutputs
                    box uiState.ExpandedGroup
                    box connections
                    box lookups
                    box connectionCounts
                    box withConnectionBadges
                    box isValueChipDragging
                |]
            )

        let groupColumnFor side =
            match side with
            | ProvenanceSide.Input -> inputGroupColumn
            | ProvenanceSide.Output -> outputGroupColumn

        // Medium tier: rails fold into slim vertical strips, one open at a time.
        let mediumRailColumn side =
            if openRail = Some side then
                Html.div [
                    prop.className
                        "swt:@container/provenancePanel swt:flex swt:min-w-0 swt:flex-col swt:gap-2 swt:overflow-hidden"
                    prop.children [
                        Html.button [
                            prop.title (
                                if side = ProvenanceSide.Input then
                                    "Hide input properties"
                                else
                                    "Hide output properties"
                            )
                            prop.type'.button
                            prop.className [
                                "swt:btn swt:btn-ghost swt:btn-xs swt:w-fit"
                                if side = ProvenanceSide.Output then
                                    "swt:self-end"
                            ]
                            prop.ariaLabel $"Hide {railSideLabel side} properties"
                            if debug then
                                prop.testId $"provenance-rail-toggle-{side}"
                            prop.onClick (fun _ -> toggleRail side)
                            prop.children [
                                Html.i [

                                    prop.className [
                                        "swt:iconify swt:size-4"
                                        if side = ProvenanceSide.Input then
                                            "swt:fluent--chevron-left-20-regular"
                                        else
                                            "swt:fluent--chevron-right-20-regular"
                                    ]
                                ]
                            ]
                        ]
                        railPanel side
                    ]
                ]
            else
                Html.button [
                    prop.title (
                        if side = ProvenanceSide.Input then
                            "Show input properties"
                        else
                            "Show output properties"
                    )
                    prop.type'.button
                    prop.className
                        "swt:btn swt:btn-ghost swt:btn-xs swt:h-auto swt:min-h-24 swt:w-fit swt:px-1 swt:py-2"
                    prop.ariaLabel $"Show {railSideLabel side} properties"
                    if debug then
                        prop.testId $"provenance-rail-toggle-{side}"
                    prop.onClick (fun _ -> toggleRail side)
                    prop.children [
                        Html.span [
                            prop.className "swt:[writing-mode:vertical-rl] swt:text-xs"
                            prop.text "Properties"
                        ]
                    ]
                ]

        // Narrow tier: everything stacks and the rails collapse behind toggles.
        let narrowRailSection side =
            Html.div [
                prop.className "swt:@container/provenancePanel swt:flex swt:min-w-0 swt:flex-col swt:gap-2"
                prop.children [
                    Html.button [
                        prop.type'.button
                        prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:w-fit"
                        prop.ariaLabel (
                            if openRail = Some side then
                                $"Hide {railSideLabel side} properties"
                            else
                                $"Show {railSideLabel side} properties"
                        )
                        if debug then
                            prop.testId $"provenance-rail-toggle-{side}"
                        prop.onClick (fun _ -> toggleRail side)
                        prop.children [
                            Html.i [
                                prop.className [
                                    "swt:iconify swt:size-4"
                                    if openRail = Some side then
                                        "swt:fluent--chevron-up-20-regular"
                                    else
                                        "swt:fluent--chevron-down-20-regular"
                                ]
                            ]
                            Html.span (
                                match side with
                                | ProvenanceSide.Input -> "Input properties"
                                | ProvenanceSide.Output -> "Output properties"
                            )
                        ]
                    ]
                    if openRail = Some side then
                        railPanel side
                ]
            ]

        let connectorLayoutSignature =
            [
                string tier
                string openRail
                string density
                string isPropertyShelfExpanded
                string panelRatios.Left
                string panelRatios.Middle
                string panelRatios.Right
            ]
            |> String.concat "|"

        let connectorOverlayState =
            React.useMemo (
                (fun () -> ConnectorOverlayState.fromUiState uiState),
                [|
                    box uiState.ExpandedGroup
                    box uiState.Detail
                    box uiState.ExpandedProperties
                |]
            )

        let selectConnection =
            React.useCallback (
                (fun (connection: DisplayConnection) -> applyUiState (State.Detail.showConnection connection.Id)),
                [||]
            )

        let connectorOverlay =
            React.useMemo (
                (fun () ->
                    ConnectorOverlay.Main(
                        surfaceRef,
                        layer.Id,
                        layer.Model,
                        inputGroups,
                        outputGroups,
                        connections,
                        activeInputRailProjection,
                        activeOutputRailProjection,
                        connectorOverlayState,
                        connectorLayoutSignature,
                        showPropertyHeaderConnectors,
                        liveDragStore.current,
                        selectConnection,
                        onRemove = removeDisplayConnection,
                        debug = debug
                    )
                ),
                [|
                    box layer
                    box inputGroups
                    box outputGroups
                    box connections
                    box activeInputRailProjection
                    box activeOutputRailProjection
                    box connectorOverlayState
                    box connectorLayoutSignature
                    box showPropertyHeaderConnectors
                |]
            )

        // Keying the surface by layer remounts it on layer switches, so the change
        // fades in as one deliberate transition instead of flashing in place.
        let surface =
            match tier with
            | LayoutTier.Wide ->
                Html.div [
                    prop.key layer.Id
                    prop.ref surfaceRef
                    prop.className "swt:relative swt:mx-4 swt:grid swt:min-w-0 swt:items-start swt:motion-fade-in"
                    prop.style [
                        style.custom ("gridTemplateColumns", Splitter.template panelRatios)
                    ]
                    if debug then
                        prop.testId "provenance-surface"
                    prop.children [
                        connectorOverlay
                        railColumn ProvenanceSide.Input

                        Splitter.handle
                            Splitter.Left
                            (activeSplit = Some Splitter.Left)
                            panelRatios.Left
                            (startPanelDrag Splitter.Left)
                            (nudgeSplit Splitter.Left)
                            resetSplit
                            debug
                        Html.div [
                            // The wide column gap is the gutter the group-to-group
                            // connectors are drawn in.
                            prop.className [
                                "swt:grid swt:min-w-0 swt:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] swt:items-start"
                                match density with
                                | Density.EditorDensity.Compact -> "swt:gap-12"
                                | _ -> "swt:gap-16"
                            ]
                            prop.children [
                                groupColumnFor ProvenanceSide.Input
                                groupColumnFor ProvenanceSide.Output
                            ]
                        ]
                        Splitter.handle
                            Splitter.Right
                            (activeSplit = Some Splitter.Right)
                            (100 - panelRatios.Right)
                            (startPanelDrag Splitter.Right)
                            (nudgeSplit Splitter.Right)
                            resetSplit
                            debug

                        railColumn ProvenanceSide.Output
                    ]
                ]
            | LayoutTier.Medium ->
                let railTrack side =
                    if openRail = Some side then
                        "minmax(10rem, 16rem)"
                    else
                        "auto"

                Html.div [
                    prop.key layer.Id
                    prop.ref surfaceRef
                    prop.className
                        "swt:relative swt:mx-4 swt:grid swt:min-w-0 swt:items-start swt:gap-x-8 swt:motion-fade-in"
                    prop.style [
                        style.custom (
                            "gridTemplateColumns",
                            $"{railTrack ProvenanceSide.Input} minmax(0, 1fr) {railTrack ProvenanceSide.Output}"
                        )
                    ]
                    if debug then
                        prop.testId "provenance-surface"
                    prop.children [
                        connectorOverlay
                        mediumRailColumn ProvenanceSide.Input
                        Html.div [
                            prop.className
                                "swt:grid swt:min-w-0 swt:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] swt:items-start swt:gap-8"
                            prop.children [
                                groupColumnFor ProvenanceSide.Input
                                groupColumnFor ProvenanceSide.Output
                            ]
                        ]
                        mediumRailColumn ProvenanceSide.Output
                    ]
                ]
            | LayoutTier.Narrow ->
                // Stacked cards cannot host readable connector curves; connection
                // badges on the cards carry that information instead.
                Html.div [
                    prop.key layer.Id
                    prop.ref surfaceRef
                    prop.className
                        "swt:relative swt:mx-4 swt:flex swt:min-w-0 swt:flex-col swt:gap-4 swt:motion-fade-in"
                    if debug then
                        prop.testId "provenance-surface"
                    prop.children [
                        narrowRailSection ProvenanceSide.Input
                        groupColumnFor ProvenanceSide.Input
                        groupColumnFor ProvenanceSide.Output
                        narrowRailSection ProvenanceSide.Output
                    ]
                ]

        let content =
            Html.div [
                prop.ref rootRef
                prop.className [
                    "swt:flex swt:flex-col swt:bg-base-200 swt:overflow-auto swt:pb-4"
                    // Without an explicit height the editor fills its host instead of
                    // forcing a fixed pixel height into responsive layouts.
                    if height.IsNone then
                        "swt:h-full swt:min-h-0"
                ]
                match height with
                | Some height -> prop.style [ style.height height ]
                | None -> ()
                if debug then
                    prop.testId "provenance-editor-root"
                prop.children [
                    // The toolbar and pending prompts stay pinned while the surface scrolls.
                    Html.div [
                        prop.className
                            "swt:sticky swt:top-0 swt:z-20 swt:flex swt:flex-col swt:gap-4 swt:bg-base-200 swt:p-4"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:flex-wrap swt:items-center swt:justify-between swt:gap-2"
                                prop.children [
                                    Controls.LayerTabs(
                                        session,
                                        (fun layerId -> Session.selectLayer layerId session |> publish),
                                        (fun name ->
                                            EditorActions.addLayer
                                                session
                                                layer.Id
                                                inputGroups
                                                outputGroups
                                                uiState
                                                name
                                                publish
                                        ),
                                        sourceColors = uiState.PropertyColors.SourceColors,
                                        onSetSourceColor = setSourceColor,
                                        debug = debug
                                    )
                                    Html.div [
                                        prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                        prop.children [
                                            Html.button [
                                                prop.title (
                                                    if showPropertyHeaderConnectors then
                                                        "Hide property header connectors"
                                                    else
                                                        "Show property header connectors"
                                                )
                                                prop.type'.button
                                                prop.className [
                                                    "swt:btn swt:btn-xs"
                                                    if showPropertyHeaderConnectors then
                                                        "swt:btn-primary"
                                                    else
                                                        "swt:btn-ghost"
                                                ]
                                                prop.custom ("aria-pressed", showPropertyHeaderConnectors)
                                                prop.ariaLabel (
                                                    if showPropertyHeaderConnectors then
                                                        "Hide property header connectors"
                                                    else
                                                        "Show property header connectors"
                                                )
                                                if debug then
                                                    prop.testId "provenance-property-connectors-toggle"
                                                prop.onClick (fun _ ->
                                                    setShowPropertyHeaderConnectors (not showPropertyHeaderConnectors)
                                                )
                                                prop.children [
                                                    Html.i [
                                                        prop.className [
                                                            "swt:iconify swt:size-4"
                                                            if showPropertyHeaderConnectors then
                                                                "swt:fluent--eye-20-regular"
                                                            else
                                                                "swt:fluent--eye-hide-20-regular"
                                                        ]
                                                    ]
                                                    Html.span "Property connectors"
                                                ]
                                            ]
                                            Html.button [
                                                match density with
                                                | Density.EditorDensity.Compact ->
                                                    prop.title "Toggle comfortable density"
                                                | Density.EditorDensity.Comfortable ->
                                                    prop.title "Toggle compact density"

                                                prop.type'.button
                                                prop.className [
                                                    "swt:btn swt:btn-xs"
                                                    if density = Density.EditorDensity.Compact then
                                                        "swt:btn-primary"
                                                    else
                                                        "swt:btn-ghost"
                                                ]
                                                prop.custom ("aria-pressed", (density = Density.EditorDensity.Compact))
                                                prop.ariaLabel (
                                                    if density = Density.EditorDensity.Compact then
                                                        "Switch to comfortable density"
                                                    else
                                                        "Switch to compact density"
                                                )
                                                if debug then
                                                    prop.testId "provenance-density-toggle"
                                                prop.onClick (fun _ ->
                                                    setDensity (
                                                        if density = Density.EditorDensity.Compact then
                                                            Density.EditorDensity.Comfortable
                                                        else
                                                            Density.EditorDensity.Compact
                                                    )
                                                )
                                                prop.text "Compact"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                            propertyShelf
                            Controls.FilterToolbar(
                                uiState.Filters,
                                (fun text -> applyUiState (State.Filters.setSearch text)),
                                setPropertySort,
                                (fun sort -> applyUiState (State.Filters.setGroupSort sort)),
                                (fun filter -> applyUiState (State.Filters.setValueCountFilter filter)),
                                (fun filter -> applyUiState (State.Filters.setOriginFilter filter)),
                                debug = debug
                            )
                            // Alerts and prompts float over the surface from a zero-height
                            // anchor: showing them no longer pushes the whole editor down
                            // (which also forced a full connector remeasure).
                            Html.div [
                                prop.className "swt:relative swt:z-30 swt:h-0"
                                prop.children [
                                    Html.div [
                                        prop.className
                                            "swt:pointer-events-none swt:absolute swt:inset-x-0 swt:top-2 swt:mx-auto swt:flex swt:w-full swt:max-w-3xl swt:flex-col swt:gap-2 swt:px-4"
                                        prop.children [
                                            let floatingPanel content =
                                                Html.div [
                                                    prop.className
                                                        "swt:pointer-events-auto swt:rounded-box swt:shadow-lg swt:motion-pop-in"
                                                    prop.children [ content ]
                                                ]

                                            match uiState.Error with
                                            | Some error -> floatingPanel (EditorPanels.errorAlert error)
                                            | None -> Html.none

                                            match uiState.PendingAssignmentBatch with
                                            | Some batch ->
                                                floatingPanel (
                                                    EditorPanels.assignmentBatchWarning
                                                        debug
                                                        batch
                                                        confirmBatch
                                                        (fun () -> applyUiState State.AssignmentBatch.clear)
                                                )
                                            | None -> Html.none

                                            match uiState.PendingMemberResolution with
                                            | Some pending ->
                                                floatingPanel (
                                                    EditorPanels.memberResolutionPrompt
                                                        debug
                                                        pending
                                                        resolveAllToAll
                                                        (fun pending ->
                                                            applyUiState (State.MemberResolution.chooseManual pending)
                                                        )
                                                        (fun () -> applyUiState State.MemberResolution.clearPending)
                                                )
                                            | None -> Html.none
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    surface
                    EditorPanels.connectionDetails debug connections uiState.Detail
                ]
            ]

        DndKit.DndContext(
            sensors = sensors,
            collisionDetection = DndKit.pointerWithin,
            onDragStart =
                (fun event ->
                    HoverHighlight.clear hoverStore.current
                    DragHandlers.handleStart surfaceRef setActiveDrag liveDragStore.current event
                ),
            onDragMove = DragHandlers.handleMove liveDragStore.current,
            onDragCancel =
                (fun _ ->
                    setActiveDrag None
                    setArmedHandle None
                    LiveDrag.clear liveDragStore.current
                ),
            onDragEnd =
                (fun event ->
                    setActiveDrag None
                    setArmedHandle None
                    LiveDrag.clear liveDragStore.current
                    DragHandlers.handleEnd dragContext event
                ),
            children =
                Density.provider
                    density
                    (ConnectionDragHints.provider
                        connectionInteraction
                        (HoverHighlight.provider
                            hoverStore.current
                            (React.Fragment [
                                content
                                DndKit.DragOverlay(
                                    children = EditorSurface.dragOverlay lookups.FindPropertyValue debug activeDrag
                                )
                            ])))
        )

    [<ReactComponent>]
    static member Editor
        (initialModel: ProvenanceModel, onChange: ProvenanceEditorChange -> unit, ?height: int, ?debug: bool)
        =
        let session, setSession = React.useState (Session.init initialModel)

        let change (next: ProvenanceEditorChange) =
            setSession next.Session
            onChange next

        ProvenanceGrouping.Main(session, change, ?height = height, ?debug = debug)
