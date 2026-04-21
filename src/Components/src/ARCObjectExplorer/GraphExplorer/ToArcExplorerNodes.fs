namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open System
open Swate.Components.Shared
open Fable.Core

type GraphNodeMeta = {
    KindLabel: string
    RoleLabel: string
    Description: string option
    Rows: (string * string) list
    CaseExamples: (string * string) list
}

[<RequireQualifiedAccess>]
module ToArcExplorerNodes =

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

    let private arcObjectCaseExamples = [
        "Arc", "Arc(path = \"C:/example/arc-graph\", datasets = [...])"
        "Datasets", "Dataset(type = Study, identifier = \"study-drought-response\", ...)"
        "Protocols", "Protocol(name = \"LC-MS Measurement\", processes = [...])"
        "FormalParameters", "FormalParameter(name = \"Instrument Model\", ...)"
        "Processes(Input)", "Input(Process(name = \"Extract metabolites\", ...))"
        "Processes(Output)", "Output(Process(name = \"Quantify features\", ...))"
    ]

    let private datasetCaseExamples = [
        "Study", "Dataset(type = Study, identifier = \"study-drought-response\", ...)"
        "Assay", "Dataset(type = Assay, identifier = \"assay-metabolomics\", ...)"
        "Workflow", "Dataset(type = Workflow, identifier = \"workflow-extraction\", ...)"
        "Run", "Dataset(type = Run, identifier = \"run-week-01\", ...)"
    ]

    let private currentProcessCaseExamples = [
        "Input", "Input(Process(name = \"Extract metabolites\", ...))"
        "Output", "Output(Process(name = \"Quantify features\", ...))"
    ]

    let private processTypeCaseExamples = [
        "Material(Source)", "Material(Sources({ id = \"source:leaf-a\"; name = \"Leaf-A\"; ... }))"
        "Material(Sample)", "Material(Samples({ id = \"sample:leaf-a\"; name = \"Leaf-A\"; ... }))"
        "Data(Files)", "Data(Files({ path = \"assays/metabolomics/feature-table.tsv\"; ... }))"
        "Data(FragmentSelector)", "Data(FragmentSelector({ selector = \"mz=100-1000\"; ... }))"
    ]

    let private materialOfArcMaterial =
        function
        | ARCMaterial.Sources material -> material, "Source"
        | ARCMaterial.Samples material -> material, "Sample"

    let private processRole =
        function
        | CurrentProcess.Input processValue -> "Input", processValue, true
        | CurrentProcess.Output processValue -> "Output", processValue, false

    let private processDisplayName (processValue: Process) =
        processValue.name
        |> asOptionalText
        |> Option.defaultValue "Unnamed process"

    let private splitCurrentProcesses (processes: CurrentProcess list) =
        let folder (inputProcessesRev, outputProcessesRev) currentProcess =
            match currentProcess with
            | CurrentProcess.Input processValue -> processValue :: inputProcessesRev, outputProcessesRev
            | CurrentProcess.Output processValue -> inputProcessesRev, processValue :: outputProcessesRev

        processes
        |> List.fold folder ([], [])
        |> fun (inputProcessesRev, outputProcessesRev) -> List.rev inputProcessesRev, List.rev outputProcessesRev

    let private summarizeProcessNames (processes: Process list) =
        processes
        |> List.map processDisplayName
        |> String.concat "; "

    let private ioCountSuffix (inputCount: int) (outputCount: int) =
        $"(I:{inputCount} O:{outputCount})"

    let private flattenNodes (nodes: ArcExplorerNode list) =
        let rec collect (node: ArcExplorerNode) =
            node :: (node.children |> List.collect collect)

        nodes |> List.collect collect

    let private isProtocolNode (node: ArcExplorerNode) =
        node.id.Contains(":protocol:")

    let private isFormalParameterNode (node: ArcExplorerNode) =
        node.id.Contains(":formal-parameter:")

    let private isProcessNode (node: ArcExplorerNode) =
        node.id.Contains(":process:")

    let private isDatasetNode (node: ArcExplorerNode) =
        match node.kind with
        | ArcExplorerNodeKind.Study
        | ArcExplorerNodeKind.Assay
        | ArcExplorerNodeKind.Workflow
        | ArcExplorerNodeKind.Run ->
            isProtocolNode node |> not
            && isProcessNode node |> not
        | _ -> false

    let private isProcessEndpointNode (node: ArcExplorerNode) =
        let containsIgnoreCase (needle: string) (haystack: string) =
            haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0

        node.id.EndsWith(":object", StringComparison.OrdinalIgnoreCase)
        || node.id.EndsWith(":result", StringComparison.OrdinalIgnoreCase)
        || containsIgnoreCase ":object:" node.id
        || containsIgnoreCase ":result:" node.id

    let private hasMetaValueType (expected: string) (node: ArcExplorerNode) (metaById: Map<string, GraphNodeMeta>) =
        metaById
        |> Map.tryFind node.id
        |> Option.exists (fun meta ->
            meta.Rows
            |> List.exists (fun (label, value) ->
                label = "Value Type"
                && String.Equals(value, expected, StringComparison.OrdinalIgnoreCase)))

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
            KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Arc
            RoleLabel = "Canonical"
            Description = Some description
            Rows = [
                yield! row "Entries" (string referenceChildren.Length)
            ]
            CaseExamples = caseExamples
        }

        categoryNode, addMeta categoryNodeId categoryMeta metaAfterChildren

    let private processTypeRows =
        function
        | ProcessType.Material arcMaterial ->
            let material, materialRole = materialOfArcMaterial arcMaterial

            [
                yield! row "Value Type" "Material"
                yield! row "Material DU Cases" "Sources | Samples"
                yield! row "Data DU Cases" "Files | FragmentSelector"
                yield! row "Material Role" materialRole
                yield! row "Material Name" material.name
                yield! row "Material Type" material.type'
                yield! row "Material Id" material.id
                yield! optionalOptionRow "Additional Property" material.additionalProperty
            ]
        | ProcessType.Data arcData ->
            let data, dataRole =
                match arcData with
                | ARCData.Files data -> data, "File"
                | ARCData.FragmentSelector data -> data, "Fragment Selector"

            [
                yield! row "Value Type" "Data"
                yield! row "Material DU Cases" "Sources | Samples"
                yield! row "Data DU Cases" "Files | FragmentSelector"
                yield! row "Data Role" dataRole
                yield! row "Path" data.path
                yield! row "Data Type" data.type'
                yield! optionalOptionRow "Data Id" data.id
                yield! optionalOptionRow "Name" data.name
                yield! optionalOptionRow "Additional Type" data.additionalType
                yield! optionalOptionRow "Selector" data.selector
                yield! optionalOptionRow "Selector Format" data.selectorFormat
                yield! optionalOptionRow "Encoding Format" data.encodingFormat
                yield! optionalOptionRow "Additional Property" data.additionalProperty
            ]

    let private processTypeDisplayName =
        function
        | ProcessType.Material arcMaterial ->
            let material, materialRole = materialOfArcMaterial arcMaterial

            let name =
                asOptionalText material.name
                |> Option.defaultValue $"Unnamed {materialRole.ToLowerInvariant()} material"

            name, $"Material {materialRole}"
        | ProcessType.Data arcData ->
            let data, dataRole =
                match arcData with
                | ARCData.Files data -> data, "File"
                | ARCData.FragmentSelector data -> data, "Fragment Selector"

            let name =
                data.name
                |> asOptionalTextOption
                |> Option.orElseWith (fun () -> asOptionalText data.path)
                |> Option.defaultValue $"Unnamed {dataRole.ToLowerInvariant()} data"

            name, $"Data {dataRole}"

    let private processTypePresentation =
        function
        | ProcessType.Material arcMaterial ->
            let _, materialRole = materialOfArcMaterial arcMaterial
            "Material", materialRole, ArcExplorerNodeKind.Sample
        | ProcessType.Data arcData ->
            let _, dataRole =
                match arcData with
                | ARCData.Files data -> data, "File"
                | ARCData.FragmentSelector data -> data, "Fragment Selector"

            "Data", dataRole, ArcExplorerNodeKind.DataMap

    let private processTypeNode
        (processNodeId: string)
        (prefix: string)
        (endpointIndex: int)
        (processType: ProcessType)
        (isReference: bool)
        (metaById: Map<string, GraphNodeMeta>)
        =
        let nodeId = $"{processNodeId}:{prefix}:{endpointIndex}"
        let displayName, description = processTypeDisplayName processType
        let valueTypeLabel, subtypeLabel, nodeKind = processTypePresentation processType
        let titlePrefix = if prefix = "object" then "Object" else "Result"
        let titleWithIndex =
            if endpointIndex = 0 then
                titlePrefix
            else
                $"{titlePrefix} {endpointIndex + 1}"
        let relationshipRole = if isReference then "Reference" else "Canonical"
        let endpointRole = $"{titlePrefix} ({relationshipRole})"
        let title = $"{titleWithIndex} {valueTypeLabel} ({subtypeLabel}): {displayName}"

        let node =
            ArcExplorerNode.create (
                nodeId,
                title,
                nodeKind,
                isReference = isReference
            )

        let meta =
            {
                KindLabel = valueTypeLabel
                RoleLabel = endpointRole
                Description = Some description
                Rows = [
                    yield! row "Endpoint Role" titlePrefix
                    yield! row "Endpoint Index" (string (endpointIndex + 1))
                    yield! row "Relationship Role" relationshipRole
                    yield! processTypeRows processType
                ]
                CaseExamples = processTypeCaseExamples
            }

        node, addMeta nodeId meta metaById

    let private processTypeGroupNode
        (processNodeId: string)
        (prefix: string)
        (processTypes: ProcessType list)
        (isReference: bool)
        (metaById: Map<string, GraphNodeMeta>)
        =
        let groupLabel =
            if prefix = "object" then
                "Objects"
            else
                "Results"

        let endpointNodes, nextState =
            processTypes
            |> List.mapi (fun endpointIndex processType -> endpointIndex, processType)
            |> List.fold (fun (nodesRev, state) (endpointIndex, processType) ->
                let node, updatedState =
                    processTypeNode processNodeId prefix endpointIndex processType isReference state

                node :: nodesRev, updatedState) ([], metaById)
            |> fun (nodesRev, state) -> List.rev nodesRev, state

        let groupNode =
            ArcExplorerNode.create (
                $"{processNodeId}:{prefix}",
                groupLabel,
                ArcExplorerNodeKind.Group,
                isSelectable = false,
                children = endpointNodes
            )

        groupNode, nextState

    let private processNode
        (protocolNodeId: string)
        (index: int)
        (currentProcess: CurrentProcess)
        (metaById: Map<string, GraphNodeMeta>)
        =
        let roleLabel, processValue, isReference = processRole currentProcess
        let processName = processValue.name |> asOptionalText |> Option.defaultValue $"Process {index + 1}"
        let processTitle = $"{roleLabel}: {processName}"
        let processNodeId = $"{protocolNodeId}:process:{(roleLabel.ToLowerInvariant())}:{index}"

        let objectNodeGroup, metaAfterObject =
            processTypeGroupNode processNodeId "object" processValue.object true metaById

        let resultNodeGroup, metaAfterResult =
            processTypeGroupNode processNodeId "result" processValue.result false metaAfterObject

        let rows = [
            yield! row "Name" processName
            yield! row "Role" roleLabel
            yield! row "Type" processValue.type'
            yield! row "Executes Protocol" processValue.ExecutesProtocol
            yield! row "Object Entries" (string processValue.object.Length)
            yield! row "Result Entries" (string processValue.result.Length)
            yield! optionalOptionRow "Process Id" processValue.id
            yield! optionalOptionRow "Additional Type" processValue.additionalType
            yield! optionalOptionRow "Parameter Value" processValue.parameterValue
        ]

        let processMeta = {
            KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Run
            RoleLabel = roleLabel
            Description = Some $"This process is marked as {roleLabel.ToLowerInvariant()} in the protocol sequence."
            Rows = rows
            CaseExamples = currentProcessCaseExamples @ processTypeCaseExamples
        }

        let node =
            ArcExplorerNode.create (
                processNodeId,
                processTitle,
                ArcExplorerNodeKind.Run,
                isReference = isReference,
                children = [ objectNodeGroup; resultNodeGroup ]
            )

        node, addMeta processNodeId processMeta metaAfterResult

    let private formalParameterNode
        (protocolNodeId: string)
        (index: int)
        (formalParameter: FormalParameter)
        (metaById: Map<string, GraphNodeMeta>)
        =
        let parameterName =
            formalParameter.name
            |> asOptionalText
            |> Option.defaultValue $"Formal Parameter {index + 1}"

        let nodeId = $"{protocolNodeId}:formal-parameter:{index}"
        let inputProcesses, outputProcesses = splitCurrentProcesses formalParameter.processes
        let inputProcessNames = summarizeProcessNames inputProcesses
        let outputProcessNames = summarizeProcessNames outputProcesses
        let formalParameterTitle =
            $"{parameterName} {ioCountSuffix inputProcesses.Length outputProcesses.Length}"

        let rows = [
            yield! row "Name" parameterName
            yield! row "Type" formalParameter.type'
            yield! row "Description" formalParameter.description
            yield! row "Intended Use" formalParameter.intendedUse
            yield! optionalOptionRow "Id" formalParameter.id
            yield! optionalOptionRow "Additional Type" formalParameter.additionalType
            yield! optionalOptionRow "Additional Property" formalParameter.additionalProperty
            yield! optionalOptionRow "Version" formalParameter.version
            yield! optionalOptionRow "URL" formalParameter.url
            yield! row "Attached Processes" (string formalParameter.processes.Length)
            yield! row "Attached Input Processes" (string inputProcesses.Length)
            yield! row "Attached Output Processes" (string outputProcesses.Length)
            yield! optionalRow "Input Process Names" inputProcessNames
            yield! optionalRow "Output Process Names" outputProcessNames
        ]

        let meta = {
            KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Table
            RoleLabel = "Canonical"
            Description = Some "Formal parameter definition used by protocol processes."
            Rows = rows
            CaseExamples = arcObjectCaseExamples
        }

        let node =
            ArcExplorerNode.create (
                nodeId,
                formalParameterTitle,
                ArcExplorerNodeKind.Table
            )

        node, addMeta nodeId meta metaById

    let private protocolNode
        (datasetNodeId: string)
        (index: int)
        (protocol: Protocol)
        (metaById: Map<string, GraphNodeMeta>)
        =
        let protocolName =
            protocol.name
            |> asOptionalText
            |> Option.defaultValue $"Protocol {index + 1}"

        let nodeId = $"{datasetNodeId}:protocol:{sanitizeIdSegment protocolName}:{index}"
        let inputProcesses, outputProcesses = splitCurrentProcesses protocol.processes
        let inputProcessNames = summarizeProcessNames inputProcesses
        let outputProcessNames = summarizeProcessNames outputProcesses
        let protocolTitle =
            $"{protocolName} {ioCountSuffix inputProcesses.Length outputProcesses.Length}"

        let formalParameterNodes, metaAfterParameters =
            protocol.formalParameters
            |> List.mapi (fun parameterIndex parameter -> parameterIndex, parameter)
            |> List.fold (fun (nodesRev, state) (parameterIndex, parameter) ->
                let node, nextState = formalParameterNode nodeId parameterIndex parameter state
                node :: nodesRev, nextState) ([], metaById)
            |> fun (nodesRev, state) -> List.rev nodesRev, state

        let processNodes, metaAfterProcesses =
            protocol.processes
            |> List.mapi (fun processIndex currentProcess -> processIndex, currentProcess)
            |> List.fold (fun (nodesRev, state) (processIndex, currentProcess) ->
                let node, nextState = processNode nodeId processIndex currentProcess state
                node :: nodesRev, nextState) ([], metaAfterParameters)
            |> fun (nodesRev, state) -> List.rev nodesRev, state

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
            ]

        let rows = [
            yield! row "Name" protocolName
            yield! row "Type" protocol.type'
            yield! row "Description" protocol.description
            yield! row "Intended Use" protocol.intendedUse
            yield! optionalOptionRow "Id" protocol.id
            yield! optionalOptionRow "Additional Type" protocol.additionalType
            yield! optionalOptionRow "Additional Property" protocol.additionalProperty
            yield! optionalOptionRow "Version" protocol.version
            yield! optionalOptionRow "URL" protocol.url
            yield! row "Processes" (string protocol.processes.Length)
            yield! row "Input Processes" (string inputProcesses.Length)
            yield! row "Output Processes" (string outputProcesses.Length)
            yield! optionalRow "Input Process Names" inputProcessNames
            yield! optionalRow "Output Process Names" outputProcessNames
            yield! row "Formal Parameters" (string protocol.formalParameters.Length)
        ]

        let meta = {
            KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Workflow
            RoleLabel = "Canonical"
            Description = Some "Protocol metadata with formal parameters and process chain."
            Rows = rows
            CaseExamples = arcObjectCaseExamples @ currentProcessCaseExamples
        }

        let node =
            ArcExplorerNode.create (
                nodeId,
                protocolTitle,
                ArcExplorerNodeKind.Workflow,
                children = children
            )

        node, addMeta nodeId meta metaAfterProcesses

    let private datasetNode
        (arcScopeId: string)
        (index: int)
        (dataset: Dataset)
        (metaById: Map<string, GraphNodeMeta>)
        =
        let kindLabel = dataset.type'.ToString()
        let datasetKind = nodeKindForDataset dataset.type'
        let datasetName = dataset.name |> asOptionalText |> Option.defaultValue dataset.identifier
        let datasetTitle = datasetName
        let nodeId = $"{arcScopeId}:{kindLabel.ToLowerInvariant()}:{sanitizeIdSegment dataset.identifier}:{index}"
        let aboutProtocols = dataset.about
        let aboutProtocolNames =
            aboutProtocols
            |> List.map (fun protocol ->
                protocol.name
                |> asOptionalText
                |> Option.defaultValue "Unnamed protocol")
            |> String.concat "; "

        let protocolNodes, metaAfterProtocols =
            aboutProtocols
            |> List.mapi (fun protocolIndex protocol -> protocolIndex, protocol)
            |> List.fold (fun (nodesRev, state) (protocolIndex, protocol) ->
                let node, nextState = protocolNode nodeId protocolIndex protocol state
                node :: nodesRev, nextState) ([], metaById)
            |> fun (nodesRev, state) -> List.rev nodesRev, state

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
            ]

        let rows = [
            yield! row "Identifier" dataset.identifier
            yield! row "Dataset Type" kindLabel
            yield! row "Type Tag" dataset.additionalType
            yield! row "Description" dataset.description
            yield! row "About Protocols" (string aboutProtocols.Length)
            yield! optionalRow "About Protocol Names" aboutProtocolNames
            yield! optionalOptionRow "Has Part" dataset.hasPart
            yield! optionalOptionRow "Additional Property" dataset.additionalProperty
            yield! row "Dataset Id" dataset.id
        ]

        let meta = {
            KindLabel = ArcExplorerNodeKind.label datasetKind
            RoleLabel = "Canonical"
            Description = Some "Dataset entry in the graph model with about protocol references."
            Rows = rows
            CaseExamples = datasetCaseExamples
        }

        let node =
            ArcExplorerNode.create (
                nodeId,
                datasetTitle,
                datasetKind,
                children = children
            )

        node, addMeta nodeId meta metaAfterProtocols

    let private groupedDatasetNodes
        (arcScopeId: string)
        (datasets: Dataset list)
        (metaById: Map<string, GraphNodeMeta>)
        =
        datasets
        |> List.groupBy (fun dataset -> dataset.type')
        |> List.sortBy (fun (kind, _) -> kind)
        |> List.fold (fun (groupsRev, state) (datasetKind, groupedDatasets) ->
            let sortedDatasets =
                groupedDatasets
                |> List.sortBy (fun dataset -> dataset.name.ToLowerInvariant())

            let datasetNodes, nextState =
                sortedDatasets
                |> List.mapi (fun datasetIndex dataset -> datasetIndex, dataset)
                |> List.fold (fun (nodesRev, state') (datasetIndex, dataset) ->
                    let node, updatedState = datasetNode arcScopeId datasetIndex dataset state'
                    node :: nodesRev, updatedState) ([], state)
                |> fun (nodesRev, updatedState) -> List.rev nodesRev, updatedState

            let groupNode =
                ArcExplorerNode.create (
                    $"{arcScopeId}:group:{(datasetKind.ToString()).ToLowerInvariant()}",
                    datasetKind.ToString(),
                    ArcExplorerNodeKind.Group,
                    isSelectable = false,
                    children = datasetNodes
                )

            groupNode :: groupsRev, nextState) ([], metaById)
        |> fun (groupsRev, state) -> List.rev groupsRev, state

    let toArcExplorerNodesWithMetaFromArcs (models: ARC list) =
        let arcNodes, metaAfterArcs =
            models
            |> List.mapi (fun arcIndex model -> arcIndex, model)
            |> List.fold (fun (arcNodesRev, state) (arcIndex, model) ->
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

                let groupNodes, metaAfterGroups =
                    groupedDatasetNodes arcNodeId model.Datasets state

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
                    yield! row "Datasets" (string model.Datasets.Length)
                ]

                let arcMeta = {
                    KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Arc
                    RoleLabel = "Canonical"
                    Description = Some "ARC node in the Storybook graph explorer."
                    Rows = arcRows
                    CaseExamples = arcObjectCaseExamples @ datasetCaseExamples
                }

                let nextState = addMeta arcNodeId arcMeta metaAfterGroups

                arcNode :: arcNodesRev, nextState) ([], Map.empty)
            |> fun (arcNodesRev, state) -> List.rev arcNodesRev, state

        let totalArcs = arcNodes.Length

        let allNodeId = "graph:all"
        let allNode =
            ArcExplorerNode.create (
                allNodeId,
                "ARCs",
                ArcExplorerNodeKind.Arc,
                children = arcNodes
            )

        let allMeta = {
            KindLabel = ArcExplorerNodeKind.label ArcExplorerNodeKind.Arc
            RoleLabel = "Canonical"
            Description = Some "Top layer containing all ARC roots."
            Rows = [
                yield! row "ARCs" (string totalArcs)
            ]
            CaseExamples = [
                "ARCs", "ARCs -> [Arc(...)]"
            ]
        }

        let canonicalNodes =
            flattenNodes arcNodes

        let datasetCandidates =
            canonicalNodes |> List.filter isDatasetNode

        let protocolCandidates =
            canonicalNodes |> List.filter isProtocolNode

        let formalParameterCandidates =
            canonicalNodes |> List.filter isFormalParameterNode

        let processCandidates =
            canonicalNodes |> List.filter isProcessNode

        let materialCandidates =
            canonicalNodes
            |> List.filter isProcessEndpointNode
            |> List.filter (fun node -> hasMetaValueType "Material" node metaAfterArcs)

        let dataCandidates =
            canonicalNodes
            |> List.filter isProcessEndpointNode
            |> List.filter (fun node -> hasMetaValueType "Data" node metaAfterArcs)

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
                [ "Protocol", "Protocol(name = \"LC-MS Measurement\", processes = [...])" ]
                protocolCandidates
                afterDatasets

        let formalParametersLayer, afterFormalParameters =
            createCategoryLayer
                "formal-parameters"
                "FormalParameters"
                "Top-level index of all formal parameter nodes."
                [ "FormalParameter", "FormalParameter(name = \"Instrument Model\", ...)" ]
                formalParameterCandidates
                afterProtocols

        let processesLayer, afterProcesses =
            createCategoryLayer
                "processes"
                "Processes"
                "Top-level index of all process nodes."
                currentProcessCaseExamples
                processCandidates
                afterFormalParameters

        let materialsLayer, afterMaterials =
            createCategoryLayer
                "materials"
                "Materials"
                "Top-level index of all material endpoint nodes."
                [
                    "Material(Source)", "Material(Sources({ id = \"source:leaf-a\"; name = \"Leaf-A\"; ... }))"
                    "Material(Sample)", "Material(Samples({ id = \"sample:leaf-a\"; name = \"Leaf-A\"; ... }))"
                ]
                materialCandidates
                afterProcesses

        let datasLayer, metaById =
            createCategoryLayer
                "Data"
                "Data"
                "Top-level index of all data endpoint nodes."
                [
                    "Data(Files)", "Data(Files({ path = \"assays/metabolomics/feature-table.tsv\"; ... }))"
                    "Data(FragmentSelector)", "Data(FragmentSelector({ selector = \"mz=100-1000\"; ... }))"
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

    let toArcExplorerNodesWithMeta (model: ARC) =
        toArcExplorerNodesWithMetaFromArcs [ model ]

    let toArcExplorerNodes (model: ARC) =
        toArcExplorerNodesWithMeta model |> fst
