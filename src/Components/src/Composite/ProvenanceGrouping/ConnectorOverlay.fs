namespace Swate.Components.Composite.ProvenanceGrouping

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Primitive.ContextMenu
open Swate.Components.Primitive.ContextMenu.Types
open Swate.Components.Composite.ProvenanceGrouping.Types

type private MeasuredConnector =
    {
        Key: string
        Path: string
        TestId: string
        ClassName: string
        StrokeWidth: float
        StrokeDasharray: string option
        InteractiveConnection: DisplayConnection option
        AriaLabel: string option
    }

type ConnectorOverlayState =
    {
        ExpandedGroup: (ProvenanceSide * string) option
        SelectedConnectionId: string option
        ManualResolutionPairs: ManualResolutionPair list
        ExpandedProperties: Set<ProvenancePairId * ProvenanceSide * GroupingKey>
    }

module ConnectorOverlayState =

    let fromUiState (uiState: UiState) =
        let expandedGroup =
            match uiState.Detail with
            | Some(ProvenanceDetail.Group(side, groupId)) -> Some(side, groupId)
            | _ -> None

        let selectedConnectionId =
            match uiState.Detail with
            | Some(ProvenanceDetail.Connection connectionId) -> Some connectionId
            | _ -> None

        {
            ExpandedGroup = expandedGroup
            SelectedConnectionId = selectedConnectionId
            ManualResolutionPairs = uiState.ManualResolutionPairs
            ExpandedProperties = uiState.ExpandedProperties
        }

    let isGroupExpanded side groupId state =
        state.ExpandedGroup = Some(side, groupId)

    let isPropertyExpanded pairId side header state =
        state.ExpandedProperties.Contains(pairId, side, { Header = header })

