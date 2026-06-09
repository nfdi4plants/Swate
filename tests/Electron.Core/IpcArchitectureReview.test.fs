module ElectronCore.IpcArchitectureReviewTests

open Fable.Core
open Main.Bindings.Path
open Main.ArcVault
open Main.ArcVaultTypes
open Main.IPC.FileSystemIO
open Main.IPC.Rename
open Main.ArcMerge
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open ARCtrl
open Vitest

let private loadArcAsync = TestHelpers.loadArcAsync
let private pathExistsAsync = TestHelpers.pathExistsAsync
let private testWindow = TestHelpers.testWindow
let private withTempArc = TestHelpers.withTempArcWith "swate-ipc-rename-sync-" "RenameSyncArc"

let private watcherEvent arcPath eventName relativePath : ArcVaultFileSystemEvent =
    {
        EventName = eventName
        RelativePath = relativePath
        AbsolutePath = join [| arcPath; relativePath |]
    }

let private renameRequest relativePath newName : RenamePathRequest = {
    relativePath = relativePath
    newName = newName
}

Vitest.describe("IPC architecture review fixes", fun () ->

    Vitest.test("watcher ARC merge handles add, change, and unlink events while preserving dirty local state", fun () ->
        withTempArc (fun arc -> arc.AddAssay(ArcAssay("ExistingAssay", title = "Initial title"))) (fun arcPath -> promise {
            let! loadedArc = loadArcAsync arcPath
            loadedArc.Title <- Some "Unsaved local investigation title"

            let vault = ArcVault(testWindow ())
            vault.path <- Some arcPath
            vault.SetArc loadedArc
            vault.RefreshHasUnsavedArcChangesFlag()

            let! diskArcForAdd = loadArcAsync arcPath
            diskArcForAdd.AddAssay(ArcAssay("DiskAssay", title = "Added on disk"))
            do! diskArcForAdd.UpdateAsync arcPath

            do!
                vault.TriggerArcInMemoryMergeOnFileWatcherEvents [
                    watcherEvent arcPath "add" "assays/DiskAssay/isa.assay.xlsx"
                ]

            let afterAdd = vault.arc.Value
            Vitest.expect(afterAdd.ContainsAssay("DiskAssay")).toBe(true)
            Vitest.expect(afterAdd.Title).toEqual(Some "Unsaved local investigation title")

            let! diskArcForChange = loadArcAsync arcPath
            diskArcForChange.GetAssay("DiskAssay").Title <- Some "Changed on disk"
            do! diskArcForChange.UpdateAsync arcPath

            do!
                vault.TriggerArcInMemoryMergeOnFileWatcherEvents [
                    watcherEvent arcPath "change" "assays/DiskAssay/isa.assay.xlsx"
                ]

            let afterChange = vault.arc.Value
            Vitest.expect(afterChange.GetAssay("DiskAssay").Title).toEqual(Some "Changed on disk")
            Vitest.expect(afterChange.Title).toEqual(Some "Unsaved local investigation title")

            let! diskArcForDelete = loadArcAsync arcPath
            diskArcForDelete.RemoveAssay("ExistingAssay")
            do! diskArcForDelete.UpdateAsync arcPath

            do!
                vault.TriggerArcInMemoryMergeOnFileWatcherEvents [
                    watcherEvent arcPath "unlink" "assays/ExistingAssay/isa.assay.xlsx"
                ]

            let afterDelete = vault.arc.Value
            Vitest.expect(afterDelete.ContainsAssay("ExistingAssay")).toBe(false)
            Vitest.expect(afterDelete.Title).toEqual(Some "Unsaved local investigation title")
            Vitest.expect(vault.hasUnsavedArcChanges).toBe(true)
        }))

    Vitest.test("ARCtrl try rename updates assay references while keeping dirty edits local-only", fun () ->
        withTempArc
            (fun arc ->
                arc.AddStudy(ArcStudy("StudyA"))
                arc.AddAssay(ArcAssay("OldAssay", title = "Persisted renamed assay title"))
                arc.AddAssay(ArcAssay("KeepAssay", title = "Persisted keep assay title"))
                arc.RegisterAssay("StudyA", "OldAssay"))
            (fun arcPath -> promise {
                let! loadedArc = loadArcAsync arcPath
                loadedArc.GetAssay("OldAssay").Title <- Some "Unsaved renamed assay title"
                loadedArc.GetAssay("KeepAssay").Title <- Some "Unsaved keep assay title"

                match!
                    ArcRenameHelper.renameArcEntityAsync
                        arcPath
                        (renameRequest "assays/OldAssay" "NewAssay")
                        loadedArc
                with
                | Error renameError -> return failwith renameError.Message
                | Ok renamedArc ->
                    Vitest.expect(renamedArc.ContainsAssay("OldAssay")).toBe(false)
                    Vitest.expect(renamedArc.ContainsAssay("NewAssay")).toBe(true)
                    Vitest.expect(renamedArc.GetAssay("NewAssay").Title).toEqual(Some "Unsaved renamed assay title")
                    Vitest.expect(renamedArc.GetAssay("KeepAssay").Title).toEqual(Some "Unsaved keep assay title")

                    let localStudy = renamedArc.GetStudy("StudyA")
                    Vitest.expect(localStudy.RegisteredAssayIdentifiers |> Seq.contains "OldAssay").toBe(false)
                    Vitest.expect(localStudy.RegisteredAssayIdentifiers |> Seq.contains "NewAssay").toBe(true)
                    Vitest.expect(renamedArc.hasInMemoryChanges()).toBe(true)

                    let! oldFolderExists = pathExistsAsync (join [| arcPath; "assays"; "OldAssay" |])
                    let! newFolderExists = pathExistsAsync (join [| arcPath; "assays"; "NewAssay" |])
                    Vitest.expect(oldFolderExists).toBe(false)
                    Vitest.expect(newFolderExists).toBe(true)

                    let! reloadedArc = loadArcAsync arcPath
                    Vitest.expect(reloadedArc.ContainsAssay("OldAssay")).toBe(false)
                    Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe(true)
                    Vitest.expect(reloadedArc.GetAssay("NewAssay").Title).toEqual(Some "Persisted renamed assay title")
                    Vitest.expect(reloadedArc.GetAssay("KeepAssay").Title).toEqual(Some "Persisted keep assay title")

                    let diskStudy = reloadedArc.GetStudy("StudyA")
                    Vitest.expect(diskStudy.RegisteredAssayIdentifiers |> Seq.contains "OldAssay").toBe(false)
                    Vitest.expect(diskStudy.RegisteredAssayIdentifiers |> Seq.contains "NewAssay").toBe(true)
            })
    )

    Vitest.test("ARCtrl try rename validates source against disk before mutating local ARC", fun () ->
        withTempArc
            (fun arc -> arc.AddAssay(ArcAssay("PersistedAssay")))
            (fun arcPath -> promise {
                let! loadedArc = loadArcAsync arcPath
                loadedArc.AddAssay(ArcAssay("LocalOnlyAssay"))

                match!
                    ArcRenameHelper.renameArcEntityAsync
                        arcPath
                        (renameRequest "assays/LocalOnlyAssay" "RenamedLocalOnlyAssay")
                        loadedArc
                with
                | Ok _ -> return failwith "Expected missing disk source rename to fail."
                | Error renameError ->
                    Vitest.expect(renameError.Message).toContain("does not contain assay")
                    Vitest.expect(loadedArc.ContainsAssay("LocalOnlyAssay")).toBe(true)
                    Vitest.expect(loadedArc.ContainsAssay("RenamedLocalOnlyAssay")).toBe(false)

                    let! reloadedArc = loadArcAsync arcPath
                    Vitest.expect(reloadedArc.ContainsAssay("LocalOnlyAssay")).toBe(false)
                    Vitest.expect(reloadedArc.ContainsAssay("PersistedAssay")).toBe(true)
            })
    )

    Vitest.test("ARCtrl try rename rejects existing targets without mutating local ARC", fun () ->
        withTempArc
            (fun arc ->
                arc.AddAssay(ArcAssay("OldAssay"))
                arc.AddAssay(ArcAssay("ExistingAssay")))
            (fun arcPath -> promise {
                let! loadedArc = loadArcAsync arcPath

                match!
                    ArcRenameHelper.renameArcEntityAsync
                        arcPath
                        (renameRequest "assays/OldAssay" "ExistingAssay")
                        loadedArc
                with
                | Ok _ -> return failwith "Expected conflicting rename target to fail."
                | Error renameError ->
                    Vitest.expect(renameError.Message).toContain("destination already exists")
                    Vitest.expect(renameError.Message).toContain("assays/OldAssay")
                    Vitest.expect(renameError.Message).toContain("assays/ExistingAssay")
                    Vitest.expect(loadedArc.ContainsAssay("OldAssay")).toBe(true)
                    Vitest.expect(loadedArc.ContainsAssay("ExistingAssay")).toBe(true)

                    let! reloadedArc = loadArcAsync arcPath
                    Vitest.expect(reloadedArc.ContainsAssay("OldAssay")).toBe(true)
                    Vitest.expect(reloadedArc.ContainsAssay("ExistingAssay")).toBe(true)
            })
    )

)

