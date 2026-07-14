module Swate.Components.Shared.ProvenanceGrouping.ProcessCoreConverter

open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreGraph

let private endpointHeader (_side: ProvenanceSide) (node: IONode) : ProvenanceIOHeader =
    let kind =
        match node with
        | SampleNode _ -> ProcessCoreKinds.sampleEndpoint
        | DataNode _ -> ProcessCoreKinds.dataEndpoint

    let additionalType =
        match node with
        | SampleNode sample -> sample.AdditionalType
        | DataNode data -> data.AdditionalType

    let text =
        additionalType
        |> Option.map (fun value -> value.Trim())
        |> Option.filter (fun value -> value <> "")
        |> Option.defaultValue kind.Label

    { Kind = kind; Text = text }

let private sideText side =
    match side with
    | ProvenanceSide.Input -> "input"
    | ProvenanceSide.Output -> "output"

let private setId
    (source: ProvenanceSourceRef)
    (side: ProvenanceSide)
    (header: ProvenanceIOHeader)
    (node: IONode)
    : ProvenanceSetId =
    $"{source.Id}::set:{sideText side}:{header.Kind.Id}:{header.Text}:{node.Key()}"

let private isBlankEndpoint (node: IONode) =
    System.String.IsNullOrWhiteSpace(nodeDisplayName node)

let private collectEndpoints
    (source: ProvenanceSourceRef)
    (datasetPath: string list)
    (selected: (int * Process) list)
    : Map<ProvenanceSetId, ProvenanceSet> *
      Map<ProvenanceSetId, ProcessCoreEndpointLocation> *
      ProcessCoreConversionWarning list
    =

    let mutable sets: Map<ProvenanceSetId, ProvenanceSet> = Map.empty
    let mutable headers: Map<ProvenanceSetId, ProvenanceIOHeader> = Map.empty

    let mutable occurrences: Map<ProvenanceSetId, ProcessCoreEndpointOccurrence list> =
        Map.empty

    let mutable warnings: ProcessCoreConversionWarning list = []

    let visit (procLocation: ProcessCoreProcessLocation) (side: ProvenanceSide) (position: int) (node: IONode) =
        if isBlankEndpoint node then
            warnings <-
                ProcessCoreConversionWarning.BlankEndpoint(procLocation, side, position)
                :: warnings
        else
            let header = endpointHeader side node
            let id = setId source side header node

            let occurrence = {
                Process = procLocation
                Side = side
                Position = position
                Node = nodeLocation node
            }

            occurrences <-
                occurrences
                |> Map.add id (occurrence :: (occurrences |> Map.tryFind id |> Option.defaultValue []))

            headers <- headers |> Map.add id header

            if not (sets.ContainsKey id) then
                sets <-
                    sets
                    |> Map.add id {
                        Id = id
                        Source = source
                        Header = header
                        Name = nodeDisplayName node
                        PropertyValueIds = []
                        InheritedPropertyValueIds = Map.empty
                    }

    for processIndex, proc in selected do
        let procLocation = processLocation datasetPath processIndex proc

        for position in 0 .. proc.Inputs.Count - 1 do
            visit procLocation ProvenanceSide.Input position proc.Inputs.[position]

        for position in 0 .. proc.Outputs.Count - 1 do
            visit procLocation ProvenanceSide.Output position proc.Outputs.[position]

    let endpointLocations =
        occurrences
        |> Map.map (fun id occurrenceList -> {
            Header = headers.[id]
            Occurrences = List.rev occurrenceList
        })

    sets, endpointLocations, List.rev warnings

let private connectionPairs (proc: Process) : (int * IONode * int * IONode) list =
    if proc.Inputs.Count = proc.Outputs.Count then
        [
            for index in 0 .. proc.Inputs.Count - 1 -> index, proc.Inputs.[index], index, proc.Outputs.[index]
        ]
    else
        [
            for inputIndex in 0 .. proc.Inputs.Count - 1 do
                for outputIndex in 0 .. proc.Outputs.Count - 1 do
                    yield inputIndex, proc.Inputs.[inputIndex], outputIndex, proc.Outputs.[outputIndex]
        ]

