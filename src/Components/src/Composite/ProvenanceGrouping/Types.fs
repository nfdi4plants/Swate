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
        SortHeader: ProvenancePropertyHeader option
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

    let patchLabels patches =
        patches
        |> List.map (function
            | ProvenanceTablePatch.UpdatePropertyValue _ -> "UpdatePropertyValue"
            | ProvenanceTablePatch.AddLoadedSet _ -> "AddLoadedSet"
            | ProvenanceTablePatch.AddLoadedPropertyValue _ -> "AddLoadedPropertyValue"
            | ProvenanceTablePatch.AddLoadedConnection _ -> "AddLoadedConnection")
        |> ResizeArray
