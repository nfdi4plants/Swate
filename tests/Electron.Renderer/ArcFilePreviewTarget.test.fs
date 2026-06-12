module ElectronRenderer.ArcFilePreviewTargetTests

open Vitest
open Fable.Core
open ARCtrl
open Renderer.Components.MainContent.ArcFilePreviewTargetHelper
open Swate.Components.Composite.Widgets.JsonImport.Types
open Swate.Components.Page.ArcFileEditor.Types
open Swate.Components.Shared


let private createAssayArcFile (tableNames: string[]) =
    let assay = ArcAssay.init "TestAssay"
    assay.Title <- Some "Metadata title"
    assay.Description <- Some "Metadata description"

    tableNames
    |> Array.iter (fun tableName -> assay.AddTable(ArcTable.init tableName))

    ArcFiles.Assay assay, assay

let private deleteSelectedTable arcFile tableIndex =
    let mutable publishedArcFile: ArcFiles option = None
    let mutable activeView: ActiveView option = None

    deleteSelectedTable
        arcFile
        tableIndex
        (fun nextArcFile -> publishedArcFile <- Some nextArcFile)
        (fun nextActiveView -> activeView <- Some nextActiveView)

    publishedArcFile, activeView

let private expectMetadataPreserved (assay: ArcAssay) =
    Vitest.expect(assay.Identifier).toBe ("TestAssay")
    Vitest.expect(assay.Title).toEqual (Some "Metadata title")
    Vitest.expect(assay.Description).toEqual (Some "Metadata description")

let private expectMetadataView activeView =
    match activeView with
    | Some ActiveView.Metadata -> ()
    | _ -> failwith "Expected the metadata view to be activated after table deletion."

let private jsonImportRequest importedFile = {
    ImportedFile = importedFile
    SourceFileName = Some "import.json"
    JsonFormat = JsonExportFormat.ARCtrl
}

Vitest.describe (
    "ArcFilePreviewTarget table deletion",
    fun () ->
        Vitest.test (
            "deletes only the selected middle table and preserves metadata",
            fun () ->
                let arcFile, assay = createAssayArcFile [| "First"; "Selected"; "Last" |]

                let publishedArcFile, activeView = deleteSelectedTable arcFile 1

                Vitest.expect(assay.Tables |> Seq.map _.Name |> Seq.toArray).toEqual ([| "First"; "Last" |])
                expectMetadataPreserved assay
                Vitest.expect(publishedArcFile.IsSome).toBe (true)
                expectMetadataView activeView
        )

        Vitest.test (
            "deletes the only table while preserving metadata",
            fun () ->
                let arcFile, assay = createAssayArcFile [| "Only Table" |]

                let publishedArcFile, activeView = deleteSelectedTable arcFile 0

                Vitest.expect(assay.Tables.Count).toBe (0)
                expectMetadataPreserved assay
                Vitest.expect(publishedArcFile.IsSome).toBe (true)
                expectMetadataView activeView
        )
)

Vitest.describe (
    "ArcFilePreviewTarget JSON import",
    fun () ->
        Vitest.test (
            "rejects replacing an Assay editor with Study JSON",
            fun () -> promise {
                let currentArcFile, _ = createAssayArcFile [||]
                let importedStudy = ArcStudy.init "ImportedStudy" |> fun study -> ArcFiles.Study(study, [])
                let mutable publishedArcFile: ArcFiles option = None
                let mutable inMemoryUpdated = false

                let! result =
                    importJsonRequestIntoCurrentTarget
                        currentArcFile
                        (jsonImportRequest importedStudy)
                        (fun nextArcFile -> publishedArcFile <- Some nextArcFile)
                        (fun _ -> promise {
                            inMemoryUpdated <- true
                            return Ok()
                        })

                match result with
                | Ok() -> failwith "Expected mismatched JSON import to fail."
                | Error exn -> Vitest.expect(exn.Message).toContain ("Cannot import Study JSON")

                Vitest.expect(publishedArcFile.IsNone).toBe (true)
                Vitest.expect(inMemoryUpdated).toBe (false)
            }
        )

        Vitest.test (
            "preserves DataMap parent info while replacing imported DataMap content",
            fun () ->
                let parentInfo = DatamapParentInfo.create "assay-parent" DataMapParent.Assay
                let currentDataMap = DataMap.init ()
                let importedDataMap = DataMap.init ()
                importedDataMap.DataContexts.Add(DataContext())

                let result =
                    Json.Import.applyToCurrentArcFile (
                        ArcFiles.DataMap(Some parentInfo, currentDataMap),
                        ArcFiles.DataMap(None, importedDataMap)
                    )

                match result with
                | Error exn -> failwith $"Expected DataMap import preparation to succeed: {exn.Message}"
                | Ok(ArcFiles.DataMap(importedParentInfo, preparedDataMap)) ->
                    Vitest.expect(importedParentInfo).toEqual (Some parentInfo)
                    Vitest.expect(preparedDataMap.DataContexts.Count).toBe (1)
                | Ok _ -> failwith "Expected prepared import to remain a DataMap."
        )

        Vitest.test (
            "successful table import appends tables with unique names and invokes in-memory ARC update",
            fun () -> promise {
                let currentArcFile, currentAssay =
                    createAssayArcFile [| "Existing"; "Duplicate"; "Duplicate 1" |]

                let importedAssay = ArcAssay.init "ImportedAssay"
                importedAssay.AddTable(ArcTable.init "Duplicate")
                importedAssay.AddTable(ArcTable.init "Fresh")
                let importedFile = ArcFiles.Assay importedAssay
                let publishedArcFiles = ResizeArray<ArcFiles>()
                let inMemoryUpdates = ResizeArray<ArcFiles>()

                let! result =
                    importJsonRequestIntoCurrentTarget
                        currentArcFile
                        (jsonImportRequest importedFile)
                        (fun nextArcFile -> publishedArcFiles.Add nextArcFile)
                        (fun nextArcFile -> promise {
                            inMemoryUpdates.Add nextArcFile
                            return Ok()
                        })

                match result with
                | Error exn -> failwith $"Expected JSON import to succeed: {exn.Message}"
                | Ok() -> ()

                Vitest.expect(publishedArcFiles.Count).toBe (1)
                Vitest.expect(inMemoryUpdates.Count).toBe (1)

                match publishedArcFiles.[0], inMemoryUpdates.[0] with
                | ArcFiles.Assay publishedAssay, ArcFiles.Assay inMemoryAssay ->
                    Vitest.expect(publishedAssay.Identifier).toBe ("TestAssay")
                    Vitest.expect(inMemoryAssay.Identifier).toBe ("TestAssay")

                    let expectedNames = [|
                        "Existing"
                        "Duplicate"
                        "Duplicate 1"
                        "Duplicate 2"
                        "Fresh"
                    |]

                    Vitest.expect(publishedAssay.Tables |> Seq.map _.Name |> Seq.toArray).toEqual (expectedNames)
                    Vitest.expect(inMemoryAssay.Tables |> Seq.map _.Name |> Seq.toArray).toEqual (expectedNames)
                    Vitest.expect(currentAssay.Tables |> Seq.map _.Name |> Seq.toArray).toEqual (expectedNames)
                | _ -> failwith "Expected imported Assay to be published and sent to in-memory update."
            }
        )
)