Vitest.describe("ARC delete and rename validation", fun () ->
    Vitest.test("ArcPathValidation.isWithinRootPath rejects out-of-root paths", fun () ->
        Vitest.expect(ArcPathValidation.isWithinRootPath "C:/arc" "C:/arc/assays/a.txt").toBe(true)
        Vitest.expect(ArcPathValidation.isWithinRootPath "C:/arc" "C:/other/place.txt").toBe(false)
    )

    Vitest.test("ArcPathValidation.isSafeRelativePathCandidate rejects absolute and traversal paths", fun () ->
        Vitest.expect(ArcPathValidation.isSafeRelativePathCandidate "assays/A/file.txt").toBe(true)
        Vitest.expect(ArcPathValidation.isSafeRelativePathCandidate "../outside.txt").toBe(false)
        Vitest.expect(ArcPathValidation.isSafeRelativePathCandidate "/outside.txt").toBe(false)
        Vitest.expect(ArcPathValidation.isSafeRelativePathCandidate "").toBe(false)
    )

    Vitest.test("isDeletePathAllowed permits safe non-ARC filesystem targets", fun () ->
        Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "studies/StudyA/isa.study.xlsx").toBe(true)
        Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "test.fsx").toBe(true)
        Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "studies").toBe(false)
        Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "README.md").toBe(false)
        Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "../studies/StudyA/isa.study.xlsx").toBe(false)
    )

    Vitest.test("classifyDeleteTarget identifies entity folder delete paths", fun () ->
        match ArcEntityPathRules.classifyDeleteTarget "assays/OldAssay" with
        | ArcEntityPathRules.DeletePathClassification.EntityFolderTarget(zone, identifier, normalizedPath) ->
            Vitest.expect(zone).toEqual(ArcEntityPathRules.AddZone.Assays)
            Vitest.expect(identifier).toBe("OldAssay")
            Vitest.expect(normalizedPath).toBe("assays/OldAssay")
        | other -> failwith $"Expected entity folder target, got {other}."
    )

    Vitest.test("classifyDeleteTarget identifies canonical entity files", fun () ->
        match ArcEntityPathRules.classifyDeleteTarget "workflows/MyWorkflow/isa.workflow.xlsx" with
        | ArcEntityPathRules.DeletePathClassification.CanonicalFileTarget(
            ArcEntityPathRules.CanonicalArcFileTarget.EntityFile(zone, identifier),
            normalizedPath
          ) ->
            Vitest.expect(zone).toEqual(ArcEntityPathRules.AddZone.Workflows)
            Vitest.expect(identifier).toBe("MyWorkflow")
            Vitest.expect(normalizedPath).toBe("workflows/MyWorkflow/isa.workflow.xlsx")
        | other -> failwith $"Expected canonical entity file target, got {other}."
    )

    Vitest.test("classifyDeleteTarget separates generic and datamap delete paths from entity deletes", fun () ->
        match ArcEntityPathRules.classifyDeleteTarget "assays/My Assay/notes/info.md" with
        | ArcEntityPathRules.DeletePathClassification.AddZoneDescendantTarget(zone, normalizedPath) ->
            Vitest.expect(zone).toEqual(ArcEntityPathRules.AddZone.Assays)
            Vitest.expect(normalizedPath).toBe("assays/My Assay/notes/info.md")
        | other -> failwith $"Expected add-zone descendant target, got {other}."

        match ArcEntityPathRules.classifyDeleteTarget "assays/My Assay/isa.datamap.xlsx" with
        | ArcEntityPathRules.DeletePathClassification.CanonicalFileTarget(
            ArcEntityPathRules.CanonicalArcFileTarget.DataMapFile(zone, identifier),
            normalizedPath
          ) ->
            Vitest.expect(zone).toEqual(ArcEntityPathRules.AddZone.Assays)
            Vitest.expect(identifier).toBe("My Assay")
            Vitest.expect(normalizedPath).toBe("assays/My Assay/isa.datamap.xlsx")
        | other -> failwith $"Expected canonical datamap file target, got {other}."
    )

    Vitest.test("directory delete fallback paths remain available for watcher unlink-dir events", fun () ->
        let fallbackPaths =
            ArcEntityPathRules.buildFallbackUnlinkPaths "workflows/MyWorkflow"

        Vitest.expect(fallbackPaths).toEqual(
            [
                "workflows/MyWorkflow/isa.workflow.xlsx"
                "workflows/MyWorkflow/isa.datamap.xlsx"
            ]
        )
    )

    Vitest.test("renameArcEntityAsync rejects non-entity rename paths without disk access", fun () -> promise {
        let arc = ARC("test-arc")

        match!
            ArcRenameHelper.renameArcEntityAsync
                "unused-arc-path"
                (renameRequest "assays/StudyA/notes/info.md" "renamed.md")
                arc
        with
        | Ok _ -> return failwith "Expected non-entity rename path classification to be rejected."
        | Error error -> Vitest.expect(error.Message.Contains "generic files or folders").toBe(true)
    })

    Vitest.test("renameArcEntityAsync rejects canonical ARC file rename paths without disk access", fun () -> promise {
        let arc = ARC("test-arc")

        match!
            ArcRenameHelper.renameArcEntityAsync
                "unused-arc-path"
                (renameRequest "assays/OldAssay/isa.assay.xlsx" "NewAssay")
                arc
        with
        | Ok _ -> return failwith "Expected canonical ARC file rename path to be rejected."
        | Error error ->
            Vitest.expect(error.Message.Contains "Rename the containing ARC entity folder instead").toBe(true)
    })

)