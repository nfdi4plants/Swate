module Swate.Components.Composite.ProvenanceGrouping.State

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Composite.ProvenanceGrouping.Types

/// Shared key helpers used by the UI state maps and sets.
module Keys =

    let groupingKey header : GroupingKey =
        { Header = header }

    let propertySlot pairId side header =
        pairId, side, groupingKey header

    let paletteKey pairId side = pairId, side

    let selectedGroup pairId groupId = pairId, groupId

/// Stores and clamps the three-panel editor layout ratios.
module PanelLayout =

    let minimumSide = 15
    let minimumMiddle = 30

    let defaultRatios =
        {
            Left = 20
            Middle = 60
            Right = 20
        }

    let private clamp left right =
        let left =
            left
            |> max minimumSide
            |> min (100 - minimumMiddle - minimumSide)

        let right =
            right
            |> max minimumSide
            |> min (100 - minimumMiddle - left)

        {
            Left = left
            Middle = 100 - left - right
            Right = right
        }

    let get pairId state =
        state.PanelRatios
        |> Map.tryFind pairId
        |> Option.defaultValue defaultRatios

    let set pairId ratios state =
        { state with PanelRatios = state.PanelRatios |> Map.add pairId (clamp ratios.Left ratios.Right) }

    let setLeft pairId left state =
        let current = get pairId state
        set pairId { current with Left = left } state

    let setRight pairId right state =
        let current = get pairId state
        set pairId { current with Right = right } state

/// Tracks the in-app prompt and expansion state for mismatched group connections.
module MemberResolution =

    let request (pending: PendingMemberResolution) state =
        { state with PendingMemberResolution = Some pending; Error = None }

    let clearPending state =
        { state with PendingMemberResolution = None }

    let chooseManual (pending: PendingMemberResolution) state =
        let pair: ManualResolutionPair =
            {
                PairId = pending.PairId
                InputGroupId = pending.InputGroupId
                OutputGroupId = pending.OutputGroupId
            }

        let pairs =
            if state.ManualResolutionPairs |> List.exists ((=) pair) then
                state.ManualResolutionPairs
            else
                state.ManualResolutionPairs @ [ pair ]

        {
            state with
                PendingMemberResolution = None
                ManualResolutionPairs = pairs
                Detail = Some(ProvenanceDetail.Group(ProvenanceSide.Input, pending.InputGroupId))
        }

    let isManualPair pairId inputGroupId outputGroupId state =
        state.ManualResolutionPairs
        |> List.exists (fun (pair: ManualResolutionPair) ->
            pair.PairId = pairId
            && pair.InputGroupId = inputGroupId
            && pair.OutputGroupId = outputGroupId)

/// Creates, finds, and synchronizes layer-local state with the current session.
module Layers =

    let empty =
        {
            GroupingAssignments = []
        }

    let get layerId state =
        state.LayerStates |> Map.tryFind layerId |> Option.defaultValue empty

    let private retainCurrentLayers session state =
        let currentIds =
            session.Layers
            |> List.map (fun layer -> layer.Id)
            |> Set.ofList

        let retained : Map<ProvenanceLayerId, LayerViewState> =
            state.LayerStates
            |> Map.filter (fun id _ -> currentIds.Contains id)

        session.Layers
        |> List.fold
            (fun (map: Map<ProvenanceLayerId, LayerViewState>) layer ->
                if map.ContainsKey layer.Id then map else map |> Map.add layer.Id empty)
            retained

    let ensure session state =
        let currentPairIds = session.PairOrder |> Set.ofList
        let layerStates = retainCurrentLayers session state

        let propertyRailPlacements =
            state.PropertyRailPlacements
            |> Map.filter (fun (pairId, _) _ -> currentPairIds.Contains pairId)

        let panelRatios =
            state.PanelRatios
            |> Map.filter (fun pairId _ -> currentPairIds.Contains pairId)

        let expandedProperties =
            state.ExpandedProperties
            |> Set.filter (fun (pairId, _, _) -> currentPairIds.Contains pairId)

        let paletteValues =
            state.PaletteValues
            |> Map.filter (fun (pairId, _) _ -> currentPairIds.Contains pairId)

        let pendingMemberResolution =
            state.PendingMemberResolution
            |> Option.filter (fun pending -> currentPairIds.Contains pending.PairId)

        let manualResolutionPairs =
            state.ManualResolutionPairs
            |> List.filter (fun pair -> currentPairIds.Contains pair.PairId)

        if layerStates = state.LayerStates
           && propertyRailPlacements = state.PropertyRailPlacements
           && panelRatios = state.PanelRatios
           && expandedProperties = state.ExpandedProperties
           && paletteValues = state.PaletteValues
           && pendingMemberResolution = state.PendingMemberResolution
           && manualResolutionPairs = state.ManualResolutionPairs then
            state
        else
            {
                state with
                    LayerStates = layerStates
                    PropertyRailPlacements = propertyRailPlacements
                    PanelRatios = panelRatios
                    ExpandedProperties = expandedProperties
                    PaletteValues = paletteValues
                    PendingMemberResolution = pendingMemberResolution
                    ManualResolutionPairs = manualResolutionPairs
            }

    let update layerId updateLayer state =
        let current = get layerId state
        let next = updateLayer current
        { state with LayerStates = state.LayerStates |> Map.add layerId next }

/// Tracks expanded property value panels on the side rails.
module PropertyExpansion =

    let isExpanded pairId side header state =
        state.ExpandedProperties |> Set.contains (Keys.propertySlot pairId side header)

    let toggle pairId side header state =
        let slot = Keys.propertySlot pairId side header
        let expanded =
            if state.ExpandedProperties.Contains slot then
                state.ExpandedProperties.Remove slot
            else
                state.ExpandedProperties.Add slot

        { state with ExpandedProperties = expanded }

