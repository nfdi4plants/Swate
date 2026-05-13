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
                    "match mergeReloadedArc reloadedArc"
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

    Vitest.test("rename event synthesis emits old canonical unlink and new canonical add events", fun () ->
        let events =
            ArcRenameHelper.buildRenameEvents "assays/OldAssay" "assays/NewAssay"

        let unlinkPaths =
            events
            |> List.choose (fun event ->
                match event.EventName with
                | EventName.Unlink -> Some event.Path
                | _ -> None
            )

        let addPaths =
            events
            |> List.choose (fun event ->
                match event.EventName with
                | EventName.Add -> Some event.Path
                | _ -> None
            )

        Vitest.expect(unlinkPaths).toEqual([ "assays/OldAssay/isa.assay.xlsx"; "assays/OldAssay/isa.datamap.xlsx" ])
        Vitest.expect(addPaths).toEqual([ "assays/NewAssay/isa.assay.xlsx"; "assays/NewAssay/isa.datamap.xlsx" ])
    )

    Vitest.test("rename event synthesis stays empty for generic path renames", fun () ->
        let events =
            ArcRenameHelper.buildRenameEvents "assays/StudyA/notes/info.md" "assays/StudyA/notes/renamed.md"

        Vitest.expect(events).toEqual([])
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


    Vitest.test("mapRenameDiskError maps ENOENT to source-missing message", fun () ->
        let mappedError =
            ArcRenameHelper.mapRenameDiskError
                "assays/OldAssay"
                "assays/NewAssay"
                (createNodeLikeError "ENOENT" "rename failed")

        Vitest.expect(mappedError.Message.Contains "source path no longer exists on disk").toBe(true)
    )

    Vitest.test("mapRenameDiskError maps EEXIST and ENOTEMPTY to destination-exists message", fun () ->
        let eexistMappedError =
            ArcRenameHelper.mapRenameDiskError
                "assays/OldAssay"
                "assays/NewAssay"
                (createNodeLikeError "EEXIST" "rename failed")

        let enotemptyMappedError =
            ArcRenameHelper.mapRenameDiskError
                "assays/OldAssay"
                "assays/NewAssay"
                (createNodeLikeError "ENOTEMPTY" "rename failed")

        Vitest.expect(eexistMappedError.Message.Contains "destination already exists").toBe(true)
        Vitest.expect(enotemptyMappedError.Message.Contains "destination already exists").toBe(true)
    )

    Vitest.test("mapRenameDiskError maps EPERM and EACCES to lock/permission guidance", fun () ->
        let epermMappedError =
            ArcRenameHelper.mapRenameDiskError
                "assays/OldAssay"
                "assays/NewAssay"
                (createNodeLikeError "EPERM" "rename failed")

        let eaccesMappedError =
            ArcRenameHelper.mapRenameDiskError
                "assays/OldAssay"
                "assays/NewAssay"
                (createNodeLikeError "EACCES" "rename failed")

        Vitest.expect(epermMappedError.Message.Contains "permission or file-lock conflict").toBe(true)
        Vitest.expect(eaccesMappedError.Message.Contains "permission or file-lock conflict").toBe(true)
    )
)
