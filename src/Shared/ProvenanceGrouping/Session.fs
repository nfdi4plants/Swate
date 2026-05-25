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
