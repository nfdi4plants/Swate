module ElectronCore.ArcVaultHelperTests

open ARCtrl
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.ARCtrlExtensions
open Main.ArcVault
open Main.ArcVaultHelper
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.Notes.NoteConstants
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Vitest

module FileImportCoordinator = Main.FileImportCoordinator
module WatcherHelpers = Main.WatcherHelpers
module Abort = Main.Bindings.Abort

let private runFileImport
    (vault: ArcVault)
    (requestId, operation: Main.Bindings.Abort.IAbortSignal -> JS.Promise<Result<ImportExternalFilesResult, exn>>)
    =
    Main.IPC.IPCHelper.withExclusiveBusyWriting
        vault
        (fun () ->
            FileImportCoordinator.run
                vault.window
                requestId
                (fun () -> Ok())
                (fun () -> vault.activeFileImport)
                (fun value -> vault.activeFileImport <- value)
                operation
        )

Vitest.describe (
    "ArcVault merge queue",
    fun () ->
        Vitest.test (
            "active import cancellation waits for import cleanup to finish",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())
                let mutable finishCleanup = ignore
                let mutable waitFinished = false

                let cleanupCompletion =
                    JS.Constructors.Promise.Create(fun resolve _ -> finishCleanup <- fun () -> resolve ())

                let import =
                    runFileImport
                        vault
                        ("import-request",
                         fun abortSignal -> promise {
                             do! cleanupCompletion

                             if abortSignal.aborted then
                                 return Ok ImportExternalFilesResult.Cancelled
                             else
                                 return Ok ImportExternalFilesResult.Completed
                         })

                let cancellation = promise {
                    let activeImport = vault.activeFileImport.Value
                    activeImport.AbortController.abort ()
                    let! _ = activeImport.Completion
                    waitFinished <- true
                }

                do! Promise.sleep 0
                Vitest.expect(waitFinished).toBe (false)

                finishCleanup ()
                do! cancellation
                let! importResult = import
                Vitest.expect(importResult).toEqual (Ok ImportExternalFilesResult.Cancelled)
                Vitest.expect(waitFinished).toBe (true)
            }
        )

        Vitest.test (
            "file import owns busy-write state and exposes its lifecycle until cleanup completes",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())
                let mutable finishImport = ignore

                let importCompletion =
                    JS.Constructors.Promise.Create(fun resolve _ -> finishImport <- fun () -> resolve ())

                let import =
                    runFileImport
                        vault
                        ("coordinated-import",
                         fun _ -> promise {
                             do! importCompletion
                             return Ok ImportExternalFilesResult.Completed
                         })

                Vitest.expect(vault.isBusyWriting).toBe (true)

                Vitest
                    .expect(vault.activeFileImport |> Option.map _.State)
                    .toEqual (
                        Some {
                            requestId = "coordinated-import"
                            phase = FileImportPhase.Copying
                        }
                    )

                let activeImport = vault.activeFileImport.Value

                vault.activeFileImport <-
                    Some {
                        activeImport with
                            State = {
                                activeImport.State with
                                    phase = FileImportPhase.Finalizing
                            }
                    }

                Vitest.expect(vault.activeFileImport.Value.State.phase = FileImportPhase.Copying).toBe (false)

                Vitest.expect(vault.activeFileImport.Value.State.phase).toEqual (FileImportPhase.Finalizing)

                finishImport ()
                let! result = import
                Vitest.expect(result).toEqual (Ok ImportExternalFilesResult.Completed)
                Vitest.expect(vault.isBusyWriting).toBe (false)
                Vitest.expect(vault.activeFileImport |> Option.map _.State).toEqual (None)
            }
        )

        Vitest.test (
            "synchronously rejected file import clears its active and busy-write state",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())
                let mutable failureMessage = None

                try
                    let! _ =
                        runFileImport vault ("rejected-import", fun _ -> raise (exn "Import failed synchronously."))

                    ()
                with error ->
                    failureMessage <- Some error.Message

                Vitest.expect(failureMessage).toEqual (Some "Import failed synchronously.")
                Vitest.expect(vault.activeFileImport).toEqual (None)
                Vitest.expect(vault.isBusyWriting).toBe (false)
            }
        )

        Vitest.test (
            "file import does not start while another ARC write owns the vault",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())
                vault.isBusyWriting <- true

                let mutable operationStarted = false

                match!
                    runFileImport
                        vault
                        ("blocked-import",
                         fun _ ->
                             operationStarted <- true
                             JS.Constructors.Promise.resolve (Ok ImportExternalFilesResult.Completed))
                with
                | Ok _ -> return failwith "Expected the import to be rejected while the vault is busy."
                | Error error ->
                    Vitest.expect(error.Message).toContain ("still saving")
                    Vitest.expect(operationStarted).toBe (false)
                    Vitest.expect(vault.isBusyWriting).toBe (true)
            }
        )

        Vitest.test (
            "watcher suppression consumes only the delayed path owned by an app write",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())
                vault.path <- Some "C:/arc"

                let importedEvent =
                    WatcherHelpers.buildWatcherEvent "C:/arc" "add" "C:/arc/imported.txt"

                vault.fileWatcherOwnedPaths.Add(
                    PathHelpers.normalizePath (importedEvent.AbsolutePath.ToLowerInvariant())
                )
                |> ignore

                let queueWatcherEvent eventName path =
                    WatcherHelpers.queueFileWatcherEvent
                        (fun event ->
                            vault.fileWatcherOwnedPaths.Remove(
                                PathHelpers.normalizePath (event.AbsolutePath.ToLowerInvariant())
                            )
                            |> not
                        )
                        vault.path
                        vault.fileWatcherPendingEvents
                        vault.fileWatcherPendingArcMergeEvents
                        eventName
                        path

                vault.isBusyWriting <- true
                queueWatcherEvent "change" "C:/arc/investigation.xlsx"
                Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (1)
                Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (1)

                vault.isBusyWriting <- false
                do! Promise.sleep 2_100

                queueWatcherEvent "change" "C:/arc/imported.txt"
                Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (2)
                Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (1)

                // Ownership is consumed by the delayed watcher event, so a later external edit
                // to the same file is no longer suppressed.
                queueWatcherEvent "add" "C:/arc/imported.txt"
                Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (2)
            }
        )

        Vitest.test (
            "failed save-and-close resets the close lifecycle so closing can be retried",
            fun () -> promise {
                let window = TestHelpers.testWindow ()
                let vault = ArcVault(window)
                let arc = ARC("close-save-failure")
                vault.SetArc arc
                arc.Title <- Some "Unsaved title"
                vault.RefreshHasUnsavedArcChangesFlag()
                Vitest.expect(vault.hasUnsavedArcChanges).toBe (true)

                let vaults = ArcVaults()
                vaults.Vaults.Add(window.id, vault)
                vault.CloseState <- CloseLifecycleState.WaitingForSaveDecision

                match! vaults.ResolveCloseRequest(window.id, SaveBeforeQuitDecision.SaveAndClose) with
                | Ok() -> return failwith "Expected save-and-close to fail without an ARC path."
                | Error _ -> Vitest.expect(vault.CloseState).toEqual (CloseLifecycleState.Idle)
            }
        )

        Vitest.test (
            "runs overlapping merges sequentially so the second observes the first result",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())
                let executionOrder = ResizeArray<string>()
                let mutable mergeResult = 0
                let mutable releaseFirstMerge = ignore

                let firstMergeGate =
                    JS.Constructors.Promise.Create(fun resolve _ -> releaseFirstMerge <- fun () -> resolve ())

                let firstMerge =
                    vault.EnqueueArcMerge(fun () -> promise {
                        executionOrder.Add "first-start"
                        do! firstMergeGate
                        mergeResult <- 1
                        executionOrder.Add "first-end"
                    })

                let secondMerge =
                    vault.EnqueueArcMerge(fun () -> promise {
                        executionOrder.Add "second-start"
                        Vitest.expect(mergeResult).toBe (1)
                        mergeResult <- 2
                        executionOrder.Add "second-end"
                    })

                releaseFirstMerge ()
                do! firstMerge
                do! secondMerge

                Vitest
                    .expect(executionOrder.ToArray())
                    .toEqual (
                        [|
                            "first-start"
                            "first-end"
                            "second-start"
                            "second-end"
                        |]
                    )

                Vitest.expect(mergeResult).toBe (2)
            }
        )

        Vitest.test (
            "continues processing after a merge operation fails",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())
                let mutable continued = false

                let failedMerge =
                    vault.EnqueueArcMerge(fun () -> promise { return raise (exn "Expected merge failure") })

                try
                    do! failedMerge
                with error ->
                    Vitest.expect(error.Message).toContain ("Expected merge failure")

                do! Promise.sleep 0
                let followingMerge = vault.EnqueueArcMerge(fun () -> promise { continued <- true })
                do! followingMerge
                Vitest.expect(continued).toBe (true)
            }
        )

        Vitest.test (
            "watcher merge reloads an open ARC whose in-memory load is missing",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-watcher-recovery-"
                    "WatcherRecoveryArc"
                    ignore
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        match! vault.TryTriggerArcInMemoryMergeOnFileWatcherEvents [] with
                        | Error error -> failwith error.Message
                        | Ok() ->
                            Vitest.expect(vault.arc.IsSome).toBe (true)
                            Vitest.expect(vault.arc.Value.Identifier).toBe ("WatcherRecoveryArc")
                    })
        )

        Vitest.test (
            "close decisions are rejected unless a save decision is pending",
            fun () -> promise {
                let window = TestHelpers.testWindow ()
                let vault = ArcVault(window)
                let vaults = ArcVaults()
                vaults.Vaults.Add(window.id, vault)

                match! vaults.ResolveCloseRequest(window.id, SaveBeforeQuitDecision.CloseWithoutSaving) with
                | Ok() -> return failwith "Expected an unsolicited close decision to be rejected."
                | Error error ->
                    Vitest.expect(error.Message).toContain ("no save decision is pending")
                    Vitest.expect(vault.CloseState).toEqual (CloseLifecycleState.Idle)
            }
        )
)

