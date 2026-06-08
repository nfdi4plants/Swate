module Swate.Components.Composite.ProvenanceGrouping.Helper

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Composite.ProvenanceGrouping.Types
open Swate.Components.Composite.ProvenanceGrouping.State

let formatValue value unit' = valueText value unit'

let dragIndicatorClasses isDragging =
    [
        "swt:transition swt:duration-150"
        if isDragging then "swt:ring-2 swt:ring-primary swt:border-primary swt:bg-primary/10 swt:shadow-md swt:opacity-80"
    ]

let draggableButtonClasses isDragging =
    [
        "swt:cursor-grab swt:active:cursor-grabbing"
        yield! dragIndicatorClasses isDragging
    ]

let draggableBoxClasses isDragging =
    [
        "swt:rounded-md swt:border swt:border-base-300 swt:bg-base-100 swt:shadow-sm"
        yield! draggableButtonClasses isDragging
    ]

let propertyValueButtonClasses isDragging =
    [
        "swt:btn swt:btn-sm swt:btn-primary swt:w-fit swt:max-w-full swt:min-h-8 swt:h-auto swt:justify-start swt:normal-case swt:px-3 swt:py-1.5 swt:text-xs swt:font-medium"
        yield! draggableButtonClasses isDragging
    ]

let propertyValueOverlayClasses =
    [
        "swt:btn swt:btn-sm swt:btn-primary swt:w-fit swt:max-w-[18rem] swt:min-h-8 swt:h-auto swt:justify-start swt:normal-case swt:px-3 swt:py-1.5 swt:text-xs swt:font-medium swt:pointer-events-none swt:shadow-lg swt:ring-2 swt:ring-primary swt:ring-offset-2 swt:ring-offset-base-100"
    ]

let addPropertyValueButtonClasses =
    [
        "swt:btn swt:btn-sm swt:btn-outline swt:btn-primary swt:w-fit swt:max-w-full swt:min-h-8 swt:h-auto swt:justify-start swt:normal-case swt:px-3 swt:py-1.5 swt:text-xs swt:font-medium"
    ]

let private setsForSide side (model: ProvenanceModel) =
    if side = ProvenanceSide.Input then model.InputSets else model.OutputSets

let private headersFromSets (propertyValueIds: ProvenanceSet -> ProvenancePropertyValueId list) side (model: ProvenanceModel) =
    setsForSide side model
    |> Map.toList
    |> List.collect (fun (_, set) ->
        propertyValueIds set
        |> List.choose (fun id -> model.PropertyValues.TryFind id)
        |> List.map (fun value -> value.Header))
    |> List.distinct
    |> List.sortBy (fun header -> header.Category.Name)

let headersForSide side (model: ProvenanceModel) =
    headersFromSets ProvenanceSet.effectivePropertyValueIds side model

let ownedHeadersForSide side (model: ProvenanceModel) =
    headersFromSets (fun set -> set.PropertyValueIds) side model

let headersForModel (model: ProvenanceModel) =
    [ yield! headersForSide ProvenanceSide.Input model
      yield! headersForSide ProvenanceSide.Output model ]
    |> List.distinct
    |> List.sortBy (fun header -> header.Category.Name)

let hasHeaderForSide side header (model: ProvenanceModel) =
    headersForSide side model |> List.contains header

let canSwitchHeader header (model: ProvenanceModel) =
    hasHeaderForSide ProvenanceSide.Input header model
    && hasHeaderForSide ProvenanceSide.Output header model

let private placedHeadersForSide pairId side (uiState: UiState) =
    uiState.PropertyRailPlacements
    |> Map.toList
    |> List.choose (fun ((currentPairId, key), targetSide) ->
        if currentPairId = pairId && targetSide = side then Some key.Header else None)

let private railPlacement pairId header (uiState: UiState) =
    uiState.PropertyRailPlacements |> Map.tryFind (pairId, { Header = header })

let propertyRailHeadersForSide pairId side model uiState =
    let ownHeaders = ownedHeadersForSide side model
    let paletteHeaders = paletteHeadersForSide pairId side uiState
    let knownHeaders =
        [ yield! headersForModel model
          yield! paletteHeaders ]
        |> List.distinct

    [ yield! ownHeaders
      yield! placedHeadersForSide pairId side uiState
      yield! paletteHeaders ]
    |> List.distinct
    |> List.filter (fun header -> knownHeaders |> List.contains header)
    |> List.filter (fun header ->
        match railPlacement pairId header uiState with
        | Some targetSide when hasHeaderForSide targetSide header model -> targetSide = side
        | Some _ -> ownHeaders |> List.contains header
        | None -> true)
    |> List.sortBy (fun header -> header.Category.Name)

let propertyValuesForSideHeader pairId side header (model: ProvenanceModel) uiState =
    let modelValues =
        setsForSide side model
        |> Map.toList
        |> List.collect (fun (_, set) -> ProvenanceSet.effectivePropertyValueIds set)
        |> List.distinct
        |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)
        |> List.filter (fun propertyValue -> propertyValue.Header = header)

    [ yield! modelValues
      yield! paletteValuesForHeader pairId side header uiState ]
    |> List.groupBy (fun propertyValue -> propertyValue.Value, propertyValue.Unit)
    |> List.map (fun (_, values) -> values |> List.sortBy (fun value -> value.Id) |> List.head)
    |> List.sortBy (fun propertyValue -> formatValue propertyValue.Value propertyValue.Unit)

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
    let assignments =
        [ yield! leftState.GroupingAssignments
          yield! rightState.GroupingAssignments ]
        |> List.distinct

    let inputs = displayGroupsForAssignments pair.Model ProvenanceSide.Input assignments
    let outputs = displayGroupsForAssignments pair.Model ProvenanceSide.Output assignments
    pair, inputs, outputs, displayConnections pair.Model inputs outputs

