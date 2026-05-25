module Swate.Components.Composite.ProvenanceGrouping.State

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Composite.ProvenanceGrouping.Types

let emptyLayer =
    {
        GroupingKeys = []
    }

let init (session: ProvenanceSession) =
    {
        LayerStates = session.Layers |> List.map (fun layer -> layer.Id, emptyLayer) |> Map.ofList
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

    let layers =
        let retained : Map<ProvenanceLayerId, LayerViewState> =
            state.LayerStates
            |> Map.filter (fun id _ -> currentIds.Contains id)
        session.Layers
        |> List.fold (fun (map: Map<ProvenanceLayerId, LayerViewState>) layer ->
            if map.ContainsKey layer.Id then map else map |> Map.add layer.Id emptyLayer) retained

    { state with LayerStates = layers }

let toggleGrouping layerId header state =
    let current = layerState layerId state
    let key = { Header = header }
    let nextKeys =
        if current.GroupingKeys |> List.contains key then
            current.GroupingKeys |> List.filter ((<>) key)
        else
            current.GroupingKeys @ [ key ]
    { state with LayerStates = state.LayerStates |> Map.add layerId { current with GroupingKeys = nextKeys } }

let select pairId side groupId state =
    let identity = pairId, groupId
    match side with
    | ProvenanceSide.Input ->
        let selected = if state.SelectedInputs.Contains identity then state.SelectedInputs.Remove identity else state.SelectedInputs.Add identity
        { state with SelectedInputs = selected }
    | ProvenanceSide.Output ->
        let selected = if state.SelectedOutputs.Contains identity then state.SelectedOutputs.Remove identity else state.SelectedOutputs.Add identity
        { state with SelectedOutputs = selected }