/// Stores property values that exist only in the rail until they are applied to a group.
module Palette =

    let valuesForSide pairId side state =
        state.PaletteValues
        |> Map.tryFind (Keys.paletteKey pairId side)
        |> Option.defaultValue []

    let valuesForHeader pairId side header state =
        valuesForSide pairId side state
        |> List.filter (fun propertyValue -> propertyValue.Header = header)

    let headersForSide pairId side state =
        valuesForSide pairId side state
        |> List.map (fun propertyValue -> propertyValue.Header)
        |> List.distinct
        |> List.sortBy (fun header -> header.Category.Name)

    let tryFindValue propertyValueId state =
        state.PaletteValues
        |> Map.toList
        |> List.collect snd
        |> List.tryFind (fun propertyValue -> propertyValue.Id = propertyValueId)

    let private nextValueId pairId side state =
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

    let addValue pairId side header value unit state =
        let key = Keys.paletteKey pairId side
        let propertyValue : ProvenancePropertyValue =
            {
                Id = nextValueId pairId side state
                Header = header
                Value = value
                Unit = unit
                Source = None
            }
        let nextValues = valuesForSide pairId side state @ [ propertyValue ]

        {
            state with
                PaletteValues = state.PaletteValues |> Map.add key nextValues
                ExpandedProperties = state.ExpandedProperties |> Set.add (Keys.propertySlot pairId side header)
                Error = None
        }

/// Manages overwrite confirmation state for dropped property values.
module Overwrite =

    let set warning state =
        { state with PendingOverwrite = Some warning; Error = None }

    let clear state =
        { state with PendingOverwrite = None }

/// Updates grouping assignments and side placement for properties.
module GroupingAssignments =

    let private removeHeader header (assignments: GroupingAssignment list) : GroupingAssignment list =
        let key = Keys.groupingKey header
        assignments |> List.filter (fun assignment -> assignment.Key <> key)

    let private removeHeaderScope header scope (assignments: GroupingAssignment list) : GroupingAssignment list =
        let key = Keys.groupingKey header
        assignments |> List.filter (fun assignment -> assignment.Key <> key || assignment.Scope <> scope)

    let private upsert (assignment: GroupingAssignment) (assignments: GroupingAssignment list) : GroupingAssignment list =
        assignments
        |> List.filter (fun current -> current.Key <> assignment.Key)
        |> fun retained -> retained @ [ assignment ]

    let toggleSide layerId side header state =
        Layers.update
            layerId
            (fun current ->
                let key = Keys.groupingKey header
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

    let toggleBoth leftLayerId rightLayerId header state =
        let key = Keys.groupingKey header
        let isSelected =
            [ leftLayerId; rightLayerId ]
            |> List.exists (fun layerId ->
                (Layers.get layerId state).GroupingAssignments
                |> List.exists (fun assignment -> assignment.Key = key && assignment.Scope = GroupingScope.Both))

        let setLayer state layerId =
            Layers.update
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

    let move pairId sourceLayerId targetLayerId targetSide header state =
        let key = Keys.groupingKey header
        let targetAssignment : GroupingAssignment =
            {
                Key = key
                Scope = scopeForSide targetSide
            }

        let withoutSource =
            Layers.update
                sourceLayerId
                (fun current -> { current with GroupingAssignments = removeHeader header current.GroupingAssignments })
                state

        let withTarget =
            Layers.update
                targetLayerId
                (fun current -> { current with GroupingAssignments = upsert targetAssignment current.GroupingAssignments })
                withoutSource

        {
            withTarget with
                PropertyRailPlacements = withTarget.PropertyRailPlacements |> Map.add (pairId, key) targetSide
        }

/// Tracks selected input/output groups for layer creation.
module Selection =

    let contains pairId side groupId state =
        let identity = Keys.selectedGroup pairId groupId
        match side with
        | ProvenanceSide.Input -> state.SelectedInputs.Contains identity
        | ProvenanceSide.Output -> state.SelectedOutputs.Contains identity

    let toggle pairId side groupId state =
        let identity = Keys.selectedGroup pairId groupId
        match side with
        | ProvenanceSide.Input ->
            let selected =
                if state.SelectedInputs.Contains identity then state.SelectedInputs.Remove identity else state.SelectedInputs.Add identity
            { state with SelectedInputs = selected }
        | ProvenanceSide.Output ->
            let selected =
                if state.SelectedOutputs.Contains identity then state.SelectedOutputs.Remove identity else state.SelectedOutputs.Add identity
            { state with SelectedOutputs = selected }

/// Tracks the details panel target shown beneath the editor surface.
module Detail =

    let isGroupExpanded side groupId state =
        state.Detail = Some(ProvenanceDetail.Group(side, groupId))

    let toggleGroup side groupId state =
        let next =
            if isGroupExpanded side groupId state then None
            else Some(ProvenanceDetail.Group(side, groupId))
        { state with Detail = next }

    let showConnection connectionId state =
        { state with Detail = Some(ProvenanceDetail.Connection connectionId) }

/// Creates the complete UI state for a provenance session.
let init (session: ProvenanceSession) =
    {
        LayerStates = session.Layers |> List.map (fun layer -> layer.Id, Layers.empty) |> Map.ofList
        PropertyRailPlacements = Map.empty
        ExpandedProperties = Set.empty
        PaletteValues = Map.empty
        PendingOverwrite = None
        PanelRatios = Map.empty
        PendingMemberResolution = None
        ManualResolutionPairs = []
        SelectedInputs = Set.empty
        SelectedOutputs = Set.empty
        Detail = None
        Error = None
    }
