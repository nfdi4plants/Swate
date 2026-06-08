module Swate.Components.Composite.ProvenanceGrouping.State

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Composite.ProvenanceGrouping.Types

let emptyLayer =
    {
        GroupingAssignments = []
    }

let init (session: ProvenanceSession) =
    {
        LayerStates = session.Layers |> List.map (fun layer -> layer.Id, emptyLayer) |> Map.ofList
        PropertyRailPlacements = Map.empty
        ExpandedProperties = Set.empty
        PaletteValues = Map.empty
        PendingOverwrite = None
        SelectedInputs = Set.empty
        SelectedOutputs = Set.empty
        Detail = None
        Error = None
    }

let layerState layerId state =
    state.LayerStates |> Map.tryFind layerId |> Option.defaultValue emptyLayer

let ensureLayers session state =
    let currentIds =
        session.Layers
        |> List.map (fun layer -> layer.Id)
        |> Set.ofList
    let currentPairIds = session.PairOrder |> Set.ofList

    let layers =
        let retained : Map<ProvenanceLayerId, LayerViewState> =
            state.LayerStates
            |> Map.filter (fun id _ -> currentIds.Contains id)
        session.Layers
        |> List.fold (fun (map: Map<ProvenanceLayerId, LayerViewState>) layer ->
            if map.ContainsKey layer.Id then map else map |> Map.add layer.Id emptyLayer) retained

    let placements =
        state.PropertyRailPlacements
        |> Map.filter (fun (pairId, _) _ -> currentPairIds.Contains pairId)

    let expandedProperties =
        state.ExpandedProperties
        |> Set.filter (fun (pairId, _, _) -> currentPairIds.Contains pairId)

    let paletteValues =
        state.PaletteValues
        |> Map.filter (fun (pairId, _) _ -> currentPairIds.Contains pairId)

    {
        state with
            LayerStates = layers
            PropertyRailPlacements = placements
            ExpandedProperties = expandedProperties
            PaletteValues = paletteValues
    }

let private groupingKey header : GroupingKey =
    { Header = header }

let private propertySlot pairId side header =
    pairId, side, groupingKey header

let isPropertyExpanded pairId side header state =
    state.ExpandedProperties |> Set.contains (propertySlot pairId side header)

let togglePropertyExpanded pairId side header state =
    let slot = propertySlot pairId side header
    let expanded =
        if state.ExpandedProperties.Contains slot then
            state.ExpandedProperties.Remove slot
        else
            state.ExpandedProperties.Add slot

    { state with ExpandedProperties = expanded }

let private paletteKey pairId side = pairId, side

let paletteValuesForSide pairId side state =
    state.PaletteValues
    |> Map.tryFind (paletteKey pairId side)
    |> Option.defaultValue []

let paletteValuesForHeader pairId side header state =
    paletteValuesForSide pairId side state
    |> List.filter (fun propertyValue -> propertyValue.Header = header)

let paletteHeadersForSide pairId side state =
    paletteValuesForSide pairId side state
    |> List.map (fun propertyValue -> propertyValue.Header)
    |> List.distinct
    |> List.sortBy (fun header -> header.Category.Name)

let tryFindPaletteValue propertyValueId state =
    state.PaletteValues
    |> Map.toList
    |> List.collect snd
    |> List.tryFind (fun propertyValue -> propertyValue.Id = propertyValueId)

let private nextPaletteValueId pairId side state =
    let sideText =
        match side with
        | ProvenanceSide.Input -> "input"
        | ProvenanceSide.Output -> "output"

    let existing =
        state.PaletteValues
        |> Map.toList
        |> List.collect snd
        |> List.map (fun propertyValue -> propertyValue.Id)
        |> Set.ofList

    let rec loop index =
        let id = $"palette-{pairId}-{sideText}-{index}"
        if existing.Contains id then loop (index + 1) else id

    loop (existing.Count + 1)

