namespace Swate.Components.Page.ARCObjectExplorer


open Swate.Components.Shared
open Swate.Components.Page.FileExplorer.Types

type ARCObjectFixture =

    static member StoryItemIdRoot = "arc"
    static member StoryItemIdStudy = "study:plant-stress"
    static member StoryItemIdStudyAssayRef = "study:plant-stress:assay-ref:metabolomics"
    static member StoryItemIdStudyAssayRef2 = "study:plant-stress:assay-ref:transcriptomics"
    static member StoryItemIdStudy2 = "study:soil-microbiome"
    static member StoryItemIdStudy2AssayRef = "study:soil-microbiome:assay-ref:amplicon"
    static member StoryItemIdAssay = "assay:metabolomics"
    static member StoryItemIdAssay2 = "assay:transcriptomics"
    static member StoryItemIdAssay3 = "assay:amplicon-sequencing"
    static member StoryItemIdWorkflow = "workflow:extraction"
    static member StoryItemIdWorkflow2 = "workflow:cleanup"
    static member StoryItemIdRun = "run:2026-04-01"
    static member StoryItemIdRun2 = "run:2026-04-08"
    static member StoryItemIdStudyDataMap = "study:plant-stress:datamap"
    static member StoryItemIdStudy2DataMap = "study:soil-microbiome:datamap"
    static member StoryItemIdAssayDataMap = "assay:metabolomics:datamap"
    static member StoryItemIdAssay2DataMap = "assay:transcriptomics:datamap"
    static member StoryItemIdAssay3DataMap = "assay:amplicon-sequencing:datamap"
    static member StoryItemIdWorkflowDataMap = "workflow:extraction:datamap"
    static member StoryItemIdWorkflow2DataMap = "workflow:cleanup:datamap"
    static member StoryItemIdRunDataMap = "run:2026-04-01:datamap"
    static member StoryItemIdRun2DataMap = "run:2026-04-08:datamap"
    static member StoryItemIdStudyTable1 = "study:plant-stress:table:design-matrix"
    static member StoryItemIdStudyTable2 = "study:plant-stress:table:phenotype-scoring"
    static member StoryItemIdStudy2Table1 = "study:soil-microbiome:table:plot-sampling"
    static member StoryItemIdAssayTable1 = "assay:metabolomics:table:metabolite-measurements"
    static member StoryItemIdAssayTable2 = "assay:metabolomics:table:peak-annotation"
    static member StoryItemIdAssayTable3 = "assay:metabolomics:table:qc-injection-summary"
    static member StoryItemIdAssay2Table1 = "assay:transcriptomics:table:rna-samples"
    static member StoryItemIdAssay2Table2 = "assay:transcriptomics:table:differential-expression"
    static member StoryItemIdAssay3Table1 = "assay:amplicon-sequencing:table:asv-abundance"
    static member StoryItemIdRunTable1 = "run:2026-04-01:table:injection-sequence"
    static member StoryItemIdRunTable2 = "run:2026-04-01:table:instrument-qc"
    static member StoryItemIdRun2Table1 = "run:2026-04-08:table:injection-sequence"
    static member StoryItemIdRun2Table2 = "run:2026-04-08:table:retention-alignment"
    static member StoryItemIdAssaySample1 = "assay:metabolomics:sample-ref:leaf-01"
    static member StoryItemIdAssaySample2 = "assay:metabolomics:sample-ref:leaf-02"
    static member StoryItemIdAssay2Sample1 = "assay:transcriptomics:sample-ref:leaf-01"
    static member StoryItemIdAssay2Sample2 = "assay:transcriptomics:sample-ref:leaf-02"
    static member StoryItemIdAssay3Sample1 = "assay:amplicon-sequencing:sample-ref:soil-core-a"
    static member StoryItemIdNote = "study:plant-stress:note-ref:sampling-protocol"
    static member StoryItemIdNote2 = "study:plant-stress:note-ref:leaf-scoring"
    static member StoryItemIdNote3 = "study:soil-microbiome:note-ref:field-observations"
    static member StoryItemIdNoteRoot1 = "note:root:project-overview"
    static member StoryItemIdNoteRoot2 = "note:root:release-checklist"
    static member StoryItemIdCanonicalStudyNote1 = "note:studies:plant-stress:sampling-protocol"
    static member StoryItemIdCanonicalStudyNote2 = "note:studies:plant-stress:leaf-scoring"

    static member StoryItemIdCanonicalStudyNote3 =
        "note:studies:soil-microbiome:field-observations"

    static member StoryItemIdSample = "sample:leaf-01"
    static member StoryItemIdSample2 = "sample:leaf-02"
    static member StoryItemIdSample3 = "sample:soil-core-a"

    static member StoryItems() : FileItem list =
        let folder kind id name isExpanded children selectable =
            let appearance = ARCExplorer.appearanceForNodeKind kind

            {
                FileTree.createFolder name None appearance.Icon with
                    Id = id
                    ItemType = ArcExplorerNodeKind.label kind
                    IconTone = appearance.IconTone
                    IsExpanded = isExpanded
                    Children = Some children
                    Selectable = selectable
            }

        let group id name children =
            folder ArcExplorerNodeKind.Group id name false children false

        let objectNode kind id name children = folder kind id name false children true

        let document kind id name =
            let appearance = ARCExplorer.appearanceForNodeKind kind

            {
                FileTree.createFile name None appearance.Icon with
                    Id = id
                    ItemType = ArcExplorerNodeKind.label kind
                    IconTone = appearance.IconTone
                    Selectable = true
            }

        let tag id name =
            let appearance = ARCExplorer.appearanceForNodeKind ArcExplorerNodeKind.Sample

            {
                FileTree.createFile name None appearance.Icon with
                    Id = id
                    ItemType = ArcExplorerNodeKind.label ArcExplorerNodeKind.Sample
                    IconTone = appearance.IconTone
                    Selectable = true
            }

        let note id name =
            let appearance = ARCExplorer.appearanceForNodeKind ArcExplorerNodeKind.Note

            {
                FileTree.createFile name None appearance.Icon with
                    Id = id
                    ItemType = ArcExplorerNodeKind.label ArcExplorerNodeKind.Note
                    IconTone = appearance.IconTone
                    Selectable = true
            }

        let datamap id =
            let appearance = ARCExplorer.appearanceForNodeKind ArcExplorerNodeKind.DataMap

            {
                FileTree.createFile "DataMap" None appearance.Icon with
                    Id = id
                    ItemType = ArcExplorerNodeKind.label ArcExplorerNodeKind.DataMap
                    IconTone = appearance.IconTone
                    Selectable = true
            }

        let table id name =
            let appearance = ARCExplorer.appearanceForNodeKind ArcExplorerNodeKind.Table

            {
                FileTree.createFile name None appearance.Icon with
                    Id = id
                    ItemType = ArcExplorerNodeKind.label ArcExplorerNodeKind.Table
                    IconTone = appearance.IconTone
                    Selectable = true
            }

        let tableWithSamples id name children =
            folder ArcExplorerNodeKind.Table id name false children true

        [
            {
                folder
                    ArcExplorerNodeKind.Arc
                    ARCObjectFixture.StoryItemIdRoot
                    "MyArc"
                    false
                    [
                        group "group:studies" "Studies" [
                            objectNode ArcExplorerNodeKind.Study ARCObjectFixture.StoryItemIdStudy "PlantStressStudy" [
                                datamap ARCObjectFixture.StoryItemIdStudyDataMap
                                group "study:plant-stress:tables" "Tables" [
                                    tableWithSamples ARCObjectFixture.StoryItemIdStudyTable1 "Study Design Matrix" [
                                        tag ARCObjectFixture.StoryItemIdSample "Leaf-01"
                                        tag ARCObjectFixture.StoryItemIdSample2 "Leaf-02"
                                    ]
                                    table ARCObjectFixture.StoryItemIdStudyTable2 "Phenotype Scoring Table"
                                ]
                                group "study:plant-stress:assays" "Assays" [
                                    document
                                        ArcExplorerNodeKind.Assay
                                        ARCObjectFixture.StoryItemIdStudyAssayRef
                                        "MetabolomicsAssay"
                                    document
                                        ArcExplorerNodeKind.Assay
                                        ARCObjectFixture.StoryItemIdStudyAssayRef2
                                        "TranscriptomicsAssay"
                                ]
                                group "study:plant-stress:notes" "Notes" [
                                    note ARCObjectFixture.StoryItemIdNote "Sampling protocol"
                                    note ARCObjectFixture.StoryItemIdNote2 "Leaf scoring rubric"
                                ]
                            ]
                            objectNode
                                ArcExplorerNodeKind.Study
                                ARCObjectFixture.StoryItemIdStudy2
                                "SoilMicrobiomeStudy"
                                [
                                    datamap ARCObjectFixture.StoryItemIdStudy2DataMap
                                    group "study:soil-microbiome:tables" "Tables" [
                                        tableWithSamples
                                            ARCObjectFixture.StoryItemIdStudy2Table1
                                            "Plot Sampling Schedule"
                                            [ tag ARCObjectFixture.StoryItemIdSample3 "SoilCore-A" ]
                                    ]
                                    group "study:soil-microbiome:assays" "Assays" [
                                        document
                                            ArcExplorerNodeKind.Assay
                                            ARCObjectFixture.StoryItemIdStudy2AssayRef
                                            "AmpliconSequencingAssay"
                                    ]
                                    group "study:soil-microbiome:notes" "Notes" [
                                        note ARCObjectFixture.StoryItemIdNote3 "Field observations"
                                    ]
                                ]
                        ]
                        group "group:assays" "Assays" [
                            objectNode ArcExplorerNodeKind.Assay ARCObjectFixture.StoryItemIdAssay "MetabolomicsAssay" [
                                datamap ARCObjectFixture.StoryItemIdAssayDataMap
                                group "assay:metabolomics:tables" "Tables" [
                                    tableWithSamples ARCObjectFixture.StoryItemIdAssayTable1 "Metabolite Measurements" [
                                        tag ARCObjectFixture.StoryItemIdAssaySample1 "Leaf-01"
                                        tag ARCObjectFixture.StoryItemIdAssaySample2 "Leaf-02"
                                    ]
                                    table ARCObjectFixture.StoryItemIdAssayTable2 "Peak Annotation"
                                    table ARCObjectFixture.StoryItemIdAssayTable3 "QC Injection Summary"
                                ]
                            ]
                            objectNode
                                ArcExplorerNodeKind.Assay
                                ARCObjectFixture.StoryItemIdAssay2
                                "TranscriptomicsAssay"
                                [
                                    datamap ARCObjectFixture.StoryItemIdAssay2DataMap
                                    group "assay:transcriptomics:tables" "Tables" [
                                        tableWithSamples ARCObjectFixture.StoryItemIdAssay2Table1 "RNA Sample Sheet" [
                                            tag ARCObjectFixture.StoryItemIdAssay2Sample1 "Leaf-01"
                                            tag ARCObjectFixture.StoryItemIdAssay2Sample2 "Leaf-02"
                                        ]
                                        table ARCObjectFixture.StoryItemIdAssay2Table2 "Differential Expression Matrix"
                                    ]
                                ]
                            objectNode
                                ArcExplorerNodeKind.Assay
                                ARCObjectFixture.StoryItemIdAssay3
                                "AmpliconSequencingAssay"
                                [
                                    datamap ARCObjectFixture.StoryItemIdAssay3DataMap
                                    group "assay:amplicon-sequencing:tables" "Tables" [
                                        tableWithSamples ARCObjectFixture.StoryItemIdAssay3Table1 "ASV Abundance Table" [
                                            tag ARCObjectFixture.StoryItemIdAssay3Sample1 "SoilCore-A"
                                        ]
                                    ]
                                ]
                        ]
                        group "group:workflows" "Workflows" [
                            objectNode
                                ArcExplorerNodeKind.Workflow
                                ARCObjectFixture.StoryItemIdWorkflow
                                "ExtractionWorkflow"
                                [ datamap ARCObjectFixture.StoryItemIdWorkflowDataMap ]
                            objectNode
                                ArcExplorerNodeKind.Workflow
                                ARCObjectFixture.StoryItemIdWorkflow2
                                "CleanupWorkflow"
                                [ datamap ARCObjectFixture.StoryItemIdWorkflow2DataMap ]
                        ]
                        group "group:runs" "Runs" [
                            objectNode ArcExplorerNodeKind.Run ARCObjectFixture.StoryItemIdRun "Run-2026-04-01" [
                                datamap ARCObjectFixture.StoryItemIdRunDataMap
                                group "run:2026-04-01:tables" "Tables" [
                                    table ARCObjectFixture.StoryItemIdRunTable1 "Injection Sequence"
                                    table ARCObjectFixture.StoryItemIdRunTable2 "Instrument QC"
                                ]
                            ]
                            objectNode ArcExplorerNodeKind.Run ARCObjectFixture.StoryItemIdRun2 "Run-2026-04-08" [
                                datamap ARCObjectFixture.StoryItemIdRun2DataMap
                                group "run:2026-04-08:tables" "Tables" [
                                    table ARCObjectFixture.StoryItemIdRun2Table1 "Injection Sequence"
                                    table ARCObjectFixture.StoryItemIdRun2Table2 "Retention Alignment QC"
                                ]
                            ]
                        ]
                        group "group:notes" "Notes" [
                            note ARCObjectFixture.StoryItemIdNoteRoot1 "Project overview"
                            note ARCObjectFixture.StoryItemIdNoteRoot2 "Release checklist"
                            note ARCObjectFixture.StoryItemIdCanonicalStudyNote1 "Sampling protocol"
                            note ARCObjectFixture.StoryItemIdCanonicalStudyNote2 "Leaf scoring rubric"
                            note ARCObjectFixture.StoryItemIdCanonicalStudyNote3 "Field observations"
                        ]
                    ]
                    true with
                    ItemType = ArcExplorerNodeKind.label ArcExplorerNodeKind.Arc
            }
        ]

    static member StoryMeta =
        [
            ARCObjectFixture.StoryItemIdRoot,
            ("MyArc", "ARC", "Canonical root", "isa.investigation.xlsx", "The ARC root groups all registered objects.")
            ARCObjectFixture.StoryItemIdStudy,
            ("PlantStressStudy",
             "Study",
             "Canonical object",
             "studies/PlantStressStudy/isa.study.xlsx",
             "Registered study with linked assay refs, notes, and derived samples.")
            ARCObjectFixture.StoryItemIdStudyAssayRef,
            ("MetabolomicsAssay",
             "Assay Reference",
             "Relationship node",
             "assays/MetabolomicsAssay/isa.assay.xlsx",
             "Reference node from the study to the canonical assay entry.")
            ARCObjectFixture.StoryItemIdStudyAssayRef2,
            ("TranscriptomicsAssay",
             "Assay Reference",
             "Relationship node",
             "assays/TranscriptomicsAssay/isa.assay.xlsx",
             "A second assay reference on the same study to show multi-assay study links.")
            ARCObjectFixture.StoryItemIdStudy2,
            ("SoilMicrobiomeStudy",
             "Study",
             "Canonical object",
             "studies/SoilMicrobiomeStudy/isa.study.xlsx",
             "A second study broadens the ARC tree and shows that the widget handles multiple study branches.")
            ARCObjectFixture.StoryItemIdStudy2AssayRef,
            ("AmpliconSequencingAssay",
             "Assay Reference",
             "Relationship node",
             "assays/AmpliconSequencingAssay/isa.assay.xlsx",
             "Study-to-assay reference for the soil microbiome branch.")
            ARCObjectFixture.StoryItemIdAssay,
            ("MetabolomicsAssay",
             "Assay",
             "Canonical object",
             "assays/MetabolomicsAssay/isa.assay.xlsx",
             "Assay metadata and tables live on the canonical assay node.")
            ARCObjectFixture.StoryItemIdAssay2,
            ("TranscriptomicsAssay",
             "Assay",
             "Canonical object",
             "assays/TranscriptomicsAssay/isa.assay.xlsx",
             "A second canonical assay highlights that the explorer is not limited to a single assay.")
            ARCObjectFixture.StoryItemIdAssay3,
            ("AmpliconSequencingAssay",
             "Assay",
             "Canonical object",
             "assays/AmpliconSequencingAssay/isa.assay.xlsx",
             "A third assay extends the storybook ARC with a distinct study branch.")
            ARCObjectFixture.StoryItemIdWorkflow,
            ("ExtractionWorkflow",
             "Workflow",
             "Canonical object",
             "workflows/ExtractionWorkflow/isa.workflow.xlsx",
             "Workflow nodes can expose subworkflow references and workflow datamaps.")
            ARCObjectFixture.StoryItemIdWorkflow2,
            ("CleanupWorkflow",
             "Workflow",
             "Canonical object",
             "workflows/CleanupWorkflow/isa.workflow.xlsx",
             "A second workflow makes the workflow area visibly multi-item.")
            ARCObjectFixture.StoryItemIdRun,
            ("Run-2026-04-01",
             "Run",
             "Canonical object",
             "runs/Run-2026-04-01/isa.run.xlsx",
             "Run nodes can reference workflows and derived sample collections.")
            ARCObjectFixture.StoryItemIdRun2,
            ("Run-2026-04-08",
             "Run",
             "Canonical object",
             "runs/Run-2026-04-08/isa.run.xlsx",
             "A second run shows repeated operational objects in the story.")
            ARCObjectFixture.StoryItemIdStudyTable1,
            ("Study Design Matrix",
             "Table",
             "Workbook child",
             "studies/PlantStressStudy/isa.study.xlsx -> Table 1",
             "Study table listing drought regimes, greenhouse blocks, and scheduled harvest days.")
            ARCObjectFixture.StoryItemIdStudyTable2,
            ("Phenotype Scoring Table",
             "Table",
             "Workbook child",
             "studies/PlantStressStudy/isa.study.xlsx -> Table 2",
             "Study table capturing wilt scores, imaging slots, and recovery observations.")
            ARCObjectFixture.StoryItemIdStudy2Table1,
            ("Plot Sampling Schedule",
             "Table",
             "Workbook child",
             "studies/SoilMicrobiomeStudy/isa.study.xlsx -> Table 1",
             "Study table mapping field plots to treatment arms and collection weeks.")
            ARCObjectFixture.StoryItemIdAssayTable1,
            ("Metabolite Measurements",
             "Table",
             "Workbook child",
             "assays/MetabolomicsAssay/isa.assay.xlsx -> Table 1",
             "Primary metabolomics matrix with feature intensities for each sampled plant.")
            ARCObjectFixture.StoryItemIdAssayTable2,
            ("Peak Annotation",
             "Table",
             "Workbook child",
             "assays/MetabolomicsAssay/isa.assay.xlsx -> Table 2",
             "Feature annotation table linking peaks to compounds and confidence levels.")
            ARCObjectFixture.StoryItemIdAssayTable3,
            ("QC Injection Summary",
             "Table",
             "Workbook child",
             "assays/MetabolomicsAssay/isa.assay.xlsx -> Table 3",
             "Quality-control summary table for pooled samples and internal standards.")
            ARCObjectFixture.StoryItemIdAssay2Table1,
            ("RNA Sample Sheet",
             "Table",
             "Workbook child",
             "assays/TranscriptomicsAssay/isa.assay.xlsx -> Table 1",
             "Transcriptomics table aligning extraction batches, libraries, and sequencing lanes.")
            ARCObjectFixture.StoryItemIdAssay2Table2,
            ("Differential Expression Matrix",
             "Table",
             "Workbook child",
             "assays/TranscriptomicsAssay/isa.assay.xlsx -> Table 2",
             "Result-oriented table summarising log fold changes and adjusted p-values.")
            ARCObjectFixture.StoryItemIdAssay3Table1,
            ("ASV Abundance Table",
             "Table",
             "Workbook child",
             "assays/AmpliconSequencingAssay/isa.assay.xlsx -> Table 1",
             "Amplicon assay table containing per-sample ASV abundances and taxonomy rollups.")
            ARCObjectFixture.StoryItemIdRunTable1,
            ("Injection Sequence",
             "Table",
             "Workbook child",
             "runs/Run-2026-04-01/isa.run.xlsx -> Table 1",
             "Run table describing acquisition order, blanks, pooled QC injections, and carryover checks.")
            ARCObjectFixture.StoryItemIdRunTable2,
            ("Instrument QC",
             "Table",
             "Workbook child",
             "runs/Run-2026-04-01/isa.run.xlsx -> Table 2",
             "Instrument quality table tracking spray stability and calibration checkpoints during the run.")
            ARCObjectFixture.StoryItemIdRun2Table1,
            ("Injection Sequence",
             "Table",
             "Workbook child",
             "runs/Run-2026-04-08/isa.run.xlsx -> Table 1",
             "Second run sequence table for the follow-up acquisition week.")
            ARCObjectFixture.StoryItemIdRun2Table2,
            ("Retention Alignment QC",
             "Table",
             "Workbook child",
             "runs/Run-2026-04-08/isa.run.xlsx -> Table 2",
             "Run table highlighting retention-time alignment drift and corrective actions.")
            ARCObjectFixture.StoryItemIdAssaySample1,
            ("Leaf-01",
             "Sample",
             "Relationship node",
             "-",
             "Sample reference derived from the metabolomics assay tables.")
            ARCObjectFixture.StoryItemIdAssaySample2,
            ("Leaf-02",
             "Sample",
             "Relationship node",
             "-",
             "A second metabolomics sample reference derived from the assay tables.")
            ARCObjectFixture.StoryItemIdAssay2Sample1,
            ("Leaf-01",
             "Sample",
             "Relationship node",
             "-",
             "Sample reference derived from the transcriptomics assay tables.")
            ARCObjectFixture.StoryItemIdAssay2Sample2,
            ("Leaf-02",
             "Sample",
             "Relationship node",
             "-",
             "A second transcriptomics sample reference derived from the assay tables.")
            ARCObjectFixture.StoryItemIdAssay3Sample1,
            ("SoilCore-A",
             "Sample",
             "Relationship node",
             "-",
             "Sample reference derived from the amplicon sequencing assay tables.")
            ARCObjectFixture.StoryItemIdStudyDataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "studies/PlantStressStudy/isa.datamap.xlsx",
             "Selecting the study datamap keeps tree focus on the DataMap object and opens the owning study in DataMap view.")
            ARCObjectFixture.StoryItemIdStudy2DataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "studies/SoilMicrobiomeStudy/isa.datamap.xlsx",
             "A second study datamap shows that multiple study branches can expose DataMap children.")
            ARCObjectFixture.StoryItemIdAssayDataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "assays/MetabolomicsAssay/isa.datamap.xlsx",
             "The metabolomics assay exposes a DataMap child in the ARC object tree.")
            ARCObjectFixture.StoryItemIdAssay2DataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "assays/TranscriptomicsAssay/isa.datamap.xlsx",
             "A second assay datamap keeps the assay branch visibly multi-object.")
            ARCObjectFixture.StoryItemIdAssay3DataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "assays/AmpliconSequencingAssay/isa.datamap.xlsx",
             "The amplicon sequencing assay also exposes its DataMap in the object tree.")
            ARCObjectFixture.StoryItemIdWorkflowDataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "workflows/ExtractionWorkflow/isa.datamap.xlsx",
             "Workflow datamaps appear as direct children of workflow objects.")
            ARCObjectFixture.StoryItemIdWorkflow2DataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "workflows/CleanupWorkflow/isa.datamap.xlsx",
             "A second workflow datamap shows that datamaps are not limited to studies and assays.")
            ARCObjectFixture.StoryItemIdRunDataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "runs/Run-2026-04-01/isa.datamap.xlsx",
             "Run datamaps appear directly under run objects.")
            ARCObjectFixture.StoryItemIdRun2DataMap,
            ("DataMap",
             "DataMap",
             "Canonical child",
             "runs/Run-2026-04-08/isa.datamap.xlsx",
             "A second run datamap rounds out the showcase across all supported ARC object kinds.")
            ARCObjectFixture.StoryItemIdNote,
            ("Sampling protocol",
             "Note",
             "Relationship node",
             "Notes/studies/PlantStressStudy/17_03_2026/sampling_protocol.md",
             "Reference node from the study to the canonical note entry in the top-level Notes section.")
            ARCObjectFixture.StoryItemIdNote2,
            ("Leaf scoring rubric",
             "Note",
             "Relationship node",
             "Notes/studies/PlantStressStudy/18_03_2026/leaf_scoring_rubric.md",
             "A second study note reference showing that a study can link multiple notes.")
            ARCObjectFixture.StoryItemIdNote3,
            ("Field observations",
             "Note",
             "Relationship node",
             "Notes/studies/SoilMicrobiomeStudy/18_03_2026/field_observations.md",
             "Reference node from the second study to its canonical note entry.")
            ARCObjectFixture.StoryItemIdNoteRoot1,
            ("Project overview",
             "Note",
             "Filesystem-backed",
             "Notes/19_03_2026/project_overview.md",
             "A root-level ARC note rendered directly under ARC -> Notes.")
            ARCObjectFixture.StoryItemIdNoteRoot2,
            ("Release checklist",
             "Note",
             "Filesystem-backed",
             "Notes/20_03_2026/release_checklist.md",
             "A second root-level note broadens the top-level Notes section in the showcase.")
            ARCObjectFixture.StoryItemIdCanonicalStudyNote1,
            ("Sampling protocol",
             "Note",
             "Filesystem-backed",
             "Notes/studies/PlantStressStudy/17_03_2026/sampling_protocol.md",
             "A study-targeted note rendered directly under ARC -> Notes.")
            ARCObjectFixture.StoryItemIdCanonicalStudyNote2,
            ("Leaf scoring rubric",
             "Note",
             "Filesystem-backed",
             "Notes/studies/PlantStressStudy/18_03_2026/leaf_scoring_rubric.md",
             "A second study-targeted note rendered directly under ARC -> Notes.")
            ARCObjectFixture.StoryItemIdCanonicalStudyNote3,
            ("Field observations",
             "Note",
             "Filesystem-backed",
             "Notes/studies/SoilMicrobiomeStudy/18_03_2026/field_observations.md",
             "A study-targeted note for the soil microbiome branch, shown directly under ARC -> Notes.")
            ARCObjectFixture.StoryItemIdSample,
            ("Leaf-01",
             "Sample",
             "Virtual node",
             "-",
             "Sample nodes are derived from table content, not standalone files.")
            ARCObjectFixture.StoryItemIdSample2,
            ("Leaf-02",
             "Sample",
             "Virtual node",
             "-",
             "A second sample keeps the plant stress study visibly multi-sample.")
            ARCObjectFixture.StoryItemIdSample3,
            ("SoilCore-A", "Sample", "Virtual node", "-", "A sample belonging to the second study branch.")
        ]
        |> Map.ofList

    static member StoryMetadataRows =
        [
            ARCObjectFixture.StoryItemIdStudy,
            [
                "Identifier", "PS-2026-001"
                "Title", "Plant Stress Response Under Progressive Drought"
                "Description",
                "A controlled greenhouse study tracking transcript, metabolite, and phenotype changes during water limitation."
                "Tables", "2"
                "Submission Date", "2026-03-12"
                "Public Release", "2026-06-01"
                "Design", "Drought stress; time series"
                "Contacts", "Nadia Green; Oliver Hartmann"
                "Publications", "1"
                "Comments", "Includes shared sample identifiers across linked assays."
            ]
            ARCObjectFixture.StoryItemIdStudy2,
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
            ARCObjectFixture.StoryItemIdAssay,
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
            ARCObjectFixture.StoryItemIdStudyAssayRef,
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
            ARCObjectFixture.StoryItemIdAssay2,
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
            ARCObjectFixture.StoryItemIdStudyAssayRef2,
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
            ARCObjectFixture.StoryItemIdAssay3,
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
            ARCObjectFixture.StoryItemIdStudy2AssayRef,
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
            ARCObjectFixture.StoryItemIdWorkflow,
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
            ARCObjectFixture.StoryItemIdWorkflow2,
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
            ARCObjectFixture.StoryItemIdRun,
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
            ARCObjectFixture.StoryItemIdRun2,
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
            ARCObjectFixture.StoryItemIdStudyTable1,
            [
                "Identifier", "Study Design Matrix"
                "Rows", "12"
                "Columns", "6"
                "Parent", "PlantStressStudy"
                "Primary Column", "Factor[Water regime]"
                "Comments", "Defines treatment blocks and harvest days for the drought study."
            ]
            ARCObjectFixture.StoryItemIdStudyTable2,
            [
                "Identifier", "Phenotype Scoring Table"
                "Rows", "36"
                "Columns", "7"
                "Parent", "PlantStressStudy"
                "Primary Column", "Characteristic[Leaf wilting]"
                "Comments", "Captures repeated visual scores for each plant across time points."
            ]
            ARCObjectFixture.StoryItemIdStudy2Table1,
            [
                "Identifier", "Plot Sampling Schedule"
                "Rows", "18"
                "Columns", "5"
                "Parent", "SoilMicrobiomeStudy"
                "Primary Column", "Factor[Compost treatment]"
                "Comments", "Maps field plots to amendment regime and collection week."
            ]
            ARCObjectFixture.StoryItemIdAssayTable1,
            [
                "Identifier", "Metabolite Measurements"
                "Rows", "96"
                "Columns", "14"
                "Parent", "MetabolomicsAssay"
                "Primary Column", "Sample Name"
                "Comments", "Main assay matrix with quantified metabolite features."
            ]
            ARCObjectFixture.StoryItemIdAssayTable2,
            [
                "Identifier", "Peak Annotation"
                "Rows", "96"
                "Columns", "9"
                "Parent", "MetabolomicsAssay"
                "Primary Column", "Metabolite Assignment"
                "Comments", "Links detected features to candidate compounds and confidence scores."
            ]
            ARCObjectFixture.StoryItemIdAssayTable3,
            [
                "Identifier", "QC Injection Summary"
                "Rows", "18"
                "Columns", "6"
                "Parent", "MetabolomicsAssay"
                "Primary Column", "QC Type"
                "Comments", "Summarises pooled QC behaviour and internal standard drift."
            ]
            ARCObjectFixture.StoryItemIdAssay2Table1,
            [
                "Identifier", "RNA Sample Sheet"
                "Rows", "48"
                "Columns", "10"
                "Parent", "TranscriptomicsAssay"
                "Primary Column", "Library Name"
                "Comments", "Associates extracted RNA with library prep batches and lanes."
            ]
            ARCObjectFixture.StoryItemIdAssay2Table2,
            [
                "Identifier", "Differential Expression Matrix"
                "Rows", "24"
                "Columns", "8"
                "Parent", "TranscriptomicsAssay"
                "Primary Column", "Gene Identifier"
                "Comments", "Aggregates contrast results used for downstream interpretation."
            ]
            ARCObjectFixture.StoryItemIdAssay3Table1,
            [
                "Identifier", "ASV Abundance Table"
                "Rows", "64"
                "Columns", "11"
                "Parent", "AmpliconSequencingAssay"
                "Primary Column", "OTU/ASV Identifier"
                "Comments", "Contains abundance counts and taxonomy summaries for each soil core."
            ]
            ARCObjectFixture.StoryItemIdRunTable1,
            [
                "Identifier", "Injection Sequence"
                "Rows", "54"
                "Columns", "7"
                "Parent", "Run-2026-04-01"
                "Primary Column", "Injection Order"
                "Comments", "Acquisition order including blanks, pooled QC, and study samples."
            ]
            ARCObjectFixture.StoryItemIdRunTable2,
            [
                "Identifier", "Instrument QC"
                "Rows", "18"
                "Columns", "6"
                "Parent", "Run-2026-04-01"
                "Primary Column", "QC Check"
                "Comments", "Instrument checks recorded throughout the first drought run."
            ]
            ARCObjectFixture.StoryItemIdRun2Table1,
            [
                "Identifier", "Injection Sequence"
                "Rows", "57"
                "Columns", "7"
                "Parent", "Run-2026-04-08"
                "Primary Column", "Injection Order"
                "Comments", "Follow-up acquisition order for the second drought week."
            ]
            ARCObjectFixture.StoryItemIdRun2Table2,
            [
                "Identifier", "Retention Alignment QC"
                "Rows", "24"
                "Columns", "5"
                "Parent", "Run-2026-04-08"
                "Primary Column", "Retention Drift"
                "Comments", "Tracks alignment drift and correction status across the run."
            ]
        ]
        |> Map.ofList
