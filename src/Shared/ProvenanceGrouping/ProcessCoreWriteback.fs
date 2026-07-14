module Swate.Components.Shared.ProvenanceGrouping.ProcessCoreWriteback

open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreGraph

type private ExistingAnnotationUpdate = {
    PropertyValueId: ProvenancePropertyValueId
    Annotations: Annotation list
    Value: ProvenanceValue
    Unit: ProvenanceTerm option
}

/// One materialized node reference, resolved and validated during preflight
/// so `apply` only ever touches already-validated `IONode` instances.
type private PlannedNode = { SetId: ProvenanceSetId; Node: IONode }

/// One internal mutation command for a direct ProcessCore process row.
/// Structure is never derived from a tabular/scaffold representation.
type private PlannedRow = {
    Input: PlannedNode option
    Output: PlannedNode option
}

type private Plan = {
    Updates: ExistingAnnotationUpdate list
    Dataset: Dataset option
    LoadedTableName: string
    ReplacedProcesses: (Process * PlannedRow list) list
    NewRows: PlannedRow list
}

let private anchorOfOrigin =
    function
    | ProvenancePropertyOrigin.Real anchor
    | ProvenancePropertyOrigin.Virtual anchor -> anchor

let private findPropertyValue (session: ProvenanceSession) (propertyValueId: ProvenancePropertyValueId) =
    session.Layers
    |> List.tryPick (fun layer -> layer.Model.PropertyValues.TryFind propertyValueId)

let private validateGraph (index: ProcessCoreWritebackIndex) (arc: ARC) : ProcessCoreWritebackError list =
    if graphFingerprint arc <> index.ArcFingerprint then
        [ ProcessCoreWritebackError.StaleArc ]
    else
        []

let private validateLayers
    (index: ProcessCoreWritebackIndex)
    (session: ProvenanceSession)
    : ProcessCoreWritebackError list =
    let hasInitialLayer =
        session.Layers
        |> List.exists (fun layer -> layer.Model.Source.Id = index.InitialSourceId)

    let layerIds = session.Layers |> List.map (fun layer -> layer.Id) |> List.sort
    let orderIds = session.LayerOrder |> List.sort

    [
        if not hasInitialLayer then
            yield ProcessCoreWritebackError.InitialLayerNotFound index.InitialSourceId
        if
            layerIds <> orderIds
            || (session.LayerOrder |> List.distinct |> List.length)
               <> session.LayerOrder.Length
        then
            yield ProcessCoreWritebackError.InvalidLayerOrder session.LayerOrder
    ]

/// Resolves one `UpdatePropertyValue` patch. A property absent from the
/// conversion index but present in the final session is editor-created
/// (`Virtual`) in this session; its value update is absorbed here because
/// its owning `AddLoadedPropertyValue` materialization (Task 7) always
/// writes the property's final session value, not the add-patch payload.
let private resolveUpdatePatch
    (index: ProcessCoreWritebackIndex)
    (session: ProvenanceSession)
    (arc: ARC)
    (propertyValueId: ProvenancePropertyValueId)
    (patchAnchor: ProvenanceWritebackAnchor)
    : Result<ExistingAnnotationUpdate option, ProcessCoreWritebackError list> =
    match index.PropertyValueLocations.TryFind propertyValueId with
    | None ->
        match findPropertyValue session propertyValueId with
        | None ->
            Error [
                ProcessCoreWritebackError.PropertyNotFound propertyValueId
            ]
        | Some _ -> Ok None
    | Some locations ->
        match findPropertyValue session propertyValueId with
        | None ->
            Error [
                ProcessCoreWritebackError.PropertyNotFound propertyValueId
            ]
        | Some finalValue ->
            let finalAnchor = anchorOfOrigin finalValue.Origin

            if finalAnchor.Source.Id <> patchAnchor.Source.Id then
                Error [
                    ProcessCoreWritebackError.SourceLocationNotFound propertyValueId
                ]
            else
                let resolutions =
                    locations
                    |> List.map (fun location ->
                        match tryResolveAnnotation location arc with
                        | Some annotation when annotationFingerprint annotation = location.Fingerprint -> Ok annotation
                        | Some _ -> Error(ProcessCoreWritebackError.SourceLocationNotFound propertyValueId)
                        | None -> Error(ProcessCoreWritebackError.SourceLocationNotFound propertyValueId)
                    )

                let errors =
                    resolutions
                    |> List.choose (
                        function
                        | Error e -> Some e
                        | Ok _ -> None
                    )

                if not errors.IsEmpty then
                    Error(errors |> List.distinct)
                else
                    let annotations =
                        resolutions
                        |> List.choose (
                            function
                            | Ok a -> Some a
                            | Error _ -> None
                        )

                    Ok(
                        Some {
                            PropertyValueId = propertyValueId
                            Annotations = annotations
                            Value = finalValue.Value
                            Unit = finalValue.Unit
                        }
                    )

