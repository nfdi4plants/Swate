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
        let bend = max 48. (abs deltaX / 2.)
        let firstControlX = start.X + direction * bend
        let secondControlX = finish.X - direction * bend
        Some $"M {start.X} {start.Y} C {firstControlX} {start.Y}, {secondControlX} {finish.Y}, {finish.X} {finish.Y}"

    let pathBetweenHandles container source target =
        match tryHandle container source, tryHandle container target with
        | Some sourceNode, Some targetNode ->
            pathBetweenPoints (center container sourceNode) (center container targetNode)
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

/// Projects model/UI state into concrete connector path definitions.
module private ConnectorPaths =

    let private measured key testId className strokeDasharray interactiveConnection ariaLabel path =
        {
            Key = key
            Path = path
            TestId = testId
            ClassName = className
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
                    "swt:text-primary swt:pointer-events-auto swt:cursor-pointer"
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
                                None
                                None
                                None
                        )))
        )

    let propertyConnections container pairId uiState =
        State.VisualConnections.forPair pairId uiState
        |> List.choose (fun connection ->
            ConnectorMeasure.pathBetweenHandles container connection.Source connection.Target
            |> Option.map (
                measured
                    $"property:{DragDrop.connectionHandleIdentity connection.Source}:{DragDrop.connectionHandleIdentity connection.Target}"
                    "provenance-property-connection"
                    "swt:text-secondary swt:pointer-events-none"
                    (Some "4 4")
                    None
                    None
            ))

    let private expandedKeys pairId side uiState =
        uiState.ExpandedProperties
        |> Set.toList
        |> List.choose (fun (expandedPairId, expandedSide, key) ->
            if expandedPairId = pairId && expandedSide = side then Some key else None)

    let private valueConnectionsForSide container pairId (model: ProvenanceModel) side groups uiState =
        let keys = expandedKeys pairId side uiState

        [
            for key in keys do
                for group in groups do
                    let matchingValueIds =
                        group.Members
                        |> List.collect (fun member' -> member'.PropertyValueIds)
                        |> List.distinct
                        |> List.filter (fun propertyValueId ->
                            model.PropertyValues.TryFind propertyValueId
                            |> Option.exists (fun propertyValue -> propertyValue.Header = key.Header))

                    for propertyValueId in matchingValueIds do
                        ConnectorMeasure.pathBetweenHandles
                            container
                            (ConnectorHandles.value side propertyValueId)
                            (ConnectorHandles.group side group.Id)
                        |> Option.map (
                            measured
                                $"value:{side}:{propertyValueId}:{group.Id}"
                                "provenance-value-connection"
                                "swt:text-accent swt:pointer-events-none"
                                (Some "3 6")
                                None
                                None
                        )
        ]
        |> List.choose id

    let valueConnections container pairId model inputGroups outputGroups uiState =
        [
            yield! valueConnectionsForSide container pairId model ProvenanceSide.Input inputGroups uiState
            yield! valueConnectionsForSide container pairId model ProvenanceSide.Output outputGroups uiState
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
                    (Some "6 4")
                    None
                    None
            ))

    let all container pairId model inputGroups outputGroups connections uiState =
        [
            yield! valueConnections container pairId model inputGroups outputGroups uiState
            yield! propertyConnections container pairId uiState
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
            ?debug: bool
        ) =
        let paths, setPaths = React.useState ([]: MeasuredConnector list)

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
                    ConnectorObserver.observeMatching
                        container
                        "[data-provenance-group-node],[data-provenance-connection-node]"
                        observer

                    FsReact.createDisposable (fun () ->
                        container.removeEventListener ("scroll", onLayout)
                        Browser.Dom.window.removeEventListener ("resize", onLayout)
                        ConnectorObserver.disconnect observer
                    )
            ),
            [| box pairId; box model; box inputGroups; box outputGroups; box connections; box uiState |]
        )

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
                        | _ -> ()

                    Svg.path [
                        svg.key measured.Key
                        svg.d measured.Path
                        svg.fill "none"
                        svg.stroke "currentColor"
                        svg.className measured.ClassName
                        match measured.StrokeDasharray with
                        | Some dash -> svg.custom ("strokeDasharray", dash)
                        | None -> ()
                        if defaultArg debug false then
                            svg.custom ("data-testid", measured.TestId)
                        match measured.InteractiveConnection with
                        | Some connection ->
                            svg.custom ("tabIndex", "0")
                            svg.custom ("role", "button")
                            svg.custom (
                                "aria-label",
                                measured.AriaLabel |> Option.defaultValue $"Select connection {connection.Id}"
                            )
                            svg.onClick (fun _ -> onSelect connection)
                            svg.onKeyDown activateFromKeyboard
                        | None -> ()
                    ]
            ]
        ]
