module ElectronCore.IpcArchitectureReviewTests

open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Path
open Main.ArcVault
open Main.IPC.ArcVaultsApi
open Main.ArcMerge
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open ARCtrl
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"

let private readUtf8FileAsync (path: string) : JS.Promise<string> =
    fsPromisesDynamic?readFile (path, "utf8") |> unbox<JS.Promise<string>>

let private sourcePath (segments: string[]) =
    join (Array.concat [| [| ".."; ".."; "src"; "Electron"; "src" |]; segments |])

let private countOccurrences (needle: string) (sourceText: string) =
    Regex.Matches(sourceText, Regex.Escape needle).Count

let private expectSourceContains (sourceText: string) (snippet: string) =
    Vitest.expect(sourceText.Contains(snippet), $"Expected source to contain: {snippet}").toBe(true)

let private expectSourceNotContains (sourceText: string) (snippet: string) =
    Vitest.expect(sourceText.Contains(snippet), $"Expected source not to contain: {snippet}").toBe(false)

let private expectSourceContainsInOrder (sourceText: string) (snippets: string[]) =
    let mutable searchStartIndex = 0

    for snippet in snippets do
        let snippetIndex = sourceText.IndexOf(snippet, searchStartIndex)
        Vitest.expect(snippetIndex >= 0, $"Expected source to contain (in order): {snippet}").toBe(true)
        searchStartIndex <- snippetIndex + snippet.Length

let private loadArcAsync = TestHelpers.loadArcAsync
let private testWindow = TestHelpers.testWindow
let private withTempArc = TestHelpers.withTempArcWith "swate-ipc-rename-sync-" "RenameSyncArc"

let private renamePathAsync sourceAbsolutePath targetAbsolutePath : JS.Promise<unit> = promise {
    let! _ = fsPromisesDynamic?rename (sourceAbsolutePath, targetAbsolutePath) |> unbox<JS.Promise<obj>>
    return ()
}

let private watcherEvent arcPath eventName relativePath : ArcVaultFileSystemEvent =
    {
        EventName = eventName
        RelativePath = relativePath
        AbsolutePath = join [| arcPath; relativePath |]
    }

let private assertEntityFolderRenameSyncOnLoadedArc
    (zoneFolder: string)
    (oldIdentifier: string)
    (newIdentifier: string)
    (seedArc: ARC -> unit)
    (assertReloadedArc: ARC -> unit)
    : JS.Promise<unit> =
    withTempArc seedArc (fun arcPath -> promise {
        let sourceRelativePath = $"{zoneFolder}/{oldIdentifier}"
        let targetRelativePath = $"{zoneFolder}/{newIdentifier}"
        let sourceAbsolutePath = join [| arcPath; zoneFolder; oldIdentifier |]
        let targetAbsolutePath = join [| arcPath; zoneFolder; newIdentifier |]

        do! renamePathAsync sourceAbsolutePath targetAbsolutePath

        let renamePlan =
            match
                ArcRenameHelper.tryBuildRenamePlan {
                    relativePath = sourceRelativePath
                    newName = newIdentifier
                }
            with
            | Error planError -> failwith planError.Message
            | Ok renamePlan ->
                Vitest.expect(renamePlan.TargetPath).toBe(targetRelativePath)
                renamePlan

        let! reloadedBeforeSync = loadArcAsync arcPath

        match! ArcRenameHelper.syncRenamedEntityIdentifierOnLoadedArc arcPath renamePlan reloadedBeforeSync with
        | Error syncError -> return failwith syncError.Message
        | Ok syncedArc ->
            assertReloadedArc syncedArc

            let! reloadedAfterSync = loadArcAsync arcPath
            assertReloadedArc reloadedAfterSync
            return ()
    })

