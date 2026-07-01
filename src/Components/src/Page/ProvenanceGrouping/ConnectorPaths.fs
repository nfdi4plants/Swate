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

/// Projects model/UI state into concrete connector path definitions.
module ConnectorPaths =

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

    type private RailConnectionTarget = {
        KeySuffix: string
        Handle: ConnectionHandleRef
    }

    let private matchingMembers model predicate (group: DisplayGroup) =
        group.Members |> List.filter (memberHasMatchingValue model predicate)

    let private railConnectionTargets
        model
        inputGroups
        outputGroups
        connections
        predicate
        side
        overlayState
        (group: DisplayGroup)
        =
        let members = matchingMembers model predicate group

        if members.IsEmpty then
            []
        elif isGroupExpanded inputGroups outputGroups connections side group.Id overlayState then
            members
            |> List.map (fun member' -> {
                KeySuffix = $"{group.Id}:{member'.SetId}"
                Handle = ConnectorHandles.memberPropertyAnchor side group.Id member'.SetId
            })
        else
            [
                {
                    KeySuffix = group.Id
                    Handle = ConnectorHandles.propertyAnchor side group.Id
                }
            ]

    /// Dashed rail connectors derived from model data only: collapsed properties
    /// draw one line per same-side group containing any value for that property.
    let private railConnectionsForSide
        context
        layerId
        (model: ProvenanceModel)
        inputGroups
        outputGroups
        connections
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

            groups
            |> List.collect (
                railConnectionTargets
                    model
                    inputGroups
                    outputGroups
                    connections
                    (fun propertyValue -> propertyValue.Header = header)
                    side
                    overlayState
            )
            |> List.choose (fun target ->
                ConnectorMeasure.pathBetweenDistantHandles
                    context
                    (ConnectorHandles.propertyHeader side header)
                    target.Handle
                |> Option.map (
                    measured
                        $"property:{side}:{DragDrop.propertyHeaderIdentity header}:{target.KeySuffix}"
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
        inputGroups
        outputGroups
        connections
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
                groups
                |> List.collect (
                    railConnectionTargets
                        model
                        inputGroups
                        outputGroups
                        connections
                        (propertyValueMatches header propertyValue.Value propertyValue.Unit)
                        side
                        overlayState
                )
                |> List.choose (fun target ->
                    ConnectorMeasure.pathBetweenDistantHandles
                        context
                        (ConnectorHandles.propertyValue side propertyValue.Id)
                        target.Handle
                    |> Option.map (
                        measured
                            $"value:{side}:{DragDrop.propertyHeaderIdentity header}:{Formatting.formatValue propertyValue.Value propertyValue.Unit}:{target.KeySuffix}"
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
        connections
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
                        inputGroups
                        outputGroups
                        connections
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
                        inputGroups
                        outputGroups
                        connections
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
                    inputGroups
                    outputGroups
                    connections
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
                    inputGroups
                    outputGroups
                    connections
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
                    connections
                    inputRailProjection
                    outputRailProjection
                    colorByHeader
                    overlayState
                    showPropertyHeaderConnectors
            yield! groupConnections context inputGroups outputGroups connections overlayState
            yield! memberConnections context model inputGroups outputGroups connections overlayState
        ]
