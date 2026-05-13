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

let private createNodeLikeError (code: string) (message: string) : exn =
    createObj [
        "message" ==> message
        "code" ==> code
    ]
    |> unbox<exn>

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
            expectSourceContains arcVaultApiSource "ArcDeleteHelper.mergeReloadedArcAfterDelete"
            expectSourceContains arcVaultApiSource "ARC.merge"
            expectSourceContains arcVaultApiSource "ArcDeletePathRules.buildFallbackUnlinkPaths"
            expectSourceContainsInOrder
                arcVaultApiSource
                [|
                    "fsPromisesDynamic?rm"
                    "do! vault.RefreshFileTree()"
                    "ArcDeleteHelper.mergeReloadedArcAfterDelete"
                |]
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

    Vitest.test("ignore missing disk delete error when target is an affected pending draft", fun () ->
        let pendingAssay = ArcAssay.init "PendingAssay"
        let pendingDto = ArcFiles.Assay pendingAssay |> FileContentDTO.fromArcFile |> Option.get
        let missingPathError = createNodeLikeError "ENOENT" "missing path"

        let shouldIgnore =
            ArcDeleteHelper.shouldIgnoreMissingDiskDeleteError
                "assays/PendingAssay/isa.assay.xlsx"
                (Some pendingDto)
                missingPathError

        Vitest.expect(shouldIgnore).toBe(true)
    )

    Vitest.test("do not ignore missing disk delete error when pending draft is unrelated", fun () ->
        let pendingAssay = ArcAssay.init "PendingAssay"
        let pendingDto = ArcFiles.Assay pendingAssay |> FileContentDTO.fromArcFile |> Option.get
        let missingPathError = createNodeLikeError "ENOENT" "missing path"

        let shouldIgnore =
            ArcDeleteHelper.shouldIgnoreMissingDiskDeleteError
                "assays/OtherAssay/isa.assay.xlsx"
                (Some pendingDto)
                missingPathError

        Vitest.expect(shouldIgnore).toBe(false)
    )

    Vitest.test("do not ignore non-ENOENT delete errors even when pending draft is affected", fun () ->
        let pendingAssay = ArcAssay.init "PendingAssay"
        let pendingDto = ArcFiles.Assay pendingAssay |> FileContentDTO.fromArcFile |> Option.get
        let accessError = createNodeLikeError "EACCES" "permission denied"

        let shouldIgnore =
            ArcDeleteHelper.shouldIgnoreMissingDiskDeleteError
                "assays/PendingAssay/isa.assay.xlsx"
                (Some pendingDto)
                accessError

        Vitest.expect(shouldIgnore).toBe(false)
    )

    Vitest.test("mergeReloadedArcAfterDelete preserves unrelated pending drafts and applies them", fun () ->
        let localArc = ARC("MergeArc")
        localArc.Title <- Some "Dirty local title"

        let reloadedArc = ARC("MergeArc")
        let pendingAssay = ArcAssay.init "AssayMergeA"
        let pendingDto = ArcFiles.Assay pendingAssay |> FileContentDTO.fromArcFile |> Option.get

        let result =
            ArcDeleteHelper.mergeReloadedArcAfterDelete
                "studies/StudyA/isa.study.xlsx"
                [ "studies/StudyA/isa.study.xlsx" ]
                localArc
                reloadedArc
                (Some pendingDto)

        match result with
        | Error error -> failwith error.Message
        | Ok mergeResult ->
            Vitest.expect(mergeResult.PendingArcFileSave.IsSome).toBe(true)
            Vitest.expect(mergeResult.Arc.TryGetAssay "AssayMergeA" |> Option.isSome).toBe(true)
    )

    Vitest.test("mergeReloadedArcAfterDelete drops pending drafts affected by deletion targets", fun () ->
        let localArc = ARC("MergeArc")
        localArc.Title <- Some "Dirty local title"

        let reloadedArc = ARC("MergeArc")
        let pendingAssay = ArcAssay.init "AssayMergeB"
        let pendingDto = ArcFiles.Assay pendingAssay |> FileContentDTO.fromArcFile |> Option.get

        let result =
            ArcDeleteHelper.mergeReloadedArcAfterDelete
                "assays/AssayMergeB"
                [ "assays/AssayMergeB/isa.assay.xlsx" ]
                localArc
                reloadedArc
                (Some pendingDto)

        match result with
        | Error error -> failwith error.Message
        | Ok mergeResult ->
            Vitest.expect(mergeResult.PendingArcFileSave).toEqual(None)
            Vitest.expect(mergeResult.Arc.TryGetAssay "AssayMergeB" |> Option.isSome).toBe(false)
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
                None

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
                None

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
            ArcDeleteHelper.mergeReloadedArcAfterDelete "assays/My Assay" [] localArc reloadedArc None

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
)
