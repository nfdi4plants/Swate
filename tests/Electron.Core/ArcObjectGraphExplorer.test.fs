module ElectronCore.ArcObjectGraphExplorerTests

open System
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer.GraphExplorer
open Swate.Components.ARCObjectExplorer.GraphExplorer.GraphObjectFixture
open Swate.Components.ARCObjectExplorer.GraphExplorer.ArcExplorerNodes
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model
open Vitest

let rec private flattenNodes (nodes: ArcExplorerNode list) =
    seq {
        for node in nodes do
            yield node
            yield! flattenNodes node.children
    }

let private expectSome value errorMessage =
    match value with
    | Some value -> value
    | None -> failwith errorMessage

let private hasTag (metaById: Map<string, GraphNodeMeta>) (expectedTag: GraphNodeTag) (node: ArcExplorerNode) =
    metaById
    |> Map.tryFind node.id
    |> Option.exists (fun meta -> meta.Tag = Some expectedTag)

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

let private collectSourceStats (models: ARCGraph list) =
    let datasets =
        models
        |> List.collect _.Datasets

    let protocols =
        datasets
        |> List.collect _.about

    let formalParameters =
        protocols
        |> List.collect _.formalParameters

    let processes =
        protocols
        |> List.collect _.processes

    let processValues =
        processes
        |> List.map (function
            | CurrentProcess.Input processValue
            | CurrentProcess.Output processValue -> processValue)

    let processEndpoints =
        processValues
        |> List.collect (fun processValue -> processValue.object @ processValue.result)

    let materialEndpoints =
        processEndpoints
        |> List.filter (function
            | ProcessType.Material _ -> true
            | _ -> false)

    let dataEndpoints =
        processEndpoints
        |> List.filter (function
            | ProcessType.Data _ -> true
            | _ -> false)

    {| datasets = datasets
       protocols = protocols
       formalParameters = formalParameters
       processes = processes
       processEndpoints = processEndpoints
       materialEndpoints = materialEndpoints
       dataEndpoints = dataEndpoints |}

Vitest.describe("ToArcExplorerNodes graph conversion", fun () ->
    Vitest.test("builds expected top-level shape for graph explorer", fun () ->
        let models = fakeGraphModels ()
        let nodes, _ = toArcExplorerNodesWithMetaFromArcs models

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
        Vitest.expect(allLayer.children.Length).toBe(models.Length))

    Vitest.test("emits metadata labels for canonical datasets and process endpoints", fun () ->
        let models = fakeGraphModels ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcs models

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
        Vitest.expect(datasetMeta.RoleLabel).toBe("Canonical")

        let materialMeta =
            metaById
            |> Map.values
            |> Seq.tryFind (fun meta -> meta.Tag = Some(GraphNodeTag.ProcessEndpoint GraphProcessEndpointValueType.Material))
            |> expectSome <| "Expected at least one material endpoint metadata entry."

        Vitest.expect(materialMeta.KindLabel).toBe("Material")
        Vitest.expect(
            materialMeta.RoleLabel.StartsWith("Object", StringComparison.Ordinal)
            || materialMeta.RoleLabel.StartsWith("Result", StringComparison.Ordinal)
        ).toBe(true)

        let datasetsLayerMeta =
            metaById
            |> Map.tryFind "graph:datasets"
            |> expectSome <| "Expected metadata for datasets top-level layer."

        Vitest.expect(datasetsLayerMeta.Description.IsSome).toBe(true)
        Vitest.expect(
            datasetsLayerMeta.Rows
            |> List.exists (fun (label, _) -> label = "Entries")
        ).toBe(true))

    Vitest.test("keeps category-layer counts aligned with aggregated source counts across all ARCs", fun () ->
        let models = fakeGraphModels ()
        let stats = collectSourceStats models
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcs models

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

        Vitest.expect(datasetNodeCount).toBe(stats.datasets.Length)
        Vitest.expect(protocolNodeCount).toBe(stats.protocols.Length)
        Vitest.expect(formalParameterNodeCount).toBe(stats.formalParameters.Length)
        Vitest.expect(processNodeCount).toBe(stats.processes.Length)
        Vitest.expect(materialEndpointNodeCount).toBe(stats.materialEndpoints.Length)
        Vitest.expect(dataEndpointNodeCount).toBe(stats.dataEndpoints.Length)

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
        Vitest.expect(materialsLayer.children.Length).toBe(stats.materialEndpoints.Length)
        Vitest.expect(dataLayer.children.Length).toBe(stats.dataEndpoints.Length)

        Vitest.expect(extractEntryCount metaById "graph:datasets").toBe(stats.datasets.Length)
        Vitest.expect(extractEntryCount metaById "graph:protocols").toBe(stats.protocols.Length)
        Vitest.expect(extractEntryCount metaById "graph:formal-parameters").toBe(stats.formalParameters.Length)
        Vitest.expect(extractEntryCount metaById "graph:processes").toBe(stats.processes.Length)
        Vitest.expect(extractEntryCount metaById "graph:materials").toBe(stats.materialEndpoints.Length)
        Vitest.expect(extractEntryCount metaById "graph:Data").toBe(stats.dataEndpoints.Length))

    Vitest.test("covers supported union-case labels on dataset and process endpoint metadata", fun () ->
        let models = fakeGraphModels ()
        let nodes, metaById = toArcExplorerNodesWithMetaFromArcs models

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
)
