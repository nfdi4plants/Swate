module ElectronCore.IpcArchitectureReviewTests

open Fable.Core
open Main.Bindings.Path
open Main.ArcVault
open Main.IPC.ArcVaultsApi
open Main.ArcMerge
open Swate.Components.Shared
open ARCtrl
open Vitest

let private loadArcAsync = TestHelpers.loadArcAsync
let private testWindow = TestHelpers.testWindow
let private withTempArc = TestHelpers.withTempArcWith "swate-ipc-rename-sync-" "RenameSyncArc"

let private watcherEvent arcPath eventName relativePath : ArcVaultFileSystemEvent =
    {
        EventName = eventName
        RelativePath = relativePath
        AbsolutePath = join [| arcPath; relativePath |]
    }

let private renamePlanOrFail relativePath newName =
    match
        ArcRenameHelper.tryBuildRenamePlan {
            relativePath = relativePath
            newName = newName
        }
    with
    | Error error -> failwith error.Message
    | Ok renamePlan -> renamePlan

let private assertArcCtrlEntityRename
    (sourceRelativePath: string)
    (newName: string)
    (seedArc: ARC -> unit)
    (mutateLoadedArc: ARC -> unit)
    (assertRenamedArc: ARC -> unit)
    : JS.Promise<unit> =
    withTempArc seedArc (fun arcPath -> promise {
        let! loadedArc = loadArcAsync arcPath
        mutateLoadedArc loadedArc

        let renamePlan = renamePlanOrFail sourceRelativePath newName

        match! ArcRenameHelper.renameArcEntityAsync arcPath renamePlan loadedArc with
        | Error renameError -> return failwith renameError.Message
        | Ok renamedArc ->
            assertRenamedArc renamedArc

            let! reloadedArc = loadArcAsync arcPath
            assertRenamedArc reloadedArc
            return ()
    })

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

    Vitest.test("ARCtrl rename updates assay folder, identifier, and study registrations", fun () ->
        assertArcCtrlEntityRename
            "assays/OldAssay"
            "NewAssay"
            (fun arc ->
                arc.InitStudy("StudyA") |> ignore
                arc.InitAssay("OldAssay") |> ignore
                arc.RegisterAssay("StudyA", "OldAssay"))
            (fun arc -> arc.GetAssay("OldAssay").Title <- Some "Renamed assay title")
            (fun arc ->
                Vitest.expect(arc.ContainsAssay("OldAssay")).toBe(false)
                Vitest.expect(arc.ContainsAssay("NewAssay")).toBe(true)
                Vitest.expect(arc.GetAssay("NewAssay").Title).toEqual(Some "Renamed assay title")

                let study = arc.GetStudy("StudyA")
                Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "OldAssay").toBe(false)
                Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "NewAssay").toBe(true))
    )

)