let private lifecycleTestWindow id isDestroyed onSend =
    // The remoting proxy calls webContents.send with channel and payload arguments.
    // Discard those transport details so lifecycle tests only observe whether a send occurred.
    let send: obj = emitJsExpr onSend "((..._args) => $0())"

    // ArcVault only needs this subset of BrowserWindow for lifecycle broadcasts. Keeping the
    // fixture minimal avoids constructing a real Electron window in the Vitest environment.
    createObj [
        "id" ==> id
        "isDestroyed" ==> (fun () -> isDestroyed)
        "webContents" ==> createObj [ "send" ==> send ]
    ]
    |> unbox<BrowserWindow>

let private mkdirRecursiveAsync (directoryPath: string) = promise {
    let! _ = mkdirAsync directoryPath (MkdirOptions(recursive = true))
    return ()
}

let private writeTextFileAsync (filePath: string) (content: string) =
    writeFileAsync filePath content TextEncoding.Utf8

let private arctrlDefaultGitignoreContent () =
    match ARCtrl.Contract.Git.gitignoreContract.DTO with
    | Some(ARCtrl.Contract.DTO.Text content) -> content
    | _ -> failwith "ARCtrl default .gitignore contract does not contain text content."

let private addDataMapToAllEntityTypes (arc: ARC) =
    let study = ArcStudy("Study With DataMap")
    study.DataMap <- Some(DataMap.init ())
    arc.AddStudy(study)

    let assay = ArcAssay("Assay With DataMap")
    assay.DataMap <- Some(DataMap.init ())
    arc.AddAssay(assay)

    let workflow = ArcWorkflow("Workflow With DataMap")
    workflow.DataMap <- Some(DataMap.init ())
    arc.AddWorkflow(workflow)

    let run = ArcRun("Run With DataMap")
    run.DataMap <- Some(DataMap.init ())
    arc.AddRun(run)

