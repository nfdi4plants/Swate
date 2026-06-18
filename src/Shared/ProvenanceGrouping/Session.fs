module Swate.Components.Shared.ProvenanceGrouping.Session

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Edit

type ProvenanceLayerId = string
type ProvenancePairId = ProvenanceLayerId
type ProvenanceLayerSideId = string

type PropertyValueSourceInfo = {
    TableName: ProvenanceTableName option
    ProcessName: ProvenanceProcessName option
    InputNames: string list
    OutputNames: string list
    IsCurrentTable: bool
}

[<RequireQualifiedAccess>]
type PropertyOrigin =
    | Current of pairId: ProvenancePairId * side: ProvenanceSide
    | UpstreamLayer of layerId: ProvenanceLayerId
    | PreviousContext of source: PropertyValueSourceInfo

type ProvenanceLayer = {
    Id: ProvenanceLayerId
    Label: string
    InputSideId: ProvenanceLayerSideId
    OutputSideId: ProvenanceLayerSideId
    Model: ProvenanceModel
} with

    member this.LeftLayerId = this.InputSideId
    member this.RightLayerId = this.OutputSideId

type ProvenanceLayerPair = ProvenanceLayer

type ProvenanceSetReference = {
    LayerId: ProvenanceLayerId
    Side: ProvenanceSide
    SetId: ProvenanceSetId
} with

    member this.PairId = this.LayerId

type ProvenanceReferenceLink = {
    Source: ProvenanceSetReference
    Target: ProvenanceSetReference
} with

    member this.Previous = this.Source
    member this.Next = this.Target

type ProvenanceBoundaryLink = ProvenanceReferenceLink

type ProvenanceSession = {
    Layers: ProvenanceLayer list
    LayerOrder: ProvenanceLayerId list
    ActiveLayerId: ProvenanceLayerId
    ReferenceLinks: ProvenanceReferenceLink list
    DirtyPropertyValueIds: Set<ProvenancePropertyValueId>
} with
    // Temporary adapter for component migration. Remove after downstream callers stop using pair terminology.
    member this.Pairs =
        this.Layers |> List.map (fun layer -> layer.Id, layer) |> Map.ofList

    member this.PairOrder = this.LayerOrder
    member this.ActivePairId = this.ActiveLayerId
    member this.BoundaryLinks = this.ReferenceLinks

[<RequireQualifiedAccess>]
type SessionError =
    | PairNotFound of ProvenancePairId
    | SetNotFound of ProvenanceSetReference
    | EditFailed of EditError

type AddLayerCommand = {
    SelectedSets: (ProvenanceSide * ProvenanceSetId) list
}

type SessionResult = Result<ProvenanceSession * ProvenanceTablePatch list, SessionError>