let private processLocationKey (location: ProcessCoreProcessLocation) =
    let path = String.concat "/" location.DatasetPath
    $"{path}:{location.ProcessIndex}"

let private processLocationKeyOfConnection (index: ProcessCoreWritebackIndex) (connectionId: ProvenanceConnectionId) =
    processLocationKey index.ConnectionLocations.[connectionId].Process

let private resolvePlannedNode
    (sets: Map<ProvenanceSetId, ProvenanceSet>)
    (setId: ProvenanceSetId)
    : Result<PlannedNode, ProcessCoreWritebackError list> =
    match sets.TryFind setId with
    | None -> Error [ ProcessCoreWritebackError.SetNotFound setId ]
    | Some set ->
        match nodeFromSet set with
        | Ok node -> Ok { SetId = setId; Node = node }
        | Error e -> Error [ e ]

/// AddLoadedSet/AddLoadedConnection patches for the loaded table. Every
/// final connection materializes exactly one two-sided row; every final
/// added set not represented by a connection materializes one one-sided
/// row. A connection added and later removed is consumed without a row -
/// its endpoint set(s) fall back to the one-sided rule.
let private planAdditions
    (index: ProcessCoreWritebackIndex)
    (finalInputSets: Map<ProvenanceSetId, ProvenanceSet>)
    (finalOutputSets: Map<ProvenanceSetId, ProvenanceSet>)
    (finalConnections: Map<ProvenanceConnectionId, ProvenanceConnection>)
    (addSetPatches: (ProvenanceSide * ProvenanceIOHeader * string) list)
    (addConnectionPatches: (ProvenanceSetId * ProvenanceSetId) list)
    : Result<PlannedRow list, ProcessCoreWritebackError list> =

    let addedSetIds (sets: Map<ProvenanceSetId, ProvenanceSet>) =
        sets
        |> Map.toList
        |> List.map fst
        |> List.filter (fun id -> not (index.EndpointLocations.ContainsKey id))
        |> Set.ofList

    let addedInputSetIds = addedSetIds finalInputSets
    let addedOutputSetIds = addedSetIds finalOutputSets

    let isConnectedInFinal setId =
        finalConnections
        |> Map.exists (fun _ connection -> connection.InputSetId = setId || connection.OutputSetId = setId)

    let claimedConnectionIds =
        System.Collections.Generic.HashSet<ProvenanceConnectionId>()

    let connectionResults =
        addConnectionPatches
        |> List.choose (fun (inputSetId, outputSetId) ->
            finalConnections
            |> Map.toList
            |> List.tryFind (fun (connectionId, connection) ->
                not (claimedConnectionIds.Contains connectionId)
                && connection.InputSetId = inputSetId
                && connection.OutputSetId = outputSetId
            )
            |> Option.map (fun (connectionId, connection) ->
                claimedConnectionIds.Add connectionId |> ignore

                match
                    resolvePlannedNode finalInputSets connection.InputSetId,
                    resolvePlannedNode finalOutputSets connection.OutputSetId
                with
                | Ok inputNode, Ok outputNode ->
                    Ok(
                        Some {
                            Input = Some inputNode
                            Output = Some outputNode
                        }
                    )
                | Error e, _
                | _, Error e -> Error e
            )
        )

    let claimedSetIds = System.Collections.Generic.HashSet<ProvenanceSetId>()

    let setResults =
        addSetPatches
        |> List.choose (fun (side, header, name) ->
            let candidates, sets =
                match side with
                | ProvenanceSide.Input -> addedInputSetIds, finalInputSets
                | ProvenanceSide.Output -> addedOutputSetIds, finalOutputSets

            candidates
            |> Set.toList
            |> List.tryFind (fun id ->
                not (claimedSetIds.Contains id)
                && sets.[id].Header = header
                && sets.[id].Name = name
            )
            |> Option.map (fun id ->
                claimedSetIds.Add id |> ignore

                if isConnectedInFinal id then
                    Ok None
                else
                    resolvePlannedNode sets id
                    |> Result.map (fun node ->
                        Some(
                            match side with
                            | ProvenanceSide.Input -> { Input = Some node; Output = None }
                            | ProvenanceSide.Output -> { Input = None; Output = Some node }
                        )
                    )
            )
        )

    let allResults = connectionResults @ setResults

    let errors =
        allResults
        |> List.collect (
            function
            | Error e -> e
            | Ok _ -> []
        )

    if not errors.IsEmpty then
        Error(errors |> List.distinct)
    else
        Ok(
            allResults
            |> List.choose (
                function
                | Ok row -> row
                | Error _ -> None
            )
        )

