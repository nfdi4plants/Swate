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

// ── Annotation normalization ────────────────────────────────────────────────

let private blankAnnotationName (annotation: Annotation) =
    System.String.IsNullOrWhiteSpace annotation.Name

let private categoryFromAnnotation (annotation: Annotation) : ProvenanceTerm = {
    Name = annotation.Name
    TermSource = None
    TermAccession = annotation.NameTAN
}

let private kindForNodeAnnotation (annotation: Annotation) =
    match annotation.AdditionalType |> Option.map (fun value -> value.ToLowerInvariant()) with
    | Some "characteristicvalue" -> ProcessCoreKinds.characteristic
    | Some "factorvalue" -> ProcessCoreKinds.factor
    | Some "parametervalue" -> ProcessCoreKinds.parameter
    | Some "component" -> ProcessCoreKinds.componentKind
    | _ -> ProcessCoreKinds.additionalProperty

let private processLocationKey (location: ProcessCoreProcessLocation) =
    let path = String.concat "/" location.DatasetPath
    $"{path}:{location.ProcessIndex}"

let private nonBlankSetIds (source: ProvenanceSourceRef) (side: ProvenanceSide) (nodes: IONode seq) =
    nodes
    |> Seq.filter (fun node -> not (isBlankEndpoint node))
    |> Seq.map (fun node -> setId source side (endpointHeader side node) node)
    |> Seq.distinct
    |> Seq.toList

/// One not-yet-collapsed converted property occurrence. Exact-duplicate
/// candidates (same header/value/unit/anchor context/targets) collapse into
/// one `ProvenancePropertyValue` while every source `Location` is retained
/// in the writeback index.
type private PropertyCandidate = {
    Header: ProvenancePropertyHeader
    Value: ProvenanceValue
    Unit: ProvenanceTerm option
    Anchor: ProvenanceWritebackAnchor
    TargetInputSetIds: ProvenanceSetId list
    TargetOutputSetIds: ProvenanceSetId list
    Location: ProcessCoreAnnotationLocation
}

