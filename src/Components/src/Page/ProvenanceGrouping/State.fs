module Swate.Components.Page.ProvenanceGrouping.State

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Page.ProvenanceGrouping.Types

/// Shared key helpers used by the UI state maps and sets.
module Keys =

    let groupingKey header : GroupingKey = { Header = header }

    let propertySlot pairId side header = pairId, side, groupingKey header

    let paletteKey pairId side = pairId, side

    let selectedGroup pairId groupId = pairId, groupId

/// Stores and clamps the three-panel editor layout ratios.
module PanelLayout =

    let minimumSide = 15
    let minimumMiddle = 30

    let defaultRatios = { Left = 20; Middle = 60; Right = 20 }

    let private clamp left right =
        let left = left |> max minimumSide |> min (100 - minimumMiddle - minimumSide)

        let right = right |> max minimumSide |> min (100 - minimumMiddle - left)

        {
            Left = left
            Middle = 100 - left - right
            Right = right
        }

    let get pairId state =
        state.PanelRatios |> Map.tryFind pairId |> Option.defaultValue defaultRatios

    let set pairId ratios state = {
        state with
            PanelRatios = state.PanelRatios |> Map.add pairId (clamp ratios.Left ratios.Right)
    }

    let setLeft pairId left state =
        let current = get pairId state
        set pairId { current with Left = left } state

    let setRight pairId right state =
        let current = get pairId state
        set pairId { current with Right = right } state

/// Tracks the in-app prompt and expansion state for mismatched group connections.
module MemberResolution =

    let request (pending: PendingMemberResolution) state = {
        state with
            PendingMemberResolution = Some pending
            Error = None
    }

    let clearPending state = {
        state with
            PendingMemberResolution = None
    }

    let chooseManual (pending: PendingMemberResolution) state =
        let pair: ManualResolutionPair = {
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
            && pair.OutputGroupId = outputGroupId
        )

module PropertyColors =

    let palette = [|
        "#2563eb"
        "#16a34a"
        "#d97706"
        "#dc2626"
        "#7c3aed"
        "#0891b2"
        "#be185d"
    |]

    let empty = {
        ManualPropertyColors = Map.empty
        LayerColors = Map.empty
    }

    let private automaticColorForLayer layerIndex = palette.[layerIndex % palette.Length]

    let setColor (header: ProvenancePropertyHeader) color state =
        let key = Keys.groupingKey header

        {
            state with
                PropertyColors = {
                    state.PropertyColors with
                        ManualPropertyColors = state.PropertyColors.ManualPropertyColors |> Map.add key color
                }
        }

    let clearColor (header: ProvenancePropertyHeader) state =
        let key = Keys.groupingKey header

        {
            state with
                PropertyColors = {
                    state.PropertyColors with
                        ManualPropertyColors = state.PropertyColors.ManualPropertyColors |> Map.remove key
                }
        }

    let setLayerColor (layerId: ProvenanceLayerId) color state = {
        state with
            PropertyColors = {
                state.PropertyColors with
                    LayerColors = state.PropertyColors.LayerColors |> Map.add layerId color
            }
    }

    let clearLayerColor (layerId: ProvenanceLayerId) state = {
        state with
            PropertyColors = {
                state.PropertyColors with
                    LayerColors = state.PropertyColors.LayerColors |> Map.remove layerId
            }
    }

    let ensureLayerColors (session: ProvenanceSession) (state: UiState) : PropertyColorSettings =
        let liveLayerIds = session.LayerOrder |> Set.ofList

        let retained =
            state.PropertyColors.LayerColors
            |> Map.filter (fun layerId _ -> liveLayerIds.Contains layerId)

        let withMissingDefaults =
            session.LayerOrder
            |> List.mapi (fun index layerId -> layerId, automaticColorForLayer index)
            |> List.fold
                (fun (colors: Map<ProvenanceLayerId, ProvenanceColor>) (layerId, color) ->
                    if colors.ContainsKey layerId then
                        colors
                    else
                        colors |> Map.add layerId color
                )
                retained

        {
            state.PropertyColors with
                LayerColors = withMissingDefaults
        }