/// RemoveLoadedConnection patches for the loaded table. Groups removals by
/// their indexed original process, replays every indexed connection from
/// that process against the final model, and plans one exact row per
/// surviving edge plus one-sided rows for endpoints left disconnected
/// everywhere in the final model. A removal matching no indexed connection
/// location is an editor-created connection and is consumed as a no-op.
let private planRemovals
    (arc: ARC)
    (index: ProcessCoreWritebackIndex)
    (finalInputSets: Map<ProvenanceSetId, ProvenanceSet>)
    (finalOutputSets: Map<ProvenanceSetId, ProvenanceSet>)
    (finalConnections: Map<ProvenanceConnectionId, ProvenanceConnection>)
    (removalPairs: (ProvenanceSetId * ProvenanceSetId) list)
    : Result<(Process * PlannedRow list) list, ProcessCoreWritebackError list> =

    let matchedConnectionIds =
        removalPairs
        |> List.choose (fun (inputSetId, outputSetId) ->
            index.ConnectionLocations
            |> Map.toList
            |> List.tryFind (fun (_, location) ->
                location.InputSetId = inputSetId && location.OutputSetId = outputSetId
            )
            |> Option.map fst
        )
        |> List.distinct

    if matchedConnectionIds.IsEmpty then
        Ok []
    else
        let processKeyOf connectionId =
            processLocationKeyOfConnection index connectionId

        let byProcess = matchedConnectionIds |> List.groupBy processKeyOf

        let results =
            byProcess
            |> List.map (fun (processKey, connectionIdsForProcess) ->
                let anyLocation = index.ConnectionLocations.[connectionIdsForProcess.Head]
                let procLocation = anyLocation.Process

                match tryResolveProcess procLocation arc with
                | None ->
                    Error [
                        ProcessCoreWritebackError.SourceLocationNotFound processKey
                    ]
                | Some originalProcess ->
                    let allForProcess =
                        index.ConnectionLocations
                        |> Map.toList
                        |> List.filter (fun (connectionId, _) ->
                            processLocationKeyOfConnection index connectionId = processKey
                        )
                        |> List.map fst

                    let surviving = allForProcess |> List.filter finalConnections.ContainsKey
                    let removed = allForProcess |> List.filter (finalConnections.ContainsKey >> not)

                    if removed.IsEmpty then
                        Ok None
                    else
                        let survivingRowResults =
                            surviving
                            |> List.map (fun connectionId ->
                                let connection = finalConnections.[connectionId]

                                match
                                    resolvePlannedNode finalInputSets connection.InputSetId,
                                    resolvePlannedNode finalOutputSets connection.OutputSetId
                                with
                                | Ok inputNode, Ok outputNode ->
                                    Ok {
                                        Input = Some inputNode
                                        Output = Some outputNode
                                    }
                                | Error e, _
                                | _, Error e -> Error e
                            )

                        let disconnectedSetRefs =
                            removed
                            |> List.collect (fun connectionId ->
                                let location = index.ConnectionLocations.[connectionId]

                                [
                                    ProvenanceSide.Input, location.InputSetId
                                    ProvenanceSide.Output, location.OutputSetId
                                ]
                            )
                            |> List.distinct
                            |> List.filter (fun (_, setId) ->
                                not (
                                    finalConnections
                                    |> Map.exists (fun _ connection ->
                                        connection.InputSetId = setId || connection.OutputSetId = setId
                                    )
                                )
                            )

                        let oneSidedRowResults =
                            disconnectedSetRefs
                            |> List.map (fun (side, setId) ->
                                let sets =
                                    match side with
                                    | ProvenanceSide.Input -> finalInputSets
                                    | ProvenanceSide.Output -> finalOutputSets

                                resolvePlannedNode sets setId
                                |> Result.map (fun node ->
                                    match side with
                                    | ProvenanceSide.Input -> { Input = Some node; Output = None }
                                    | ProvenanceSide.Output -> { Input = None; Output = Some node }
                                )
                            )

                        let combined = survivingRowResults @ oneSidedRowResults

                        let errors =
                            combined
                            |> List.collect (
                                function
                                | Error e -> e
                                | Ok _ -> []
                            )

                        if not errors.IsEmpty then
                            Error(errors |> List.distinct)
                        else
                            Ok(
                                Some(
                                    originalProcess,
                                    combined
                                    |> List.choose (
                                        function
                                        | Ok row -> Some row
                                        | Error _ -> None
                                    )
                                )
                            )
            )

        let errors =
            results
            |> List.collect (
                function
                | Error e -> e
                | Ok _ -> []
            )

        if not errors.IsEmpty then
            Error(errors |> List.distinct)
        else
            Ok(
                results
                |> List.choose (
                    function
                    | Ok x -> x
                    | Error _ -> None
                )
            )

