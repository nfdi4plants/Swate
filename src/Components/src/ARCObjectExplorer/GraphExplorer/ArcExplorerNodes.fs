module Swate.Components.ARCObjectExplorer.GraphExplorer.ArcExplorerNodes

open System
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model

type private ProcessEndpointValue =
    | MaterialEndpoint of materialRole: string * material: Material
    | DataEndpoint of dataRole: string * data: Data

let private asOptionalText (value: string) =
    if String.IsNullOrWhiteSpace value then
        None
    else
        Some value

let private asOptionalTextOption (value: string option) =
    value
    |> Option.bind (fun text ->
        if String.IsNullOrWhiteSpace text then
            None
        else
            Some text)

let private sanitizeIdSegment (value: string) =
    value
        .Trim()
        .ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("/", "-")
        .Replace("\\", "-")
        .Replace(":", "-")

let private nodeKindForDataset =
    function
    | ARCDatasets.Assay -> ArcExplorerNodeKind.Assay
    | ARCDatasets.Study -> ArcExplorerNodeKind.Study
    | ARCDatasets.Workflow -> ArcExplorerNodeKind.Workflow
    | ARCDatasets.Run -> ArcExplorerNodeKind.Run

let private row label value = [ label, value ]

let private optionalRow label value =
    value
    |> asOptionalText
    |> Option.map (fun text -> [ label, text ])
    |> Option.defaultValue []

let private optionalOptionRow label value =
    value
    |> asOptionalTextOption
    |> Option.map (fun text -> [ label, text ])
    |> Option.defaultValue []

let private addMeta (nodeId: string) (meta: GraphNodeMeta) (metaById: Map<string, GraphNodeMeta>) =
    metaById |> Map.add nodeId meta

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

let private optionalRowsFromPropertyValues label (values: PropertyValue array) =
    if Array.isEmpty values then
        []
    else
        values
        |> Array.map formatPropertyValue
        |> String.concat "; "
        |> row label

