namespace Swate.Components

open Fable.Core
open Feliz
open WidgetsLocalStorage
open Swate.Components.FileExplorerTypes

[<Erase; Mangle(false)>]
type ARCObjectWidget =

    static member private StoryPrefix = "ARC_OBJECT_WIDGET"
    static member private StorySize = { X = 1180; Y = 760 }
    static member private StoryPosition = { X = 24; Y = 24 }

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-3 swt:p-2 swt:w-[72rem] swt:max-w-[95vw] swt:h-[70vh] swt:max-h-[80vh]"

    static member private StoryItemIdRoot = "arc"
    static member private StoryItemIdStudy = "study:plant-stress"
    static member private StoryItemIdStudyAssayRef = "study:plant-stress:assay-ref:metabolomics"
    static member private StoryItemIdStudyAssayRef2 = "study:plant-stress:assay-ref:transcriptomics"
    static member private StoryItemIdStudy2 = "study:soil-microbiome"
    static member private StoryItemIdStudy2AssayRef = "study:soil-microbiome:assay-ref:amplicon"
    static member private StoryItemIdAssay = "assay:metabolomics"
    static member private StoryItemIdAssay2 = "assay:transcriptomics"
    static member private StoryItemIdAssay3 = "assay:amplicon-sequencing"
    static member private StoryItemIdWorkflow = "workflow:extraction"
    static member private StoryItemIdWorkflow2 = "workflow:cleanup"
    static member private StoryItemIdRun = "run:2026-04-01"
    static member private StoryItemIdRun2 = "run:2026-04-08"
    static member private StoryItemIdStudyDataMap = "study:plant-stress:datamap"
    static member private StoryItemIdStudy2DataMap = "study:soil-microbiome:datamap"
    static member private StoryItemIdAssayDataMap = "assay:metabolomics:datamap"
    static member private StoryItemIdAssay2DataMap = "assay:transcriptomics:datamap"
    static member private StoryItemIdAssay3DataMap = "assay:amplicon-sequencing:datamap"
    static member private StoryItemIdWorkflowDataMap = "workflow:extraction:datamap"
    static member private StoryItemIdWorkflow2DataMap = "workflow:cleanup:datamap"
    static member private StoryItemIdRunDataMap = "run:2026-04-01:datamap"
    static member private StoryItemIdRun2DataMap = "run:2026-04-08:datamap"
    static member private StoryItemIdNote = "study:plant-stress:note-ref:sampling-protocol"
    static member private StoryItemIdNote2 = "study:plant-stress:note-ref:leaf-scoring"
    static member private StoryItemIdNote3 = "study:soil-microbiome:note-ref:field-observations"
    static member private StoryItemIdNoteRoot1 = "note:root:project-overview"
    static member private StoryItemIdNoteRoot2 = "note:root:release-checklist"
    static member private StoryItemIdCanonicalStudyNote1 = "note:studies:plant-stress:sampling-protocol"
    static member private StoryItemIdCanonicalStudyNote2 = "note:studies:plant-stress:leaf-scoring"
    static member private StoryItemIdCanonicalStudyNote3 = "note:studies:soil-microbiome:field-observations"
    static member private StoryItemIdSample = "sample:leaf-01"
    static member private StoryItemIdSample2 = "sample:leaf-02"
    static member private StoryItemIdSample3 = "sample:soil-core-a"

    static member private StoryItems() : FileItem list =
        let folder iconPath id name children =
            {
                FileTree.createFolder name None iconPath with
                    Id = id
                    IsExpanded = true
                    Children = Some children
            }

        let group id name children =
            folder "swt:fluent--folder-24-regular" id name children

        let objectNode id name children =
            folder "swt:fluent--document-24-regular" id name children

        let document id name =
            {
                FileTree.createFile name None "swt:fluent--document-24-regular" with
                    Id = id
            }

        let tag id name =
            {
                FileTree.createFile name None "swt:fluent--tag-24-regular" with
                    Id = id
            }

        let note id name =
            {
                FileTree.createFile name None "swt:fluent--document-24-regular" with
                    Id = id
            }

        let datamap id =
            {
                FileTree.createFile "DataMap" None "swt:fluent--database-24-regular" with
                    Id = id
            }

        [
            group
                ARCObjectWidget.StoryItemIdRoot
                "MyArc"
                [
                    group
                        "group:studies"
                        "Studies"
                        [
                            objectNode
                                ARCObjectWidget.StoryItemIdStudy
                                "PlantStressStudy"
                                [
                                    datamap ARCObjectWidget.StoryItemIdStudyDataMap
                                    group
                                        "study:plant-stress:assays"
                                        "Assays"
                                        [
                                            document ARCObjectWidget.StoryItemIdStudyAssayRef "MetabolomicsAssay"
                                            document ARCObjectWidget.StoryItemIdStudyAssayRef2 "TranscriptomicsAssay"
                                        ]
                                    group
                                        "study:plant-stress:notes"
                                        "Notes"
                                        [
                                            note ARCObjectWidget.StoryItemIdNote "Sampling protocol"
                                            note ARCObjectWidget.StoryItemIdNote2 "Leaf scoring rubric"
                                        ]
                                    group
                                        "study:plant-stress:samples"
                                        "Samples"
                                        [
                                            tag ARCObjectWidget.StoryItemIdSample "Leaf-01"
                                            tag ARCObjectWidget.StoryItemIdSample2 "Leaf-02"
                                        ]
                                ]
                            objectNode
                                ARCObjectWidget.StoryItemIdStudy2
                                "SoilMicrobiomeStudy"
                                [
                                    datamap ARCObjectWidget.StoryItemIdStudy2DataMap
                                    group
                                        "study:soil-microbiome:assays"
                                        "Assays"
                                        [ document ARCObjectWidget.StoryItemIdStudy2AssayRef "AmpliconSequencingAssay" ]
                                    group
                                        "study:soil-microbiome:notes"
                                        "Notes"
                                        [ note ARCObjectWidget.StoryItemIdNote3 "Field observations" ]
                                    group
                                        "study:soil-microbiome:samples"
                                        "Samples"
                                        [ tag ARCObjectWidget.StoryItemIdSample3 "SoilCore-A" ]
                                ]
                        ]
                    group
                        "group:assays"
                        "Assays"
                        [
                            objectNode ARCObjectWidget.StoryItemIdAssay "MetabolomicsAssay" [ datamap ARCObjectWidget.StoryItemIdAssayDataMap ]
                            objectNode ARCObjectWidget.StoryItemIdAssay2 "TranscriptomicsAssay" [ datamap ARCObjectWidget.StoryItemIdAssay2DataMap ]
                            objectNode ARCObjectWidget.StoryItemIdAssay3 "AmpliconSequencingAssay" [ datamap ARCObjectWidget.StoryItemIdAssay3DataMap ]
                        ]
                    group
                        "group:workflows"
                        "Workflows"
                        [
                            objectNode ARCObjectWidget.StoryItemIdWorkflow "ExtractionWorkflow" [ datamap ARCObjectWidget.StoryItemIdWorkflowDataMap ]
                            objectNode ARCObjectWidget.StoryItemIdWorkflow2 "CleanupWorkflow" [ datamap ARCObjectWidget.StoryItemIdWorkflow2DataMap ]
                        ]
                    group
                        "group:runs"
                        "Runs"
                        [
                            objectNode ARCObjectWidget.StoryItemIdRun "Run-2026-04-01" [ datamap ARCObjectWidget.StoryItemIdRunDataMap ]
                            objectNode ARCObjectWidget.StoryItemIdRun2 "Run-2026-04-08" [ datamap ARCObjectWidget.StoryItemIdRun2DataMap ]
                        ]
                    group
                        "group:notes"
                        "Notes"
                        [
                            note ARCObjectWidget.StoryItemIdNoteRoot1 "Project overview"
                            note ARCObjectWidget.StoryItemIdNoteRoot2 "Release checklist"
                            note ARCObjectWidget.StoryItemIdCanonicalStudyNote1 "Sampling protocol"
                            note ARCObjectWidget.StoryItemIdCanonicalStudyNote2 "Leaf scoring rubric"
                            note ARCObjectWidget.StoryItemIdCanonicalStudyNote3 "Field observations"
                        ]
                ]
        ]

    static member private StoryMeta =
        [
            ARCObjectWidget.StoryItemIdRoot,
            ("MyArc", "ARC", "Canonical root", "isa.investigation.xlsx", "The ARC root groups all registered objects.")
            ARCObjectWidget.StoryItemIdStudy,
            ("PlantStressStudy", "Study", "Canonical object", "studies/PlantStressStudy/isa.study.xlsx", "Registered study with linked assay refs, notes, and derived samples.")
            ARCObjectWidget.StoryItemIdStudyAssayRef,
            ("MetabolomicsAssay", "Assay Reference", "Relationship node", "assays/MetabolomicsAssay/isa.assay.xlsx", "Reference node from the study to the canonical assay entry.")
            ARCObjectWidget.StoryItemIdStudyAssayRef2,
            ("TranscriptomicsAssay", "Assay Reference", "Relationship node", "assays/TranscriptomicsAssay/isa.assay.xlsx", "A second assay reference on the same study to show multi-assay study links.")
            ARCObjectWidget.StoryItemIdStudy2,
            ("SoilMicrobiomeStudy", "Study", "Canonical object", "studies/SoilMicrobiomeStudy/isa.study.xlsx", "A second study broadens the ARC tree and shows that the widget handles multiple study branches.")
            ARCObjectWidget.StoryItemIdStudy2AssayRef,
            ("AmpliconSequencingAssay", "Assay Reference", "Relationship node", "assays/AmpliconSequencingAssay/isa.assay.xlsx", "Study-to-assay reference for the soil microbiome branch.")
            ARCObjectWidget.StoryItemIdAssay,
            ("MetabolomicsAssay", "Assay", "Canonical object", "assays/MetabolomicsAssay/isa.assay.xlsx", "Assay metadata and tables live on the canonical assay node.")
            ARCObjectWidget.StoryItemIdAssay2,
            ("TranscriptomicsAssay", "Assay", "Canonical object", "assays/TranscriptomicsAssay/isa.assay.xlsx", "A second canonical assay highlights that the explorer is not limited to a single assay.")
            ARCObjectWidget.StoryItemIdAssay3,
            ("AmpliconSequencingAssay", "Assay", "Canonical object", "assays/AmpliconSequencingAssay/isa.assay.xlsx", "A third assay extends the storybook ARC with a distinct study branch.")
            ARCObjectWidget.StoryItemIdWorkflow,
            ("ExtractionWorkflow", "Workflow", "Canonical object", "workflows/ExtractionWorkflow/isa.workflow.xlsx", "Workflow nodes can expose subworkflow references and workflow datamaps.")
            ARCObjectWidget.StoryItemIdWorkflow2,
            ("CleanupWorkflow", "Workflow", "Canonical object", "workflows/CleanupWorkflow/isa.workflow.xlsx", "A second workflow makes the workflow area visibly multi-item.")
            ARCObjectWidget.StoryItemIdRun,
            ("Run-2026-04-01", "Run", "Canonical object", "runs/Run-2026-04-01/isa.run.xlsx", "Run nodes can reference workflows and derived sample collections.")
            ARCObjectWidget.StoryItemIdRun2,
            ("Run-2026-04-08", "Run", "Canonical object", "runs/Run-2026-04-08/isa.run.xlsx", "A second run shows repeated operational objects in the story.")
            ARCObjectWidget.StoryItemIdStudyDataMap,
            ("DataMap", "DataMap", "Canonical child", "studies/PlantStressStudy/isa.datamap.xlsx", "Selecting the study datamap keeps tree focus on the DataMap object and opens the owning study in DataMap view.")
            ARCObjectWidget.StoryItemIdStudy2DataMap,
            ("DataMap", "DataMap", "Canonical child", "studies/SoilMicrobiomeStudy/isa.datamap.xlsx", "A second study datamap shows that multiple study branches can expose DataMap children.")
            ARCObjectWidget.StoryItemIdAssayDataMap,
            ("DataMap", "DataMap", "Canonical child", "assays/MetabolomicsAssay/isa.datamap.xlsx", "The metabolomics assay exposes a DataMap child in the ARC object tree.")
            ARCObjectWidget.StoryItemIdAssay2DataMap,
            ("DataMap", "DataMap", "Canonical child", "assays/TranscriptomicsAssay/isa.datamap.xlsx", "A second assay datamap keeps the assay branch visibly multi-object.")
            ARCObjectWidget.StoryItemIdAssay3DataMap,
            ("DataMap", "DataMap", "Canonical child", "assays/AmpliconSequencingAssay/isa.datamap.xlsx", "The amplicon sequencing assay also exposes its DataMap in the object tree.")
            ARCObjectWidget.StoryItemIdWorkflowDataMap,
            ("DataMap", "DataMap", "Canonical child", "workflows/ExtractionWorkflow/isa.datamap.xlsx", "Workflow datamaps appear as direct children of workflow objects.")
            ARCObjectWidget.StoryItemIdWorkflow2DataMap,
            ("DataMap", "DataMap", "Canonical child", "workflows/CleanupWorkflow/isa.datamap.xlsx", "A second workflow datamap shows that datamaps are not limited to studies and assays.")
            ARCObjectWidget.StoryItemIdRunDataMap,
            ("DataMap", "DataMap", "Canonical child", "runs/Run-2026-04-01/isa.datamap.xlsx", "Run datamaps appear directly under run objects.")
            ARCObjectWidget.StoryItemIdRun2DataMap,
            ("DataMap", "DataMap", "Canonical child", "runs/Run-2026-04-08/isa.datamap.xlsx", "A second run datamap rounds out the showcase across all supported ARC object kinds.")
            ARCObjectWidget.StoryItemIdNote,
            ("Sampling protocol", "Note", "Relationship node", "Notes/studies/PlantStressStudy/17_03_2026/sampling_protocol.md", "Reference node from the study to the canonical note entry in the top-level Notes section.")
            ARCObjectWidget.StoryItemIdNote2,
            ("Leaf scoring rubric", "Note", "Relationship node", "Notes/studies/PlantStressStudy/18_03_2026/leaf_scoring_rubric.md", "A second study note reference showing that a study can link multiple notes.")
            ARCObjectWidget.StoryItemIdNote3,
            ("Field observations", "Note", "Relationship node", "Notes/studies/SoilMicrobiomeStudy/18_03_2026/field_observations.md", "Reference node from the second study to its canonical note entry.")
            ARCObjectWidget.StoryItemIdNoteRoot1,
            ("Project overview", "Note", "Filesystem-backed", "Notes/19_03_2026/project_overview.md", "A root-level ARC note rendered directly under ARC -> Notes.")
            ARCObjectWidget.StoryItemIdNoteRoot2,
            ("Release checklist", "Note", "Filesystem-backed", "Notes/20_03_2026/release_checklist.md", "A second root-level note broadens the top-level Notes section in the showcase.")
            ARCObjectWidget.StoryItemIdCanonicalStudyNote1,
            ("Sampling protocol", "Note", "Filesystem-backed", "Notes/studies/PlantStressStudy/17_03_2026/sampling_protocol.md", "A study-targeted note rendered directly under ARC -> Notes.")
            ARCObjectWidget.StoryItemIdCanonicalStudyNote2,
            ("Leaf scoring rubric", "Note", "Filesystem-backed", "Notes/studies/PlantStressStudy/18_03_2026/leaf_scoring_rubric.md", "A second study-targeted note rendered directly under ARC -> Notes.")
            ARCObjectWidget.StoryItemIdCanonicalStudyNote3,
            ("Field observations", "Note", "Filesystem-backed", "Notes/studies/SoilMicrobiomeStudy/18_03_2026/field_observations.md", "A study-targeted note for the soil microbiome branch, shown directly under ARC -> Notes.")
            ARCObjectWidget.StoryItemIdSample,
            ("Leaf-01", "Sample", "Virtual node", "-", "Sample nodes are derived from table content, not standalone files.")
            ARCObjectWidget.StoryItemIdSample2,
            ("Leaf-02", "Sample", "Virtual node", "-", "A second sample keeps the plant stress study visibly multi-sample.")
            ARCObjectWidget.StoryItemIdSample3,
            ("SoilCore-A", "Sample", "Virtual node", "-", "A sample belonging to the second study branch.")
        ]
        |> Map.ofList

    static member private StoryMetadataRows =
        [
            ARCObjectWidget.StoryItemIdStudy,
            [
                "Identifier", "PS-2026-001"
                "Title", "Plant Stress Response Under Progressive Drought"
                "Description", "A controlled greenhouse study tracking transcript, metabolite, and phenotype changes during water limitation."
                "Tables", "2"
                "Submission Date", "2026-03-12"
                "Public Release", "2026-06-01"
                "Design", "Drought stress; time series"
                "Contacts", "Nadia Green; Oliver Hartmann"
                "Publications", "1"
                "Comments", "Includes shared sample identifiers across linked assays."
            ]
            ARCObjectWidget.StoryItemIdStudy2,
            [
                "Identifier", "SM-2026-004"
                "Title", "Soil Microbiome Recovery After Compost Amendment"
                "Description", "A field study following microbial community changes after regenerative soil treatment."
                "Tables", "1"
                "Submission Date", "2026-03-18"
                "Public Release", "2026-07-15"
                "Design", "Field trial; amplicon sequencing"
                "Contacts", "Lina Becker"
                "Publications", "0"
                "Comments", "Samples are grouped by treatment plot and collection week."
            ]
            ARCObjectWidget.StoryItemIdAssay,
            [
                "Identifier", "MetabolomicsAssay"
                "Title", "Leaf Metabolite Profiling"
                "Description", "Mass spectrometry assay quantifying osmoprotectants and stress-associated metabolites."
                "Tables", "3"
                "Measurement", "metabolite profiling"
                "Technology", "mass spectrometry"
                "Platform", "Orbitrap Exploris 240"
                "Performers", "Elena Rossi; Max Weber"
                "Comments", "Aligned with the plant stress study time points."
            ]
            ARCObjectWidget.StoryItemIdStudyAssayRef,
            [
                "Identifier", "MetabolomicsAssay"
                "Title", "Leaf Metabolite Profiling"
                "Description", "Mass spectrometry assay quantifying osmoprotectants and stress-associated metabolites."
                "Tables", "3"
                "Measurement", "metabolite profiling"
                "Technology", "mass spectrometry"
                "Platform", "Orbitrap Exploris 240"
                "Performers", "Elena Rossi; Max Weber"
                "Comments", "Aligned with the plant stress study time points."
            ]
            ARCObjectWidget.StoryItemIdAssay2,
            [
                "Identifier", "TranscriptomicsAssay"
                "Title", "Leaf Transcriptome Sequencing"
                "Description", "RNA-seq assay tracking drought-induced expression programs."
                "Tables", "2"
                "Measurement", "transcript profiling"
                "Technology", "RNA sequencing"
                "Platform", "Illumina NovaSeq 6000"
                "Performers", "Priya Shah"
                "Comments", "Shares the same plant cohort as the metabolomics branch."
            ]
            ARCObjectWidget.StoryItemIdStudyAssayRef2,
            [
                "Identifier", "TranscriptomicsAssay"
                "Title", "Leaf Transcriptome Sequencing"
                "Description", "RNA-seq assay tracking drought-induced expression programs."
                "Tables", "2"
                "Measurement", "transcript profiling"
                "Technology", "RNA sequencing"
                "Platform", "Illumina NovaSeq 6000"
                "Performers", "Priya Shah"
                "Comments", "Shares the same plant cohort as the metabolomics branch."
            ]
            ARCObjectWidget.StoryItemIdAssay3,
            [
                "Identifier", "AmpliconSequencingAssay"
                "Title", "16S rRNA Community Survey"
                "Description", "Amplicon sequencing assay capturing bacterial community shifts after soil amendment."
                "Tables", "1"
                "Measurement", "microbial community profiling"
                "Technology", "amplicon sequencing"
                "Platform", "Illumina MiSeq"
                "Performers", "Jonas Klein"
                "Comments", "Primary assay for the soil microbiome branch."
            ]
            ARCObjectWidget.StoryItemIdStudy2AssayRef,
            [
                "Identifier", "AmpliconSequencingAssay"
                "Title", "16S rRNA Community Survey"
                "Description", "Amplicon sequencing assay capturing bacterial community shifts after soil amendment."
                "Tables", "1"
                "Measurement", "microbial community profiling"
                "Technology", "amplicon sequencing"
                "Platform", "Illumina MiSeq"
                "Performers", "Jonas Klein"
                "Comments", "Primary assay for the soil microbiome branch."
            ]
            ARCObjectWidget.StoryItemIdWorkflow,
            [
                "Identifier", "ExtractionWorkflow"
                "Title", "Polar Metabolite Extraction"
                "Description", "Bench workflow covering quenching, grinding, extraction, and QC pooling."
                "Version", "2.1"
                "Type", "wet-lab protocol"
                "URI", "https://example.org/workflows/extraction-v2-1"
                "Subworkflows", "cleanup"
                "Contacts", "Marta Ivanova"
                "Comments", "Feeds directly into the metabolomics assay branch."
            ]
            ARCObjectWidget.StoryItemIdWorkflow2,
            [
                "Identifier", "CleanupWorkflow"
                "Title", "Post-Extraction Cleanup"
                "Description", "Sample cleanup and concentration workflow executed before LC-MS injection."
                "Version", "1.4"
                "Type", "sample preparation"
                "URI", "https://example.org/workflows/cleanup-v1-4"
                "Subworkflows", "None"
                "Contacts", "Marta Ivanova"
                "Comments", "Reusable downstream workflow for multiple metabolomics studies."
            ]
            ARCObjectWidget.StoryItemIdRun,
            [
                "Identifier", "Run-2026-04-01"
                "Title", "Week 1 Drought Run"
                "Description", "Operational run capturing the first drought sampling week."
                "Tables", "2"
                "Measurement", "metabolite profiling"
                "Technology", "mass spectrometry"
                "Platform", "Orbitrap Exploris 240"
                "Workflows", "ExtractionWorkflow"
                "Performers", "Elena Rossi"
                "Comments", "Includes pooled QC injections at the beginning and end of the sequence."
            ]
            ARCObjectWidget.StoryItemIdRun2,
            [
                "Identifier", "Run-2026-04-08"
                "Title", "Week 2 Drought Run"
                "Description", "Follow-up run extending the same acquisition series into the second drought week."
                "Tables", "2"
                "Measurement", "metabolite profiling"
                "Technology", "mass spectrometry"
                "Platform", "Orbitrap Exploris 240"
                "Workflows", "ExtractionWorkflow; CleanupWorkflow"
                "Performers", "Elena Rossi; Max Weber"
                "Comments", "Carries forward retention-time alignment controls from the first run."
            ]
        ]
        |> Map.ofList

    static member private TryFindItemAndParent(itemId: string, items: FileItem list) =
        let rec loop (parent: FileItem option) (items: FileItem list) =
            items
            |> List.tryPick (fun item ->
                if item.Id = itemId then
                    Some(item, parent)
                else
                    item.Children |> Option.bind (loop (Some item)))

        loop None items

    static member private GetExplorerItems(selectedId: string option, items: FileItem list) =
        selectedId
        |> Option.bind (fun itemId -> ARCObjectWidget.TryFindItemAndParent(itemId, items))
        |> Option.map (fun (selectedItem, _parentItem) ->
            let children = selectedItem.Children |> Option.defaultValue []

            if List.isEmpty children then
                ("Current", selectedItem.Name, selectedItem.Id, [ selectedItem ])
            else
                ("Children", selectedItem.Name, selectedItem.Id, children))

    [<ReactComponent>]
    static member ExplorerContent(items: FileItem list, ?selectedItemId: string, ?onItemClick: FileItem -> unit) =
        let onItemClick = defaultArg onItemClick ignore
        let explorerItems = ARCObjectWidget.GetExplorerItems(selectedItemId, items)

        let iconTile (subtitle: string) (item: FileItem) isCurrentTarget =
            Html.button [
                prop.type'.button
                prop.className [
                    "swt:flex swt:flex-col swt:items-center swt:justify-center swt:gap-3 swt:rounded-xl swt:border swt:border-base-300 swt:bg-base-100 swt:p-4 swt:min-h-28 swt:text-center swt:transition-colors hover:swt:border-primary/60 hover:swt:bg-base-200/60"
                    if isCurrentTarget then "swt:border-primary swt:bg-primary/10"
                ]
                prop.onClick (fun _ -> onItemClick item)
                prop.children [
                    Html.i [
                        prop.className [ "swt:iconify swt:text-4xl swt:text-primary"; item.IconPath ]
                    ]
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.span [ prop.className "swt:text-sm swt:font-medium"; prop.text item.Name ]
                            Html.span [ prop.className "swt:text-xs swt:opacity-60"; prop.text subtitle ]
                        ]
                    ]
                ]
            ]

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-4 swt:h-full swt:overflow-auto"
            prop.children [
                match explorerItems with
                | Some(relationLabel, sourceName, sourceId, visibleItems) ->
                    let tileSubtitle =
                        match relationLabel with
                        | "Children" -> $"Child of {sourceName}"
                        | "Current" -> "Selected object"
                        | _ -> sourceName

                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.span [
                                prop.className "swt:text-xs swt:uppercase swt:tracking-wide swt:opacity-60"
                                prop.text relationLabel
                            ]
                            Html.h4 [ prop.className "swt:text-lg swt:font-semibold"; prop.text sourceName ]
                            Html.p [
                                prop.className "swt:text-sm swt:opacity-70"
                                prop.text (
                                    match relationLabel with
                                    | "Children" -> "Direct children of the selected ARC object."
                                    | "Current" -> "The selected ARC object has no children, so it is shown directly."
                                    | _ -> "Current selection."
                                )
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:grid swt:grid-cols-2 swt:xl:grid-cols-3 swt:gap-3"
                        prop.children [
                            for item in visibleItems do
                                iconTile tileSubtitle item (item.Id = sourceId)
                        ]
                    ]
                | None ->
                    Html.div [
                        prop.className
                            "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:rounded-lg swt:border swt:border-dashed swt:border-base-300 swt:bg-base-200/40 swt:p-6"
                        prop.children [
                            Html.p [
                                prop.className "swt:text-sm swt:text-center swt:opacity-70"
                                prop.text "Select an ARC object in the tree to explore its nearby objects."
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private StoryExample() =
        let selectedId, setSelectedId = React.useState ARCObjectWidget.StoryItemIdStudy
        let items = ARCObjectWidget.StoryItems()
        let selectedMeta =
            ARCObjectWidget.StoryMeta
            |> Map.tryFind selectedId
            |> Option.defaultValue ("Unknown", "Unknown", "Unknown", "-", "No details available.")

        let selectedMetadataRows =
            ARCObjectWidget.StoryMetadataRows
            |> Map.tryFind selectedId

        let selectedTitle, kind, role, previewTarget, description = selectedMeta

        let treePane =
            Swate.Components.FileExplorer.FileExplorer(
                initialItems = items,
                selectedItemId = selectedId,
                onItemClick = (fun item -> setSelectedId item.Id)
            )

        let explorerPane =
            ARCObjectWidget.ExplorerContent(items, selectedItemId = selectedId, onItemClick = fun item -> setSelectedId item.Id)

        let detailsPane =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                prop.children [
                    Html.div [
                        prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-200/40 swt:p-3"
                        prop.children [
                            Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Properties" ]
                            Html.dl [
                                prop.className "swt:grid swt:grid-cols-[auto_1fr] swt:gap-x-3 swt:gap-y-2 swt:text-sm"
                                prop.children [
                                    Html.dt [ prop.className "swt:font-medium"; prop.text "Kind" ]
                                    Html.dd kind
                                    Html.dt [ prop.className "swt:font-medium"; prop.text "Role" ]
                                    Html.dd role
                                    Html.dt [ prop.className "swt:font-medium"; prop.text "Preview" ]
                                    Html.dd previewTarget
                                ]
                            ]
                        ]
                    ]
                    match selectedMetadataRows with
                    | Some rows ->
                        Html.div [
                            prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                            prop.children [
                                Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Metadata" ]
                                Html.dl [
                                    prop.className "swt:grid swt:grid-cols-[auto_1fr] swt:gap-x-3 swt:gap-y-2 swt:text-sm"
                                    prop.children [
                                        for label, value in rows do
                                            Html.dt [ prop.className "swt:font-medium"; prop.text label ]
                                            Html.dd [
                                                prop.className "swt:break-words"
                                                prop.text value
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    | None -> Html.none
                    Html.div [
                        prop.className "swt:flex-1 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                        prop.children [
                            Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Notes" ]
                            Html.p [ prop.className "swt:text-sm swt:opacity-80"; prop.text description ]
                        ]
                    ]
                ]
            ]

        ARCObjectWidget.Main(treePane = treePane, explorerPane = explorerPane, detailsPane = detailsPane)

    [<ReactComponent>]
    static member Main(?treePane: ReactElement, ?explorerPane: ReactElement, ?detailsPane: ReactElement) =
        Html.div [
            prop.className ARCObjectWidget.WidgetContainerClass
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-base swt:font-bold"
                            prop.text "ARC Object Widget"
                        ]
                        Html.p [
                            prop.className "swt:text-sm swt:opacity-70"
                            prop.text "Select an object in the tree to inspect its children, or the selected object itself when it is a leaf."
                        ]
                    ]
                ]
                Html.div [
                    prop.className
                        "swt:grid swt:grid-cols-1 swt:lg:grid-cols-[minmax(16rem,20rem)_minmax(0,1fr)_minmax(14rem,18rem)] swt:gap-3 swt:flex-1 swt:min-h-0"
                    prop.children [
                        ARCObjectTree.Main(?content = treePane)
                        ARCObjectExplorer.Main(?content = explorerPane)
                        ARCObjectDetails.Main(?content = detailsPane)
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private EntryControls() =
        let context = WidgetContext.useWidgetController ()
        let isActive = context.isActive WidgetType.ARCObject

        Html.div [
            prop.className "swt:flex swt:items-center swt:justify-center"
            prop.children [
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if isActive then "swt:btn-primary" else "swt:btn-outline"
                    ]
                    prop.text (if isActive then "Close ARC Object" else "Open ARC Object")
                    prop.onClick (fun _ -> context.toggleWidget WidgetType.ARCObject)
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        React.useEffectOnce (fun _ ->
            Size.write (ARCObjectWidget.StoryPrefix, ARCObjectWidget.StorySize)
            Position.write (ARCObjectWidget.StoryPrefix, ARCObjectWidget.StoryPosition)
        )

        let widgets: Map<WidgetType, WidgetDefinition> =
            [
                WidgetType.ARCObject,
                {|
                    prefix = ARCObjectWidget.StoryPrefix
                    content = ARCObjectWidget.StoryExample()
                |}
            ]
            |> Map.ofList

        Widget.WidgetController(widgets, children = [ ARCObjectWidget.EntryControls() ])
