module ElectronCore.IpcArchitectureReviewTests

open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Path
open Main.IPC.ArcVaultsApi
open Main.ArcMerge
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open ARCtrl
open Vitest

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"

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

let private createTempDirectoryAsync () : JS.Promise<string> =
    let prefix =
        join [|
            osDynamic?tmpdir () |> unbox<string>
            "swate-ipc-rename-sync-"
        |]

    fsPromisesDynamic?mkdtemp (prefix) |> unbox<JS.Promise<string>>

let private removeDirectoryAsync (path: string) : JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm (path, createObj [ "recursive" ==> true; "force" ==> true ])
        |> unbox<JS.Promise<obj>>

    return ()
}

let private withTempArc (seedArc: ARC -> unit) (testBody: string -> JS.Promise<unit>) : JS.Promise<unit> = promise {
    let! rootPath = createTempDirectoryAsync ()
    let arcPath = join [| rootPath; "arc" |]

    try
        let arc = ARC("RenameSyncArc")
        seedArc arc
        do! arc.WriteAsync arcPath
        do! testBody arcPath
        do! removeDirectoryAsync rootPath
    with error ->
        do! removeDirectoryAsync rootPath
        return raise error
}

let private assertEntityFolderRenameSync
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

        let! _ = fsPromisesDynamic?rename (sourceAbsolutePath, targetAbsolutePath) |> unbox<JS.Promise<obj>>

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

        match! ArcRenameHelper.syncRenamedEntityIdentifierOnDisk arcPath renamePlan with
        | Error syncError -> return failwith syncError.Message
        | Ok() ->
            match! ARC.tryLoadAsync arcPath with
            | Error loadError -> return failwith $"Expected ARC reload to succeed after rename sync: {loadError}"
            | Ok reloadedArc ->
                assertReloadedArc reloadedArc
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

    Vitest.test("Arc vault IPC contract and implementation expose deletePath", fun () ->
        promise {
            let! ipcTypesSource = sourcePath [| "Swate.Electron.Shared"; "IPCTypes.fs" |] |> readUtf8FileAsync
            let! arcVaultApiSource = sourcePath [| "Main"; "IPC"; "IArcVaultsApi.fs" |] |> readUtf8FileAsync

            expectSourceContains ipcTypesSource "deletePath: string -> JS.Promise<Result<unit, exn>>"
            expectSourceContains arcVaultApiSource "deletePath ="
            expectSourceContains arcVaultApiSource "ArcDeletePathRules.isDeletePathAllowed"
            expectSourceContains arcVaultApiSource "do! vault.RefreshFileTree()"
            expectSourceContains arcVaultApiSource "runArcDiskMutation"
            expectSourceContains arcVaultApiSource "ArcDeleteHelper.mergeReloadedArcAfterDelete"
            expectSourceContains arcVaultApiSource "ARC.merge"
            expectSourceContains arcVaultApiSource "ArcDeletePathRules.buildFallbackUnlinkPaths"
            expectSourceContainsInOrder
                arcVaultApiSource
                [|
                    "fsPromisesDynamic?rm"
                    "runArcDiskMutation"
                    "ArcDeleteHelper.mergeReloadedArcAfterDelete"
                |]
            expectSourceContainsInOrder
                arcVaultApiSource
                [|
                    "let private runArcDiskMutation"
                    "do! vault.RefreshFileTree()"
                    "match! postReloadArcMutation reloadedArc"
                    "match mergeReloadedArc postMutationArc"
                |]
        })

    Vitest.test("Arc vault IPC contract and implementation expose renamePath", fun () ->
        promise {
            let! ipcTypesSource = sourcePath [| "Swate.Electron.Shared"; "IPCTypes.fs" |] |> readUtf8FileAsync
            let! fileIoTypesSource = sourcePath [| "Swate.Electron.Shared"; "FileIOTypes.fs" |] |> readUtf8FileAsync

            expectSourceContains fileIoTypesSource "type RenamePathRequest = {"
            expectSourceContains fileIoTypesSource "relativePath: string"
            expectSourceContains fileIoTypesSource "newName: string"
            expectSourceContains ipcTypesSource "renamePath: RenamePathRequest -> JS.Promise<Result<unit, exn>>"
        })

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
        | Ok mergeResult ->
            Vitest.expect(mergeResult.Arc.TryGetAssay "AssayMergeA" |> Option.isSome).toBe(true)
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
        | Ok mergeResult ->
            Vitest.expect(mergeResult.Arc.ContainsAssay("My Assay")).toBe(false)
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
        | Ok mergeResult ->
            Vitest.expect(mergeResult.Arc.Assays.[0].Title).toEqual(Some "Local assay title")
            Vitest.expect(mergeResult.Arc.Assays.[0].DataMap).toEqual(None)

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
        | Ok mergeResult ->
            Vitest.expect(mergeResult.Arc.ContainsAssay("My Assay")).toBe(false)
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
        | Ok mergeResult ->
            Vitest.expect(mergeResult.Arc.ContainsAssay("AssayDelete")).toBe(false)
            Vitest.expect(mergeResult.Arc.ContainsAssay("AssayKeep")).toBe(true)

            let followUpContracts = mergeResult.Arc.GetUpdateContracts()
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

    Vitest.test("syncRenamedEntityIdentifierOnDisk updates assay identifier after assay folder rename", fun () ->
        assertEntityFolderRenameSync
            "assays"
            "OldAssay"
            "NewAssay"
            (fun arc -> arc.InitAssay("OldAssay") |> ignore)
            (fun reloadedArc ->
                Vitest.expect(reloadedArc.ContainsAssay("OldAssay")).toBe(false)
                Vitest.expect(reloadedArc.ContainsAssay("NewAssay")).toBe(true))
    )

    Vitest.test("syncRenamedEntityIdentifierOnDisk updates study identifier after study folder rename", fun () ->
        assertEntityFolderRenameSync
            "studies"
            "OldStudy"
            "NewStudy"
            (fun arc -> arc.InitStudy("OldStudy") |> ignore)
            (fun reloadedArc ->
                Vitest.expect(reloadedArc.ContainsStudy("OldStudy")).toBe(false)
                Vitest.expect(reloadedArc.ContainsStudy("NewStudy")).toBe(true))
    )

    Vitest.test("syncRenamedEntityIdentifierOnDisk updates run identifier after run folder rename", fun () ->
        assertEntityFolderRenameSync
            "runs"
            "OldRun"
            "NewRun"
            (fun arc -> arc.InitRun("OldRun") |> ignore)
            (fun reloadedArc ->
                Vitest.expect(reloadedArc.ContainsRun("OldRun")).toBe(false)
                Vitest.expect(reloadedArc.ContainsRun("NewRun")).toBe(true))
    )

    Vitest.test("syncRenamedEntityIdentifierOnDisk updates workflow identifier after workflow folder rename", fun () ->
        assertEntityFolderRenameSync
            "workflows"
            "OldWorkflow"
            "NewWorkflow"
            (fun arc -> arc.InitWorkflow("OldWorkflow") |> ignore)
            (fun reloadedArc ->
                Vitest.expect(reloadedArc.ContainsWorkflow("OldWorkflow")).toBe(false)
                Vitest.expect(reloadedArc.ContainsWorkflow("NewWorkflow")).toBe(true))
    )

    Vitest.test("syncRenamedEntityIdentifierOnDisk updates study assay registrations when an assay is renamed", fun () ->
        assertEntityFolderRenameSync
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
        | Ok mergeResult ->
            Vitest.expect(mergeResult.Arc.ContainsAssay("OldAssay")).toBe(false)
            Vitest.expect(mergeResult.Arc.ContainsAssay("NewAssay")).toBe(true)

            let renamedAssay = mergeResult.Arc.GetAssay("NewAssay")
            Vitest.expect(renamedAssay.Title).toEqual(Some "Unsaved assay title")
            Vitest.expect(renamedAssay.DataMap.IsSome).toBe(true)

            let study = mergeResult.Arc.GetStudy("StudyA")
            Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "OldAssay").toBe(false)
            Vitest.expect(study.RegisteredAssayIdentifiers |> Seq.contains "NewAssay").toBe(true)
    )
)