let setsInGroups pairId (groups: DisplayGroup list) selectedIds =
    groups
    |> List.filter (fun (group: DisplayGroup) -> selectedIds |> Set.contains (pairId, group.Id))
    |> List.collect (fun (group: DisplayGroup) -> group.Members |> List.map (fun (member': DisplayMember) -> member'.SetId))
    |> List.distinct

let layerCommand pairId inputGroups outputGroups uiState =
    let inputs =
        setsInGroups pairId inputGroups uiState.SelectedInputs
        |> List.map (fun id -> ProvenanceSide.Input, id)
    let outputs =
        setsInGroups pairId outputGroups uiState.SelectedOutputs
        |> List.map (fun id -> ProvenanceSide.Output, id)
    { AddLayerCommand.SelectedSets = inputs @ outputs }

let private encode (value: string) = System.Uri.EscapeDataString value
let private decode (value: string) = System.Uri.UnescapeDataString value

let private termIdentity (term: ProvenanceTerm) =
    let source = term.TermSource |> Option.defaultValue ""
    let accession = term.TermAccession |> Option.defaultValue ""
    $"{encode term.Name}|{encode source}|{encode accession}"

let propertyHeaderIdentity (header: ProvenancePropertyHeader) =
    let kind =
        match header.Kind with
        | ProvenancePropertyKind.Characteristic -> "Characteristic"
        | ProvenancePropertyKind.Factor -> "Factor"
        | ProvenancePropertyKind.Parameter -> "Parameter"
        | ProvenancePropertyKind.Component -> "Component"
    $"{kind}:{termIdentity header.Category}"

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
let propertyDragId side header = $"provenance-property|{side}|{encode (propertyHeaderIdentity header)}"
let propertyRailDropId side = $"provenance-property-drop|{side}"
let groupDragId side groupId = $"provenance-group|{side}|{encode groupId}"
let groupDropId side groupId = $"provenance-drop|{side}|{encode groupId}"
let groupNodeId side groupId = $"provenance-node::{side}::{encode groupId}"

type DragPayload =
    | PropertyValue of ProvenancePropertyValueId
    | PropertyHeader of ProvenanceSide * string
    | Group of ProvenanceSide * string

let tryDragId (id: string) =
    match id.Split('|') with
    | [| "provenance-value"; valueId |] -> Some(PropertyValue(decode valueId))
    | [| "provenance-property"; "Input"; headerId |] -> Some(PropertyHeader(ProvenanceSide.Input, decode headerId))
    | [| "provenance-property"; "Output"; headerId |] -> Some(PropertyHeader(ProvenanceSide.Output, decode headerId))
    | [| "provenance-group"; "Input"; groupId |] -> Some(Group(ProvenanceSide.Input, decode groupId))
    | [| "provenance-group"; "Output"; groupId |] -> Some(Group(ProvenanceSide.Output, decode groupId))
    | _ -> None

let tryDropId (id: string) =
    match id.Split('|') with
    | [| "provenance-drop"; "Input"; groupId |] -> Some(ProvenanceSide.Input, decode groupId)
    | [| "provenance-drop"; "Output"; groupId |] -> Some(ProvenanceSide.Output, decode groupId)
    | _ -> None

let tryPropertyDropId (id: string) =
    match id.Split('|') with
    | [| "provenance-property-drop"; "Input" |] -> Some ProvenanceSide.Input
    | [| "provenance-property-drop"; "Output" |] -> Some ProvenanceSide.Output
    | _ -> None

let targetForGroup side (group: DisplayGroup) =
    let ids = group.Members |> List.map (fun m -> m.SetId)
    match side with
    | ProvenanceSide.Input -> ProvenancePropertyTarget.InputSets ids
    | ProvenanceSide.Output -> ProvenancePropertyTarget.OutputSets ids

let private memberValuesForHeader header (model: ProvenanceModel) (member': DisplayMember) =
    member'.PropertyValueIds
    |> List.distinct
    |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)
    |> List.filter (fun propertyValue -> propertyValue.Header = header)

let planPropertyValueDrop
    (source: ValueAssignmentSource)
    (group: DisplayGroup)
    (model: ProvenanceModel)
    : Result<ValueAssignmentPlan, ValueAssignmentError> =
    let memberValues =
        group.Members
        |> List.map (fun member' -> member'.SetId, memberValuesForHeader source.Header model member')

    if memberValues.IsEmpty then
        Error ValueAssignmentError.EmptyTarget
    else
        let membersWithMultipleValues =
            memberValues
            |> List.choose (fun (setId, values) ->
                if values.Length > 1 then Some setId else None)

        if not membersWithMultipleValues.IsEmpty then
            Error(ValueAssignmentError.MultiplePropertyValues(source.Header, membersWithMultipleValues))
        elif memberValues |> List.forall (fun (_, values) -> values.IsEmpty) then
            Ok(
                AddCurrent
                    {
                        Target = targetForGroup group.Side group
                        CopiedFrom = source.CopiedFrom
                        Header = source.Header
                        Value = source.Value
                        Unit = source.Unit
                    })
        elif memberValues |> List.forall (fun (_, values) -> values.Length = 1) then
            Ok(
                ConfirmOverwrite
                    {
                        Target = targetForGroup group.Side group
                        ExistingValueIds =
                            memberValues
                            |> List.collect (fun (_, values) -> values |> List.map (fun value -> value.Id))
                            |> List.distinct
                        Header = source.Header
                        Value = source.Value
                        Unit = source.Unit
                    })
        else
            Error(ValueAssignmentError.MixedPropertyValueCounts source.Header)

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
