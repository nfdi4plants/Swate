module ElectronCore.ArcVaultHelperTests

open ARCtrl
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
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
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Vitest

module FileImportCoordinator = Main.FileImportCoordinator
module WatcherHelpers = Main.WatcherHelpers
module Abort = Main.Bindings.Abort

let private electronMock: obj = import "__electronMock" "electron"

let private resetElectronMock () = electronMock?reset () |> ignore

let private setBrowserWindowFactory (factory: obj -> obj) =
    electronMock?setBrowserWindowFactory (factory) |> ignore

let private setBrowserWindowFromWebContents (handler: obj -> obj) =
    electronMock?setBrowserWindowFromWebContents (handler) |> ignore

let private setShowOpenDialog (handler: obj -> obj -> obj) =
    electronMock?setShowOpenDialog (handler) |> ignore

let private setShowMessageBox (handler: obj -> obj -> obj) =
    electronMock?setShowMessageBox (handler) |> ignore

let private ipcEventWithSenderId senderId : IpcMainInvokeEvent =
    createObj [ "sender" ==> createObj [ "id" ==> senderId ] ]
    |> unbox<IpcMainInvokeEvent>

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

let private focusTrackingTestWindow id onFocus =
    let send: obj = emitJsExpr () "((..._args) => {})"

    createObj [
        "id" ==> id
        "title" ==> ""
        "isDestroyed" ==> (fun () -> false)
        "focus" ==> onFocus
        "webContents" ==> createObj [ "send" ==> send ]
    ]
    |> unbox<BrowserWindow>

let private registrationTestWindow id (loadError: exn) onLoad =
    let mutable destroyed = false
    let mutable closeHandlerAttached = false
    let mutable closedHandlerAttached = false
    let mutable lifecycleWasAttachedWhenLoadStarted = false
    let send: obj = emitJsExpr () "((..._args) => {})"
    let noop: obj = emitJsExpr () "((..._args) => {})"

    let failLoad (_: string) =
        lifecycleWasAttachedWhenLoadStarted <- closeHandlerAttached && closedHandlerAttached
        onLoad ()
        JS.Constructors.Promise.reject loadError

    let onEvent (eventName: string) (_handler: obj) =
        if eventName = "close" then
            closeHandlerAttached <- true
        elif eventName = "closed" then
            closedHandlerAttached <- true

    let onEventJs: obj =
        emitJsExpr onEvent "((eventName, handler) => $0(eventName, handler))"

    let window =
        createObj [
            "id" ==> id
            "title" ==> ""
            "isDestroyed" ==> (fun () -> destroyed)
            "destroy" ==> (fun () -> destroyed <- true)
            "focus" ==> ignore
            "loadFile" ==> failLoad
            "loadURL" ==> failLoad
            "on" ==> onEventJs
            "webContents"
            ==> createObj [
                "send" ==> send
                "setWindowOpenHandler" ==> noop
                "on" ==> noop
                "openDevTools" ==> noop
            ]
        ]
        |> unbox<BrowserWindow>

    window, (fun () -> destroyed), (fun () -> lifecycleWasAttachedWhenLoadStarted)

let private successfulRegistrationTestWindow id onLoad onShow =
    let mutable destroyed = false
    let mutable shown = false
    let mutable closeHandlerAttached = false
    let mutable closedHandlerAttached = false
    let mutable lifecycleWasAttachedWhenLoadStarted = false
    let send: obj = emitJsExpr () "((..._args) => {})"
    let noop: obj = emitJsExpr () "((..._args) => {})"

    let load (_: string) =
        lifecycleWasAttachedWhenLoadStarted <- closeHandlerAttached && closedHandlerAttached
        onLoad ()
        JS.Constructors.Promise.resolve ()

    let onEvent (eventName: string) (_handler: obj) =
        if eventName = "close" then
            closeHandlerAttached <- true
        elif eventName = "closed" then
            closedHandlerAttached <- true

    let onEventJs: obj =
        emitJsExpr onEvent "((eventName, handler) => $0(eventName, handler))"

    let window =
        createObj [
            "id" ==> id
            "title" ==> ""
            "isDestroyed" ==> (fun () -> destroyed)
            "destroy" ==> (fun () -> destroyed <- true)
            "focus" ==> ignore
            "show"
            ==> (fun () ->
                shown <- true
                onShow ()
            )
            "loadFile" ==> load
            "loadURL" ==> load
            "on" ==> onEventJs
            "webContents"
            ==> createObj [
                "send" ==> send
                "setWindowOpenHandler" ==> noop
                "on" ==> noop
                "openDevTools" ==> noop
            ]
        ]
        |> unbox<BrowserWindow>

    window, (fun () -> shown), (fun () -> destroyed), (fun () -> lifecycleWasAttachedWhenLoadStarted)

let private closingWhileArcLoadsRegistrationTestWindow id isArcOpening destroyedCheckCountBeforeClose onShow =
    let mutable destroyed = false
    let mutable shown = false
    let mutable lifecycleAttachedWhenShown = false
    let mutable arcWasOpeningWhenClosed = false
    let mutable preventedCloseCount = 0
    let mutable sendsAfterDestroy = 0
    let mutable titleWritesAfterDestroy = 0
    let mutable destroyedCheckCount = 0
    let mutable closeHandler: (obj -> unit) option = None
    let mutable closedHandler: (unit -> unit) option = None
    let noop: obj = emitJsExpr () "((..._args) => {})"

    let send: obj =
        emitJsExpr
            (fun () ->
                if destroyed then
                    sendsAfterDestroy <- sendsAfterDestroy + 1
            )
            "((..._args) => $0())"

    let load (_: string) = JS.Constructors.Promise.resolve ()

    let onEvent (eventName: string) (handler: obj) =
        if eventName = "close" then
            closeHandler <- Some(unbox handler)
        elif eventName = "closed" then
            closedHandler <- Some(unbox handler)

    let onEventJs: obj =
        emitJsExpr onEvent "((eventName, handler) => $0(eventName, handler))"

    let closeWhenArcIsOpening () =
        arcWasOpeningWhenClosed <- isArcOpening ()

        if arcWasOpeningWhenClosed then
            let preventedCloseCountBeforeAttempt = preventedCloseCount

            closeHandler.Value(
                createObj [
                    "preventDefault" ==> (fun () -> preventedCloseCount <- preventedCloseCount + 1)
                ]
            )

            if preventedCloseCount = preventedCloseCountBeforeAttempt then
                destroyed <- true
                closedHandler.Value()

    let show () =
        shown <- true
        lifecycleAttachedWhenShown <- closeHandler.IsSome && closedHandler.IsSome
        onShow ()

    let isDestroyed () =
        if shown && not destroyed then
            destroyedCheckCount <- destroyedCheckCount + 1
            let shouldClose = isArcOpening ()

            if shouldClose && destroyedCheckCount >= destroyedCheckCountBeforeClose then
                closeWhenArcIsOpening ()

        destroyed

    let windowObject =
        createObj [
            "id" ==> id
            "isDestroyed" ==> isDestroyed
            "destroy" ==> (fun () -> destroyed <- true)
            "focus" ==> ignore
            "show" ==> show
            "loadFile" ==> load
            "loadURL" ==> load
            "on" ==> onEventJs
            "webContents"
            ==> createObj [
                "send" ==> send
                "setWindowOpenHandler" ==> noop
                "on" ==> noop
                "openDevTools" ==> noop
            ]
        ]

    let setTitle (_value: string) =
        if destroyed then
            titleWritesAfterDestroy <- titleWritesAfterDestroy + 1
            failwith "Object has been destroyed"

    emitJsExpr
        (windowObject, setTitle)
        "Object.defineProperty($0, 'title', { configurable: true, get: () => '', set: value => $1(value) })"
    |> ignore

    let window = windowObject |> unbox<BrowserWindow>

    window,
    (fun () -> shown),
    (fun () -> lifecycleAttachedWhenShown),
    (fun () -> arcWasOpeningWhenClosed),
    (fun () -> destroyed),
    (fun () -> sendsAfterDestroy),
    (fun () -> titleWritesAfterDestroy)

let rec private scheduleAfterMicrotaskTurns remaining action =
    if remaining <= 0 then
        action ()
    else
        promise {
            do! JS.Constructors.Promise.resolve ()
            scheduleAfterMicrotaskTurns (remaining - 1) action
        }
        |> Promise.start

let rec private waitUntilWithEventLoopTurns predicate remaining = promise {
    if predicate () then
        return true
    elif remaining <= 0 then
        return false
    else
        do! Promise.sleep 0
        return! waitUntilWithEventLoopTurns predicate (remaining - 1)
}

