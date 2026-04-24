module ElectronCore.ArcObjectGraphExplorerTests

open System
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer
open Swate.Components.ARCObjectExplorer.GraphExplorer
open Swate.Components.ARCObjectExplorer.GraphExplorer.GraphObjectFixture
open Swate.Components.ARCObjectExplorer.GraphExplorer.GraphExplorerNodes
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model
open Swate.Components.FileExplorerTypes
open Vitest

type private EndpointValue =
    | MaterialValue of Material
    | DataValue of Data

let rec private flattenNodes (nodes: ArcExplorerNode list) =
    seq {
        for node in nodes do
            yield node
            yield! flattenNodes node.children
    }

let rec private flattenFileItems (items: FileItem list) =
    seq {
        for item in items do
            yield item
            yield! flattenFileItems (item.Children |> Option.defaultValue [])
    }

let private groupItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Group
let private materialItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Material
let private dataItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Data

let private isLeafRehomeCandidateAtSiblingLevel (item: FileItem) =
    let hasMarker (marker: string) =
        item.Id.Contains marker

    item.ItemType = materialItemType
    || item.ItemType = dataItemType
    || (
        item.ItemType <> groupItemType
        && (
            hasMarker ":additional-property:"
            || hasMarker ":parameter-value:"
            || hasMarker ":formal-parameter:"
        )
    )

let private expectSome value errorMessage =
    match value with
    | Some value -> value
    | None -> failwith errorMessage

let private hasTag (metaById: Map<string, GraphNodeMeta>) (expectedTag: GraphNodeTag) (node: ArcExplorerNode) =
    metaById
    |> Map.tryFind node.id
    |> Option.exists (fun meta -> meta.Tag = Some expectedTag)

let private tryGetNodeLineageById (nodeId: string) (nodes: ArcExplorerNode list) =
    let rec loop (ancestors: ArcExplorerNode list) (nodes: ArcExplorerNode list) =
        nodes
        |> List.tryPick (fun node ->
            if node.id = nodeId then
                Some(node, List.rev ancestors)
            else
                loop (node :: ancestors) node.children)

    loop [] nodes

let private graphOptionIndexByLabel (label: string) =
    KindFilter.graphObjectExplorerOptions
    |> Array.tryFindIndex (fun option -> option.item = label)
    |> expectSome <| $"Missing graph kind option '{label}'."

let private graphOptionIndicesByLabels (labels: string list) =
    labels
    |> List.map graphOptionIndexByLabel
    |> Set.ofList

let private sortedIndices (indices: Set<int>) =
    indices |> Seq.sort |> Seq.toList

let private extractEntryCount (metaById: Map<string, GraphNodeMeta>) (layerId: string) =
    metaById
    |> Map.tryFind layerId
    |> expectSome <| $"Missing metadata for {layerId}."
    |> fun layerMeta ->
        layerMeta.Rows
        |> List.tryPick (fun (label, value) ->
            if label = "Entries" then
                Int32.TryParse value
                |> function
                    | true, count -> Some count
                    | false, _ -> None
            else
                None)
        |> expectSome <| $"Missing numeric Entries row in metadata for {layerId}."

let private flattenDatasetKinds (datasetKinds: DatasetKinds) =
    [
        yield! datasetKinds.Studies |> Array.toList
        yield! datasetKinds.Assays |> Array.toList
        yield! datasetKinds.Workflows |> Array.toList
        yield! datasetKinds.Runs |> Array.toList
    ]

let private materialEndpointsFromKinds (materialKindsValues: MaterialKinds array) =
    materialKindsValues
    |> Array.toList
    |> List.collect (fun materialKinds ->
        [
            yield! materialKinds.Sources |> Array.toList |> List.map MaterialValue
            yield! materialKinds.Samples |> Array.toList |> List.map MaterialValue
        ])

let private dataEndpointsFromKinds (dataKindsValues: DataKinds array) =
    dataKindsValues
    |> Array.toList
    |> List.collect (fun dataKinds ->
        [
            yield! dataKinds.Files |> Array.toList |> List.map DataValue
            yield! dataKinds.FragmentSelector |> Array.toList |> List.map DataValue
        ])

let private processTypeEndpoints (processType: ProcessType) =
    materialEndpointsFromKinds processType.Materials
    @ dataEndpointsFromKinds processType.Data

let private materialEndpointsFromMaterials (materials: Material array) =
    materials
    |> Array.toList
    |> List.map MaterialValue

let private dataEndpointsFromData (dataValues: Data array) =
    dataValues
    |> Array.toList
    |> List.map DataValue

let private processLevelEndpoints (processValue: LabProcess) =
    materialEndpointsFromMaterials processValue.Materials
    @ dataEndpointsFromData processValue.Data

let private endpointIdentity =
    function
    | MaterialValue material ->
        String.Join(
            "|",
            [
                "material"
                material.id
                material.name
                material.type'
            ]
        )
    | DataValue data ->
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
            yield! processValue.inputs |> Array.toList |> List.collect processTypeEndpoints
            yield! processValue.outputs |> Array.toList |> List.collect processTypeEndpoints
        ]
        |> List.map endpointIdentity
        |> Set.ofList

    processLevelEndpoints processValue
    |> List.filter (fun endpoint -> ioEndpointKeys.Contains(endpointIdentity endpoint) |> not)
    |> List.distinctBy endpointIdentity

let private endpointTypeCounts (endpoints: EndpointValue list) =
    let materialCount =
        endpoints
        |> List.filter (function | MaterialValue _ -> true | DataValue _ -> false)
        |> List.length

    let dataCount =
        endpoints
        |> List.filter (function | DataValue _ -> true | MaterialValue _ -> false)
        |> List.length

    materialCount, dataCount

let private collectSourceStats (graphObjects: ARCObjects list) =
    let datasets = ResizeArray<Dataset>()
    let protocols = ResizeArray<LabProtocol>()
    let formalParameters = ResizeArray<FormalParameter>()
    let processes = ResizeArray<LabProcess>()

    let mutable arcCount = 0
    let mutable materialEndpointCount = 0
    let mutable dataEndpointCount = 0

    let addEndpoints (endpoints: EndpointValue list) =
        let materialCount, dataCount = endpointTypeCounts endpoints
        materialEndpointCount <- materialEndpointCount + materialCount
        dataEndpointCount <- dataEndpointCount + dataCount

    let rec addProtocol (protocol: LabProtocol) =
        protocols.Add protocol

        protocol.parameters
        |> Array.iter (fun formalParameter -> formalParameters.Add formalParameter)

        protocol.processes
        |> Array.iter addProcess

    and addProcess (processValue: LabProcess) =
        processes.Add processValue

        let ioEndpoints =
            [
                yield! processValue.inputs |> Array.toList |> List.collect processTypeEndpoints
                yield! processValue.outputs |> Array.toList |> List.collect processTypeEndpoints
            ]

        let unassociatedEndpoints =
            unassociatedProcessEndpoints processValue

        addEndpoints (ioEndpoints @ unassociatedEndpoints)

    and addDataset (dataset: Dataset) =
        datasets.Add dataset
        dataset.about |> Array.iter addProtocol
        dataset.hasPart |> Array.iter addDataset

    graphObjects
    |> List.iter (function
        | ARCObjects.Arc arcs ->
            arcs
            |> Array.iter (fun arcGraph ->
                arcCount <- arcCount + 1

                arcGraph.Datasets
                |> flattenDatasetKinds
                |> List.iter addDataset)
        | _ ->
            ())

    {| arcs = arcCount
       datasets = datasets |> Seq.toList
       protocols = protocols |> Seq.toList
       formalParameters = formalParameters |> Seq.toList
       processes = processes |> Seq.toList
       materialEndpoints = materialEndpointCount
       dataEndpoints = dataEndpointCount |}

let private createPropertyValue
    (
        id: string,
        name: string,
        value: string option,
        additionalType: string option
    )
    : PropertyValue =
    {
        id = id
        type' = "PropertyValue"
        additionalType = additionalType
        name = name
        value = value
        unit = Some "UO:0000000"
        nameTAN = Some $"{name}:tan"
        valueTAN = value |> Option.map (fun valueText -> $"{valueText}:tan")
        unitTAN = Some "UO:0000000:tan"
    }

