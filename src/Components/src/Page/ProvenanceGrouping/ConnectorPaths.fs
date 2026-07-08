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

/// Projects model/UI state into logical connector specs, and measures specs into
/// concrete SVG paths. Spec derivation is pure and memoizable; only the measure
/// step reads the DOM, so layout observers can remeasure cheaply.
module ConnectorPaths =

    let private spec
        key
        testId
        className
        strokeWidth
        strokeDasharray
        interactiveConnection
        ariaLabel
        color
        skipWhenClose
        source
        target
        : ConnectorSpec =
        {
            Key = key
            TestId = testId
            ClassName = className
            StrokeWidth = strokeWidth
            StrokeDasharray = strokeDasharray
            InteractiveConnection = interactiveConnection
            AriaLabel = ariaLabel
            Color = color
            Source = source
            Target = target
            SkipWhenClose = skipWhenClose
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
        ConnectorOverlayState.followsExpandedNeighbors overlayState
        && isGroupedCard inputGroups outputGroups side groupId
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

    let groupConnectionSpecs
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
        |> List.map (fun connection ->
            spec
                $"connection:{connection.Id}"
                "provenance-connection"
                "swt:text-primary"
                2.25
                None
                (Some connection)
                (Some $"Select connection {connection.Id}")
                None
                false
                (ConnectorHandles.group ProvenanceSide.Input connection.SourceGroupId)
                (ConnectorHandles.group ProvenanceSide.Output connection.TargetGroupId)
        )

    let memberConnectionSpecs
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
                    |> Option.map (fun connection ->
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

                        spec
                            $"member:{displayConnection.Id}:{connectionId}"
                            "provenance-member-connection"
                            "swt:text-primary/70 swt:pointer-events-none"
                            2.0
                            None
                            (Some singleConnection)
                            (Some $"Select connection {displayConnection.Id}")
                            None
                            false
                            source
                            target
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
    let private railConnectionSpecsForSide
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
            |> List.map (fun target ->
                spec
                    $"property:{side}:{DragDrop.propertyHeaderIdentity header}:{target.KeySuffix}"
                    "provenance-property-connection"
                    "swt:text-secondary swt:pointer-events-none"
                    1.75
                    (Some "4 4")
                    None
                    None
                    color
                    true
                    (ConnectorHandles.propertyHeader side header)
                    target.Handle
            )
        )

    let private propertyValueMatches header value unit' (propertyValue: ProvenancePropertyValue) =
        propertyValue.Header = header
        && propertyValue.Value = value
        && propertyValue.Unit = unit'

    let private valueRailConnectionSpecsForSide
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
                |> List.map (fun target ->
                    spec
                        $"value:{side}:{DragDrop.propertyHeaderIdentity header}:{Formatting.formatValue propertyValue.Value propertyValue.Unit}:{target.KeySuffix}"
                        "provenance-value-connection"
                        "swt:text-accent swt:pointer-events-none"
                        2.0
                        (Some "4 4")
                        None
                        None
                        color
                        true
                        (ConnectorHandles.propertyValue side propertyValue.Id)
                        target.Handle
                )
            )
        )

    let railConnectionSpecs
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
                    railConnectionSpecsForSide
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
                    railConnectionSpecsForSide
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
                valueRailConnectionSpecsForSide
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
                valueRailConnectionSpecsForSide
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
            |> Option.map (fun path -> {
                Key = "live"
                Path = path
                TestId = "provenance-live-connection"
                // connector-flow marches the dashes toward the pointer while aiming.
                ClassName = "swt:text-primary swt:pointer-events-none swt:opacity-80 swt:connector-flow"
                StrokeWidth = 2.25
                StrokeDasharray = Some "6 4"
                InteractiveConnection = None
                AriaLabel = None
                Color = None
                Midpoint = None
            })
        )

    /// All logical connectors for the current editor state, in paint order.
    let specs
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
        : ConnectorSpec list =
        [
            yield!
                railConnectionSpecs
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
            yield! groupConnectionSpecs inputGroups outputGroups connections overlayState
            yield! memberConnectionSpecs model inputGroups outputGroups connections overlayState
        ]

    /// Resolves specs against the measured DOM; specs whose handles are missing or
    /// (for rail connectors) too close together are dropped.
    let measure context (specs: ConnectorSpec list) : MeasuredConnector list =
        specs
        |> List.choose (fun spec ->
            let measured =
                if spec.SkipWhenClose then
                    ConnectorMeasure.pathBetweenDistantHandles context spec.Source spec.Target
                    |> Option.map (fun path -> path, None)
                else
                    ConnectorMeasure.pathWithMidpointBetweenHandles context spec.Source spec.Target
                    |> Option.map (fun (path, midpoint) -> path, Some midpoint)

            measured
            |> Option.map (fun (path, midpoint) -> {
                Key = spec.Key
                Path = path
                TestId = spec.TestId
                ClassName = spec.ClassName
                StrokeWidth = spec.StrokeWidth
                StrokeDasharray = spec.StrokeDasharray
                InteractiveConnection = spec.InteractiveConnection
                AriaLabel = spec.AriaLabel
                Color = spec.Color
                Midpoint = midpoint
            })
        )