let private optionalRowsFromPropertyOption label (value: PropertyValue option) =
    value
    |> Option.map (fun propertyValue ->
        formatPropertyValue propertyValue
        |> row label)
    |> Option.defaultValue []

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
            ArcExplorerNodeKind.Table,
            isReference = isReference
        )

    let relationshipRole = if isReference then "Reference" else "Canonical"

    let meta = {
        Tag = Some(GraphNodeTag.PropertyValue ownerTag)
        KindLabel = "PropertyValue"
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
            |> List.mapi (fun index propertyValue -> index, propertyValue)
            |> List.fold (fun (nodesRev, state) (index, propertyValue) ->
                let node, updatedState =
                    propertyValueNode
                        ownerNodeId
                        groupKey
                        fieldLabel
                        ownerLabel
                        ownerTag
                        isReference
                        index
                        propertyValue
                        state

                node :: nodesRev, updatedState) ([], metaById)
            |> fun (nodesRev, state) -> List.rev nodesRev, state

        let groupNode =
            ArcExplorerNode.create (
                $"{ownerNodeId}:group:{groupKey}",
                groupLabel,
                ArcExplorerNodeKind.Group,
                isSelectable = false,
                children = valueNodes
            )

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
        "Material", materialRole, ArcExplorerNodeKind.Sample
    | DataEndpoint(dataRole, _) ->
        "Data", dataRole, ArcExplorerNodeKind.DataMap

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

let private hasTag (tag: GraphNodeTag) (node: ArcExplorerNode) (metaById: Map<string, GraphNodeMeta>) =
    metaById
    |> Map.tryFind node.id
    |> Option.exists (fun meta -> meta.Tag = Some tag)

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
            ArcExplorerNodeKind.Arc,
            children = referenceChildren
        )

    let categoryMeta = {
        Tag = None
        KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Arc
        RoleLabel = "Canonical"
        Description = Some description
        Rows = [
            yield! row "Entries" (string referenceChildren.Length)
        ]
        CaseExamples = caseExamples
    }

    categoryNode, addMeta categoryNodeId categoryMeta metaAfterChildren

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
    let valueTypeLabel, subtypeLabel, nodeKind = processEndpointPresentation endpoint
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
            nodeKind,
            isReference = isReference,
            children = (additionalPropertyGroupNode |> Option.toList)
        )

    let meta =
        {
            Tag = Some(GraphNodeTag.ProcessEndpoint (processEndpointValueType endpoint))
            KindLabel = valueTypeLabel
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

let private processEndpointGroupNodeFromProcessTypes
    (processNodeId: string)
    (directionKey: string)
    (directionLabel: string)
    (groupLabel: string)
    (processTypes: ProcessType array)
    (isReference: bool)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let materialEndpointNodesRev, dataEndpointNodesRev, nextState, endpointCount =
        processTypes
        |> Array.toList
        |> List.mapi (fun processTypeIndex processType -> processTypeIndex, processType)
        |> List.fold (fun (materialNodesRev, dataNodesRev, state, totalCount) (processTypeIndex, processType) ->
            processTypeEndpoints processType
            |> List.mapi (fun endpointIndex endpoint -> endpointIndex, endpoint)
            |> List.fold (fun (bundleMaterialNodesRev, bundleDataNodesRev, bundleState, bundleCount) (endpointIndex, endpoint) ->
                let node, updatedState =
                    processEndpointNode
                        processNodeId
                        directionKey
                        directionLabel
                        (Some processTypeIndex)
                        endpointIndex
                        endpoint
                        isReference
                        bundleState

                match endpoint with
                | MaterialEndpoint _ ->
                    node :: bundleMaterialNodesRev, bundleDataNodesRev, updatedState, bundleCount + 1
                | DataEndpoint _ ->
                    bundleMaterialNodesRev, node :: bundleDataNodesRev, updatedState, bundleCount + 1)
                (materialNodesRev, dataNodesRev, state, totalCount))
            ([], [], metaById, 0)
        |> fun (materialNodesRev, dataNodesRev, state, count) ->
            List.rev materialNodesRev, List.rev dataNodesRev, state, count

    let groupedChildren =
        [
            if List.isEmpty materialEndpointNodesRev |> not then
                ArcExplorerNode.create (
                    $"{processNodeId}:{directionKey}:group:material",
                    "Material",
                    ArcExplorerNodeKind.Group,
                    isSelectable = false,
                    children = materialEndpointNodesRev
                )

            if List.isEmpty dataEndpointNodesRev |> not then
                ArcExplorerNode.create (
                    $"{processNodeId}:{directionKey}:group:data",
                    "Data",
                    ArcExplorerNodeKind.Group,
                    isSelectable = false,
                    children = dataEndpointNodesRev
                )
        ]

    let groupNode =
        ArcExplorerNode.create (
            $"{processNodeId}:{directionKey}",
            groupLabel,
            ArcExplorerNodeKind.Group,
            isSelectable = false,
            children = groupedChildren
        )

    groupNode, nextState, endpointCount

let private processEndpointGroupNodeFromEndpoints
    (processNodeId: string)
    (directionKey: string)
    (directionLabel: string)
    (groupLabel: string)
    (endpoints: ProcessEndpointValue list)
    (isReference: bool)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let endpointNodes, nextState, endpointCount =
        endpoints
        |> List.mapi (fun endpointIndex endpoint -> endpointIndex, endpoint)
        |> List.fold (fun (nodesRev, state, totalCount) (endpointIndex, endpoint) ->
            let node, updatedState =
                processEndpointNode
                    processNodeId
                    directionKey
                    directionLabel
                    None
                    endpointIndex
                    endpoint
                    isReference
                    state

            node :: nodesRev, updatedState, totalCount + 1) ([], metaById, 0)
        |> fun (nodesRev, state, count) -> List.rev nodesRev, state, count

    let groupNode =
        ArcExplorerNode.create (
            $"{processNodeId}:{directionKey}",
            groupLabel,
            ArcExplorerNodeKind.Group,
            isSelectable = false,
            children = endpointNodes
        )

    groupNode, nextState, endpointCount

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
            |> asOptionalTextOption
            |> Option.defaultValue definedTerm.id

        match intendedUseValue |> asOptionalText with
        | Some value -> $"{termLabel}: {value}"
        | None -> termLabel)

let rec private formalParameterNode
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
        KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Table
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
            ArcExplorerNodeKind.Table
        )

    node, addMeta nodeId meta metaById

and private processNode
    (parentNodeId: string)
    (index: int)
    (processValue: LabProcess)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let processName = processDisplayName processValue
    let processNodeId = $"{parentNodeId}:process:{sanitizeIdSegment processName}:{index}"

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
        KindLabel = "LabProcess"
        RoleLabel = "Canonical"
        Description = Some "LabProcess with input, output, and unassociated endpoint groups."
        Rows = rows
        CaseExamples = processCaseExamples @ processTypeCaseExamples
    }

    let node =
        ArcExplorerNode.create (
            processNodeId,
            processName,
            ArcExplorerNodeKind.Run,
            children =
                [
                    inputNodeGroup
                    outputNodeGroup
                    unassociatedNodeGroup
                    yield! parameterValueGroupNode |> Option.toList
                ]
        )

    node, addMeta processNodeId processMeta metaAfterParameterValues

