module ElectronCore.ArcDeleteHelperTests

open Main.ARCtrlExtensions
open Main.ArcVault
open Main.Bindings.Path
open Main.IPC.Delete
open ARCtrl
open Vitest

let private expectSome (value: 'T option) (message: string) : 'T =
    match value with
    | Some value -> value
    | None -> failwith message

let private pathExistsAsync = TestHelpers.pathExistsAsync
let private loadArcAsync = TestHelpers.loadArcAsync
let private testWindow = TestHelpers.testWindow

let private withTempArc =
    TestHelpers.withTempArcWith "swate-delete-arc-file-" "DeleteArcFileArc"

let private deleteEntityOrFail arcPath relativePath arc = promise {
    match! ArcDeleteHelper.deleteArcEntityAsync arcPath relativePath arc with
    | Error error -> return failwith error.Message
    | Ok deletedArc -> return deletedArc
}

let private assertDeletedOnDisk arcPath relativeParts containsEntity = promise {
    let folderPath = join (Array.append [| arcPath |] relativeParts)
    let! folderExists = pathExistsAsync folderPath
    Vitest.expect(folderExists).toBe (false)

    let! reloadedArc = loadArcAsync arcPath
    Vitest.expect(containsEntity reloadedArc).toBe (false)
}

Vitest.describe (
    "ARC delete helper",
    fun () ->
        Vitest.test (
            "deletes an assay and removes its folder",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DeleteAssay")))
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        let! deletedArc = deleteEntityOrFail arcPath "assays/DeleteAssay" arc

                        Vitest.expect(deletedArc.ContainsAssay("DeleteAssay")).toBe (false)

                        do!
                            assertDeletedOnDisk
                                arcPath
                                [| "assays"; "DeleteAssay" |]
                                (fun arc -> arc.ContainsAssay("DeleteAssay"))
                    })
        )

        Vitest.test (
            "deletes a clean assay without marking the ARC dirty",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DeleteAssay")))
                    (fun arcPath -> promise {
                        let! loadedArc = loadArcAsync arcPath
                        baselineArcStaticHashes loadedArc
                        Vitest.expect(loadedArc.hasInMemoryChanges ()).toBe (false)

                        let! deletedArc = deleteEntityOrFail arcPath "assays/DeleteAssay" loadedArc

                        Vitest.expect(deletedArc.ContainsAssay("DeleteAssay")).toBe (false)
                        Vitest.expect(deletedArc.hasInMemoryChanges ()).toBe (false)

                        do!
                            assertDeletedOnDisk
                                arcPath
                                [| "assays"; "DeleteAssay" |]
                                (fun arc -> arc.ContainsAssay("DeleteAssay"))
                    })
        )

        Vitest.test (
            "deletes an entity when given the canonical workbook path",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DeleteAssay")))
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        let! deletedArc = deleteEntityOrFail arcPath "assays/DeleteAssay/isa.assay.xlsx" arc

                        Vitest.expect(deletedArc.ContainsAssay("DeleteAssay")).toBe (false)

                        do!
                            assertDeletedOnDisk
                                arcPath
                                [| "assays"; "DeleteAssay" |]
                                (fun arc -> arc.ContainsAssay("DeleteAssay"))
                    })
        )

        Vitest.test (
            "deletes a study and removes its folder",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddStudy(ArcStudy("DeleteStudy")))
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        let! deletedArc = deleteEntityOrFail arcPath "studies/DeleteStudy" arc

                        Vitest.expect(deletedArc.ContainsStudy("DeleteStudy")).toBe (false)

                        do!
                            assertDeletedOnDisk
                                arcPath
                                [| "studies"; "DeleteStudy" |]
                                (fun arc -> arc.ContainsStudy("DeleteStudy"))
                    })
        )

        Vitest.test (
            "deletes a run and removes its folder",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddRun(ArcRun("DeleteRun")))
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        let! deletedArc = deleteEntityOrFail arcPath "runs/DeleteRun" arc

                        Vitest.expect(deletedArc.ContainsRun("DeleteRun")).toBe (false)

                        do!
                            assertDeletedOnDisk
                                arcPath
                                [| "runs"; "DeleteRun" |]
                                (fun arc -> arc.ContainsRun("DeleteRun"))
                    })
        )

        Vitest.test (
            "deletes a workflow and removes its folder",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddWorkflow(ArcWorkflow("DeleteWorkflow")))
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath
                        let! deletedArc = deleteEntityOrFail arcPath "workflows/DeleteWorkflow" arc

                        Vitest.expect(deletedArc.ContainsWorkflow("DeleteWorkflow")).toBe (false)

                        do!
                            assertDeletedOnDisk
                                arcPath
                                [| "workflows"; "DeleteWorkflow" |]
                                (fun arc -> arc.ContainsWorkflow("DeleteWorkflow"))
                    })
        )

        Vitest.test (
            "rejects missing entity identifiers without mutating the in-memory ARC",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("KeepAssay")))
                    (fun arcPath -> promise {
                        let! arc = loadArcAsync arcPath

                        match! ArcDeleteHelper.deleteArcEntityAsync arcPath "assays/MissingAssay" arc with
                        | Ok _ -> failwith "Expected missing assay delete to fail."
                        | Error error ->
                            Vitest.expect(error.Message).toContain ("does not contain assay")
                            Vitest.expect(arc.ContainsAssay("KeepAssay")).toBe (true)
                    })
        )

        Vitest.test (
            "deletes through the scoped ARC path without persisting unrelated dirty in-memory edits",
            fun () ->
                withTempArc
                    (fun arc ->
                        arc.AddAssay(ArcAssay("DeleteAssay"))
                        arc.AddAssay(ArcAssay("KeepAssay", title = "Old title"))
                    )
                    (fun arcPath -> promise {
                        let! loadedArc = loadArcAsync arcPath
                        loadedArc.GetAssay("KeepAssay").Title <- Some "Unsaved local title"

                        let vault = ArcVault(testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc
                        vault.RefreshHasUnsavedArcChangesFlag()

                        match! ArcDeleteHelper.deleteArcEntityAsync arcPath "assays/DeleteAssay" loadedArc with
                        | Error deleteError -> failwith deleteError.Message
                        | Ok deletedArc ->
                            vault.SetArc deletedArc
                            vault.RefreshHasUnsavedArcChangesFlag()

                            let! reloadedAfterDelete = loadArcAsync arcPath
                            Vitest.expect(reloadedAfterDelete.ContainsAssay("DeleteAssay")).toBe (false)
                            Vitest.expect(reloadedAfterDelete.GetAssay("KeepAssay").Title).toEqual (Some "Old title")

                            let inMemoryArc = vault.arc |> expectSome <| "Expected vault ARC."
                            Vitest.expect(inMemoryArc.ContainsAssay("DeleteAssay")).toBe (false)
                            Vitest.expect(inMemoryArc.GetAssay("KeepAssay").Title).toEqual (Some "Unsaved local title")
                            Vitest.expect(vault.hasUnsavedArcChanges).toBe (true)
                    })
        )
)