Vitest.describe("ARC delete and rename validation", fun () ->
    Vitest.test("ArcPathValidation.isWithinRootPath rejects out-of-root paths", fun () ->
        Vitest.expect(ArcPathValidation.isWithinRootPath "C:/arc" "C:/arc/assays/a.txt").toBe(true)
        Vitest.expect(ArcPathValidation.isWithinRootPath "C:/arc" "C:/other/place.txt").toBe(false)
    )

    Vitest.test("isDeletePathAllowed only permits add-zone descendants", fun () ->
        Vitest.expect(ArcDeletePathRules.isDeletePathAllowed "studies/StudyA/isa.study.xlsx").toBe(true)
        Vitest.expect(ArcDeletePathRules.isDeletePathAllowed "studies").toBe(false)
        Vitest.expect(ArcDeletePathRules.isDeletePathAllowed "README.md").toBe(false)
        Vitest.expect(ArcDeletePathRules.isDeletePathAllowed "../studies/StudyA/isa.study.xlsx").toBe(false)
    )

    Vitest.test("classifyDeleteTarget identifies entity folder delete paths", fun () ->
        match ArcDeletePathRules.classifyDeleteTarget "assays/OldAssay" with
        | ArcDeletePathRules.DeletePathClassification.EntityFolderTarget(zone, identifier, normalizedPath) ->
            Vitest.expect(zone).toEqual(ArcDeletePathRules.AddZone.Assays)
            Vitest.expect(identifier).toBe("OldAssay")
            Vitest.expect(normalizedPath).toBe("assays/OldAssay")
        | other -> failwith $"Expected entity folder target, got {other}."
    )

    Vitest.test("classifyDeleteTarget identifies canonical entity files", fun () ->
        match ArcDeletePathRules.classifyDeleteTarget "workflows/MyWorkflow/isa.workflow.xlsx" with
        | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
            ArcDeletePathRules.CanonicalArcFileTarget.EntityFile(zone, identifier),
            normalizedPath
          ) ->
            Vitest.expect(zone).toEqual(ArcDeletePathRules.AddZone.Workflows)
            Vitest.expect(identifier).toBe("MyWorkflow")
            Vitest.expect(normalizedPath).toBe("workflows/MyWorkflow/isa.workflow.xlsx")
        | other -> failwith $"Expected canonical entity file target, got {other}."
    )

    Vitest.test("classifyDeleteTarget separates generic and datamap delete paths from entity deletes", fun () ->
        match ArcDeletePathRules.classifyDeleteTarget "assays/My Assay/notes/info.md" with
        | ArcDeletePathRules.DeletePathClassification.AddZoneDescendantTarget(zone, normalizedPath) ->
            Vitest.expect(zone).toEqual(ArcDeletePathRules.AddZone.Assays)
            Vitest.expect(normalizedPath).toBe("assays/My Assay/notes/info.md")
        | other -> failwith $"Expected add-zone descendant target, got {other}."

        match ArcDeletePathRules.classifyDeleteTarget "assays/My Assay/isa.datamap.xlsx" with
        | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
            ArcDeletePathRules.CanonicalArcFileTarget.DataMapFile(zone, identifier),
            normalizedPath
          ) ->
            Vitest.expect(zone).toEqual(ArcDeletePathRules.AddZone.Assays)
            Vitest.expect(identifier).toBe("My Assay")
            Vitest.expect(normalizedPath).toBe("assays/My Assay/isa.datamap.xlsx")
        | other -> failwith $"Expected canonical datamap file target, got {other}."
    )

    Vitest.test("directory delete fallback paths remain available for watcher unlink-dir events", fun () ->
        let fallbackPaths =
            ArcDeletePathRules.buildFallbackUnlinkPaths "workflows/MyWorkflow"

        Vitest.expect(fallbackPaths).toEqual(
            [
                "workflows/MyWorkflow/isa.workflow.xlsx"
                "workflows/MyWorkflow/isa.datamap.xlsx"
            ]
        )
    )

    Vitest.test("tryBuildRenamePlan rejects non-entity rename paths", fun () ->
        let result =
            ArcRenameHelper.tryBuildRenamePlan {
                relativePath = "assays/StudyA/notes/info.md"
                newName = "renamed.md"
            }

        match result with
        | Ok _ -> failwith "Expected non-entity rename path classification to be rejected."
        | Error error ->
            Vitest.expect(error.Message.Contains "generic files or folders").toBe(true)
    )

    Vitest.test("tryBuildRenamePlan accepts entity-folder rename paths", fun () ->
        let result =
            ArcRenameHelper.tryBuildRenamePlan {
                relativePath = "assays/OldAssay"
                newName = "NewAssay"
            }

        match result with
        | Error error -> failwith error.Message
        | Ok plan ->
            Vitest.expect(plan.SourcePath).toBe("assays/OldAssay")
            Vitest.expect(plan.TargetPath).toBe("assays/NewAssay")
            Vitest.expect(plan.SyncPlan.Zone).toEqual(ArcDeletePathRules.AddZone.Assays)
            Vitest.expect(plan.SyncPlan.OldIdentifier).toBe("OldAssay")
            Vitest.expect(plan.SyncPlan.NewIdentifier).toBe("NewAssay")
    )

    Vitest.test("tryBuildRenamePlan rejects canonical ARC file rename paths", fun () ->
        let result =
            ArcRenameHelper.tryBuildRenamePlan {
                relativePath = "assays/OldAssay/isa.assay.xlsx"
                newName = "NewAssay"
            }

        match result with
        | Ok _ -> failwith "Expected canonical ARC file rename path to be rejected."
        | Error error ->
            Vitest.expect(error.Message.Contains "Rename the containing ARC entity folder instead").toBe(true)
    )

)
