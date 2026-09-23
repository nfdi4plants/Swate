module ElectronCore.ArcVaultHelperTests

open ARCtrl
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron.Main
open Main.ARCtrlExtensions
open Main.ArcVault
open Main.ArcVaultHelper
open Main.ArcVaultTypes
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.Notes.NoteConstants
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Vitest

module FileImportCoordinator = Main.FileImportCoordinator
module WatcherHelpers = Main.WatcherHelpers
module Abort = Main.Bindings.Abort

let private withTempArc =
    TestHelpers.withTempArcWith "swate-watcher-merge-validation-" "WatcherMergeValidationArc"

let private watcherEvent arcPath eventName relativePath : ArcVaultFileSystemEvent = {
    EventName = eventName
    RelativePath = relativePath
    AbsolutePath = join [| arcPath; relativePath |]
}

let private mkdirWatcherDirectoryAsync (directoryPath: string) = promise {
    let! _ = mkdirAsync directoryPath (MkdirOptions(recursive = true))
    return ()
}

let private writeWatcherTextFileAsync (filePath: string) (content: string) =
    writeFileAsync filePath content TextEncoding.Utf8

let private recordingWatcherApi (loadingChanges: ResizeArray<bool>) : IArcFileWatcherApi = {
    IsLoadingChanges = fun isLoading -> loadingChanges.Add isLoading
}

type private WatcherLoadingCall = {
    IsLoading: bool
    PendingEvents: int
    PendingArcMergeEvents: int
}

let private recordingWatcherApiWithPendingState
    (vault: ArcVault)
    (loadingCalls: ResizeArray<WatcherLoadingCall>)
    : IArcFileWatcherApi =
    {
        IsLoadingChanges =
            fun isLoading ->
                loadingCalls.Add {
                    IsLoading = isLoading
                    PendingEvents = vault.fileWatcherPendingEvents.Count
                    PendingArcMergeEvents = vault.fileWatcherPendingArcMergeEvents.Count
                }
    }

let private nowMs () : float = emitJsExpr () "Date.now()"

let private expectWatcherAssayTitle (vault: ArcVault) expectedTitle = promise {
    let titleMatches () =
        vault.arc.Value.GetAssay("DiskAssay").Title = Some expectedTitle

    let mutable attempts = 0

    while attempts < 250 && not (titleMatches ()) do
        do! Promise.sleep 20
        attempts <- attempts + 1

    Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some expectedTitle)
}

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
            "nested busy scopes release the flag with the outermost scope",
            fun () -> promise {
                let vault = ArcVault(TestHelpers.testWindow ())

                do!
                    Main.IPC.IPCHelper.withBusyWritingScope
                        vault
                        (fun () -> promise {
                            Vitest.expect(vault.isBusyWriting).toBe true

                            do!
                                vault.WithBusyWritingScope(fun () -> promise {
                                    Vitest.expect(vault.isBusyWriting).toBe true
                                })

                            Vitest.expect(vault.isBusyWriting).toBe true
                        })

                Vitest.expect(vault.isBusyWriting).toBe false
            }
        )

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
                let mutable operationStarted = false

                do!
                    vault.WithBusyWritingScope(fun () -> promise {
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
                    })

                Vitest.expect(vault.isBusyWriting).toBe (false)
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

)

