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

    let makeProperty(id: string, name: string, value: string) : PropertyValue = {
        id = id
        type' = "PropertyValue"
        additionalType = None
        name = name
        value = Some value
        unit = None
        nameTAN = None
        valueTAN = None
        unitTAN = None
    }

    let sanitizeSegment (value: string) =
        value
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(":", "-")

    let makeOwnerProperty (ownerKind: string) (ownerId: string) (name: string) (value: string) =
        makeProperty(
            $"prop:{ownerKind}:{sanitizeSegment ownerId}",
            name,
            value
        )

    let makeMaterial(id: string, name: string, materialType: string, additionalType: string option) : Material = {
        id = id
        type' = materialType
        additionalType = additionalType
        name = name
        additionalProperty = [|
            makeOwnerProperty "material" id "Material Property" materialType
        |]
    }

    let makeFilesData(id: string, path: string, dataType: string) : Data = {
        id = Some id
        type' = dataType
        additionalType = None
        path = path
        selector = None
        selectorFormat = None
        encodingFormat = Some "text/tab-separated-values"
        additionalProperty = [|
            makeOwnerProperty "data" id "Data Property" path
        |]
    }

    let makeFragmentSelectorData
        (
            id: string,
            path: string,
            dataType: string,
            selector: string,
            selectorFormat: string
        ) : Data =
        {
            id = Some id
            type' = dataType
            additionalType = Some "FragmentSelector"
            path = path
            selector = Some selector
            selectorFormat = Some selectorFormat
            encodingFormat = Some "application/json"
            additionalProperty = [|
                makeOwnerProperty "data" id "Data Property" selector
            |]
        }

    let makeProcessType
        (
            sourceMaterials: Material list,
            sampleMaterials: Material list,
            filesData: Data list,
            fragmentSelectorData: Data list
        )
        : ProcessType =
        let materialKinds : MaterialKinds = {
            Sources = sourceMaterials |> List.toArray
            Samples = sampleMaterials |> List.toArray
        }

        let dataKinds : DataKinds = {
            Files = filesData |> List.toArray
            FragmentSelector = fragmentSelectorData |> List.toArray
        }

        {
            Materials = [| materialKinds |]
            Data = [| dataKinds |]
        }

    let processTypeMaterials (processType: ProcessType) =
        processType.Materials
        |> Array.toList
        |> List.collect (fun materialKinds ->
            [
                yield! materialKinds.Sources |> Array.toList
                yield! materialKinds.Samples |> Array.toList
            ])

    let processTypeData (processType: ProcessType) =
        processType.Data
        |> Array.toList
        |> List.collect (fun dataKinds ->
            [
                yield! dataKinds.Files |> Array.toList
                yield! dataKinds.FragmentSelector |> Array.toList
            ])

    let makeProcessWithUnassociated
        (
            name: string,
            executesProtocol: string,
            inputs: ProcessType list,
            outputs: ProcessType list,
            additionalMaterials: Material list,
            additionalData: Data list
        )
        : LabProcess =
        let processMaterials =
            [|
                for processType in inputs do
                    yield! processTypeMaterials processType

                for processType in outputs do
                    yield! processTypeMaterials processType

                yield! additionalMaterials |> List.toArray
            |]

        let processData =
            [|
                for processType in inputs do
                    yield! processTypeData processType

                for processType in outputs do
                    yield! processTypeData processType

                yield! additionalData |> List.toArray
            |]

        {
            id = Some(($"{executesProtocol}:{name}").ToLowerInvariant().Replace(" ", "-"))
            type' = "Process"
            additionalType = None
            name = name
            inputs = inputs |> List.toArray
            outputs = outputs |> List.toArray
            Materials = processMaterials
            Data = processData
            executesProtocol = executesProtocol
            parameterValue = [|
                makeOwnerProperty "process" $"{executesProtocol}:{name}" "Process Parameter" executesProtocol
            |]
        }

    let makeProcess
        (
            name: string,
            executesProtocol: string,
            inputs: ProcessType list,
            outputs: ProcessType list
        )
        : LabProcess =
        makeProcessWithUnassociated(
            name,
            executesProtocol,
            inputs,
            outputs,
            [],
            []
        )

    let makeFormalParameter(id: string, name: string, defaultValue: string option) : FormalParameter = {
        id = id
        type' = "FormalParameter"
        name = Some name
        nameTAN = None
        defaultValue = defaultValue
    }

    let makeDefinedTerm(id: string, name: string, termCode: string option) : DefinedTerm = {
        id = id
        type' = "DefinedTerm"
        name = name
        termCode = termCode
        inDefinedTermSet = None
        additionalProperty = Some(makeOwnerProperty "defined-term" id "DefinedTerm Property" name)
    }

    let makeProtocol
        (
            id: string,
            name: string,
            description: string,
            intendedUse: (DefinedTerm * string) option,
            parameters: FormalParameter list,
            processes: LabProcess list
        )
        : LabProtocol =
        {
            id = Some id
            type' = "Protocol"
            additionalType = None
            name = Some name
            parameters = parameters |> List.toArray
            description = Some description
            intendedUse = intendedUse
            processes = processes |> List.toArray
            additionalProperty = Some(makeOwnerProperty "protocol" id "Protocol Property" name)
            version = Some "1.0.0"
            url = None
        }

    let makeDataset
        (
            kind: ARCDatasets,
            identifier: string,
            name: string,
            description: string,
            aboutProtocols: LabProtocol list,
            hasPart: Dataset list,
            additionalProperties: PropertyValue array
        )
        : Dataset =
        {
            id = $"{(kindLabel kind).ToLowerInvariant()}:{identifier}"
            type' = kind
            additionalType = kindLabel kind
            identifier = identifier
            name = Some name
            description = Some description
            about = aboutProtocols |> List.toArray
            hasPart = hasPart |> List.toArray
            additionalProperty =
                Array.append
                    additionalProperties
                    [| makeOwnerProperty "dataset" identifier "Dataset Property" (kindLabel kind) |]
        }

    let mapDatasetKinds (f: Dataset -> Dataset) (datasetKinds: DatasetKinds) : DatasetKinds = {
        Studies = datasetKinds.Studies |> Array.map f
        Assays = datasetKinds.Assays |> Array.map f
        Workflows = datasetKinds.Workflows |> Array.map f
        Runs = datasetKinds.Runs |> Array.map f
    }

    let rec withSecondarySuffix (dataset: Dataset) : Dataset =
        {
            dataset with
                id = $"{dataset.id}-secondary"
                identifier = $"{dataset.identifier}-secondary"
                name = dataset.name |> Option.map (fun value -> $"{value} (Secondary ARC)")
                hasPart = dataset.hasPart |> Array.map withSecondarySuffix
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
        let sourceLeaf = makeMaterial("leaf-a", "Leaf-A", "Source", None)
        let sampleLeaf = makeMaterial("leaf-a", "Leaf-A", "Sample", None)
        let replicateLeaf = makeMaterial("leaf-a-replicate", "Leaf-A Replicate", "Sample", None)
        let extractMaterial = makeMaterial("extract-a", "Leaf-Extract-A", "Extract", None)
        let measurementSample = makeMaterial("measurement-qc", "Measurement QC", "Sample", Some "QualityControl")

        let cultivationLogData = makeFilesData("cultivation-log", "studies/drought-response/cultivation-log.tsv", "Data")
        let extractionManifestData = makeFilesData("extraction-manifest", "workflows/extraction/extraction-manifest.tsv", "Data")
        let assaySampleSheetData = makeFilesData("sample-sheet", "assays/metabolomics/sample-sheet.tsv", "Data")
        let quantTableData = makeFilesData("feature-table", "assays/metabolomics/feature-table.tsv", "Data")
        let runLogData = makeFilesData("run-log", "runs/week-01/source-log.tsv", "Data")

        let cultivationFragmentData =
            makeFragmentSelectorData(
                "cultivation-window",
                "studies/drought-response/cultivation-log.tsv",
                "Data",
                "day=0-21;light=16h",
                "SWATE:FRAGMENT:CULTIVATION"
            )

        let extractionFragmentData =
            makeFragmentSelectorData(
                "extraction-window",
                "workflows/extraction/extraction-manifest.tsv",
                "Data",
                "fraction=polar;replicate=1-3",
                "SWATE:FRAGMENT:EXTRACTION"
            )

        let measurementFragmentData =
            makeFragmentSelectorData(
                "measurement-window",
                "assays/metabolomics/feature-table.tsv",
                "Data",
                "mz=150-900;rt=60-780",
                "MS:1000515"
            )

        let runFragmentData =
            makeFragmentSelectorData(
                "window",
                "runs/week-01/source-log.tsv",
                "Data",
                "cycle=1-20",
                "SWATE:FRAGMENT:RUN"
            )

        let cultivationProtocolId = "protocol:cultivation"
        let extractionProtocolId = "protocol:extraction"
        let measurementProtocolId = "protocol:lcms-measurement"
        let runProtocolId = "protocol:week-01"

        let endpointBundle
            (sourceMaterial: Material)
            (sampleMaterial: Material)
            (filesData: Data)
            (fragmentSelectorData: Data)
            =
            makeProcessType(
                [ sourceMaterial ],
                [ sampleMaterial ],
                [ filesData ],
                [ fragmentSelectorData ]
            )

        let cultivationInput =
            endpointBundle sourceLeaf sampleLeaf cultivationLogData cultivationFragmentData

        let cultivationOutput =
            endpointBundle sourceLeaf replicateLeaf cultivationLogData cultivationFragmentData

        let extractionInput =
            endpointBundle sourceLeaf extractMaterial extractionManifestData extractionFragmentData

        let extractionOutput =
            endpointBundle sourceLeaf extractMaterial assaySampleSheetData extractionFragmentData

        let measurementInput =
            endpointBundle sourceLeaf measurementSample assaySampleSheetData measurementFragmentData

        let measurementOutput =
            endpointBundle sourceLeaf measurementSample quantTableData measurementFragmentData

        let runInput =
            endpointBundle sourceLeaf extractMaterial runLogData runFragmentData

        let runOutput =
            endpointBundle sourceLeaf extractMaterial quantTableData runFragmentData

        let cultivationUnassociatedMaterial =
            makeMaterial("sample:cultivation-reference", "Cultivation Reference Pool", "Sample", Some "Reference")

        let cultivationUnassociatedData =
            makeFilesData("data:cultivation-observations", "studies/drought-response/observations.tsv", "Data")

        let cultivationProcesses = [
            makeProcessWithUnassociated(
                "Grow plants",
                cultivationProtocolId,
                [ cultivationInput ],
                [ cultivationOutput ],
                [ cultivationUnassociatedMaterial ],
                [ cultivationUnassociatedData ]
            )
            makeProcess("Snapshot source inventory", cultivationProtocolId, [ cultivationInput ], [ cultivationInput ])
        ]

        let extractionProcesses = [
            makeProcess("Extract metabolites", extractionProtocolId, [ extractionInput ], [ extractionOutput ])
        ]

        let measurementProcesses = [
            makeProcess("Quantify features", measurementProtocolId, [ measurementInput ], [ measurementOutput ])
        ]

        let runProcesses = [
            makeProcess("Inject extracts", runProtocolId, [ runInput ], [ runOutput ])
        ]

        let cultivationProtocol =
            makeProtocol(
                cultivationProtocolId,
                "Plant Cultivation",
                "Prepare biological source material before extraction.",
                Some(
                    makeDefinedTerm("dt:cultivation", "Plant Cultivation", Some "SWATE:CULTIVATION"),
                    "Generate healthy source and sample material."
                ),
                [
                    makeFormalParameter("fp:light-cycle", "Light Cycle", Some "16h/8h")
                ],
                cultivationProcesses
            )

        let extractionProtocol =
            makeProtocol(
                extractionProtocolId,
                "Extraction",
                "Transform samples into assay-ready extracts.",
                Some(
                    makeDefinedTerm("dt:extraction", "Extraction", Some "SWATE:EXTRACTION"),
                    "Generate measurable extract material."
                ),
                [
                    makeFormalParameter("fp:solvent", "Extraction Solvent", Some "80% methanol")
                    makeFormalParameter("fp:temperature", "Extraction Temperature", Some "4C")
                ],
                extractionProcesses
            )

        let measurementProtocol =
            makeProtocol(
                measurementProtocolId,
                "LC-MS Measurement",
                "Instrument acquisition for metabolomics feature detection.",
                Some(
                    makeDefinedTerm("dt:measurement", "LC-MS Measurement", Some "MS:1001837"),
                    "Produce quantified metabolite features."
                ),
                [
                    makeFormalParameter("fp:instrument", "Instrument Model", Some "Q Exactive")
                ],
                measurementProcesses
            )

        let runProtocol =
            makeProtocol(
                runProtocolId,
                "Week 1 Injection",
                "Run-specific acquisition execution.",
                Some(
                    makeDefinedTerm("dt:run", "Run", Some "SWATE:RUN"),
                    "Track one acquisition cycle."
                ),
                [
                    makeFormalParameter("fp:batch", "Batch Identifier", Some "batch-001")
                ],
                runProcesses
            )

        let studySubsetDataset =
            makeDataset(
                ARCDatasets.Study,
                "study-drought-response-subset",
                "Drought Response Subset",
                "Nested subset of study objects.",
                [],
                [],
                [||]
            )

        let studyDataset =
            makeDataset(
                ARCDatasets.Study,
                "study-drought-response",
                "Drought Response",
                "Field study tracking response to drought stress.",
                [ cultivationProtocol ],
                [ studySubsetDataset ],
                [| makeProperty("prop:region", "Region", "Field-01") |]
            )

        let assayDataset =
            makeDataset(
                ARCDatasets.Assay,
                "assay-metabolomics",
                "Metabolomics",
                "LC-MS assay for drought response metabolites.",
                [ measurementProtocol ],
                [],
                [||]
            )

        let workflowDataset =
            makeDataset(
                ARCDatasets.Workflow,
                "workflow-extraction",
                "Extraction",
                "Workflow transforming leaf material to assay-ready extracts.",
                [ extractionProtocol ],
                [],
                [||]
            )

        let runDataset =
            makeDataset(
                ARCDatasets.Run,
                "run-week-01",
                "Week 1 Instrument",
                "First acquisition run in the drought series.",
                [ runProtocol ],
                [],
                [||]
            )

        {
            path = "C:/example/arc-graph"
            Datasets = {
                Studies = [| studyDataset |]
                Assays = [| assayDataset |]
                Workflows = [| workflowDataset |]
                Runs = [| runDataset |]
            }
        }

    let fakeGraphModels() : ARCGraph list =
        let primaryArc = fakeGraphModel ()

        let secondaryArc = {
            primaryArc with
                path = "C:/example/arc-graph-secondary"
                Datasets = primaryArc.Datasets |> mapDatasetKinds withSecondarySuffix
        }

        [ primaryArc; secondaryArc ]

    let fakeGraphObjects() : ARCObjects list =
        let arcs = fakeGraphModels () |> List.toArray

        [
            ARCObjects.Arc arcs
        ]

let collapseExplorerItems(items: FileItem list) =
    GraphObjectFixtureHelper.collapseExplorerItems items

let fakeGraphModels() =
    GraphObjectFixtureHelper.fakeGraphModels ()

let fakeGraphObjects() =
    GraphObjectFixtureHelper.fakeGraphObjects ()
