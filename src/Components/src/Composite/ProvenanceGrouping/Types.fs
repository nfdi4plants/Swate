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
        SelectedInputs: Set<ProvenancePairId * string>
        SelectedOutputs: Set<ProvenancePairId * string>
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
    let createRetaggedTypedSampleSession () =
        let model = typedSampleModel ()
        let propertyValue = model.PropertyValues.["pv-output-a-instrument"]
        let retagged =
            {
                Name = "mass spectrometer"
                TermSource = Some "MS"
                TermAccession = Some "MS:1000031"
            }
        {
            model with
                PropertyValues =
                    model.PropertyValues
                    |> Map.add propertyValue.Id { propertyValue with Value = ProvenanceValue.Term retagged }
        }
        |> Session.init

    let private valueKind value =
        match value with
        | ProvenanceValue.Text _ -> "Text"
        | ProvenanceValue.Integer _ -> "Integer"
        | ProvenanceValue.Float _ -> "Float"
        | ProvenanceValue.Term _ -> "Term"

    let private unitName (unit': ProvenanceTerm option) =
        unit' |> Option.map (fun term -> term.Name) |> Option.defaultValue "none"

    let private valueMetadata value =
        match value with
        | ProvenanceValue.Term term ->
            let source = term.TermSource |> Option.defaultValue "none"
            let accession = term.TermAccession |> Option.defaultValue "none"
            $":{source}:{accession}"
        | _ -> ""

    let patchDetails patches =
        patches
        |> List.map (function
            | ProvenanceTablePatch.UpdatePropertyValue(_, _, _, value, unit') ->
                $"UpdatePropertyValue:{valueKind value}:{unitName unit'}{valueMetadata value}"
            | ProvenanceTablePatch.AddLoadedSet(_, _, header, _) ->
                match header.Kind with
                | ProvenanceIOKind.FreeText text -> $"AddLoadedSet:FreeText:{text}"
                | kind -> $"AddLoadedSet:{kind}"
            | ProvenanceTablePatch.AddLoadedPropertyValue(_, _, _, value, unit') ->
                $"AddLoadedPropertyValue:{valueKind value}:{unitName unit'}{valueMetadata value}"
            | ProvenanceTablePatch.AddLoadedConnection _ -> "AddLoadedConnection")
        |> ResizeArray