Vitest.describe (
    "ArcVaultHelper",
    fun () ->
        Vitest.test (
            "DataMap add synchronization preserves the persisted static-hash baseline",
            fun () ->
                let parentInfo = DatamapParentInfo.create "DataMapAssay" DataMapParent.Assay
                let persistedDataMap = DataMap.init ()
                persistedDataMap.StaticHash <- 123
                let persistedAssay = ArcAssay("DataMapAssay")
                persistedAssay.DataMap <- Some persistedDataMap
                let persistedArc = ARC("PersistedArc")
                persistedArc.AddAssay persistedAssay

                let localDataMap = DataMap.init ()
                localDataMap.StaticHash <- 456
                let localAssay = ArcAssay("DataMapAssay")
                localAssay.DataMap <- Some localDataMap
                let localArc = ARC("LocalArc")
                localArc.AddAssay localAssay

                syncAddedArcFileFromPersisted
                    persistedArc
                    localArc
                    (ArcFiles.DataMap(Some parentInfo, persistedDataMap))

                Vitest.expect(persistedDataMap.StaticHash).toBe (123)
                Vitest.expect(persistedArc.GetAssay("DataMapAssay").DataMap.Value.StaticHash).toBe (123)
                Vitest.expect(localArc.GetAssay("DataMapAssay").DataMap.Value.StaticHash).toBe (123)
        )

        Vitest.test (
            "recent ARC broadcasts skip windows destroyed during simultaneous shutdown",
            fun () ->
                let mutable aliveWindowSendCount = 0

                let aliveWindow =
                    lifecycleTestWindow 1 false (fun () -> aliveWindowSendCount <- aliveWindowSendCount + 1)

                let destroyedWindow =
                    lifecycleTestWindow 2 true (fun () -> failwith "Destroyed window received an IPC message.")

                let vaults = ArcVaults()
                vaults.Vaults.Add(aliveWindow.id, ArcVault(aliveWindow))
                vaults.Vaults.Add(destroyedWindow.id, ArcVault(destroyedWindow))

                vaults.BroadcastRecentARCs()

                Vitest.expect(aliveWindowSendCount).toBe (1)
        )

        Vitest.test (
            "a repeated close attempt restores a lost save dialog request",
            fun () ->
                let mutable closeHandler: obj -> unit = ignore
                let mutable saveDialogRequestCount = 0
                let mutable preventedCloseCount = 0

                let window =
                    createObj [
                        "id" ==> 3
                        "title" ==> ""
                        "isDestroyed" ==> (fun () -> false)
                        "close" ==> ignore
                        "on"
                        ==> (fun (eventName: string) (handler: obj -> unit) ->
                            if eventName = "close" then
                                closeHandler <- handler
                        )
                        "webContents"
                        ==> createObj [
                            "send"
                            ==> (fun (_channel: string) (_payload: obj) ->
                                saveDialogRequestCount <- saveDialogRequestCount + 1
                            )
                        ]
                    ]
                    |> unbox<BrowserWindow>

                let vault = ArcVault(window)
                let arc = ARC("UnsavedArc")
                vault.SetArc arc
                arc.Title <- Some "Unsaved title"
                vault.RefreshHasUnsavedArcChangesFlag()
                saveDialogRequestCount <- 0

                let vaults = ArcVaults()
                vaults.OnCloseWindow(window, vault, window.id)

                let closeEvent =
                    createObj [
                        "preventDefault" ==> (fun () -> preventedCloseCount <- preventedCloseCount + 1)
                    ]

                closeHandler closeEvent
                closeHandler closeEvent

                Vitest.expect(vault.CloseState).toEqual (CloseLifecycleState.WaitingForSaveDecision)
                Vitest.expect(preventedCloseCount).toBe (2)
                Vitest.expect(saveDialogRequestCount).toBe (2)
        )

        Vitest.test (
            "an import rollback failure keeps the window open and resets the close lifecycle",
            fun () -> promise {
                let mutable closeHandler: obj -> unit = ignore
                let mutable closeCount = 0
                let mutable preventedCloseCount = 0

                let window =
                    createObj [
                        "id" ==> 4
                        "title" ==> ""
                        "isDestroyed" ==> (fun () -> false)
                        "close" ==> (fun () -> closeCount <- closeCount + 1)
                        "on"
                        ==> (fun (eventName: string) (handler: obj -> unit) ->
                            if eventName = "close" then
                                closeHandler <- handler
                        )
                        "webContents" ==> createObj [ "send" ==> (fun (_: string) (_: obj) -> ()) ]
                    ]
                    |> unbox<BrowserWindow>

                let vault = ArcVault(window)
                let abortController = Abort.AbortController.create ()

                vault.activeFileImport <-
                    Some {
                        State = {
                            requestId = "failed-rollback"
                            phase = FileImportPhase.Copying
                        }
                        AbortController = abortController
                        Completion = JS.Constructors.Promise.resolve (Error(exn "EPERM during rollback"))
                    }

                let vaults = ArcVaults()
                vaults.OnCloseWindow(window, vault, window.id)

                let closeEvent =
                    createObj [
                        "preventDefault" ==> (fun () -> preventedCloseCount <- preventedCloseCount + 1)
                    ]

                closeHandler closeEvent
                do! Promise.sleep 0

                Vitest.expect(preventedCloseCount).toBe (1)
                Vitest.expect(closeCount).toBe (0)
                Vitest.expect(abortController.signal.aborted).toBe (true)
                Vitest.expect(vault.CloseState).toEqual (CloseLifecycleState.Idle)
            }
        )

        Vitest.test (
            "file watcher polling defaults to Windows only",
            fun () ->
                Vitest.expect(shouldUsePollingByDefault "win32").toBe (true)
                Vitest.expect(shouldUsePollingByDefault "WIN32").toBe (true)
                Vitest.expect(shouldUsePollingByDefault "linux").toBe (false)
                Vitest.expect(shouldUsePollingByDefault "darwin").toBe (false)
        )

        Vitest.test (
            "Git metadata path detection excludes only exact .git path segments",
            fun () ->
                Vitest.expect(isGitMetadataPath ".git").toBe (true)
                Vitest.expect(isGitMetadataPath ".git/objects/ab/object").toBe (true)
                Vitest.expect(isGitMetadataPath "notes\\.GIT\\config").toBe (true)
                Vitest.expect(isGitMetadataPath ".gitignore").toBe (false)
                Vitest.expect(isGitMetadataPath ".gitattributes").toBe (false)
                Vitest.expect(isGitMetadataPath "notes/my.git/file.txt").toBe (false)
        )

        Vitest.test (
            "legacy isa_datamap paths are ignored in favor of the canonical workbook",
            fun () ->
                Vitest.expect(isLegacyDataMapPath "assays/assay_1/isa_datamap").toBe (true)
                Vitest.expect(isLegacyDataMapPath "assays/assay_1/ISA_DATAMAP").toBe (true)
                Vitest.expect(isLegacyDataMapPath "assays/assay_1/isa.datamap.xlsx").toBe (false)
                Vitest.expect(isLegacyDataMapPath "assays/isa_datamap/data.txt").toBe (false)
        )

        Vitest.test (
            "legacy isa_datamap file is migrated to the canonical workbook path",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-legacy-datamap-"

                try
                    let assayFolder = join [| rootPath; "assays"; "assay_1" |]
                    let legacyPath = join [| assayFolder; "isa_datamap" |]
                    let canonicalPath = join [| assayFolder; "isa.datamap.xlsx" |]
                    do! mkdirRecursiveAsync assayFolder
                    do! writeTextFileAsync legacyPath "legacy-content"

                    let! migratedPaths = migrateLegacyDataMapPathsAsync rootPath [| "assays/assay_1/isa_datamap" |]

                    Vitest.expect(migratedPaths).toEqual ([| "assays/assay_1/isa.datamap.xlsx" |])
                    Vitest.expect(existsSync legacyPath).toBe (false)
                    Vitest.expect(existsSync canonicalPath).toBe (true)

                    let! migratedContent = readFileAsync canonicalPath TextEncoding.Utf8
                    Vitest.expect(migratedContent).toBe ("legacy-content")
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "canonical DataMap workbook wins over a legacy isa_datamap file",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-canonical-datamap-"

                try
                    let assayFolder = join [| rootPath; "assays"; "assay_1" |]
                    let legacyPath = join [| assayFolder; "isa_datamap" |]
                    let canonicalPath = join [| assayFolder; "isa.datamap.xlsx" |]
                    do! mkdirRecursiveAsync assayFolder
                    do! writeTextFileAsync legacyPath "legacy-content"
                    do! writeTextFileAsync canonicalPath "canonical-content"

                    let! migratedPaths =
                        migrateLegacyDataMapPathsAsync rootPath [|
                            "assays/assay_1/isa_datamap"
                            "assays/assay_1/isa.datamap.xlsx"
                        |]

                    Vitest.expect(migratedPaths).toEqual ([| "assays/assay_1/isa.datamap.xlsx" |])
                    Vitest.expect(existsSync legacyPath).toBe (true)

                    let! canonicalContent = readFileAsync canonicalPath TextEncoding.Utf8
                    Vitest.expect(canonicalContent).toBe ("canonical-content")
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "Swate write contracts contain only targeted scaffold and ARC files",
            fun () ->
                let arc = ARC("TargetedWriteArc")
                addDataMapToAllEntityTypes arc
                arc.SetLicenseFulltext("license text")

                arc.SetFilePaths(
                    [|
                        ".git/config"
                        "payload.txt"
                        "missing-payload.txt"
                        "assays/Assay With DataMap/README.md"
                    |]
                )

                let actualPaths = arc.GetWriteContractsSwate() |> Array.map _.Path |> Array.sort

                let expectedPaths =
                    [|
                        ".gitignore"
                        "LICENSE"
                        "assays/.gitkeep"
                        "assays/Assay With DataMap/isa.assay.xlsx"
                        "assays/Assay With DataMap/isa.datamap.xlsx"
                        "isa.investigation.xlsx"
                        "notes/README.md"
                        "runs/.gitkeep"
                        "runs/Run With DataMap/isa.datamap.xlsx"
                        "runs/Run With DataMap/isa.run.xlsx"
                        "studies/.gitkeep"
                        "studies/Study With DataMap/isa.datamap.xlsx"
                        "studies/Study With DataMap/isa.study.xlsx"
                        "workflows/.gitkeep"
                        "workflows/Workflow With DataMap/isa.datamap.xlsx"
                        "workflows/Workflow With DataMap/isa.workflow.xlsx"
                    |]
                    |> Array.sort

                Vitest.expect(actualPaths).toEqual (expectedPaths)
        )

        Vitest.test (
            "TryWriteAsyncSwate writes the default gitignore and notes README into an ARC root",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-default-gitignore-"
                let arcPath = join [| rootPath; "arc" |]
                let gitignorePath = join [| arcPath; ".gitignore" |]
                let notesReadmePath = join [| arcPath; NotesRootFolderName; NotesReadmeFileName |]

                try
                    do! mkdirRecursiveAsync arcPath

                    let arc = ARC("ScaffoldArc")

                    match! arc.TryWriteAsyncSwate(arcPath) with
                    | Error errors -> failwith (String.concat "\n" errors)
                    | Ok _ -> ()

                    let! gitignoreContent = readFileAsync gitignorePath TextEncoding.Utf8
                    let! notesReadmeContent = readFileAsync notesReadmePath TextEncoding.Utf8
                    Vitest.expect(gitignoreContent).toBe (arctrlDefaultGitignoreContent ())
                    Vitest.expect(notesReadmeContent).toBe (NotesReadmeContent)
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "TryWriteAsyncSwate preserves payload and does not create unmanaged file-tree entries",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-targeted-write-"
                let arcPath = join [| rootPath; "arc" |]
                let gitFolder = join [| arcPath; ".git" |]
                let assayFolder = join [| arcPath; "assays"; "Assay 1" |]
                let payloadPath = join [| arcPath; "payload.txt" |]
                let gitConfigPath = join [| gitFolder; "config" |]
                let readmePath = join [| assayFolder; "README.md" |]
                let missingPayloadPath = join [| arcPath; "missing-payload.txt" |]

                try
                    do! mkdirRecursiveAsync gitFolder
                    do! mkdirRecursiveAsync assayFolder
                    do! writeTextFileAsync payloadPath "payload"
                    do! writeTextFileAsync gitConfigPath "git-config"
                    do! writeTextFileAsync readmePath "readme"

                    let arc = ARC("TargetedWriteArc")
                    arc.AddAssay(ArcAssay("Assay 1"))

                    arc.SetFilePaths(
                        [|
                            ".git/config"
                            "payload.txt"
                            "missing-payload.txt"
                            "assays/Assay 1/README.md"
                        |]
                    )

                    match! arc.TryWriteAsyncSwate(arcPath) with
                    | Error errors -> failwith (String.concat "\n" errors)
                    | Ok _ -> ()

                    let! payload = readFileAsync payloadPath TextEncoding.Utf8
                    let! gitConfig = readFileAsync gitConfigPath TextEncoding.Utf8
                    let! readme = readFileAsync readmePath TextEncoding.Utf8
                    let! missingPayloadExists = TestHelpers.pathExistsAsync missingPayloadPath

                    let! assayFileExists = TestHelpers.pathExistsAsync (join [| assayFolder; "isa.assay.xlsx" |])

                    let! collectionGitKeepExists =
                        TestHelpers.pathExistsAsync (join [| arcPath; "assays"; ".gitkeep" |])

                    Vitest.expect(payload).toBe ("payload")
                    Vitest.expect(gitConfig).toBe ("git-config")
                    Vitest.expect(readme).toBe ("readme")
                    Vitest.expect(missingPayloadExists).toBe (false)
                    Vitest.expect(assayFileExists).toBe (true)
                    Vitest.expect(collectionGitKeepExists).toBe (true)
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "ARC vault save preserves note markdown files",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-save-notes-"
                    "NotesPreservingArc"
                    ignore
                    (fun arcPath -> promise {
                        let rootNotesFolder = join [| arcPath; "notes"; "2026-04-27"; "root_note" |]
                        let studyNotesFolder = join [| arcPath; "notes"; "2026-04-27"; "study_note" |]
                        let rootNotePath = join [| rootNotesFolder; "root_note.md" |]
                        let studyNotePath = join [| studyNotesFolder; "study_note.md" |]

                        let rootNoteContent = "---\ntitle: Root note\n---\n\nRoot note body."
                        let studyNoteContent = "---\ntitle: Study note\n---\n\nStudy note body."

                        do! mkdirRecursiveAsync rootNotesFolder
                        do! mkdirRecursiveAsync studyNotesFolder
                        do! writeTextFileAsync rootNotePath rootNoteContent
                        do! writeTextFileAsync studyNotePath studyNoteContent

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        vault.arc.Value.Title <- Some "Saved title"
                        vault.arc.Value.StaticHash <- 0

                        match! vault.WriteArc() with
                        | Error error -> failwith error.Message
                        | Ok() -> ()

                        let! rootNoteAfterSave = readFileAsync rootNotePath TextEncoding.Utf8
                        let! studyNoteAfterSave = readFileAsync studyNotePath TextEncoding.Utf8
                        let! persistedArc = TestHelpers.loadArcAsync arcPath

                        Vitest.expect(rootNoteAfterSave).toBe (rootNoteContent)
                        Vitest.expect(studyNoteAfterSave).toBe (studyNoteContent)
                        Vitest.expect(persistedArc.Title).toEqual (Some "Saved title")
                    })
        )

        Vitest.test (
            "ARC loading and writing ignore Git metadata and preserve payload",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-ignore-git-"
                    "IgnoreGitArc"
                    ignore
                    (fun arcPath -> promise {
                        let gitObjectFolder = join [| arcPath; ".git"; "objects"; "ab" |]
                        let nestedGitFolder = join [| arcPath; "notes"; ".git" |]
                        let payloadPath = join [| arcPath; "payload.txt" |]
                        let gitObjectPath = join [| gitObjectFolder; "object" |]
                        do! mkdirRecursiveAsync gitObjectFolder
                        do! mkdirRecursiveAsync nestedGitFolder
                        do! writeTextFileAsync gitObjectPath "git-object"
                        do! writeTextFileAsync (join [| nestedGitFolder; "config" |]) "nested-git-config"
                        do! writeTextFileAsync payloadPath "payload"

                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult
                        let paths = loadedArc.FileSystem.Tree.ToFilePaths()

                        Vitest.expect(paths |> Array.exists isGitMetadataPath).toBe (false)

                        loadedArc.SetFilePaths(Array.append paths [| ".git/objects/ab/object" |])
                        loadedArc.Title <- Some "Saved title"
                        do! loadedArc.UpdateAsync arcPath

                        let! payload = readFileAsync payloadPath TextEncoding.Utf8
                        let! gitObject = readFileAsync gitObjectPath TextEncoding.Utf8
                        Vitest.expect(payload).toBe ("payload")
                        Vitest.expect(gitObject).toBe ("git-object")
                    })
        )

        Vitest.test (
            "normal ARC save does not restore deleted DTO-less files from a stale file tree",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-save-deleted-file-"
                    "DeletedFileArc"
                    ignore
                    (fun arcPath -> promise {
                        let payloadPath = join [| arcPath; "payload.txt" |]
                        let collectionGitKeepPath = join [| arcPath; "assays"; ".gitkeep" |]
                        do! writeTextFileAsync payloadPath "payload"

                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult
                        let stalePaths = loadedArc.FileSystem.Tree.ToFilePaths()

                        Vitest.expect(stalePaths |> Array.contains "payload.txt").toBe (true)
                        Vitest.expect(stalePaths |> Array.contains "assays/.gitkeep").toBe (true)

                        do! rmAsync payloadPath (RmOptions())
                        do! rmAsync collectionGitKeepPath (RmOptions())

                        loadedArc.Title <- Some "Saved title"
                        do! loadedArc.UpdateAsync arcPath

                        let! payloadExists = TestHelpers.pathExistsAsync payloadPath
                        let! collectionGitKeepExists = TestHelpers.pathExistsAsync collectionGitKeepPath
                        Vitest.expect(payloadExists).toBe (false)
                        Vitest.expect(collectionGitKeepExists).toBe (false)

                        let! reloadedArc = TestHelpers.loadArcAsync arcPath
                        Vitest.expect(reloadedArc.Title).toEqual (Some "Saved title")
                    })
        )

        Vitest.test (
            "LoadArc reports load errors without crashing the printf formatter",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-load-arc-error-"

                try
                    let vault = ArcVault(TestHelpers.testWindow ())
                    vault.path <- Some rootPath

                    let mutable capturedError: exn option = None

                    try
                        do! vault.LoadArc()
                    with error ->
                        capturedError <- Some error

                    match capturedError with
                    | None ->
                        do! TestHelpers.removeDirectoryAsync rootPath
                        return failwith "Expected LoadArc to fail for an invalid ARC folder."
                    | Some error ->
                        Vitest.expect(error.Message).toContain ("[Swate-0] Unable to load ARC:")
                        Vitest.expect(error.Message).not.toContain ("fmt.cont")
                        do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "RenameOpenArcRoot moves the active ARC folder and updates the vault path",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-"
                    "RenameRootArc"
                    ignore
                    (fun arcPath -> promise {
                        let targetPath =
                            join [| dirname arcPath; "renamed-arc" |] |> PathHelpers.normalizePath

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        match! vault.RenameOpenArcRoot "renamed-arc" with
                        | Error error -> failwith error.Message
                        | Ok renamedPath ->
                            Vitest.expect(renamedPath).toBe (targetPath)
                            Vitest.expect(vault.path).toEqual (Some targetPath)

                            let! oldPathExists = TestHelpers.pathExistsAsync arcPath
                            let! newPathExists = TestHelpers.pathExistsAsync targetPath
                            Vitest.expect(oldPathExists).toBe (false)
                            Vitest.expect(newPathExists).toBe (true)

                            let! reloadedArc = TestHelpers.loadArcAsync targetPath
                            Vitest.expect(reloadedArc.Identifier).toBe ("RenameRootArc")
                    })
        )

        Vitest.test (
            "RenameOpenArcRoot clears pending watcher state before moving the active ARC",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-watcher-"
                    "RenameRootWatcherArc"
                    ignore
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        let timeoutId = Fable.Core.JS.setTimeout (fun () -> ()) 60000

                        vault.fileWatcherReloadArcTimeout <- Some timeoutId

                        vault.fileWatcherPendingEvents.Add {
                            EventName = "change"
                            RelativePath = "isa.investigation.xlsx"
                            AbsolutePath = join [| arcPath; "isa.investigation.xlsx" |]
                        }

                        vault.fileWatcherPendingArcMergeEvents.Add {
                            EventName = "change"
                            RelativePath = "isa.investigation.xlsx"
                            AbsolutePath = join [| arcPath; "isa.investigation.xlsx" |]
                        }

                        try
                            match! vault.RenameOpenArcRoot "renamed-arc-watcher" with
                            | Error error -> failwith error.Message
                            | Ok _ ->
                                Vitest.expect(vault.fileWatcherReloadArcTimeout).toEqual (None)
                                Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (0)
                                Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                        finally
                            vault.fileWatcherReloadArcTimeout |> Option.iter Fable.Core.JS.clearTimeout
                    })
        )

        Vitest.test (
            "RenameOpenArcRoot rejects destination conflicts without moving the active ARC",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-rename-arc-root-conflict-"
                    "RenameRootConflictArc"
                    ignore
                    (fun arcPath -> promise {
                        let targetPath =
                            join [| dirname arcPath; "existing-arc" |] |> PathHelpers.normalizePath

                        do! mkdirRecursiveAsync targetPath

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        do! vault.LoadArc()

                        match! vault.RenameOpenArcRoot "existing-arc" with
                        | Ok _ -> failwith "Expected active ARC root rename to reject an existing destination."
                        | Error error ->
                            Vitest.expect(error.Message).toContain ("destination already exists")
                            Vitest.expect(vault.path).toEqual (Some arcPath)

                            let! oldPathExists = TestHelpers.pathExistsAsync arcPath
                            let! targetPathExists = TestHelpers.pathExistsAsync targetPath
                            Vitest.expect(oldPathExists).toBe (true)
                            Vitest.expect(targetPathExists).toBe (true)
                    })
        )

        Vitest.test (
            "tryBuildOpenArcRootRenamePlan applies the shared rename-name validation rules",
            fun () ->
                match tryBuildOpenArcRootRenamePlan "C:/work/current-arc" "bad\u0000name" with
                | Ok _ -> failwith "Expected ARC root rename to reject null characters."
                | Error error -> Vitest.expect(error.Message).toContain ("null")
        )

        Vitest.test (
            "LoadArc repairs zero-byte canonical ARC workbooks before retrying",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-repair-"
                    "RepairArc"
                    ignore
                    (fun arcPath -> promise {
                        let assayFolder = join [| arcPath; "assays"; "New Assay" |]
                        let assayFile = join [| assayFolder; "isa.assay.xlsx" |]

                        do! mkdirRecursiveAsync assayFolder
                        do! writeTextFileAsync assayFile ""

                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        Vitest.expect(vault.arc.IsSome).toBe (true)
                        Vitest.expect(vault.arc.Value.ContainsAssay("New Assay")).toBe (true)
                        Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                        Vitest.expect(vault.arc.Value.hasInMemoryChanges ()).toBe (false)

                        let! reloadedArc = TestHelpers.loadArcAsync arcPath
                        Vitest.expect(reloadedArc.ContainsAssay("New Assay")).toBe (true)
                    })
        )

        Vitest.test (
            "LoadArc baselines loaded datamap hashes without marking the ARC dirty",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-load-arc-datamap-baseline-"
                    "DatamapBaselineArc"
                    addDataMapToAllEntityTypes
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        do! vault.LoadArc()

                        Vitest.expect(vault.arc.IsSome).toBe (true)
                        Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                        Vitest.expect(vault.arc.Value.hasInMemoryChanges ()).toBe (false)

                        let loadedArc = vault.arc.Value
                        Vitest.expect(loadedArc.GetAssay("Assay With DataMap").DataMap.Value.StaticHash).not.toBe (0)
                        Vitest.expect(loadedArc.GetStudy("Study With DataMap").DataMap.Value.StaticHash).not.toBe (0)

                        Vitest
                            .expect(loadedArc.GetWorkflow("Workflow With DataMap").DataMap.Value.StaticHash)
                            .not.toBe (0)

                        Vitest.expect(loadedArc.GetRun("Run With DataMap").DataMap.Value.StaticHash).not.toBe (0)
                    })
        )
)
