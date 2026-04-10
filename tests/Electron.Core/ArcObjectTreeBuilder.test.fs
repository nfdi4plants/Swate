module ElectronCore.ArcObjectTreeBuilderTests

open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Vitest

module ArcObjectTreeBuilder = Main.ArcObjectTreeBuilder

let private createTextCells (values: string list) =
    values |> List.map CompositeCell.FreeText |> ResizeArray

let private createTermCells (values: string list) =
    values
    |> List.map (fun value -> OntologyAnnotation.create(name = value) |> CompositeCell.Term)
    |> ResizeArray

let private createOutputSampleTable (tableName: string) (sampleNames: string list) =
    let table = ArcTable.init tableName
    table.AddColumn(CompositeHeader.Output IOType.Sample, createTextCells sampleNames)
    table

let private createCharacteristicSampleTable
    (tableName: string)
    (sampleName: string)
    (characteristicName: string)
    (characteristicValue: string)
    =
    let table = ArcTable.init tableName
    table.AddColumn(CompositeHeader.Input IOType.Sample, createTextCells [ sampleName ])
    table.AddColumn(
        CompositeHeader.Characteristic(OntologyAnnotation.create characteristicName),
        createTermCells [ characteristicValue ]
    )
    table.AddColumn(CompositeHeader.Output IOType.Sample, createTextCells [ sampleName ])
    table

let private createFactorSampleTable
    (tableName: string)
    (sampleName: string)
    (factorName: string)
    (factorValue: string)
    =
    let table = ArcTable.init tableName
    table.AddColumn(CompositeHeader.Input IOType.Sample, createTextCells [ sampleName ])
    table.AddColumn(CompositeHeader.Factor(OntologyAnnotation.create factorName), createTermCells [ factorValue ])
    table.AddColumn(CompositeHeader.Output IOType.Sample, createTextCells [ sampleName ])
    table

let private createArc (studies: ArcStudy list) (assays: ArcAssay list) =
    ARC("MyArc", studies = ResizeArray studies, assays = ResizeArray assays)

let rec private flattenNodes (nodes: ArcExplorerNode list) =
    seq {
        for node in nodes do
            yield node
            yield! flattenNodes node.children
    }

let private expectNode predicate nodes =
    match nodes |> flattenNodes |> Seq.tryFind predicate with
    | Some node -> node
    | None -> failwith "Expected node to exist."

let private expectNoDirectSamples parentNode =
    let sampleChildren =
        parentNode.children
        |> List.filter (fun child -> child.kind = ArcExplorerNodeKind.Sample)

    Vitest.expect(sampleChildren.Length).toBe(0)

let private expectSampleNode sampleName parentNode =
    match parentNode.children |> List.tryFind (fun child -> child.kind = ArcExplorerNodeKind.Sample && child.name = sampleName) with
    | Some sampleNode -> sampleNode
    | None -> failwith $"Expected sample node {sampleName} under {parentNode.name}."

let private expectSampleSummary (node: ArcExplorerNode) =
    match node.sampleSummary with
    | Some summary -> summary
    | None -> failwith $"Expected sample summary on node {node.name}."

let private expectRelatedSample targetId (node: ArcExplorerNode) =
    match node.relatedSamples |> List.tryFind (fun link -> link.targetId = targetId) with
    | Some link -> link
    | None -> failwith $"Expected related sample link {targetId} on node {node.name}."

