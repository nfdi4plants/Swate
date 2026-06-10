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
        GroupingAssignments: GroupingAssignment list
    }

type ProvenanceDetail =
    | Group of side: ProvenanceSide * groupId: string
    | Connection of connectionId: string

type ValueAssignmentSource =
    {
        CopiedFrom: ProvenancePropertyValueId option
        Header: ProvenancePropertyHeader
        Value: ProvenanceValue
        Unit: ProvenanceTerm option
    }

type ValueAssignmentWarning =
    {
        Target: ProvenancePropertyTarget
        ExistingValueIds: ProvenancePropertyValueId list
        Header: ProvenancePropertyHeader
        Value: ProvenanceValue
        Unit: ProvenanceTerm option
    }

type ValueAssignmentPlan =
    | AddCurrent of CreateLoadedPropertyValueCommand
    | ConfirmOverwrite of ValueAssignmentWarning

[<RequireQualifiedAccess>]
type ValueAssignmentError =
    | EmptyTarget
    | MixedPropertyValueCounts of ProvenancePropertyHeader
    | MultiplePropertyValues of ProvenancePropertyHeader * ProvenanceSetId list

type PanelRatios =
    {
        Left: int
        Middle: int
        Right: int
    }

[<RequireQualifiedAccess>]
type ConnectionHandleKind =
    | GroupCard
    | GroupMember
    | PropertyHeader
    | PropertyValue
    /// Measurement-only anchor on the property-facing edge of a group card.
    /// Property and value connectors attach here; it is never draggable or droppable.
    | GroupPropertyAnchor

type ConnectionHandleRef =
    {
        Kind: ConnectionHandleKind
        Side: ProvenanceSide
        Id: string
        ParentGroupId: string option
    }

type ConnectionPoint =
    {
        X: float
        Y: float
    }

type LiveConnectionDrag =
    {
        Source: ConnectionHandleRef
        Start: ConnectionPoint
        Current: ConnectionPoint
    }

type PendingMemberResolution =
    {
        PairId: ProvenancePairId
        InputGroupId: string
        OutputGroupId: string
        InputMemberCount: int
        OutputMemberCount: int
    }

type ManualResolutionPair =
    {
        PairId: ProvenancePairId
        InputGroupId: string
        OutputGroupId: string
    }

type UiState =
    {
        LayerStates: Map<ProvenanceLayerId, LayerViewState>
        PropertyRailPlacements: Map<ProvenancePairId * GroupingKey, ProvenanceSide>
        ExpandedProperties: Set<ProvenancePairId * ProvenanceSide * GroupingKey>
        PaletteValues: Map<ProvenancePairId * ProvenanceSide, ProvenancePropertyValue list>
        PendingOverwrite: ValueAssignmentWarning option
        PanelRatios: Map<ProvenancePairId, PanelRatios>
        LiveConnectionDrag: LiveConnectionDrag option
        PendingMemberResolution: PendingMemberResolution option
        ManualResolutionPairs: ManualResolutionPair list
        SelectedInputs: Set<ProvenancePairId * string>
        SelectedOutputs: Set<ProvenancePairId * string>
        Detail: ProvenanceDetail option
        Error: string option
    }

/// Builds demo sessions used by ProvenanceGrouping stories and browser tests.
module StoryFixtures =

    let createSampleSession () = sampleSession ()
    let createInputOnlySession () = inputOnlyModel () |> Session.init
    let createOutputOnlySession () = outputOnlyModel () |> Session.init
    let createSwitchablePropertySession () = switchablePropertyModel () |> Session.init
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

/// Converts emitted table patches into compact strings for Storybook assertions.
module PatchPreview =

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
                $"AddLoadedSet:{header.Kind.Id}:{ProvenanceKind.displayName header.Kind}"
            | ProvenanceTablePatch.AddLoadedPropertyValue(_, _, _, value, unit') ->
                $"AddLoadedPropertyValue:{valueKind value}:{unitName unit'}{valueMetadata value}"
            | ProvenanceTablePatch.AddLoadedConnection _ -> "AddLoadedConnection")
        |> ResizeArray

[<Mangle(false)>]
module Exports =
    let createSampleSession () = StoryFixtures.createSampleSession ()
    let createInputOnlySession () = StoryFixtures.createInputOnlySession ()
    let createOutputOnlySession () = StoryFixtures.createOutputOnlySession ()
    let createSwitchablePropertySession () = StoryFixtures.createSwitchablePropertySession ()
    let createTypedSampleSession () = StoryFixtures.createTypedSampleSession ()
    let createDataOutputOnlySession () = StoryFixtures.createDataOutputOnlySession ()
    let createRetaggedTypedSampleSession () = StoryFixtures.createRetaggedTypedSampleSession ()
    let patchDetails patches = PatchPreview.patchDetails patches
