module Swate.Components.Page.ARCObjectExplorer.GraphExplorer.GraphExplorerNodes

open System
open Swate.Components.Shared
open Swate.Components.Page.ARCObjectExplorer.GraphExplorer.Model
open Swate.Components.Page.FileExplorer.Types


type private ProcessEndpointValue =
    | MaterialEndpoint of materialRole: string * material: Material
    | DataEndpoint of dataRole: string * data: Data

type private GraphFileItemAppearance = {
    Icon: FileItemIcon
    IconTone: FileItemIconTone option
}

let private normalizeText (value: string) =
    if String.IsNullOrWhiteSpace value then
        None
    else
        Some value

let private asOptionalText (value: string) =
    normalizeText value

let private asOptionalTextOption (value: string option) =
    value
    |> Option.bind normalizeText

let private graphKindForDataset =
    function
    | ARCDatasets.Assay -> GraphExplorerNodeKind.Assay
    | ARCDatasets.Study -> GraphExplorerNodeKind.Study
    | ARCDatasets.Workflow -> GraphExplorerNodeKind.Workflow
    | ARCDatasets.Run -> GraphExplorerNodeKind.Run

let private graphKindLabel = GraphExplorerNodeKind.label
let private arcKindForGraphKind = GraphExplorerNodeKind.toArcExplorerNodeKind

let private row label value = [ label, value ]

let private optionalRowFromOption label (value: string option) =
    value
    |> asOptionalTextOption
    |> Option.map (fun text -> [ label, text ])
    |> Option.defaultValue []

let private optionalRow label value =
    value
    |> Some
    |> optionalRowFromOption label

let private optionalOptionRow label value =
    optionalRowFromOption label value

let private addMeta (nodeId: string) (meta: GraphNodeMeta) (metaById: Map<string, GraphNodeMeta>) =
    metaById |> Map.add nodeId meta

