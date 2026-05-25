module Swate.Components.Composite.ProvenanceGrouping.Helper

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Composite.ProvenanceGrouping.Types
open Swate.Components.Composite.ProvenanceGrouping.State

let formatValue (value: ProvenanceValue) (unit': ProvenanceTerm option) =
    let text =
        match value with
        | ProvenanceValue.Text text -> text
        | ProvenanceValue.Integer value -> string value
        | ProvenanceValue.Float value -> string value
        | ProvenanceValue.Term term -> term.Name
    unit' |> Option.map (fun u -> $"{text} {u.Name}") |> Option.defaultValue text

let headersForSide side (model: ProvenanceModel) =
    let sets = if side = ProvenanceSide.Input then model.InputSets else model.OutputSets
    sets
    |> Map.toList
    |> List.collect (fun (_, set) ->
        set.PropertyValueIds
        |> List.choose (fun id -> model.PropertyValues.TryFind id)
        |> List.map (fun value -> value.Header))
    |> List.distinct
    |> List.sortBy (fun header -> header.Category.Name)

let headersForModel (model: ProvenanceModel) =
    [ yield! headersForSide ProvenanceSide.Input model
      yield! headersForSide ProvenanceSide.Output model ]
    |> List.distinct
    |> List.sortBy (fun header -> header.Category.Name)

let defaultEndpointKind side (model: ProvenanceModel) =
    let oppositeSets =
        match side with
        | ProvenanceSide.Input -> model.OutputSets
        | ProvenanceSide.Output -> model.InputSets

    match oppositeSets |> Map.toList |> List.map (fun (_, set) -> set.Header.Kind) |> List.distinct with
    | [ ProvenanceIOKind.Unknown ]
    | [] -> ProvenanceIOKind.Sample
    | [ kind ] -> kind
    | _ -> ProvenanceIOKind.Sample

let endpointKindIdentity kind =
    match kind with
    | ProvenanceIOKind.Source -> "Source"
    | ProvenanceIOKind.Sample -> "Sample"
    | ProvenanceIOKind.Data -> "Data"
    | ProvenanceIOKind.Material -> "Material"
    | ProvenanceIOKind.FreeText text -> $"FreeText:{text}"
    | ProvenanceIOKind.Unknown -> "Unknown"

let displayPair session uiState =
    let pair = Session.activePair session
    let leftState = layerState pair.LeftLayerId uiState
    let rightState = layerState pair.RightLayerId uiState
    let inputs = displayGroups pair.Model ProvenanceSide.Input leftState.GroupingKeys
    let outputs = displayGroups pair.Model ProvenanceSide.Output rightState.GroupingKeys
    pair, inputs, outputs, displayConnections pair.Model inputs outputs

let setsInGroups (groups: DisplayGroup list) selectedIds =
    groups
    |> List.filter (fun (group: DisplayGroup) -> selectedIds |> Set.contains group.Id)
    |> List.collect (fun (group: DisplayGroup) -> group.Members |> List.map (fun (member': DisplayMember) -> member'.SetId))
    |> List.distinct

let layerCommand inputGroups outputGroups uiState =
    let inputs =
        setsInGroups inputGroups uiState.SelectedInputs
        |> List.map (fun id -> ProvenanceSide.Input, id)
    let outputs =
        setsInGroups outputGroups uiState.SelectedOutputs
        |> List.map (fun id -> ProvenanceSide.Output, id)
    { AddLayerCommand.SelectedSets = inputs @ outputs }

let private encode (value: string) = System.Uri.EscapeDataString value
let private decode (value: string) = System.Uri.UnescapeDataString value

let private termIdentity (term: ProvenanceTerm) =
    let source = term.TermSource |> Option.defaultValue ""
    let accession = term.TermAccession |> Option.defaultValue ""
    $"{encode term.Name}|{encode source}|{encode accession}"

let propertyValueIdentity (propertyValue: ProvenancePropertyValue) =
    let value =
        match propertyValue.Value with
        | ProvenanceValue.Text text -> $"Text:{encode text}"
        | ProvenanceValue.Integer integer -> $"Integer:{integer}"
        | ProvenanceValue.Float float -> $"Float:{float}"
        | ProvenanceValue.Term term -> $"Term:{termIdentity term}"
    let unit =
        propertyValue.Unit
        |> Option.map termIdentity
        |> Option.defaultValue ""
    $"{propertyValue.Id}:{value}:Unit:{unit}"

let valueDragId propertyValueId = $"provenance-value|{encode propertyValueId}"
let groupDragId side groupId = $"provenance-group|{side}|{encode groupId}"
let groupDropId side groupId = $"provenance-drop|{side}|{encode groupId}"
let groupNodeId side groupId = $"provenance-node::{side}::{encode groupId}"

type DragPayload =
    | PropertyValue of ProvenancePropertyValueId
    | Group of ProvenanceSide * string

let tryDragId (id: string) =
    match id.Split('|') with
    | [| "provenance-value"; valueId |] -> Some(PropertyValue(decode valueId))
    | [| "provenance-group"; "Input"; groupId |] -> Some(Group(ProvenanceSide.Input, decode groupId))
    | [| "provenance-group"; "Output"; groupId |] -> Some(Group(ProvenanceSide.Output, decode groupId))
    | _ -> None

let tryDropId (id: string) =
    match id.Split('|') with
    | [| "provenance-drop"; "Input"; groupId |] -> Some(ProvenanceSide.Input, decode groupId)
    | [| "provenance-drop"; "Output"; groupId |] -> Some(ProvenanceSide.Output, decode groupId)
    | _ -> None

let targetForGroup side (group: DisplayGroup) =
    let ids = group.Members |> List.map (fun m -> m.SetId)
    match side with
    | ProvenanceSide.Input -> ProvenancePropertyTarget.InputSets ids
    | ProvenanceSide.Output -> ProvenancePropertyTarget.OutputSets ids

let endpointHeader side kind =
    let prefix = if side = ProvenanceSide.Input then "Input" else "Output"
    let text =
        match kind with
        | ProvenanceIOKind.Source -> $"{prefix} [Source Name]"
        | ProvenanceIOKind.Sample -> $"{prefix} [Sample Name]"
        | ProvenanceIOKind.Data -> $"{prefix} [Data]"
        | ProvenanceIOKind.Material -> $"{prefix} [Material]"
        | ProvenanceIOKind.FreeText text -> text
        | ProvenanceIOKind.Unknown -> $"{prefix} [Unknown]"
    { Kind = kind; Text = text }
