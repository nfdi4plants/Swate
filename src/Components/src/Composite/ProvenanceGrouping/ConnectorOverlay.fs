namespace Swate.Components.Composite.ProvenanceGrouping

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
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

    let value side propertyValueId : ConnectionHandleRef =
        {
            Kind = ConnectionHandleKind.PropertyValue
            Side = side
            Id = propertyValueId
            ParentGroupId = None
        }

    let propertyHeader side header : ConnectionHandleRef =
        {
            Kind = ConnectionHandleKind.PropertyHeader
            Side = side
            Id = DragDrop.propertyHeaderIdentity header
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

    let private isManuallyResolving pairId side groupId uiState =
        uiState.ManualResolutionPairs
        |> List.exists (fun resolution ->
            resolution.PairId = pairId
            && ((side = ProvenanceSide.Input && resolution.InputGroupId = groupId)
                || (side = ProvenanceSide.Output && resolution.OutputGroupId = groupId)))

    let private isConnectedToExpanded connections side groupId uiState =
        connections
        |> List.exists (fun connection ->
            match side with
            | ProvenanceSide.Input ->
                connection.SourceGroupId = groupId
                && State.Detail.isGroupExpanded ProvenanceSide.Output connection.TargetGroupId uiState
            | ProvenanceSide.Output ->
                connection.TargetGroupId = groupId
                && State.Detail.isGroupExpanded ProvenanceSide.Input connection.SourceGroupId uiState)

    let private isGroupExpanded pairId connections side groupId uiState =
        State.Detail.isGroupExpanded side groupId uiState
        || isManuallyResolving pairId side groupId uiState
        || isConnectedToExpanded connections side groupId uiState

    let groupConnections container connections =
        connections
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

    let memberConnections container pairId (model: ProvenanceModel) connections uiState =
        connections
        |> List.collect (fun displayConnection ->
            let inputExpanded =
                isGroupExpanded pairId connections ProvenanceSide.Input displayConnection.SourceGroupId uiState

            let outputExpanded =
                isGroupExpanded pairId connections ProvenanceSide.Output displayConnection.TargetGroupId uiState

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

                        ConnectorMeasure.pathBetweenHandles container source target
                        |> Option.map (
                            measured
                                $"member:{displayConnection.Id}:{connectionId}"
                                "provenance-member-connection"
                                "swt:text-primary/70 swt:pointer-events-none"
                                2.0
                                None
                                None
                                None
                        )))
        )

    let private expandedHeaders pairId side uiState =
        uiState.ExpandedProperties
        |> Set.toList
        |> List.choose (fun (expandedPairId, expandedSide, key) ->
            if expandedPairId = pairId && expandedSide = side then Some key.Header else None)

    let private memberHasMatchingValue (model: ProvenanceModel) predicate (member': DisplayMember) =
        member'.PropertyValueIds
        |> List.exists (fun propertyValueId ->
            model.PropertyValues.TryFind propertyValueId |> Option.exists predicate)

    let private groupsMatching model predicate groups =
        groups
        |> List.filter (fun (group: DisplayGroup) ->
            group.Members |> List.exists (memberHasMatchingValue model predicate))

    /// Dashed rail connectors derived from model data only: a collapsed property draws one
    /// line per same-side group containing any value for that property, and an expanded
    /// property instead draws one line per value chip and group containing that exact value.
    /// Rail chips deduplicate equal values, so chips match by value, not by occurrence ID.
    let private railConnectionsForSide container pairId (model: ProvenanceModel) side groups uiState =
        let expanded = expandedHeaders pairId side uiState

        PropertyRails.propertyRailHeadersForSide pairId side model uiState
        |> List.collect (fun header ->
            if expanded |> List.contains header then
                PropertyRails.propertyValuesForSideHeader pairId side header model uiState
                |> List.collect (fun chip ->
                    let matchesChip (propertyValue: ProvenancePropertyValue) =
                        propertyValue.Header = chip.Header
                        && propertyValue.Value = chip.Value
                        && propertyValue.Unit = chip.Unit

                    groupsMatching model matchesChip groups
                    |> List.choose (fun group ->
                        ConnectorMeasure.pathBetweenDistantHandles
                            container
                            (ConnectorHandles.value side chip.Id)
                            (ConnectorHandles.propertyAnchor side group.Id)
                        |> Option.map (
                            measured
                                $"value:{side}:{chip.Id}:{group.Id}"
                                "provenance-value-connection"
                                "swt:text-accent swt:pointer-events-none"
                                1.75
                                (Some "3 6")
                                None
                                None
                        )))
            else
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

    let railConnections container pairId model inputGroups outputGroups uiState =
        [
            yield! railConnectionsForSide container pairId model ProvenanceSide.Input inputGroups uiState
            yield! railConnectionsForSide container pairId model ProvenanceSide.Output outputGroups uiState
        ]

    let liveConnection uiState =
        uiState.LiveConnectionDrag
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

    let all container pairId model inputGroups outputGroups connections uiState =
        [
            yield! railConnections container pairId model inputGroups outputGroups uiState
            yield! groupConnections container connections
            yield! memberConnections container pairId model connections uiState
            match liveConnection uiState with
            | Some path -> path
            | None -> ()
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
            uiState: UiState,
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
                ConnectorPaths.all container pairId model inputGroups outputGroups connections uiState
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
            [| box pairId; box model; box inputGroups; box outputGroups; box connections; box uiState |]
        )

        let selectedConnectionId =
            match uiState.Detail with
            | Some(ProvenanceDetail.Connection connectionId) -> Some connectionId
            | _ -> None

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
                            // A wide transparent stroke is the actual click/keyboard target,
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