Vitest.describe (
    "watcher merge validation",
    fun () ->
        Vitest.test (
            "a write that starts while the watcher merge waits in the queue defers it",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        let mutable watcherMergeBarrierCalled = false

                        vault.WatcherMergeBarrier <-
                            Some(fun () ->
                                watcherMergeBarrierCalled <- true
                                promise { return () }
                            )

                        let mutable releaseFirstMerge = ignore

                        let firstMergeGate =
                            JS.Constructors.Promise.Create(fun resolve _ -> releaseFirstMerge <- fun () -> resolve ())

                        let firstMerge = vault.EnqueueArcMerge(fun () -> firstMergeGate)

                        let watcherEvents = [
                            watcherEvent arcPath "change" "assays/DiskAssay/isa.assay.xlsx"
                        ]

                        let watcherMerge = vault.TryApplyWatcherArcMergeIfEligible watcherEvents

                        let mutable releaseWrite = ignore

                        let writeGate =
                            JS.Constructors.Promise.Create(fun resolve _ -> releaseWrite <- fun () -> resolve ())

                        let writeScope = vault.WithBusyWritingScope(fun () -> writeGate)
                        releaseFirstMerge ()
                        do! firstMerge

                        match! watcherMerge with
                        | WatcherMergeOutcome.Deferred -> ()
                        | _ -> return failwith "The watcher merge should defer while the write is busy."

                        Vitest.expect(watcherMergeBarrierCalled).toBe (false)
                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Old title")

                        releaseWrite ()
                        do! writeScope
                        do! Promise.sleep 600

                        vault.WatcherMergeBarrier <- None

                        match! vault.TryApplyWatcherArcMergeIfEligible watcherEvents with
                        | WatcherMergeOutcome.Applied -> ()
                        | _ -> return failwith "The watcher merge should apply after the write suppression ends."

                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Changed on disk")
                    })
        )

        Vitest.test (
            "a write that starts and ends while the snapshot loads defers it even after suppression",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        vault.WatcherMergeBarrier <-
                            Some(fun () -> promise {
                                do! vault.WithBusyWritingScope(fun () -> promise { return () })

                                let mutable attempts = 0

                                while not vault.IsFileWatcherArcMergeEligible && attempts < 200 do
                                    do! Promise.sleep 20
                                    attempts <- attempts + 1

                                if not vault.IsFileWatcherArcMergeEligible then
                                    return failwith "Watcher merge eligibility did not return after 200 polls."
                            })

                        match!
                            vault.TryApplyWatcherArcMergeIfEligible [
                                watcherEvent arcPath "change" "assays/DiskAssay/isa.assay.xlsx"
                            ]
                        with
                        | WatcherMergeOutcome.Deferred -> ()
                        | _ -> return failwith "The watcher merge should defer after the write generation changes."

                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Old title")
                        Vitest.expect(vault.IsFileWatcherArcMergeEligible).toBe (true)
                    })
        )

        Vitest.test (
            "an unlink admitted before a write that recreated the file merges as a change",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        match!
                            vault.TryApplyWatcherArcMergeIfEligible [
                                watcherEvent arcPath "unlink" "assays/DiskAssay/isa.assay.xlsx"
                            ]
                        with
                        | WatcherMergeOutcome.Applied -> ()
                        | _ -> return failwith "The recreated assay should apply as a change."

                        Vitest.expect(vault.arc.Value.ContainsAssay("DiskAssay")).toBe (true)
                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Changed on disk")
                    })
        )

        Vitest.test (
            "a stale directory delete keeps the entity whose canonical file came back and drops the one that did not",
            fun () ->
                withTempArc
                    (fun arc ->
                        arc.AddAssay(ArcAssay("A1"))
                        arc.AddAssay(ArcAssay("A2"))
                    )
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let a1Folder = join [| arcPath; "assays"; "A1" |]
                        let a2Folder = join [| arcPath; "assays"; "A2" |]
                        do! rmAsync a1Folder (RmOptions(recursive = true, force = true))
                        do! rmAsync a2Folder (RmOptions(recursive = true, force = true))

                        let persistedArc = ARC("WatcherMergeValidationArc")
                        persistedArc.AddAssay(ArcAssay("A1"))

                        match! persistedArc.TryWriteAsyncSwate arcPath with
                        | Error _ -> return failwith "Could not recreate the canonical A1 file."
                        | Ok _ -> ()

                        do! mkdirWatcherDirectoryAsync a2Folder

                        match!
                            vault.TryApplyWatcherArcMergeIfEligible [
                                watcherEvent arcPath "unlinkDir" "assays/A1"
                                watcherEvent arcPath "unlinkDir" "assays/A2"
                            ]
                        with
                        | WatcherMergeOutcome.Applied -> ()
                        | _ -> return failwith "The stale directory deletes should apply."

                        Vitest.expect(vault.arc.Value.ContainsAssay("A1")).toBe (true)
                        Vitest.expect(vault.arc.Value.ContainsAssay("A2")).toBe (false)
                    })
        )

        Vitest.test (
            "a change for a file that is missing at merge time is dropped",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Persisted title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        vault.arc.Value.GetAssay("DiskAssay").Title <- Some "Unsaved title"

                        let assayFile = join [| arcPath; "assays"; "DiskAssay"; "isa.assay.xlsx" |]
                        do! rmAsync assayFile (RmOptions(force = true))

                        match!
                            vault.TryApplyWatcherArcMergeIfEligible [
                                watcherEvent arcPath "change" "assays/DiskAssay/isa.assay.xlsx"
                            ]
                        with
                        | WatcherMergeOutcome.Applied -> ()
                        | _ -> return failwith "The missing-file change should apply with an empty ARC batch."

                        Vitest.expect(vault.arc.Value.ContainsAssay("DiskAssay")).toBe (true)
                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Unsaved title")
                    })
        )

        Vitest.test (
            "normalizeAgainstDisk keeps directory deletes and drops missing-file changes",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-watcher-normalize-"

                try
                    let existingDirectory = join [| rootPath; "existing-directory" |]
                    let existingFile = join [| rootPath; "existing-file.txt" |]
                    let missingFile = join [| rootPath; "missing-file.txt" |]
                    do! mkdirWatcherDirectoryAsync existingDirectory
                    do! writeWatcherTextFileAsync existingFile "existing"

                    let directoryDelete = watcherEvent rootPath "unlinkDir" "existing-directory"
                    let missingChange = watcherEvent rootPath "change" "missing-file.txt"
                    let existingUnlink = watcherEvent rootPath "unlink" "existing-file.txt"

                    let absoluteEvent: ArcVaultFileSystemEvent = {
                        EventName = "change"
                        RelativePath = existingFile
                        AbsolutePath = existingFile
                    }

                    let normalized =
                        WatcherHelpers.normalizeAgainstDisk [
                            directoryDelete
                            missingChange
                            existingUnlink
                            absoluteEvent
                        ]

                    Vitest.expect(normalized |> List.exists (fun event -> event = directoryDelete)).toBe (true)
                    Vitest.expect(normalized |> List.exists (fun event -> event = missingChange)).toBe (false)

                    let existingFileEvent =
                        normalized
                        |> List.find (fun event -> event.RelativePath = existingUnlink.RelativePath)

                    Vitest.expect(existingFileEvent.EventName).toBe ("change")
                    Vitest.expect(normalized |> List.exists (fun event -> event = absoluteEvent)).toBe (true)

                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "overlapping tree updates keep both entries",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        let firstPath = join [| arcPath; "tree-first.txt" |]
                        let secondPath = join [| arcPath; "tree-second.txt" |]
                        do! writeWatcherTextFileAsync firstPath "first"
                        do! writeWatcherTextFileAsync secondPath "second"

                        let firstUpdate =
                            vault.ApplyWatcherFileTreeEvents [ watcherEvent arcPath "add" "tree-first.txt" ]

                        let secondUpdate =
                            vault.ApplyWatcherFileTreeEvents [ watcherEvent arcPath "add" "tree-second.txt" ]

                        do! firstUpdate
                        do! secondUpdate

                        Vitest.expect(vault.fileTree.ContainsKey(firstPath)).toBe (true)
                        Vitest.expect(vault.fileTree.ContainsKey(secondPath)).toBe (true)
                    })
        )

        Vitest.test (
            "a snapshot loaded before a rename is not published",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        vault.WatcherMergeBarrier <-
                            Some(fun () ->
                                vault.ClearPendingFileWatcherState()
                                promise { return () }
                            )

                        match!
                            vault.TryApplyWatcherArcMergeIfEligible [
                                watcherEvent arcPath "change" "assays/DiskAssay/isa.assay.xlsx"
                            ]
                        with
                        | WatcherMergeOutcome.Deferred -> ()
                        | _ -> return failwith "The watcher merge should defer after the pending state reset."

                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Old title")
                    })
        )

        Vitest.test (
            "the import merge inside a busy scope still applies",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        do!
                            vault.WithBusyWritingScope(fun () -> promise {
                                match!
                                    vault.TryTriggerArcInMemoryMergeOnFileWatcherEvents [
                                        watcherEvent arcPath "change" "assays/DiskAssay/isa.assay.xlsx"
                                    ]
                                with
                                | Ok() ->
                                    Vitest
                                        .expect(vault.arc.Value.GetAssay("DiskAssay").Title)
                                        .toEqual (Some "Changed on disk")
                                | Error mergeError -> return raise mergeError
                            })
                    })
        )

        Vitest.test (
            "the reload keeps admitted events while a write runs and merges them after it",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        let loadingChanges = ResizeArray<bool>()

                        let handleFileEvent =
                            vault._FileEventController (recordingWatcherApi loadingChanges)

                        let assayPath = join [| arcPath; "assays/DiskAssay/isa.assay.xlsx" |]
                        handleFileEvent "change" assayPath

                        let mutable releaseWrite = ignore

                        let writeGate =
                            JS.Constructors.Promise.Create(fun resolve _ -> releaseWrite <- fun () -> resolve ())

                        let writeScope = vault.WithBusyWritingScope(fun () -> writeGate)

                        do! Promise.sleep 700
                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Old title")

                        Vitest
                            .expect(
                                vault.fileWatcherPendingArcMergeEvents
                                |> Seq.exists (fun event -> event.RelativePath = "assays/DiskAssay/isa.assay.xlsx")
                            )
                            .toBe (true)

                        releaseWrite ()
                        do! writeScope
                        do! expectWatcherAssayTitle vault "Changed on disk"

                        // The merge publishes before the callback applies the tree batch and clears the loading flag.
                        let mutable flagAttempts = 0

                        while flagAttempts < 250
                              && (loadingChanges.Count = 0 || loadingChanges.[loadingChanges.Count - 1]) do
                            do! Promise.sleep 20
                            flagAttempts <- flagAttempts + 1

                        Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (0)
                        Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                        Vitest.expect(loadingChanges.[loadingChanges.Count - 1]).toBe (false)
                    })
        )

        Vitest.test (
            "a deferred reload keeps the tree batch and retries",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        let mutable releaseMergeQueue = ignore

                        let mergeQueueGate =
                            JS.Constructors.Promise.Create(fun resolve _ -> releaseMergeQueue <- fun () -> resolve ())

                        let queuedMerge = vault.EnqueueArcMerge(fun () -> mergeQueueGate)
                        let loadingChanges = ResizeArray<bool>()

                        let handleFileEvent =
                            vault._FileEventController (recordingWatcherApi loadingChanges)

                        let handlerLoadingCallCount = loadingChanges.Count
                        let assayPath = join [| arcPath; "assays/DiskAssay/isa.assay.xlsx" |]
                        handleFileEvent "change" assayPath

                        do! Promise.sleep 600

                        let mutable releaseWrite = ignore

                        let writeGate =
                            JS.Constructors.Promise.Create(fun resolve _ -> releaseWrite <- fun () -> resolve ())

                        let writeScope = vault.WithBusyWritingScope(fun () -> writeGate)
                        releaseMergeQueue ()
                        do! queuedMerge
                        do! Promise.sleep 100

                        Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count > 0).toBe (true)
                        Vitest.expect(vault.fileWatcherPendingEvents.Count > 0).toBe (true)

                        let falseRecordedSinceHandler =
                            loadingChanges
                            |> Seq.skip handlerLoadingCallCount
                            |> Seq.exists (fun isLoading -> not isLoading)

                        Vitest.expect(falseRecordedSinceHandler).toBe (false)

                        releaseWrite ()
                        do! writeScope
                        do! expectWatcherAssayTitle vault "Changed on disk"

                        Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (0)
                        Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                    })
        )

        Vitest.test (
            "a deferred batch is merged when an overlapping reload applies",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        let assayPath = join [| arcPath; "assays/DiskAssay/isa.assay.xlsx" |]
                        let loadingCalls = ResizeArray<WatcherLoadingCall>()
                        let mutable handleFileEvent: string -> string -> unit = fun _ _ -> ()
                        let mutable barrierCallCount = 0

                        handleFileEvent <-
                            vault._FileEventController (recordingWatcherApiWithPendingState vault loadingCalls)

                        vault.WatcherMergeBarrier <-
                            Some(fun () -> promise {
                                if barrierCallCount = 0 then
                                    barrierCallCount <- barrierCallCount + 1
                                    handleFileEvent "change" assayPath
                                    let secondHandlerStartedAt = nowMs ()
                                    do! vault.WithBusyWritingScope(fun () -> promise { return () })

                                    let mutable attempts = 0

                                    while attempts < 150
                                          && (not vault.IsFileWatcherArcMergeEligible
                                              || nowMs () - secondHandlerStartedAt < 1100.) do
                                        do! Promise.sleep 20
                                        attempts <- attempts + 1

                                    if
                                        not vault.IsFileWatcherArcMergeEligible
                                        || nowMs () - secondHandlerStartedAt < 1100.
                                    then
                                        return failwith "The overlapping reload did not pass the suppression window."
                                else
                                    ()
                            })

                        handleFileEvent "change" assayPath

                        let mutable attempts = 0

                        while attempts < 300
                              && (loadingCalls.Count = 0 || loadingCalls.[loadingCalls.Count - 1].IsLoading) do
                            do! Promise.sleep 20
                            attempts <- attempts + 1

                        if loadingCalls.Count = 0 then
                            return failwith "The watcher controller did not report a loading state."

                        let falseWithPendingEvents =
                            loadingCalls
                            |> Seq.exists (fun call ->
                                not call.IsLoading && (call.PendingEvents > 0 || call.PendingArcMergeEvents > 0)
                            )

                        Vitest.expect(barrierCallCount > 0).toBe (true)
                        Vitest.expect(loadingCalls.[loadingCalls.Count - 1].IsLoading).toBe (false)
                        Vitest.expect(falseWithPendingEvents).toBe (false)
                        Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (0)
                        Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                        Vitest.expect(vault.fileWatcherReloadArcTimeout).toEqual (None)
                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Changed on disk")
                    })
        )

        Vitest.test (
            "a busy reload with nothing pending finishes",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let loadingChanges = ResizeArray<bool>()

                        let handleFileEvent =
                            vault._FileEventController (recordingWatcherApi loadingChanges)

                        let mutable releaseWrite = ignore

                        let writeGate =
                            JS.Constructors.Promise.Create(fun resolve _ -> releaseWrite <- fun () -> resolve ())

                        let writeScope = vault.WithBusyWritingScope(fun () -> writeGate)
                        handleFileEvent "change" (join [| arcPath; "assays/DiskAssay/isa.assay.xlsx" |])

                        // The handler admitted the event to the tree list. Dropping it makes the reload fire
                        // with nothing pending while the write is still open.
                        vault.fileWatcherPendingEvents.Clear()
                        vault.fileWatcherPendingArcMergeEvents.Clear()

                        do! Promise.sleep 700

                        Vitest.expect(loadingChanges.Count > 0).toBe (true)
                        Vitest.expect(loadingChanges.[loadingChanges.Count - 1]).toBe (false)
                        Vitest.expect(vault.fileWatcherReloadArcTimeout).toEqual (None)

                        releaseWrite ()
                        do! writeScope
                    })
        )

        Vitest.test (
            "a reload deferred three times during a write publishes the tree and merges the ARC after it",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        let treeEntryPath = join [| arcPath; "watcher-deferred-tree.txt" |]
                        do! writeWatcherTextFileAsync treeEntryPath "tree entry"

                        let loadingChanges = ResizeArray<bool>()

                        let handleFileEvent =
                            vault._FileEventController (recordingWatcherApi loadingChanges)

                        let assayPath = join [| arcPath; "assays/DiskAssay/isa.assay.xlsx" |]
                        handleFileEvent "change" assayPath
                        handleFileEvent "add" treeEntryPath

                        let mutable releaseWrite = ignore

                        let writeGate =
                            JS.Constructors.Promise.Create(fun resolve _ -> releaseWrite <- fun () -> resolve ())

                        let writeScope = vault.WithBusyWritingScope(fun () -> writeGate)

                        try
                            let waitStartedAt = nowMs ()
                            let mutable attempts = 0

                            // Watcher events carry normalized paths, so the tree key uses forward slashes.
                            let treeHasEntry () =
                                let expectedKey = PathHelpers.normalizePath treeEntryPath

                                vault.fileTree.Keys
                                |> Seq.exists (fun key -> PathHelpers.normalizePath key = expectedKey)

                            let fallbackPublished () =
                                vault.HasReachedWatcherDeferralLimit
                                && treeHasEntry ()
                                && vault.fileWatcherPendingArcMergeEvents.Count > 0

                            while attempts < 80
                                  && (nowMs () - waitStartedAt < 2300.0 || not (fallbackPublished ())) do
                                do! Promise.sleep 50
                                attempts <- attempts + 1

                            Vitest.expect(vault.HasReachedWatcherDeferralLimit).toBe (true)
                            Vitest.expect(treeHasEntry ()).toBe (true)
                            Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count > 0).toBe (true)
                            Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Old title")
                        finally
                            releaseWrite ()

                        do! writeScope
                        do! expectWatcherAssayTitle vault "Changed on disk"

                        Vitest.expect(vault.HasReachedWatcherDeferralLimit).toBe (false)
                        Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                    })
        )

        Vitest.test (
            "a reload whose batch went stale finishes",
            fun () ->
                withTempArc
                    (fun arc -> arc.AddAssay(ArcAssay("DiskAssay", title = "Old title")))
                    (fun arcPath -> promise {
                        let! loadedArc = TestHelpers.loadArcAsync arcPath
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath
                        vault.SetArc loadedArc

                        let! diskArc = TestHelpers.loadArcAsync arcPath
                        diskArc.GetAssay("DiskAssay").Title <- Some "Changed on disk"
                        do! diskArc.UpdateAsync arcPath

                        let loadingChanges = ResizeArray<bool>()

                        let handleFileEvent =
                            vault._FileEventController (recordingWatcherApi loadingChanges)

                        vault.WatcherMergeBarrier <-
                            Some(fun () ->
                                vault.ClearPendingFileWatcherState()
                                promise { return () }
                            )

                        handleFileEvent "change" (join [| arcPath; "assays/DiskAssay/isa.assay.xlsx" |])

                        let mutable attempts = 0

                        while attempts < 250
                              && (loadingChanges.Count = 0 || loadingChanges.[loadingChanges.Count - 1]) do
                            do! Promise.sleep 20
                            attempts <- attempts + 1

                        Vitest.expect(loadingChanges.[loadingChanges.Count - 1]).toBe (false)
                        Vitest.expect(vault.fileWatcherPendingEvents.Count).toBe (0)
                        Vitest.expect(vault.fileWatcherPendingArcMergeEvents.Count).toBe (0)
                        Vitest.expect(vault.arc.Value.GetAssay("DiskAssay").Title).toEqual (Some "Old title")
                    })
        )

        Vitest.test (
            "a tree unlink for a path that exists again keeps the entry",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        let filePath = join [| arcPath; "tree-kept.txt" |]
                        do! writeWatcherTextFileAsync filePath "kept"

                        do! vault.ApplyWatcherFileTreeEvents [ watcherEvent arcPath "add" "tree-kept.txt" ]
                        Vitest.expect(vault.fileTree.ContainsKey(filePath)).toBe (true)

                        do! vault.ApplyWatcherFileTreeEvents [ watcherEvent arcPath "unlink" "tree-kept.txt" ]
                        Vitest.expect(vault.fileTree.ContainsKey(filePath)).toBe (true)

                        do! rmAsync filePath (RmOptions(force = true))
                        do! vault.ApplyWatcherFileTreeEvents [ watcherEvent arcPath "unlink" "tree-kept.txt" ]
                        Vitest.expect(vault.fileTree.ContainsKey(filePath)).toBe (false)
                    })
        )

        Vitest.test (
            "a tree update queued before a state reset does not publish",
            fun () ->
                withTempArc
                    ignore
                    (fun arcPath -> promise {
                        let vault = ArcVault(TestHelpers.testWindow ())
                        vault.path <- Some arcPath

                        let filePath = join [| arcPath; "tree-reset.txt" |]
                        do! writeWatcherTextFileAsync filePath "reset"

                        let mutable releaseTail = ignore

                        let tailGate =
                            JS.Constructors.Promise.Create(fun resolve _ -> releaseTail <- fun () -> resolve ())

                        // The update waits behind the tail while the pending state is reset.
                        vault.FileTreeUpdateTail <- tailGate

                        let update =
                            vault.ApplyWatcherFileTreeEvents [ watcherEvent arcPath "add" "tree-reset.txt" ]

                        vault.ClearPendingFileWatcherState()
                        releaseTail ()
                        do! update

                        Vitest.expect(vault.fileTree.ContainsKey(filePath)).toBe (false)
                    })
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
        "webContents"
        ==> createObj [
            "send" ==> send
            "isDestroyed" ==> (fun () -> isDestroyed)
        ]
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
                        "webContents"
                        ==> createObj [
                            "send" ==> (fun (_: string) (_: obj) -> ())
                            "isDestroyed" ==> (fun () -> false)
                        ]
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
                Vitest.expect(vault.isWaitingForImportCleanup).toBe (false)
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