and private protocolNode
    (parentNodeId: string)
    (index: int)
    (protocol: LabProtocol)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let protocolName =
        protocol.name
        |> asOptionalTextOption
        |> Option.defaultValue $"Protocol {index + 1}"

    let nodeId = $"{parentNodeId}:protocol:{sanitizeIdSegment protocolName}:{index}"

    let formalParameterNodes, metaAfterParameters =
        protocol.parameters
        |> Array.toList
        |> List.mapi (fun parameterIndex parameter -> parameterIndex, parameter)
        |> List.fold (fun (nodesRev, state) (parameterIndex, parameter) ->
            let node, nextState = formalParameterNode nodeId parameterIndex parameter state
            node :: nodesRev, nextState) ([], metaById)
        |> fun (nodesRev, state) -> List.rev nodesRev, state

    let processNodes, metaAfterProcesses =
        protocol.processes
        |> Array.toList
        |> List.mapi (fun processIndex processValue -> processIndex, processValue)
        |> List.fold (fun (nodesRev, state) (processIndex, processValue) ->
            let node, nextState = processNode nodeId processIndex processValue state
            node :: nodesRev, nextState) ([], metaAfterParameters)
        |> fun (nodesRev, state) -> List.rev nodesRev, state

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
            if List.isEmpty formalParameterNodes |> not then
                ArcExplorerNode.create (
                    $"{nodeId}:group:formal-parameters",
                    "Formal Parameters",
                    ArcExplorerNodeKind.Group,
                    isSelectable = false,
                    children = formalParameterNodes
                )

            if List.isEmpty processNodes |> not then
                ArcExplorerNode.create (
                    $"{nodeId}:group:processes",
                    "Processes",
                    ArcExplorerNodeKind.Group,
                    isSelectable = false,
                    children = processNodes
                )

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
        KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Workflow
        RoleLabel = "Canonical"
        Description = Some "Protocol metadata with formal parameters and process chain."
        Rows = rows
        CaseExamples = arcObjectCaseExamples @ processCaseExamples
    }

    let node =
        ArcExplorerNode.create (
            nodeId,
            protocolName,
            ArcExplorerNodeKind.Workflow,
            children = children
        )

    node, addMeta nodeId meta metaAfterAdditionalProperties

and private datasetDisplayName (dataset: Dataset) =
    dataset.name
    |> asOptionalTextOption
    |> Option.defaultValue dataset.identifier