/// Every planned node materialization, across replacement and new rows,
/// grouped by ProcessCore node key. Two distinct editor sets that would
/// materialize to the same node with differing header identity are a
/// conflict; reuse through matching header identity remains valid.
let private validateNodeIdentity (rows: PlannedRow list) : ProcessCoreWritebackError list =
    let allNodes =
        rows |> List.collect (fun row -> [ row.Input; row.Output ] |> List.choose id)

    allNodes
    |> List.groupBy (fun planned -> planned.Node.Key())
    |> List.choose (fun (nodeKey, planned) ->
        let distinctSetIds = planned |> List.map (fun p -> p.SetId) |> List.distinct

        if distinctSetIds.Length > 1 then
            Some(ProcessCoreWritebackError.ConflictingNodeIdentity(nodeKey, distinctSetIds))
        else
            None
    )

let private addSetPatchesFor (tableName: string) (patchLog: ProvenanceTablePatch list) =
    patchLog
    |> List.choose (
        function
        | ProvenanceTablePatch.AddLoadedSet(side, patchTableName, header, name) when patchTableName = tableName ->
            Some(side, header, name)
        | _ -> None
    )

let private addConnectionPatchesFor (tableName: string) (patchLog: ProvenanceTablePatch list) =
    patchLog
    |> List.choose (
        function
        | ProvenanceTablePatch.AddLoadedConnection(patchTableName, _, _, inputSetId, outputSetId) when
            patchTableName = tableName
            ->
            Some(inputSetId, outputSetId)
        | _ -> None
    )

let private removeConnectionPatchesFor (tableName: string) (patchLog: ProvenanceTablePatch list) =
    patchLog
    |> List.choose (
        function
        | ProvenanceTablePatch.RemoveLoadedConnection(patchTableName, _, _, inputSetId, outputSetId) when
            patchTableName = tableName
            ->
            Some(inputSetId, outputSetId)
        | _ -> None
    )