let private collectPropertyCandidates
    (source: ProvenanceSourceRef)
    (datasetPath: string list)
    (selected: (int * Process) list)
    : PropertyCandidate list * ProcessCoreConversionWarning list =

    let mutable candidates: PropertyCandidate list = []
    let mutable warnings: ProcessCoreConversionWarning list = []

    let addNodeAnnotations
        (procLocation: ProcessCoreProcessLocation)
        (side: ProvenanceSide)
        (node: IONode)
        (targetSetId: ProvenanceSetId)
        =
        let owner = ProcessCoreAnnotationOwner.NodeAdditionalProperty(nodeLocation node)

        node
        |> nodeAdditionalProperties
        |> Seq.iteri (fun position annotation ->
            if blankAnnotationName annotation then
                warnings <- ProcessCoreConversionWarning.BlankAnnotationName(owner, position) :: warnings
            else
                let header = {
                    Kind = kindForNodeAnnotation annotation
                    Category = categoryFromAnnotation annotation
                }

                let anchor = {
                    Source = source
                    ProcessId = Some(processId procLocation)
                    ProcessName = Some procLocation.ExpectedName
                    Header = header
                    InputNames =
                        if side = ProvenanceSide.Input then
                            [ nodeDisplayName node ]
                        else
                            []
                    OutputNames =
                        if side = ProvenanceSide.Output then
                            [ nodeDisplayName node ]
                        else
                            []
                }

                let location = {
                    Owner = owner
                    Position = position
                    Fingerprint = annotationFingerprint annotation
                }

                candidates <-
                    {
                        Header = header
                        Value = valueFromAnnotation annotation
                        Unit = unitFromAnnotation annotation
                        Anchor = anchor
                        TargetInputSetIds = if side = ProvenanceSide.Input then [ targetSetId ] else []
                        TargetOutputSetIds = if side = ProvenanceSide.Output then [ targetSetId ] else []
                        Location = location
                    }
                    :: candidates
        )

    let addProcessLevelAnnotations
        (owner: ProcessCoreAnnotationOwner)
        (procLocation: ProcessCoreProcessLocation)
        (kind: ProvenanceKind)
        (targetInputSetIds: ProvenanceSetId list)
        (targetOutputSetIds: ProvenanceSetId list)
        (annotations: Annotation seq)
        =
        annotations
        |> Seq.iteri (fun position annotation ->
            if blankAnnotationName annotation then
                warnings <- ProcessCoreConversionWarning.BlankAnnotationName(owner, position) :: warnings
            elif targetInputSetIds.IsEmpty && targetOutputSetIds.IsEmpty then
                warnings <-
                    ProcessCoreConversionWarning.PropertyWithoutEndpoint(procLocation, annotation.Name)
                    :: warnings
            else
                let header = {
                    Kind = kind
                    Category = categoryFromAnnotation annotation
                }

                let anchor = {
                    Source = source
                    ProcessId = Some(processId procLocation)
                    ProcessName = Some procLocation.ExpectedName
                    Header = header
                    InputNames = []
                    OutputNames = []
                }

                let location = {
                    Owner = owner
                    Position = position
                    Fingerprint = annotationFingerprint annotation
                }

                candidates <-
                    {
                        Header = header
                        Value = valueFromAnnotation annotation
                        Unit = unitFromAnnotation annotation
                        Anchor = anchor
                        TargetInputSetIds = targetInputSetIds
                        TargetOutputSetIds = targetOutputSetIds
                        Location = location
                    }
                    :: candidates
        )

    for processIndex, proc in selected do
        let procLocation = processLocation datasetPath processIndex proc

        for position in 0 .. proc.Inputs.Count - 1 do
            let node = proc.Inputs.[position]

            if not (isBlankEndpoint node) then
                let id =
                    setId source ProvenanceSide.Input (endpointHeader ProvenanceSide.Input node) node

                addNodeAnnotations procLocation ProvenanceSide.Input node id

        for position in 0 .. proc.Outputs.Count - 1 do
            let node = proc.Outputs.[position]

            if not (isBlankEndpoint node) then
                let id =
                    setId source ProvenanceSide.Output (endpointHeader ProvenanceSide.Output node) node

                addNodeAnnotations procLocation ProvenanceSide.Output node id

        let targetInputSetIds = nonBlankSetIds source ProvenanceSide.Input proc.Inputs
        let targetOutputSetIds = nonBlankSetIds source ProvenanceSide.Output proc.Outputs

        addProcessLevelAnnotations
            (ProcessCoreAnnotationOwner.ProcessParameterValue procLocation)
            procLocation
            ProcessCoreKinds.parameter
            targetInputSetIds
            targetOutputSetIds
            proc.ParameterValue

        match proc.ExecutesProtocol with
        | Some recipe ->
            addProcessLevelAnnotations
                (ProcessCoreAnnotationOwner.RecipeComponent procLocation)
                procLocation
                ProcessCoreKinds.componentKind
                targetInputSetIds
                targetOutputSetIds
                recipe.Components
        | None -> ()

    List.rev candidates, List.rev warnings

