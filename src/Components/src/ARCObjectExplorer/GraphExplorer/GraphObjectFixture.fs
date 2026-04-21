module Swate.Components.ARCObjectExplorer.GraphExplorer.GraphObjectFixture

open Swate.Components.FileExplorerTypes
open Swate.Components.ARCObjectExplorer.GraphExplorer.Model

module private GraphObjectFixtureHelper =

    let kindLabel =
        function
        | ARCDatasets.Assay -> "Assay"
        | ARCDatasets.Study -> "Study"
        | ARCDatasets.Workflow -> "Workflow"
        | ARCDatasets.Run -> "Run"

    let makeMaterial(id: string, name: string, materialType: string, isSource: bool) =
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

    let makeFilesData(id: string, name: string, path: string, dataType: string) =
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

    let makeFragmentSelectorData
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

    let makeProcess
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

    let makeFormalParameter
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

    let makeProtocol
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

    let makeDataset
        (
            kind: ARCDatasets,
            identifier: string,
            name: string,
            description: string,
            aboutProtocols: Protocol list
        )
        : Dataset =
        {
            id = $"{(kindLabel kind).ToLowerInvariant()}:{identifier}"
            type' = kind
            additionalType = kindLabel kind
            identifier = identifier
            name = name
            description = description
            hasPart = None
            additionalProperty = None
            about = aboutProtocols
        }

    let collapseExplorerItems (items: FileItem list) =
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

    let fakeGraphModel() : ARCGraph =
        let sourceLeaf = makeMaterial("source:leaf-a", "Leaf-A", "Source", true)
        let sampleLeaf = makeMaterial("sample:leaf-a", "Leaf-A", "Sample", false)
        let extractMaterial = makeMaterial("material:extract-a", "Leaf-Extract-A", "Extract", false)
        let replicateMaterial =
            makeMaterial("sample:leaf-a-replicate", "Leaf-A Replicate", "Sample", false)
        let cultivationLogData =
            makeFilesData("data:cultivation-log", "Cultivation Log", "studies/drought-response/cultivation-log.tsv", "Data")
        let extractionManifestData =
            makeFilesData("data:extraction-manifest", "Extraction Manifest", "workflows/extraction/extraction-manifest.tsv", "Data")
        let metadataData = makeFilesData("data:sample-sheet", "Sample Sheet", "assays/metabolomics/sample-sheet.tsv", "Data")
        let quantData = makeFilesData("data:feature-table", "Feature Table", "assays/metabolomics/feature-table.tsv", "Data")
        let fragmentData =
            makeFragmentSelectorData(
                "data:feature-window",
                "Feature Window",
                "assays/metabolomics/feature-table.tsv",
                "Data",
                "mz=100-1000;rt=0-900",
                "MS:1000515"
            )
        let fragmentReviewData =
            makeFragmentSelectorData(
                "data:feature-window-reviewed",
                "Feature Window (Reviewed)",
                "assays/metabolomics/feature-table.tsv",
                "Data",
                "mz=120-950;rt=30-840",
                "MS:1000515"
            )
        let cultivationFragmentData =
            makeFragmentSelectorData(
                "data:cultivation-window",
                "Cultivation Window",
                "studies/drought-response/cultivation-log.tsv",
                "Data",
                "day=0-21;light=16h",
                "SWATE:FRAGMENT:CULTIVATION"
            )
        let extractionSourceMaterial =
            makeMaterial("source:extraction-reference", "Extraction Reference Leaf", "Source", true)
        let extractionFragmentData =
            makeFragmentSelectorData(
                "data:extraction-window",
                "Extraction Window",
                "workflows/extraction/extraction-manifest.tsv",
                "Data",
                "fraction=polar;replicate=1-3",
                "SWATE:FRAGMENT:EXTRACTION"
            )
        let measurementSourceMaterial =
            makeMaterial("source:measurement-reference", "Measurement Reference Extract", "Source", true)
        let measurementSampleMaterial =
            makeMaterial("sample:measurement-qc", "Measurement QC Sample", "Sample", false)
        let measurementFragmentData =
            makeFragmentSelectorData(
                "data:measurement-window",
                "Measurement Window",
                "assays/metabolomics/feature-table.tsv",
                "Data",
                "mz=150-900;rt=60-780",
                "MS:1000515"
            )
        let runSourceMaterial =
            makeMaterial("source:run-reference", "Run Reference Extract", "Source", true)
        let runLogData =
            makeFilesData("data:run-source-log", "Run Source Log", "runs/week-01/source-log.tsv", "Data")

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
            makeProcess(
                "Grow plants",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf replicateMaterial cultivationLogData cultivationFragmentData
            )

        let cultivationInputProcess =
            makeProcess(
                "Register source material",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData
            )

        let sourceSnapshotProcess =
            makeProcess(
                "Snapshot source inventory",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf replicateMaterial cultivationLogData cultivationFragmentData
            )

        let replicatePreparationProcess =
            makeProcess(
                "Prepare sample replicate",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf replicateMaterial cultivationLogData cultivationFragmentData
            )

        let cultivationFragmentSelectionProcess =
            makeProcess(
                "Define cultivation observation window",
                cultivationProtocolId,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData,
                completeEndpointSet sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData
            )

        let extractionSourceRegistrationProcess =
            makeProcess(
                "Register extraction source material",
                extractionProtocolId,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData
            )

        let extractionProcess =
            makeProcess(
                "Extract metabolites",
                extractionProtocolId,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData,
                completeEndpointSet extractionSourceMaterial extractMaterial metadataData extractionFragmentData
            )

        let annotationProcess =
            makeProcess(
                "Attach sample metadata",
                extractionProtocolId,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData,
                completeEndpointSet extractionSourceMaterial extractMaterial metadataData extractionFragmentData
            )

        let extractionFragmentSelectionProcess =
            makeProcess(
                "Define extraction window",
                extractionProtocolId,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData,
                completeEndpointSet extractionSourceMaterial extractMaterial extractionManifestData extractionFragmentData
            )

        let measurementSourceRegistrationProcess =
            makeProcess(
                "Register measurement source material",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData
            )

        let measurementProcess =
            makeProcess(
                "Quantify features",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial quantData measurementFragmentData
            )

        let measurementInputProcess =
            makeProcess(
                "Prepare acquisition metadata",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData
            )

        let measurementSamplePreparationProcess =
            makeProcess(
                "Prepare measurement QC sample",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial metadataData measurementFragmentData
            )

        let measurementFragmentSelectionProcess =
            makeProcess(
                "Define measurement window",
                measurementProtocolId,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial quantData measurementFragmentData,
                completeEndpointSet measurementSourceMaterial measurementSampleMaterial quantData measurementFragmentData
            )

        let runSourceRegistrationProcess =
            makeProcess(
                "Register run source material",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial runLogData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial runLogData fragmentReviewData
            )

        let injectionProcess =
            makeProcess(
                "Inject extracts",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial runLogData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData
            )

        let fragmentSelectionProcess =
            makeProcess(
                "Select fragment window",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData
            )

        let fragmentReviewProcess =
            makeProcess(
                "Review fragment window",
                runProtocolId,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentData,
                completeEndpointSet runSourceMaterial extractMaterial quantData fragmentReviewData
            )

        let fragmentRefinementProcess =
            makeProcess(
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
            makeProtocol(
                cultivationProtocolId,
                "Plant Cultivation",
                "Prepare biological source material before extraction.",
                "Generate healthy source and sample material.",
                [
                    makeFormalParameter(
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
            makeProtocol(
                extractionProtocolId,
                "Extraction",
                "Transform samples into assay-ready extracts.",
                "Generate measurable extract material.",
                [
                    makeFormalParameter(
                        "fp:solvent",
                        "Extraction Solvent",
                        "Solvent composition used for extraction.",
                        "Control extraction chemistry.",
                        extractionProcesses
                    )
                    makeFormalParameter(
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
            makeProtocol(
                measurementProtocolId,
                "LC-MS Measurement",
                "Instrument acquisition for metabolomics feature detection.",
                "Produce quantified metabolite features.",
                [
                    makeFormalParameter(
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
            makeProtocol(
                runProtocolId,
                "Week 1 Injection",
                "Run-specific acquisition execution.",
                "Track one acquisition cycle.",
                [
                    makeFormalParameter(
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
            makeDataset(
                ARCDatasets.Study,
                "study-drought-response",
                "Drought Response Study",
                "Field study tracking response to drought stress.",
                [ cultivationProtocol ]
            )

        let assayDataset =
            makeDataset(
                ARCDatasets.Assay,
                "assay-metabolomics",
                "Metabolomics Assay",
                "LC-MS assay for drought response metabolites.",
                [ measurementProtocol ]
            )

        let workflowDataset =
            makeDataset(
                ARCDatasets.Workflow,
                "workflow-extraction",
                "Extraction Workflow",
                "Workflow transforming leaf material to assay-ready extracts.",
                [ extractionProtocol ]
            )

        let runDataset =
            makeDataset(
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

    let fakeGraphModels() : ARCGraph list =
        let primaryArc = fakeGraphModel ()

        let secondaryArc = {
            primaryArc with
                path = "C:/example/arc-graph-secondary"
                Datasets =
                    primaryArc.Datasets
                    |> List.map (fun dataset -> {
                        dataset with
                            id = $"{dataset.id}-secondary"
                            identifier = $"{dataset.identifier}-secondary"
                            name = $"{dataset.name} (Secondary ARC)"
                    })
        }

        [ primaryArc; secondaryArc ]

let collapseExplorerItems(items: FileItem list) =
    GraphObjectFixtureHelper.collapseExplorerItems items

let fakeGraphModels() =
    GraphObjectFixtureHelper.fakeGraphModels ()