/// Measures connection handles and builds SVG connector paths between them.
module private ConnectorMeasure =

    let private tryHandle (container: HTMLElement) handle =
        let node: HTMLElement =
            !!container?querySelector($"[data-provenance-connection-node='{DragDrop.connectionHandleNodeId handle}']")

        if isNull node then None else Some node

    let private center (container: HTMLElement) (node: HTMLElement) =
        let origin = container.getBoundingClientRect ()
        let rect = node.getBoundingClientRect ()

        {
            X = rect.left - origin.left + float container.scrollLeft + rect.width / 2.
            Y = rect.top - origin.top + float container.scrollTop + rect.height / 2.
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

    let pathBetweenHandles container source target =
        match tryHandle container source, tryHandle container target with
        | Some sourceNode, Some targetNode ->
            pathBetweenPoints (center container sourceNode) (center container targetNode)
        | _ -> None

    /// Rail connectors shorter than this are skipped entirely so dense layouts do not
    /// fill the rail gutters with overlapping stubs.
    let minimumConnectorDistance = 24.

    let private distanceBetween start finish =
        let deltaX = finish.X - start.X
        let deltaY = finish.Y - start.Y
        sqrt (deltaX * deltaX + deltaY * deltaY)

    let pathBetweenDistantHandles container source target =
        match tryHandle container source, tryHandle container target with
        | Some sourceNode, Some targetNode ->
            let start = center container sourceNode
            let finish = center container targetNode

            if distanceBetween start finish < minimumConnectorDistance then
                None
            else
                pathBetweenPoints start finish
        | _ -> None

/// Builds handle references used by overlay measurements.
module private ConnectorHandles =

    let group side groupId : ConnectionHandleRef =
        {
            Kind = ConnectionHandleKind.GroupCard
            Side = side
            Id = groupId
            ParentGroupId = None
        }

    let member' side groupId setId : ConnectionHandleRef =
        {
            Kind = ConnectionHandleKind.GroupMember
            Side = side
            Id = setId
            ParentGroupId = Some groupId
        }

    let propertyHeader side header : ConnectionHandleRef =
        {
            Kind = ConnectionHandleKind.PropertyHeader
            Side = side
            Id = DragDrop.propertyHeaderIdentity header
            ParentGroupId = None
        }

    let propertyValue side propertyValueId : ConnectionHandleRef =
        {
            Kind = ConnectionHandleKind.PropertyValue
            Side = side
            Id = propertyValueId
            ParentGroupId = None
        }

    /// Measurement-only anchor on the property-facing card edge; rail connectors end here
    /// instead of at the draggable group handle on the opposite edge.
    let propertyAnchor side groupId : ConnectionHandleRef =
        {
            Kind = ConnectionHandleKind.GroupPropertyAnchor
            Side = side
            Id = groupId
            ParentGroupId = None
        }

/// Projects model/UI state into concrete connector path definitions.
module private ConnectorPaths =

    let private measured key testId className strokeWidth strokeDasharray interactiveConnection ariaLabel path =
        {
            Key = key
            Path = path
            TestId = testId
            ClassName = className
            StrokeWidth = strokeWidth
            StrokeDasharray = strokeDasharray
            InteractiveConnection = interactiveConnection
            AriaLabel = ariaLabel
        }

    let private isManuallyResolving pairId side groupId overlayState =
        overlayState.ManualResolutionPairs
        |> List.exists (fun resolution ->
            resolution.PairId = pairId
            && ((side = ProvenanceSide.Input && resolution.InputGroupId = groupId)
                || (side = ProvenanceSide.Output && resolution.OutputGroupId = groupId)))

    let private isConnectedToExpanded connections side groupId overlayState =
        connections
        |> List.exists (fun connection ->
            match side with
            | ProvenanceSide.Input ->
                connection.SourceGroupId = groupId
                && ConnectorOverlayState.isGroupExpanded ProvenanceSide.Output connection.TargetGroupId overlayState
            | ProvenanceSide.Output ->
                connection.TargetGroupId = groupId
                && ConnectorOverlayState.isGroupExpanded ProvenanceSide.Input connection.SourceGroupId overlayState)

    let private isGroupExpanded pairId connections side groupId overlayState =
        ConnectorOverlayState.isGroupExpanded side groupId overlayState
        || isManuallyResolving pairId side groupId overlayState
        || isConnectedToExpanded connections side groupId overlayState

    let groupConnections container pairId connections overlayState =
        connections
        // Expanded endpoints swap the aggregate group connector for the
        // member-level connectors, so the group line disappears instead of
        // doubling up underneath them.
        |> List.filter (fun connection ->
            not (isGroupExpanded pairId connections ProvenanceSide.Input connection.SourceGroupId overlayState)
            && not (isGroupExpanded pairId connections ProvenanceSide.Output connection.TargetGroupId overlayState))
        |> List.choose (fun connection ->
            ConnectorMeasure.pathBetweenHandles
                container
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
            ))

    let memberConnections container pairId (model: ProvenanceModel) connections overlayState =
        connections
        |> List.collect (fun displayConnection ->
            let inputExpanded =
                isGroupExpanded pairId connections ProvenanceSide.Input displayConnection.SourceGroupId overlayState

            let outputExpanded =
                isGroupExpanded pairId connections ProvenanceSide.Output displayConnection.TargetGroupId overlayState

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

                        let singleConnection =
                            {
                                displayConnection with
                                    ConnectionIds = [ connectionId ]
                            }

                        ConnectorMeasure.pathBetweenHandles container source target
                        |> Option.map (
                            measured
                                $"member:{displayConnection.Id}:{connectionId}"
                                "provenance-member-connection"
                                "swt:text-primary/70 swt:pointer-events-none"
                                2.0
                                None
                                (Some singleConnection)
                                (Some $"Select connection {displayConnection.Id}")
                        )))
        )

    let private memberHasMatchingValue (model: ProvenanceModel) predicate (member': DisplayMember) =
        member'.PropertyValueIds
        |> List.exists (fun propertyValueId ->
            model.PropertyValues.TryFind propertyValueId |> Option.exists predicate)

    let private groupsMatching model predicate groups =
        groups
        |> List.filter (fun (group: DisplayGroup) ->
            group.Members |> List.exists (memberHasMatchingValue model predicate))

    /// Dashed rail connectors derived from model data only: collapsed properties
    /// draw one line per same-side group containing any value for that property.
    let private railConnectionsForSide
        container
        pairId
        (model: ProvenanceModel)
        side
        groups
        (railProjection: PropertyRails.RailProjection)
        overlayState
        =
        railProjection.Headers
        |> List.filter (fun header -> not (ConnectorOverlayState.isPropertyExpanded pairId side header overlayState))
        |> List.collect (fun header ->
            groupsMatching model (fun propertyValue -> propertyValue.Header = header) groups
            |> List.choose (fun group ->
                ConnectorMeasure.pathBetweenDistantHandles
                    container
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
                )))

    let private propertyValueMatches header value unit' (propertyValue: ProvenancePropertyValue) =
        propertyValue.Header = header
        && propertyValue.Value = value
        && propertyValue.Unit = unit'

    let private valueRailConnectionsForSide
        container
        pairId
        (model: ProvenanceModel)
        side
        groups
        (railProjection: PropertyRails.RailProjection)
        overlayState
        =
        railProjection.Headers
        |> List.filter (fun header -> ConnectorOverlayState.isPropertyExpanded pairId side header overlayState)
        |> List.collect (fun header ->
            railProjection.ValuesByHeader
            |> Map.tryFind header
            |> Option.defaultValue []
            |> List.collect (fun propertyValue ->
                groupsMatching model (propertyValueMatches header propertyValue.Value propertyValue.Unit) groups
                |> List.choose (fun group ->
                    ConnectorMeasure.pathBetweenDistantHandles
                        container
                        (ConnectorHandles.propertyValue side propertyValue.Id)
                        (ConnectorHandles.propertyAnchor side group.Id)
                    |> Option.map (
                        measured
                            $"value:{side}:{DragDrop.propertyHeaderIdentity header}:{Formatting.formatValue propertyValue.Value propertyValue.Unit}:{group.Id}"
                            "provenance-value-connection"
                            "swt:text-secondary swt:pointer-events-none"
                            1.75
                            (Some "4 4")
                            None
                            None
                    ))))

    let railConnections container pairId model inputGroups outputGroups inputRailProjection outputRailProjection overlayState =
        [
            yield! railConnectionsForSide container pairId model ProvenanceSide.Input inputGroups inputRailProjection overlayState
            yield! railConnectionsForSide container pairId model ProvenanceSide.Output outputGroups outputRailProjection overlayState
            yield! valueRailConnectionsForSide container pairId model ProvenanceSide.Input inputGroups inputRailProjection overlayState
            yield! valueRailConnectionsForSide container pairId model ProvenanceSide.Output outputGroups outputRailProjection overlayState
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
            ))

    let all container pairId model inputGroups outputGroups connections inputRailProjection outputRailProjection overlayState =
        [
            yield! railConnections container pairId model inputGroups outputGroups inputRailProjection outputRailProjection overlayState
            yield! groupConnections container pairId connections overlayState
            yield! memberConnections container pairId model connections overlayState
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
            if isNull node then None else Some(node.getAttribute connectionKeyAttribute)
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

[<Erase; Mangle(false)>]
type ConnectorOverlay =

    [<ReactComponent>]
    static member Main
        (
            containerRef: IRefValue<HTMLElement option>,
            pairId: ProvenancePairId,
            model: ProvenanceModel,
            inputGroups: DisplayGroup list,
            outputGroups: DisplayGroup list,
            connections: DisplayConnection list,
            inputRailProjection: PropertyRails.RailProjection,
            outputRailProjection: PropertyRails.RailProjection,
            overlayState: ConnectorOverlayState,
            liveConnectionDrag: LiveConnectionDrag option,
            onSelect: DisplayConnection -> unit,
            ?onRemove: DisplayConnection -> unit,
            ?debug: bool
        ) =
        let paths, setPaths = React.useState ([]: MeasuredConnector list)
        let hoveredKey, setHoveredKey = React.useState<string option> None

        let measure () =
            match containerRef.current with
            | None -> setPaths []
            | Some container ->
                ConnectorPaths.all
                    container
                    pairId
                    model
                    inputGroups
                    outputGroups
                    connections
                    inputRailProjection
                    outputRailProjection
                    overlayState
                |> setPaths

        React.useEffect (
            (fun () ->
                measure ()

                match containerRef.current with
                | None -> FsReact.createDisposable (fun () -> ())
                | Some container ->
                    let onLayout = fun (_: Event) -> measure ()
                    container.addEventListener ("scroll", onLayout)
                    Browser.Dom.window.addEventListener ("resize", onLayout)
                    let observer = ConnectorObserver.create measure
                    ConnectorObserver.observeNode observer container
                    // Resize nodes are the content-sized boxes (property headers, value
                    // chips); their handles move when the box grows, without the handle
                    // itself changing size, so the boxes must be observed directly.
                    ConnectorObserver.observeMatching
                        container
                        "[data-provenance-group-node],[data-provenance-connection-node],[data-provenance-resize-node]"
                        observer

                    FsReact.createDisposable (fun () ->
                        container.removeEventListener ("scroll", onLayout)
                        Browser.Dom.window.removeEventListener ("resize", onLayout)
                        ConnectorObserver.disconnect observer
                    )
            ),
            [| box pairId
               box model
               box inputGroups
               box outputGroups
               box connections
               box inputRailProjection
               box outputRailProjection
               box overlayState |]
        )

        let selectedConnectionId = overlayState.SelectedConnectionId
        let livePath = ConnectorPaths.liveConnection liveConnectionDrag

        let renderedPaths =
            match livePath with
            | Some live -> paths @ [ live ]
            | None -> paths

        React.Fragment [
            Svg.svg [
                svg.className "swt:absolute swt:inset-0 swt:pointer-events-none swt:size-full"
                svg.children [
                    for measured in renderedPaths do
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

                        let debugAttributes = [
                            if defaultArg debug false then
                                svg.custom ("data-testid", measured.TestId)
                                svg.custom ("data-provenance-connection-key", measured.Key)
                        ]

                        Svg.g [
                            svg.key measured.Key
                            svg.children [
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
                                    svg.stroke "currentColor"
                                    svg.strokeWidth strokeWidth
                                    svg.strokeLineCap "round"
                                    svg.custom ("strokeOpacity", strokeOpacity)
                                    svg.className measured.ClassName
                                    match measured.StrokeDasharray with
                                    | Some dash -> svg.custom ("strokeDasharray", dash)
                                    | None -> ()
                                    if measured.InteractiveConnection.IsNone then
                                        yield! debugAttributes
                                ]
                                // A wide transparent stroke is the actual pointer/keyboard target,
                                // so selecting a thin curve no longer needs pixel accuracy.
                                match measured.InteractiveConnection with
                                | Some connection ->
                                    Svg.path [
                                        svg.d measured.Path
                                        svg.fill "none"
                                        svg.stroke "transparent"
                                        svg.strokeWidth 14
                                        svg.className "swt:pointer-events-auto swt:cursor-pointer focus:swt:outline-none"
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
            match onRemove with
            | Some remove ->
                ContextMenu.ContextMenu(
                    ConnectorContextMenu.items remove,
                    ref = containerRef,
                    onSpawn = ConnectorContextMenu.spawnData paths,
                    debug = defaultArg debug false
                )
            | None -> Html.none
        ]