Vitest.describe("ArcObjectTreeBuilder sample projection", fun () ->
    Vitest.test("creates table-scoped canonical study samples and assay sample references", fun () ->
        let study = ArcStudy.init "PlantStressStudy"
        let assay = ArcAssay.init "MetabolomicsAssay"

        study.Tables.Add(createOutputSampleTable "Study Samples" [ "Leaf-01" ])
        assay.Tables.Add(createOutputSampleTable "Assay Samples" [ "Leaf-01" ])
        study.RegisterAssay assay.Identifier

        let tree = ArcObjectTreeBuilder.create "C:/repo" (createArc [ study ] [ assay ]) Seq.empty

        let studyNode = expectNode (fun node -> node.id = "study:PlantStressStudy") tree
        let assayNode = expectNode (fun node -> node.id = "assay:MetabolomicsAssay") tree
        let studyTableNode = expectNode (fun node -> node.id = "study:PlantStressStudy:table:0") tree
        let assayTableNode = expectNode (fun node -> node.id = "assay:MetabolomicsAssay:table:0") tree
        expectNoDirectSamples studyNode
        expectNoDirectSamples assayNode
        let studySampleNode = expectSampleNode "Leaf-01" studyTableNode
        let assaySampleNode = expectSampleNode "Leaf-01" assayTableNode
        let studySummary = expectSampleSummary studySampleNode
        let assaySummary = expectSampleSummary assaySampleNode
        let studyTableLink = expectRelatedSample studySampleNode.id studyTableNode
        let assayTableLink = expectRelatedSample assaySampleNode.id assayTableNode

        Vitest.expect(studySampleNode.isReference).toBe(false)
        Vitest.expect(studySampleNode.id).toBe("study:PlantStressStudy:table:0:sample:Leaf-01")
        Vitest.expect(studySampleNode.path).toEqual(Some(ARCtrl.Helper.Identifier.Study.fileNameFromIdentifier study.Identifier))
        Vitest.expect(studySummary.Studies).toEqual([ study.Identifier ])
        Vitest.expect(studySummary.Assays).toEqual([])
        Vitest.expect(studyTableLink.name).toBe("Leaf-01")
        Vitest.expect(studyTableLink.subtitle).toEqual(Some "Canonical sample")

        Vitest.expect(assaySampleNode.isReference).toBe(true)
        Vitest.expect(assaySampleNode.id).toBe("assay:MetabolomicsAssay:table:0:sample-ref:Leaf-01")
        Vitest.expect(assaySampleNode.path).toEqual(Some(ARCtrl.Helper.Identifier.Assay.fileNameFromIdentifier assay.Identifier))
        Vitest.expect(assaySummary.Studies).toEqual([ study.Identifier ])
        Vitest.expect(assaySummary.Assays).toEqual([ assay.Identifier ])
        Vitest.expect(assayTableLink.name).toBe("Leaf-01")
        Vitest.expect(assayTableLink.subtitle).toEqual(Some "Reference sample"))

    Vitest.test("keeps same sample names from different tables separate", fun () ->
        let study = ArcStudy.init "MergeStudy"

        study.Tables.Add(createCharacteristicSampleTable "Characteristics Table" "Leaf-01" "Organism" "Arabidopsis thaliana")
        study.Tables.Add(createFactorSampleTable "Factors Table" "Leaf-01" "Treatment" "Drought")

        let tree = ArcObjectTreeBuilder.create "C:/repo" (createArc [ study ] []) Seq.empty
        let studyNode = expectNode (fun node -> node.id = "study:MergeStudy") tree
        let characteristicsTable = expectNode (fun node -> node.id = "study:MergeStudy:table:0") tree
        let factorsTable = expectNode (fun node -> node.id = "study:MergeStudy:table:1") tree
        let characteristicSampleNode = expectSampleNode "Leaf-01" characteristicsTable
        let factorSampleNode = expectSampleNode "Leaf-01" factorsTable
        let characteristicSummary = expectSampleSummary characteristicSampleNode
        let factorSummary = expectSampleSummary factorSampleNode

        expectNoDirectSamples studyNode
        Vitest.expect(characteristicSampleNode.id = factorSampleNode.id).toBe(false)
        Vitest.expect(characteristicSummary.Characteristics).toEqual([ "Organism" ])
        Vitest.expect(characteristicSummary.Factors).toEqual([])
        Vitest.expect(characteristicSummary.SourceTables).toEqual([ "Characteristics Table" ])
        Vitest.expect(factorSummary.Characteristics).toEqual([])
        Vitest.expect(factorSummary.Factors).toEqual([ "Treatment" ])
        Vitest.expect(factorSummary.SourceTables).toEqual([ "Factors Table" ]))

    Vitest.test("keeps case-distinct sample names separate within the same table", fun () ->
        let study = ArcStudy.init "CaseSensitiveStudy"

        study.Tables.Add(createOutputSampleTable "Case Samples" [ "Leaf-01"; "leaf-01" ])

        let tree = ArcObjectTreeBuilder.create "C:/repo" (createArc [ study ] []) Seq.empty
        let tableNode = expectNode (fun node -> node.id = "study:CaseSensitiveStudy:table:0") tree
        let upperCaseSampleNode = expectSampleNode "Leaf-01" tableNode
        let lowerCaseSampleNode = expectSampleNode "leaf-01" tableNode
        let upperCaseSummary = expectSampleSummary upperCaseSampleNode
        let lowerCaseSummary = expectSampleSummary lowerCaseSampleNode

        Vitest.expect(upperCaseSampleNode.id).toBe("study:CaseSensitiveStudy:table:0:sample:Leaf-01")
        Vitest.expect(lowerCaseSampleNode.id).toBe("study:CaseSensitiveStudy:table:0:sample:leaf-01")
        Vitest.expect(upperCaseSampleNode.id = lowerCaseSampleNode.id).toBe(false)
        Vitest.expect(upperCaseSummary.SourceTables).toEqual([ "Case Samples" ])
        Vitest.expect(lowerCaseSummary.SourceTables).toEqual([ "Case Samples" ]))

    Vitest.test("merges repeated exact-name duplicates within the same table", fun () ->
        let study = ArcStudy.init "ExactDuplicateStudy"

        study.Tables.Add(createOutputSampleTable "Duplicate Samples" [ "Leaf-01"; "Leaf-01" ])

        let tree = ArcObjectTreeBuilder.create "C:/repo" (createArc [ study ] []) Seq.empty
        let tableNode = expectNode (fun node -> node.id = "study:ExactDuplicateStudy:table:0") tree
        let sampleChildren =
            tableNode.children |> List.filter (fun child -> child.kind = ArcExplorerNodeKind.Sample)
        let sampleNode = expectSampleNode "Leaf-01" tableNode
        let summary = expectSampleSummary sampleNode

        Vitest.expect(sampleChildren.Length).toBe(1)
        Vitest.expect(sampleNode.id).toBe("study:ExactDuplicateStudy:table:0:sample:Leaf-01")
        Vitest.expect(summary.SourceTables).toEqual([ "Duplicate Samples" ]))

    Vitest.test("ignores blank sample names", fun () ->
        let assay = ArcAssay.init "BlankFilterAssay"

        assay.Tables.Add(createOutputSampleTable "Assay Sheet" [ ""; "   "; "Leaf-01" ])

        let tree = ArcObjectTreeBuilder.create "C:/repo" (createArc [] [ assay ]) Seq.empty
        let assayNode = expectNode (fun node -> node.id = "assay:BlankFilterAssay") tree
        let assayTableNode = expectNode (fun node -> node.id = "assay:BlankFilterAssay:table:0") tree
        let sampleChildren =
            assayTableNode.children |> List.filter (fun child -> child.kind = ArcExplorerNodeKind.Sample)

        expectNoDirectSamples assayNode
        Vitest.expect(sampleChildren.Length).toBe(1)
        Vitest.expect(sampleChildren.[0].name).toBe("Leaf-01"))

    Vitest.test("keeps assay-only samples scoped to the assay table", fun () ->
        let study = ArcStudy.init "AssayOnlyStudy"
        let assay = ArcAssay.init "TranscriptomicsAssay"

        assay.Tables.Add(createOutputSampleTable "RNA Sample Sheet" [ "RNA-01" ])
        study.RegisterAssay assay.Identifier

        let tree = ArcObjectTreeBuilder.create "C:/repo" (createArc [ study ] [ assay ]) Seq.empty
        let studyNode = expectNode (fun node -> node.id = "study:AssayOnlyStudy") tree
        let assayTableNode = expectNode (fun node -> node.id = "assay:TranscriptomicsAssay:table:0") tree
        let sampleNode = expectSampleNode "RNA-01" assayTableNode
        let summary = expectSampleSummary sampleNode

        expectNoDirectSamples studyNode
        Vitest.expect(summary.Studies).toEqual([ study.Identifier ])
        Vitest.expect(summary.Assays).toEqual([ assay.Identifier ])
        Vitest.expect(summary.SourceTables).toEqual([ "RNA Sample Sheet" ]))

    Vitest.test("keeps duplicate sample names from different studies separate", fun () ->
        let studyA = ArcStudy.init "StudyA"
        let studyB = ArcStudy.init "StudyB"

        studyA.Tables.Add(createOutputSampleTable "Study A Samples" [ "Shared-01" ])
        studyB.Tables.Add(createOutputSampleTable "Study B Samples" [ "Shared-01" ])

        let tree = ArcObjectTreeBuilder.create "C:/repo" (createArc [ studyA; studyB ] []) Seq.empty

        let sharedSamples =
            tree
            |> flattenNodes
            |> Seq.filter (fun node -> node.kind = ArcExplorerNodeKind.Sample && node.name = "Shared-01")
            |> Seq.toList

        Vitest.expect(sharedSamples.Length).toBe(2)
        Vitest.expect(sharedSamples |> List.map _.id |> List.distinct |> List.length).toBe(2)
        Vitest.expect(sharedSamples |> List.map (expectSampleSummary >> fun summary -> summary.Studies)).toEqual([ [ "StudyA" ]; [ "StudyB" ] ]))
)