let private graphObjectsWithPropertyValues () : ARCObjects list =
    let datasetProperty =
        createPropertyValue(
            "prop:dataset-region",
            "Dataset Region",
            Some "Field-01",
            Some "Geospatial"
        )

    let protocolProperty =
        createPropertyValue(
            "prop:protocol-revision",
            "Protocol Revision",
            Some "2026-04",
            Some "RevisionTag"
        )

    let processParameterValue =
        createPropertyValue(
            "prop:process-batch",
            "Batch",
            Some "B-42",
            Some "BatchNumber"
        )

    let materialProperty =
        createPropertyValue(
            "prop:material-origin",
            "Material Origin",
            Some "Greenhouse",
            Some "OriginTag"
        )

    let dataProperty =
        createPropertyValue(
            "prop:data-format",
            "Data Format",
            Some "tsv",
            Some "MimeHint"
        )

    let sourceMaterial: Material = {
        id = "material:leaf-a"
        type' = "Source"
        additionalType = None
        name = "Leaf-A"
        additionalProperty = [| materialProperty |]
    }

    let measurementData: Data = {
        id = Some "data:leaf-a-measurement"
        type' = "Data"
        additionalType = None
        path = "assays/metabolomics/leaf-a.tsv"
        selector = None
        selectorFormat = None
        encodingFormat = Some "text/tab-separated-values"
        additionalProperty = [| dataProperty |]
    }

    let processType: ProcessType = {
        Materials = [| {
            Sources = [| sourceMaterial |]
            Samples = [||]
        } |]
        Data = [| {
            Files = [| measurementData |]
            FragmentSelector = [||]
        } |]
    }

    let processValue: LabProcess = {
        id = Some "process:extract"
        type' = "Process"
        additionalType = None
        name = "Extract metabolites"
        inputs = [| processType |]
        outputs = [| processType |]
        Materials = [| sourceMaterial |]
        Data = [| measurementData |]
        executesProtocol = "protocol:extraction"
        parameterValue = [| processParameterValue |]
    }

    let protocol: LabProtocol = {
        id = Some "protocol:extraction"
        type' = "Protocol"
        additionalType = None
        name = Some "Extraction"
        parameters = [||]
        description = Some "Minimal protocol fixture for property-value nodes."
        intendedUse = None
        processes = [| processValue |]
        additionalProperty = Some protocolProperty
        version = Some "1.0.0"
        url = None
    }

    let dataset: Dataset = {
        id = "study:property-values"
        type' = ARCDatasets.Study
        additionalType = "Study"
        identifier = "study-property-values"
        name = Some "Property Values Study"
        description = Some "Fixture dataset for property-value graph coverage."
        about = [| protocol |]
        hasPart = [||]
        additionalProperty = [| datasetProperty |]
    }

    let graph: ARCGraph = {
        path = "C:/example/property-values-graph"
        Datasets = {
            Studies = [| dataset |]
            Assays = [||]
            Workflows = [||]
            Runs = [||]
        }
    }

    [
        ARCObjects.Arc [| graph |]
    ]