let private mapFoldIndexed
    (folder: int -> 'state -> 'item -> 'mapped * 'state)
    (state: 'state)
    (items: 'item list)
    : 'mapped list * 'state =
    let rec loop index currentState mappedRev remainingItems =
        match remainingItems with
        | [] ->
            List.rev mappedRev, currentState
        | item :: tail ->
            let mappedItem, nextState =
                folder index currentState item

            loop (index + 1) nextState (mappedItem :: mappedRev) tail

    loop 0 state [] items

let private createGroupNode
    (groupNodeId: string)
    (groupLabel: string)
    (children: ArcExplorerNode list)
    =
    ArcExplorerNode.create (
        groupNodeId,
        groupLabel,
        ArcExplorerNodeKind.Group,
        isSelectable = false,
        children = children
    )

let private optionalGroupNode
    (groupNodeId: string)
    (groupLabel: string)
    (children: ArcExplorerNode list)
    =
    if List.isEmpty children then
        None
    else
        Some(createGroupNode groupNodeId groupLabel children)

let private propertyValueDisplayName (propertyValue: PropertyValue) =
    propertyValue.name
    |> asOptionalText
    |> Option.defaultValue propertyValue.id

let private formatPropertyValue (propertyValue: PropertyValue) =
    let propertyLabel = propertyValueDisplayName propertyValue

    match propertyValue.value with
    | Some propertyContent when String.IsNullOrWhiteSpace propertyContent |> not ->
        $"{propertyLabel}={propertyContent}"
    | _ ->
        propertyLabel

let private optionalRowsFromPropertyList label (values: PropertyValue list) =
    if List.isEmpty values then
        []
    else
        values
        |> List.map formatPropertyValue
        |> String.concat "; "
        |> row label

let private optionalRowsFromPropertyValues label (values: PropertyValue array) =
    values
    |> Array.toList
    |> optionalRowsFromPropertyList label

let private optionalRowsFromPropertyOption label (value: PropertyValue option) =
    value
    |> Option.toList
    |> optionalRowsFromPropertyList label

let private propertyValueRows
    (ownerLabel: string)
    (fieldLabel: string)
    (index: int)
    (propertyValue: PropertyValue)
    =
    [
        yield! row "Owner Kind" ownerLabel
        yield! row "Source Field" fieldLabel
        yield! row "Index" (string (index + 1))
        yield! row "Type" propertyValue.type'
        yield! row "Id" propertyValue.id
        yield! row "Name" propertyValue.name
        yield! optionalOptionRow "Value" propertyValue.value
        yield! optionalOptionRow "Unit" propertyValue.unit
        yield! optionalOptionRow "Name TAN" propertyValue.nameTAN
        yield! optionalOptionRow "Value TAN" propertyValue.valueTAN
        yield! optionalOptionRow "Unit TAN" propertyValue.unitTAN
        yield! optionalOptionRow "Additional Type" propertyValue.additionalType
    ]

let private propertyValueNode
    (ownerNodeId: string)
    (fieldKey: string)
    (fieldLabel: string)
    (ownerLabel: string)
    (ownerTag: GraphPropertyValueOwnerTag)
    (isReference: bool)
    (index: int)
    (propertyValue: PropertyValue)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let nodeId = $"{ownerNodeId}:{fieldKey}:{index}"
    let nodeLabel = propertyValueDisplayName propertyValue

    let node =
        ArcExplorerNode.create (
            nodeId,
            nodeLabel,
            arcKindForGraphKind GraphExplorerNodeKind.PropertyValue,
            isReference = isReference
        )

    let relationshipRole = if isReference then "Reference" else "Canonical"

    let meta = {
        Tag = Some(GraphNodeTag.PropertyValue ownerTag)
        GraphKind = GraphExplorerNodeKind.PropertyValue
        KindLabel = graphKindLabel GraphExplorerNodeKind.PropertyValue
        RoleLabel = relationshipRole
        Description = Some $"{fieldLabel} entry attached to {ownerLabel.ToLowerInvariant()}."
        Rows = propertyValueRows ownerLabel fieldLabel index propertyValue
        CaseExamples = [
            "PropertyValue", "{ id = \"prop:region\"; type' = \"PropertyValue\"; name = \"Region\"; value = Some \"Field-01\" }"
        ]
    }

    node, addMeta nodeId meta metaById

let private propertyValueGroupNode
    (ownerNodeId: string)
    (groupKey: string)
    (groupLabel: string)
    (fieldLabel: string)
    (ownerLabel: string)
    (ownerTag: GraphPropertyValueOwnerTag)
    (isReference: bool)
    (values: PropertyValue list)
    (metaById: Map<string, GraphNodeMeta>)
    =
    match values with
    | [] ->
        None, metaById
    | values ->
        let valueNodes, nextState =
            values
            |> mapFoldIndexed (fun index state propertyValue ->
                propertyValueNode
                    ownerNodeId
                    groupKey
                    fieldLabel
                    ownerLabel
                    ownerTag
                    isReference
                    index
                    propertyValue
                    state) metaById

        let groupNode =
            createGroupNode
                $"{ownerNodeId}:group:{groupKey}"
                groupLabel
                valueNodes

        Some groupNode, nextState

let private arcObjectCaseExamples = [
    "Arc", "Arc([| { path = \"C:/example/arc-graph\"; Datasets = ... } |])"
    "Datasets", "Datasets([| { type' = Study; identifier = \"study-drought-response\"; ... } |])"
    "Protocols", "Protocols([| { name = Some \"LC-MS Measurement\"; processes = [| ... |] } |])"
    "FormalParameters", "FormalParameters([| { name = Some \"Instrument Model\"; defaultValue = Some \"Q Exactive\" } |])"
    "Processes", "Processes([| { name = \"Extract metabolites\"; inputs = [| ... |]; outputs = [| ... |] } |])"
]

let private datasetCaseExamples = [
    "Study", "Dataset(type = Study, identifier = \"study-drought-response\", ...)"
    "Assay", "Dataset(type = Assay, identifier = \"assay-metabolomics\", ...)"
    "Workflow", "Dataset(type = Workflow, identifier = \"workflow-extraction\", ...)"
    "Run", "Dataset(type = Run, identifier = \"run-week-01\", ...)"
]

let private processCaseExamples = [
    "LabProcess", "LabProcess(name = \"Extract metabolites\", inputs = [| ... |], outputs = [| ... |], Materials = [| ... |], Data = [| ... |])"
    "Input Bundle", "{ Materials = [| { Sources = [| ... |]; Samples = [| ... |] } |]; Data = [| ... |] }"
]

let private processTypeCaseExamples = [
    "Source", "Materials = [| { Sources = [| { id = \"source:leaf-a\"; ... } |]; Samples = [||] } |]"
    "Sample", "Materials = [| { Sources = [||]; Samples = [| { id = \"sample:leaf-a\"; ... } |] } |]"
    "Files", "Data = [| { Files = [| { path = \"assays/metabolomics/feature-table.tsv\"; ... } |]; FragmentSelector = [||] } |]"
    "FragmentSelector", "Data = [| { Files = [||]; FragmentSelector = [| { selector = Some \"mz=100-1000\"; ... } |] } |]"
]

let private flattenDatasetKinds (datasetKinds: DatasetKinds) =
    [
        yield! datasetKinds.Studies |> Array.toList
        yield! datasetKinds.Assays |> Array.toList
        yield! datasetKinds.Workflows |> Array.toList
        yield! datasetKinds.Runs |> Array.toList
    ]

let private collectArcGraphs (arcObjects: ARCObjects list) =
    arcObjects
    |> List.choose (function
        | ARCObjects.Arc arcs -> Some (arcs |> Array.toList)
        | _ -> None)
    |> List.concat

let private materialKindsEndpoints (materialKindsValues: MaterialKinds array) =
    materialKindsValues
    |> Array.toList
    |> List.collect (fun materialKinds ->
        [
            yield!
                materialKinds.Sources
                |> Array.toList
                |> List.map (fun material -> MaterialEndpoint("Source", material))

            yield!
                materialKinds.Samples
                |> Array.toList
                |> List.map (fun material -> MaterialEndpoint("Sample", material))
        ])

let private dataKindsEndpoints (dataKindsValues: DataKinds array) =
    dataKindsValues
    |> Array.toList
    |> List.collect (fun dataKinds ->
        [
            yield!
                dataKinds.Files
                |> Array.toList
                |> List.map (fun data -> DataEndpoint("File", data))

            yield!
                dataKinds.FragmentSelector
                |> Array.toList
                |> List.map (fun data -> DataEndpoint("Fragment Selector", data))
        ])

let private processTypeEndpoints (processType: ProcessType) =
    materialKindsEndpoints processType.Materials
    @ dataKindsEndpoints processType.Data

let private inferMaterialRole (material: Material) =
    let normalizedType =
        material.type'
            .Trim()
            .ToLowerInvariant()

    if normalizedType.Contains("source") then
        "Source"
    elif normalizedType.Contains("sample") then
        "Sample"
    else
        "Material"

let private inferDataRole (data: Data) =
    let hasSelector =
        data.selector
        |> Option.exists (fun selector -> String.IsNullOrWhiteSpace selector |> not)

    let additionalType =
        data.additionalType
        |> Option.defaultValue ""
        |> fun value -> value.Trim().ToLowerInvariant()

    if hasSelector || additionalType.Contains("fragmentselector") || additionalType.Contains("fragment selector") then
        "Fragment Selector"
    else
        "File"

let private processLevelEndpoints (processValue: LabProcess) =
    let materialEndpoints =
        processValue.Materials
        |> Array.toList
        |> List.map (fun material -> MaterialEndpoint(inferMaterialRole material, material))

    let dataEndpoints =
        processValue.Data
        |> Array.toList
        |> List.map (fun data -> DataEndpoint(inferDataRole data, data))

    materialEndpoints @ dataEndpoints

let private endpointIdentity =
    function
    | MaterialEndpoint(_, material) ->
        String.Join(
            "|",
            [
                "material"
                material.id
                material.name
                material.type'
            ]
        )
    | DataEndpoint(_, data) ->
        String.Join(
            "|",
            [
                "data"
                (data.id |> Option.defaultValue "")
                data.path
                (data.selector |> Option.defaultValue "")
                data.type'
            ]
        )

let private unassociatedProcessEndpoints (processValue: LabProcess) =
    let ioEndpointKeys =
        [
            yield!
                processValue.inputs
                |> Array.toList
                |> List.collect processTypeEndpoints

            yield!
                processValue.outputs
                |> Array.toList
                |> List.collect processTypeEndpoints
        ]
        |> List.map endpointIdentity
        |> Set.ofList

    processLevelEndpoints processValue
    |> List.filter (fun endpoint -> ioEndpointKeys.Contains(endpointIdentity endpoint) |> not)
    |> List.distinctBy endpointIdentity

let private processEndpointDisplayName =
    function
    | MaterialEndpoint(materialRole, material) ->
        material.name
        |> asOptionalText
        |> Option.defaultValue $"Unnamed {materialRole.ToLowerInvariant()} material"
    | DataEndpoint(dataRole, data) ->
        data.id
        |> asOptionalTextOption
        |> Option.orElseWith (fun () -> asOptionalText data.path)
        |> Option.defaultValue $"Unnamed {dataRole.ToLowerInvariant()} data"

let private processEndpointPresentation =
    function
    | MaterialEndpoint(materialRole, _) ->
        "Material", materialRole, GraphExplorerNodeKind.Material
    | DataEndpoint(dataRole, _) ->
        "Data", dataRole, GraphExplorerNodeKind.Data

let private processEndpointValueType =
    function
    | MaterialEndpoint _ -> GraphProcessEndpointValueType.Material
    | DataEndpoint _ -> GraphProcessEndpointValueType.Data

let private processEndpointRows =
    function
    | MaterialEndpoint(materialRole, material) ->
        [
            yield! row "Value Type" "Material"
            yield! row "Material DU Cases" "Sources | Samples"
            yield! row "Data DU Cases" "Files | FragmentSelector"
            yield! row "Material Role" materialRole
            yield! row "Material Name" material.name
            yield! row "Material Type" material.type'
            yield! row "Material Id" material.id
            yield! optionalOptionRow "Additional Type" material.additionalType
            yield! optionalRowsFromPropertyValues "Additional Property" material.additionalProperty
        ]
    | DataEndpoint(dataRole, data) ->
        [
            yield! row "Value Type" "Data"
            yield! row "Material DU Cases" "Sources | Samples"
            yield! row "Data DU Cases" "Files | FragmentSelector"
            yield! row "Data Role" dataRole
            yield! row "Path" data.path
            yield! row "Data Type" data.type'
            yield! optionalOptionRow "Data Id" data.id
            yield! optionalOptionRow "Additional Type" data.additionalType
            yield! optionalOptionRow "Selector" data.selector
            yield! optionalOptionRow "Selector Format" data.selectorFormat
            yield! optionalOptionRow "Encoding Format" data.encodingFormat
            yield! optionalRowsFromPropertyValues "Additional Property" data.additionalProperty
        ]

let private flattenNodes (nodes: ArcExplorerNode list) =
    let rec collect (node: ArcExplorerNode) =
        node :: (node.children |> List.collect collect)

    nodes |> List.collect collect

let private createReferenceChildren
    (categoryNodeId: string)
    (nodes: ArcExplorerNode list)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let toReferenceMeta (meta: GraphNodeMeta) = {
        meta with
            RoleLabel = "Reference"
            Description =
                meta.Description
                |> Option.orElse (Some "Reference entry in a top-level index layer.")
    }

    let rec createReferenceNode
        (sourceNode: ArcExplorerNode)
        (state: Map<string, GraphNodeMeta>)
        =
        let referenceId = $"{categoryNodeId}:ref:{sourceNode.id}"

        let childReferenceNodes, afterChildren =
            sourceNode.children
            |> List.fold (fun (childrenRev, currentState) child ->
                let childReferenceNode, nextState = createReferenceNode child currentState
                childReferenceNode :: childrenRev, nextState) ([], state)
            |> fun (childrenRev, currentState) -> List.rev childrenRev, currentState

        let referenceNode =
            ArcExplorerNode.create (
                referenceId,
                sourceNode.name,
                sourceNode.kind,
                path = sourceNode.path,
                previewTarget = sourceNode.previewTarget,
                isSelectable = sourceNode.isSelectable,
                isReference = true,
                sampleSummary = sourceNode.sampleSummary,
                relatedSamples = sourceNode.relatedSamples,
                isLfs = sourceNode.isLfs,
                children = childReferenceNodes
            )

        let nextState =
            match Map.tryFind sourceNode.id afterChildren with
            | Some meta -> addMeta referenceId (toReferenceMeta meta) afterChildren
            | None -> afterChildren

        referenceNode, nextState

    nodes
    |> List.distinctBy _.id
    |> List.sortBy (fun node -> node.name.ToLowerInvariant())
    |> List.fold (fun (childrenRev, state) node ->
        let referenceNode, nextState = createReferenceNode node state
        referenceNode :: childrenRev, nextState) ([], metaById)
    |> fun (childrenRev, state) -> List.rev childrenRev, state

let private createCategoryLayer
    (categoryKey: string)
    (categoryLabel: string)
    (description: string)
    (caseExamples: (string * string) list)
    (candidates: ArcExplorerNode list)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let categoryNodeId = $"graph:{categoryKey}"
    let referenceChildren, metaAfterChildren =
        createReferenceChildren categoryNodeId candidates metaById

    let categoryNode =
        ArcExplorerNode.create (
            categoryNodeId,
            categoryLabel,
            arcKindForGraphKind GraphExplorerNodeKind.Arc,
            children = referenceChildren
        )

    let categoryMeta = {
        Tag = None
        GraphKind = GraphExplorerNodeKind.Arc
        KindLabel = graphKindLabel GraphExplorerNodeKind.Arc
        RoleLabel = "Canonical"
        Description = Some description
        Rows = [
            yield! row "Entries" (string referenceChildren.Length)
        ]
        CaseExamples = caseExamples
    }

    categoryNode, addMeta categoryNodeId categoryMeta metaAfterChildren

type private CategoryLayerDescriptor = {
    LayerKey: string
    LayerLabel: string
    Description: string
    CaseExamples: (string * string) list
    MatchTag: GraphNodeTag
}

let private categoryLayerDescriptors = [
    {
        LayerKey = "datasets"
        LayerLabel = "Datasets"
        Description = "Top-level index of all dataset nodes."
        CaseExamples = datasetCaseExamples
        MatchTag = GraphNodeTag.Dataset
    }
    {
        LayerKey = "protocols"
        LayerLabel = "Protocols"
        Description = "Top-level index of all protocol nodes."
        CaseExamples = [ "Protocol", "Protocol(name = Some \"LC-MS Measurement\", processes = [| ... |])" ]
        MatchTag = GraphNodeTag.Protocol
    }
    {
        LayerKey = "formal-parameters"
        LayerLabel = "FormalParameters"
        Description = "Top-level index of all formal parameter nodes."
        CaseExamples = [ "FormalParameter", "FormalParameter(name = Some \"Instrument Model\", defaultValue = Some \"Q Exactive\")" ]
        MatchTag = GraphNodeTag.FormalParameter
    }
    {
        LayerKey = "processes"
        LayerLabel = "Processes"
        Description = "Top-level index of all process nodes."
        CaseExamples = processCaseExamples
        MatchTag = GraphNodeTag.Process
    }
    {
        LayerKey = "materials"
        LayerLabel = "Materials"
        Description = "Top-level index of all material endpoint nodes."
        CaseExamples = [
            "Source", "Sources = [| { id = \"source:leaf-a\"; ... } |]"
            "Sample", "Samples = [| { id = \"sample:leaf-a\"; ... } |]"
        ]
        MatchTag = GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material
    }
    {
        LayerKey = "Data"
        LayerLabel = "Data"
        Description = "Top-level index of all data endpoint nodes."
        CaseExamples = [
            "Data(Files)", "Files = [| { path = \"assays/metabolomics/feature-table.tsv\"; ... } |]"
            "Data(FragmentSelector)", "FragmentSelector = [| { selector = Some \"mz=100-1000\"; ... } |]"
        ]
        MatchTag = GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data
    }
]

let private collectCategoryCandidates
    (descriptors: CategoryLayerDescriptor list)
    (nodes: ArcExplorerNode list)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let candidateIndex =
        descriptors
        |> List.map (fun descriptor -> descriptor.MatchTag, [])
        |> Map.ofList

    nodes
    |> List.fold (fun acc node ->
        let maybeTag =
            metaById
            |> Map.tryFind node.id
            |> Option.bind _.Tag

        match maybeTag with
        | Some tag when Map.containsKey tag acc ->
            let existingNodes = acc |> Map.find tag
            acc |> Map.add tag (node :: existingNodes)
        | _ ->
            acc) candidateIndex
    |> Map.map (fun _ candidatesRev -> List.rev candidatesRev)

let private createCategoryLayers
    (descriptors: CategoryLayerDescriptor list)
    (candidatesByTag: Map<GraphNodeTag, ArcExplorerNode list>)
    (metaById: Map<string, GraphNodeMeta>)
    =
    descriptors
    |> List.fold (fun (layersRev, state) descriptor ->
        let candidates =
            candidatesByTag
            |> Map.tryFind descriptor.MatchTag
            |> Option.defaultValue []

        let layerNode, nextState =
            createCategoryLayer
                descriptor.LayerKey
                descriptor.LayerLabel
                descriptor.Description
                descriptor.CaseExamples
                candidates
                state

        layerNode :: layersRev, nextState) ([], metaById)
    |> fun (layersRev, state) -> List.rev layersRev, state

let private processEndpointNode
    (processNodeId: string)
    (directionKey: string)
    (directionLabel: string)
    (processTypeIndex: int option)
    (endpointIndex: int)
    (endpoint: ProcessEndpointValue)
    (isReference: bool)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let processTypeSegment =
        processTypeIndex
        |> Option.map string
        |> Option.defaultValue "na"

    let nodeId = $"{processNodeId}:{directionKey}:{processTypeSegment}:{endpointIndex}"
    let displayName = processEndpointDisplayName endpoint
    let _, subtypeLabel, graphKind = processEndpointPresentation endpoint
    let relationshipRole = if isReference then "Reference" else "Canonical"
    let endpointRole = $"{directionLabel} ({relationshipRole})"

    let endpointPropertyValues, propertyOwnerTag, propertyOwnerLabel =
        match endpoint with
        | MaterialEndpoint(_, material) ->
            material.additionalProperty |> Array.toList,
            GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Material,
            "Material Endpoint"
        | DataEndpoint(_, data) ->
            data.additionalProperty |> Array.toList,
            GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Data,
            "Data Endpoint"

    let additionalPropertyGroupNode, metaAfterPropertyValues =
        propertyValueGroupNode
            nodeId
            "additional-property"
            "Additional Properties"
            "additionalProperty"
            propertyOwnerLabel
            propertyOwnerTag
            isReference
            endpointPropertyValues
            metaById

    let node =
        ArcExplorerNode.create (
            nodeId,
            displayName,
            arcKindForGraphKind graphKind,
            isReference = isReference,
            children = (additionalPropertyGroupNode |> Option.toList)
        )

    let meta =
        {
            Tag = Some(GraphNodeTag.ProcessEndpoint (processEndpointValueType endpoint))
            GraphKind = graphKind
            KindLabel = graphKindLabel graphKind
            RoleLabel = endpointRole
            Description = Some $"{subtypeLabel} endpoint in process {directionLabel.ToLowerInvariant()} bundle."
            Rows = [
                yield! row "Endpoint Direction" directionLabel
                yield!
                    processTypeIndex
                    |> Option.map (fun index -> row "ProcessType Index" (string (index + 1)))
                    |> Option.defaultValue []
                yield! row "Endpoint Index" (string (endpointIndex + 1))
                yield! row "Relationship Role" relationshipRole
                yield! processEndpointRows endpoint
            ]
            CaseExamples = processTypeCaseExamples
        }

    node, addMeta nodeId meta metaAfterPropertyValues

type private IndexedProcessEndpoint = {
    ProcessTypeIndex: int option
    EndpointIndex: int
    Value: ProcessEndpointValue
}

let private indexedEndpointsFromProcessTypes (processTypes: ProcessType array) =
    processTypes
    |> Array.toList
    |> List.mapi (fun processTypeIndex processType ->
        processTypeEndpoints processType
        |> List.mapi (fun endpointIndex endpoint -> {
            ProcessTypeIndex = Some processTypeIndex
            EndpointIndex = endpointIndex
            Value = endpoint
        }))
    |> List.concat

let private indexedEndpointsFromEndpoints (endpoints: ProcessEndpointValue list) =
    endpoints
    |> List.mapi (fun endpointIndex endpoint -> {
        ProcessTypeIndex = None
        EndpointIndex = endpointIndex
        Value = endpoint
    })

let private processEndpointGroupNode
    (processNodeId: string)
    (directionKey: string)
    (directionLabel: string)
    (groupLabel: string)
    (splitByValueType: bool)
    (indexedEndpoints: IndexedProcessEndpoint list)
    (isReference: bool)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let endpointNodesWithKinds, nextState =
        indexedEndpoints
        |> List.fold (fun (nodesRev, state) indexedEndpoint ->
            let node, updatedState =
                processEndpointNode
                    processNodeId
                    directionKey
                    directionLabel
                    indexedEndpoint.ProcessTypeIndex
                    indexedEndpoint.EndpointIndex
                    indexedEndpoint.Value
                    isReference
                    state

            (indexedEndpoint.Value, node) :: nodesRev, updatedState) ([], metaById)
        |> fun (nodesRev, state) -> List.rev nodesRev, state

    let endpointCount = endpointNodesWithKinds.Length

    let groupedChildren =
        if splitByValueType then
            let materialNodes =
                endpointNodesWithKinds
                |> List.choose (fun (endpoint, node) ->
                    match endpoint with
                    | MaterialEndpoint _ -> Some node
                    | DataEndpoint _ -> None)

            let dataNodes =
                endpointNodesWithKinds
                |> List.choose (fun (endpoint, node) ->
                    match endpoint with
                    | MaterialEndpoint _ -> None
                    | DataEndpoint _ -> Some node)

            [
                yield! optionalGroupNode $"{processNodeId}:{directionKey}:group:material" "Material" materialNodes |> Option.toList
                yield! optionalGroupNode $"{processNodeId}:{directionKey}:group:data" "Data" dataNodes |> Option.toList
            ]
        else
            endpointNodesWithKinds
            |> List.map snd

    let groupNode =
        createGroupNode
            $"{processNodeId}:{directionKey}"
            groupLabel
            groupedChildren

    groupNode, nextState, endpointCount

let private processEndpointGroupNodeFromProcessTypes
    (processNodeId: string)
    (directionKey: string)
    (directionLabel: string)
    (groupLabel: string)
    (processTypes: ProcessType array)
    (isReference: bool)
    (metaById: Map<string, GraphNodeMeta>)
    =
    processEndpointGroupNode
        processNodeId
        directionKey
        directionLabel
        groupLabel
        true
        (indexedEndpointsFromProcessTypes processTypes)
        isReference
        metaById

let private processEndpointGroupNodeFromEndpoints
    (processNodeId: string)
    (directionKey: string)
    (directionLabel: string)
    (groupLabel: string)
    (endpoints: ProcessEndpointValue list)
    (isReference: bool)
    (metaById: Map<string, GraphNodeMeta>)
    =
    processEndpointGroupNode
        processNodeId
        directionKey
        directionLabel
        groupLabel
        false
        (indexedEndpointsFromEndpoints endpoints)
        isReference
        metaById

let private processDisplayName (processValue: LabProcess) =
    processValue.name
    |> asOptionalText
    |> Option.defaultValue "Unnamed process"

let private summarizeProcessNames (processes: LabProcess list) =
    processes
    |> List.map processDisplayName
    |> String.concat "; "

let private formatIntendedUse (intendedUse: (DefinedTerm * string) option) =
    intendedUse
    |> Option.map (fun (definedTerm, intendedUseValue) ->
        let termLabel =
            definedTerm.name
            |> asOptionalText
            |> Option.defaultValue definedTerm.id

        match intendedUseValue |> asOptionalText with
        | Some value -> $"{termLabel}: {value}"
        | None -> termLabel)

let private formalParameterNode
    (parentNodeId: string)
    (index: int)
    (formalParameter: FormalParameter)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let parameterName =
        formalParameter.name
        |> asOptionalTextOption
        |> Option.defaultValue $"Formal Parameter {index + 1}"

    let nodeId = $"{parentNodeId}:formal-parameter:{index}"

    let rows = [
        yield! row "Name" parameterName
        yield! row "Type" formalParameter.type'
        yield! row "Id" formalParameter.id
        yield! optionalOptionRow "Name TAN" formalParameter.nameTAN
        yield! optionalOptionRow "Default Value" formalParameter.defaultValue
    ]

    let meta = {
        Tag = Some GraphNodeTag.FormalParameter
        GraphKind = GraphExplorerNodeKind.FormalParameter
        KindLabel = graphKindLabel GraphExplorerNodeKind.FormalParameter
        RoleLabel = "Canonical"
        Description = Some "Formal parameter definition from the graph model."
        Rows = rows
        CaseExamples = [
            "FormalParameter", "{ id = \"fp:instrument\"; name = Some \"Instrument Model\"; defaultValue = Some \"Q Exactive\" }"
        ]
    }

    let node =
        ArcExplorerNode.create (
            nodeId,
            parameterName,
            arcKindForGraphKind GraphExplorerNodeKind.FormalParameter
        )

    node, addMeta nodeId meta metaById

let private processNode
    (parentNodeId: string)
    (index: int)
    (processValue: LabProcess)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let processName = processDisplayName processValue
    let processNodeId = $"{parentNodeId}:process:{sanitizeSegment processName}:{index}"

    let inputNodeGroup, metaAfterInput, inputEndpointCount =
        processEndpointGroupNodeFromProcessTypes processNodeId "input" "Input" "Inputs" processValue.inputs true metaById

    let outputNodeGroup, metaAfterOutput, outputEndpointCount =
        processEndpointGroupNodeFromProcessTypes processNodeId "output" "Output" "Outputs" processValue.outputs false metaAfterInput

    let unassociatedEndpoints =
        unassociatedProcessEndpoints processValue

    let unassociatedNodeGroup, metaAfterUnassociated, unassociatedEndpointCount =
        processEndpointGroupNodeFromEndpoints
            processNodeId
            "unassociated"
            "Unassociated"
            "Unassociated"
            unassociatedEndpoints
            false
            metaAfterOutput

    let parameterValueGroupNode, metaAfterParameterValues =
        propertyValueGroupNode
            processNodeId
            "parameter-value"
            "Parameter Values"
            "parameterValue"
            "Process"
            GraphPropertyValueOwnerTag.Process
            false
            (processValue.parameterValue |> Array.toList)
            metaAfterUnassociated

    let rows = [
        yield! row "Name" processName
        yield! row "Type" "LabProcess"
        yield! row "Object Type" processValue.type'
        yield! row "Executes Protocol" processValue.executesProtocol
        yield! row "Input Bundles" (string processValue.inputs.Length)
        yield! row "Output Bundles" (string processValue.outputs.Length)
        yield! row "Process Materials" (string processValue.Materials.Length)
        yield! row "Process Data" (string processValue.Data.Length)
        yield! row "Input Endpoints" (string inputEndpointCount)
        yield! row "Output Endpoints" (string outputEndpointCount)
        yield! row "Unassociated Endpoints" (string unassociatedEndpointCount)
        yield! optionalOptionRow "Process Id" processValue.id
        yield! optionalOptionRow "Additional Type" processValue.additionalType
        yield! optionalRowsFromPropertyValues "Parameter Value" processValue.parameterValue
    ]

    let processMeta = {
        Tag = Some GraphNodeTag.Process
        GraphKind = GraphExplorerNodeKind.Process
        KindLabel = graphKindLabel GraphExplorerNodeKind.Process
        RoleLabel = "Canonical"
        Description = Some "LabProcess with input, output, and unassociated endpoint groups."
        Rows = rows
        CaseExamples = processCaseExamples @ processTypeCaseExamples
    }

    let node =
        ArcExplorerNode.create (
            processNodeId,
            processName,
            arcKindForGraphKind GraphExplorerNodeKind.Process,
            children =
                [
                    inputNodeGroup
                    outputNodeGroup
                    unassociatedNodeGroup
                    yield! parameterValueGroupNode |> Option.toList
                ]
        )

    node, addMeta processNodeId processMeta metaAfterParameterValues

let private protocolNode
    (parentNodeId: string)
    (index: int)
    (protocol: LabProtocol)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let protocolName =
        protocol.name
        |> asOptionalTextOption
        |> Option.defaultValue $"Protocol {index + 1}"

    let nodeId = $"{parentNodeId}:protocol:{sanitizeSegment protocolName}:{index}"

    let formalParameterNodes, metaAfterParameters =
        protocol.parameters
        |> Array.toList
        |> mapFoldIndexed (fun parameterIndex state parameter ->
            formalParameterNode nodeId parameterIndex parameter state) metaById

    let processNodes, metaAfterProcesses =
        protocol.processes
        |> Array.toList
        |> mapFoldIndexed (fun processIndex state processValue ->
            processNode nodeId processIndex processValue state) metaAfterParameters

    let additionalPropertyGroupNode, metaAfterAdditionalProperties =
        propertyValueGroupNode
            nodeId
            "additional-property"
            "Additional Properties"
            "additionalProperty"
            "Protocol"
            GraphPropertyValueOwnerTag.Protocol
            false
            (protocol.additionalProperty |> Option.toList)
            metaAfterProcesses

    let children =
        [
            yield! optionalGroupNode $"{nodeId}:group:formal-parameters" "Formal Parameters" formalParameterNodes |> Option.toList
            yield! optionalGroupNode $"{nodeId}:group:processes" "Processes" processNodes |> Option.toList
            yield! additionalPropertyGroupNode |> Option.toList
        ]

    let processNames = protocol.processes |> Array.toList |> summarizeProcessNames

    let rows = [
        yield! row "Name" protocolName
        yield! row "Type" protocol.type'
        yield! optionalOptionRow "Id" protocol.id
        yield! optionalOptionRow "Additional Type" protocol.additionalType
        yield! optionalOptionRow "Description" protocol.description
        yield! optionalOptionRow "Intended Use" (formatIntendedUse protocol.intendedUse)
        yield! optionalRowsFromPropertyOption "Additional Property" protocol.additionalProperty
        yield! optionalOptionRow "Version" protocol.version
        yield! optionalOptionRow "URL" protocol.url
        yield! row "Formal Parameters" (string protocol.parameters.Length)
        yield! row "Processes" (string protocol.processes.Length)
        yield! optionalRow "Process Names" processNames
    ]

    let meta = {
        Tag = Some GraphNodeTag.Protocol
        GraphKind = GraphExplorerNodeKind.Protocol
        KindLabel = graphKindLabel GraphExplorerNodeKind.Protocol
        RoleLabel = "Canonical"
        Description = Some "Protocol metadata with formal parameters and process chain."
        Rows = rows
        CaseExamples = arcObjectCaseExamples @ processCaseExamples
    }

    let node =
        ArcExplorerNode.create (
            nodeId,
            protocolName,
            arcKindForGraphKind GraphExplorerNodeKind.Protocol,
            children = children
        )

    node, addMeta nodeId meta metaAfterAdditionalProperties

let private datasetDisplayName (dataset: Dataset) =
    dataset.name
    |> asOptionalTextOption
    |> Option.defaultValue dataset.identifier

let rec private datasetNode
    (scopeNodeId: string)
    (index: int)
    (dataset: Dataset)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let kindLabel = dataset.type'.ToString()
    let datasetKind = graphKindForDataset dataset.type'
    let title = datasetDisplayName dataset
    let nodeId = $"{scopeNodeId}:{kindLabel.ToLowerInvariant()}:{sanitizeSegment dataset.identifier}:{index}"

    let aboutProtocols = dataset.about |> Array.toList

    let aboutProtocolNames =
        aboutProtocols
        |> List.map (fun protocol ->
            protocol.name
            |> asOptionalTextOption
            |> Option.defaultValue "Unnamed protocol")
        |> String.concat "; "

    let protocolNodes, metaAfterProtocols =
        aboutProtocols
        |> mapFoldIndexed (fun protocolIndex state protocol ->
            protocolNode nodeId protocolIndex protocol state) metaById

    let hasPartDatasets = dataset.hasPart |> Array.toList

    let partDatasetGroups, metaAfterParts =
        groupedDatasetNodes (nodeId + ":has-part") hasPartDatasets metaAfterProtocols

    let additionalPropertyGroupNode, metaAfterAdditionalProperties =
        propertyValueGroupNode
            nodeId
            "additional-property"
            "Additional Properties"
            "additionalProperty"
            "Dataset"
            GraphPropertyValueOwnerTag.Dataset
            false
            (dataset.additionalProperty |> Array.toList)
            metaAfterParts

    let children =
        [
            yield! optionalGroupNode $"{nodeId}:group:protocols" "Protocols" protocolNodes |> Option.toList
            yield! optionalGroupNode $"{nodeId}:group:has-part" "DataSets" partDatasetGroups |> Option.toList
            yield! additionalPropertyGroupNode |> Option.toList
        ]

    let rows = [
        yield! row "Identifier" dataset.identifier
        yield! row "Dataset Type" kindLabel
        yield! row "Type Tag" dataset.additionalType
        yield! optionalOptionRow "Name" dataset.name
        yield! optionalOptionRow "Description" dataset.description
        yield! row "About Protocols" (string aboutProtocols.Length)
        yield! optionalRow "About Protocol Names" aboutProtocolNames
        yield! row "DataSets" (string dataset.hasPart.Length)
        yield! optionalRowsFromPropertyValues "Additional Property" dataset.additionalProperty
        yield! row "Dataset Id" dataset.id
    ]

    let meta = {
        Tag = Some GraphNodeTag.Dataset
        GraphKind = datasetKind
        KindLabel = graphKindLabel datasetKind
        RoleLabel = "Canonical"
        Description = Some "Dataset entry in the graph model with protocol references and nested parts."
        Rows = rows
        CaseExamples = datasetCaseExamples
    }

    let node =
        ArcExplorerNode.create (
            nodeId,
            title,
            arcKindForGraphKind datasetKind,
            children = children
        )

    node, addMeta nodeId meta metaAfterAdditionalProperties

and private groupedDatasetNodes
    (scopeNodeId: string)
    (datasets: Dataset list)
    (metaById: Map<string, GraphNodeMeta>)
    =
    datasets
    |> List.groupBy (fun dataset -> dataset.type')
    |> List.sortBy (fun (kind, _) -> kind)
    |> List.fold (fun (groupsRev, state) (datasetKind, groupedDatasets) ->
        let sortedDatasets =
            groupedDatasets
            |> List.sortBy (fun dataset -> (datasetDisplayName dataset).ToLowerInvariant())

        let datasetNodes, nextState =
            sortedDatasets
            |> mapFoldIndexed (fun datasetIndex state' dataset ->
                datasetNode scopeNodeId datasetIndex dataset state') state

        let groupNode =
            createGroupNode
                $"{scopeNodeId}:group:{(datasetKind.ToString()).ToLowerInvariant()}"
                (datasetKind.ToString())
                datasetNodes

        groupNode :: groupsRev, nextState) ([], metaById)
    |> fun (groupsRev, state) -> List.rev groupsRev, state

let private arcNode
    (arcIndex: int)
    (model: ARCGraph)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let rootName =
        if String.IsNullOrWhiteSpace model.path then
            $"ARC Graph {arcIndex + 1}"
        else
            PathHelpers.getNameFromPath model.path

    let rootPath =
        model.path
        |> asOptionalText
        |> Option.map PathHelpers.normalizePath

    let arcNodeId = $"graph:arc:{arcIndex}"
    let datasets = flattenDatasetKinds model.Datasets

    let groupNodes, metaAfterGroups =
        groupedDatasetNodes arcNodeId datasets metaById

    let arcNode =
        ArcExplorerNode.create (
            arcNodeId,
            rootName,
            arcKindForGraphKind GraphExplorerNodeKind.Arc,
            path = rootPath,
            children = groupNodes
        )

    let arcRows = [
        yield! optionalRow "Path" model.path
        yield! row "Datasets" (string datasets.Length)
    ]

    let arcMeta = {
        Tag = None
        GraphKind = GraphExplorerNodeKind.Arc
        KindLabel = graphKindLabel GraphExplorerNodeKind.Arc
        RoleLabel = "Canonical"
        Description = Some "ARC node in the Storybook graph explorer."
        Rows = arcRows
        CaseExamples = arcObjectCaseExamples @ datasetCaseExamples
    }

    arcNode, addMeta arcNodeId arcMeta metaAfterGroups

let toArcExplorerNodesWithMetaFromArcObjects (arcObjects: ARCObjects list) =
    let arcGraphs = collectArcGraphs arcObjects

    let arcNodes, metaAfterArcs =
        arcGraphs
        |> mapFoldIndexed (fun arcIndex state model ->
            arcNode arcIndex model state) Map.empty

    let canonicalRoots =
        arcNodes

    let allNodeId = "graph:all"

    let allNode =
        ArcExplorerNode.create (
            allNodeId,
            "ARCs",
            arcKindForGraphKind GraphExplorerNodeKind.Arc,
            children = canonicalRoots
        )

    let allMeta = {
        Tag = None
        GraphKind = GraphExplorerNodeKind.Arc
        KindLabel = graphKindLabel GraphExplorerNodeKind.Arc
        RoleLabel = "Canonical"
        Description = Some "Top layer containing all ARC-object roots."
        Rows = [
            yield! row "Entries" (string canonicalRoots.Length)
            yield! row "Arc Entries" (string arcNodes.Length)
        ]
        CaseExamples = [
            "ARCObjects", "Arc | Datasets | Protocols | FormalParameters | Processes (non-Arc entries are ignored in canonical tree)"
        ]
    }

    let canonicalNodes =
        flattenNodes canonicalRoots

    let candidatesByTag =
        collectCategoryCandidates
            categoryLayerDescriptors
            canonicalNodes
            metaAfterArcs

    let allWithMetaById =
        metaAfterArcs
        |> addMeta allNodeId allMeta

    let categoryLayerNodes, metaById =
        createCategoryLayers
            categoryLayerDescriptors
            candidatesByTag
            allWithMetaById

    allNode :: categoryLayerNodes, metaById

let private graphAppearanceByKind : Map<GraphExplorerNodeKind, GraphFileItemAppearance> =
    Map.ofList [
        GraphExplorerNodeKind.Arc,
        {
            Icon = FileItemIcon.Folder
            IconTone = Some FileItemIconTone.BaseMuted
        }
        GraphExplorerNodeKind.Group,
        {
            Icon = FileItemIcon.Folder
            IconTone = Some FileItemIconTone.BaseSubtle
        }
        GraphExplorerNodeKind.Study,
        {
            Icon = FileItemIcon.Document
            IconTone = Some FileItemIconTone.Secondary
        }
        GraphExplorerNodeKind.Assay,
        {
            Icon = FileItemIcon.Document
            IconTone = Some FileItemIconTone.Success
        }
        GraphExplorerNodeKind.Workflow,
        {
            Icon = FileItemIcon.Document
            IconTone = Some FileItemIconTone.Primary
        }
        GraphExplorerNodeKind.Run,
        {
            Icon = FileItemIcon.Document
            IconTone = Some FileItemIconTone.Warning
        }
        GraphExplorerNodeKind.Protocol,
        {
            Icon = FileItemIcon.Document
            IconTone = Some FileItemIconTone.Primary
        }
        GraphExplorerNodeKind.Process,
        {
            Icon = FileItemIcon.Document
            IconTone = Some FileItemIconTone.Warning
        }
        GraphExplorerNodeKind.FormalParameter,
        {
            Icon = FileItemIcon.Table
            IconTone = Some FileItemIconTone.Info
        }
        GraphExplorerNodeKind.Material,
        {
            Icon = FileItemIcon.Tag
            IconTone = Some FileItemIconTone.BaseMuted
        }
        GraphExplorerNodeKind.Data,
        {
            Icon = FileItemIcon.Database
            IconTone = Some FileItemIconTone.Accent
        }
        GraphExplorerNodeKind.PropertyValue,
        {
            Icon = FileItemIcon.Table
            IconTone = Some FileItemIconTone.Info
        }
    ]

let private graphAppearanceForKind (graphKind: GraphExplorerNodeKind) : GraphFileItemAppearance =
    graphAppearanceByKind
    |> Map.tryFind graphKind
    |> Option.defaultValue {
        Icon = FileItemIcon.Document
        IconTone = None
    }

let private graphKindForNodeId
    (nodeId: string)
    (fallbackArcKind: ArcExplorerNodeKind)
    (nodeMetaById: Map<string, GraphNodeMeta>)
    =
    nodeMetaById
    |> Map.tryFind nodeId
    |> Option.map _.GraphKind
    |> Option.defaultValue (GraphExplorerNodeKind.ofArcExplorerNodeKind fallbackArcKind)

let private fileItemForGraphNode
    (nodeMetaById: Map<string, GraphNodeMeta>)
    (createItem: string -> string option -> FileItemIcon -> FileItem)
    (node: ArcExplorerNode)
    =
    let graphKind = graphKindForNodeId node.id node.kind nodeMetaById
    let appearance = graphAppearanceForKind graphKind

    {
        createItem node.name node.path appearance.Icon with
            Id = node.id
            ItemType = graphKindLabel graphKind
            IconTone = appearance.IconTone
            IsLFS = node.isLfs
            Selectable = node.isSelectable
    }

let rec private toGraphFileItem
    (nodeMetaById: Map<string, GraphNodeMeta>)
    (node: ArcExplorerNode)
    =
    let graphKind = graphKindForNodeId node.id node.kind nodeMetaById
    let children = node.children |> List.map (toGraphFileItem nodeMetaById)
    let isDirectory = graphKind = GraphExplorerNodeKind.Arc || graphKind = GraphExplorerNodeKind.Group || not (List.isEmpty children)

    if isDirectory then
        {
            fileItemForGraphNode nodeMetaById FileTree.createFolder node with
                IsExpanded = graphKind = GraphExplorerNodeKind.Arc
                Children = Some children
        }
    else
        fileItemForGraphNode nodeMetaById FileTree.createFile node

let toGraphFileItems
    (nodeMetaById: Map<string, GraphNodeMeta>)
    (nodes: ArcExplorerNode list)
    =
    nodes |> List.map (toGraphFileItem nodeMetaById)

let toArcExplorerNodesWithMetaFromArcs (models: ARCGraph list) =
    toArcExplorerNodesWithMetaFromArcObjects [ ARCObjects.Arc(models |> List.toArray) ]

let toArcExplorerNodesWithMeta (model: ARCGraph) =
    toArcExplorerNodesWithMetaFromArcs [ model ]

let toArcExplorerNodes (model: ARCGraph) =
    toArcExplorerNodesWithMeta model |> fst

