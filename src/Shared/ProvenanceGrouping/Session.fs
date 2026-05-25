module Swate.Components.Shared.ProvenanceGrouping.Session

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Edit

type ProvenanceLayerId = string
type ProvenancePairId = string

type ProvenanceLayer =
    {
        Id: ProvenanceLayerId
        Label: string
    }

type ProvenanceSetReference =
    {
        PairId: ProvenancePairId
        Side: ProvenanceSide
        SetId: ProvenanceSetId
    }

type ProvenanceBoundaryLink =
    {
        Previous: ProvenanceSetReference
        Next: ProvenanceSetReference
    }

type ProvenanceLayerPair =
    {
        Id: ProvenancePairId
        LeftLayerId: ProvenanceLayerId
        RightLayerId: ProvenanceLayerId
        Model: ProvenanceModel
    }

type ProvenanceSession =
    {
        Layers: ProvenanceLayer list
        Pairs: Map<ProvenancePairId, ProvenanceLayerPair>
        PairOrder: ProvenancePairId list
        ActivePairId: ProvenancePairId
        BoundaryLinks: ProvenanceBoundaryLink list
    }

[<RequireQualifiedAccess>]
type SessionError =
    | PairNotFound of ProvenancePairId
    | SetNotFound of ProvenanceSetReference
    | EditFailed of EditError

type AddLayerCommand =
    {
        SelectedSets: (ProvenanceSide * ProvenanceSetId) list
    }

type SessionResult =
    Result<ProvenanceSession * ProvenanceTablePatch list, SessionError>

module Session =

    let init model =
        let pair =
            {
                Id = "pair-1"
                LeftLayerId = "layer-1"
                RightLayerId = "layer-2"
                Model = model
            }

        {
            Layers =
                [
                    { Id = "layer-1"; Label = "Inputs" }
                    { Id = "layer-2"; Label = "Outputs" }
                ]
            Pairs = Map.ofList [ pair.Id, pair ]
            PairOrder = [ pair.Id ]
            ActivePairId = pair.Id
            BoundaryLinks = []
        }

    let activePair session =
        session.Pairs.[session.ActivePairId]

    let selectPair pairId session : SessionResult =
        match session.Pairs.TryFind pairId with
        | Some _ -> Ok({ session with ActivePairId = pairId }, [])
        | None -> Error(SessionError.PairNotFound pairId)

    let private values map = map |> Map.toList |> List.map snd

    let private setAt side setId pair =
        match side with
        | ProvenanceSide.Input -> pair.Model.InputSets.TryFind setId
        | ProvenanceSide.Output -> pair.Model.OutputSets.TryFind setId

    let private nextIndex session = session.PairOrder.Length + 1

    let private nextInputSetId pairId side index setId =
        let sideText =
            match side with
            | ProvenanceSide.Input -> "input"
            | ProvenanceSide.Output -> "output"

        $"{pairId}-from-{sideText}-{index}-{setId}"

    let addLayer command session : SessionResult =
        let current = activePair session

        let selectedSets =
            match command.SelectedSets with
            | [] ->
                current.Model.OutputSets
                |> values
                |> List.sortBy (fun set -> set.Name, set.Id)
                |> List.map (fun set -> ProvenanceSide.Output, set.Id)
            | selected -> selected

        let missing =
            selectedSets
            |> List.tryPick (fun (side, setId) ->
                if setAt side setId current |> Option.isNone then
                    Some { PairId = current.Id; Side = side; SetId = setId }
                else
                    None)

        match missing with
        | Some setRef -> Error(SessionError.SetNotFound setRef)
        | None ->
            let pairIndex = nextIndex session
            let layerNumber = pairIndex + 1
            let pairId = $"pair-{pairIndex}"
            let layerId = $"layer-{layerNumber}"
            let hasSelectedInput = selectedSets |> List.exists (fst >> (=) ProvenanceSide.Input)
            let hasSelectedOutput = selectedSets |> List.exists (fst >> (=) ProvenanceSide.Output)

            let leftLayerId, newLayers =
                match hasSelectedInput, hasSelectedOutput with
                | true, true ->
                    let selectionId = $"selection-{layerNumber}"
                    selectionId,
                    [
                        { Id = selectionId; Label = $"Selection {layerNumber}" }
                        { Id = layerId; Label = $"Layer {layerNumber}" }
                    ]
                | true, false ->
                    current.LeftLayerId, [ { Id = layerId; Label = $"Layer {layerNumber}" } ]
                | false, _ ->
                    current.RightLayerId, [ { Id = layerId; Label = $"Layer {layerNumber}" } ]

            let inputs, links =
                selectedSets
                |> List.mapi (fun seedIndex (side, setId) ->
                    let source = (setAt side setId current).Value
                    let nextId = nextInputSetId pairId side seedIndex setId
                    let projected = { source with Id = nextId }
                    let link =
                        {
                            Previous = { PairId = current.Id; Side = side; SetId = setId }
                            Next = { PairId = pairId; Side = ProvenanceSide.Input; SetId = nextId }
                        }

                    projected.Id, projected, link)
                |> List.fold
                    (fun (sets, links) (id, projected, link) ->
                        Map.add id projected sets, link :: links)
                     (Map.empty, [])

            let projectedPropertyValueIds =
                inputs
                |> Map.toList
                |> List.collect (fun (_, set) -> set.PropertyValueIds)
                |> Set.ofList

            let projectedPropertyValues =
                current.Model.PropertyValues
                |> Map.filter (fun id _ -> projectedPropertyValueIds.Contains id)

            let pair =
                {
                    Id = pairId
                    LeftLayerId = leftLayerId
                    RightLayerId = layerId
                    Model =
                        {
                            LoadedTableName = current.Model.LoadedTableName
                            PropertyValues = projectedPropertyValues
                            InputSets = inputs
                            OutputSets = Map.empty
                            Connections = Map.empty
                        }
                }

            Ok(
                {
                    session with
                        Layers = session.Layers @ newLayers
                        Pairs = session.Pairs |> Map.add pair.Id pair
                        PairOrder = session.PairOrder @ [ pair.Id ]
                        ActivePairId = pair.Id
                        BoundaryLinks = session.BoundaryLinks @ List.rev links
                },
                [])