Vitest.describe("ToArcExplorerNodes graph conversion", fun () ->
    Vitest.test("uses semantic graph filter options in storybook", fun () ->
        let labels =
            KindFilter.graphObjectExplorerOptions
            |> Array.map _.item
            |> Array.toList

        Vitest.expect(labels).toEqual([
            "Datasets"
            "Study"
            "Assay"
            "Workflow"
            "Run"
            "Protocols"
            "FormalParameters"
            "Processes"
            "Materials"
            "Data"
        ]))

    Vitest.test("syncs dataset child kinds when Datasets is toggled on", fun () ->
        let datasetIndex = graphOptionIndexByLabel "Datasets"
        let protocolsIndex = graphOptionIndexByLabel "Protocols"
        let datasetChildIndices = graphOptionIndicesByLabels [ "Study"; "Assay"; "Workflow"; "Run" ]

        let previousSelection = Set.singleton protocolsIndex
        let nextSelection = previousSelection |> Set.add datasetIndex

        let syncedSelection =
            GraphObjectExplorerFilter.syncDatasetKindSelection
                KindFilter.graphObjectExplorerOptions
                previousSelection
                nextSelection

        let expectedSelection =
            Set.union nextSelection datasetChildIndices

        Vitest.expect(sortedIndices syncedSelection).toEqual(sortedIndices expectedSelection))

    Vitest.test("syncs dataset child kinds when Datasets is toggled off", fun () ->
        let datasetIndex = graphOptionIndexByLabel "Datasets"
        let protocolsIndex = graphOptionIndexByLabel "Protocols"
        let datasetChildIndices = graphOptionIndicesByLabels [ "Study"; "Assay"; "Workflow"; "Run" ]

        let previousSelection =
            Set.union (Set.ofList [ datasetIndex; protocolsIndex ]) datasetChildIndices

        let nextSelection = previousSelection |> Set.remove datasetIndex

        let syncedSelection =
            GraphObjectExplorerFilter.syncDatasetKindSelection
                KindFilter.graphObjectExplorerOptions
                previousSelection
                nextSelection

        let expectedSelection =
            Set.difference nextSelection datasetChildIndices

        Vitest.expect(sortedIndices syncedSelection).toEqual(sortedIndices expectedSelection))

    Vitest.test("keeps Datasets unchanged when only child kinds are toggled", fun () ->
        let datasetIndex = graphOptionIndexByLabel "Datasets"
        let studyIndex = graphOptionIndexByLabel "Study"
        let datasetChildIndices = graphOptionIndicesByLabels [ "Study"; "Assay"; "Workflow"; "Run" ]

        let previousSelection =
            Set.union (Set.singleton datasetIndex) datasetChildIndices

        let nextSelection =
            previousSelection |> Set.remove studyIndex

        let syncedSelection =
            GraphObjectExplorerFilter.syncDatasetKindSelection
                KindFilter.graphObjectExplorerOptions
                previousSelection
                nextSelection

        Vitest.expect(sortedIndices syncedSelection).toEqual(sortedIndices nextSelection)
        Vitest.expect(syncedSelection.Contains datasetIndex).toBe(true))

    Vitest.test("keeps select-all and clear-all idempotent with dataset sync", fun () ->
        let allIndices = KindFilter.defaultSelectedIndices KindFilter.graphObjectExplorerOptions
        let emptyIndices = Set.empty<int>

        let syncedFromSelectAll =
            GraphObjectExplorerFilter.syncDatasetKindSelection
                KindFilter.graphObjectExplorerOptions
                emptyIndices
                allIndices

        let syncedFromClearAll =
            GraphObjectExplorerFilter.syncDatasetKindSelection
                KindFilter.graphObjectExplorerOptions
                allIndices
                emptyIndices

        Vitest.expect(sortedIndices syncedFromSelectAll).toEqual(sortedIndices allIndices)
        Vitest.expect(sortedIndices syncedFromClearAll).toEqual(sortedIndices emptyIndices))

    Vitest.test("builds expected top-level shape for graph explorer and omits standalone roots", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, _ = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let topLevelNames = nodes |> List.map _.name

        Vitest.expect(topLevelNames).toEqual([
            "ARCs"
            "Datasets"
            "Protocols"
            "FormalParameters"
            "Processes"
            "Materials"
            "Data"
        ])

        let datasetsLayer = nodes |> List.tryFind (fun node -> node.id = "graph:datasets") |> expectSome <| "Missing datasets layer."
        Vitest.expect(datasetsLayer.children.Length > 0).toBe(true)

        let allLayer = nodes |> List.tryFind (fun node -> node.id = "graph:all") |> expectSome <| "Missing all layer."
        Vitest.expect(allLayer.children.Length > 0).toBe(true)

        let standaloneRoots =
            allLayer.children
            |> List.filter (fun node -> node.id.StartsWith("graph:standalone-", StringComparison.Ordinal))

        Vitest.expect(standaloneRoots.Length).toBe(0))

    Vitest.test("flattens assay descendants into structural folders while keeping folder descendants expandable", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, nodeMetaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects
        let explorerItems = toGraphFileItems nodeMetaById nodes

        let treePaneItems =
            GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel explorerItems

        let assayNode =
            treePaneItems
            |> flattenFileItems
            |> Seq.tryFind (fun item ->
                item.ItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Assay
                && item.Name = "Metabolomics")
            |> expectSome <| "Expected Metabolomics assay node in flattened tree."

        let assayChildren = assayNode.Children |> Option.defaultValue []

        let protocolNodesAtAssayLevel =
            assayChildren
            |> List.filter (fun item -> item.Name = "LC-MS Measurement")

        let processNodesAtAssayLevel =
            assayChildren
            |> List.filter (fun item -> item.Name = "Quantify features")

        let protocolsGroup =
            assayChildren
            |> List.tryFind (fun item -> item.Name = "Protocols")
            |> expectSome <| "Expected structural Protocols group at assay parent level."

        let processesGroup =
            assayChildren
            |> List.tryFind (fun item -> item.Name = "Processes")
            |> expectSome <| "Expected structural Processes group at assay parent level."

        let inputsGroup =
            assayChildren
            |> List.tryFind (fun item -> item.Name = "Inputs")
            |> expectSome <| "Expected structural Inputs group at assay parent level."

        let outputsGroup =
            assayChildren
            |> List.tryFind (fun item -> item.Name = "Outputs")
            |> expectSome <| "Expected structural Outputs group at assay parent level."

        let inputsAtAssayLevel =
            assayChildren
            |> List.filter (fun item -> item.Name = "Inputs")

        let outputsAtAssayLevel =
            assayChildren
            |> List.filter (fun item -> item.Name = "Outputs")

        let protocolNodeInProtocolsGroup =
            protocolsGroup.Children
            |> Option.defaultValue []
            |> List.tryFind (fun item -> item.Name = "LC-MS Measurement")
            |> expectSome <| "Expected protocol node inside Protocols group."

        let processNodeInProcessesGroup =
            processesGroup.Children
            |> Option.defaultValue []
            |> List.tryFind (fun item -> item.Name = "Quantify features")
            |> expectSome <| "Expected process node inside Processes group."

        Vitest.expect(protocolNodesAtAssayLevel.Length).toBe(0)
        Vitest.expect(processNodesAtAssayLevel.Length).toBe(0)
        Vitest.expect(inputsAtAssayLevel.Length).toBe(1)
        Vitest.expect(outputsAtAssayLevel.Length).toBe(1)
        Vitest.expect(protocolsGroup.Selectable).toBe(false)
        Vitest.expect(processesGroup.Selectable).toBe(false)
        Vitest.expect(inputsGroup.Selectable).toBe(false)
        Vitest.expect(outputsGroup.Selectable).toBe(false)
        Vitest.expect(protocolNodeInProtocolsGroup.Selectable).toBe(true)
        Vitest.expect(processNodeInProcessesGroup.Selectable).toBe(true)

        let processChildren = processNodeInProcessesGroup.Children |> Option.defaultValue []
        let processChildNames = processChildren |> List.map _.Name

        Vitest.expect(processChildren.Length > 0).toBe(true)
        Vitest.expect(processChildNames |> List.contains "Inputs").toBe(true)
        Vitest.expect(processChildNames |> List.contains "Outputs").toBe(true))

    Vitest.test("merges repeated Inputs and Outputs folders on flattened ARC root level", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, nodeMetaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects
        let explorerItems = toGraphFileItems nodeMetaById nodes

        let treePaneItems =
            GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel explorerItems

        let arcsRoot =
            treePaneItems
            |> List.tryFind (fun item -> item.Id = "graph:all")
            |> expectSome <| "Expected ARCs root node in flattened tree."

        let arcsChildren = arcsRoot.Children |> Option.defaultValue []

        let inputFolders =
            arcsChildren
            |> List.filter (fun item -> item.Name = "Inputs")

        let outputFolders =
            arcsChildren
            |> List.filter (fun item -> item.Name = "Outputs")

        Vitest.expect(inputFolders.Length).toBe(1)
        Vitest.expect(outputFolders.Length).toBe(1)

        let mergedInputs = inputFolders |> List.head
        let mergedOutputs = outputFolders |> List.head

        let mergedInputChildren =
            mergedInputs.Children
            |> Option.defaultValue []

        let mergedOutputChildren =
            mergedOutputs.Children
            |> Option.defaultValue []

        let mergedInputChildNames = mergedInputChildren |> List.map _.Name
        let mergedOutputChildNames = mergedOutputChildren |> List.map _.Name

        let mergedInputChildIds = mergedInputChildren |> List.map _.Id
        let mergedOutputChildIds = mergedOutputChildren |> List.map _.Id

        let mergedInputMaterialFolders =
            mergedInputChildren
            |> List.filter (fun item ->
                item.ItemType = groupItemType
                && item.Name = "Material")

        let mergedInputDataFolders =
            mergedInputChildren
            |> List.filter (fun item ->
                item.ItemType = groupItemType
                && item.Name = "Data")

        let mergedOutputMaterialFolders =
            mergedOutputChildren
            |> List.filter (fun item ->
                item.ItemType = groupItemType
                && item.Name = "Material")

        let mergedOutputDataFolders =
            mergedOutputChildren
            |> List.filter (fun item ->
                item.ItemType = groupItemType
                && item.Name = "Data")

        let inputSiblingLeafCandidates =
            mergedInputChildren
            |> List.filter isLeafRehomeCandidateAtSiblingLevel

        let outputSiblingLeafCandidates =
            mergedOutputChildren
            |> List.filter isLeafRehomeCandidateAtSiblingLevel

        let mergedInputMaterialChildren =
            mergedInputMaterialFolders
            |> List.tryHead
            |> Option.bind _.Children
            |> Option.defaultValue []

        let mergedInputDataChildren =
            mergedInputDataFolders
            |> List.tryHead
            |> Option.bind _.Children
            |> Option.defaultValue []

        let mergedOutputMaterialChildren =
            mergedOutputMaterialFolders
            |> List.tryHead
            |> Option.bind _.Children
            |> Option.defaultValue []

        let mergedOutputDataChildren =
            mergedOutputDataFolders
            |> List.tryHead
            |> Option.bind _.Children
            |> Option.defaultValue []

        Vitest.expect(mergedInputs.Selectable).toBe(false)
        Vitest.expect(mergedOutputs.Selectable).toBe(false)
        Vitest.expect(mergedInputChildIds.Length).toBe((mergedInputChildIds |> List.distinct).Length)
        Vitest.expect(mergedOutputChildIds.Length).toBe((mergedOutputChildIds |> List.distinct).Length)
        Vitest.expect(mergedInputMaterialFolders.Length).toBe(1)
        Vitest.expect(mergedInputDataFolders.Length).toBe(1)
        Vitest.expect(mergedOutputMaterialFolders.Length).toBe(1)
        Vitest.expect(mergedOutputDataFolders.Length).toBe(1)
        Vitest.expect(inputSiblingLeafCandidates.Length).toBe(0)
        Vitest.expect(outputSiblingLeafCandidates.Length).toBe(0)
        Vitest.expect(mergedInputMaterialChildren |> List.exists (fun item -> item.ItemType = materialItemType)).toBe(true)
        Vitest.expect(mergedInputDataChildren |> List.exists (fun item -> item.ItemType = dataItemType)).toBe(true)
        Vitest.expect(mergedOutputMaterialChildren |> List.exists (fun item -> item.ItemType = materialItemType)).toBe(true)
        Vitest.expect(mergedOutputDataChildren |> List.exists (fun item -> item.ItemType = dataItemType)).toBe(true)
        Vitest.expect(mergedInputChildNames |> List.contains "Material").toBe(true)
        Vitest.expect(mergedInputChildNames |> List.contains "Data").toBe(true)
        Vitest.expect(mergedOutputChildNames |> List.contains "Material").toBe(true)
        Vitest.expect(mergedOutputChildNames |> List.contains "Data").toBe(true))

    Vitest.test("keeps direct ARC children first on flattened ARCs root before nested folders", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, nodeMetaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects
        let explorerItems = toGraphFileItems nodeMetaById nodes

        let treePaneItems =
            GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel explorerItems

        let arcsRoot =
            treePaneItems
            |> List.tryFind (fun item -> item.Id = "graph:all")
            |> expectSome <| "Expected ARCs root node in flattened tree."

        let arcsChildren = arcsRoot.Children |> Option.defaultValue []

        let isDirectArcChild (item: FileItem) =
            let prefix = "graph:arc:"

            item.ItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Arc
            && item.Id.StartsWith(prefix, StringComparison.Ordinal)
            && item.Id.Substring(prefix.Length).Contains(":") |> not

        let arcChildren =
            arcsChildren
            |> List.filter isDirectArcChild

        let firstNestedChildIndex =
            arcsChildren
            |> List.tryFindIndex (isDirectArcChild >> not)

        Vitest.expect(arcChildren.Length > 0).toBe(true)

        match firstNestedChildIndex with
        | Some index ->
            Vitest.expect(index).toBe(arcChildren.Length)
        | None ->
            Vitest.expect(arcsChildren.Length).toBe(arcChildren.Length))

    Vitest.test("fuses repeated non-directional group folders on flattened ARC root level", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, nodeMetaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects
        let explorerItems = toGraphFileItems nodeMetaById nodes

        let treePaneItems =
            GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel explorerItems

        let arcsRoot =
            treePaneItems
            |> List.tryFind (fun item -> item.Id = "graph:all")
            |> expectSome <| "Expected ARCs root node in flattened tree."

        let arcsChildren = arcsRoot.Children |> Option.defaultValue []

        let protocolGroupsAtRoot =
            arcsChildren
            |> List.filter (fun item ->
                item.ItemType = groupItemType
                && item.Name = "Protocols")

        let processGroupsAtRoot =
            arcsChildren
            |> List.filter (fun item ->
                item.ItemType = groupItemType
                && item.Name = "Processes")

        let additionalPropertiesGroupsAtRoot =
            arcsChildren
            |> List.filter (fun item ->
                item.ItemType = groupItemType
                && item.Name = "Additional Properties")

        Vitest.expect(protocolGroupsAtRoot.Length).toBe(1)
        Vitest.expect(processGroupsAtRoot.Length).toBe(1)
        Vitest.expect(additionalPropertiesGroupsAtRoot.Length).toBe(1))

    Vitest.test("summarizes nested additional properties into one parent-level folder in flattened tree view", fun () ->
        let graphObjects = graphObjectsWithPropertyValues ()
        let nodes, nodeMetaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects
        let explorerItems = toGraphFileItems nodeMetaById nodes

        let treePaneItems =
            GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel explorerItems

        let studyNode =
            treePaneItems
            |> flattenFileItems
            |> Seq.tryFind (fun item ->
                item.ItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Study
                && item.Name = "Property Values Study")
            |> expectSome <| "Expected Property Values Study node in flattened tree."

        let studyChildren = studyNode.Children |> Option.defaultValue []

        let summaryFolders =
            studyChildren
            |> List.filter (fun item ->
                item.Name = "Additional Properties"
                && item.Id.EndsWith(":group:flattened-additional-properties", StringComparison.Ordinal))

        Vitest.expect(summaryFolders.Length).toBe(1)

        let additionalPropertyBranchNodesAtStudyLevel =
            studyChildren
            |> List.filter (fun item ->
                item.Id.Contains(":group:additional-property")
                || item.Id.Contains(":additional-property:"))

        Vitest.expect(additionalPropertyBranchNodesAtStudyLevel.Length).toBe(0)

        let summaryFolder =
            summaryFolders
            |> List.head

        let summarizedAdditionalProperties = summaryFolder.Children |> Option.defaultValue []
        let summarizedAdditionalPropertyNames = summarizedAdditionalProperties |> List.map _.Name

        Vitest.expect(summaryFolder.Selectable).toBe(false)
        Vitest.expect(summaryFolder.IsDirectory).toBe(true)
        Vitest.expect(summarizedAdditionalProperties.Length > 0).toBe(true)
        Vitest.expect(summarizedAdditionalProperties |> List.forall (fun item -> item.Id.Contains(":additional-property:"))).toBe(true)
        Vitest.expect(summarizedAdditionalPropertyNames |> List.contains "Dataset Region").toBe(true)
        Vitest.expect(summarizedAdditionalPropertyNames |> List.contains "Protocol Revision").toBe(true)
        Vitest.expect(summarizedAdditionalPropertyNames |> List.contains "Material Origin").toBe(true)
        Vitest.expect(summarizedAdditionalPropertyNames |> List.contains "Data Format").toBe(true)
        Vitest.expect(summarizedAdditionalPropertyNames |> List.contains "Batch").toBe(false))

    Vitest.test("summarizes nested parameter values into one parent-level folder in flattened tree view", fun () ->
        let graphObjects = graphObjectsWithPropertyValues ()
        let nodes, nodeMetaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects
        let explorerItems = toGraphFileItems nodeMetaById nodes

        let treePaneItems =
            GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel explorerItems

        let studyNode =
            treePaneItems
            |> flattenFileItems
            |> Seq.tryFind (fun item ->
                item.ItemType = GraphExplorerNodeKind.label GraphExplorerNodeKind.Study
                && item.Name = "Property Values Study")
            |> expectSome <| "Expected Property Values Study node in flattened tree."

        let studyChildren = studyNode.Children |> Option.defaultValue []

        let summaryFolders =
            studyChildren
            |> List.filter (fun item ->
                item.Name = "Parameter Values"
                && item.Id.EndsWith(":group:flattened-parameter-value", StringComparison.Ordinal))

        Vitest.expect(summaryFolders.Length).toBe(1)

        let parameterValueBranchNodesAtStudyLevel =
            studyChildren
            |> List.filter (fun item ->
                item.Id.Contains(":group:parameter-value")
                || item.Id.Contains(":parameter-value:"))

        Vitest.expect(parameterValueBranchNodesAtStudyLevel.Length).toBe(0)

        let summaryFolder =
            summaryFolders
            |> List.head

        let summarizedParameterValues = summaryFolder.Children |> Option.defaultValue []
        let summarizedParameterValueNames = summarizedParameterValues |> List.map _.Name

        Vitest.expect(summaryFolder.Selectable).toBe(false)
        Vitest.expect(summaryFolder.IsDirectory).toBe(true)
        Vitest.expect(summarizedParameterValues.Length > 0).toBe(true)
        Vitest.expect(summarizedParameterValues |> List.forall (fun item -> item.Id.Contains(":parameter-value:"))).toBe(true)
        Vitest.expect(summarizedParameterValueNames |> List.contains "Batch").toBe(true))

    Vitest.test("summarizes nested formal parameters into one parent-level folder in flattened tree view", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, nodeMetaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects
        let explorerItems = toGraphFileItems nodeMetaById nodes

        let treePaneItems =
            GraphObjectExplorerTreeData.flattenNestedChildrenOnParentLevel explorerItems

        let parentWithFormalSummary =
            treePaneItems
            |> flattenFileItems
            |> Seq.tryFind (fun item ->
                item.Children
                |> Option.defaultValue []
                |> List.exists (fun child -> child.Id.EndsWith(":group:flattened-formal-parameters", StringComparison.Ordinal)))
            |> expectSome <| "Expected at least one flattened parent with a Formal Parameters summary folder."

        let parentChildren = parentWithFormalSummary.Children |> Option.defaultValue []

        let summaryFolders =
            parentChildren
            |> List.filter (fun item ->
                item.Name = "Formal Parameters"
                && item.Id.EndsWith(":group:flattened-formal-parameters", StringComparison.Ordinal))

        Vitest.expect(summaryFolders.Length).toBe(1)

        let formalParameterBranchNodesAtParentLevel =
            parentChildren
            |> List.filter (fun item ->
                item.Id.Contains(":group:formal-parameters")
                || item.Id.Contains(":formal-parameter:"))

        Vitest.expect(formalParameterBranchNodesAtParentLevel.Length).toBe(0)

        let summaryFolder =
            summaryFolders
            |> List.head

        let summarizedFormalParameters = summaryFolder.Children |> Option.defaultValue []

        Vitest.expect(summaryFolder.Selectable).toBe(false)
        Vitest.expect(summaryFolder.IsDirectory).toBe(true)
        Vitest.expect(summarizedFormalParameters.Length > 0).toBe(true)
        Vitest.expect(summarizedFormalParameters |> List.forall (fun item -> item.Id.Contains(":formal-parameter:"))).toBe(true))

    Vitest.test("shows only selected semantic top-level layer alongside ARCs root", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let filteredNodes =
            GraphObjectExplorerFilter.filterNodesBySemanticKinds
                (Set.ofList [ "Datasets" ])
                nodes
                metaById
                None

        let topLevelIds =
            filteredNodes
            |> List.map _.id

        Vitest.expect(topLevelIds).toEqual([
            "graph:all"
            "graph:datasets"
        ])

        Vitest.expect(topLevelIds |> List.contains "graph:protocols").toBe(false)
        Vitest.expect(topLevelIds |> List.contains "graph:formal-parameters").toBe(false)
        Vitest.expect(topLevelIds |> List.contains "graph:processes").toBe(false)
        Vitest.expect(topLevelIds |> List.contains "graph:materials").toBe(false)
        Vitest.expect(topLevelIds |> List.contains "graph:Data").toBe(false))

    Vitest.test("shows study datasets without exposing datasets top-level layer", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let filteredNodes =
            GraphObjectExplorerFilter.filterNodesBySemanticKinds
                (Set.ofList [ "Study" ])
                nodes
                metaById
                None

        let topLevelIds =
            filteredNodes
            |> List.map _.id

        Vitest.expect(topLevelIds).toEqual([
            "graph:all"
        ])

        Vitest.expect(topLevelIds |> List.contains "graph:datasets").toBe(false)

        let allLayer =
            filteredNodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer after Study semantic filtering."

        let canonicalNodes =
            allLayer.children
            |> flattenNodes
            |> Seq.toList

        let datasetNodes =
            canonicalNodes
            |> List.filter (hasTag metaById GraphNodeTag.Dataset)

        let studyNodes =
            datasetNodes
            |> List.filter (fun node -> node.kind = ArcExplorerNodeKind.Study)

        let assayNodes =
            datasetNodes
            |> List.filter (fun node -> node.kind = ArcExplorerNodeKind.Assay)

        let workflowNodes =
            datasetNodes
            |> List.filter (fun node -> node.kind = ArcExplorerNodeKind.Workflow)

        let runNodes =
            datasetNodes
            |> List.filter (fun node -> node.kind = ArcExplorerNodeKind.Run)

        Vitest.expect(studyNodes.Length > 0).toBe(true)
        Vitest.expect(assayNodes.Length).toBe(0)
        Vitest.expect(workflowNodes.Length).toBe(0)
        Vitest.expect(runNodes.Length).toBe(0))

    Vitest.test("keeps only assay datasets in canonical and datasets layers for Datasets + Assay", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let filteredNodes =
            GraphObjectExplorerFilter.filterNodesBySemanticKinds
                (Set.ofList [ "Datasets"; "Assay" ])
                nodes
                metaById
                None

        let topLevelIds =
            filteredNodes
            |> List.map _.id

        Vitest.expect(topLevelIds).toEqual([
            "graph:all"
            "graph:datasets"
        ])

        let datasetNodesInLayer (layerId: string) =
            filteredNodes
            |> List.tryFind (fun node -> node.id = layerId)
            |> expectSome <| $"Missing {layerId} layer after semantic filtering."
            |> fun layer ->
                layer.children
                |> flattenNodes
                |> Seq.toList
                |> List.filter (hasTag metaById GraphNodeTag.Dataset)

        let assertContainsOnlyAssays (nodes: ArcExplorerNode list) =
            let studyCount =
                nodes
                |> List.filter (fun node -> node.kind = ArcExplorerNodeKind.Study)
                |> List.length

            let assayCount =
                nodes
                |> List.filter (fun node -> node.kind = ArcExplorerNodeKind.Assay)
                |> List.length

            let workflowCount =
                nodes
                |> List.filter (fun node -> node.kind = ArcExplorerNodeKind.Workflow)
                |> List.length

            let runCount =
                nodes
                |> List.filter (fun node -> node.kind = ArcExplorerNodeKind.Run)
                |> List.length

            Vitest.expect(assayCount > 0).toBe(true)
            Vitest.expect(studyCount).toBe(0)
            Vitest.expect(workflowCount).toBe(0)
            Vitest.expect(runCount).toBe(0)

        assertContainsOnlyAssays (datasetNodesInLayer "graph:all")
        assertContainsOnlyAssays (datasetNodesInLayer "graph:datasets"))

    Vitest.test("prunes hidden dataset branches from graph:all for Protocols-only filtering", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let filteredNodes =
            GraphObjectExplorerFilter.filterNodesBySemanticKinds
                (Set.ofList [ "Protocols" ])
                nodes
                metaById
                None

        let topLevelIds =
            filteredNodes
            |> List.map _.id

        Vitest.expect(topLevelIds).toEqual([
            "graph:all"
            "graph:protocols"
        ])

        let allLayer =
            filteredNodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer after Protocols-only filtering."

        let allLayerCanonicalNodes =
            allLayer.children
            |> flattenNodes
            |> Seq.toList

        let datasetNodeCountInAllLayer =
            allLayerCanonicalNodes
            |> List.filter (hasTag metaById GraphNodeTag.Dataset)
            |> List.length

        let protocolNodeCountInAllLayer =
            allLayerCanonicalNodes
            |> List.filter (hasTag metaById GraphNodeTag.Protocol)
            |> List.length

        Vitest.expect(datasetNodeCountInAllLayer).toBe(0)
        Vitest.expect(protocolNodeCountInAllLayer).toBe(0)

        let protocolsLayer =
            filteredNodes
            |> List.tryFind (fun node -> node.id = "graph:protocols")
            |> expectSome <| "Missing graph:protocols layer after Protocols-only filtering."

        let protocolNodeCountInProtocolsLayer =
            protocolsLayer.children
            |> flattenNodes
            |> Seq.toList
            |> List.filter (hasTag metaById GraphNodeTag.Protocol)
            |> List.length

        Vitest.expect(protocolNodeCountInProtocolsLayer > 0).toBe(true))

    Vitest.test("keeps selected visible protocol and only its ancestor chain in graph:all", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let selectedProtocol =
            nodes
            |> flattenNodes
            |> Seq.tryFind (fun node ->
                not node.isReference
                && hasTag metaById GraphNodeTag.Protocol node)
            |> expectSome <| "Expected canonical protocol node for selected-path filtering."

        let _, selectedAncestors =
            tryGetNodeLineageById selectedProtocol.id nodes
            |> expectSome <| $"Missing lineage for selected protocol {selectedProtocol.id}."

        let selectedPathNodeIdsInAllLayer =
            selectedAncestors
            |> List.map _.id
            |> Set.ofList
            |> Set.remove "graph:all"
            |> Set.add selectedProtocol.id

        let filteredNodes =
            GraphObjectExplorerFilter.filterNodesBySemanticKinds
                (Set.ofList [ "Protocols" ])
                nodes
                metaById
                (Some selectedProtocol.id)

        let allLayer =
            filteredNodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer after selected-path filtering."

        let allLayerNodes =
            allLayer.children
            |> flattenNodes
            |> Seq.toList

        let allLayerNodeIds =
            allLayerNodes
            |> List.map _.id
            |> Set.ofList

        selectedPathNodeIdsInAllLayer
        |> Set.iter (fun nodeId ->
            Vitest.expect(allLayerNodeIds.Contains nodeId).toBe(true))

        let visibleProtocolNodesInAllLayer =
            allLayerNodes
            |> List.filter (hasTag metaById GraphNodeTag.Protocol)

        Vitest.expect(visibleProtocolNodesInAllLayer.Length).toBe(1)
        Vitest.expect(visibleProtocolNodesInAllLayer.Head.id).toBe(selectedProtocol.id))

    Vitest.test("does not preserve hidden selected nodes and keeps resolved selection empty", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let selectedStudy =
            nodes
            |> flattenNodes
            |> Seq.tryFind (fun node ->
                not node.isReference
                && hasTag metaById GraphNodeTag.Dataset node
                && node.kind = ArcExplorerNodeKind.Study)
            |> expectSome <| "Expected canonical study node for hidden-selection regression."

        let filteredNodes =
            GraphObjectExplorerFilter.filterNodesBySemanticKinds
                (Set.ofList [ "Protocols" ])
                nodes
                metaById
                (Some selectedStudy.id)

        let allLayer =
            filteredNodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer after hidden-selection filtering."

        let allLayerNodeIds =
            allLayer.children
            |> flattenNodes
            |> Seq.map _.id
            |> Set.ofSeq

        Vitest.expect(allLayerNodeIds.Contains selectedStudy.id).toBe(false)

        let viewModel =
            Swate.Components.ARCObjectExplorer.Model.create
                filteredNodes
                (ArcSelection.forExplorerNode selectedStudy.id None)
                KindFilter.arcObjectExplorerOptions
                (KindFilter.defaultSelectedIndices KindFilter.arcObjectExplorerOptions)

        Vitest.expect(viewModel.Selection.IsNone).toBe(true))

    Vitest.test("isolates endpoint categories without kind-label leakage", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let filteredNodes =
            GraphObjectExplorerFilter.filterNodesBySemanticKinds
                (Set.ofList [ "Materials" ])
                nodes
                metaById
                None

        let topLevelIds =
            filteredNodes
            |> List.map _.id

        Vitest.expect(topLevelIds).toEqual([
            "graph:all"
            "graph:materials"
        ])

        let allLayer =
            filteredNodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer after semantic filtering."

        let canonicalNodes =
            allLayer.children
            |> flattenNodes
            |> Seq.toList

        let materialEndpointTag = GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material
        let dataEndpointTag = GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data

        let materialEndpointNodeCount =
            canonicalNodes
            |> List.filter (hasTag metaById materialEndpointTag)
            |> List.length

        let dataEndpointNodeCount =
            canonicalNodes
            |> List.filter (hasTag metaById dataEndpointTag)
            |> List.length

        let materialsLayer =
            filteredNodes
            |> List.tryFind (fun node -> node.id = "graph:materials")
            |> expectSome <| "Missing materials layer after semantic filtering."

        Vitest.expect(materialsLayer.children.Length > 0).toBe(true)
        Vitest.expect(materialEndpointNodeCount > 0).toBe(true)
        Vitest.expect(dataEndpointNodeCount).toBe(0))

    Vitest.test("ignores non-Arc ARCObjects entries when creating the canonical tree", fun () ->
        let graphObjects = fakeGraphObjects ()
        let baselineNodes, _ = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let noiseProperty: PropertyValue = {
            id = "prop:noise"
            type' = "PropertyValue"
            additionalType = None
            name = "Noise"
            value = Some "Standalone"
            unit = None
            nameTAN = None
            valueTAN = None
            unitTAN = None
        }

        let noiseMaterial: Material = {
            id = "material:noise"
            type' = "Sample"
            additionalType = None
            name = "Standalone Noise Material"
            additionalProperty = [||]
        }

        let noiseData: Data = {
            id = Some "data:noise"
            type' = "Data"
            additionalType = None
            path = "standalone/noise.tsv"
            selector = None
            selectorFormat = None
            encodingFormat = None
            additionalProperty = [||]
        }

        let noiseProcessType: ProcessType = {
            Materials = [| {
                Sources = [||]
                Samples = [| noiseMaterial |]
            } |]
            Data = [| {
                Files = [| noiseData |]
                FragmentSelector = [||]
            } |]
        }

        let noiseProcess: LabProcess = {
            id = Some "process:noise"
            type' = "Process"
            additionalType = None
            name = "Standalone Noise Process"
            inputs = [| noiseProcessType |]
            outputs = [| noiseProcessType |]
            Materials = [| noiseMaterial |]
            Data = [| noiseData |]
            executesProtocol = "protocol:noise"
            parameterValue = [||]
        }

        let noiseFormalParameter: FormalParameter = {
            id = "fp:noise"
            type' = "FormalParameter"
            name = Some "Noise Parameter"
            nameTAN = None
            defaultValue = Some "1"
        }

        let noiseProtocol: LabProtocol = {
            id = Some "protocol:noise"
            type' = "Protocol"
            additionalType = None
            name = Some "Standalone Noise Protocol"
            parameters = [| noiseFormalParameter |]
            description = Some "Standalone protocol that should be ignored."
            intendedUse = None
            processes = [| noiseProcess |]
            additionalProperty = None
            version = None
            url = None
        }

        let noiseDataset: Dataset = {
            id = "workflow:noise"
            type' = ARCDatasets.Workflow
            additionalType = "Workflow"
            identifier = "workflow-noise"
            name = Some "Standalone Noise Workflow"
            description = Some "Standalone dataset that should be ignored."
            about = [| noiseProtocol |]
            hasPart = [||]
            additionalProperty = [| noiseProperty |]
        }

        let noisyGraphObjects =
            graphObjects @ [
                ARCObjects.Datasets [| noiseDataset |]
                ARCObjects.Protocols [| noiseProtocol |]
                ARCObjects.FormalParameters [| noiseFormalParameter |]
                ARCObjects.Processes [| noiseProcess |]
            ]

        let noisyNodes, _ = toArcExplorerNodesWithMetaFromArcObjects noisyGraphObjects

        let baselineAllLayer =
            baselineNodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing baseline graph:all layer."

        let noisyAllLayer =
            noisyNodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing noisy graph:all layer."

        Vitest.expect(noisyAllLayer.children.Length).toBe(baselineAllLayer.children.Length)

        let baselineDatasetsLayer = baselineNodes |> List.tryFind (fun node -> node.id = "graph:datasets") |> expectSome <| "Missing baseline datasets layer."
        let noisyDatasetsLayer = noisyNodes |> List.tryFind (fun node -> node.id = "graph:datasets") |> expectSome <| "Missing noisy datasets layer."

        Vitest.expect(noisyDatasetsLayer.children.Length).toBe(baselineDatasetsLayer.children.Length))

    Vitest.test("emits metadata labels for canonical datasets, processes, and process endpoints", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let datasetNode =
            nodes
            |> flattenNodes
            |> Seq.tryFind (fun node ->
                node.kind = ArcExplorerNodeKind.Study
                && node.isReference = false)
            |> expectSome <| "Expected canonical dataset node."

        let datasetMeta =
            metaById
            |> Map.tryFind datasetNode.id
            |> expectSome <| "Expected metadata for canonical dataset node."

        Vitest.expect(datasetMeta.KindLabel).toBe("Study")
        Vitest.expect(datasetMeta.GraphKind).toBe(GraphExplorerNodeKind.Study)
        Vitest.expect(datasetMeta.RoleLabel).toBe("Canonical")

        let processMeta =
            metaById
            |> Map.values
            |> Seq.tryFind (fun meta -> meta.Tag = Some GraphNodeTag.Process)
            |> expectSome <| "Expected at least one process metadata entry."

        Vitest.expect(processMeta.KindLabel).toBe("Process")
        Vitest.expect(processMeta.GraphKind).toBe(GraphExplorerNodeKind.Process)
        Vitest.expect(processMeta.RoleLabel).toBe("Canonical")
        Vitest.expect(processMeta.Rows |> List.exists (fun (label, value) -> label = "Type" && value = "LabProcess")).toBe(true)
        Vitest.expect(processMeta.Rows |> List.exists (fun (label, _) -> label = "Object Type")).toBe(true)

        let protocolMeta =
            metaById
            |> Map.values
            |> Seq.tryFind (fun meta -> meta.Tag = Some GraphNodeTag.Protocol)
            |> expectSome <| "Expected at least one protocol metadata entry."

        Vitest.expect(protocolMeta.KindLabel).toBe("Protocol")
        Vitest.expect(protocolMeta.GraphKind).toBe(GraphExplorerNodeKind.Protocol)

        let formalParameterMeta =
            metaById
            |> Map.values
            |> Seq.tryFind (fun meta -> meta.Tag = Some GraphNodeTag.FormalParameter)
            |> expectSome <| "Expected at least one formal parameter metadata entry."

        Vitest.expect(formalParameterMeta.KindLabel).toBe("FormalParameter")
        Vitest.expect(formalParameterMeta.GraphKind).toBe(GraphExplorerNodeKind.FormalParameter)

        let materialMeta =
            metaById
            |> Map.values
            |> Seq.tryFind (fun meta -> meta.Tag = Some(GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material))
            |> expectSome <| "Expected at least one material endpoint metadata entry."

        Vitest.expect(materialMeta.KindLabel).toBe("Material")
        Vitest.expect(materialMeta.GraphKind).toBe(GraphExplorerNodeKind.Material)
        Vitest.expect(
            materialMeta.RoleLabel.StartsWith("Input", StringComparison.Ordinal)
            || materialMeta.RoleLabel.StartsWith("Output", StringComparison.Ordinal)
            || materialMeta.RoleLabel.StartsWith("Unassociated", StringComparison.Ordinal)
        ).toBe(true)

        let dataMeta =
            metaById
            |> Map.values
            |> Seq.tryFind (fun meta -> meta.Tag = Some(GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data))
            |> expectSome <| "Expected at least one data endpoint metadata entry."

        Vitest.expect(dataMeta.KindLabel).toBe("Data")
        Vitest.expect(dataMeta.GraphKind).toBe(GraphExplorerNodeKind.Data)

        let propertyValueMeta =
            metaById
            |> Map.values
            |> Seq.tryFind (fun meta ->
                match meta.Tag with
                | Some(GraphNodeTag.PropertyValue _) -> true
                | _ -> false)
            |> expectSome <| "Expected at least one property-value metadata entry."

        Vitest.expect(propertyValueMeta.KindLabel).toBe("PropertyValue")
        Vitest.expect(propertyValueMeta.GraphKind).toBe(GraphExplorerNodeKind.PropertyValue)

        let datasetsLayerMeta =
            metaById
            |> Map.tryFind "graph:datasets"
            |> expectSome <| "Expected metadata for datasets top-level layer."

        Vitest.expect(datasetsLayerMeta.Description.IsSome).toBe(true)
        Vitest.expect(
            datasetsLayerMeta.Rows
            |> List.exists (fun (label, _) -> label = "Entries")
        ).toBe(true))

    Vitest.test("renders Inputs, Outputs, Unassociated, and Parameter Values endpoint groups for process nodes", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let allLayer =
            nodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer."

        let processNodes =
            allLayer.children
            |> flattenNodes
            |> Seq.filter (hasTag metaById GraphNodeTag.Process)
            |> Seq.toList

        Vitest.expect(processNodes.Length > 0).toBe(true)

        processNodes
        |> List.iter (fun processNode ->
            Vitest.expect(processNode.children |> List.map _.name).toEqual([
                "Inputs"
                "Outputs"
                "Unassociated"
                "Parameter Values"
            ])

            let inputsGroup =
                processNode.children
                |> List.tryFind (fun child -> child.name = "Inputs")
                |> expectSome <| "Missing Inputs group on process node."

            let outputsGroup =
                processNode.children
                |> List.tryFind (fun child -> child.name = "Outputs")
                |> expectSome <| "Missing Outputs group on process node."

            Vitest.expect(inputsGroup.children |> List.map _.name).toEqual([
                "Material"
                "Data"
            ])

            Vitest.expect(outputsGroup.children |> List.map _.name).toEqual([
                "Material"
                "Data"
            ])))

    Vitest.test("keeps category-layer counts aligned with aggregated source counts across ARCObjects", fun () ->
        let graphObjects = fakeGraphObjects ()
        let stats = collectSourceStats graphObjects
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let allLayer =
            nodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer."

        let canonicalNodes =
            allLayer.children
            |> flattenNodes
            |> Seq.toList

        let materialEndpointTag = GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material
        let dataEndpointTag = GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data

        let datasetNodeCount = canonicalNodes |> List.filter (hasTag metaById GraphNodeTag.Dataset) |> List.length
        let protocolNodeCount = canonicalNodes |> List.filter (hasTag metaById GraphNodeTag.Protocol) |> List.length
        let formalParameterNodeCount = canonicalNodes |> List.filter (hasTag metaById GraphNodeTag.FormalParameter) |> List.length
        let processNodeCount = canonicalNodes |> List.filter (hasTag metaById GraphNodeTag.Process) |> List.length
        let materialEndpointNodeCount = canonicalNodes |> List.filter (hasTag metaById materialEndpointTag) |> List.length
        let dataEndpointNodeCount = canonicalNodes |> List.filter (hasTag metaById dataEndpointTag) |> List.length

        Vitest.expect(allLayer.children.Length).toBe(stats.arcs)
        Vitest.expect(datasetNodeCount).toBe(stats.datasets.Length)
        Vitest.expect(protocolNodeCount).toBe(stats.protocols.Length)
        Vitest.expect(formalParameterNodeCount).toBe(stats.formalParameters.Length)
        Vitest.expect(processNodeCount).toBe(stats.processes.Length)
        Vitest.expect(materialEndpointNodeCount).toBe(stats.materialEndpoints)
        Vitest.expect(dataEndpointNodeCount).toBe(stats.dataEndpoints)

        let datasetsLayer = nodes |> List.tryFind (fun node -> node.id = "graph:datasets") |> expectSome <| "Missing datasets layer."
        let protocolsLayer = nodes |> List.tryFind (fun node -> node.id = "graph:protocols") |> expectSome <| "Missing protocols layer."
        let formalParametersLayer = nodes |> List.tryFind (fun node -> node.id = "graph:formal-parameters") |> expectSome <| "Missing formal-parameters layer."
        let processesLayer = nodes |> List.tryFind (fun node -> node.id = "graph:processes") |> expectSome <| "Missing processes layer."
        let materialsLayer = nodes |> List.tryFind (fun node -> node.id = "graph:materials") |> expectSome <| "Missing materials layer."
        let dataLayer = nodes |> List.tryFind (fun node -> node.id = "graph:Data") |> expectSome <| "Missing data layer."

        Vitest.expect(datasetsLayer.children.Length).toBe(stats.datasets.Length)
        Vitest.expect(protocolsLayer.children.Length).toBe(stats.protocols.Length)
        Vitest.expect(formalParametersLayer.children.Length).toBe(stats.formalParameters.Length)
        Vitest.expect(processesLayer.children.Length).toBe(stats.processes.Length)
        Vitest.expect(materialsLayer.children.Length).toBe(stats.materialEndpoints)
        Vitest.expect(dataLayer.children.Length).toBe(stats.dataEndpoints)

        Vitest.expect(extractEntryCount metaById "graph:datasets").toBe(stats.datasets.Length)
        Vitest.expect(extractEntryCount metaById "graph:protocols").toBe(stats.protocols.Length)
        Vitest.expect(extractEntryCount metaById "graph:formal-parameters").toBe(stats.formalParameters.Length)
        Vitest.expect(extractEntryCount metaById "graph:processes").toBe(stats.processes.Length)
        Vitest.expect(extractEntryCount metaById "graph:materials").toBe(stats.materialEndpoints)
        Vitest.expect(extractEntryCount metaById "graph:Data").toBe(stats.dataEndpoints))

    Vitest.test("covers supported dataset and process endpoint bucket labels on metadata", fun () ->
        let graphObjects = fakeGraphObjects ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let datasetsLayerMeta =
            metaById
            |> Map.tryFind "graph:datasets"
            |> expectSome <| "Missing graph:datasets metadata."

        let datasetCaseLabels =
            datasetsLayerMeta.CaseExamples
            |> List.map fst
            |> Set.ofList

        Vitest.expect(datasetCaseLabels.Contains "Study").toBe(true)
        Vitest.expect(datasetCaseLabels.Contains "Assay").toBe(true)
        Vitest.expect(datasetCaseLabels.Contains "Workflow").toBe(true)
        Vitest.expect(datasetCaseLabels.Contains "Run").toBe(true)

        let allLayer =
            nodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer."

        let endpointMetas =
            let materialEndpointTag = GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material
            let dataEndpointTag = GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data

            allLayer.children
            |> flattenNodes
            |> Seq.choose (fun node ->
                if hasTag metaById materialEndpointTag node || hasTag metaById dataEndpointTag node then
                    metaById |> Map.tryFind node.id
                else
                    None)
            |> Seq.toList

        Vitest.expect(endpointMetas.Length > 0).toBe(true)

        let endpointDirections =
            endpointMetas
            |> List.choose (fun meta ->
                meta.Rows
                |> List.tryPick (fun (label, value) ->
                    if label = "Endpoint Direction" then Some value else None))
            |> Set.ofList

        Vitest.expect(endpointDirections.Contains "Input").toBe(true)
        Vitest.expect(endpointDirections.Contains "Output").toBe(true)
        Vitest.expect(endpointDirections.Contains "Unassociated").toBe(true)

        endpointMetas
        |> List.iter (fun meta ->
            Vitest.expect(
                meta.Rows
                |> List.exists (fun (label, value) ->
                    label = "Material DU Cases"
                    && value = "Sources | Samples")
            ).toBe(true)

            Vitest.expect(
                meta.Rows
                |> List.exists (fun (label, value) ->
                    label = "Data DU Cases"
                    && value = "Files | FragmentSelector")
            ).toBe(true)

            let valueType =
                meta.Rows
                |> List.tryPick (fun (label, value) ->
                    if label = "Value Type" then Some value else None)
                |> expectSome <| "Missing Value Type row on endpoint metadata."

            match valueType with
            | "Material" ->
                Vitest.expect(meta.Rows |> List.exists (fun (label, _) -> label = "Material Role")).toBe(true)
            | "Data" ->
                Vitest.expect(meta.Rows |> List.exists (fun (label, _) -> label = "Data Role")).toBe(true)
            | _ ->
                failwith $"Unexpected endpoint Value Type: {valueType}"))

    Vitest.test("adds grouped PropertyValue nodes with deterministic ids for all supported owners", fun () ->
        let graphObjects = graphObjectsWithPropertyValues ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let allLayer =
            nodes
            |> List.tryFind (fun node -> node.id = "graph:all")
            |> expectSome <| "Missing graph:all layer."

        let canonicalNodes =
            allLayer.children
            |> flattenNodes
            |> Seq.toList

        let findCanonicalNodeByTag (tag: GraphNodeTag) (errorMessage: string) =
            canonicalNodes
            |> List.tryFind (fun node ->
                not node.isReference
                && hasTag metaById tag node)
            |> expectSome <| errorMessage

        let expectPropertyGroup
            (ownerNode: ArcExplorerNode)
            (groupLabel: string)
            (fieldKey: string)
            (expectedOwnerTag: GraphPropertyValueOwnerTag)
            (expectedOwnerKind: string)
            (expectedSourceField: string)
            =
            let groupNode =
                ownerNode.children
                |> List.tryFind (fun child -> child.name = groupLabel)
                |> expectSome <| $"Missing '{groupLabel}' child group on '{ownerNode.id}'."

            Vitest.expect(groupNode.id).toBe($"{ownerNode.id}:group:{fieldKey}")
            Vitest.expect(groupNode.isSelectable).toBe(false)
            Vitest.expect(groupNode.children.Length).toBe(1)

            let valueNode =
                groupNode.children
                |> List.tryHead
                |> expectSome <| $"Missing PropertyValue node in group '{groupNode.id}'."

            Vitest.expect(valueNode.id).toBe($"{ownerNode.id}:{fieldKey}:0")
            Vitest.expect(valueNode.isSelectable).toBe(true)

            let valueMeta =
                metaById
                |> Map.tryFind valueNode.id
                |> expectSome <| $"Missing metadata for PropertyValue node '{valueNode.id}'."

            Vitest.expect(valueMeta.Tag).toEqual(Some(GraphNodeTag.PropertyValue expectedOwnerTag))
            Vitest.expect(valueMeta.Rows |> List.exists (fun (label, value) -> label = "Owner Kind" && value = expectedOwnerKind)).toBe(true)
            Vitest.expect(valueMeta.Rows |> List.exists (fun (label, value) -> label = "Source Field" && value = expectedSourceField)).toBe(true)
            Vitest.expect(valueMeta.Rows |> List.exists (fun (label, _) -> label = "Id")).toBe(true)
            Vitest.expect(valueMeta.Rows |> List.exists (fun (label, _) -> label = "Type")).toBe(true)
            Vitest.expect(valueMeta.Rows |> List.exists (fun (label, _) -> label = "Name")).toBe(true)

        let datasetNode =
            findCanonicalNodeByTag
                GraphNodeTag.Dataset
                "Expected canonical dataset node."

        let protocolNode =
            findCanonicalNodeByTag
                GraphNodeTag.Protocol
                "Expected canonical protocol node."

        let processNode =
            findCanonicalNodeByTag
                GraphNodeTag.Process
                "Expected canonical process node."

        let materialEndpointNode =
            findCanonicalNodeByTag
                (GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material)
                "Expected canonical material endpoint node."

        let dataEndpointNode =
            findCanonicalNodeByTag
                (GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Data)
                "Expected canonical data endpoint node."

        expectPropertyGroup
            datasetNode
            "Additional Properties"
            "additional-property"
            GraphPropertyValueOwnerTag.Dataset
            "Dataset"
            "additionalProperty"

        expectPropertyGroup
            protocolNode
            "Additional Properties"
            "additional-property"
            GraphPropertyValueOwnerTag.Protocol
            "Protocol"
            "additionalProperty"

        expectPropertyGroup
            processNode
            "Parameter Values"
            "parameter-value"
            GraphPropertyValueOwnerTag.Process
            "Process"
            "parameterValue"

        expectPropertyGroup
            materialEndpointNode
            "Additional Properties"
            "additional-property"
            (GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Material)
            "Material Endpoint"
            "additionalProperty"

        expectPropertyGroup
            dataEndpointNode
            "Additional Properties"
            "additional-property"
            (GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Data)
            "Data Endpoint"
            "additionalProperty")

    Vitest.test("keeps PropertyValue nodes visible in matching semantic filters", fun () ->
        let graphObjects = graphObjectsWithPropertyValues ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcObjects graphObjects

        let propertyOwnerTagsForSemanticKind (semanticKind: string) =
            let filteredNodes =
                GraphObjectExplorerFilter.filterNodesBySemanticKinds
                    (Set.ofList [ semanticKind ])
                    nodes
                    metaById
                    None

            let allLayer =
                filteredNodes
                |> List.tryFind (fun node -> node.id = "graph:all")
                |> expectSome <| $"Missing graph:all layer for semantic kind '{semanticKind}'."

            allLayer.children
            |> flattenNodes
            |> Seq.choose (fun node ->
                metaById
                |> Map.tryFind node.id
                |> Option.bind (fun meta ->
                    match meta.Tag with
                    | Some(GraphNodeTag.PropertyValue ownerTag) -> Some ownerTag
                    | _ -> None))
            |> Set.ofSeq

        let assertSingleOwnerTag (semanticKind: string) (expectedOwnerTag: GraphPropertyValueOwnerTag) =
            let ownerTags = propertyOwnerTagsForSemanticKind semanticKind

            Vitest.expect(ownerTags.Count).toBe(1)
            Vitest.expect(ownerTags.Contains expectedOwnerTag).toBe(true)

        let datasetOwnerTags = propertyOwnerTagsForSemanticKind "Datasets"
        Vitest.expect(datasetOwnerTags.Count).toBe(0)

        assertSingleOwnerTag "Study" GraphPropertyValueOwnerTag.Dataset
        assertSingleOwnerTag "Protocols" GraphPropertyValueOwnerTag.Protocol
        assertSingleOwnerTag "Processes" GraphPropertyValueOwnerTag.Process

        assertSingleOwnerTag
            "Materials"
            (GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Material)

        assertSingleOwnerTag
            "Data"
            (GraphPropertyValueOwnerTag.ProcessEndpoint GraphProcessEndpointValueType.Data))
)