let private closingAfterStartupTestWindow id isStartupComplete scheduleFromFocus microtaskTurns onShow =
    let mutable destroyed = false
    let mutable shown = false
    let mutable lifecycleAttachedWhenShown = false
    let mutable closeScheduledAfterStartup = false
    let mutable startupWasCompleteWhenClosed = false
    let mutable preventedCloseCount = 0
    let mutable sendsAfterDestroy = 0
    let mutable titleWritesAfterDestroy = 0
    let mutable closeHandler: (obj -> unit) option = None
    let mutable closedHandler: (unit -> unit) option = None
    let noop: obj = emitJsExpr () "((..._args) => {})"

    let send: obj =
        emitJsExpr
            (fun () ->
                if destroyed then
                    sendsAfterDestroy <- sendsAfterDestroy + 1
            )
            "((..._args) => $0())"

    let load (_: string) = JS.Constructors.Promise.resolve ()

    let onEvent (eventName: string) (handler: obj) =
        if eventName = "close" then
            closeHandler <- Some(unbox handler)
        elif eventName = "closed" then
            closedHandler <- Some(unbox handler)

    let onEventJs: obj =
        emitJsExpr onEvent "((eventName, handler) => $0(eventName, handler))"

    let closeAfterStartup () =
        startupWasCompleteWhenClosed <- isStartupComplete ()

        if startupWasCompleteWhenClosed && not destroyed then
            let preventedCloseCountBeforeAttempt = preventedCloseCount

            closeHandler.Value(
                createObj [
                    "preventDefault" ==> (fun () -> preventedCloseCount <- preventedCloseCount + 1)
                ]
            )

            if preventedCloseCount = preventedCloseCountBeforeAttempt then
                destroyed <- true
                closedHandler.Value()

    let scheduleCloseIfStartupComplete () =
        if not closeScheduledAfterStartup && isStartupComplete () then
            closeScheduledAfterStartup <- true
            scheduleAfterMicrotaskTurns microtaskTurns closeAfterStartup

    let show () =
        shown <- true
        lifecycleAttachedWhenShown <- closeHandler.IsSome && closedHandler.IsSome
        onShow ()

    let focus () =
        if scheduleFromFocus then
            scheduleCloseIfStartupComplete ()

    let windowObject =
        createObj [
            "id" ==> id
            "isDestroyed" ==> (fun () -> destroyed)
            "destroy" ==> (fun () -> destroyed <- true)
            "focus" ==> focus
            "show" ==> show
            "loadFile" ==> load
            "loadURL" ==> load
            "on" ==> onEventJs
            "webContents"
            ==> createObj [
                "send" ==> send
                "setWindowOpenHandler" ==> noop
                "on" ==> noop
                "openDevTools" ==> noop
            ]
        ]

    let setTitle (_value: string) =
        if destroyed then
            titleWritesAfterDestroy <- titleWritesAfterDestroy + 1
            failwith "Object has been destroyed"
        elif not scheduleFromFocus then
            scheduleCloseIfStartupComplete ()

    emitJsExpr
        (windowObject, setTitle)
        "Object.defineProperty($0, 'title', { configurable: true, get: () => '', set: value => $1(value) })"
    |> ignore

    let window = windowObject |> unbox<BrowserWindow>

    window,
    (fun () -> shown),
    (fun () -> lifecycleAttachedWhenShown),
    (fun () -> closeScheduledAfterStartup),
    (fun () -> startupWasCompleteWhenClosed),
    (fun () -> destroyed),
    (fun () -> sendsAfterDestroy),
    (fun () -> titleWritesAfterDestroy)

let private closingCurrentArcLoadTestWindow id isArcOpening =
    let mutable destroyed = false
    let mutable arcWasOpeningWhenClosed = false
    let mutable preventedCloseCount = 0
    let mutable sendsAfterDestroy = 0
    let mutable titleWritesAfterDestroy = 0
    let mutable closeHandler: (obj -> unit) option = None
    let mutable closedHandler: (unit -> unit) option = None
    let noop: obj = emitJsExpr () "((..._args) => {})"

    let send: obj =
        emitJsExpr
            (fun () ->
                if destroyed then
                    sendsAfterDestroy <- sendsAfterDestroy + 1
            )
            "((..._args) => $0())"

    let onEvent (eventName: string) (handler: obj) =
        if eventName = "close" then
            closeHandler <- Some(unbox handler)
        elif eventName = "closed" then
            closedHandler <- Some(unbox handler)

    let onEventJs: obj =
        emitJsExpr onEvent "((eventName, handler) => $0(eventName, handler))"

    let isDestroyed () =
        if not destroyed then
            arcWasOpeningWhenClosed <- isArcOpening ()

            closeHandler.Value(
                createObj [
                    "preventDefault" ==> (fun () -> preventedCloseCount <- preventedCloseCount + 1)
                ]
            )

            if preventedCloseCount = 0 then
                destroyed <- true
                closedHandler.Value()

        destroyed

    let windowObject =
        createObj [
            "id" ==> id
            "isDestroyed" ==> isDestroyed
            "destroy" ==> (fun () -> destroyed <- true)
            "focus" ==> ignore
            "on" ==> onEventJs
            "webContents" ==> createObj [ "send" ==> send; "on" ==> noop ]
        ]

    let setTitle (_value: string) =
        if destroyed then
            titleWritesAfterDestroy <- titleWritesAfterDestroy + 1
            failwith "Object has been destroyed"

    emitJsExpr
        (windowObject, setTitle)
        "Object.defineProperty($0, 'title', { configurable: true, get: () => '', set: value => $1(value) })"
    |> ignore

    let window = windowObject |> unbox<BrowserWindow>

    window,
    (fun () -> arcWasOpeningWhenClosed),
    (fun () -> destroyed),
    (fun () -> sendsAfterDestroy),
    (fun () -> titleWritesAfterDestroy)

let private expectRegistrationLoadFailure
    (expectedError: exn)
    (vaults: ArcVaults)
    (windowId: int)
    (isDestroyed: unit -> bool)
    (registration: unit -> JS.Promise<int>)
    =
    promise {
        let mutable capturedError: exn option = None

        try
            let! _ = registration ()
            ()
        with error ->
            capturedError <- Some error

        Vitest.expect(capturedError).toEqual (Some expectedError)
        Vitest.expect(vaults.Vaults.ContainsKey(windowId)).toBe (false)
        Vitest.expect(isDestroyed ()).toBe (true)
    }

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

let private tryGetDirtyStateMessage (args: obj array) =
    if args.Length = 2 && (string args.[0]).Contains("arcUnsavedChangesUpdate") then
        Some(unbox<bool> args.[1])
    else
        None

let private createRollbackTestWindow id failOnTitleWrite failOnDirtyReset =
    let mutable title = Swate.Electron.Shared.ApplicationVersion.windowTitle None
    let mutable titleWriteCount = 0
    let sentMessages = ResizeArray<obj array>()

    let send: obj =
        emitJsExpr
            (fun (args: obj array) ->
                sentMessages.Add(args)

                match tryGetDirtyStateMessage args, failOnDirtyReset with
                | Some false, Some error -> raise error
                | _ -> ()
            )
            "((...args) => $0(args))"

    let windowObject =
        createObj [
            "id" ==> id
            "isDestroyed" ==> (fun () -> false)
            "webContents" ==> createObj [ "send" ==> send ]
        ]

    let setTitle (value: string) =
        titleWriteCount <- titleWriteCount + 1

        match failOnTitleWrite with
        | Some(failingWrite, error) when titleWriteCount = failingWrite -> raise error
        | _ -> title <- value

    emitJsExpr
        (windowObject, (fun () -> title), setTitle)
        "Object.defineProperty($0, 'title', { configurable: true, get: () => $1(), set: value => $2(value) })"
    |> ignore

    windowObject |> unbox<BrowserWindow>, sentMessages