let addPaletteValue pairId side header value unit state =
    let key = paletteKey pairId side
    let propertyValue : ProvenancePropertyValue =
        {
            Id = nextPaletteValueId pairId side state
            Header = header
            Value = value
            Unit = unit
            Source = None
        }
    let nextValues = paletteValuesForSide pairId side state @ [ propertyValue ]

    {
        state with
            PaletteValues = state.PaletteValues |> Map.add key nextValues
            ExpandedProperties = state.ExpandedProperties |> Set.add (propertySlot pairId side header)
            Error = None
    }

let setPendingOverwrite warning state =
    { state with PendingOverwrite = Some warning; Error = None }

let clearPendingOverwrite state =
    { state with PendingOverwrite = None }

let private removeHeader header (assignments: GroupingAssignment list) : GroupingAssignment list =
    let key = groupingKey header
    assignments |> List.filter (fun assignment -> assignment.Key <> key)

let private removeHeaderScope header scope (assignments: GroupingAssignment list) : GroupingAssignment list =
    let key = groupingKey header
    assignments |> List.filter (fun assignment -> assignment.Key <> key || assignment.Scope <> scope)

let private upsert (assignment: GroupingAssignment) (assignments: GroupingAssignment list) : GroupingAssignment list =
    assignments
    |> List.filter (fun current -> current.Key <> assignment.Key)
    |> fun retained -> retained @ [ assignment ]

let private updateLayer layerId update state =
    let current = layerState layerId state
    let next = update current
    { state with LayerStates = state.LayerStates |> Map.add layerId next }

let toggleSideGrouping layerId side header state =
    updateLayer
        layerId
        (fun current ->
            let key = groupingKey header
            let scope = scopeForSide side
            let assignment : GroupingAssignment = { Key = key; Scope = scope }
            let isSelected =
                current.GroupingAssignments
                |> List.exists (fun current -> current.Key = key && current.Scope = scope)

            let nextAssignments =
                if isSelected then
                    removeHeaderScope header scope current.GroupingAssignments
                else
                    upsert assignment current.GroupingAssignments

            { current with GroupingAssignments = nextAssignments })
        state

let toggleBothGrouping leftLayerId rightLayerId header state =
    let key = groupingKey header
    let isSelected =
        [ leftLayerId; rightLayerId ]
        |> List.exists (fun layerId ->
            (layerState layerId state).GroupingAssignments
            |> List.exists (fun assignment -> assignment.Key = key && assignment.Scope = GroupingScope.Both))

    let setLayer state layerId =
        updateLayer
            layerId
            (fun current ->
                let nextAssignments =
                    if isSelected then
                        removeHeaderScope header GroupingScope.Both current.GroupingAssignments
                    else
                        upsert ({ Key = key; Scope = GroupingScope.Both } : GroupingAssignment) current.GroupingAssignments

                { current with GroupingAssignments = nextAssignments })
            state

    let withLeft = setLayer state leftLayerId
    setLayer withLeft rightLayerId

let moveGrouping pairId sourceLayerId targetLayerId targetSide header state =
    let key = groupingKey header
    let targetAssignment : GroupingAssignment =
        {
            Key = key
            Scope = scopeForSide targetSide
        }

    let withoutSource =
        updateLayer
            sourceLayerId
            (fun current -> { current with GroupingAssignments = removeHeader header current.GroupingAssignments })
            state

    let withTarget =
        updateLayer
            targetLayerId
            (fun current -> { current with GroupingAssignments = upsert targetAssignment current.GroupingAssignments })
            withoutSource

    {
        withTarget with
            PropertyRailPlacements = withTarget.PropertyRailPlacements |> Map.add (pairId, key) targetSide
    }

let select pairId side groupId state =
    let identity = pairId, groupId
    match side with
    | ProvenanceSide.Input ->
        let selected = if state.SelectedInputs.Contains identity then state.SelectedInputs.Remove identity else state.SelectedInputs.Add identity
        { state with SelectedInputs = selected }
    | ProvenanceSide.Output ->
        let selected = if state.SelectedOutputs.Contains identity then state.SelectedOutputs.Remove identity else state.SelectedOutputs.Add identity
        { state with SelectedOutputs = selected }