/// Walks upstream from one selected input node, gathering non-selected
/// producer processes' parameters/components and their own input nodes'
/// annotations. All collected values target the originally selected input
/// set - the boundary node's own annotations are already collected as part
/// of the loaded input itself and are never revisited here.
let private walkPreviousContext
    (source: ProvenanceSourceRef)
    (datasetEntriesList: DatasetEntry list)
    (selectedKeys: Set<string>)
    (inputSetId: ProvenanceSetId)
    (startNode: IONode)
    : PropertyCandidate list * ProcessCoreConversionWarning list =

    let mutable candidates: PropertyCandidate list = []
    let mutable warnings: ProcessCoreConversionWarning list = []
    let visited = System.Collections.Generic.HashSet<string>()

    let addAnnotations
        (owner: ProcessCoreAnnotationOwner)
        (previousSource: ProvenanceSourceRef)
        (procLocation: ProcessCoreProcessLocation)
        (producerName: string)
        (inputNames: string list)
        (kindSelector: Annotation -> ProvenanceKind)
        (annotations: Annotation seq)
        =
        annotations
        |> Seq.iteri (fun position annotation ->
            if blankAnnotationName annotation then
                warnings <- ProcessCoreConversionWarning.BlankAnnotationName(owner, position) :: warnings
            else
                let header = {
                    Kind = kindSelector annotation
                    Category = categoryFromAnnotation annotation
                }

                let anchor = {
                    Source = previousSource
                    ProcessId = Some(processId procLocation)
                    ProcessName = Some producerName
                    Header = header
                    InputNames = inputNames
                    OutputNames = []
                }

                let location = {
                    Owner = owner
                    Position = position
                    Fingerprint = annotationFingerprint annotation
                }

                candidates <-
                    {
                        Header = header
                        Value = valueFromAnnotation annotation
                        Unit = unitFromAnnotation annotation
                        Anchor = anchor
                        TargetInputSetIds = [ inputSetId ]
                        TargetOutputSetIds = []
                        Location = location
                    }
                    :: candidates
        )

    let rec walk (node: IONode) =
        for producer in node.GetOutputOf() |> Seq.toList do
            match producer.ProcessOf with
            | Some ownerDataset ->
                match
                    datasetEntriesList
                    |> List.tryFind (fun entry -> obj.ReferenceEquals(entry.Dataset, ownerDataset))
                with
                | Some entry ->
                    match
                        entry.Dataset.Processes
                        |> Seq.tryFindIndex (fun candidate -> obj.ReferenceEquals(candidate, producer))
                    with
                    | Some processIndex ->
                        let procLocation = processLocation entry.Path processIndex producer
                        let key = processLocationKey procLocation

                        if visited.Add key then
                            if not (selectedKeys.Contains key) then
                                let previousSource =
                                    sourceRef {
                                        DatasetPath = procLocation.DatasetPath
                                        TableName = producer.Name
                                    }

                                addAnnotations
                                    (ProcessCoreAnnotationOwner.ProcessParameterValue procLocation)
                                    previousSource
                                    procLocation
                                    producer.Name
                                    []
                                    (fun _ -> ProcessCoreKinds.parameter)
                                    producer.ParameterValue

                                match producer.ExecutesProtocol with
                                | Some recipe ->
                                    addAnnotations
                                        (ProcessCoreAnnotationOwner.RecipeComponent procLocation)
                                        previousSource
                                        procLocation
                                        producer.Name
                                        []
                                        (fun _ -> ProcessCoreKinds.componentKind)
                                        recipe.Components
                                | None -> ()

                                for upstreamInput in producer.Inputs do
                                    addAnnotations
                                        (ProcessCoreAnnotationOwner.NodeAdditionalProperty(nodeLocation upstreamInput))
                                        previousSource
                                        procLocation
                                        producer.Name
                                        [ nodeDisplayName upstreamInput ]
                                        kindForNodeAnnotation
                                        (nodeAdditionalProperties upstreamInput)

                            for upstreamInput in producer.Inputs do
                                walk upstreamInput
                    | None -> ()
                | None -> ()
            | None -> ()

    walk startNode
    List.rev candidates, List.rev warnings

let private collectPreviousContextCandidates
    (arc: ARC)
    (source: ProvenanceSourceRef)
    (datasetPath: string list)
    (selected: (int * Process) list)
    : PropertyCandidate list * ProcessCoreConversionWarning list =

    let entries = datasetEntries arc

    let selectedKeys =
        selected
        |> List.map (fun (index, proc) -> processLocationKey (processLocation datasetPath index proc))
        |> Set.ofList

    let mutable candidates: PropertyCandidate list = []
    let mutable warnings: ProcessCoreConversionWarning list = []

    for _, proc in selected do
        for position in 0 .. proc.Inputs.Count - 1 do
            let node = proc.Inputs.[position]

            if not (isBlankEndpoint node) then
                let inputSetId =
                    setId source ProvenanceSide.Input (endpointHeader ProvenanceSide.Input node) node

                let nodeCandidates, nodeWarnings =
                    walkPreviousContext source entries selectedKeys inputSetId node

                candidates <- candidates @ nodeCandidates
                warnings <- warnings @ nodeWarnings

    candidates, warnings

