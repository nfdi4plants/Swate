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

    tableNames
    |> Array.iter (fun tableName -> assay.AddTable(ArcTable.init tableName))

    ArcFiles.Assay assay, assay

let private jsonImportRequest importedFile = {
    ImportedFile = importedFile
    SourceFileName = Some "import.json"
    JsonFormat = JsonExportFormat.ARCtrl
}

Vitest.describe (
    "ArcFilePreviewTarget active-view behavior",
    fun () ->
        Vitest.test (
            "preserves an internally selected table across ordinary ARC value updates",
            fun () ->
                let arcFile, _ = createAssayArcFile [| "First"; "Selected" |]
                let refreshedArcFile = ArcFiles.refreshRef arcFile

                Vitest.expect(ActiveView.Forward(refreshedArcFile, ActiveView.Table 1)).toEqual (ActiveView.Table 1)
        )

        Vitest.test (
            "changes the editor remount key for sidebar view selections but not ARC value refreshes",
            fun () ->
                let arcFile, _ = createAssayArcFile [| "Table" |]
                let refreshedArcFile = ArcFiles.refreshRef arcFile
                let tableKey = editorKey arcFile (Some(ActiveView.Table 0))

                Vitest.expect(editorKey refreshedArcFile (Some(ActiveView.Table 0))).toEqual (tableKey)
                Vitest.expect(editorKey arcFile (Some ActiveView.Metadata)).not.toEqual (tableKey)
                Vitest.expect(editorKey arcFile (Some ActiveView.DataMap)).not.toEqual (tableKey)
        )

        Vitest.test (
            "keeps the active table valid through reorder and deletion while drag identifiers stay stable",
            fun () ->
                let arcFile, _ = createAssayArcFile [| "First"; "Selected"; "Last" |]

                let dragIdsBefore =
                    Swate.Components.Page.ArcFileEditor.Helper.tableDragIds (arcFile.Tables().Count)
                    |> Seq.toArray

                let reorderedArcFile = ArcFiles.refreshRef arcFile
                reorderedArcFile.ArcTables().MoveTable(1, 2)
                let activeAfterReorder = ActiveView.Table 2

                let dragIdsAfterReorder =
                    Swate.Components.Page.ArcFileEditor.Helper.tableDragIds (reorderedArcFile.Tables().Count)
                    |> Seq.toArray

                Vitest.expect(ActiveView.Forward(reorderedArcFile, activeAfterReorder)).toEqual (activeAfterReorder)
                Vitest.expect(dragIdsAfterReorder).toEqual (dragIdsBefore)

                let afterDeletion = ArcFiles.refreshRef reorderedArcFile
                afterDeletion.ArcTables().RemoveTableAt 2

                Vitest.expect(ActiveView.Forward(afterDeletion, activeAfterReorder)).toEqual (ActiveView.Table 0)
        )
)

Vitest.describe (
    "ArcFilePreviewTarget JSON import",
    fun () ->
        Vitest.test (
            "rejects replacing an Assay editor with Study JSON without committing",
            fun () -> promise {
                let currentArcFile, _ = createAssayArcFile [||]

                let importedStudy =
                    ArcStudy.init "ImportedStudy" |> fun study -> ArcFiles.Study(study, [])

                let mutable mutated = false
                let mutable replaced = false

                let! result =
                    importJsonRequestIntoCurrentTarget
                        currentArcFile
                        (jsonImportRequest importedStudy)
                        (fun _ -> mutated <- true)
                        (fun _ -> replaced <- true)

                match result with
                | Ok() -> failwith "Expected mismatched JSON import to fail."
                | Error exn -> Vitest.expect(exn.Message).toContain ("Cannot import study JSON")

                Vitest.expect(mutated).toBe (false)
                Vitest.expect(replaced).toBe (false)
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
            "successful table import appends tables in place and commits through mutate",
            fun () -> promise {
                let currentArcFile, currentAssay =
                    createAssayArcFile [| "Existing"; "Duplicate"; "Duplicate 1" |]

                let importedAssay = ArcAssay.init "ImportedAssay"
                importedAssay.AddTable(ArcTable.init "Duplicate")
                importedAssay.AddTable(ArcTable.init "Fresh")
                let importedFile = ArcFiles.Assay importedAssay
                let mutatedArcFiles = ResizeArray<ArcFiles>()
                let mutable replaced = false

                let! result =
                    importJsonRequestIntoCurrentTarget
                        currentArcFile
                        (jsonImportRequest importedFile)
                        (fun update ->
                            update currentArcFile
                            mutatedArcFiles.Add currentArcFile
                        )
                        (fun _ -> replaced <- true)

                match result with
                | Error exn -> failwith $"Expected JSON import to succeed: {exn.Message}"
                | Ok() -> ()

                Vitest.expect(mutatedArcFiles.Count).toBe (1)
                Vitest.expect(System.Object.ReferenceEquals(mutatedArcFiles.[0], currentArcFile)).toBe (true)
                Vitest.expect(replaced).toBe (false)

                let expectedNames = [|
                    "Existing"
                    "Duplicate"
                    "Duplicate 1"
                    "Duplicate 2"
                    "Fresh"
                |]

                Vitest.expect(currentAssay.Tables |> Seq.map _.Name |> Seq.toArray).toEqual (expectedNames)
            }
        )

        Vitest.test (
            "DataMap import replaces the open arc file through replace",
            fun () -> promise {
                let parentInfo = DatamapParentInfo.create "assay-parent" DataMapParent.Assay
                let currentArcFile = ArcFiles.DataMap(Some parentInfo, DataMap.init ())
                let importedDataMap = DataMap.init ()
                importedDataMap.DataContexts.Add(DataContext())
                let importedFile = ArcFiles.DataMap(None, importedDataMap)
                let mutable mutated = false
                let replacedArcFiles = ResizeArray<ArcFiles>()

                let! result =
                    importJsonRequestIntoCurrentTarget
                        currentArcFile
                        (jsonImportRequest importedFile)
                        (fun _ -> mutated <- true)
                        (fun nextArcFile -> replacedArcFiles.Add nextArcFile)

                match result with
                | Error exn -> failwith $"Expected DataMap import to succeed: {exn.Message}"
                | Ok() -> ()

                Vitest.expect(mutated).toBe (false)
                Vitest.expect(replacedArcFiles.Count).toBe (1)

                match replacedArcFiles.[0] with
                | ArcFiles.DataMap(importedParentInfo, preparedDataMap) ->
                    Vitest.expect(importedParentInfo).toEqual (Some parentInfo)
                    Vitest.expect(preparedDataMap.DataContexts.Count).toBe (1)
                | _ -> failwith "Expected the replaced arc file to remain a DataMap."
            }
        )
)