Vitest.describe("IPC architecture review fixes", fun () ->
    Vitest.test("Arc vault dialogs consistently use a centralized IPC dialog parent helper", fun () ->
        promise {
            let! ipcHelperSource = sourcePath [| "Main"; "IPC"; "IPCHelper.fs" |] |> readUtf8FileAsync
            let! arcVaultApiSource = sourcePath [| "Main"; "IPC"; "IArcVaultsApi.fs" |] |> readUtf8FileAsync

            expectSourceContains ipcHelperSource "let dialogParentFromIpcEvent"
            expectSourceNotContains arcVaultApiSource "unbox<BaseWindow>"

            let dialogCalls = countOccurrences "dialog.showOpenDialog" arcVaultApiSource
            let parentedDialogCalls = countOccurrences "?window = window" arcVaultApiSource

            Vitest.expect(parentedDialogCalls).toBe(dialogCalls)
        })

    Vitest.test("AppStateContext exposes only the current ARC root path", fun () ->
        promise {
            let! appStateContextSource =
                sourcePath [| "Renderer"; "Context"; "AppStateContext.fs" |] |> readUtf8FileAsync

            let! appSource = sourcePath [| "Renderer"; "App.fs" |] |> readUtf8FileAsync

            expectSourceContains appStateContextSource "React.createContext<ArcRootPath>"
            expectSourceNotContains appStateContextSource "MainSyncedState<ArcRootPath>"
            expectSourceContains appSource "Context.AppStateContext.AppStateCtx.Provider("
            expectSourceContains appSource "model.ArcRootPath,"
        })

    Vitest.test("Git LFS remoting helper is internal to Main and does not expose unused raw channels", fun () ->
        promise {
            let! ipcTypesSource = sourcePath [| "Swate.Electron.Shared"; "IPCTypes.fs" |] |> readUtf8FileAsync
            let! gitLfsSource = sourcePath [| "Main"; "IPC"; "GitLfs.fs" |] |> readUtf8FileAsync
            let! preloadSource = sourcePath [| "Preload"; "preload.fs" |] |> readUtf8FileAsync

            expectSourceNotContains ipcTypesSource "type IGitLfsApi"
            expectSourceNotContains gitLfsSource "GitLfsRunChannel"
            expectSourceNotContains gitLfsSource "GitLfsCancelChannel"
            expectSourceNotContains gitLfsSource "GitLfsProgressChannel"
            expectSourceNotContains gitLfsSource "event.sender.send"
            expectSourceContains ipcTypesSource "type IGitLfsProgressRendererApi"
            expectSourceContains preloadSource "Remoting.buildBridge<IGitLfsProgressRendererApi>"
            expectSourceContains gitLfsSource "Remoting.buildProxySender<IGitLfsProgressRendererApi>"
        })

    Vitest.test("Arc vault IPC contract and implementation expose watcher-driven filetree mutations", fun () ->
        promise {
            let! ipcTypesSource = sourcePath [| "Swate.Electron.Shared"; "IPCTypes.fs" |] |> readUtf8FileAsync
            let! arcVaultApiSource = sourcePath [| "Main"; "IPC"; "IArcVaultsApi.fs" |] |> readUtf8FileAsync
            let! arcVaultSource = sourcePath [| "Main"; "ArcVault"; "ArcVault.fs" |] |> readUtf8FileAsync

            expectSourceContains ipcTypesSource "addArcFile: FileContentDTO -> JS.Promise<Result<unit, exn>>"
            expectSourceContains ipcTypesSource "createFileSystemItem: CreateFileSystemItemRequest -> JS.Promise<Result<string, exn>>"
            expectSourceContains ipcTypesSource "deletePath: string -> JS.Promise<Result<unit, exn>>"
            expectSourceContains ipcTypesSource "renamePath: RenamePathRequest -> JS.Promise<Result<unit, exn>>"
            expectSourceContains arcVaultApiSource "addArcFile ="
            expectSourceContains arcVaultApiSource "createFileSystemItem ="
            expectSourceContains arcVaultApiSource "deletePath ="
            expectSourceContains arcVaultApiSource "renamePath ="
            expectSourceContains arcVaultApiSource "ArcDeletePathRules.isDeletePathAllowed"
            expectSourceContains arcVaultApiSource "ArcDeleteHelper.mergeReloadedArcAfterDelete"
            expectSourceContains arcVaultApiSource "ArcRenameHelper.mergeReloadedArcAfterRename"
            expectSourceContains arcVaultApiSource "vault.RegisterPendingArcFileTreeMutation"
            expectSourceContains arcVaultSource "member this.ApplyFileWatcherEvents"
            expectSourceContains arcVaultSource "ARC.merge arcLocal reloadedArc events"
            expectSourceContains arcVaultSource "pendingArcFileTreeMutation"
            expectSourceContainsInOrder
                arcVaultApiSource
                [|
                    "addArcFile ="
                    "vault.AddArcFile request"
                    "createFileSystemItem ="
                    "ArcFileSystemHelper.createFileSystemItemOnDisk"
                    "deletePath ="
                    "vault.RegisterPendingArcFileTreeMutation"
                    "fsPromisesDynamic?rm"
                |]
            expectSourceContainsInOrder
                arcVaultApiSource
                [|
                    "renamePath ="
                    "vault.RegisterPendingArcFileTreeMutation"
                    "renameWithRetriesAsync"
                |]
        })

    Vitest.test("watcher ARC merge handles add, change, and unlink events while preserving dirty local state", fun () ->
        withTempArc (fun arc -> arc.AddAssay(ArcAssay("ExistingAssay", title = "Initial title"))) (fun arcPath -> promise {
            let! loadedArc = loadArcAsync arcPath
            loadedArc.Title <- Some "Unsaved local investigation title"

            let vault = ArcVault(testWindow ())
            vault.path <- Some arcPath
            vault.SetArc loadedArc
            vault.SetHasUnsavedArcChanges true

            let! diskArcForAdd = loadArcAsync arcPath
            diskArcForAdd.AddAssay(ArcAssay("DiskAssay", title = "Added on disk"))
            do! diskArcForAdd.UpdateAsync arcPath

            do!
                vault.ApplyFileWatcherEvents [
                    watcherEvent arcPath "add" "assays/DiskAssay/isa.assay.xlsx"
                ]

            let afterAdd = vault.arc.Value
            Vitest.expect(afterAdd.ContainsAssay("DiskAssay")).toBe(true)
            Vitest.expect(afterAdd.Title).toEqual(Some "Unsaved local investigation title")

            let! diskArcForChange = loadArcAsync arcPath
            diskArcForChange.GetAssay("DiskAssay").Title <- Some "Changed on disk"
            do! diskArcForChange.UpdateAsync arcPath

            do!
                vault.ApplyFileWatcherEvents [
                    watcherEvent arcPath "change" "assays/DiskAssay/isa.assay.xlsx"
                ]

            let afterChange = vault.arc.Value
            Vitest.expect(afterChange.GetAssay("DiskAssay").Title).toEqual(Some "Changed on disk")
            Vitest.expect(afterChange.Title).toEqual(Some "Unsaved local investigation title")

            let! diskArcForDelete = loadArcAsync arcPath
            diskArcForDelete.RemoveAssay("ExistingAssay")
            do! diskArcForDelete.UpdateAsync arcPath

            do!
                vault.ApplyFileWatcherEvents [
                    watcherEvent arcPath "unlink" "assays/ExistingAssay/isa.assay.xlsx"
                ]

            let afterDelete = vault.arc.Value
            Vitest.expect(afterDelete.ContainsAssay("ExistingAssay")).toBe(false)
            Vitest.expect(afterDelete.Title).toEqual(Some "Unsaved local investigation title")
            Vitest.expect(vault.hasUnsavedArcChanges).toBe(true)
        }))

    Vitest.test("pending watcher rename mutation syncs identifiers and preserves dirty local entity state", fun () ->
        withTempArc
            (fun arc ->
                arc.InitStudy("StudyA") |> ignore
                arc.InitAssay("OldAssay") |> ignore
                arc.RegisterAssay("StudyA", "OldAssay"))
            (fun arcPath -> promise {
                let! loadedArc = loadArcAsync arcPath
                loadedArc.GetAssay("OldAssay").Title <- Some "Unsaved assay title"

                let vault = ArcVault(testWindow ())
                vault.path <- Some arcPath
                vault.SetArc loadedArc
                vault.SetHasUnsavedArcChanges true

                let sourceAbsolutePath = join [| arcPath; "assays"; "OldAssay" |]
                let targetAbsolutePath = join [| arcPath; "assays"; "NewAssay" |]
                do! renamePathAsync sourceAbsolutePath targetAbsolutePath

                let renamePlan =
                    match
                        ArcRenameHelper.tryBuildRenamePlan {
                            relativePath = "assays/OldAssay"
                            newName = "NewAssay"
                        }
                    with
                    | Error error -> failwith error.Message
                    | Ok renamePlan -> renamePlan

                vault.RegisterPendingArcFileTreeMutation {
                    Description = "rename test"
                    MergeReloadedArc =
                        fun arcLocal reloadedArc _ -> promise {
                            match!
                                ArcRenameHelper.syncRenamedEntityIdentifierOnLoadedArc
                                    arcPath
                                    renamePlan
                                    reloadedArc
                            with
                            | Error syncError -> return Error syncError
                            | Ok syncedArc ->
                                return
                                    ArcRenameHelper.mergeReloadedArcAfterRename
                                        renamePlan
                                        arcLocal
                                        syncedArc
                        }
                }

                do!
                    vault.ApplyFileWatcherEvents [
                        watcherEvent arcPath "unlink" "assays/OldAssay/isa.assay.xlsx"
                        watcherEvent arcPath "add" "assays/NewAssay/isa.assay.xlsx"
                    ]

                let mergedArc = vault.arc.Value
                Vitest.expect(mergedArc.ContainsAssay("OldAssay")).toBe(false)
                Vitest.expect(mergedArc.ContainsAssay("NewAssay")).toBe(true)
                Vitest.expect(mergedArc.GetAssay("NewAssay").Title).toEqual(Some "Unsaved assay title")

                let study = mergedArc.GetStudy("StudyA")
                Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "OldAssay").toBe(false)
                Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "NewAssay").toBe(true)
            }))

    Vitest.test("Git LFS storage management is exposed through IGitApi only", fun () ->
        promise {
            let! ipcTypesSource = sourcePath [| "Swate.Electron.Shared"; "IPCTypes.fs" |] |> readUtf8FileAsync

            expectSourceContainsInOrder
                ipcTypesSource
                [|
                    "setGitLfsSettings: GitLfsSettingsDto -> JS.Promise<Result<GitOperationResult, exn>>"
                    "gitLfsPrune: unit -> JS.Promise<Result<GitOperationResult, exn>>"
                    "gitLfsDedup: unit -> JS.Promise<Result<GitOperationResult, exn>>"
                    "gitLfsFreeLocalCopy: GitLfsFreeLocalCopyRequest -> JS.Promise<Result<GitOperationResult, exn>>"
                |]
        })
)