let private dedupKey (candidate: PropertyCandidate) =
    candidate.Header,
    candidate.Value,
    candidate.Unit,
    candidate.Anchor.Source.Id,
    candidate.Anchor.ProcessId,
    candidate.Anchor.ProcessName,
    List.sort candidate.TargetInputSetIds,
    List.sort candidate.TargetOutputSetIds

let private buildPropertyValues (source: ProvenanceSourceRef) (candidates: PropertyCandidate list) =
    let groups = candidates |> List.groupBy dedupKey

    let mutable propertyValues: Map<ProvenancePropertyValueId, ProvenancePropertyValue> =
        Map.empty

    let mutable inputAttachments: Map<ProvenanceSetId, ProvenancePropertyValueId list> =
        Map.empty

    let mutable outputAttachments: Map<ProvenanceSetId, ProvenancePropertyValueId list> =
        Map.empty

    let mutable locations: Map<ProvenancePropertyValueId, ProcessCoreAnnotationLocation list> =
        Map.empty

    groups
    |> List.iteri (fun ordinal (_, group) ->
        let first = List.head group
        let id = $"{source.Id}::property:{ordinal + 1}"

        let propertyValue = {
            Id = id
            Header = first.Header
            Value = first.Value
            Unit = first.Unit
            Origin = ProvenancePropertyOrigin.Real first.Anchor
        }

        propertyValues <- propertyValues |> Map.add id propertyValue

        locations <-
            locations
            |> Map.add id (group |> List.map (fun candidate -> candidate.Location))

        for targetSetId in first.TargetInputSetIds do
            inputAttachments <-
                inputAttachments
                |> Map.add targetSetId (id :: (inputAttachments |> Map.tryFind targetSetId |> Option.defaultValue []))

        for targetSetId in first.TargetOutputSetIds do
            outputAttachments <-
                outputAttachments
                |> Map.add targetSetId (id :: (outputAttachments |> Map.tryFind targetSetId |> Option.defaultValue []))
    )

    let attach
        (attachments: Map<ProvenanceSetId, ProvenancePropertyValueId list>)
        (sets: Map<ProvenanceSetId, ProvenanceSet>)
        =
        sets
        |> Map.map (fun id set -> {
            set with
                PropertyValueIds = attachments |> Map.tryFind id |> Option.defaultValue [] |> List.rev
        })

    propertyValues, locations, attach inputAttachments, attach outputAttachments

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

                let sets, endpointLocations, endpointWarnings =
                    collectEndpoints source location.DatasetPath selected

                let connections, connectionLocations =
                    collectConnections source location.DatasetPath selected

                let propertyCandidates, propertyWarnings =
                    collectPropertyCandidates source location.DatasetPath selected

                let previousCandidates, previousWarnings =
                    collectPreviousContextCandidates arc source location.DatasetPath selected

                let propertyValues, propertyLocations, attachInputs, attachOutputs =
                    buildPropertyValues source (propertyCandidates @ previousCandidates)

                let inputSets =
                    partitionBySide sets endpointLocations ProvenanceSide.Input |> attachInputs

                let outputSets =
                    partitionBySide sets endpointLocations ProvenanceSide.Output |> attachOutputs

                let model =
                    {
                        Source = source
                        PropertyValues = propertyValues
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
                        PropertyValueLocations = propertyLocations
                        ConnectionLocations = connectionLocations
                    }
                    Warnings = endpointWarnings @ propertyWarnings @ previousWarnings
                }