Vitest.describe (
    "ArcVaultHelper",
    fun () ->
        Vitest.afterEach (fun () -> resetElectronMock ())

        Vitest.test (
            "BrowserWindow factory preserves title property descriptors",
            fun () ->
                let mutable writtenTitle = ""
                let factoryWindow = createObj []

                emitJsExpr
                    (factoryWindow, fun (value: string) -> writtenTitle <- value)
                    "Object.defineProperty($0, 'title', { configurable: true, get: () => '', set: value => $1(value) })"
                |> ignore

                setBrowserWindowFactory (fun _ -> factoryWindow)

                let window = BrowserWindow()
                window.title <- "Descriptor-preserving title"

                Vitest.expect(writtenTitle).toBe ("Descriptor-preserving title")
        )

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
            "RegisterVault cleans up when renderer loading fails",
            fun () -> promise {
                let windowId = 11
                let loadError = exn "Expected renderer load failure"
                let window, isDestroyed, _ = registrationTestWindow windowId loadError ignore
                setBrowserWindowFactory (fun _ -> window :> obj)

                let vaults = ArcVaults()
                let mutable windowWasAliveWhenFailureWasReported = false

                do!
                    expectRegistrationLoadFailure
                        loadError
                        vaults
                        windowId
                        isDestroyed
                        (fun () ->
                            vaults.RegisterVault(
                                onFailureBeforeCleanup =
                                    fun _ -> windowWasAliveWhenFailureWasReported <- not (isDestroyed ())
                            )
                        )

                Vitest.expect(windowWasAliveWhenFailureWasReported).toBe (true)
            }
        )

        Vitest.test (
            "RegisterVault shows a successfully loaded renderer and keeps its vault registered",
            fun () -> promise {
                let windowId = 14
                let mutable constructorOptions: obj option = None

                let window, wasShown, isDestroyed, _ =
                    successfulRegistrationTestWindow windowId ignore ignore

                setBrowserWindowFactory (fun options ->
                    constructorOptions <- Some options
                    window :> obj
                )

                let vaults = ArcVaults()
                let! registeredWindowId = vaults.RegisterVault()

                Vitest.expect(registeredWindowId).toBe (windowId)
                Vitest.expect(constructorOptions.IsSome).toBe (true)
                Vitest.expect(constructorOptions.Value?show).toBe (false)
                Vitest.expect(wasShown ()).toBe (true)
                Vitest.expect(vaults.Vaults.ContainsKey(windowId)).toBe (true)
                Vitest.expect(isDestroyed ()).toBe (false)
            }
        )

        Vitest.test (
            "OpenOrFocusArc rejects an invalid target before creating a window for an occupied caller",
            fun () -> promise {
                let! invalidTargetPath = TestHelpers.createTempDirectoryAsync "swate-open-invalid-occupied-"
                let callingWindowId = 42
                let targetWindowId = 43
                let existingArcPath = "C:/already-open-validation-guard"
                let callingWindow = lifecycleTestWindow callingWindowId false ignore
                let callingVault = ArcVault(callingWindow)
                let existingArc = ARC("Existing ARC")
                let seededEntry = FileEntry.create ("existing.txt", "existing.txt", false)
                let mutable createdWindowCount = 0

                callingVault.path <- Some existingArcPath
                callingVault.SetArc(existingArc)
                callingVault.fileTree.Add(seededEntry.path, seededEntry)

                let targetWindow, _, _, _ =
                    successfulRegistrationTestWindow targetWindowId ignore ignore

                setBrowserWindowFactory (fun _ ->
                    createdWindowCount <- createdWindowCount + 1
                    targetWindow :> obj
                )

                let vaults = ArcVaults()
                vaults.Vaults.Add(callingWindowId, callingVault)
                let mutable openError: exn option = None

                try
                    try
                        let! _ = vaults.OpenOrFocusArc(callingWindowId, invalidTargetPath)
                        ()
                    with error ->
                        openError <- Some error

                    Vitest.expect(openError.IsSome).toBe (true)
                    Vitest.expect(openError.Value.Message).toContain (ARCtrl.ArcPathHelper.InvestigationFileName)
                    Vitest.expect(createdWindowCount).toBe (0)
                    Vitest.expect(vaults.TryGetVault(callingWindowId)).toEqual (Some callingVault)
                    Vitest.expect(callingVault.path).toEqual (Some existingArcPath)
                    Vitest.expect(callingVault.arc.IsSome).toBe (true)
                    Vitest.expect(callingVault.arc.Value).toBe (existingArc)
                    Vitest.expect(callingVault.watcher).toEqual (None)
                    Vitest.expect(callingVault.fileTree.Count).toBe (1)
                    Vitest.expect(callingVault.fileTree.ContainsKey(seededEntry.path)).toBe (true)

                    vaults.Vaults.Remove(callingWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync invalidTargetPath
                with error ->
                    vaults.Vaults.Remove(callingWindowId) |> ignore
                    vaults.Vaults.Remove(targetWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync invalidTargetPath
                    return raise error
            }
        )

        Vitest.test (
            "OpenOrFocusArc shows the new renderer before loading an existing ARC",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-open-or-focus-order-"
                    "Renderer First ARC"
                    ignore
                    (fun arcPath -> promise {
                        let callingWindowId = 26
                        let targetWindowId = 27
                        let vaults = ArcVaults()
                        let mutable createdWindowCount = 0
                        let mutable registeredVault: ArcVault option = None
                        let mutable vaultWasRegisteredWhenLoadStarted = false
                        let mutable vaultWasEmptyWhenShown = false

                        let window, wasShown, isDestroyed, lifecycleWasAttachedWhenLoadStarted =
                            successfulRegistrationTestWindow
                                targetWindowId
                                (fun () ->
                                    registeredVault <- vaults.TryGetVault(targetWindowId)
                                    vaultWasRegisteredWhenLoadStarted <- registeredVault.IsSome
                                )
                                (fun () ->
                                    vaultWasEmptyWhenShown <-
                                        registeredVault
                                        |> Option.exists (fun vault -> vault.path.IsNone && vault.arc.IsNone)
                                )

                        setBrowserWindowFactory (fun _ ->
                            createdWindowCount <- createdWindowCount + 1
                            window :> obj
                        )

                        try
                            Vitest.expect(vaults.Vaults.ContainsKey(callingWindowId)).toBe (false)

                            let! disposition = vaults.OpenOrFocusArc(callingWindowId, arcPath)
                            let normalizedArcPath = PathHelpers.normalizePath arcPath

                            match disposition with
                            | ArcOpenDisposition.OpenedInNewWindow openedPath ->
                                Vitest.expect(openedPath).toBe (normalizedArcPath)
                            | _ -> failwith "Expected the existing ARC to open in a new window."

                            Vitest.expect(createdWindowCount).toBe (1)
                            Vitest.expect(vaultWasRegisteredWhenLoadStarted).toBe (true)
                            Vitest.expect(lifecycleWasAttachedWhenLoadStarted ()).toBe (true)
                            Vitest.expect(wasShown ()).toBe (true)
                            Vitest.expect(vaultWasEmptyWhenShown).toBe (true)
                            Vitest.expect(isDestroyed ()).toBe (false)
                            Vitest.expect(registeredVault.IsSome).toBe (true)
                            Vitest.expect(vaults.Vaults.ContainsKey(targetWindowId)).toBe (true)
                            Vitest.expect(registeredVault.Value.path).toEqual (Some normalizedArcPath)
                            Vitest.expect(registeredVault.Value.arc.IsSome).toBe (true)

                            do! registeredVault.Value.StopFileWatcher()
                        with error ->
                            match registeredVault with
                            | Some vault -> do! vault.StopFileWatcher()
                            | None -> ()

                            return raise error
                    })
        )

        Vitest.test (
            "OpenOrFocusArc cleans up when the target window closes during ARC loading",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-open-or-focus-close-during-load-"
                    "Closing ARC"
                    ignore
                    (fun arcPath -> promise {
                        let callingWindowId = 28
                        let targetWindowId = 29
                        let vaults = ArcVaults()
                        let mutable createdWindowCount = 0
                        let mutable loadingVault: ArcVault option = None

                        let isArcOpening () =
                            loadingVault <- vaults.TryGetVault(targetWindowId)

                            loadingVault
                            |> Option.exists (fun vault ->
                                vault.path.IsSome && vault.arc.IsNone && vault.watcher.IsNone
                            )

                        let (window,
                             wasShown,
                             lifecycleWasAttached,
                             arcWasOpening,
                             isDestroyed,
                             sendsAfterDestroy,
                             titleWritesAfterDestroy) =
                            closingWhileArcLoadsRegistrationTestWindow targetWindowId isArcOpening 1 ignore

                        setBrowserWindowFactory (fun _ ->
                            createdWindowCount <- createdWindowCount + 1
                            window :> obj
                        )

                        let mutable capturedError: exn option = None

                        try
                            Vitest.expect(vaults.Vaults.ContainsKey(callingWindowId)).toBe (false)

                            try
                                let! _ = vaults.OpenOrFocusArc(callingWindowId, arcPath)
                                ()
                            with error ->
                                capturedError <- Some error

                            match capturedError with
                            | Some(ArcLoadCancelledException cancelledWindowId) ->
                                Vitest.expect(cancelledWindowId).toBe (targetWindowId)
                            | _ -> failwith "Expected an explicit ARC-load cancellation marker."

                            Vitest.expect(createdWindowCount).toBe (1)
                            Vitest.expect(wasShown ()).toBe (true)
                            Vitest.expect(lifecycleWasAttached ()).toBe (true)
                            Vitest.expect(arcWasOpening ()).toBe (true)
                            Vitest.expect(isDestroyed ()).toBe (true)
                            Vitest.expect(loadingVault.IsSome).toBe (true)
                            Vitest.expect(loadingVault.Value.watcher.IsNone).toBe (true)
                            Vitest.expect(vaults.Vaults.ContainsKey(targetWindowId)).toBe (false)
                            Vitest.expect(sendsAfterDestroy ()).toBe (0)
                            Vitest.expect(titleWritesAfterDestroy ()).toBe (0)
                        with error ->
                            match loadingVault with
                            | Some vault -> do! vault.StopFileWatcher()
                            | None -> ()

                            return raise error
                    })
        )

        Vitest.test (
            "OpenOrFocusArc cleans up when renderer loading fails",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-open-or-focus-renderer-failure-"
                    "Renderer Failure ARC"
                    ignore
                    (fun arcPath -> promise {
                        let callingWindowId = 30
                        let targetWindowId = 31
                        let loadError = exn "Expected renderer load failure"
                        let vaults = ArcVaults()
                        let mutable createdWindowCount = 0
                        let mutable vaultAtLoad: ArcVault option = None

                        let window, isDestroyed, lifecycleWasAttachedWhenLoadStarted =
                            registrationTestWindow
                                targetWindowId
                                loadError
                                (fun () -> vaultAtLoad <- vaults.TryGetVault(targetWindowId))

                        setBrowserWindowFactory (fun _ ->
                            createdWindowCount <- createdWindowCount + 1
                            window :> obj
                        )

                        let mutable capturedError: exn option = None

                        try
                            Vitest.expect(vaults.Vaults.ContainsKey(callingWindowId)).toBe (false)

                            try
                                let! _ = vaults.OpenOrFocusArc(callingWindowId, arcPath)
                                ()
                            with error ->
                                capturedError <- Some error

                            Vitest.expect(capturedError).toEqual (Some loadError)
                            Vitest.expect(createdWindowCount).toBe (1)
                            Vitest.expect(vaultAtLoad.IsSome).toBe (true)
                            Vitest.expect(lifecycleWasAttachedWhenLoadStarted ()).toBe (true)
                            Vitest.expect(vaultAtLoad.Value.path).toEqual (None)
                            Vitest.expect(vaultAtLoad.Value.arc).toEqual (None)
                            Vitest.expect(vaultAtLoad.Value.watcher.IsNone).toBe (true)
                            Vitest.expect(isDestroyed ()).toBe (true)
                            Vitest.expect(vaults.Vaults.ContainsKey(targetWindowId)).toBe (false)
                        with error ->
                            match vaultAtLoad with
                            | Some vault -> do! vault.StopFileWatcher()
                            | None -> ()

                            return raise error
                    })
        )

        Vitest.test (
            "openARCByPath preserves the open error when the native error dialog throws",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-ipc-dialog-failure-"
                    "Dialog Failure ARC"
                    ignore
                    (fun arcPath -> promise {
                        let originatingWindowId = 21
                        let targetWindowId = 22
                        let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                        let originatingVault = ArcVault(originatingWindow)
                        originatingVault.path <- Some "C:/already-open-dialog-failure"
                        originatingVault.SetArc(ARC("Originating ARC"))

                        let openError = exn "Expected renderer load failure"
                        let dialogError = exn "Expected native dialog failure"

                        let targetWindow, isTargetDestroyed, _ =
                            registrationTestWindow targetWindowId openError ignore

                        let mutable dialogCount = 0

                        setBrowserWindowFactory (fun _ -> targetWindow :> obj)
                        setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)

                        setShowMessageBox (fun _ _ ->
                            dialogCount <- dialogCount + 1
                            raise dialogError
                        )

                        ARC_VAULTS.Vaults.Add(originatingWindowId, originatingVault)

                        try
                            let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                            match! api.openARCByPath arcPath with
                            | Ok _ -> failwith "Expected ARC opening to fail."
                            | Error returnedError ->
                                Vitest.expect(returnedError).toBe (openError)
                                Vitest.expect(returnedError).not.toBe (dialogError)
                                Vitest.expect(dialogCount).toBe (1)
                                Vitest.expect(isTargetDestroyed ()).toBe (true)
                                Vitest.expect(ARC_VAULTS.Vaults.ContainsKey(targetWindowId)).toBe (false)

                            ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                        with error ->
                            ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                            ARC_VAULTS.Vaults.Remove(targetWindowId) |> ignore
                            return raise error
                    })
        )

        Vitest.test (
            "openARCByPath treats closing a newly opened window during ARC loading as cancellation",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-ipc-close-during-load-"
                    "Closing ARC"
                    ignore
                    (fun arcPath -> promise {
                        let originatingWindowId = 18
                        let targetWindowId = 20
                        let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                        let originatingVault = ArcVault(originatingWindow)
                        originatingVault.path <- Some "C:/already-open-arc"
                        originatingVault.SetArc(ARC("Originating ARC"))

                        let mutable loadingVault: ArcVault option = None

                        let isArcOpening () =
                            loadingVault <- ARC_VAULTS.TryGetVault(targetWindowId)

                            loadingVault
                            |> Option.exists (fun vault ->
                                vault.path.IsSome && vault.arc.IsNone && vault.watcher.IsNone
                            )

                        let (targetWindow,
                             wasShown,
                             lifecycleWasAttached,
                             arcWasOpening,
                             isDestroyed,
                             sendsAfterDestroy,
                             titleWritesAfterDestroy) =
                            closingWhileArcLoadsRegistrationTestWindow targetWindowId isArcOpening 1 ignore

                        let mutable dialogCount = 0

                        setBrowserWindowFactory (fun _ -> targetWindow :> obj)
                        setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)

                        setShowMessageBox (fun _ _ ->
                            dialogCount <- dialogCount + 1
                            createObj [ "response" ==> 0; "checkboxChecked" ==> false ]
                        )

                        ARC_VAULTS.Vaults.Add(originatingWindowId, originatingVault)

                        try
                            let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                            match! api.openARCByPath arcPath with
                            | Ok _ -> failwith "Expected closing the loading ARC window to cancel the operation."
                            | Error cancellation ->
                                match cancellation with
                                | ArcLoadCancelledException cancelledWindowId ->
                                    Vitest.expect(cancelledWindowId).toBe (targetWindowId)
                                | _ -> failwith "Expected an explicit ARC-load cancellation marker."

                                Vitest.expect(wasShown ()).toBe (true)
                                Vitest.expect(lifecycleWasAttached ()).toBe (true)
                                Vitest.expect(arcWasOpening ()).toBe (true)
                                Vitest.expect(isDestroyed ()).toBe (true)
                                Vitest.expect(loadingVault.IsSome).toBe (true)
                                Vitest.expect(loadingVault.Value.watcher.IsNone).toBe (true)
                                Vitest.expect(ARC_VAULTS.Vaults.ContainsKey(targetWindowId)).toBe (false)
                                Vitest.expect(sendsAfterDestroy ()).toBe (0)
                                Vitest.expect(titleWritesAfterDestroy ()).toBe (0)
                                Vitest.expect(dialogCount).toBe (0)
                                Vitest.expect(originatingWindow.isDestroyed ()).toBe (false)

                            ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                        with error ->
                            ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                            ARC_VAULTS.Vaults.Remove(targetWindowId) |> ignore
                            return raise error
                    })
        )

        Vitest.test (
            "openARCByPath treats closing a new window after startup but before file-tree publication as cancellation",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-ipc-close-during-open-tree-"
                    "Closing During Open Tree Scan"
                    ignore
                    (fun arcPath -> promise {
                        let originatingWindowId = 44
                        let targetWindowId = 45
                        let expectedPath = PathHelpers.normalizePath arcPath
                        let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                        let originatingVault = ArcVault(originatingWindow)
                        originatingVault.path <- Some "C:/already-open-tree-race"
                        originatingVault.SetArc(ARC("Originating ARC"))
                        let mutable createdWindowCount = 0
                        let mutable targetVault: ArcVault option = None
                        let mutable targetWasRegistered = false
                        let mutable pathWasAssigned = false
                        let mutable arcWasLoaded = false
                        let mutable watcherWasRunning = false
                        let mutable fileTreeWasEmpty = false

                        let isArcStartedBeforeFileTreePublication () =
                            targetVault <- ARC_VAULTS.TryGetVault(targetWindowId)

                            match targetVault with
                            | Some vault ->
                                targetWasRegistered <- true
                                pathWasAssigned <- vault.path = Some expectedPath
                                arcWasLoaded <- vault.arc.IsSome
                                watcherWasRunning <- vault.watcher.IsSome
                                fileTreeWasEmpty <- vault.fileTree.Count = 0

                                pathWasAssigned && arcWasLoaded && watcherWasRunning && fileTreeWasEmpty
                            | None -> false

                        let (targetWindow,
                             wasShown,
                             lifecycleWasAttached,
                             closeScheduledAfterStartup,
                             startupWasCompleteWhenClosed,
                             isTargetDestroyed,
                             sendsAfterDestroy,
                             titleWritesAfterDestroy) =
                            closingAfterStartupTestWindow
                                targetWindowId
                                isArcStartedBeforeFileTreePublication
                                true
                                8
                                ignore

                        let mutable dialogCount = 0

                        setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)

                        setBrowserWindowFactory (fun _ ->
                            createdWindowCount <- createdWindowCount + 1
                            targetWindow :> obj
                        )

                        setShowMessageBox (fun _ _ ->
                            dialogCount <- dialogCount + 1
                            createObj [ "response" ==> 0; "checkboxChecked" ==> false ]
                        )

                        ARC_VAULTS.Vaults.Add(originatingWindowId, originatingVault)

                        try
                            let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)
                            let! openResult = api.openARCByPath arcPath

                            Vitest.expect(createdWindowCount).toBe (1)
                            Vitest.expect(targetWasRegistered).toBe (true)
                            Vitest.expect(lifecycleWasAttached ()).toBe (true)
                            Vitest.expect(wasShown ()).toBe (true)
                            Vitest.expect(pathWasAssigned).toBe (true)
                            Vitest.expect(arcWasLoaded).toBe (true)
                            Vitest.expect(watcherWasRunning).toBe (true)
                            Vitest.expect(fileTreeWasEmpty).toBe (true)
                            Vitest.expect(closeScheduledAfterStartup ()).toBe (true)
                            Vitest.expect(startupWasCompleteWhenClosed ()).toBe (true)
                            Vitest.expect(isTargetDestroyed ()).toBe (true)
                            Vitest.expect(targetVault.IsSome).toBe (true)
                            Vitest.expect(targetVault.Value.fileTree.Count).toBe (0)
                            Vitest.expect(ARC_VAULTS.Vaults.ContainsKey(targetWindowId)).toBe (false)
                            Vitest.expect(sendsAfterDestroy ()).toBe (0)
                            Vitest.expect(titleWritesAfterDestroy ()).toBe (0)
                            Vitest.expect(dialogCount).toBe (0)

                            let! watcherStopped =
                                waitUntilWithEventLoopTurns (fun () -> targetVault.Value.watcher.IsNone) 20

                            Vitest.expect(watcherStopped).toBe (true)
                            Vitest.expect(targetVault.Value.watcher.IsNone).toBe (true)

                            match openResult with
                            | Ok _ -> failwith "Expected closing during the file-tree scan to cancel ARC opening."
                            | Error cancellation ->
                                match cancellation with
                                | ArcLoadCancelledException cancelledWindowId ->
                                    Vitest.expect(cancelledWindowId).toBe (targetWindowId)
                                | _ -> failwith "Expected an explicit ARC-load cancellation marker."

                            ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                        with error ->
                            match targetVault with
                            | Some vault -> do! vault.StopFileWatcher()
                            | None -> ()

                            ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                            ARC_VAULTS.Vaults.Remove(targetWindowId) |> ignore
                            return raise error
                    })
        )

        Vitest.test (
            "openARCByPath treats closing the current window during ARC loading as cancellation",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-ipc-current-close-during-load-"
                    "Closing Current ARC"
                    ignore
                    (fun arcPath -> promise {
                        let windowId = 32
                        let mutable loadingVault: ArcVault option = None
                        let mutable vaultExistedWhenClosed = false
                        let mutable pathWasAssignedWhenClosed = false
                        let mutable arcWasNoneWhenClosed = false
                        let mutable watcherWasAbsentWhenClosed = false

                        let isArcOpening () =
                            loadingVault <- ARC_VAULTS.TryGetVault(windowId)

                            match loadingVault with
                            | Some vault ->
                                vaultExistedWhenClosed <- true
                                pathWasAssignedWhenClosed <- vault.path = Some(PathHelpers.normalizePath arcPath)

                                arcWasNoneWhenClosed <- vault.arc.IsNone
                                watcherWasAbsentWhenClosed <- vault.watcher.IsNone
                            | None -> ()

                            vaultExistedWhenClosed
                            && pathWasAssignedWhenClosed
                            && arcWasNoneWhenClosed
                            && watcherWasAbsentWhenClosed

                        let (window, arcWasOpening, isDestroyed, sendsAfterDestroy, titleWritesAfterDestroy) =
                            closingCurrentArcLoadTestWindow windowId isArcOpening

                        let vault = ArcVault(window)
                        let mutable dialogCount = 0
                        let mutable createdWindowCount = 0

                        ARC_VAULTS.Vaults.Add(windowId, vault)
                        ARC_VAULTS.OnCloseWindow(window, vault, windowId)
                        setBrowserWindowFromWebContents (fun _ -> window :> obj)

                        setBrowserWindowFactory (fun _ ->
                            createdWindowCount <- createdWindowCount + 1
                            failwith "Same-window ARC opening must not create a BrowserWindow."
                        )

                        setShowMessageBox (fun _ _ ->
                            dialogCount <- dialogCount + 1
                            createObj [ "response" ==> 0; "checkboxChecked" ==> false ]
                        )

                        try
                            let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId windowId)

                            match! api.openARCByPath arcPath with
                            | Ok _ -> failwith "Expected closing the current ARC window to cancel the operation."
                            | Error cancellation ->
                                match cancellation with
                                | ArcLoadCancelledException cancelledWindowId ->
                                    Vitest.expect(cancelledWindowId).toBe (windowId)
                                | _ -> failwith "Expected an explicit ARC-load cancellation marker."

                                Vitest.expect(createdWindowCount).toBe (0)
                                Vitest.expect(arcWasOpening ()).toBe (true)
                                Vitest.expect(vaultExistedWhenClosed).toBe (true)
                                Vitest.expect(pathWasAssignedWhenClosed).toBe (true)
                                Vitest.expect(arcWasNoneWhenClosed).toBe (true)
                                Vitest.expect(watcherWasAbsentWhenClosed).toBe (true)
                                Vitest.expect(isDestroyed ()).toBe (true)
                                Vitest.expect(vault.watcher.IsNone).toBe (true)
                                Vitest.expect(ARC_VAULTS.Vaults.ContainsKey(windowId)).toBe (false)
                                Vitest.expect(sendsAfterDestroy ()).toBe (0)
                                Vitest.expect(titleWritesAfterDestroy ()).toBe (0)
                                Vitest.expect(dialogCount).toBe (0)
                        with error ->
                            ARC_VAULTS.Vaults.Remove(windowId) |> ignore
                            do! vault.StopFileWatcher()
                            return raise error
                    })
        )

        Vitest.test (
            "openARCByPath treats closing the current window after startup but before file-tree publication as cancellation",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-ipc-current-close-during-tree-"
                    "Closing Current ARC During Tree Scan"
                    ignore
                    (fun arcPath -> promise {
                        let windowId = 48
                        let expectedPath = PathHelpers.normalizePath arcPath
                        let mutable currentVault: ArcVault option = None
                        let mutable pathWasAssigned = false
                        let mutable arcWasLoaded = false
                        let mutable watcherWasRunning = false
                        let mutable fileTreeWasEmpty = false

                        let isArcStartedBeforeFileTreePublication () =
                            currentVault <- ARC_VAULTS.TryGetVault(windowId)

                            match currentVault with
                            | Some vault ->
                                pathWasAssigned <- vault.path = Some expectedPath
                                arcWasLoaded <- vault.arc.IsSome
                                watcherWasRunning <- vault.watcher.IsSome
                                fileTreeWasEmpty <- vault.fileTree.Count = 0

                                pathWasAssigned && arcWasLoaded && watcherWasRunning && fileTreeWasEmpty
                            | None -> false

                        let (window,
                             _,
                             _,
                             closeScheduledAfterStartup,
                             startupWasCompleteWhenClosed,
                             isDestroyed,
                             sendsAfterDestroy,
                             titleWritesAfterDestroy) =
                            closingAfterStartupTestWindow windowId isArcStartedBeforeFileTreePublication false 6 ignore

                        let vault = ArcVault(window)
                        let mutable dialogCount = 0
                        let mutable createdWindowCount = 0

                        ARC_VAULTS.Vaults.Add(windowId, vault)
                        ARC_VAULTS.OnCloseWindow(window, vault, windowId)
                        setBrowserWindowFromWebContents (fun _ -> window :> obj)

                        setBrowserWindowFactory (fun _ ->
                            createdWindowCount <- createdWindowCount + 1
                            failwith "Same-window ARC opening must not create a BrowserWindow."
                        )

                        setShowMessageBox (fun _ _ ->
                            dialogCount <- dialogCount + 1
                            createObj [ "response" ==> 0; "checkboxChecked" ==> false ]
                        )

                        try
                            let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId windowId)

                            match! api.openARCByPath arcPath with
                            | Ok _ -> failwith "Expected closing during the file-tree scan to cancel ARC opening."
                            | Error cancellation ->
                                match cancellation with
                                | ArcLoadCancelledException cancelledWindowId ->
                                    Vitest.expect(cancelledWindowId).toBe (windowId)
                                | _ -> failwith "Expected an explicit ARC-load cancellation marker."

                            Vitest.expect(createdWindowCount).toBe (0)
                            Vitest.expect(pathWasAssigned).toBe (true)
                            Vitest.expect(arcWasLoaded).toBe (true)
                            Vitest.expect(watcherWasRunning).toBe (true)
                            Vitest.expect(fileTreeWasEmpty).toBe (true)
                            Vitest.expect(closeScheduledAfterStartup ()).toBe (true)
                            Vitest.expect(startupWasCompleteWhenClosed ()).toBe (true)
                            Vitest.expect(isDestroyed ()).toBe (true)
                            Vitest.expect(currentVault.IsSome).toBe (true)
                            Vitest.expect(currentVault.Value.fileTree.Count).toBe (0)
                            Vitest.expect(ARC_VAULTS.Vaults.ContainsKey(windowId)).toBe (false)
                            Vitest.expect(sendsAfterDestroy ()).toBe (0)
                            Vitest.expect(titleWritesAfterDestroy ()).toBe (0)
                            Vitest.expect(dialogCount).toBe (0)
                        with error ->
                            ARC_VAULTS.Vaults.Remove(windowId) |> ignore
                            do! vault.StopFileWatcher()
                            return raise error
                    })
        )

        Vitest.test (
            "openARCByPath rejects an invalid ARC and shows one native error with folder context",
            fun () -> promise {
                let! folderPath = TestHelpers.createTempDirectoryAsync "swate-ipc-invalid-arc-"
                let windowId = 16
                let window = lifecycleTestWindow windowId false ignore
                let vault = ArcVault(window)
                let mutable dialogCount = 0
                let mutable dialogOptions: obj option = None

                ARC_VAULTS.Vaults.Add(windowId, vault)
                setBrowserWindowFromWebContents (fun _ -> window :> obj)

                setShowMessageBox (fun _ options ->
                    dialogCount <- dialogCount + 1
                    dialogOptions <- Some options
                    createObj [ "response" ==> 0; "checkboxChecked" ==> false ]
                )

                try
                    let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId windowId)

                    match! api.openARCByPath folderPath with
                    | Ok _ -> failwith "Expected a folder without an investigation file to be rejected."
                    | Error error ->
                        Vitest.expect(error :? ArcLoadCancelledException).toBe (false)
                        Vitest.expect(error.Message).toContain (ARCtrl.ArcPathHelper.InvestigationFileName)
                        Vitest.expect(vault.path).toEqual (None)
                        Vitest.expect(vault.arc).toEqual (None)
                        Vitest.expect(dialogCount).toBe (1)
                        Vitest.expect(dialogOptions.IsSome).toBe (true)
                        Vitest.expect(dialogOptions.Value?message).toBe ("The ARC could not be opened.")

                        Vitest
                            .expect(dialogOptions.Value?detail)
                            .toContain ($"Folder: {PathHelpers.normalizePath folderPath}")

                        Vitest.expect(dialogOptions.Value?detail).toContain (ARCtrl.ArcPathHelper.InvestigationFileName)

                    ARC_VAULTS.Vaults.Remove(windowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync folderPath
                with error ->
                    ARC_VAULTS.Vaults.Remove(windowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync folderPath
                    return raise error
            }
        )

        Vitest.test (
            "openARC omits folder context when the dialog returns multiple paths",
            fun () -> promise {
                let windowId = 17
                let window = lifecycleTestWindow windowId false ignore
                let mutable dialogOptions: obj option = None

                setBrowserWindowFromWebContents (fun _ -> window :> obj)

                setShowOpenDialog (fun _ _ ->
                    createObj [
                        "canceled" ==> false
                        "filePaths" ==> [| "C:/first"; "C:/second" |]
                    ]
                )

                setShowMessageBox (fun _ options ->
                    dialogOptions <- Some options
                    createObj [ "response" ==> 0; "checkboxChecked" ==> false ]
                )

                let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId windowId)

                match! api.openARC () with
                | Ok _ -> failwith "Expected multiple dialog paths to be rejected."
                | Error error ->
                    Vitest.expect(error.Message).toContain ("Not exactly one path")
                    Vitest.expect(dialogOptions.IsSome).toBe (true)
                    Vitest.expect(dialogOptions.Value?detail).toContain ("Not exactly one path")
                    Vitest.expect(dialogOptions.Value?detail).not.toContain ("Folder:")
            }
        )

        Vitest.test (
            "openARC preserves the multi-selection error when the native error dialog throws",
            fun () -> promise {
                let windowId = 33
                let window = lifecycleTestWindow windowId false ignore
                let dialogError = exn "Expected native dialog failure"
                let mutable dialogCount = 0

                setBrowserWindowFromWebContents (fun _ -> window :> obj)

                setShowOpenDialog (fun _ _ ->
                    createObj [
                        "canceled" ==> false
                        "filePaths" ==> [| "C:/first"; "C:/second" |]
                    ]
                )

                setShowMessageBox (fun _ _ ->
                    dialogCount <- dialogCount + 1
                    raise dialogError
                )

                let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId windowId)

                match! api.openARC () with
                | Ok _ -> return failwith "Expected multiple dialog paths to be rejected."
                | Error returnedError ->
                    Vitest.expect(returnedError.Message).toBe ("Not exactly one path")
                    Vitest.expect(returnedError).not.toBe (dialogError)
                    Vitest.expect(dialogCount).toBe (1)
            }
        )

        Vitest.test (
            "openARC preserves the folder-picker error when the native error dialog throws",
            fun () -> promise {
                let windowId = 34
                let window = lifecycleTestWindow windowId false ignore
                let openDialogError = exn "Expected folder-picker failure"
                let messageDialogError = exn "Expected native dialog failure"
                let mutable dialogCount = 0

                setBrowserWindowFromWebContents (fun _ -> window :> obj)
                setShowOpenDialog (fun _ _ -> raise openDialogError)

                setShowMessageBox (fun _ _ ->
                    dialogCount <- dialogCount + 1
                    raise messageDialogError
                )

                let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId windowId)

                match! api.openARC () with
                | Ok _ -> return failwith "Expected the throwing folder picker to fail ARC opening."
                | Error returnedError ->
                    Vitest.expect(returnedError).toBe (openDialogError)
                    Vitest.expect(returnedError).not.toBe (messageDialogError)
                    Vitest.expect(dialogCount).toBe (1)
            }
        )

        Vitest.test (
            "createARC resolves Cancelled when the folder picker is cancelled",
            fun () -> promise {
                let originatingWindowId = 35
                let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                let mutable createdWindowCount = 0

                setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)

                setBrowserWindowFactory (fun _ ->
                    createdWindowCount <- createdWindowCount + 1
                    failwith "Folder-picker cancellation must not create a BrowserWindow."
                )

                setShowOpenDialog (fun _ _ -> createObj [ "canceled" ==> true; "filePaths" ==> [||] ])

                let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                let request: CreateArcRequest = {
                    identifier = "Cancelled ARC"
                    initGit = false
                }

                match! api.createARC request with
                | Ok CreateArcOutcome.Cancelled -> Vitest.expect(createdWindowCount).toBe (0)
                | Ok outcome -> return failwithf "Expected Cancelled, received %A." outcome
                | Error error -> return failwithf "Expected cancellation to be non-error, received %s." error.Message
            }
        )

        Vitest.test (
            "createARC resolves FocusedExisting without creating or writing another ARC",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-ipc-create-focus-existing-"
                let originatingWindowId = 40
                let existingWindowId = 41
                let identifier = "Already Open ARC"
                let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                let mutable focusCount = 0
                let mutable createdWindowCount = 0

                let existingWindow =
                    focusTrackingTestWindow existingWindowId (fun () -> focusCount <- focusCount + 1)

                let expectedPath =
                    ARCtrl.ArcPathHelper.combine rootPath identifier |> PathHelpers.normalizePath

                let existingVault = ArcVault(existingWindow)
                existingVault.path <- Some expectedPath
                existingVault.SetArc(ARC(identifier))

                setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)

                setBrowserWindowFactory (fun _ ->
                    createdWindowCount <- createdWindowCount + 1
                    failwith "Focusing an existing ARC must not create a BrowserWindow."
                )

                setShowOpenDialog (fun _ _ -> createObj [ "canceled" ==> false; "filePaths" ==> [| rootPath |] ])
                ARC_VAULTS.Vaults.Add(existingWindowId, existingVault)

                try
                    let! existedBeforeCreate = TestHelpers.pathExistsAsync expectedPath
                    Vitest.expect(existedBeforeCreate).toBe (false)

                    let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                    let request: CreateArcRequest = {
                        identifier = identifier
                        initGit = false
                    }

                    match! api.createARC request with
                    | Ok(CreateArcOutcome.FocusedExisting focusedPath) -> Vitest.expect(focusedPath).toBe (expectedPath)
                    | Ok outcome -> failwithf "Expected FocusedExisting, received %A." outcome
                    | Error error -> failwithf "Expected focusing to be non-error, received %s." error.Message

                    Vitest.expect(focusCount).toBe (1)
                    Vitest.expect(createdWindowCount).toBe (0)
                    Vitest.expect(ARC_VAULTS.TryGetVault(existingWindowId)).toEqual (Some existingVault)

                    let! existsAfterCreate = TestHelpers.pathExistsAsync expectedPath
                    Vitest.expect(existsAfterCreate).toBe (false)

                    ARC_VAULTS.Vaults.Remove(existingWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    ARC_VAULTS.Vaults.Remove(existingWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "createARC resolves Created with the normalized path after successful creation",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-ipc-create-success-"
                let originatingWindowId = 36
                let targetWindowId = 37
                let identifier = "Successful ARC"
                let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                let originatingVault = ArcVault(originatingWindow)
                originatingVault.path <- Some "C:/already-open-create-success"
                originatingVault.SetArc(ARC("Originating ARC"))
                let mutable createdWindowCount = 0
                let mutable targetVault: ArcVault option = None

                let targetWindow, wasShown, isTargetDestroyed, lifecycleWasAttachedWhenLoadStarted =
                    successfulRegistrationTestWindow
                        targetWindowId
                        (fun () -> targetVault <- ARC_VAULTS.TryGetVault(targetWindowId))
                        ignore

                setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)

                setBrowserWindowFactory (fun _ ->
                    createdWindowCount <- createdWindowCount + 1
                    targetWindow :> obj
                )

                setShowOpenDialog (fun _ _ -> createObj [ "canceled" ==> false; "filePaths" ==> [| rootPath |] ])
                ARC_VAULTS.Vaults.Add(originatingWindowId, originatingVault)

                try
                    let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                    let request: CreateArcRequest = {
                        identifier = identifier
                        initGit = false
                    }

                    let expectedPath =
                        ARCtrl.ArcPathHelper.combine rootPath identifier |> PathHelpers.normalizePath

                    match! api.createARC request with
                    | Ok(CreateArcOutcome.Created createdPath) -> Vitest.expect(createdPath).toBe (expectedPath)
                    | Ok outcome -> failwithf "Expected Created, received %A." outcome
                    | Error error -> failwithf "Expected successful creation, received %s." error.Message

                    Vitest.expect(createdWindowCount).toBe (1)
                    Vitest.expect(wasShown ()).toBe (true)
                    Vitest.expect(lifecycleWasAttachedWhenLoadStarted ()).toBe (true)
                    Vitest.expect(isTargetDestroyed ()).toBe (false)
                    Vitest.expect(targetVault.IsSome).toBe (true)
                    Vitest.expect(targetVault.Value.path).toEqual (Some expectedPath)
                    Vitest.expect(targetVault.Value.watcher.IsSome).toBe (true)

                    let! arcExists = TestHelpers.pathExistsAsync expectedPath
                    Vitest.expect(arcExists).toBe (true)

                    do! targetVault.Value.StopFileWatcher()
                    ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                    ARC_VAULTS.Vaults.Remove(targetWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    match targetVault with
                    | Some vault -> do! vault.StopFileWatcher()
                    | None -> ()

                    ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                    ARC_VAULTS.Vaults.Remove(targetWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "createARC resolves CreatedButClosed when the new window closes during post-write loading",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-ipc-create-close-during-load-"
                let originatingWindowId = 38
                let targetWindowId = 39
                let identifier = "Created Then Closed ARC"
                let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                let originatingVault = ArcVault(originatingWindow)
                originatingVault.path <- Some "C:/already-open-create-close"
                originatingVault.SetArc(ARC("Originating ARC"))

                let expectedPath =
                    ARCtrl.ArcPathHelper.combine rootPath identifier |> PathHelpers.normalizePath

                let mutable createdWindowCount = 0
                let mutable loadingVault: ArcVault option = None
                let mutable targetWasRegistered = false
                let mutable targetPathWasAssigned = false
                let mutable creationReachedPostWriteLoad = false
                let mutable watcherWasAbsent = false
                let mutable vaultWasEmptyWhenShown = false

                let isArcLoadingAfterWrite () =
                    loadingVault <- ARC_VAULTS.TryGetVault(targetWindowId)

                    match loadingVault with
                    | Some vault ->
                        targetWasRegistered <- true
                        targetPathWasAssigned <- vault.path = Some expectedPath
                        creationReachedPostWriteLoad <- vault.arc.IsSome
                        watcherWasAbsent <- vault.watcher.IsNone

                        targetWasRegistered
                        && targetPathWasAssigned
                        && creationReachedPostWriteLoad
                        && watcherWasAbsent
                        && not vault.hasUnsavedArcChanges
                    | None -> false

                let (targetWindow,
                     wasShown,
                     lifecycleWasAttached,
                     arcWasLoadingAfterWrite,
                     isTargetDestroyed,
                     sendsAfterDestroy,
                     titleWritesAfterDestroy) =
                    closingWhileArcLoadsRegistrationTestWindow
                        targetWindowId
                        isArcLoadingAfterWrite
                        // The first destroyed check comes from the initial in-memory SetArc before the disk write.
                        // Close on a later check, once reload has cleared the unsaved flag but before the watcher starts.
                        2
                        (fun () ->
                            vaultWasEmptyWhenShown <-
                                ARC_VAULTS.TryGetVault(targetWindowId)
                                |> Option.exists (fun vault -> vault.path.IsNone && vault.arc.IsNone)
                        )

                setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)

                setBrowserWindowFactory (fun _ ->
                    createdWindowCount <- createdWindowCount + 1
                    targetWindow :> obj
                )

                setShowOpenDialog (fun _ _ -> createObj [ "canceled" ==> false; "filePaths" ==> [| rootPath |] ])
                ARC_VAULTS.Vaults.Add(originatingWindowId, originatingVault)

                try
                    let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                    let request: CreateArcRequest = {
                        identifier = identifier
                        initGit = false
                    }

                    match! api.createARC request with
                    | Ok(CreateArcOutcome.CreatedButClosed createdPath) ->
                        Vitest.expect(createdPath).toBe (expectedPath)
                    | Ok outcome -> failwithf "Expected CreatedButClosed, received %A." outcome
                    | Error error -> failwithf "Expected close-after-create to be non-error, received %s." error.Message

                    Vitest.expect(createdWindowCount).toBe (1)
                    Vitest.expect(targetWasRegistered).toBe (true)
                    Vitest.expect(lifecycleWasAttached ()).toBe (true)
                    Vitest.expect(wasShown ()).toBe (true)
                    Vitest.expect(vaultWasEmptyWhenShown).toBe (true)
                    Vitest.expect(targetPathWasAssigned).toBe (true)
                    Vitest.expect(creationReachedPostWriteLoad).toBe (true)
                    Vitest.expect(arcWasLoadingAfterWrite ()).toBe (true)
                    Vitest.expect(isTargetDestroyed ()).toBe (true)
                    Vitest.expect(loadingVault.IsSome).toBe (true)
                    Vitest.expect(loadingVault.Value.watcher.IsNone).toBe (true)
                    Vitest.expect(ARC_VAULTS.Vaults.ContainsKey(targetWindowId)).toBe (false)
                    Vitest.expect(sendsAfterDestroy ()).toBe (0)
                    Vitest.expect(titleWritesAfterDestroy ()).toBe (0)

                    let! arcDirectoryExists = TestHelpers.pathExistsAsync expectedPath

                    let investigationPath =
                        ARCtrl.ArcPathHelper.combine expectedPath ARCtrl.ArcPathHelper.InvestigationFileName

                    let! investigationFileExists = TestHelpers.pathExistsAsync investigationPath
                    Vitest.expect(arcDirectoryExists).toBe (true)
                    Vitest.expect(investigationFileExists).toBe (true)

                    ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    match loadingVault with
                    | Some vault -> do! vault.StopFileWatcher()
                    | None -> ()

                    ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                    ARC_VAULTS.Vaults.Remove(targetWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "createARC resolves CreatedButClosed when the new window closes after startup but before file-tree publication",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-ipc-create-close-during-tree-"
                let originatingWindowId = 46
                let targetWindowId = 47
                let identifier = "Created During Tree Scan ARC"
                let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                let originatingVault = ArcVault(originatingWindow)
                originatingVault.path <- Some "C:/already-open-create-tree-race"
                originatingVault.SetArc(ARC("Originating ARC"))

                let expectedPath =
                    ARCtrl.ArcPathHelper.combine rootPath identifier |> PathHelpers.normalizePath

                let mutable createdWindowCount = 0
                let mutable targetVault: ArcVault option = None
                let mutable targetWasRegistered = false
                let mutable pathWasAssigned = false
                let mutable arcWasLoaded = false
                let mutable watcherWasRunning = false
                let mutable fileTreeWasEmpty = false
                let mutable vaultWasEmptyWhenShown = false

                let isArcStartedBeforeFileTreePublication () =
                    targetVault <- ARC_VAULTS.TryGetVault(targetWindowId)

                    match targetVault with
                    | Some vault ->
                        targetWasRegistered <- true
                        pathWasAssigned <- vault.path = Some expectedPath
                        arcWasLoaded <- vault.arc.IsSome
                        watcherWasRunning <- vault.watcher.IsSome
                        fileTreeWasEmpty <- vault.fileTree.Count = 0

                        pathWasAssigned
                        && arcWasLoaded
                        && watcherWasRunning
                        && fileTreeWasEmpty
                        && not vault.hasUnsavedArcChanges
                    | None -> false

                let (targetWindow,
                     wasShown,
                     lifecycleWasAttached,
                     closeScheduledAfterStartup,
                     startupWasCompleteWhenClosed,
                     isTargetDestroyed,
                     sendsAfterDestroy,
                     titleWritesAfterDestroy) =
                    closingAfterStartupTestWindow
                        targetWindowId
                        isArcStartedBeforeFileTreePublication
                        true
                        8
                        (fun () ->
                            vaultWasEmptyWhenShown <-
                                ARC_VAULTS.TryGetVault(targetWindowId)
                                |> Option.exists (fun vault -> vault.path.IsNone && vault.arc.IsNone)
                        )

                setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)

                setBrowserWindowFactory (fun _ ->
                    createdWindowCount <- createdWindowCount + 1
                    targetWindow :> obj
                )

                setShowOpenDialog (fun _ _ -> createObj [ "canceled" ==> false; "filePaths" ==> [| rootPath |] ])
                ARC_VAULTS.Vaults.Add(originatingWindowId, originatingVault)

                try
                    let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                    let request: CreateArcRequest = {
                        identifier = identifier
                        initGit = false
                    }

                    match! api.createARC request with
                    | Ok(CreateArcOutcome.CreatedButClosed createdPath) ->
                        Vitest.expect(createdPath).toBe (expectedPath)
                    | Ok outcome -> failwithf "Expected CreatedButClosed, received %A." outcome
                    | Error error -> failwithf "Expected close-after-create to be non-error, received %s." error.Message

                    Vitest.expect(createdWindowCount).toBe (1)
                    Vitest.expect(targetWasRegistered).toBe (true)
                    Vitest.expect(lifecycleWasAttached ()).toBe (true)
                    Vitest.expect(wasShown ()).toBe (true)
                    Vitest.expect(vaultWasEmptyWhenShown).toBe (true)
                    Vitest.expect(pathWasAssigned).toBe (true)
                    Vitest.expect(arcWasLoaded).toBe (true)
                    Vitest.expect(watcherWasRunning).toBe (true)
                    Vitest.expect(fileTreeWasEmpty).toBe (true)
                    Vitest.expect(closeScheduledAfterStartup ()).toBe (true)
                    Vitest.expect(startupWasCompleteWhenClosed ()).toBe (true)
                    Vitest.expect(isTargetDestroyed ()).toBe (true)
                    Vitest.expect(targetVault.IsSome).toBe (true)
                    Vitest.expect(targetVault.Value.fileTree.Count).toBe (0)
                    Vitest.expect(ARC_VAULTS.Vaults.ContainsKey(targetWindowId)).toBe (false)
                    Vitest.expect(sendsAfterDestroy ()).toBe (0)
                    Vitest.expect(titleWritesAfterDestroy ()).toBe (0)

                    let! arcDirectoryExists = TestHelpers.pathExistsAsync expectedPath

                    let investigationPath =
                        ARCtrl.ArcPathHelper.combine expectedPath ARCtrl.ArcPathHelper.InvestigationFileName

                    let! investigationFileExists = TestHelpers.pathExistsAsync investigationPath
                    Vitest.expect(arcDirectoryExists).toBe (true)
                    Vitest.expect(investigationFileExists).toBe (true)

                    ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    match targetVault with
                    | Some vault -> do! vault.StopFileWatcher()
                    | None -> ()

                    ARC_VAULTS.Vaults.Remove(originatingWindowId) |> ignore
                    ARC_VAULTS.Vaults.Remove(targetWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "createARC resolves Error when new-window creation fails",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-ipc-create-failure-"
                let originatingWindowId = 23
                let targetWindowId = 24
                let identifier = "Creation Failure ARC"
                let createError = exn "Expected new ARC renderer load failure"
                let originatingWindow = lifecycleTestWindow originatingWindowId false ignore

                let targetWindow, isTargetDestroyed, _ =
                    registrationTestWindow targetWindowId createError ignore

                setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)
                setBrowserWindowFactory (fun _ -> targetWindow :> obj)

                setShowOpenDialog (fun _ _ -> createObj [ "canceled" ==> false; "filePaths" ==> [| rootPath |] ])

                try
                    let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                    let request: Swate.Electron.Shared.IPCTypes.CreateArcRequest = {
                        identifier = identifier
                        initGit = false
                    }

                    match! api.createARC request with
                    | Ok _ -> failwith "Expected new ARC creation to fail."
                    | Error returnedError ->
                        Vitest.expect(returnedError).toBe (createError)
                        Vitest.expect(isTargetDestroyed ()).toBe (true)
                        Vitest.expect(ARC_VAULTS.Vaults.ContainsKey(targetWindowId)).toBe (false)

                    let requestedArcPath = join [| rootPath; identifier |]
                    let! requestedArcExists = TestHelpers.pathExistsAsync requestedArcPath
                    Vitest.expect(requestedArcExists).toBe (false)

                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    ARC_VAULTS.Vaults.Remove(targetWindowId) |> ignore
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "createARC resolves Error when the folder dialog throws",
            fun () -> promise {
                let originatingWindowId = 25
                let originatingWindow = lifecycleTestWindow originatingWindowId false ignore
                let dialogError = exn "Expected folder dialog failure"

                setBrowserWindowFromWebContents (fun _ -> originatingWindow :> obj)
                setShowOpenDialog (fun _ _ -> raise dialogError)

                let api = Main.IPC.ArcVaultsApi.api (ipcEventWithSenderId originatingWindowId)

                let request: Swate.Electron.Shared.IPCTypes.CreateArcRequest = {
                    identifier = "Dialog Failure ARC"
                    initGit = false
                }

                match! api.createARC request with
                | Ok _ -> return failwith "Expected the throwing folder dialog to fail ARC creation."
                | Error returnedError -> Vitest.expect(returnedError).toBe (dialogError)
            }
        )

        Vitest.test (
            "RegisterVaultWithNewArc loads and shows the renderer before creating the ARC",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-registration-create-order-"
                let arcPath = join [| rootPath; "new-arc" |]
                let mutable vaultsRef: ArcVaults option = None
                let mutable vaultWasEmptyWhenShown = false
                let mutable arcDirectoryWasAbsentWhenShown = false
                let mutable registeredVault: ArcVault option = None

                try
                    let windowId = 13

                    let window, wasShown, isDestroyed, lifecycleWasAttachedWhenLoadStarted =
                        successfulRegistrationTestWindow
                            windowId
                            ignore
                            (fun () ->
                                registeredVault <- vaultsRef.Value.TryGetVault(windowId)

                                vaultWasEmptyWhenShown <-
                                    registeredVault
                                    |> Option.exists (fun vault -> vault.path.IsNone && vault.arc.IsNone)

                                arcDirectoryWasAbsentWhenShown <- not (existsSync arcPath)
                            )

                    let vaults = ArcVaults()
                    vaultsRef <- Some vaults
                    setBrowserWindowFactory (fun _ -> window :> obj)

                    let! registeredWindowId = vaults.RegisterVaultWithNewArc(arcPath, "New ARC")

                    Vitest.expect(registeredWindowId).toBe (windowId)
                    Vitest.expect(lifecycleWasAttachedWhenLoadStarted ()).toBe (true)
                    Vitest.expect(wasShown ()).toBe (true)
                    Vitest.expect(vaultWasEmptyWhenShown).toBe (true)
                    Vitest.expect(arcDirectoryWasAbsentWhenShown).toBe (true)
                    Vitest.expect(existsSync arcPath).toBe (true)
                    Vitest.expect(isDestroyed ()).toBe (false)
                    Vitest.expect(registeredVault.IsSome).toBe (true)
                    Vitest.expect(vaults.Vaults.ContainsKey(windowId)).toBe (true)

                    do! registeredVault.Value.StopFileWatcher()
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    match registeredVault with
                    | Some vault -> do! vault.StopFileWatcher()
                    | None -> ()

                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "RegisterVaultWithNewArc renderer-load failure does not create an ARC",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-registration-create-"
                let arcPath = join [| rootPath; "new-arc" |]
                let mutable initializedVault: ArcVault option = None
                let mutable vaultsRef: ArcVaults option = None

                try
                    let windowId = 19
                    let loadError = exn "Expected renderer load failure"

                    let window, isDestroyed, _ =
                        registrationTestWindow
                            windowId
                            loadError
                            (fun () -> initializedVault <- vaultsRef.Value.TryGetVault(windowId))

                    setBrowserWindowFactory (fun _ -> window :> obj)

                    let vaults = ArcVaults()
                    vaultsRef <- Some vaults

                    do!
                        expectRegistrationLoadFailure
                            loadError
                            vaults
                            windowId
                            isDestroyed
                            (fun () -> vaults.RegisterVaultWithNewArc(arcPath, "New ARC"))

                    Vitest.expect(initializedVault.IsSome).toBe (true)
                    Vitest.expect(initializedVault.Value.watcher.IsNone).toBe (true)

                    let! arcDirectoryExists = TestHelpers.pathExistsAsync arcPath
                    Vitest.expect(arcDirectoryExists).toBe (false)

                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
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

        Vitest.test (
            "CreateARC write failure restores the current empty vault and leaves it reusable",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-create-write-rollback-"
                let blockedParent = join [| rootPath; "not-a-directory" |]
                let failedArcPath = join [| blockedParent; "failed-arc" |]
                let reusableArcPath = join [| rootPath; "reusable-arc" |]
                let windowId = 71
                let window, sentMessages = createRollbackTestWindow windowId None None
                let vault = ArcVault(window)
                let vaults = ArcVaults()
                let seededEntry = FileEntry.create ("seeded.txt", "seeded.txt", false)
                let mutable createdWindowCount = 0

                vault.fileTree.Add(seededEntry.path, seededEntry)
                vaults.Vaults.Add(windowId, vault)
                do! writeTextFileAsync blockedParent "This file prevents creation of a nested ARC directory."

                setBrowserWindowFactory (fun _ ->
                    createdWindowCount <- createdWindowCount + 1
                    failwith "A reusable current vault must not create another BrowserWindow."
                )

                try
                    let mutable creationError: exn option = None

                    try
                        do! vault.CreateARC(failedArcPath, "Failed ARC")
                    with error ->
                        creationError <- Some error

                    Vitest.expect(creationError.IsSome).toBe (true)
                    Vitest.expect(creationError.Value.Message).toContain ("Could not write ARC")
                    Vitest.expect(vault.path).toEqual (None)
                    Vitest.expect(vault.arc).toEqual (None)
                    Vitest.expect(vault.watcher).toEqual (None)
                    Vitest.expect(vault.fileTree.Count).toBe (0)
                    Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                    Vitest.expect(vault.window.title).toBe (Swate.Electron.Shared.ApplicationVersion.windowTitle None)

                    Vitest
                        .expect(
                            sentMessages
                            |> Seq.exists (fun message ->
                                JS.JSON.stringify(message).Contains(PathHelpers.normalizePath failedArcPath)
                            )
                        )
                        .toBe (false)

                    let dirtyStateMessages =
                        sentMessages |> Seq.choose tryGetDirtyStateMessage |> Seq.toArray

                    Vitest.expect(dirtyStateMessages).toEqual ([| true; false |])

                    match! vaults.CreateOrFocusArc(windowId, reusableArcPath, "Reusable ARC") with
                    | ArcOpenDisposition.CreatedInCurrent createdPath ->
                        Vitest.expect(createdPath).toBe (PathHelpers.normalizePath reusableArcPath)
                    | disposition -> failwithf "Expected the current vault to be reused, received %A." disposition

                    Vitest.expect(createdWindowCount).toBe (0)
                    Vitest.expect(vault.path).toEqual (Some(PathHelpers.normalizePath reusableArcPath))
                    Vitest.expect(vault.arc.IsSome).toBe (true)
                    Vitest.expect(vault.watcher.IsSome).toBe (true)

                    do! vault.StopFileWatcher()
                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! vault.StopFileWatcher()
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "CreateARC preserves the write failure when rollback dirty-state notification throws",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-create-notification-rollback-"
                let blockedParent = join [| rootPath; "not-a-directory" |]
                let failedArcPath = join [| blockedParent; "failed-arc" |]
                let rollbackError = exn "Expected rollback renderer notification failure."
                let window, sentMessages = createRollbackTestWindow 73 None (Some rollbackError)
                let vault = ArcVault(window)
                let seededEntry = FileEntry.create ("seeded.txt", "seeded.txt", false)
                vault.fileTree.Add(seededEntry.path, seededEntry)
                do! writeTextFileAsync blockedParent "This file prevents creation of a nested ARC directory."

                try
                    let mutable creationError: exn option = None

                    try
                        do! vault.CreateARC(failedArcPath, "Failed ARC")
                    with error ->
                        creationError <- Some error

                    Vitest.expect(creationError.IsSome).toBe (true)
                    Vitest.expect(vault.path).toEqual (None)
                    Vitest.expect(vault.arc).toEqual (None)
                    Vitest.expect(vault.watcher).toEqual (None)
                    Vitest.expect(vault.fileTree.Count).toBe (0)
                    Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                    Vitest.expect(creationError.Value).not.toBe (rollbackError)
                    Vitest.expect(creationError.Value.Message).toContain ("Could not write ARC")
                    Vitest.expect(creationError.Value.Message).not.toContain (rollbackError.Message)

                    let dirtyStateMessages =
                        sentMessages |> Seq.choose tryGetDirtyStateMessage |> Seq.toArray

                    Vitest.expect(dirtyStateMessages).toEqual ([| true; false |])

                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! vault.StopFileWatcher()
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "CreateARC Startup failure preserves the written ARC while restoring the empty vault",
            fun () -> promise {
                let! rootPath = TestHelpers.createTempDirectoryAsync "swate-create-startup-rollback-"
                let arcPath = join [| rootPath; "written-arc" |]
                let startupError = exn "Expected Startup title failure."
                let window, sentMessages = createRollbackTestWindow 72 (Some(3, startupError)) None
                let vault = ArcVault(window)
                let seededEntry = FileEntry.create ("seeded.txt", "seeded.txt", false)
                vault.fileTree.Add(seededEntry.path, seededEntry)

                try
                    let mutable creationError: exn option = None

                    try
                        do! vault.CreateARC(arcPath, "Persisted ARC")
                    with error ->
                        creationError <- Some error

                    Vitest.expect(creationError).toEqual (Some startupError)
                    Vitest.expect(vault.path).toEqual (None)
                    Vitest.expect(vault.arc).toEqual (None)
                    Vitest.expect(vault.watcher).toEqual (None)
                    Vitest.expect(vault.fileTree.Count).toBe (0)
                    Vitest.expect(vault.hasUnsavedArcChanges).toBe (false)
                    Vitest.expect(vault.window.title).toBe (Swate.Electron.Shared.ApplicationVersion.windowTitle None)

                    Vitest
                        .expect(
                            sentMessages
                            |> Seq.exists (fun message ->
                                JS.JSON.stringify(message).Contains(PathHelpers.normalizePath arcPath)
                            )
                        )
                        .toBe (false)

                    let investigationPath =
                        ARCtrl.ArcPathHelper.combine arcPath ARCtrl.ArcPathHelper.InvestigationFileName

                    let! arcDirectoryExists = TestHelpers.pathExistsAsync arcPath
                    let! investigationExists = TestHelpers.pathExistsAsync investigationPath
                    Vitest.expect(arcDirectoryExists).toBe (true)
                    Vitest.expect(investigationExists).toBe (true)

                    do! TestHelpers.removeDirectoryAsync rootPath
                with error ->
                    do! vault.StopFileWatcher()
                    do! TestHelpers.removeDirectoryAsync rootPath
                    return raise error
            }
        )

        Vitest.test (
            "OpenARC clears pre-existing vault state after opening a non-ARC folder fails",
            fun () -> promise {
                let! folderPath = TestHelpers.createTempDirectoryAsync "swate-open-invalid-arc-"

                try
                    let vault = ArcVault(TestHelpers.testWindow ())
                    let seededEntry = FileEntry.create ("seeded.txt", "seeded.txt", false)
                    vault.fileTree.Add(seededEntry.path, seededEntry)
                    let mutable failed = false

                    Vitest.expect(vault.fileTree.Count).toBe (1)

                    try
                        do! vault.OpenARC folderPath
                    with _ ->
                        failed <- true

                    Vitest.expect(failed).toBe (true)
                    Vitest.expect(vault.path).toEqual (None)
                    Vitest.expect(vault.arc).toEqual (None)
                    Vitest.expect(vault.watcher).toEqual (None)
                    Vitest.expect(vault.fileTree.Count).toBe (0)
                    do! TestHelpers.removeDirectoryAsync folderPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync folderPath
                    return raise error
            }
        )

        Vitest.test (
            "Startup does not start a watcher when ARC loading fails",
            fun () -> promise {
                let! folderPath = TestHelpers.createTempDirectoryAsync "swate-invalid-arc-startup-"

                try
                    let vault = ArcVault(TestHelpers.testWindow ())
                    vault.path <- Some folderPath
                    let mutable startupError: exn option = None

                    try
                        do! vault.Startup()
                    with error ->
                        startupError <- Some error

                    Vitest.expect(startupError.IsSome).toBe (true)
                    Vitest.expect(startupError.Value.Message).toContain (ARCtrl.ArcPathHelper.InvestigationFileName)
                    Vitest.expect(vault.arc).toEqual (None)
                    Vitest.expect(vault.watcher).toEqual (None)
                    do! TestHelpers.removeDirectoryAsync folderPath
                with error ->
                    do! TestHelpers.removeDirectoryAsync folderPath
                    return raise error
            }
        )

        Vitest.test (
            "ClearArc resets the window title",
            fun () ->
                let vault = ArcVault(TestHelpers.testWindow ())
                vault.SetArc(ARC("LoadedArc"))

                vault.ClearArc()

                Vitest.expect(vault.arc).toEqual (None)
                Vitest.expect(vault.window.title).toBe (Swate.Electron.Shared.ApplicationVersion.windowTitle None)
        )
)