let private preflight
    (index: ProcessCoreWritebackIndex)
    (session: ProvenanceSession)
    (arc: ARC)
    : Result<Plan, ProcessCoreWritebackError list> =
    let structuralErrors = validateGraph index arc @ validateLayers index session

    if not structuralErrors.IsEmpty then
        Error structuralErrors
    else
        let initialLayer =
            session.Layers
            |> List.find (fun layer -> layer.Model.Source.Id = index.InitialSourceId)

        let tableName = initialLayer.Model.Source.Name

        let updateResults =
            session.PatchLog
            |> List.choose (
                function
                | ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, anchor, _, _, _) ->
                    Some(resolveUpdatePatch index session arc propertyValueId anchor)
                | _ -> None
            )

        let updateErrors =
            updateResults
            |> List.collect (
                function
                | Error e -> e
                | Ok _ -> []
            )

        let structureResult =
            planAdditions
                index
                initialLayer.Model.InputSets
                initialLayer.Model.OutputSets
                initialLayer.Model.Connections
                (addSetPatchesFor tableName session.PatchLog)
                (addConnectionPatchesFor tableName session.PatchLog)
            |> Result.bind (fun additionRows ->
                planRemovals
                    arc
                    index
                    initialLayer.Model.InputSets
                    initialLayer.Model.OutputSets
                    initialLayer.Model.Connections
                    (removeConnectionPatchesFor tableName session.PatchLog)
                |> Result.map (fun replacedProcesses -> additionRows, replacedProcesses)
            )

        let structureErrors, structureValue =
            match structureResult with
            | Error errors -> errors, None
            | Ok(additionRows, replacedProcesses) ->
                let allPlannedRows = additionRows @ (replacedProcesses |> List.collect snd)
                validateNodeIdentity allPlannedRows, Some(additionRows, replacedProcesses)

        let unhandledPatches =
            session.PatchLog
            |> List.choose (
                function
                | ProvenanceTablePatch.UpdatePropertyValue _ -> None
                | ProvenanceTablePatch.AddLoadedSet(_, patchTableName, _, _)
                | ProvenanceTablePatch.AddLoadedConnection(patchTableName, _, _, _, _)
                | ProvenanceTablePatch.RemoveLoadedConnection(patchTableName, _, _, _, _) when
                    patchTableName = tableName
                    ->
                    None
                | ProvenanceTablePatch.AddLoadedSet _
                | ProvenanceTablePatch.AddLoadedConnection _
                | ProvenanceTablePatch.RemoveLoadedConnection _ ->
                    // Not the initial layer: deferred to a session-created layer's own
                    // materialization (Task 8), never resolved against these pools.
                    None
                | other -> Some(ProcessCoreWritebackError.InvalidPatchTarget(sprintf "%A" other))
            )

        let allErrors = updateErrors @ structureErrors @ unhandledPatches

        if not allErrors.IsEmpty then
            Error(allErrors |> List.distinct)
        else
            let updates =
                updateResults
                |> List.choose (
                    function
                    | Ok(Some update) -> Some update
                    | _ -> None
                )
                |> List.distinctBy (fun update -> update.PropertyValueId)

            let additionRows, replacedProcesses = structureValue.Value

            Ok {
                Updates = updates
                Dataset = tryResolveDataset index.LoadedTable.DatasetPath arc
                LoadedTableName = tableName
                ReplacedProcesses = replacedProcesses
                NewRows = additionRows
            }

let private nodesOf (row: PlannedRow) =
    [ row.Input; row.Output ] |> List.choose id |> List.map (fun p -> p.Node)

let private ioOf (row: PlannedRow) =
    (row.Input |> Option.map (fun p -> p.Node) |> Option.toList),
    (row.Output |> Option.map (fun p -> p.Node) |> Option.toList)

let private apply (arc: ARC) (plan: Plan) : ProcessCoreWritebackSummary =
    let touchedAnnotations =
        System.Collections.Generic.HashSet<Annotation>(HashIdentity.Reference)

    for update in plan.Updates do
        for annotation in update.Annotations do
            applyValue update.Value update.Unit annotation
            touchedAnnotations.Add annotation |> ignore

    let mutable addedProcesses = 0
    let mutable removedProcesses = 0
    let mutable addedNodes = 0

    let existingNodeKeys =
        System.Collections.Generic.HashSet<string>(arc.AllNodes() |> Seq.map (fun node -> node.Key()))

    let countNewNodes (row: PlannedRow) =
        for node in nodesOf row do
            if existingNodeKeys.Add(node.Key()) then
                addedNodes <- addedNodes + 1

    match plan.Dataset with
    | None -> ()
    | Some dataset ->
        for original, rows in plan.ReplacedProcesses do
            match rows with
            | [] ->
                removeProcess dataset original
                removedProcesses <- removedProcesses + 1
            | first :: rest ->
                countNewNodes first
                let inputs, outputs = ioOf first
                replaceProcessIO inputs outputs original

                for row in rest do
                    countNewNodes row
                    let clone = cloneProcessShell original
                    addProcess dataset clone
                    let inputs, outputs = ioOf row
                    replaceProcessIO inputs outputs clone
                    addedProcesses <- addedProcesses + 1

        for row in plan.NewRows do
            countNewNodes row
            let proc = Process(plan.LoadedTableName)
            addProcess dataset proc
            let inputs, outputs = ioOf row
            replaceProcessIO inputs outputs proc
            addedProcesses <- addedProcesses + 1

    {
        UpdatedAnnotations = touchedAnnotations.Count
        AddedAnnotations = 0
        AddedNodes = addedNodes
        AddedProcesses = addedProcesses
        RemovedProcesses = removedProcesses
    }

let writeBack
    (index: ProcessCoreWritebackIndex)
    (session: ProvenanceSession)
    (arc: ARC)
    : Result<ProcessCoreWritebackSummary, ProcessCoreWritebackError list> =
    preflight index session arc |> Result.map (apply arc)
