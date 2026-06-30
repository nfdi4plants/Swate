namespace Swate.Components.Page.ProvenanceGrouping

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Primitive.ContextMenu
open Swate.Components.Primitive.ContextMenu.Types
open Swate.Components.Page.ProvenanceGrouping.Types

type private MeasuredConnector = {
    Key: string
    Path: string
    TestId: string
    ClassName: string
    StrokeWidth: float
    StrokeDasharray: string option
    InteractiveConnection: DisplayConnection option
    AriaLabel: string option
    Color: string option
}

type ConnectorOverlayState = {
    ExpandedGroup: (ProvenanceSide * string) option
    SelectedConnectionId: string option
    ExpandedProperties: Set<ProvenanceLayerId * ProvenanceSide * GroupingKey>
}

module ConnectorOverlayState =

    let fromUiState (uiState: UiState) =
        let selectedConnectionId =
            match uiState.Detail with
            | Some(ProvenanceDetail.Connection connectionId) -> Some connectionId
            | _ -> None

        {
            ExpandedGroup = uiState.ExpandedGroup
            SelectedConnectionId = selectedConnectionId
            ExpandedProperties = uiState.ExpandedProperties
        }

    let isGroupExpanded side groupId state =
        state.ExpandedGroup = Some(side, groupId)

    let isPropertyExpanded layerId side header state =
        state.ExpandedProperties.Contains(layerId, side, { Header = header })

type private ConnectorMeasureContext = {
    Container: HTMLElement
    Origin: ClientRect
    Nodes: Map<string, HTMLElement>
}

module private ConnectorDom =

    [<Emit("Array.from($0.querySelectorAll($1))")>]
    let querySelectorAll (_container: HTMLElement) (_selector: string) : HTMLElement[] = jsNative

    let connectionNodes (container: HTMLElement) =
        querySelectorAll container "[data-provenance-connection-node]"
        |> Array.choose (fun node ->
            let id = node.getAttribute "data-provenance-connection-node"
            if isNull id then None else Some(id, node)
        )
        |> Map.ofArray

/// Measures connection handles and builds SVG connector paths between them.
module private ConnectorMeasure =

    let createContext container = {
        Container = container
        Origin = container.getBoundingClientRect ()
        Nodes = ConnectorDom.connectionNodes container
    }

    let private tryHandle (context: ConnectorMeasureContext) handle =
        context.Nodes |> Map.tryFind (DragDrop.connectionHandleNodeId handle)

    let private center (context: ConnectorMeasureContext) (node: HTMLElement) =
        let origin = context.Origin
        let rect = node.getBoundingClientRect ()

        {
            X = rect.left - origin.left + float context.Container.scrollLeft + rect.width / 2.
            Y = rect.top - origin.top + float context.Container.scrollTop + rect.height / 2.
        }

    let pathBetweenPoints start finish =
        let deltaX = finish.X - start.X
        let direction = if deltaX >= 0. then 1. else -1.
        // The bend scales with the horizontal span, so connectors between nearby
        // endpoints stay short stubs instead of looping past their targets.
        let bend = max 8. (abs deltaX / 2.)
        let firstControlX = start.X + direction * bend
        let secondControlX = finish.X - direction * bend
        Some $"M {start.X} {start.Y} C {firstControlX} {start.Y}, {secondControlX} {finish.Y}, {finish.X} {finish.Y}"

    let pathBetweenHandles context source target =
        match tryHandle context source, tryHandle context target with
        | Some sourceNode, Some targetNode -> pathBetweenPoints (center context sourceNode) (center context targetNode)
        | _ -> None

    /// Rail connectors shorter than this are skipped entirely so dense layouts do not
    /// fill the rail gutters with overlapping stubs.
    let minimumConnectorDistance = 24.

    let private distanceBetween start finish =
        let deltaX = finish.X - start.X
        let deltaY = finish.Y - start.Y
        sqrt (deltaX * deltaX + deltaY * deltaY)

    let pathBetweenDistantHandles context source target =
        match tryHandle context source, tryHandle context target with
        | Some sourceNode, Some targetNode ->
            let start = center context sourceNode
            let finish = center context targetNode

            if distanceBetween start finish < minimumConnectorDistance then
                None
            else
                pathBetweenPoints start finish
        | _ -> None