and private datasetNode
    (scopeNodeId: string)
    (index: int)
    (dataset: Dataset)
    (metaById: Map<string, GraphNodeMeta>)
    =
    let kindLabel = dataset.type'.ToString()
    let datasetKind = nodeKindForDataset dataset.type'
    let title = datasetDisplayName dataset
    let nodeId = $"{scopeNodeId}:{kindLabel.ToLowerInvariant()}:{sanitizeIdSegment dataset.identifier}:{index}"

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
        |> List.mapi (fun protocolIndex protocol -> protocolIndex, protocol)
        |> List.fold (fun (nodesRev, state) (protocolIndex, protocol) ->
            let node, nextState = protocolNode nodeId protocolIndex protocol state
            node :: nodesRev, nextState) ([], metaById)
        |> fun (nodesRev, state) -> List.rev nodesRev, state

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
            if List.isEmpty protocolNodes |> not then
                ArcExplorerNode.create (
                    $"{nodeId}:group:protocols",
                    "Protocols",
                    ArcExplorerNodeKind.Group,
                    isSelectable = false,
                    children = protocolNodes
                )

            if List.isEmpty partDatasetGroups |> not then
                ArcExplorerNode.create (
                    $"{nodeId}:group:has-part",
                    "Has Part",
                    ArcExplorerNodeKind.Group,
                    isSelectable = false,
                    children = partDatasetGroups
                )

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
        yield! row "Has Part" (string dataset.hasPart.Length)
        yield! optionalRowsFromPropertyValues "Additional Property" dataset.additionalProperty
        yield! row "Dataset Id" dataset.id
    ]

    let meta = {
        Tag = Some GraphNodeTag.Dataset
        KindLabel = ArcExplorerNodeKind.label datasetKind
        RoleLabel = "Canonical"
        Description = Some "Dataset entry in the graph model with protocol references and nested parts."
        Rows = rows
        CaseExamples = datasetCaseExamples
    }

    let node =
        ArcExplorerNode.create (
            nodeId,
            title,
            datasetKind,
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
            |> List.mapi (fun datasetIndex dataset -> datasetIndex, dataset)
            |> List.fold (fun (nodesRev, state') (datasetIndex, dataset) ->
                let node, updatedState = datasetNode scopeNodeId datasetIndex dataset state'
                node :: nodesRev, updatedState) ([], state)
            |> fun (nodesRev, updatedState) -> List.rev nodesRev, updatedState

        let groupNode =
            ArcExplorerNode.create (
                $"{scopeNodeId}:group:{(datasetKind.ToString()).ToLowerInvariant()}",
                datasetKind.ToString(),
                ArcExplorerNodeKind.Group,
                isSelectable = false,
                children = datasetNodes
            )

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
            ArcExplorerNodeKind.Arc,
            path = rootPath,
            children = groupNodes
        )

    let arcRows = [
        yield! optionalRow "Path" model.path
        yield! row "Datasets" (string datasets.Length)
    ]

    let arcMeta = {
        Tag = None
        KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Arc
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
        |> List.mapi (fun arcIndex model -> arcIndex, model)
        |> List.fold (fun (arcNodesRev, state) (arcIndex, model) ->
            let node, nextState = arcNode arcIndex model state
            node :: arcNodesRev, nextState) ([], Map.empty)
        |> fun (arcNodesRev, state) -> List.rev arcNodesRev, state

    let canonicalRoots =
        arcNodes

    let allNodeId = "graph:all"

    let allNode =
        ArcExplorerNode.create (
            allNodeId,
            "ARCs",
            ArcExplorerNodeKind.Arc,
            children = canonicalRoots
        )

    let allMeta = {
        Tag = None
        KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Arc
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

    let datasetCandidates =
        canonicalNodes |> List.filter (fun node -> hasTag GraphNodeTag.Dataset node metaAfterArcs)

    let protocolCandidates =
        canonicalNodes |> List.filter (fun node -> hasTag GraphNodeTag.Protocol node metaAfterArcs)

    let formalParameterCandidates =
        canonicalNodes |> List.filter (fun node -> hasTag GraphNodeTag.FormalParameter node metaAfterArcs)

    let processCandidates =
        canonicalNodes |> List.filter (fun node -> hasTag GraphNodeTag.Process node metaAfterArcs)

    let materialCandidates =
        canonicalNodes
        |> List.filter (fun node ->
            hasTag (GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material) node metaAfterArcs)

    let dataCandidates =
        canonicalNodes
        |> List.filter (fun node ->
            hasTag (GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data) node metaAfterArcs)

    let allWithMetaById =
        metaAfterArcs
        |> addMeta allNodeId allMeta

    let datasetsLayer, afterDatasets =
        createCategoryLayer
            "datasets"
            "Datasets"
            "Top-level index of all dataset nodes."
            datasetCaseExamples
            datasetCandidates
            allWithMetaById

    let protocolsLayer, afterProtocols =
        createCategoryLayer
            "protocols"
            "Protocols"
            "Top-level index of all protocol nodes."
            [ "Protocol", "Protocol(name = Some \"LC-MS Measurement\", processes = [| ... |])" ]
            protocolCandidates
            afterDatasets

    let formalParametersLayer, afterFormalParameters =
        createCategoryLayer
            "formal-parameters"
            "FormalParameters"
            "Top-level index of all formal parameter nodes."
            [ "FormalParameter", "FormalParameter(name = Some \"Instrument Model\", defaultValue = Some \"Q Exactive\")" ]
            formalParameterCandidates
            afterProtocols

    let processesLayer, afterProcesses =
        createCategoryLayer
            "processes"
            "Processes"
            "Top-level index of all process nodes."
            processCaseExamples
            processCandidates
            afterFormalParameters

    let materialsLayer, afterMaterials =
        createCategoryLayer
            "materials"
            "Materials"
            "Top-level index of all material endpoint nodes."
            [
                "Source", "Sources = [| { id = \"source:leaf-a\"; ... } |]"
                "Sample", "Samples = [| { id = \"sample:leaf-a\"; ... } |]"
            ]
            materialCandidates
            afterProcesses

    let datasLayer, metaById =
        createCategoryLayer
            "Data"
            "Data"
            "Top-level index of all data endpoint nodes."
            [
                "Data(Files)", "Files = [| { path = \"assays/metabolomics/feature-table.tsv\"; ... } |]"
                "Data(FragmentSelector)", "FragmentSelector = [| { selector = Some \"mz=100-1000\"; ... } |]"
            ]
            dataCandidates
            afterMaterials

    [
        allNode
        datasetsLayer
        protocolsLayer
        formalParametersLayer
        processesLayer
        materialsLayer
        datasLayer
    ], metaById

let toArcExplorerNodesWithMetaFromArcs (models: ARCGraph list) =
    toArcExplorerNodesWithMetaFromArcObjects [ ARCObjects.Arc(models |> List.toArray) ]

let toArcExplorerNodesWithMeta (model: ARCGraph) =
    toArcExplorerNodesWithMetaFromArcs [ model ]

let toArcExplorerNodes (model: ARCGraph) =
    toArcExplorerNodesWithMeta model |> fst
