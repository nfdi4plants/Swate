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

    let private activeRef side setId session = {
        LayerId = session.ActiveLayerId
        Side = side
        SetId = setId
    }

    let private layerAtRef reference session = layerById reference.LayerId session

    let private setAtRef reference session =
        let layer = layerAtRef reference session
        setAt reference.Side reference.SetId layer

    let private referencesForLayer layerId session =
        let layer = layerById layerId session

        [
            for setId, _ in layer.Model.InputSets |> Map.toList do
                yield {
                    LayerId = layerId
                    Side = ProvenanceSide.Input
                    SetId = setId
                }
            for setId, _ in layer.Model.OutputSets |> Map.toList do
                yield {
                    LayerId = layerId
                    Side = ProvenanceSide.Output
                    SetId = setId
                }
        ]

    let private adjacentReferences reference session =
        session.ReferenceLinks
        |> List.collect (fun link -> [
            if link.Source = reference then
                yield link.Target
            if link.Target = reference then
                yield link.Source
        ])

    let private referenceComponent seedRefs session =
        let rec loop pending seen =
            match pending with
            | [] -> seen
            | current :: rest when seen |> Set.contains current -> loop rest seen
            | current :: rest ->
                let next = adjacentReferences current session
                loop (next @ rest) (seen |> Set.add current)

        loop seedRefs Set.empty

    let private referenceContainsPropertyValue propertyValueId reference session =
        match setAtRef reference session with
        | None -> false
        | Some set -> ProvenanceSet.effectivePropertyValueIds set |> List.contains propertyValueId

    let private dirtyReferencesInLayer layerId session =
        referencesForLayer layerId session
        |> List.filter (fun reference ->
            session.DirtyPropertyValueIds
            |> Set.exists (fun propertyValueId -> referenceContainsPropertyValue propertyValueId reference session)
        )

    let private affectedComponent previousLayerId nextLayerId session =
        [
            yield! dirtyReferencesInLayer previousLayerId session
            yield! referencesForLayer nextLayerId session
        ]
        |> List.distinct
        |> fun refs -> referenceComponent refs session

    let private referencedPropertyValues set model =
        ProvenanceSet.effectivePropertyValueIds set
        |> List.choose (fun id -> model.PropertyValues.TryFind id |> Option.map (fun value -> id, value))

    let private copySetData sourceRef targetRef session =
        let sourceLayer = layerAtRef sourceRef session
        let targetLayer = layerAtRef targetRef session
        let sourceSet = (setAtRef sourceRef session).Value
        let targetSet = (setAtRef targetRef session).Value

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
            referencedPropertyValues sourceSet sourceLayer.Model
            |> List.fold (fun state (id, value) -> Map.add id value state) targetLayer.Model.PropertyValues

        let targetModel =
            match targetRef.Side with
            | ProvenanceSide.Input -> {
                targetLayer.Model with
                    InputSets = targetLayer.Model.InputSets |> Map.add targetSet.Id targetSet
                    PropertyValues = propertyValues
              }
            | ProvenanceSide.Output -> {
                targetLayer.Model with
                    OutputSets = targetLayer.Model.OutputSets |> Map.add targetSet.Id targetSet
                    PropertyValues = propertyValues
              }
            |> ProvenanceModel.refreshInheritedOutputProperties

        replaceLayer { targetLayer with Model = targetModel } session

    let private layerIndex (session: ProvenanceSession) (layerId: ProvenanceLayerId) =
        session.LayerOrder
        |> List.tryFindIndex ((=) layerId)
        |> Option.defaultValue System.Int32.MaxValue

    let private propertyValueFromLayer
        (layerId: ProvenanceLayerId)
        (propertyValueId: ProvenancePropertyValueId)
        (session: ProvenanceSession)
        =
        (layerById layerId session).Model.PropertyValues.TryFind propertyValueId

    let private applyPropertyValueToLayer
        (layerId: ProvenanceLayerId)
        (propertyValue: ProvenancePropertyValue)
        (session: ProvenanceSession)
        =
        let layer = layerById layerId session

        if layer.Model.PropertyValues.ContainsKey propertyValue.Id then
            replaceLayer
                {
                    layer with
                        Model = {
                            layer.Model with
                                PropertyValues = layer.Model.PropertyValues |> Map.add propertyValue.Id propertyValue
                        }
                }
                session
        else
            session

    let private applyPropertyValueToReference
        (propertyValue: ProvenancePropertyValue)
        (reference: ProvenanceSetReference)
        (session: ProvenanceSession)
        =
        if referenceContainsPropertyValue propertyValue.Id reference session then
            applyPropertyValueToLayer reference.LayerId propertyValue session
        else
            session

    let private refreshDirtyProperties
        previousLayerId
        (referenceSet: Set<ProvenanceSetReference>)
        (session: ProvenanceSession)
        =
        session.DirtyPropertyValueIds
        |> Set.fold
            (fun state propertyValueId ->
                match propertyValueFromLayer previousLayerId propertyValueId state with
                | None -> state
                | Some propertyValue ->
                    referenceSet
                    |> Set.toList
                    |> List.fold
                        (fun current reference -> applyPropertyValueToReference propertyValue reference current)
                        state
            )
            session

    let private refreshStructuralLinks (referenceSet: Set<ProvenanceSetReference>) session =
        session.ReferenceLinks
        |> List.filter (fun link -> referenceSet.Contains link.Source && referenceSet.Contains link.Target)
        |> List.sortBy (fun link -> layerIndex session link.Source.LayerId, layerIndex session link.Target.LayerId)
        |> List.fold (fun state link -> copySetData link.Source link.Target state) session

    let private refreshForFocusChange previousLayerId nextLayerId session =
        let referenceSet = affectedComponent previousLayerId nextLayerId session

        let withPropertyEdits = refreshDirtyProperties previousLayerId referenceSet session

        let withStructuralRefresh = refreshStructuralLinks referenceSet withPropertyEdits

        {
            withStructuralRefresh with
                DirtyPropertyValueIds = Set.empty
        }

    let selectLayer layerId session : SessionResult =
        match tryLayer layerId session with
        | Some _ ->
            let refreshed = refreshForFocusChange session.ActiveLayerId layerId session

            Ok(
                {
                    refreshed with
                        ActiveLayerId = layerId
                },
                []
            )
        | None -> Error(SessionError.PairNotFound layerId)

    let selectPair pairId session : SessionResult = selectLayer pairId session

    let rec private nativeOwner reference session =
        match session.ReferenceLinks |> List.tryFind (fun link -> link.Next = reference) with
        | Some link -> nativeOwner link.Previous session
        | None -> reference

    let updatePropertyValue propertyValueId value unit session : SessionResult =
        let layer = activeLayer session

        Edit.updatePropertyValue propertyValueId value unit layer.Model
        |> mapEditError
        |> Result.bind (fun (model, patches) ->
            updateLayerModel layer.Id model session
            |> Result.map (fun next ->
                {
                    next with
                        DirtyPropertyValueIds = next.DirtyPropertyValueIds |> Set.add propertyValueId
                },
                patches
            )
        )

    let private validatePropertyValueUpdate propertyValueId value unit session =
        let layer = activeLayer session

        Edit.updatePropertyValue propertyValueId value unit layer.Model
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
        let layer = activeLayer session

        Edit.createLoadedPropertyValue command layer.Model
        |> mapEditError
        |> Result.bind (fun (model, patches) ->
            updateLayerModel layer.Id model session
            |> Result.map (fun next -> next, patches)
        )

    let createLoadedPropertyValue (command: CreateLoadedPropertyValueCommand) session : SessionResult =
        let layer = activeLayer session

        Edit.createLoadedPropertyValue command layer.Model
        |> mapEditError
        |> Result.bind (fun (model, patches) ->
            updateLayerModel layer.Id model session
            |> Result.map (fun next -> next, patches)
        )

    let copyPropertyValueToLoadedTarget propertyValueId (target: ProvenancePropertyTarget) session : SessionResult =
        let layer = activeLayer session

        match layer.Model.PropertyValues.TryFind propertyValueId with
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
        (_session: ProvenanceSession)
        : ProvenanceLayerId =
        reference.LayerId

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