module Session =

    let private sideId layerId side =
        match side with
        | ProvenanceSide.Input -> $"{layerId}-input"
        | ProvenanceSide.Output -> $"{layerId}-output"

    let private layerLabel index =
        if index = 1 then "Layer 1" else $"Layer {index}"

    let init model =
        let layerId = "layer-1"

        let layer = {
            Id = layerId
            Label = layerLabel 1
            InputSideId = sideId layerId ProvenanceSide.Input
            OutputSideId = sideId layerId ProvenanceSide.Output
            Model = model
        }

        {
            Layers = [ layer ]
            LayerOrder = [ layer.Id ]
            ActiveLayerId = layer.Id
            ReferenceLinks = []
            DirtyPropertyValueIds = Set.empty
        }

    let tryLayer layerId session =
        session.Layers |> List.tryFind (fun layer -> layer.Id = layerId)

    let layerById layerId session =
        tryLayer layerId session
        |> Option.defaultWith (fun () -> failwithf "Unknown provenance layer '%s'." layerId)

    let activeLayer session = layerById session.ActiveLayerId session

    let activePair session = activeLayer session

    let selectLayer layerId session : SessionResult =
        match tryLayer layerId session with
        | Some _ -> Ok({ session with ActiveLayerId = layerId }, [])
        | None -> Error(SessionError.PairNotFound layerId)

    let selectPair pairId session : SessionResult = selectLayer pairId session

    let private values map = map |> Map.toList |> List.map snd

    let private setAt side setId pair =
        match side with
        | ProvenanceSide.Input -> pair.Model.InputSets.TryFind setId
        | ProvenanceSide.Output -> pair.Model.OutputSets.TryFind setId

    let private nextIndex session = session.LayerOrder.Length + 1

    let private nextInputSetId layerId side index setId =
        let sideText =
            match side with
            | ProvenanceSide.Input -> "input"
            | ProvenanceSide.Output -> "output"

        $"{layerId}-from-{sideText}-{index}-{setId}"

    let addLayer command session : SessionResult =
        let current = activeLayer session

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
                    Some {
                        LayerId = current.Id
                        Side = side
                        SetId = setId
                    }
                else
                    None
            )

        match missing with
        | Some setRef -> Error(SessionError.SetNotFound setRef)
        | None ->
            let layerIndex = nextIndex session
            let layerId = $"layer-{layerIndex}"

            let inputs, links =
                selectedSets
                |> List.mapi (fun seedIndex (side, setId) ->
                    let source = (setAt side setId current).Value
                    let nextId = nextInputSetId layerId side seedIndex setId
                    let projected = { source with Id = nextId }

                    let link = {
                        Source = {
                            LayerId = current.Id
                            Side = side
                            SetId = setId
                        }
                        Target = {
                            LayerId = layerId
                            Side = ProvenanceSide.Input
                            SetId = nextId
                        }
                    }

                    projected.Id, projected, link
                )
                |> List.fold
                    (fun (sets, links) (id, projected, link) -> Map.add id projected sets, link :: links)
                    (Map.empty, [])

            let projectedPropertyValueIds =
                inputs
                |> Map.toList
                |> List.collect (fun (_, set) -> ProvenanceSet.effectivePropertyValueIds set)
                |> Set.ofList

            let projectedPropertyValues =
                current.Model.PropertyValues
                |> Map.filter (fun id _ -> projectedPropertyValueIds.Contains id)

            let pair = {
                Id = layerId
                Label = layerLabel layerIndex
                InputSideId = sideId layerId ProvenanceSide.Input
                OutputSideId = sideId layerId ProvenanceSide.Output
                Model = {
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
                        Layers = session.Layers @ [ pair ]
                        LayerOrder = session.LayerOrder @ [ pair.Id ]
                        ActiveLayerId = pair.Id
                        ReferenceLinks = session.ReferenceLinks @ List.rev links
                },
                []
            )

    let private replaceLayer layer session = {
        session with
            Layers =
                session.Layers
                |> List.map (fun current -> if current.Id = layer.Id then layer else current)
    }

    let private mapEditError = Result.mapError SessionError.EditFailed

    let private updateLayerModel layerId model session =
        match tryLayer layerId session with
        | None -> Error(SessionError.PairNotFound layerId)
        | Some layer -> Ok(replaceLayer { layer with Model = model } session)

    let private referencedPropertyValues set model =
        ProvenanceSet.effectivePropertyValueIds set
        |> List.choose (fun id -> model.PropertyValues.TryFind id |> Option.map (fun value -> id, value))

    let private copySetData
        (sourceRef: ProvenanceSetReference)
        (targetRef: ProvenanceSetReference)
        (session: ProvenanceSession)
        =
        let sourcePair = session.Pairs.[sourceRef.PairId]
        let targetPair = session.Pairs.[targetRef.PairId]
        let sourceSet = (setAt sourceRef.Side sourceRef.SetId sourcePair).Value
        let targetSet = (setAt targetRef.Side targetRef.SetId targetPair).Value

        let targetSet =
            let inheritedPropertyValueIds =
                match targetRef.Side with
                | ProvenanceSide.Input -> sourceSet.InheritedPropertyValueIds
                | ProvenanceSide.Output -> targetSet.InheritedPropertyValueIds

            {
                targetSet with
                    PropertyValueIds = sourceSet.PropertyValueIds
                    InheritedPropertyValueIds = inheritedPropertyValueIds
            }

        let propertyValues =
            referencedPropertyValues sourceSet sourcePair.Model
            |> List.fold (fun state (id, value) -> Map.add id value state) targetPair.Model.PropertyValues

        let targetModel =
            match targetRef.Side with
            | ProvenanceSide.Input -> {
                targetPair.Model with
                    InputSets = targetPair.Model.InputSets |> Map.add targetSet.Id targetSet
                    PropertyValues = propertyValues
              }
            | ProvenanceSide.Output -> {
                targetPair.Model with
                    OutputSets = targetPair.Model.OutputSets |> Map.add targetSet.Id targetSet
                    PropertyValues = propertyValues
              }
            |> ProvenanceModel.refreshInheritedOutputProperties

        replaceLayer { targetPair with Model = targetModel } session

    let private linkedRefs origin session =
        let rec collect pending seen =
            match pending with
            | [] -> seen
            | current :: rest when Set.contains current seen -> collect rest seen
            | current :: rest ->
                let adjacent =
                    session.ReferenceLinks
                    |> List.collect (fun link ->
                        if link.Previous = current then [ link.Next ]
                        elif link.Next = current then [ link.Previous ]
                        else []
                    )

                collect (adjacent @ rest) (Set.add current seen)

        collect [ origin ] Set.empty |> Set.toList

    let private synchronizeSet origin session =
        linkedRefs origin session
        |> List.filter ((<>) origin)
        |> List.fold (fun state target -> copySetData origin target state) session

    let private activeRef side setId session = {
        LayerId = session.ActiveLayerId
        Side = side
        SetId = setId
    }

    let private displayTargetRefs
        (target: ProvenancePropertyTarget)
        (session: ProvenanceSession)
        : Result<ProvenanceSetReference list, SessionError> =
        match target with
        | ProvenancePropertyTarget.InputSets ids ->
            ids |> List.map (fun id -> activeRef ProvenanceSide.Input id session) |> Ok
        | ProvenancePropertyTarget.OutputSets ids ->
            ids |> List.map (fun id -> activeRef ProvenanceSide.Output id session) |> Ok
        | ProvenancePropertyTarget.Connections ids ->
            let pair = activePair session

            ids
            |> List.fold
                (fun result id ->
                    result
                    |> Result.bind (fun references ->
                        match pair.Model.Connections.TryFind id with
                        | None -> Error(SessionError.EditFailed(EditError.ConnectionNotFound id))
                        | Some connection ->
                            Ok(
                                references
                                @ [
                                    activeRef ProvenanceSide.Input connection.InputSetId session
                                    activeRef ProvenanceSide.Output connection.OutputSetId session
                                ]
                            )
                    )
                )
                (Ok [])

    let rec private nativeOwner reference session =
        match session.ReferenceLinks |> List.tryFind (fun link -> link.Next = reference) with
        | Some link -> nativeOwner link.Previous session
        | None -> reference

    let private ownedSetTargets (target: ProvenancePropertyTarget) session =
        match target with
        | ProvenancePropertyTarget.Connections _ -> Ok []
        | _ ->
            displayTargetRefs target session
            |> Result.map (fun references ->
                references
                |> List.map (fun reference -> nativeOwner reference session)
                |> List.distinct
                |> List.groupBy (fun reference -> reference.PairId, reference.Side)
                |> List.map (fun ((pairId, side), references) ->
                    let setIds = references |> List.map (fun reference -> reference.SetId)

                    let target =
                        match side with
                        | ProvenanceSide.Input -> ProvenancePropertyTarget.InputSets setIds
                        | ProvenanceSide.Output -> ProvenancePropertyTarget.OutputSets setIds

                    pairId, target, references
                )
            )

    let private activePropertyOwnerPair propertyValueId session =
        let pair = activePair session

        [
            for setId, set in pair.Model.InputSets |> Map.toList do
                if ProvenanceSet.effectivePropertyValueIds set |> List.contains propertyValueId then
                    yield activeRef ProvenanceSide.Input setId session
            for setId, set in pair.Model.OutputSets |> Map.toList do
                if ProvenanceSet.effectivePropertyValueIds set |> List.contains propertyValueId then
                    yield activeRef ProvenanceSide.Output setId session
        ]
        |> List.tryHead
        |> Option.map (fun reference -> (nativeOwner reference session).PairId)
        |> Option.defaultValue pair.Id

    let updatePropertyValue propertyValueId value unit session : SessionResult =
        let ownerPairId = activePropertyOwnerPair propertyValueId session
        let pair = session.Pairs.[ownerPairId]

        Edit.updatePropertyValue propertyValueId value unit pair.Model
        |> mapEditError
        |> Result.bind (fun (model, patches) ->
            updateLayerModel pair.Id model session
            |> Result.map (fun next ->
                let propagated =
                    next.Pairs
                    |> Map.fold
                        (fun state pairId candidate ->
                            if candidate.Model.PropertyValues.ContainsKey propertyValueId then
                                let candidateModel = {
                                    candidate.Model with
                                        PropertyValues =
                                            candidate.Model.PropertyValues
                                            |> Map.add propertyValueId model.PropertyValues.[propertyValueId]
                                }

                                replaceLayer
                                    {
                                        candidate with
                                            Model = candidateModel
                                    }
                                    state
                            else
                                state
                        )
                        next

                propagated, patches
            )
        )

    let private validatePropertyValueUpdate propertyValueId value unit session =
        let ownerPairId = activePropertyOwnerPair propertyValueId session

        match session.Pairs.TryFind ownerPairId with
        | None -> Error(SessionError.PairNotFound ownerPairId)
        | Some pair ->
            Edit.updatePropertyValue propertyValueId value unit pair.Model
            |> mapEditError
            |> Result.map ignore

    let updatePropertyValues
        (updates: (ProvenancePropertyValueId * ProvenanceValue * ProvenanceTerm option) list)
        session
        : SessionResult =
        let updates =
            updates |> List.distinctBy (fun (propertyValueId, _, _) -> propertyValueId)

        let rec validate remaining =
            match remaining with
            | [] -> Ok()
            | (propertyValueId, value, unit) :: tail ->
                validatePropertyValueUpdate propertyValueId value unit session
                |> Result.bind (fun () -> validate tail)

        validate updates
        |> Result.bind (fun () ->
            updates
            |> List.fold
                (fun result (propertyValueId, value, unit) ->
                    result
                    |> Result.bind (fun (state, patches) ->
                        updatePropertyValue propertyValueId value unit state
                        |> Result.map (fun (next, addedPatches) -> next, patches @ addedPatches)
                    )
                )
                (Ok(session, []))
        )

    let createCurrentLoadedPropertyValue (command: CreateLoadedPropertyValueCommand) session : SessionResult =
        let pair = activePair session

        Edit.createLoadedPropertyValue command pair.Model
        |> mapEditError
        |> Result.bind (fun (model, patches) ->
            updateLayerModel pair.Id model session |> Result.map (fun next -> next, patches)
        )

    let createLoadedPropertyValue (command: CreateLoadedPropertyValueCommand) session : SessionResult =
        match command.Target with
        | ProvenancePropertyTarget.Connections _ ->
            let pair = activePair session

            displayTargetRefs command.Target session
            |> Result.bind (fun references ->
                Edit.createLoadedPropertyValue command pair.Model
                |> mapEditError
                |> Result.bind (fun (model, patches) ->
                    updateLayerModel pair.Id model session
                    |> Result.map (fun next ->
                        let synchronized =
                            references
                            |> List.fold (fun current reference -> synchronizeSet reference current) next

                        synchronized, patches
                    )
                )
            )
        | _ ->
            ownedSetTargets command.Target session
            |> Result.bind (fun targets ->
                targets
                |> List.fold
                    (fun result (pairId, target, references) ->
                        result
                        |> Result.bind (fun (state, patches) ->
                            let pair = state.Pairs.[pairId]

                            Edit.createLoadedPropertyValue { command with Target = target } pair.Model
                            |> mapEditError
                            |> Result.bind (fun (model, addedPatches) ->
                                updateLayerModel pair.Id model state
                                |> Result.map (fun next ->
                                    let synchronized =
                                        references
                                        |> List.fold
                                            (fun current reference -> synchronizeSet reference current)
                                            next

                                    synchronized, patches @ addedPatches
                                )
                            )
                        )
                    )
                    (Ok(session, []))
            )

    let copyPropertyValueToLoadedTarget propertyValueId (target: ProvenancePropertyTarget) session : SessionResult =
        let pair = activePair session

        match pair.Model.PropertyValues.TryFind propertyValueId with
        | None -> Error(SessionError.EditFailed(EditError.PropertyNotFound propertyValueId))
        | Some propertyValue ->
            createLoadedPropertyValue
                {
                    Target = target
                    CopiedFrom = Some propertyValueId
                    Header = propertyValue.Header
                    Value = propertyValue.Value
                    Unit = propertyValue.Unit
                }
                session

    let createLoadedSet command session : SessionResult =
        let pair = activePair session

        Edit.createLoadedSet command pair.Model
        |> mapEditError
        |> Result.bind (fun (model, patches) ->
            updateLayerModel pair.Id model session |> Result.map (fun next -> next, patches)
        )

    let connectSets inputSetId outputSetId processName session : SessionResult =
        let pair = activePair session

        Edit.connectSets inputSetId outputSetId processName pair.Model
        |> mapEditError
        |> Result.bind (fun (model, patches) ->
            updateLayerModel pair.Id model session |> Result.map (fun next -> next, patches)
        )

    let removeConnection connectionId session : SessionResult =
        let pair = activePair session

        Edit.removeConnection connectionId pair.Model
        |> mapEditError
        |> Result.bind (fun (model, patches) ->
            updateLayerModel pair.Id model session |> Result.map (fun next -> next, patches)
        )

    let removeConnections connectionIds session : SessionResult =
        connectionIds
        |> List.distinct
        |> List.fold
            (fun result connectionId ->
                result
                |> Result.bind (fun (current, patches) ->
                    removeConnection connectionId current
                    |> Result.map (fun (next, added) -> next, patches @ added)
                )
            )
            (Ok(session, []))

    let private sourceInfoFromAnchor
        (pair: ProvenanceLayerPair)
        (source: ProvenanceWritebackAnchor)
        : PropertyValueSourceInfo =
        {
            TableName = Some source.TableName
            ProcessName = source.ProcessName
            InputNames = source.InputNames
            OutputNames = source.OutputNames
            IsCurrentTable = source.TableName = pair.Model.LoadedTableName
        }

    let private propertyValueSourceFromLoadedMembership
        (pair: ProvenanceLayerPair)
        (propertyValue: ProvenancePropertyValue)
        : PropertyValueSourceInfo option =
        let inputSets =
            pair.Model.InputSets
            |> values
            |> List.filter (fun set -> ProvenanceSet.effectivePropertyValueIds set |> List.contains propertyValue.Id)

        let outputSets =
            pair.Model.OutputSets
            |> values
            |> List.filter (fun set -> ProvenanceSet.effectivePropertyValueIds set |> List.contains propertyValue.Id)

        match inputSets, outputSets with
        | [], [] -> None
        | _ ->
            Some {
                TableName = Some pair.Model.LoadedTableName
                ProcessName = None
                InputNames = inputSets |> List.map (fun set -> set.Name) |> List.distinct
                OutputNames = outputSets |> List.map (fun set -> set.Name) |> List.distinct
                IsCurrentTable = true
            }

    let propertyValueSourceInfo
        (pair: ProvenanceLayerPair)
        (propertyValue: ProvenancePropertyValue)
        : PropertyValueSourceInfo option =
        propertyValue.Source
        |> Option.map (sourceInfoFromAnchor pair)
        |> Option.orElseWith (fun () -> propertyValueSourceFromLoadedMembership pair propertyValue)

    let private ownerReferences
        (propertyValueId: ProvenancePropertyValueId)
        (session: ProvenanceSession)
        : ProvenanceSetReference list =
        let pair = activePair session

        [
            for setId, set in pair.Model.InputSets |> Map.toList do
                if ProvenanceSet.effectivePropertyValueIds set |> List.contains propertyValueId then
                    yield activeRef ProvenanceSide.Input setId session
            for setId, set in pair.Model.OutputSets |> Map.toList do
                if ProvenanceSet.effectivePropertyValueIds set |> List.contains propertyValueId then
                    yield activeRef ProvenanceSide.Output setId session
        ]
        |> List.map (fun reference -> nativeOwner reference session)
        |> List.distinct

    let private layerIdForReference
        (reference: ProvenanceSetReference)
        (session: ProvenanceSession)
        : ProvenanceLayerId =
        let pair = session.Pairs.[reference.PairId]

        match reference.Side with
        | ProvenanceSide.Input -> pair.LeftLayerId
        | ProvenanceSide.Output -> pair.RightLayerId

    let propertyValueOriginInSession
        (pairId: ProvenancePairId)
        (side: ProvenanceSide)
        (propertyValueId: ProvenancePropertyValueId)
        (session: ProvenanceSession)
        : PropertyOrigin option =
        match session.Pairs.TryFind pairId with
        | None -> None
        | Some pair ->
            pair.Model.PropertyValues.TryFind propertyValueId
            |> Option.map (fun propertyValue ->
                let scoped = { session with ActiveLayerId = pairId }

                match propertyValueSourceInfo pair propertyValue with
                | Some source when source.TableName <> Some pair.Model.LoadedTableName ->
                    PropertyOrigin.PreviousContext source
                | _ ->
                    match ownerReferences propertyValueId scoped with
                    | owner :: _ when owner.PairId = pairId -> PropertyOrigin.Current(pairId, side)
                    | owner :: _ -> PropertyOrigin.UpstreamLayer(layerIdForReference owner scoped)
                    | [] -> PropertyOrigin.Current(pairId, side)
            )