let private collectConnections
    (source: ProvenanceSourceRef)
    (datasetPath: string list)
    (selected: (int * Process) list)
    : Map<ProvenanceConnectionId, ProvenanceConnection> * Map<ProvenanceConnectionId, ProcessCoreConnectionLocation> =

    let mutable connections: Map<ProvenanceConnectionId, ProvenanceConnection> =
        Map.empty

    let mutable locations: Map<ProvenanceConnectionId, ProcessCoreConnectionLocation> =
        Map.empty

    for processIndex, proc in selected do
        let procLocation = processLocation datasetPath processIndex proc

        for inputPosition, inputNode, outputPosition, outputNode in connectionPairs proc do
            if not (isBlankEndpoint inputNode) && not (isBlankEndpoint outputNode) then
                let inputSetId =
                    setId source ProvenanceSide.Input (endpointHeader ProvenanceSide.Input inputNode) inputNode

                let outputSetId =
                    setId source ProvenanceSide.Output (endpointHeader ProvenanceSide.Output outputNode) outputNode

                let connectionId =
                    $"{source.Id}::connection:{processIndex}:{inputPosition}:{outputPosition}"

                let connection = {
                    Id = connectionId
                    Source = source
                    ProcessId = Some(processId procLocation)
                    ProcessName = Some proc.Name
                    InputSetId = inputSetId
                    OutputSetId = outputSetId
                }

                let location = {
                    Process = procLocation
                    InputPosition = inputPosition
                    OutputPosition = outputPosition
                    InputSetId = inputSetId
                    OutputSetId = outputSetId
                }

                connections <- connections |> Map.add connectionId connection
                locations <- locations |> Map.add connectionId location

    connections, locations

let private partitionBySide
    (sets: Map<ProvenanceSetId, ProvenanceSet>)
    (endpointLocations: Map<ProvenanceSetId, ProcessCoreEndpointLocation>)
    (side: ProvenanceSide)
    =
    sets
    |> Map.filter (fun id _ ->
        match endpointLocations.TryFind id with
        | Some location -> location.Occurrences |> List.exists (fun occurrence -> occurrence.Side = side)
        | None -> false
    )

let fromArc
    (location: ProcessCoreTableLocation)
    (arc: ARC)
    : Result<ProcessCoreConversionResult, ProcessCoreConversionError> =
    if location.DatasetPath.IsEmpty then
        Error ProcessCoreConversionError.EmptyDatasetPath
    else
        match resolveDatasetMatches location.DatasetPath arc with
        | [] -> Error(ProcessCoreConversionError.DatasetNotFound location.DatasetPath)
        | _ :: _ :: _ -> Error(ProcessCoreConversionError.AmbiguousDatasetPath location.DatasetPath)
        | [ dataset ] ->
            let selected =
                dataset.Processes
                |> Seq.mapi (fun index proc -> index, proc)
                |> Seq.filter (fun (_, proc) -> proc.Name = location.TableName)
                |> Seq.toList

            if selected.IsEmpty then
                Error(ProcessCoreConversionError.ProcessGroupNotFound location)
            else
                let source = sourceRef location

                let sets, endpointLocations, warnings =
                    collectEndpoints source location.DatasetPath selected

                let inputSets = partitionBySide sets endpointLocations ProvenanceSide.Input
                let outputSets = partitionBySide sets endpointLocations ProvenanceSide.Output

                let connections, connectionLocations =
                    collectConnections source location.DatasetPath selected

                let model =
                    {
                        Source = source
                        PropertyValues = Map.empty
                        InputSets = inputSets
                        OutputSets = outputSets
                        Connections = connections
                    }
                    |> ProvenanceModel.refreshInheritedOutputProperties

                Ok {
                    Model = model
                    Index = {
                        LoadedTable = location
                        InitialSourceId = source.Id
                        ArcFingerprint = graphFingerprint arc
                        EndpointLocations = endpointLocations
                        PropertyValueLocations = Map.empty
                        ConnectionLocations = connectionLocations
                    }
                    Warnings = warnings
                }
