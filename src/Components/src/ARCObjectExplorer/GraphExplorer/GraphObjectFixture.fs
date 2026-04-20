namespace Swate.Components.ARCObjectExplorer.GraphExplorer

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.ARCObjectExplorer
open Swate.Components.FileExplorerTypes

[<Erase; Mangle(false)>]
type GraphObjectFixture =

    static member private kindLabel =
        function
        | ARCDatasets.Assay -> "Assay"
        | ARCDatasets.Study -> "Study"
        | ARCDatasets.Workflow -> "Workflow"
        | ARCDatasets.Run -> "Run"

    static member private makeMaterial(id: string, name: string, materialType: string, isSource: bool) =
        let material = {
            id = id
            type' = materialType
            name = name
            additionalProperty = None
        }

        if isSource then
            ProcessType.Material(ARCMaterial.Sources material)
        else
            ProcessType.Material(ARCMaterial.Samples material)

    static member private makeFilesData(id: string, name: string, path: string, dataType: string) =
        ARCData.Files {
            id = Some id
            type' = dataType
            additionalType = None
            path = path
            selector = None
            selectorFormat = None
            encodingFormat = Some "text/tab-separated-values"
            name = Some name
            additionalProperty = None
        }
        |> ProcessType.Data

    static member private makeFragmentSelectorData
        (
            id: string,
            name: string,
            path: string,
            dataType: string,
            selector: string,
            selectorFormat: string
        ) =
        ARCData.FragmentSelector {
            id = Some id
            type' = dataType
            additionalType = Some "FragmentSelector"
            path = path
            selector = Some selector
            selectorFormat = Some selectorFormat
            encodingFormat = Some "application/json"
            name = Some name
            additionalProperty = None
        }
        |> ProcessType.Data

    static member private makeProcess
        (
            name: string,
            executesProtocol: string,
            object': ProcessType list,
            result: ProcessType list
        )
        : Process =
        {
            id = Some(($"{executesProtocol}:{name}").ToLowerInvariant().Replace(" ", "-"))
            type' = "Process"
            additionalType = None
            name = name
            object = object'
            result = result
            ExecutesProtocol = executesProtocol
            parameterValue = None
        }

    static member private makeFormalParameter
        (
            id: string,
            name: string,
            description: string,
            intendedUse: string,
            processes: CurrentProcess list
        )
        : FormalParameter =
        {
            id = Some id
            type' = "FormalParameter"
            additionalType = None
            name = name
            description = description
            intendedUse = intendedUse
            additionalProperty = None
            version = None
            url = None
            processes = processes
        }

    static member private makeProtocol
        (
            id: string,
            name: string,
            description: string,
            intendedUse: string,
            formalParameters: FormalParameter list,
            processes: CurrentProcess list
        )
        : Protocol =
        {
            id = Some id
            type' = "Protocol"
            additionalType = None
            name = name
            description = description
            intendedUse = intendedUse
            additionalProperty = None
            version = Some "1.0.0"
            url = None
            processes = processes
            formalParameters = formalParameters
        }

    static member private makeDataset
        (
            kind: ARCDatasets,
            identifier: string,
            name: string,
            description: string,
            aboutProtocols: Protocol list
        )
        : Dataset =
        {
            id = $"{(GraphObjectFixture.kindLabel kind).ToLowerInvariant()}:{identifier}"
            type' = kind
            additionalType = GraphObjectFixture.kindLabel kind
            identifier = identifier
            name = name
            description = description
            hasPart = None
            additionalProperty = None
            about = aboutProtocols
        }

    static member private collapseExplorerItems (items: FileItem list) =
        let rec collapseItem (item: FileItem) =
            let collapsedChildren =
                item.Children
                |> Option.map (List.map collapseItem)

            {
                item with
                    IsExpanded = false
                    Children = collapsedChildren
            }

        items |> List.map collapseItem

    static member private fakeGraphModel() : ARC =
        let sourceLeaf = GraphObjectFixture.makeMaterial("source:leaf-a", "Leaf-A", "Source", true)
        let sampleLeaf = GraphObjectFixture.makeMaterial("sample:leaf-a", "Leaf-A", "Sample", false)
        let extractMaterial = GraphObjectFixture.makeMaterial("material:extract-a", "Leaf-Extract-A", "Extract", false)
        let replicateMaterial =
            GraphObjectFixture.makeMaterial("sample:leaf-a-replicate", "Leaf-A Replicate", "Sample", false)
        let cultivationLogData =
            GraphObjectFixture.makeFilesData("data:cultivation-log", "Cultivation Log", "studies/drought-response/cultivation-log.tsv", "Data")
        let extractionManifestData =
            GraphObjectFixture.makeFilesData("data:extraction-manifest", "Extraction Manifest", "workflows/extraction/extraction-manifest.tsv", "Data")
        let metadataData = GraphObjectFixture.makeFilesData("data:sample-sheet", "Sample Sheet", "assays/metabolomics/sample-sheet.tsv", "Data")
        let quantData = GraphObjectFixture.makeFilesData("data:feature-table", "Feature Table", "assays/metabolomics/feature-table.tsv", "Data")
        let fragmentData =
            GraphObjectFixture.makeFragmentSelectorData(
                "data:feature-window",
                "Feature Window",
                "assays/metabolomics/feature-table.tsv",
                "Data",
                "mz=100-1000;rt=0-900",
                "MS:1000515"
            )
        let fragmentReviewData =
            GraphObjectFixture.makeFragmentSelectorData(
                "data:feature-window-reviewed",
                "Feature Window (Reviewed)",
                "assays/metabolomics/feature-table.tsv",
                "Data",
                "mz=120-950;rt=30-840",
                "MS:1000515"
            )
        let cultivationFragmentData =
            GraphObjectFixture.makeFragmentSelectorData(
                "data:cultivation-window",
                "Cultivation Window",
                "studies/drought-response/cultivation-log.tsv",
                "Data",
                "day=0-21;light=16h",
                "SWATE:FRAGMENT:CULTIVATION"
            )
        let extractionSourceMaterial =
            GraphObjectFixture.makeMaterial("source:extraction-reference", "Extraction Reference Leaf", "Source", true)
        let extractionFragmentData =
            GraphObjectFixture.makeFragmentSelectorData(
                "data:extraction-window",
                "Extraction Window",
                "workflows/extraction/extraction-manifest.tsv",
                "Data",
                "fraction=polar;replicate=1-3",
                "SWATE:FRAGMENT:EXTRACTION"
            )
        let measurementSourceMaterial =
            GraphObjectFixture.makeMaterial("source:measurement-reference", "Measurement Reference Extract", "Source", true)
        let measurementSampleMaterial =
            GraphObjectFixture.makeMaterial("sample:measurement-qc", "Measurement QC Sample", "Sample", false)
        let measurementFragmentData =
            GraphObjectFixture.makeFragmentSelectorData(
                "data:measurement-window",
                "Measurement Window",
                "assays/metabolomics/feature-table.tsv",
                "Data",
                "mz=150-900;rt=60-780",
                "MS:1000515"
            )
        let runSourceMaterial =
            GraphObjectFixture.makeMaterial("source:run-reference", "Run Reference Extract", "Source", true)
        let runLogData =
            GraphObjectFixture.makeFilesData("data:run-source-log", "Run Source Log", "runs/week-01/source-log.tsv", "Data")

        let cultivationProtocolId = "study:protocol:cultivation"
        let extractionProtocolId = "workflow:protocol:extraction"
        let measurementProtocolId = "assay:protocol:lcms-measurement"
        let runProtocolId = "run:protocol:week-01"

        let completeEndpointSet
            (sourceMaterial: ProcessType)
            (sampleMaterial: ProcessType)
            (filesData: ProcessType)
            (fragmentSelectorData: ProcessType)
            =
            [ sourceMaterial; sampleMaterial; filesData; fragmentSelectorData ]

        let cultivationProcess =
            GraphObjectFixture.makeProcess(
                "Grow plants",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf replicateMaterial cultivationLogData cultivationFragmentData
            )

        let cultivationInputProcess =
            GraphObjectFixture.makeProcess(
                "Register source material",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData
            )

        let sourceSnapshotProcess =
            GraphObjectFixture.makeProcess(
                "Snapshot source inventory",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf replicateMaterial cultivationLogData cultivationFragmentData
            )

        let replicatePreparationProcess =
            GraphObjectFixture.makeProcess(
                "Prepare sample replicate",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf replicateMaterial cultivationLogData cultivationFragmentData
            )

        let cultivationFragmentSelectionProcess =
            GraphObjectFixture.makeProcess(
                "Define cultivation observation window",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData
            )

        let extractionSourceRegistrationProcess =
            GraphObjectFixture.makeProcess(
                "Register extraction source material",
                extractionProtocolId,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData
            )

        let extractionProcess =
            GraphObjectFixture.makeProcess(
                "Extract metabolites",
                extractionProtocolId,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData,
                completeEndpointSet extractionSourceMaterial extractMaterial metadataData extractionFragmentData
            )

        let annotationProcess =
            GraphObjectFixture.makeProcess(
                "Attach sample metadata",
                extractionProtocolId,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData,
                completeEndpointSet extractionSourceMaterial extractMaterial metadataData extractionFragmentData
            )

        let extractionFragmentSelectionProcess =
            GraphObjectFixture.makeProcess(
                "Define extraction window",
                extractionProtocolId,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData
            )

        let measurementSourceRegistrationProcess =
            GraphObjectFixture.makeProcess(
                "Register measurement source material",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData
            )

        let measurementProcess =
            GraphObjectFixture.makeProcess(
                "Quantify features",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial quantData measurementFragmentData
            )

        let measurementInputProcess =
            GraphObjectFixture.makeProcess(
                "Prepare acquisition metadata",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData
            )

        let measurementSamplePreparationProcess =
            GraphObjectFixture.makeProcess(
                "Prepare measurement QC sample",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData
            )

        let measurementFragmentSelectionProcess =
            GraphObjectFixture.makeProcess(
                "Define measurement window",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial quantData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial quantData measurementFragmentData
            )

        let runSourceRegistrationProcess =
            GraphObjectFixture.makeProcess(
                "Register run source material",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial runLogData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial runLogData fragmentReviewData
            )

        let injectionProcess =
            GraphObjectFixture.makeProcess(
                "Inject extracts",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial runLogData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData
            )

        let fragmentSelectionProcess =
            GraphObjectFixture.makeProcess(
                "Select fragment window",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData
            )

        let fragmentReviewProcess =
            GraphObjectFixture.makeProcess(
                "Review fragment window",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentReviewData
            )

        let fragmentRefinementProcess =
            GraphObjectFixture.makeProcess(
                "Refine fragment window",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentReviewData
            )

        let cultivationProcesses = [
            CurrentProcess.Input cultivationInputProcess
            CurrentProcess.Output cultivationProcess
            CurrentProcess.Output sourceSnapshotProcess
            CurrentProcess.Output replicatePreparationProcess
            CurrentProcess.Output cultivationFragmentSelectionProcess
        ]
        let extractionProcesses = [
            CurrentProcess.Input extractionSourceRegistrationProcess
            CurrentProcess.Input extractionProcess
            CurrentProcess.Output annotationProcess
            CurrentProcess.Output extractionFragmentSelectionProcess
        ]
        let measurementProcesses = [
            CurrentProcess.Input measurementSourceRegistrationProcess
            CurrentProcess.Input measurementInputProcess
            CurrentProcess.Output measurementProcess
            CurrentProcess.Output measurementSamplePreparationProcess
            CurrentProcess.Output measurementFragmentSelectionProcess
        ]
        let runProcesses = [
            CurrentProcess.Input runSourceRegistrationProcess
            CurrentProcess.Input injectionProcess
            CurrentProcess.Input fragmentReviewProcess
            CurrentProcess.Output fragmentSelectionProcess
            CurrentProcess.Output fragmentRefinementProcess
        ]

        let cultivationProtocol =
            GraphObjectFixture.makeProtocol(
                cultivationProtocolId,
                "Plant Cultivation",
                "Prepare biological source material before extraction.",
                "Generate healthy source and sample material.",
                [
                    GraphObjectFixture.makeFormalParameter(
                        "fp:light-cycle",
                        "Light Cycle",
                        "Configured day/night cycle.",
                        "Keep light cycle reproducible.",
                        cultivationProcesses
                    )
                ],
                cultivationProcesses
            )

        let extractionProtocol =
            GraphObjectFixture.makeProtocol(
                extractionProtocolId,
                "Extraction",
                "Transform samples into assay-ready extracts.",
                "Generate measurable extract material.",
                [
                    GraphObjectFixture.makeFormalParameter(
                        "fp:solvent",
                        "Extraction Solvent",
                        "Solvent composition used for extraction.",
                        "Control extraction chemistry.",
                        extractionProcesses
                    )
                    GraphObjectFixture.makeFormalParameter(
                        "fp:temperature",
                        "Extraction Temperature",
                        "Extraction temperature in degrees Celsius.",
                        "Maintain extraction consistency.",
                        extractionProcesses
                    )
                ],
                extractionProcesses
            )

        let measurementProtocol =
            GraphObjectFixture.makeProtocol(
                measurementProtocolId,
                "LC-MS Measurement",
                "Instrument acquisition for metabolomics feature detection.",
                "Produce quantified metabolite features.",
                [
                    GraphObjectFixture.makeFormalParameter(
                        "fp:instrument",
                        "Instrument Model",
                        "Mass spectrometer platform identifier.",
                        "Trace acquisition platform.",
                        measurementProcesses
                    )
                ],
                measurementProcesses
            )

        let runProtocol =
            GraphObjectFixture.makeProtocol(
                runProtocolId,
                "Week 1 Injection",
                "Run-specific acquisition execution.",
                "Track one acquisition cycle.",
                [
                    GraphObjectFixture.makeFormalParameter(
                        "fp:batch",
                        "Batch Identifier",
                        "Run batch label.",
                        "Tie measurements to run logistics.",
                        runProcesses
                    )
                ],
                runProcesses
            )

        let studyDataset =
            GraphObjectFixture.makeDataset(
                ARCDatasets.Study,
                "study-drought-response",
                "Drought Response Study",
                "Field study tracking response to drought stress.",
                [ cultivationProtocol ]
            )

        let assayDataset =
            GraphObjectFixture.makeDataset(
                ARCDatasets.Assay,
                "assay-metabolomics",
                "Metabolomics Assay",
                "LC-MS assay for drought response metabolites.",
                [ measurementProtocol ]
            )

        let workflowDataset =
            GraphObjectFixture.makeDataset(
                ARCDatasets.Workflow,
                "workflow-extraction",
                "Extraction Workflow",
                "Workflow transforming leaf material to assay-ready extracts.",
                [ extractionProtocol ]
            )

        let runDataset =
            GraphObjectFixture.makeDataset(
                ARCDatasets.Run,
                "run-week-01",
                "Week 1 Instrument Run",
                "First acquisition run in the drought series.",
                [ runProtocol ]
            )

        {
            path = "C:/example/arc-graph"
            Datasets = [ studyDataset; assayDataset; workflowDataset; runDataset ]
        }

    [<ReactComponent>]
    static member private StoryExample() =
        let graphModel = React.useMemo ((fun () -> GraphObjectFixture.fakeGraphModel ()), [||])

        let nodes, nodeMetaById =
            React.useMemo ((fun () -> ToArcExplorerNodes.toArcExplorerNodesWithMeta graphModel), [| box graphModel |])

        let selection, setSelection = React.useState ArcSelection.empty

        let selectedKindIndices, setSelectedKindIndices =
            React.useState (ARCObjectWidget.DefaultKindFilterIndices())

        let viewModel =
            ArcObjectExplorerView.create
                nodes
                selection
                selectedKindIndices

        let collapsedExplorerItems =
            React.useMemo (
                (fun () -> GraphObjectFixture.collapseExplorerItems viewModel.ExplorerItems),
                [| box viewModel.ExplorerItems |]
            )

        let setExplorerSelection (nodeId: string) (path: string option) =
            setSelection (ArcSelection.forExplorerNode nodeId path)

        let searchAction =
            ARCObjectWidget.SearchAction(
                viewModel.SearchItems,
                (fun (name, _, _) -> name),
                (fun (_, _, item) ->
                    if item.Selectable then
                        setExplorerSelection item.Id item.Path),
                itemSubtitle = (fun (_, subtitle, _) -> subtitle),
                placeholder = "Search graph objects..."
            )

        let treePane =
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = collapsedExplorerItems,
                ?selectedItemId = Some(ArcObjectExplorerView.selectedItemId viewModel),
                onItemClick =
                    (fun item ->
                        if item.Selectable then
                            setExplorerSelection item.Id item.Path),
                showBreadcrumbs = false,
                useDirectoryChevronToggle = true
            )

        let explorerPane =
            ARCObjectWidget.ExplorerContent(
                collapsedExplorerItems,
                ?selectedItemId = ArcObjectExplorerView.selectedItemId viewModel,
                onItemClick =
                    (fun item ->
                        if item.Selectable then
                            setExplorerSelection item.Id item.Path)
            )

        let detailsPane =
            GraphObjectDetails.Main(
                ArcObjectExplorerView.selectedNode viewModel,
                ArcObjectExplorerView.selectedAncestors viewModel,
                nodeMetaById,
                (fun nodeId ->
                    match ARCExplorer.tryFindNodeById nodeId nodes with
                    | Some node -> setExplorerSelection node.id node.path
                    | None -> setSelection (ArcSelection.forExplorerNode nodeId None))
            )

        ARCObjectWidget.Main(
            navbar =
                ARCObjectWidget.Navbar(
                    ArcObjectExplorerView.selectedTitle viewModel,
                    ArcObjectExplorerView.selectedSubtitle viewModel,
                    selectedKindIndices,
                    setSelectedKindIndices,
                    rightActions = searchAction
                ),
            treePane = treePane,
            explorerPane = explorerPane,
            detailsPane = detailsPane
        )

    [<ReactComponent>]
    static member Entry() =
        Html.div [
            prop.className "swt:min-h-screen swt:bg-base-200 swt:p-6"
            prop.children [ GraphObjectFixture.StoryExample() ]
        ]