Vitest.describe("ArcDeleteHelper merge and validation", fun () ->
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

    Vitest.test("mergeReloadedArcAfterDelete preserves unrelated local in-memory entities", fun () ->
        let localArc = ARC("MergeArc")
        localArc.Title <- Some "Dirty local title"
        localArc.InitAssay("AssayMergeA") |> ignore

        let reloadedArc = ARC("MergeArc")

        let result =
            ArcDeleteHelper.mergeReloadedArcAfterDelete
                "studies/StudyA/isa.study.xlsx"
                [ "studies/StudyA/isa.study.xlsx" ]
                localArc
                reloadedArc

        match result with
        | Error error -> failwith error.Message
        | Ok mergedArc ->
            Vitest.expect(mergedArc.TryGetAssay "AssayMergeA" |> Option.isSome).toBe(true)
    )

    Vitest.test("entity unlink event removes the targeted entity", fun () ->
        let localArc = ARC("MergeArc")
        localArc.Title <- Some "Dirty local title"
        localArc.InitAssay("My Assay") |> ignore

        let reloadedArc = localArc.Copy()
        let result =
            ArcDeleteHelper.mergeReloadedArcAfterDelete
                "assays/My Assay/isa.assay.xlsx"
                [ "assays/My Assay/isa.assay.xlsx" ]
                localArc
                reloadedArc

        match result with
        | Error error -> failwith error.Message
        | Ok mergedArc ->
            Vitest.expect(mergedArc.ContainsAssay("My Assay")).toBe(false)
    )

    Vitest.test("datamap unlink clears DataMap but keeps entity fields", fun () ->
        let localArc = ARC("MergeArc")
        localArc.Title <- Some "Dirty local title"
        localArc.InitAssay("My Assay") |> ignore
        localArc.Assays.[0].Title <- Some "Local assay title"
        localArc.Assays.[0].DataMap <- Some(DataMap.init())

        let reloadedArc = localArc.Copy()
        reloadedArc.Assays.[0].Title <- Some "Remote assay title"

        let result =
            ArcDeleteHelper.mergeReloadedArcAfterDelete
                "assays/My Assay/isa.datamap.xlsx"
                [ "assays/My Assay/isa.datamap.xlsx" ]
                localArc
                reloadedArc

        match result with
        | Error error -> failwith error.Message
        | Ok mergedArc ->
            Vitest.expect(mergedArc.Assays.[0].Title).toEqual(Some "Local assay title")
            Vitest.expect(mergedArc.Assays.[0].DataMap).toEqual(None)

    )

    Vitest.test("directory delete fallback synthesizes canonical unlink events", fun () ->
        let localArc = ARC("MergeArc")
        localArc.Title <- Some "Dirty local title"
        localArc.InitAssay("My Assay") |> ignore

        let reloadedArc = localArc.Copy()

        let result =
            ArcDeleteHelper.mergeReloadedArcAfterDelete "assays/My Assay" [] localArc reloadedArc

        match result with
        | Error error -> failwith error.Message
        | Ok mergedArc ->
            Vitest.expect(mergedArc.ContainsAssay("My Assay")).toBe(false)
    )

    Vitest.test("mergeReloadedArcAfterDelete preserves hash baseline so unaffected entities do not get rewritten", fun () ->
        let localArc = ARC("MergeArc")
        localArc.InitAssay("AssayKeep") |> ignore
        localArc.InitAssay("AssayDelete") |> ignore
        localArc.GetWriteContracts() |> ignore

        let reloadedArc = localArc.Copy()
        reloadedArc.RemoveAssay("AssayDelete")

        let result =
            ArcDeleteHelper.mergeReloadedArcAfterDelete
                "assays/AssayDelete/isa.assay.xlsx"
                [ "assays/AssayDelete/isa.assay.xlsx" ]
                localArc
                reloadedArc

        match result with
        | Error error -> failwith error.Message
        | Ok mergedArc ->
            Vitest.expect(mergedArc.ContainsAssay("AssayDelete")).toBe(false)
            Vitest.expect(mergedArc.ContainsAssay("AssayKeep")).toBe(true)

            let followUpContracts = mergedArc.GetUpdateContracts()
            Vitest.expect(followUpContracts.Length).toBe(0)
    )

    Vitest.test("unlink event path dedupe is idempotent", fun () ->
        let events =
            ArcDeleteHelper.buildDeleteUnlinkEvents
                "assays/My Assay/isa.assay.xlsx"
                [ "assays/My Assay/isa.assay.xlsx"; "assays/My Assay/isa.assay.xlsx" ]

        Vitest.expect(events.Length).toBe(1)
        Vitest.expect(events.[0].EventName).toEqual(EventName.Unlink)
        Vitest.expect(events.[0].Path).toBe("assays/My Assay/isa.assay.xlsx")
    )

    Vitest.test("delete fallback unlink paths are sourced from shared rules", fun () ->
        let sharedFallbackPaths =
            ArcDeletePathRules.buildFallbackUnlinkPaths "workflows/MyWorkflow"

        let helperFallbackPaths =
            ArcDeleteHelper.buildDeleteUnlinkEvents "workflows/MyWorkflow" []
            |> List.map _.Path

        Vitest.expect(helperFallbackPaths).toEqual(sharedFallbackPaths)
    )

    Vitest.test("syncRenamedEntityIdentifierOnLoadedArc updates assay identifier after assay folder rename", fun () ->
        assertEntityFolderRenameSyncOnLoadedArc
            "assays"
            "OldAssay"
            "NewAssay"
            (fun arc -> arc.InitAssay("OldAssay") |> ignore)
            (fun reloadedArc ->
                Vitest.expect(reloadedArc.ContainsAssay("OldAssay")).toBe(false)
                Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe(true))
    )

    Vitest.test("syncRenamedEntityIdentifierOnLoadedArc updates study identifier after study folder rename", fun () ->
        assertEntityFolderRenameSyncOnLoadedArc
            "studies"
            "OldStudy"
            "NewStudy"
            (fun arc -> arc.InitStudy("OldStudy") |> ignore)
            (fun reloadedArc ->
                Vitest.expect(reloadedArc.ContainsStudy("OldStudy")).toBe(false)
                Vitest.expect(reloadedArc.ContainsStudy("NewStudy")).toBe(true))
    )

    Vitest.test("syncRenamedEntityIdentifierOnLoadedArc updates run identifier after run folder rename", fun () ->
        assertEntityFolderRenameSyncOnLoadedArc
            "runs"
            "OldRun"
            "NewRun"
            (fun arc -> arc.InitRun("OldRun") |> ignore)
            (fun reloadedArc ->
                Vitest.expect(reloadedArc.ContainsRun("OldRun")).toBe(false)
                Vitest.expect(reloadedArc.ContainsRun("NewRun")).toBe(true))
    )

    Vitest.test("syncRenamedEntityIdentifierOnLoadedArc updates workflow identifier after workflow folder rename", fun () ->
        assertEntityFolderRenameSyncOnLoadedArc
            "workflows"
            "OldWorkflow"
            "NewWorkflow"
            (fun arc -> arc.InitWorkflow("OldWorkflow") |> ignore)
            (fun reloadedArc ->
                Vitest.expect(reloadedArc.ContainsWorkflow("OldWorkflow")).toBe(false)
                Vitest.expect(reloadedArc.ContainsWorkflow("NewWorkflow")).toBe(true))
    )

    Vitest.test("syncRenamedEntityIdentifierOnLoadedArc updates study assay registrations when an assay is renamed", fun () ->
        assertEntityFolderRenameSyncOnLoadedArc
            "assays"
            "OldAssay"
            "NewAssay"
            (fun arc ->
                arc.InitStudy("StudyA") |> ignore
                arc.InitAssay("OldAssay") |> ignore
                arc.RegisterAssay("StudyA", "OldAssay"))
            (fun reloadedArc ->
                let study = reloadedArc.GetStudy("StudyA")
                Vitest.expect(reloadedArc.ContainsAssay("OldAssay")).toBe(false)
                Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe(true)
                Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "OldAssay").toBe(false)
                Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "NewAssay").toBe(true))
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

    Vitest.test("mergeReloadedArcAfterRename preserves dirty local assay state under the new identifier", fun () ->
        let localArc = ARC("MergeArc")
        localArc.InitStudy("StudyA") |> ignore
        localArc.InitAssay("OldAssay") |> ignore
        localArc.RegisterAssay("StudyA", "OldAssay")
        localArc.Assays.[0].Title <- Some "Unsaved assay title"
        localArc.Assays.[0].DataMap <- Some(DataMap.init ())

        let reloadedArc = ARC("MergeArc")
        reloadedArc.InitStudy("StudyA") |> ignore
        reloadedArc.InitAssay("NewAssay") |> ignore
        reloadedArc.Assays.[0].Title <- Some "Disk assay title"
        reloadedArc.RegisterAssay("StudyA", "NewAssay")

        let renamePlan =
            match
                ArcRenameHelper.tryBuildRenamePlan {
                    relativePath = "assays/OldAssay"
                    newName = "NewAssay"
                }
            with
            | Error error -> failwith error.Message
            | Ok renamePlan -> renamePlan

        match ArcRenameHelper.mergeReloadedArcAfterRename renamePlan localArc reloadedArc with
        | Error error -> failwith error.Message
        | Ok mergedArc ->
            Vitest.expect(mergedArc.ContainsAssay("OldAssay")).toBe(false)
            Vitest.expect(mergedArc.ContainsAssay("NewAssay")).toBe(true)

            let renamedAssay = mergedArc.GetAssay("NewAssay")
            Vitest.expect(renamedAssay.Title).toEqual(Some "Unsaved assay title")
            Vitest.expect(renamedAssay.DataMap.IsSome).toBe(true)

            let study = mergedArc.GetStudy("StudyA")
            Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "OldAssay").toBe(false)
            Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "NewAssay").toBe(true)
    )
)