/// Builds handle references used by overlay measurements.
module private ConnectorHandles =

    let group side groupId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.GroupCard
        Side = side
        Id = groupId
        ParentGroupId = None
    }

    let member' side groupId setId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.GroupMember
        Side = side
        Id = setId
        ParentGroupId = Some groupId
    }

    let propertyHeader side header : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.PropertyHeader
        Side = side
        Id = DragDrop.propertyHeaderIdentity header
        ParentGroupId = None
    }

    let propertyValue side propertyValueId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.PropertyValue
        Side = side
        Id = propertyValueId
        ParentGroupId = None
    }

    /// Measurement-only anchor on the property-facing card edge; rail connectors end here
    /// instead of at the draggable group handle on the opposite edge.
    let propertyAnchor side groupId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.GroupPropertyAnchor
        Side = side
        Id = groupId
        ParentGroupId = None
    }

/// Projects model/UI state into concrete connector path definitions.
module private ConnectorPaths =

    let private measured key testId className strokeWidth strokeDasharray interactiveConnection ariaLabel color path = {
        Key = key
        Path = path
        TestId = testId
        ClassName = className
        StrokeWidth = strokeWidth
        StrokeDasharray = strokeDasharray
        InteractiveConnection = interactiveConnection
        AriaLabel = ariaLabel
        Color = color
    }

    let private groupById (inputGroups: DisplayGroup list) (outputGroups: DisplayGroup list) side groupId =
        let groups =
            match side with
            | ProvenanceSide.Input -> inputGroups
            | ProvenanceSide.Output -> outputGroups

        groups |> List.tryFind (fun group -> group.Id = groupId)

    let private isGroupedCard (inputGroups: DisplayGroup list) (outputGroups: DisplayGroup list) side groupId =
        groupById inputGroups outputGroups side groupId
        |> Option.exists (fun group -> group.GroupingValues |> List.isEmpty |> not)

    let private isConnectedToExpanded
        (inputGroups: DisplayGroup list)
        (outputGroups: DisplayGroup list)
        connections
        side
        groupId
        overlayState
        =
        isGroupedCard inputGroups outputGroups side groupId
        && (connections
            |> List.exists (fun connection ->
                match side with
                | ProvenanceSide.Input ->
                    connection.SourceGroupId = groupId
                    && ConnectorOverlayState.isGroupExpanded
                        ProvenanceSide.Output
                        connection.TargetGroupId
                        overlayState
                | ProvenanceSide.Output ->
                    connection.TargetGroupId = groupId
                    && ConnectorOverlayState.isGroupExpanded ProvenanceSide.Input connection.SourceGroupId overlayState
            ))

    let private isGroupExpanded
        (inputGroups: DisplayGroup list)
        (outputGroups: DisplayGroup list)
        connections
        side
        groupId
        overlayState
        =
        ConnectorOverlayState.isGroupExpanded side groupId overlayState
        || isConnectedToExpanded inputGroups outputGroups connections side groupId overlayState

    let groupConnections
        context
        (inputGroups: DisplayGroup list)
        (outputGroups: DisplayGroup list)
        connections
        overlayState
        =
        connections
        // Expanded endpoints swap the aggregate group connector for the
        // member-level connectors, so the group line disappears instead of
        // doubling up underneath them.
        |> List.filter (fun connection ->
            not (
                isGroupExpanded
                    inputGroups
                    outputGroups
                    connections
                    ProvenanceSide.Input
                    connection.SourceGroupId
                    overlayState
            )
            && not (
                isGroupExpanded
                    inputGroups
                    outputGroups
                    connections
                    ProvenanceSide.Output
                    connection.TargetGroupId
                    overlayState
            )
        )
        |> List.choose (fun connection ->
            ConnectorMeasure.pathBetweenHandles
                context
                (ConnectorHandles.group ProvenanceSide.Input connection.SourceGroupId)
                (ConnectorHandles.group ProvenanceSide.Output connection.TargetGroupId)
            |> Option.map (
                measured
                    $"connection:{connection.Id}"
                    "provenance-connection"
                    "swt:text-primary"
                    2.25
                    None
                    (Some connection)
                    (Some $"Select connection {connection.Id}")
                    None
            )
        )

    let memberConnections
        context
        (model: ProvenanceModel)
        (inputGroups: DisplayGroup list)
        (outputGroups: DisplayGroup list)
        connections
        overlayState
        =
        connections
        |> List.collect (fun displayConnection ->
            let inputExpanded =
                isGroupExpanded
                    inputGroups
                    outputGroups
                    connections
                    ProvenanceSide.Input
                    displayConnection.SourceGroupId
                    overlayState

            let outputExpanded =
                isGroupExpanded
                    inputGroups
                    outputGroups
                    connections
                    ProvenanceSide.Output
                    displayConnection.TargetGroupId
                    overlayState

            if not inputExpanded && not outputExpanded then
                []
            else
                displayConnection.ConnectionIds
                |> List.choose (fun connectionId ->
                    model.Connections.TryFind connectionId
                    |> Option.bind (fun connection ->
                        let source =
                            if inputExpanded then
                                ConnectorHandles.member'
                                    ProvenanceSide.Input
                                    displayConnection.SourceGroupId
                                    connection.InputSetId
                            else
                                ConnectorHandles.group ProvenanceSide.Input displayConnection.SourceGroupId

                        let target =
                            if outputExpanded then
                                ConnectorHandles.member'
                                    ProvenanceSide.Output
                                    displayConnection.TargetGroupId
                                    connection.OutputSetId
                            else
                                ConnectorHandles.group ProvenanceSide.Output displayConnection.TargetGroupId

                        let singleConnection = {
                            displayConnection with
                                ConnectionIds = [ connectionId ]
                        }

                        ConnectorMeasure.pathBetweenHandles context source target
                        |> Option.map (
                            measured
                                $"member:{displayConnection.Id}:{connectionId}"
                                "provenance-member-connection"
                                "swt:text-primary/70 swt:pointer-events-none"
                                2.0
                                None
                                (Some singleConnection)
                                (Some $"Select connection {displayConnection.Id}")
                                None
                        )
                    )
                )
        )

    let private memberHasMatchingValue (model: ProvenanceModel) predicate (member': DisplayMember) =
        member'.PropertyValueIds
        |> List.exists (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId |> Option.exists predicate)

    let private groupsMatching model predicate groups =
        groups
        |> List.filter (fun (group: DisplayGroup) ->
            group.Members |> List.exists (memberHasMatchingValue model predicate)
        )

    /// Dashed rail connectors derived from model data only: collapsed properties
    /// draw one line per same-side group containing any value for that property.
    let private railConnectionsForSide
        context
        layerId
        (model: ProvenanceModel)
        side
        groups
        (railProjection: PropertyRails.RailProjection)
        (colorByHeader: Map<ProvenancePropertyHeader, string option>)
        overlayState
        =
        railProjection.Headers
        |> List.filter (fun header -> not (ConnectorOverlayState.isPropertyExpanded layerId side header overlayState))
        |> List.collect (fun header ->
            let color =
                railProjection.ColorByHeader
                |> Map.tryFind header
                |> Option.bind id
                |> Option.orElseWith (fun () -> colorByHeader |> Map.tryFind header |> Option.bind id)

            groupsMatching model (fun propertyValue -> propertyValue.Header = header) groups
            |> List.choose (fun group ->
                ConnectorMeasure.pathBetweenDistantHandles
                    context
                    (ConnectorHandles.propertyHeader side header)
                    (ConnectorHandles.propertyAnchor side group.Id)
                |> Option.map (
                    measured
                        $"property:{side}:{DragDrop.propertyHeaderIdentity header}:{group.Id}"
                        "provenance-property-connection"
                        "swt:text-secondary swt:pointer-events-none"
                        1.75
                        (Some "4 4")
                        None
                        None
                        color
                )
            )
        )

    let private propertyValueMatches header value unit' (propertyValue: ProvenancePropertyValue) =
        propertyValue.Header = header
        && propertyValue.Value = value
        && propertyValue.Unit = unit'

    let private valueRailConnectionsForSide
        context
        layerId
        (model: ProvenanceModel)
        side
        groups
        (railProjection: PropertyRails.RailProjection)
        (colorByHeader: Map<ProvenancePropertyHeader, string option>)
        overlayState
        =
        railProjection.Headers
        |> List.filter (fun header -> ConnectorOverlayState.isPropertyExpanded layerId side header overlayState)
        |> List.collect (fun header ->
            let color =
                railProjection.ColorByHeader
                |> Map.tryFind header
                |> Option.bind id
                |> Option.orElseWith (fun () -> colorByHeader |> Map.tryFind header |> Option.bind id)

            railProjection.ValuesByHeader
            |> Map.tryFind header
            |> Option.defaultValue []
            |> List.collect (fun propertyValue ->
                groupsMatching model (propertyValueMatches header propertyValue.Value propertyValue.Unit) groups
                |> List.choose (fun group ->
                    ConnectorMeasure.pathBetweenDistantHandles
                        context
                        (ConnectorHandles.propertyValue side propertyValue.Id)
                        (ConnectorHandles.propertyAnchor side group.Id)
                    |> Option.map (
                        measured
                            $"value:{side}:{DragDrop.propertyHeaderIdentity header}:{Formatting.formatValue propertyValue.Value propertyValue.Unit}:{group.Id}"
                            "provenance-value-connection"
                            "swt:text-accent swt:pointer-events-none"
                            2.0
                            (Some "4 4")
                            None
                            None
                            color
                    )
                )
            )
        )

    let railConnections
        context
        layerId
        model
        inputGroups
        outputGroups
        inputRailProjection
        outputRailProjection
        (colorByHeader: Map<ProvenancePropertyHeader, string option>)
        overlayState
        showPropertyHeaderConnectors
        =
        [
            if showPropertyHeaderConnectors then
                yield!
                    railConnectionsForSide
                        context
                        layerId
                        model
                        ProvenanceSide.Input
                        inputGroups
                        inputRailProjection
                        colorByHeader
                        overlayState

                yield!
                    railConnectionsForSide
                        context
                        layerId
                        model
                        ProvenanceSide.Output
                        outputGroups
                        outputRailProjection
                        colorByHeader
                        overlayState
            yield!
                valueRailConnectionsForSide
                    context
                    layerId
                    model
                    ProvenanceSide.Input
                    inputGroups
                    inputRailProjection
                    colorByHeader
                    overlayState
            yield!
                valueRailConnectionsForSide
                    context
                    layerId
                    model
                    ProvenanceSide.Output
                    outputGroups
                    outputRailProjection
                    colorByHeader
                    overlayState
        ]

    let liveConnection liveConnectionDrag =
        liveConnectionDrag
        |> Option.bind (fun live ->
            ConnectorMeasure.pathBetweenPoints live.Start live.Current
            |> Option.map (
                measured
                    "live"
                    "provenance-live-connection"
                    "swt:text-primary swt:pointer-events-none swt:opacity-80"
                    2.25
                    (Some "6 4")
                    None
                    None
                    None
            )
        )

    let all
        context
        layerId
        model
        inputGroups
        outputGroups
        connections
        inputRailProjection
        outputRailProjection
        (colorByHeader: Map<ProvenancePropertyHeader, string option>)
        overlayState
        showPropertyHeaderConnectors
        =
        [
            yield!
                railConnections
                    context
                    layerId
                    model
                    inputGroups
                    outputGroups
                    inputRailProjection
                    outputRailProjection
                    colorByHeader
                    overlayState
                    showPropertyHeaderConnectors
            yield! groupConnections context inputGroups outputGroups connections overlayState
            yield! memberConnections context model inputGroups outputGroups connections overlayState
        ]

module private ConnectorSvg =

    let debugAttributes debug measured = [
        if debug then
            svg.custom ("data-testid", measured.TestId)
            svg.custom ("data-provenance-connection-key", measured.Key)
    ]

    let strokeElements measured strokeWidth strokeOpacity debug =
        let strokeColor = measured.Color |> Option.defaultValue "currentColor"
        let debugAttributes = debugAttributes debug measured

        [
            // A surface-colored halo keeps crossing connectors readable.
            Svg.path [
                svg.d measured.Path
                svg.fill "none"
                svg.stroke "currentColor"
                svg.strokeWidth (strokeWidth + 2.5)
                svg.strokeLineCap "round"
                svg.className "swt:text-base-200"
                match measured.StrokeDasharray with
                | Some dash -> svg.custom ("strokeDasharray", dash)
                | None -> ()
            ]
            Svg.path [
                svg.d measured.Path
                svg.fill "none"
                svg.stroke strokeColor
                svg.strokeWidth strokeWidth
                svg.strokeLineCap "round"
                svg.custom ("strokeOpacity", strokeOpacity)
                svg.className measured.ClassName
                match measured.StrokeDasharray with
                | Some dash -> svg.custom ("strokeDasharray", dash)
                | None -> ()
                match measured.Color with
                | Some color -> svg.custom ("data-provenance-color", color)
                | None -> ()
                if measured.InteractiveConnection.IsNone then
                    yield! debugAttributes
            ]
        ]

module private ConnectorContextMenu =

    let connectionKeyAttribute = "data-provenance-interactive-connection-key"

    let private tryTargetElement (event: Browser.Types.Event) : Browser.Types.Element option =
        let targetObj: obj = box event.target

        if isNullOrUndefined targetObj || isNullOrUndefined targetObj?closest then
            None
        else
            Some(unbox<Browser.Types.Element> targetObj)

    let private interactiveConnectionKey (event: Browser.Types.MouseEvent) =
        event
        |> tryTargetElement
        |> Option.bind (fun target ->
            let node: Browser.Types.Element = !!target?closest($"[{connectionKeyAttribute}]")

            if isNull node then
                None
            else
                Some(node.getAttribute connectionKeyAttribute)
        )
        |> Option.bind (fun key -> if isNull key then None else Some key)

    let spawnData paths event =
        interactiveConnectionKey event
        |> Option.bind (fun key ->
            paths
            |> List.tryFind (fun path -> path.Key = key)
            |> Option.bind (fun path -> path.InteractiveConnection)
        )
        |> Option.map box

    let items remove (data: obj) =
        let connection = data |> unbox<DisplayConnection>

        [
            ContextMenuItem(
                text = Html.span "Delete",
                icon =
                    Html.i [
                        prop.className "swt:iconify swt:fluent--delete-20-regular swt:size-4"
                    ],
                onClick =
                    (fun event ->
                        event.buttonEvent.stopPropagation ()
                        remove connection
                    )
            )
        ]

/// Thin ResizeObserver bindings used to remeasure connector paths after layout changes.
module private ConnectorObserver =

    [<Emit("new ResizeObserver(() => $0())")>]
    let create (callback: unit -> unit) : obj = jsNative

    [<Emit("$0.observe($1)")>]
    let observeNode (observer: obj) (target: HTMLElement) : unit = jsNative

    [<Emit("$0.querySelectorAll($1).forEach(node => $2.observe(node))")>]
    let observeMatching (container: HTMLElement) (selector: string) (observer: obj) : unit = jsNative

    [<Emit("$0.disconnect()")>]
    let disconnect (observer: obj) : unit = jsNative

module private ConnectorMutationObserver =

    [<Emit("new MutationObserver(() => $0())")>]
    let create (callback: unit -> unit) : obj = jsNative

    [<Emit("$0.observe($1, { childList: true, subtree: true, attributes: true, attributeFilter: ['class', 'style'] })")>]
    let observe (observer: obj) (target: HTMLElement) : unit = jsNative

    [<Emit("$0.disconnect()")>]
    let disconnect (observer: obj) : unit = jsNative

module private AnimationFrame =

    [<Emit("requestAnimationFrame($0)")>]
    let request (_callback: unit -> unit) : float = jsNative

    [<Emit("cancelAnimationFrame($0)")>]
    let cancel (_handle: float) : unit = jsNative

[<Erase; Mangle(false)>]
type ConnectorOverlay =

    [<ReactComponent>]
    static member private LiveConnectorLayer(store: LiveDrag.Store, ?debug: bool) =
        let _, bump = React.useStateWithUpdater 0

        React.useEffect (
            (fun () ->
                let unsubscribe =
                    store |> LiveDrag.subscribe (fun () -> bump (fun version -> version + 1))

                FsReact.createDisposable unsubscribe
            ),
            [| box store |]
        )

        match ConnectorPaths.liveConnection store.Current with
        | Some measured ->
            Svg.svg [
                svg.className "swt:absolute swt:inset-0 swt:pointer-events-none swt:size-full"
                svg.children [
                    yield! ConnectorSvg.strokeElements measured measured.StrokeWidth 1.0 (defaultArg debug false)
                ]
            ]
        | None -> Html.none

    [<ReactComponent>]
    static member Main
        (
            containerRef: IRefValue<HTMLElement option>,
            layerId: ProvenanceLayerId,
            model: ProvenanceModel,
            inputGroups: DisplayGroup list,
            outputGroups: DisplayGroup list,
            connections: DisplayConnection list,
            inputRailProjection: PropertyRails.RailProjection,
            outputRailProjection: PropertyRails.RailProjection,
            overlayState: ConnectorOverlayState,
            layoutSignature: string,
            showPropertyHeaderConnectors: bool,
            liveDragStore: LiveDrag.Store,
            onSelect: DisplayConnection -> unit,
            ?onRemove: DisplayConnection -> unit,
            ?debug: bool,
            ?railColorByHeader: Map<ProvenancePropertyHeader, string option>
        ) =
        let paths, setPaths = React.useStateWithUpdater ([]: MeasuredConnector list)
        let hoveredKey, setHoveredKey = React.useState<string option> None
        let pendingFrame = React.useRef (None: float option)
        let debugEnabled = defaultArg debug false
        let colorByHeader = defaultArg railColorByHeader Map.empty

        let setMeasuredPaths next =
            setPaths (fun current -> if current = next then current else next)

        let measureNow () =
            pendingFrame.current <- None

            match containerRef.current with
            | None -> setMeasuredPaths []
            | Some container ->
                let context = ConnectorMeasure.createContext container

                ConnectorPaths.all
                    context
                    layerId
                    model
                    inputGroups
                    outputGroups
                    connections
                    inputRailProjection
                    outputRailProjection
                    colorByHeader
                    overlayState
                    showPropertyHeaderConnectors
                |> setMeasuredPaths

        let scheduleMeasure () =
            match pendingFrame.current with
            | Some _ -> ()
            | None -> pendingFrame.current <- Some(AnimationFrame.request measureNow)

        let cancelPendingFrame () =
            match pendingFrame.current with
            | Some handle ->
                AnimationFrame.cancel handle
                pendingFrame.current <- None
            | None -> ()

        React.useEffect (
            (fun () ->
                measureNow ()

                match containerRef.current with
                | None -> FsReact.createDisposable cancelPendingFrame
                | Some container ->
                    let onLayout = fun (_: Event) -> scheduleMeasure ()
                    container.addEventListener ("scroll", onLayout)
                    Browser.Dom.window.addEventListener ("resize", onLayout)

                    let observer = ConnectorObserver.create scheduleMeasure

                    let observeCurrentNodes () =
                        ConnectorObserver.observeNode observer container

                        ConnectorObserver.observeMatching
                            container
                            "[data-provenance-group-node],[data-provenance-connection-node],[data-provenance-resize-node]"
                            observer

                    let mutationFrame = ref (None: float option)

                    let cancelMutationFrame () =
                        match mutationFrame.Value with
                        | Some handle ->
                            AnimationFrame.cancel handle
                            mutationFrame.Value <- None
                        | None -> ()

                    let scheduleMutationMeasure () =
                        match mutationFrame.Value with
                        | Some _ -> ()
                        | None ->
                            mutationFrame.Value <-
                                Some(
                                    AnimationFrame.request (fun () ->
                                        mutationFrame.Value <- None
                                        observeCurrentNodes ()
                                        scheduleMeasure ()
                                    )
                                )

                    let mutationObserver =
                        ConnectorMutationObserver.create (fun () -> scheduleMutationMeasure ())

                    observeCurrentNodes ()
                    ConnectorMutationObserver.observe mutationObserver container

                    FsReact.createDisposable (fun () ->
                        container.removeEventListener ("scroll", onLayout)
                        Browser.Dom.window.removeEventListener ("resize", onLayout)
                        ConnectorObserver.disconnect observer
                        ConnectorMutationObserver.disconnect mutationObserver
                        cancelMutationFrame ()
                        cancelPendingFrame ()
                    )
            ),
            [|
                box layerId
                box model
                box inputGroups
                box outputGroups
                box connections
                box inputRailProjection
                box outputRailProjection
                box overlayState.ExpandedGroup
                box overlayState.ExpandedProperties
                box showPropertyHeaderConnectors
            |]
        )

        React.useEffect ((fun () -> scheduleMeasure ()), [| box layoutSignature |])

        let selectedConnectionId = overlayState.SelectedConnectionId

        React.Fragment [
            Svg.svg [
                svg.className "swt:absolute swt:inset-0 swt:pointer-events-none swt:size-full"
                svg.children [
                    for measured in paths do
                        let activateFromKeyboard (event: KeyboardEvent) =
                            match measured.InteractiveConnection, event.key with
                            | Some connection, "Enter"
                            | Some connection, " "
                            | Some connection, "Spacebar" ->
                                event.preventDefault ()
                                onSelect connection
                            | Some connection, "Delete"
                            | Some connection, "Backspace" ->
                                match onRemove with
                                | Some remove ->
                                    event.preventDefault ()
                                    remove connection
                                | None -> ()
                            | _ -> ()

                        let isSelected =
                            match measured.InteractiveConnection, selectedConnectionId with
                            | Some connection, Some selectedId -> connection.Id = selectedId
                            | _ -> false

                        let isEmphasized = isSelected || hoveredKey = Some measured.Key

                        let strokeWidth =
                            if isEmphasized then
                                measured.StrokeWidth + 1.25
                            else
                                measured.StrokeWidth

                        // Selecting a connection keeps it bright and recedes its siblings.
                        let strokeOpacity =
                            match measured.InteractiveConnection with
                            | Some _ when isEmphasized -> 1.0
                            | Some _ when selectedConnectionId.IsSome -> 0.3
                            | Some _ -> 0.85
                            | None -> 1.0

                        let debugAttributes = ConnectorSvg.debugAttributes debugEnabled measured

                        Svg.g [
                            svg.key measured.Key
                            svg.children [
                                yield! ConnectorSvg.strokeElements measured strokeWidth strokeOpacity debugEnabled
                                // A wide transparent stroke is the actual pointer/keyboard target,
                                // so selecting a thin curve no longer needs pixel accuracy.
                                match measured.InteractiveConnection with
                                | Some connection ->
                                    Svg.path [
                                        svg.d measured.Path
                                        svg.fill "none"
                                        svg.stroke "transparent"
                                        svg.strokeWidth 14
                                        svg.className
                                            "swt:pointer-events-auto swt:cursor-pointer swt:outline-none swt:shadow-none"
                                        svg.custom ("tabIndex", "0")
                                        svg.custom ("role", "button")
                                        svg.custom (
                                            "aria-label",
                                            measured.AriaLabel
                                            |> Option.defaultValue $"Select connection {connection.Id}"
                                        )
                                        svg.custom (ConnectorContextMenu.connectionKeyAttribute, measured.Key)
                                        yield! debugAttributes
                                        svg.onClick (fun _ -> onSelect connection)
                                        svg.onKeyDown activateFromKeyboard
                                        svg.onMouseEnter (fun _ -> setHoveredKey (Some measured.Key))
                                        svg.onMouseLeave (fun _ -> setHoveredKey None)
                                        svg.onFocus (fun _ -> setHoveredKey (Some measured.Key))
                                        svg.onBlur (fun _ -> setHoveredKey None)
                                    ]
                                | None -> ()
                            ]
                        ]
                ]
            ]
            ConnectorOverlay.LiveConnectorLayer(liveDragStore, ?debug = debug)
            match onRemove with
            | Some remove ->
                ContextMenu.ContextMenu(
                    ConnectorContextMenu.items remove,
                    ref = containerRef,
                    onSpawn = ConnectorContextMenu.spawnData paths,
                    debug = debugEnabled
                )
            | None -> Html.none
        ]