module Filters =

    let defaultState = {
        SearchText = ""
        PropertySort = PropertySort.ValueCountDesc
        GroupSort = GroupSort.NameAsc
        ValueCountFilter = PropertyValueCountFilter.Any
        OriginFilter = PropertyOriginFilter.AnyOrigin
    }

    let setSearch text state = {
        state with
            Filters = { state.Filters with SearchText = text }
    }

    let setValueCountFilter filter state = {
        state with
            Filters = {
                state.Filters with
                    ValueCountFilter = filter
            }
    }

    let setOriginFilter filter state = {
        state with
            Filters = {
                state.Filters with
                    OriginFilter = filter
            }
    }

    let setPropertySort sort state = {
        state with
            Filters = {
                state.Filters with
                    PropertySort = sort
            }
    }

    let setGroupSort sort state = {
        state with
            Filters = { state.Filters with GroupSort = sort }
    }

/// Creates, finds, and synchronizes side-local state with the current session.
module Sides =

    let empty = { GroupingAssignments = [] }

    let private sideIdsForSession session =
        session.LayerOrder
        |> List.collect (fun layerId ->
            let layer = Session.layerById layerId session
            [ layer.InputSideId; layer.OutputSideId ]
        )

    let get sideId state =
        state.SideStates |> Map.tryFind sideId |> Option.defaultValue empty

    let private retainCurrentSides session state =
        let currentIds = sideIdsForSession session |> Set.ofList

        let retained: Map<ProvenanceLayerSideId, SideViewState> =
            state.SideStates |> Map.filter (fun id _ -> currentIds.Contains id)

        sideIdsForSession session
        |> List.fold
            (fun (map: Map<ProvenanceLayerSideId, SideViewState>) sideId ->
                if map.ContainsKey sideId then
                    map
                else
                    map |> Map.add sideId empty
            )
            retained

    let ensure (session: ProvenanceSession) state =
        let currentPairIds = session.PairOrder |> Set.ofList
        let sideStates = retainCurrentSides session state

        let propertyRailPlacements =
            state.PropertyRailPlacements
            |> Map.filter (fun (pairId, _) _ -> currentPairIds.Contains pairId)

        let panelRatios =
            state.PanelRatios |> Map.filter (fun pairId _ -> currentPairIds.Contains pairId)

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

        let propertyColors = PropertyColors.ensureLayerColors session state

        if
            sideStates = state.SideStates
            && propertyRailPlacements = state.PropertyRailPlacements
            && panelRatios = state.PanelRatios
            && expandedProperties = state.ExpandedProperties
            && paletteValues = state.PaletteValues
            && pendingMemberResolution = state.PendingMemberResolution
            && manualResolutionPairs = state.ManualResolutionPairs
            && propertyColors = state.PropertyColors
        then
            state
        else
            {
                state with
                    SideStates = sideStates
                    PropertyRailPlacements = propertyRailPlacements
                    PanelRatios = panelRatios
                    ExpandedProperties = expandedProperties
                    PaletteValues = paletteValues
                    PendingMemberResolution = pendingMemberResolution
                    ManualResolutionPairs = manualResolutionPairs
                    PropertyColors = propertyColors
            }

    let update sideId updateSide state =
        let current = get sideId state
        let next = updateSide current

        {
            state with
                SideStates = state.SideStates |> Map.add sideId next
        }

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

        {
            state with
                ExpandedProperties = expanded
        }

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

        let propertyValue: ProvenancePropertyValue = {
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

/// Manages batch assignment confirmation state for dropped property values.
module AssignmentBatch =

    let set (batch: PendingAssignmentBatch) state = {
        state with
            PendingAssignmentBatch = Some batch
            Error = None
    }

    let clear state = {
        state with
            PendingAssignmentBatch = None
    }

/// Updates grouping assignments and side placement for properties.
module GroupingAssignments =

    let private removeHeader header (assignments: GroupingAssignment list) : GroupingAssignment list =
        let key = Keys.groupingKey header
        assignments |> List.filter (fun assignment -> assignment.Key <> key)

    let private removeHeaderScope header scope (assignments: GroupingAssignment list) : GroupingAssignment list =
        let key = Keys.groupingKey header

        assignments
        |> List.filter (fun assignment -> assignment.Key <> key || assignment.Scope <> scope)

    let private upsert
        (assignment: GroupingAssignment)
        (assignments: GroupingAssignment list)
        : GroupingAssignment list =
        assignments
        |> List.filter (fun current -> current.Key <> assignment.Key)
        |> fun retained -> retained @ [ assignment ]

    let toggleSide sideId side header state =
        Sides.update
            sideId
            (fun current ->
                let key = Keys.groupingKey header
                let scope = scopeForSide side
                let assignment: GroupingAssignment = { Key = key; Scope = scope }

                let isSelected =
                    current.GroupingAssignments
                    |> List.exists (fun current -> current.Key = key && current.Scope = scope)

                let nextAssignments =
                    if isSelected then
                        removeHeaderScope header scope current.GroupingAssignments
                    else
                        upsert assignment current.GroupingAssignments

                {
                    current with
                        GroupingAssignments = nextAssignments
                }
            )
            state

    let toggleBoth inputSideId outputSideId header state =
        let key = Keys.groupingKey header

        let isSelected =
            [ inputSideId; outputSideId ]
            |> List.exists (fun sideId ->
                (Sides.get sideId state).GroupingAssignments
                |> List.exists (fun assignment -> assignment.Key = key && assignment.Scope = GroupingScope.Both)
            )

        let setSide state sideId =
            Sides.update
                sideId
                (fun current ->
                    let nextAssignments =
                        if isSelected then
                            removeHeaderScope header GroupingScope.Both current.GroupingAssignments
                        else
                            upsert
                                ({
                                    Key = key
                                    Scope = GroupingScope.Both
                                }
                                : GroupingAssignment)
                                current.GroupingAssignments

                    {
                        current with
                            GroupingAssignments = nextAssignments
                    }
                )
                state

        let withInput = setSide state inputSideId
        setSide withInput outputSideId

    let move layerId sourceSideId targetSideId targetSide header state =
        let key = Keys.groupingKey header

        let targetAssignment: GroupingAssignment = {
            Key = key
            Scope = scopeForSide targetSide
        }

        let withoutSource =
            Sides.update
                sourceSideId
                (fun current -> {
                    current with
                        GroupingAssignments = removeHeader header current.GroupingAssignments
                })
                state

        let withTarget =
            Sides.update
                targetSideId
                (fun current -> {
                    current with
                        GroupingAssignments = upsert targetAssignment current.GroupingAssignments
                })
                withoutSource

        {
            withTarget with
                PropertyRailPlacements = withTarget.PropertyRailPlacements |> Map.add (layerId, key) targetSide
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
                if state.SelectedInputs.Contains identity then
                    state.SelectedInputs.Remove identity
                else
                    state.SelectedInputs.Add identity

            { state with SelectedInputs = selected }
        | ProvenanceSide.Output ->
            let selected =
                if state.SelectedOutputs.Contains identity then
                    state.SelectedOutputs.Remove identity
                else
                    state.SelectedOutputs.Add identity

            {
                state with
                    SelectedOutputs = selected
            }

/// Tracks the details panel target shown beneath the editor surface.
module Detail =

    let isGroupExpanded side groupId state =
        state.Detail = Some(ProvenanceDetail.Group(side, groupId))

    let toggleGroup side groupId state =
        let next =
            if isGroupExpanded side groupId state then
                None
            else
                Some(ProvenanceDetail.Group(side, groupId))

        { state with Detail = next }

    let showConnection connectionId state = {
        state with
            Detail = Some(ProvenanceDetail.Connection connectionId)
    }

/// Creates the complete UI state for a provenance session.
let init (session: ProvenanceSession) = {
    SideStates =
        session.LayerOrder
        |> List.collect (fun layerId ->
            let layer = Session.layerById layerId session

            [
                layer.InputSideId, Sides.empty
                layer.OutputSideId, Sides.empty
            ]
        )
        |> Map.ofList
    PropertyRailPlacements = Map.empty
    ExpandedProperties = Set.empty
    PaletteValues = Map.empty
    PendingAssignmentBatch = None
    PanelRatios = Map.empty
    PendingMemberResolution = None
    ManualResolutionPairs = []
    SelectedInputs = Set.empty
    SelectedOutputs = Set.empty
    Detail = None
    Error = None
    PropertyColors = PropertyColors.empty
    Filters = Filters.defaultState
}
