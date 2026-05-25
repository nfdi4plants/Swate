module Swate.Components.Composite.ProvenanceGrouping.Types

open Fable.Core
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Shared.ProvenanceGrouping.Fixtures

type ProvenanceEditorChange =
    {
        Session: ProvenanceSession
        Patches: ProvenanceTablePatch list
    }

type LayerViewState =
    {
        GroupingKeys: GroupingKey list
    }

type ProvenanceDetail =
    | Group of side: ProvenanceSide * groupId: string
    | Connection of connectionId: string

type UiState =
    {
        LayerStates: Map<ProvenanceLayerId, LayerViewState>
        SelectedInputs: Set<string>
        SelectedOutputs: Set<string>
        Detail: ProvenanceDetail option
        Error: string option
    }

[<Mangle(false)>]
module Exports =
    let createSampleSession () = sampleSession ()
    let createInputOnlySession () = inputOnlyModel () |> Session.init
    let createOutputOnlySession () = outputOnlyModel () |> Session.init
    let createTypedSampleSession () = typedSampleModel () |> Session.init
    let createDataOutputOnlySession () = dataOutputOnlyModel () |> Session.init

    let private valueKind value =
        match value with
        | ProvenanceValue.Text _ -> "Text"
        | ProvenanceValue.Integer _ -> "Integer"
        | ProvenanceValue.Float _ -> "Float"
        | ProvenanceValue.Term _ -> "Term"

    let private unitName (unit': ProvenanceTerm option) =
        unit' |> Option.map (fun term -> term.Name) |> Option.defaultValue "none"

    let patchDetails patches =
        patches
        |> List.map (function
            | ProvenanceTablePatch.UpdatePropertyValue(_, _, _, value, unit') ->
                $"UpdatePropertyValue:{valueKind value}:{unitName unit'}"
            | ProvenanceTablePatch.AddLoadedSet(_, _, header, _) ->
                match header.Kind with
                | ProvenanceIOKind.FreeText text -> $"AddLoadedSet:FreeText:{text}"
                | kind -> $"AddLoadedSet:{kind}"
            | ProvenanceTablePatch.AddLoadedPropertyValue(_, _, _, value, unit') ->
                $"AddLoadedPropertyValue:{valueKind value}:{unitName unit'}"
            | ProvenanceTablePatch.AddLoadedConnection _ -> "AddLoadedConnection")
        |> ResizeArray
