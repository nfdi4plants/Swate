[<AutoOpen>]
module Main.ArcVault

open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
open Fable.Electron.Main
open Main
open Main.Bindings.PromiseRace
open Main.ARCtrlExtensions
open Main.Bindings
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.ArcMerge
open Main.ArcVaultHelper
open Main.FileImportCoordinator
open Swate.Components.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open ARCtrl

type internal FileWatcherReadyOutcome =
    | Ready
    | WatcherError of exn
    | Cancelled

type internal LoadedDirectoryWatcherState = {
    LifecycleActive: bool
    MutationActive: bool
    Generation: int
    ActiveGeneration: int option
    EstablishmentCancellation: (int * (unit -> unit)) option
    Watcher: Chokidar.IWatcher option
    LoadedDirectories: Set<string>
    PendingHandoffs: Set<string>
    MutationQueueTail: JS.Promise<unit>
    WatcherTransitionTail: JS.Promise<unit>
    Refreshes: Map<string, bool>
}

[<RequireQualifiedAccess>]
module private LoadedDirectoryWatcherState =

    let initial = {
        LifecycleActive = true
        MutationActive = false
        Generation = 0
        ActiveGeneration = None
        EstablishmentCancellation = None
        Watcher = None
        LoadedDirectories = Set.empty
        PendingHandoffs = Set.empty
        MutationQueueTail = JS.Constructors.Promise.resolve ()
        WatcherTransitionTail = JS.Constructors.Promise.resolve ()
        Refreshes = Map.empty
    }

/// Owns the single changing value for the loaded-directory watcher subsystem.
/// ArcVault keeps an immutable reference to this controller; every change replaces the immutable snapshot.
type internal LoadedDirectoryWatcherController() =
    let state = ref LoadedDirectoryWatcherState.initial

    member _.Current = state.Value

    member private _.Update transition = state.Value <- transition state.Value

    member this.BeginEstablishment() =
        state.Value.EstablishmentCancellation
        |> Option.iter (fun (_, cancel) -> cancel ())

        let cancel = ref ignore

        let cancellation =
            JS.Constructors.Promise.Create(fun resolve _ -> cancel.Value <- fun () -> resolve ())

        let generation = state.Value.Generation + 1

        state.Value <- {
            state.Value with
                Generation = generation
                EstablishmentCancellation = Some(generation, cancel.Value)
        }

        generation, cancellation

    member this.CompleteEstablishment generation =
        this.Update(fun current ->
            match current.EstablishmentCancellation with
            | Some(currentGeneration, _) when currentGeneration = generation -> {
                current with
                    EstablishmentCancellation = None
              }
            | _ -> current
        )

    member this.ActivateGeneration(generation, watcher) =
        let previous = state.Value.Watcher

        this.Update(fun current -> {
            current with
                ActiveGeneration = Some generation
                Watcher = Some watcher
        })

        previous

    member this.RetireWatcher() =
        let previous = state.Value.Watcher

        this.Update(fun current -> {
            current with
                ActiveGeneration = None
                Watcher = None
        })

        previous

    member this.BeginMutation() =
        this.Update(fun current -> { current with MutationActive = true })

    member this.EndMutation() =
        this.Update(fun current -> { current with MutationActive = false })

    member this.ActivateLifecycle() =
        this.Update(fun current -> { current with LifecycleActive = true })

    member this.StopLifecycle() =
        this.Update(fun current -> {
            current with
                LifecycleActive = false
                LoadedDirectories = Set.empty
                PendingHandoffs = Set.empty
                Refreshes = Map.empty
        })

    member this.ClearLifecycleData() =
        this.Update(fun current -> {
            current with
                LoadedDirectories = Set.empty
                PendingHandoffs = Set.empty
                Refreshes = Map.empty
        })

    member this.ClearLoadedDirectories() =
        this.Update(fun current -> {
            current with
                LoadedDirectories = Set.empty
        })

    member this.ClearPendingHandoffs() =
        this.Update(fun current -> {
            current with
                PendingHandoffs = Set.empty
        })

    member this.RemoveLoadedDirectory path =
        this.Update(fun current -> {
            current with
                LoadedDirectories = current.LoadedDirectories.Remove path
                PendingHandoffs = current.PendingHandoffs.Remove path
        })

    member this.RemovePendingHandoff path =
        this.Update(fun current -> {
            current with
                PendingHandoffs = current.PendingHandoffs.Remove path
        })

    member this.AddLoadedDirectory(path, pendingHandoff) =
        this.Update(fun current ->
            let next = {
                current with
                    LoadedDirectories = current.LoadedDirectories.Add path
            }

            if pendingHandoff then
                {
                    next with
                        PendingHandoffs = next.PendingHandoffs.Add path
                }
            else
                next
        )

    member this.AddLoadedDirectoriesToPendingHandoffs() =
        this.Update(fun current -> {
            current with
                PendingHandoffs = Set.union current.PendingHandoffs current.LoadedDirectories
        })

    member this.SetMutationQueueTail promise =
        this.Update(fun current -> {
            current with
                MutationQueueTail = promise
        })

    member this.SetLifecycleTail promise =
        this.Update(fun current -> {
            current with
                WatcherTransitionTail = promise
        })

    member this.UpdateRefreshes update =
        this.Update(fun current -> {
            current with
                Refreshes = update current.Refreshes
        })

type ArcLoadCancelledException(targetWindowId: int) =
    inherit exn($"Loading the ARC was cancelled because window {targetWindowId} was closed.")
    member _.targetWindowId = targetWindowId

let (|ArcLoadCancelledException|_|) (error: exn) =
    match error with
    | :? ArcLoadCancelledException as cancellation -> Some cancellation.targetWindowId
    | _ -> None

exception ArcCreatedButClosedException of targetWindowId: int

/// Tests shorten this delay to cover timeout behavior without waiting 30 seconds.
let mutable closeWaitTimeoutMilliseconds = 30000

let private startFileWatcherOwnWriteArcMergeSuppression suppressionMs currentTimeout onElapsed =
    currentTimeout |> Option.iter Fable.Core.JS.clearTimeout
    Fable.Core.JS.setTimeout onElapsed suppressionMs

/// Describes the result of a queued watcher ARC merge for its caller.
[<RequireQualifiedAccess>]
type WatcherMergeOutcome =
    /// The merge updated the in-memory ARC, so the caller can publish its matching tree batch.
    | Applied
    /// The caller retains the ARC batch and retries it. After three consecutive deferrals it publishes the tree batch first.
    | Deferred
    /// Snapshot loading or merging failed, so the caller can publish the tree batch after logging the error.
    | Failed of exn

/// <summary>
/// Represents a vault window in the application, optionally associated with a file path.
/// </summary>
/// <param name="path">Can be None if not opened ARC.</param>
type ArcVault(window: BrowserWindow) =

    let fileWatcherOwnWriteArcMergeSuppressionMs = 500
    let arcMergeQueue = ArcMergeQueue(window.id)
    let mutable busyWritingDepth = 0
    let mutable writeGeneration = 0
    let mutable watcherDeferralCount = 0
    let mutable watcherEpoch = 0
    let loadedDirectoryWatcherController = LoadedDirectoryWatcherController()

    let mutable fileTreeUpdateTail: Fable.Core.JS.Promise<unit> =
        JS.Constructors.Promise.resolve ()

    let mutable pendingFileTreeReset: (string * Fable.Core.JS.Promise<unit>) option =
        None

    member val window: BrowserWindow = window with get
    member val path: string option = None with get, set
    member val arc: ARC option = None with get, private set
    // This flag is intentionally coarse and can remain true even if later edits restore the previous logical state.
    //Good workaround, for a missing member 👍 Might even be more performant, than calculating a isDirty flag. On the other hand, it is unable to detect if changes are removed again. For example:
    //Add Assay 1 -> Set hasUnsavedArcChanges <- true
    //Remove Assay 1 -> Still true, even tough changes were removed again and it is in its original state.
    //I am not sure if performance of calculating changes or precision should be more important. Lets see in the future. Maybe you can add this comment as /// comment on this member?
    /// Dirty marker for unsaved in-memory ARC mutations.
    member val hasUnsavedArcChanges: bool = false with get, private set
    member val fileTree: Dictionary<string, FileEntry> = Dictionary<string, FileEntry>() with get, set
    member val fileTreeDirectoryHasMore: Dictionary<string, bool> = Dictionary<string, bool>() with get
    member val watcher: Chokidar.IWatcher option = None with get, set

    member _.loadedFileTreeDirectories =
        loadedDirectoryWatcherController.Current.LoadedDirectories

    member val fileWatcherReloadArcTimeout: int option = None with get, set
    member val fileWatcherPendingEvents: ResizeArray<ArcVaultFileSystemEvent> = ResizeArray() with get
    member val fileWatcherPendingArcMergeEvents: ResizeArray<ArcVaultFileSystemEvent> = ResizeArray() with get
    /// Imported paths awaiting their delayed Chokidar events. These events update the tree but must not re-merge the import.
    member val importedFileWatcherPaths: HashSet<string> = HashSet() with get
    member val private isBusyWritingValue: bool = false with get, set
    member val private fileWatcherOwnWriteArcMergeSuppressionTimeout: int option = None with get, set
    member val activeFileImport: ActiveFileImport option = None with get, set
    member val isWaitingForImportCleanup = false with get, set
    /// True only while an ARC is being opened or initially created in this vault.
    member val isInitializingArc = false with get, private set
    member val isWaitingForOperationsOnClose = false with get, set
    member val isOperationCloseApproved = false with get, set
    member val internal FileWatcherReady: Fable.Core.JS.Promise<FileWatcherReadyOutcome> option = None with get, set
    member val internal CancelFileWatcherReadyWait: (unit -> unit) option = None with get, set
    member val internal SchedulePendingFileWatcherEvents: (unit -> unit) option = None with get, set
    /// Settles when the in-memory ARC model for the currently opening path is ready.
    member val internal ArcInitialization: Fable.Core.JS.Promise<unit> option = None with get, set
    member val internal FileTreeDirectoryCursors = Dictionary<string, FileTreeDirectoryCursor>() with get

    /// Runs ARC merges sequentially so every operation observes the result of the preceding merge.
    member this.EnqueueArcMerge<'T>(operation: unit -> Fable.Core.JS.Promise<'T>) : Fable.Core.JS.Promise<'T> =
        arcMergeQueue.Enqueue operation

    /// Counts entries into write scopes. Only WithBusyWritingScope increments it.
    /// A nested scope increments it again. Comparing it before and after an await detects a write that started and finished in between.
    member this.WriteGeneration = writeGeneration

    member internal this.IncrementWatcherDeferralCount() =
        watcherDeferralCount <- watcherDeferralCount + 1
        watcherDeferralCount

    member internal this.ResetWatcherDeferralCount() = watcherDeferralCount <- 0

    member internal this.HasReachedWatcherDeferralLimit = watcherDeferralCount >= 3

    /// Counts pending-state resets. The watcher controller and ARC merge compare it with a captured value.
    member internal this.WatcherEpoch = watcherEpoch

    member internal this.IncrementWatcherEpoch() = watcherEpoch <- watcherEpoch + 1

    member internal _.LoadedDirectoryWatcherController = loadedDirectoryWatcherController

    member internal _.PendingLoadedDirectoryHandoffs =
        loadedDirectoryWatcherController.Current.PendingHandoffs

    member internal _.LoadedDirectoryRefreshes =
        loadedDirectoryWatcherController.Current.Refreshes

    member internal this.FileTreeUpdateTail
        with get () = fileTreeUpdateTail
        and set value = fileTreeUpdateTail <- value

    member internal this.PendingFileTreeReset
        with get () = pendingFileTreeReset
        and set value = pendingFileTreeReset <- value

    member internal this.CloseFileTreeDirectoryCursor(relativeDirectoryPath: string) = promise {
        let normalizedPath =
            PathHelpers.normalizeCanonicalRelativePath relativeDirectoryPath

        match this.FileTreeDirectoryCursors.TryGetValue normalizedPath with
        | false, _ -> ()
        | true, cursor ->
            this.FileTreeDirectoryCursors.Remove normalizedPath |> ignore

            try
                do! cursor.Directory.close ()
            with _ ->
                ()
    }

    member internal this.CloseFileTreeDirectoryCursors(?underPath: string) = promise {
        let cursorPaths = this.FileTreeDirectoryCursors.Keys |> Seq.toArray

        let pathsToClose =
            match underPath with
            | None -> cursorPaths
            | Some path ->
                let normalizedPath = PathHelpers.normalizeCanonicalRelativePath path

                cursorPaths
                |> Array.filter (fun cursorPath -> PathHelpers.isSameOrDescendantPath cursorPath normalizedPath)

        for cursorPath in pathsToClose do
            do! this.CloseFileTreeDirectoryCursor cursorPath

    }

    member internal this.AcquireFileTreeDirectoryCursor(arcPath: string, relativeDirectoryPath: string) = promise {
        let normalizedPath =
            PathHelpers.normalizeCanonicalRelativePath relativeDirectoryPath

        match this.FileTreeDirectoryCursors.TryGetValue normalizedPath with
        | true, cursor ->
            let directoryPath = join [| arcPath; normalizedPath |] |> PathHelpers.normalizePath
            return directoryPath, cursor
        | false, _ ->
            let! directoryPath, cursor = openFileTreeDirectoryCursor arcPath normalizedPath

            try
                this.FileTreeDirectoryCursors.[normalizedPath] <- cursor

                return directoryPath, cursor
            with error ->
                try
                    do! cursor.Directory.close ()
                with _ ->
                    ()

                return raise error
    }

    member this.arcActivityState: ArcActivityState = {
        isInitializing = this.isInitializingArc
        isBusyWriting = this.isBusyWritingValue
        hasUnsavedChanges = this.hasUnsavedArcChanges
    }

    member internal this.PublishArcActivityState() =
        if not (this.window.isDestroyed ()) then
            WindowSend.send<IArcActivityRendererApi>
                this.window
                (fun api -> api.arcActivityChanged this.arcActivityState)

    member internal this.SetArcInitializationState(isInitializing: bool) =
        if this.isInitializingArc <> isInitializing then
            this.isInitializingArc <- isInitializing
            this.PublishArcActivityState()

    /// Indicates whether the vault is currently busy writing changes to disk.
    /// When a write finishes, watcher ARC merges stay suppressed briefly to cover delayed own-write events.
    member this.isBusyWriting
        with get () = this.isBusyWritingValue
        and private set value =
            let wasBusyWriting = this.isBusyWritingValue
            this.isBusyWritingValue <- value

            if wasBusyWriting <> value then
                this.PublishArcActivityState()

            if wasBusyWriting && not value then
                this.fileWatcherOwnWriteArcMergeSuppressionTimeout <-
                    startFileWatcherOwnWriteArcMergeSuppression
                        fileWatcherOwnWriteArcMergeSuppressionMs
                        this.fileWatcherOwnWriteArcMergeSuppressionTimeout
                        (fun () -> this.fileWatcherOwnWriteArcMergeSuppressionTimeout <- None)
                    |> Some

    /// Marks this vault busy until the supplied promise settles, including nested writes.
    /// The increment and the flag update sit before the promise builder, paired with the finally
    /// that undoes them. Fable.Promise runs the body in the calling tick, so the placement is for
    /// readability. What matters is that the flag is set before the caller yields, because the
    /// entry guards (WriteArc and its siblings, withExclusiveBusyWriting) read it right after a
    /// scope opens.
    member this.WithBusyWritingScope<'T>(operation: unit -> Fable.Core.JS.Promise<'T>) : Fable.Core.JS.Promise<'T> =
        busyWritingDepth <- busyWritingDepth + 1
        writeGeneration <- writeGeneration + 1

        if busyWritingDepth = 1 then
            this.isBusyWriting <- true

        promise {
            try
                return! operation ()
            finally
                busyWritingDepth <- busyWritingDepth - 1

                if busyWritingDepth = 0 then
                    this.isBusyWriting <- false
        }

    /// Indicates whether a captured watcher event is eligible to update the in-memory ARC.
    member this.IsFileWatcherArcMergeEligible =
        not this.isBusyWritingValue
        && this.fileWatcherOwnWriteArcMergeSuppressionTimeout.IsNone

    /// Indicates whether a close confirmation dialog is currently open.
    member val isCloseRequestPending: bool = false with get, set
    /// Allows a confirmed close to pass through the onClose handler exactly once.
    member val isCloseApproved: bool = false with get, set

    /// This function mutably sets the active ARC in memory without persisting to disk.
    member this.SetArc(arc: ARC) =
        this.arc <- Some arc

        if not (this.window.isDestroyed ()) then
            this.window.title <- Swate.Electron.Shared.ApplicationVersion.windowTitle (Some arc.Identifier)

    member this.ClearArc() =
        let dirtyStateChanged = this.hasUnsavedArcChanges
        this.arc <- None
        this.hasUnsavedArcChanges <- false

        if not (this.window.isDestroyed ()) then
            this.window.title <- Swate.Electron.Shared.ApplicationVersion.windowTitle None

        if dirtyStateChanged then
            this.PublishArcActivityState()

    /// Sets the dirty marker for unsaved in-memory ARC mutations.
    member this.RefreshHasUnsavedArcChangesFlag() =
        // Use this value to only send updates to the renderer when the dirty state actually changes. This avoids redundant updates.
        if this.arc.IsSome then
            let hasNewChanges = this.arc.Value.hasInMemoryChanges ()
            let valueIsChanging = this.hasUnsavedArcChanges <> hasNewChanges
            this.hasUnsavedArcChanges <- hasNewChanges

            if valueIsChanging then
                this.PublishArcActivityState()

[<AutoOpen>]
module ArcVaultExtensions =

    type ArcVault with

        /// Serializes FileTree mutations while keeping the tail usable after a rejected update.
        member private this.EnqueueFileTreeUpdate<'T>(operation: unit -> JS.Promise<'T>) : JS.Promise<'T> =
            let queuedUpdate = this.FileTreeUpdateTail |> Promise.bind operation
            this.FileTreeUpdateTail <- queuedUpdate |> Promise.map (fun _ -> ()) |> Promise.catch (fun _ -> ())
            queuedUpdate

        member internal this.InvalidateFileTreeDirectoryCursors(relativeDirectoryPaths: string seq) =
            let normalizedPaths =
                relativeDirectoryPaths
                |> Seq.map PathHelpers.normalizeCanonicalRelativePath
                |> Seq.distinct
                |> Seq.toArray

            this.EnqueueFileTreeUpdate(fun () -> promise {
                for relativeDirectoryPath in normalizedPaths do
                    do! this.CloseFileTreeDirectoryCursors(underPath = relativeDirectoryPath)
            })

        member private this.LoadWatcherSnapshot() : Fable.Core.JS.Promise<Result<ARC, exn>> = promise {
            try
                match this.path, this.arc with
                | Some arcPath, None ->
                    match! ARC.LoadAsyncSwateZeroByteRepair arcPath with
                    | Error loadError -> return Error(exn (PathHelpers.formatContractErrors loadError))
                    | Ok snapshot -> return Ok snapshot
                | Some arcPath, Some _ ->
                    match! ARC.LoadAsyncSwate arcPath with
                    | Error loadError -> return Error(exn (PathHelpers.formatContractErrors loadError))
                    | Ok snapshot -> return Ok snapshot
                | _ -> return Error(arcNotOpenError ())
            with loadError ->
                return Error loadError
        }

        member private this.PublishWatcherSnapshot(snapshot: ARC, events: FileEvent list) : Result<unit, exn> =
            match this.arc with
            | None ->
                this.SetArc snapshot
                this.RefreshHasUnsavedArcChangesFlag()
                Ok()
            | Some arcLocal ->
                // The file watcher is our source of truth for changes made on disk. Establish the reloaded
                // ARC as a clean hash baseline before merging it; without this, unchanged entities have
                // missing/stale hashes and the next save can unnecessarily overwrite multiple XLSX files.
                baselineArcStaticHashes snapshot

                let mergeResult =
                    try
                        Ok(ARC.merge arcLocal snapshot events)
                    with mergeError ->
                        Error mergeError

                match mergeResult with
                | Error mergeError -> Error mergeError
                | Ok mergedArc ->
                    // ARC.merge may copy or reconstruct entities without retaining the hashes established
                    // above, so transfer the disk baseline to the merged ARC. Watcher-applied values then
                    // stay clean, while values that still differ because of local edits remain unsaved.
                    syncArcStaticHashes snapshot mergedArc
                    this.SetArc mergedArc
                    this.RefreshHasUnsavedArcChangesFlag()
                    Ok()

        member internal this.ApplyWatcherFileTreeEvents(events: ArcVaultFileSystemEvent list) =
            // Normalize against disk when the queued update reaches the serialized tail, so tree events use apply-time state.
            // Capture the epoch at the call so an update queued before a pending-state reset cannot publish the old root.
            let capturedWatcherEpoch = this.WatcherEpoch

            this.EnqueueFileTreeUpdate(fun () -> promise {
                match this.path with
                | None -> ()
                | Some _ when capturedWatcherEpoch <> this.WatcherEpoch -> ()
                | Some arcPath ->
                    let normalizedEvents = WatcherHelpers.normalizeAgainstDisk events
                    let nextFileTree = Dictionary<string, FileEntry>(this.fileTree)
                    let mutable hasFileTreeChanges = false

                    let! readEntries =
                        normalizedEvents
                        |> List.map (fun event -> promise {
                            if
                                WatcherHelpers.eventNameEquals Chokidar.Events.Add event.EventName
                                || WatcherHelpers.eventNameEquals Chokidar.Events.Change event.EventName
                                || WatcherHelpers.eventNameEquals Chokidar.Events.AddDir event.EventName
                            then
                                try
                                    let! entry = getFileEntry event.AbsolutePath
                                    return Some entry
                                with fileTreeError ->
                                    swatelogfn
                                        this.window.id
                                        "Unable to update file tree for watcher event '%s' on '%s': %s"
                                        event.EventName
                                        event.RelativePath
                                        fileTreeError.Message

                                    return None
                            else
                                return None
                        })
                        |> Promise.all

                    let changedFiles =
                        Array.zip (normalizedEvents |> List.toArray) readEntries
                        |> Array.choose (fun (event, entry) ->
                            match entry with
                            | Some entry when
                                not entry.isDirectory
                                && (WatcherHelpers.eventNameEquals Chokidar.Events.Add event.EventName
                                    || WatcherHelpers.eventNameEquals Chokidar.Events.Change event.EventName)
                                ->
                                Some entry
                            | _ -> None
                        )

                    // TEMPORARILY DISABLED due to LFS problems:
                    // FileTree LFS enrichment performs a repository-wide ListObjects operation.
                    // Re-enable only when VersionControlService offers bounded object-state lookup
                    // for the explicit changed paths.
                    // let! enrichedChangedFiles = getFileEntriesWithLfsMetadata arcPath changedFiles
                    let enrichedChangedFiles = changedFiles
                    let mutable enrichedChangedFileIndex = 0

                    for event, readEntry in Array.zip (normalizedEvents |> List.toArray) readEntries do
                        try
                            if
                                WatcherHelpers.eventNameEquals Chokidar.Events.Add event.EventName
                                || WatcherHelpers.eventNameEquals Chokidar.Events.Change event.EventName
                            then
                                match readEntry with
                                | Some changedFile when not changedFile.isDirectory ->
                                    let enrichedChangedFile = enrichedChangedFiles.[enrichedChangedFileIndex]
                                    enrichedChangedFileIndex <- enrichedChangedFileIndex + 1
                                    nextFileTree.[enrichedChangedFile.path] <- enrichedChangedFile
                                    hasFileTreeChanges <- true
                                | Some changedFile ->
                                    nextFileTree.[changedFile.path] <- changedFile
                                    hasFileTreeChanges <- true
                                | None -> ()
                            elif WatcherHelpers.eventNameEquals Chokidar.Events.AddDir event.EventName then
                                match readEntry with
                                | Some addedDirectory ->
                                    nextFileTree.[addedDirectory.path] <- addedDirectory
                                    hasFileTreeChanges <- true
                                | None -> ()
                            elif
                                WatcherHelpers.eventNameEquals Chokidar.Events.Unlink event.EventName
                                || (WatcherHelpers.eventNameEquals Chokidar.Events.UnlinkDir event.EventName
                                    && not (WatcherHelpers.existsSyncWithExactName event.AbsolutePath))
                            then
                                // Normalization above already turned the unlink of an existing file into a change. Directory unlinks
                                // stay admitted, so they still need this check.
                                removePathAndDescendantsInPlace event.AbsolutePath nextFileTree

                                if WatcherHelpers.eventNameEquals Chokidar.Events.UnlinkDir event.EventName then
                                    let parentPath =
                                        PathHelpers.tryGetParentPath event.RelativePath |> Option.defaultValue ""

                                    do! this.CloseFileTreeDirectoryCursors(underPath = event.RelativePath)
                                    do! this.CloseFileTreeDirectoryCursor parentPath

                                hasFileTreeChanges <- true
                        with fileTreeError ->
                            swatelogfn
                                this.window.id
                                "Unable to update file tree for watcher event '%s' on '%s': %s"
                                event.EventName
                                event.RelativePath
                                fileTreeError.Message

                    if hasFileTreeChanges && capturedWatcherEpoch = this.WatcherEpoch then
                        this.SetFileTree(nextFileTree)
            })

        member private this.NormalizeLoadedDirectoryPath(relativeDirectoryPath: string) =
            PathHelpers.normalizeCanonicalRelativePath relativeDirectoryPath

        member private this.CountLoadedDirectoryChildren(arcPath: string, relativeDirectoryPath: string) =
            let absoluteDirectoryPath =
                join [| arcPath; relativeDirectoryPath |] |> PathHelpers.normalizePath

            this.fileTree.Values
            |> Seq.filter (fun entry ->
                PathHelpers.pathsEqual (dirname (PathHelpers.normalizePath entry.path)) absoluteDirectoryPath
            )
            |> Seq.length

        member this.IsFileTreeDirectoryLoaded(relativeDirectoryPath: string) =
            relativeDirectoryPath
            |> this.NormalizeLoadedDirectoryPath
            |> this.loadedFileTreeDirectories.Contains

        member private this.RemoveUnavailableLoadedDirectories(arcPath: string) = promise {
            let unavailable =
                this.loadedFileTreeDirectories
                |> Seq.filter (fun relativePath ->
                    let absolutePath = join [| arcPath; relativePath |] |> PathHelpers.normalizePath
                    not (existsSync absolutePath)
                )
                |> Seq.toArray

            unavailable
            |> Array.iter this.LoadedDirectoryWatcherController.RemoveLoadedDirectory

            if unavailable.Length > 0 then
                let capturedWatcherEpoch = this.WatcherEpoch

                this.EnqueueFileTreeUpdate(fun () -> promise {
                    match this.path with
                    | Some currentArcPath when
                        capturedWatcherEpoch = this.WatcherEpoch
                        && PathHelpers.pathsEqual currentArcPath arcPath
                        ->
                        for relativePath in unavailable do
                            do! this.CloseFileTreeDirectoryCursors(underPath = relativePath)
                    | _ -> ()
                })
                |> Promise.catch (fun error ->
                    swatelogfn this.window.id "Failed to retire unavailable directory cursors: %s" error.Message
                )
                |> Promise.start
        }

        member internal this.QueueLoadedDirectoryRefresh(relativeDirectoryPath: string, ?watcherGeneration: int) =
            let normalizedRelativePath = this.NormalizeLoadedDirectoryPath relativeDirectoryPath
            let capturedWatcherEpoch = this.WatcherEpoch
            let capturedArcPath = this.path

            let watcherOwnsActiveLifecycle () =
                let state = this.LoadedDirectoryWatcherController.Current

                state.LifecycleActive
                && watcherGeneration
                   |> Option.forall (fun generation -> state.ActiveGeneration = Some generation)

            let lifecycleIsCurrent () =
                capturedWatcherEpoch = this.WatcherEpoch
                && match capturedArcPath, this.path with
                   | Some expectedPath, Some currentPath -> PathHelpers.pathsEqual expectedPath currentPath
                   | None, None -> true
                   | _ -> false

            if watcherOwnsActiveLifecycle () && this.PendingFileTreeReset.IsNone then
                match this.LoadedDirectoryRefreshes.TryFind normalizedRelativePath with
                | Some _ ->
                    this.LoadedDirectoryWatcherController.UpdateRefreshes(fun refreshes ->
                        refreshes.Add(normalizedRelativePath, true)
                    )
                | None ->
                    this.LoadedDirectoryWatcherController.UpdateRefreshes(fun refreshes ->
                        refreshes.Add(normalizedRelativePath, false)
                    )

                    let rec refreshWhileDirty () = promise {
                        try
                            do! this.RefreshFileTreeDirectory normalizedRelativePath
                        with refreshError ->
                            swatelogfn
                                this.window.id
                                "Unable to refresh loaded directory '%s': %s"
                                normalizedRelativePath
                                refreshError.Message

                        if
                            lifecycleIsCurrent ()
                            && this.PendingFileTreeReset.IsNone
                            && this.LoadedDirectoryRefreshes.TryFind normalizedRelativePath = Some true
                            && this.IsFileTreeDirectoryLoaded normalizedRelativePath
                        then
                            this.LoadedDirectoryWatcherController.UpdateRefreshes(fun refreshes ->
                                refreshes.Add(normalizedRelativePath, false)
                            )

                            return! refreshWhileDirty ()
                        elif lifecycleIsCurrent () then
                            this.LoadedDirectoryWatcherController.UpdateRefreshes(fun refreshes ->
                                refreshes.Remove normalizedRelativePath
                            )
                    }

                    refreshWhileDirty () |> Promise.start

        /// The sole owner of loaded-directory watcher creation, readiness, replacement and retirement.
        /// A new request cancels the preceding candidate, while the current watcher remains active until
        /// its ready replacement can take over.
        member internal this.RequestLoadedDirectoryWatcherCoverage(shouldWatch: bool, ?allowDuringMutation: bool) =
            let controller = this.LoadedDirectoryWatcherController
            let generation, cancelled = controller.BeginEstablishment()
            let precedingLifecycle = controller.Current.WatcherTransitionTail
            let allowDuringMutation = defaultArg allowDuringMutation false

            let suspensionAllowsCoverage () =
                not controller.Current.MutationActive || allowDuringMutation

            let closeWatcher (watcher: Chokidar.IWatcher) = promise {
                try
                    do! watcher.close ()
                with _ ->
                    ()
            }

            let retireWatcher () = promise {
                match controller.RetireWatcher() with
                | Some watcher -> do! closeWatcher watcher
                | None -> ()
            }

            let lifecycle = promise {
                do! precedingLifecycle

                if generation <> controller.Current.Generation then
                    return None
                elif
                    not shouldWatch
                    || not controller.Current.LifecycleActive
                    || not (suspensionAllowsCoverage ())
                then
                    do! retireWatcher ()

                    controller.CompleteEstablishment generation
                    return None
                else
                    match this.path with
                    | None ->
                        controller.ClearLoadedDirectories()
                        do! retireWatcher ()

                        controller.CompleteEstablishment generation
                        return None
                    | Some arcPath ->
                        do! this.RemoveUnavailableLoadedDirectories arcPath
                        let relativePaths = this.loadedFileTreeDirectories |> Seq.toArray

                        if relativePaths.Length = 0 then
                            do! retireWatcher ()

                            controller.CompleteEstablishment generation
                            return None
                        else
                            let absolutePaths =
                                relativePaths
                                |> Array.map (fun relativePath -> join [| arcPath; relativePath |])

                            let candidate, ready =
                                createLoadedDirectoryWatcherWithReady
                                    arcPath
                                    absolutePaths
                                    (fun error ->
                                        swatelogfn this.window.id "Loaded-directory watcher error: %s" error.Message
                                    )

                            candidate.on (
                                Chokidar.Events.All,
                                fun eventName changedPath ->
                                    if
                                        not (WatcherHelpers.eventNameEquals Chokidar.Events.Error eventName)
                                        && not (WatcherHelpers.eventNameEquals Chokidar.Events.Ready eventName)
                                    then
                                        let absoluteChangedPath =
                                            if isAbsolute changedPath then
                                                changedPath
                                            else
                                                join [| arcPath; changedPath |]

                                        match tryGetRepoRelativePath arcPath (dirname absoluteChangedPath) with
                                        | Some parentPath when
                                            controller.Current.LifecycleActive
                                            && controller.Current.ActiveGeneration = Some generation
                                            && this.IsFileTreeDirectoryLoaded parentPath
                                            ->
                                            if
                                                WatcherHelpers.eventNameEquals Chokidar.Events.Unlink eventName
                                                || WatcherHelpers.eventNameEquals Chokidar.Events.UnlinkDir eventName
                                            then
                                                let watcherEvent =
                                                    WatcherHelpers.buildWatcherEvent
                                                        arcPath
                                                        eventName
                                                        absoluteChangedPath

                                                this.ApplyWatcherFileTreeEvents [ watcherEvent ]
                                                |> Promise.catch (fun error ->
                                                    swatelogfn
                                                        this.window.id
                                                        "Unable to apply loaded-directory watcher event '%s' on '%s': %s"
                                                        eventName
                                                        watcherEvent.RelativePath
                                                        error.Message
                                                )
                                                |> Promise.start

                                            this.QueueLoadedDirectoryRefresh(parentPath, generation)
                                        | _ -> ()
                            )
                            |> ignore

                            let! readyOutcome =
                                race [|
                                    ready |> Promise.map Some
                                    cancelled |> Promise.map (fun () -> None)
                                |]

                            let candidateBecameReady =
                                match readyOutcome with
                                | Some(Ok()) -> true
                                | _ -> false

                            if
                                candidateBecameReady
                                && controller.Current.Generation = generation
                                && controller.Current.LifecycleActive
                                && suspensionAllowsCoverage ()
                            then
                                let previous = controller.ActivateGeneration(generation, candidate)

                                controller.CompleteEstablishment generation

                                match previous with
                                | Some watcher -> do! closeWatcher watcher
                                | None -> ()

                                return Some generation
                            else
                                do! closeWatcher candidate
                                controller.CompleteEstablishment generation
                                return None
            }

            controller.SetLifecycleTail(lifecycle |> Promise.map ignore |> Promise.catch (fun _ -> ()))
            lifecycle

        member private this.CatchUpPendingLoadedDirectoryHandoffs(arcPath: string, generation: int) = promise {
            let controller = this.LoadedDirectoryWatcherController
            let pendingDirectories = this.PendingLoadedDirectoryHandoffs |> Seq.toArray

            for relativePath in pendingDirectories do
                let canPublish () =
                    controller.Current.Generation = generation
                    && this.IsFileTreeDirectoryLoaded relativePath
                    && match this.path with
                       | Some currentArcPath -> PathHelpers.pathsEqual currentArcPath arcPath
                       | None -> false

                if canPublish () then
                    let knownChildCount = this.CountLoadedDirectoryChildren(arcPath, relativePath)

                    let! page = readFileTreeDirectoryPrefix arcPath relativePath 0 (max 100 knownChildCount)

                    if canPublish () then
                        let nextFileTree =
                            if page.HasMore then
                                mergeFileTreeDirectoryPage page.Entries this.fileTree
                            else
                                reconcileFileTreeDirectory arcPath relativePath page.Entries this.fileTree

                        this.fileTreeDirectoryHasMore.[relativePath] <- page.HasMore
                        this.SetFileTree nextFileTree
                        controller.RemovePendingHandoff relativePath
        }

        member private this.RestoreLoadedDirectoryWatcherAndPendingHandoffs() = promise {
            match! this.RequestLoadedDirectoryWatcherCoverage(true, allowDuringMutation = true) with
            | None -> ()
            | Some generation ->
                match this.path with
                | None -> ()
                | Some arcPath ->
                    do!
                        this.EnqueueFileTreeUpdate(fun () ->
                            this.CatchUpPendingLoadedDirectoryHandoffs(arcPath, generation)
                        )
        }

        member this.WithLoadedDirectoryWatcherSuspended<'T>(operation: unit -> JS.Promise<'T>) =
            let controller = this.LoadedDirectoryWatcherController
            let preceding = controller.Current.MutationQueueTail

            let captureOperationOutcome () =
                try
                    operation () |> Promise.map Ok |> Promise.catch (fun error -> Error error)
                with error ->
                    JS.Constructors.Promise.resolve (Error error)

            let lifecycle = promise {
                do! preceding
                controller.BeginMutation()

                try
                    controller.AddLoadedDirectoriesToPendingHandoffs()
                    do! this.RequestLoadedDirectoryWatcherCoverage false |> Promise.map ignore

                    let! operationOutcome = captureOperationOutcome ()

                    let! restorationOutcome =
                        this.RestoreLoadedDirectoryWatcherAndPendingHandoffs()
                        |> Promise.map Ok
                        |> Promise.catch (fun error -> Error error)

                    match operationOutcome, restorationOutcome with
                    | Ok result, Ok() -> return result
                    | Error operationError, Ok() -> return raise operationError
                    | Ok _, Error restorationError -> return raise restorationError
                    | Error operationError, Error restorationError ->
                        swatelogfn
                            this.window.id
                            "Unable to restore loaded-directory watcher after failed operation: %s"
                            restorationError.Message

                        return raise operationError
                finally
                    controller.EndMutation()
            }

            controller.SetMutationQueueTail(lifecycle |> Promise.map ignore |> Promise.catch (fun _ -> ()))
            lifecycle

        /// Returns the load error to the import caller when loading fails. The import can then report that its in-memory merge did not happen.
        member this.TryTriggerArcInMemoryMergeOnFileWatcherEvents(events: ArcVaultFileSystemEvent list) = promise {
            let arcEvents = WatcherHelpers.toArcMergeEvents events

            return!
                this.EnqueueArcMerge(fun () -> promise {
                    match! this.LoadWatcherSnapshot() with
                    | Error loadError -> return Error loadError
                    | Ok snapshot -> return this.PublishWatcherSnapshot(snapshot, arcEvents)
                })
        }

        member this.TryApplyWatcherArcMergeIfEligible
            (events: ArcVaultFileSystemEvent list)
            : Fable.Core.JS.Promise<WatcherMergeOutcome> =
            // The epoch is captured before the queue wait, so a pending-state reset that lands
            // between the caller's snapshot and the dequeue still invalidates this batch.
            let capturedWatcherEpoch = this.WatcherEpoch

            this.EnqueueArcMerge(fun () -> promise {
                if not this.IsFileWatcherArcMergeEligible then
                    return WatcherMergeOutcome.Deferred
                elif capturedWatcherEpoch <> this.WatcherEpoch then
                    return WatcherMergeOutcome.Deferred
                else
                    let capturedWriteGeneration = this.WriteGeneration

                    match! this.LoadWatcherSnapshot() with
                    | Error loadError ->
                        if
                            not this.IsFileWatcherArcMergeEligible
                            || capturedWriteGeneration <> this.WriteGeneration
                            || capturedWatcherEpoch <> this.WatcherEpoch
                        then
                            return WatcherMergeOutcome.Deferred
                        else
                            return WatcherMergeOutcome.Failed loadError
                    | Ok snapshot ->
                        if
                            not this.IsFileWatcherArcMergeEligible
                            || capturedWriteGeneration <> this.WriteGeneration
                            || capturedWatcherEpoch <> this.WatcherEpoch
                        then
                            return WatcherMergeOutcome.Deferred
                        else
                            match this.path with
                            | None ->
                                return
                                    WatcherMergeOutcome.Failed(
                                        exn "The ARC path was unavailable while applying a watcher merge."
                                    )
                            | Some root ->
                                let normalizedEvents = WatcherHelpers.normalizeAgainstDisk events

                                let arcEvents =
                                    WatcherHelpers.toArcMergeEvents normalizedEvents
                                    |> List.filter (fun event ->
                                        event.EventName <> EventName.Unlink
                                        || not (
                                            WatcherHelpers.existsSyncWithExactName (
                                                if isAbsolute event.Path then
                                                    event.Path
                                                else
                                                    join [| root; event.Path |]
                                            )
                                        )
                                    )

                                match this.PublishWatcherSnapshot(snapshot, arcEvents) with
                                | Ok() -> return WatcherMergeOutcome.Applied
                                | Error mergeError -> return WatcherMergeOutcome.Failed mergeError
            })

        member internal this._FileEventController(sendMsgApi: IArcFileWatcherApi) =
            // A long write retries every 500 ms, so only the first deferral and the limit crossing are logged.
            let logWatcherDeferral deferralCount =
                match deferralCount with
                | 1 ->
                    swatelogfn
                        this.window.id
                        "Watcher merge deferred because a write is running or ran during the snapshot load."
                | 3 ->
                    swatelogfn
                        this.window.id
                        "Watcher merge deferred three times in a row. The tree batch is published on this and every further deferral until a merge applies."
                | _ -> ()

            let rec scheduleReload () =
                let mutable ownTimeoutId = None

                let finishReload () =
                    if
                        this.fileWatcherPendingEvents.Count > 0
                        || this.fileWatcherPendingArcMergeEvents.Count > 0
                    then
                        if
                            this.fileWatcherReloadArcTimeout.IsNone
                            || this.fileWatcherReloadArcTimeout = ownTimeoutId
                        then
                            this.fileWatcherReloadArcTimeout <- None
                            scheduleReload ()
                        else
                            ()
                    else if
                        this.fileWatcherReloadArcTimeout.IsNone
                        || this.fileWatcherReloadArcTimeout = ownTimeoutId
                    then
                        this.fileWatcherReloadArcTimeout <- None
                        sendMsgApi.IsLoadingChanges false
                    else
                        ()

                let timeoutId =
                    Fable.Core.JS.setTimeout
                        (fun () ->
                            promise {
                                swatelogfn this.window.id "Scheduled ARC reload triggered by file watcher."
                                let callbackEpoch = this.WatcherEpoch

                                if this.isBusyWriting then
                                    let hasPendingEvents =
                                        this.fileWatcherPendingEvents.Count > 0
                                        || this.fileWatcherPendingArcMergeEvents.Count > 0

                                    // A fire with nothing pending defers nothing, so it neither counts nor logs.
                                    if hasPendingEvents then
                                        let deferralCount = this.IncrementWatcherDeferralCount()
                                        logWatcherDeferral deferralCount

                                        if this.HasReachedWatcherDeferralLimit then
                                            let pendingEvents = this.fileWatcherPendingEvents |> Seq.toList
                                            this.fileWatcherPendingEvents.Clear()

                                            do! this.ApplyWatcherFileTreeEvents pendingEvents

                                    if
                                        this.fileWatcherPendingEvents.Count = 0
                                        && this.fileWatcherPendingArcMergeEvents.Count = 0
                                    then
                                        // Nothing is left to retry, so the retry chain ends here and the loading
                                        // flag goes off. The flag means that watcher changes are loading, so a
                                        // false during a write is truthful, and the next admitted event turns it
                                        // on again. The counter resets so the next batch starts its own count.
                                        this.ResetWatcherDeferralCount()
                                        finishReload ()
                                    elif this.fileWatcherReloadArcTimeout = ownTimeoutId then
                                        this.fileWatcherReloadArcTimeout <- None
                                        scheduleReload ()
                                    else
                                        ()
                                else
                                    let pendingEvents = this.fileWatcherPendingEvents |> Seq.toList
                                    let pendingArcMergeEvents = this.fileWatcherPendingArcMergeEvents |> Seq.toList
                                    this.fileWatcherPendingEvents.Clear()
                                    this.fileWatcherPendingArcMergeEvents.Clear()

                                    if pendingArcMergeEvents.IsEmpty then
                                        this.ResetWatcherDeferralCount()
                                        do! this.ApplyWatcherFileTreeEvents pendingEvents
                                        finishReload ()
                                    else
                                        match! this.TryApplyWatcherArcMergeIfEligible pendingArcMergeEvents with
                                        | (WatcherMergeOutcome.Applied | WatcherMergeOutcome.Failed _) as outcome ->
                                            match outcome with
                                            | WatcherMergeOutcome.Failed mergeError ->
                                                swatelogfn
                                                    this.window.id
                                                    "Unable to merge ARC after file watcher event: %s"
                                                    mergeError.Message
                                            | _ -> ()

                                            this.ResetWatcherDeferralCount()

                                            // FileTree updates are renderer-visible and can trigger an immediate openFile call.
                                            // A successful merge means that call reads the same ARC state represented by the published tree.
                                            // A batch snapshotted before a pending-state reset carries paths under the old root.
                                            if callbackEpoch = this.WatcherEpoch then
                                                do! this.ApplyWatcherFileTreeEvents pendingEvents

                                            finishReload ()
                                        | WatcherMergeOutcome.Deferred ->
                                            if callbackEpoch <> this.WatcherEpoch then
                                                swatelogfn
                                                    this.window.id
                                                    "Watcher merge deferred after watcher state was cleared. Dropping the stale batch."

                                                finishReload ()
                                            else
                                                let deferralCount = this.IncrementWatcherDeferralCount()

                                                logWatcherDeferral deferralCount

                                                // Read once: an overlapping callback can reset the counter during the await below.
                                                let reachedDeferralLimit = this.HasReachedWatcherDeferralLimit

                                                if reachedDeferralLimit then
                                                    // The limit latches during a write, so every later deferral publishes the
                                                    // tree batch too. A file the tree shows before its entity is merged opens
                                                    // through the disk fallback of openFile (raw file text) until the retained
                                                    // ARC batch merges after the write. A tree that hides every external change
                                                    // for the whole duration of a long write would be worse.
                                                    // The ARC batch is restored first, so a rejected tree update cannot lose it.
                                                    // Appending is enough because both consumers re-check the disk at apply time.
                                                    this.fileWatcherPendingArcMergeEvents.AddRange
                                                        pendingArcMergeEvents

                                                    do! this.ApplyWatcherFileTreeEvents pendingEvents
                                                else
                                                    ()

                                                if callbackEpoch = this.WatcherEpoch then
                                                    if not reachedDeferralLimit then
                                                        this.fileWatcherPendingArcMergeEvents.AddRange
                                                            pendingArcMergeEvents

                                                        this.fileWatcherPendingEvents.AddRange pendingEvents
                                                    else
                                                        ()

                                                    if
                                                        this.fileWatcherReloadArcTimeout.IsNone
                                                        || this.fileWatcherReloadArcTimeout = ownTimeoutId
                                                    then
                                                        this.fileWatcherReloadArcTimeout <- None
                                                        scheduleReload ()
                                                    else
                                                        ()
                                                else
                                                    ()
                            }
                            |> Promise.catch (fun ex ->
                                swatelogfn this.window.id "Scheduled ARC reload failed: %s" ex.Message
                                finishReload ()
                            )
                            |> Promise.start
                        )
                        500

                ownTimeoutId <- Some timeoutId
                this.fileWatcherReloadArcTimeout <- Some timeoutId

            this.SchedulePendingFileWatcherEvents <- Some scheduleReload

            let isArcMergeRelevantEvent event =
                WatcherHelpers.filterArcMergeRelevantEvents [| event |] |> Array.isEmpty |> not

            fun (eventName: string) (path: string) ->

                swatelogfn this.window.id "File change detected: %s on %s" eventName path

                WatcherHelpers.queueFileWatcherEvent
                    (fun event ->
                        let isImportedPath =
                            this.importedFileWatcherPaths.Remove(
                                PathHelpers.normalizePath (event.AbsolutePath.ToLowerInvariant())
                            )

                        isArcMergeRelevantEvent event
                        && not isImportedPath
                        && (this.isInitializingArc
                            || this.activeFileImport.IsSome
                            || this.IsFileWatcherArcMergeEligible)
                    )
                    this.path
                    this.fileWatcherPendingEvents
                    this.fileWatcherPendingArcMergeEvents
                    eventName
                    path

                if not this.isInitializingArc then
                    match this.fileWatcherReloadArcTimeout with
                    | Some timeoutId ->
                        Fable.Core.JS.clearTimeout timeoutId
                        this.fileWatcherReloadArcTimeout <- None
                    | None -> sendMsgApi.IsLoadingChanges true

                    scheduleReload ()

        /// Applies an ARC content DTO to the in-memory ARC and marks the vault dirty.
        member this.UpdateArcByFileContentDTO(request: FileContentDTO) : Result<unit, exn> =
            match this.arc with
            | None -> Error(arcNotOpenError ())
            | Some arc ->
                let normalizedRequest =
                    Swate.Electron.Shared.FileIOHelper.FileContentDTO.normalizeArcFileRequestPath request

                match updateARCByFileContentDTO arc normalizedRequest with
                | Error saveError -> Error saveError
                | Ok newArc ->
                    this.SetArc(newArc)
                    this.RefreshHasUnsavedArcChangesFlag()
                    Ok()

        /// Writes the active in-memory ARC scaffold to disk without touching unmanaged files such as notes.
        member this.WriteArc() : Fable.Core.JS.Promise<Result<unit, exn>> = promise {
            match this.isBusyWriting, this.path, this.arc with
            | true, _, _ ->
                return Error(exn "Swate is still saving another change. Please wait a moment and try again.")
            | false, Some arcPath, Some arc ->
                return!
                    this.WithBusyWritingScope(fun () -> promise {
                        try
                            match! arc.TryUpdateAsyncSwate(arcPath) with
                            | Error errors -> return Error(exn (PathHelpers.formatContractErrors errors))
                            | Ok _ ->
                                this.RefreshHasUnsavedArcChangesFlag()
                                return Ok()
                        with e ->
                            return Error(exn $"Failed to persist ARC to disk: {e.Message}")
                    })
            | _ -> return Error(arcNotOpenError ())
        }

        /// Adds a new ARC entity through ARCtrl's scoped add path.
        /// Watcher ARC merges are suppressed during the disk write; static hashes are resynced from disk afterwards.
        member this.AddArcFile(request: FileContentDTO) : Fable.Core.JS.Promise<Result<unit, exn>> = promise {
            match this.isBusyWriting, this.path, this.arc with
            | true, _, _ ->
                return Error(exn "Swate is still saving another change. Please wait a moment and try again.")
            | false, Some arcPath, Some arcLocal ->
                let normalizedRequest =
                    Swate.Electron.Shared.FileIOHelper.FileContentDTO.normalizeArcFileRequestPath request

                match Swate.Electron.Shared.FileIOHelper.FileContentDTO.toArcFile normalizedRequest with
                | None -> return Error(exn $"Unsupported file type for adding: {normalizedRequest.fileType}")
                | Some arcFile ->
                    return!
                        this.WithBusyWritingScope(fun () -> promise {
                            match! arcLocal.TryAddArcFileAsync(arcPath, arcFile, false) with
                            | Error errors -> return Error(exn (PathHelpers.formatContractErrors errors))
                            | Ok _ ->
                                match! ARC.LoadAsyncSwate arcPath with
                                | Ok persistedArc ->
                                    baselineArcStaticHashes persistedArc
                                    syncAddedArcFileFromPersisted persistedArc arcLocal arcFile
                                    syncArcStaticHashes persistedArc arcLocal
                                    this.RefreshHasUnsavedArcChangesFlag()

                                    match arcFile with
                                    | ArcFiles.DataMap(Some parentInfo, _) ->
                                        let parentPath =
                                            DatamapParentInfo.toPath parentInfo
                                            |> PathHelpers.tryGetParentPath
                                            |> Option.defaultValue ""

                                        do! this.RefreshFileTreeDirectory parentPath
                                    | _ -> ()

                                    return Ok()
                                | Error loadErrors ->
                                    this.RefreshHasUnsavedArcChangesFlag()

                                    return
                                        Error(
                                            exn (
                                                "Added ARC file, but could not reload the persisted hash baseline: "
                                                + (PathHelpers.formatContractErrors loadErrors)
                                            )
                                        )
                        })
            | _ -> return Error(arcNotOpenError ())
        }

        member this.SetFileTree(fileTree: Dictionary<string, FileEntry>) =
            this.fileTree <- fileTree

            let rendererSnapshot =
                match this.path with
                | Some arcPath -> {
                    entries = toRendererFileTree arcPath fileTree.Values
                    directoryHasMore = Dictionary<string, bool>(this.fileTreeDirectoryHasMore)
                  }
                | None -> {
                    entries = Dictionary<string, FileEntry>()
                    directoryHasMore = Dictionary<string, bool>()
                  }

            WindowSend.send<IFileTreeRendererApi> this.window (fun api -> api.fileTreeUpdate rendererSnapshot)

        member this.RefreshFileTreeDirectory(relativeDirectoryPath: string) =
            let normalizedRelativePath = this.NormalizeLoadedDirectoryPath relativeDirectoryPath
            let capturedWatcherEpoch = this.WatcherEpoch
            let capturedArcPath = this.path

            let lifecycleIsCurrent () =
                capturedWatcherEpoch = this.WatcherEpoch
                && match capturedArcPath, this.path with
                   | Some expectedPath, Some currentPath -> PathHelpers.pathsEqual expectedPath currentPath
                   | None, None -> true
                   | _ -> false

            this.EnqueueFileTreeUpdate(fun () -> promise {
                match capturedArcPath with
                | None -> return raise (arcNotOpenError ())
                | Some _ when not (lifecycleIsCurrent ()) -> ()
                | Some arcPath ->
                    try
                        let wasLoaded = this.IsFileTreeDirectoryLoaded normalizedRelativePath

                        let knownChildCount =
                            this.CountLoadedDirectoryChildren(arcPath, normalizedRelativePath)

                        let! page =
                            readFileTreeDirectoryPrefix arcPath normalizedRelativePath 0 (max 100 knownChildCount)

                        if lifecycleIsCurrent () then
                            let controller = this.LoadedDirectoryWatcherController

                            if not wasLoaded && not (System.String.IsNullOrWhiteSpace normalizedRelativePath) then
                                controller.AddLoadedDirectory(normalizedRelativePath, true)

                                if not controller.Current.MutationActive then
                                    match! this.RequestLoadedDirectoryWatcherCoverage true with
                                    | Some generation when lifecycleIsCurrent () ->
                                        do! this.CatchUpPendingLoadedDirectoryHandoffs(arcPath, generation)
                                    | _ -> ()

                            let shouldPublishInitialRead =
                                wasLoaded
                                || System.String.IsNullOrWhiteSpace normalizedRelativePath
                                || this.PendingLoadedDirectoryHandoffs.Contains normalizedRelativePath

                            if lifecycleIsCurrent () && shouldPublishInitialRead then
                                this.fileTreeDirectoryHasMore.[normalizedRelativePath] <- page.HasMore

                                if not page.HasMore then
                                    do! this.CloseFileTreeDirectoryCursor normalizedRelativePath

                                let nextFileTree =
                                    if page.HasMore then
                                        mergeFileTreeDirectoryPage page.Entries this.fileTree
                                    else
                                        reconcileFileTreeDirectory
                                            arcPath
                                            normalizedRelativePath
                                            page.Entries
                                            this.fileTree

                                this.SetFileTree nextFileTree

                                if not (System.String.IsNullOrWhiteSpace normalizedRelativePath) then
                                    controller.AddLoadedDirectory(normalizedRelativePath, false)

                                do! this.RemoveUnavailableLoadedDirectories arcPath
                    with refreshError ->
                        if
                            lifecycleIsCurrent ()
                            && not (System.String.IsNullOrWhiteSpace normalizedRelativePath)
                        then
                            let controller = this.LoadedDirectoryWatcherController
                            controller.RemoveLoadedDirectory normalizedRelativePath

                            if not controller.Current.MutationActive then
                                do! this.RequestLoadedDirectoryWatcherCoverage true |> Promise.map ignore

                        if lifecycleIsCurrent () then
                            return raise refreshError
            })

        /// Loads exactly one additional page for an expanded directory.
        member this.LoadNextFileTreeDirectoryPage(relativeDirectoryPath: string) = promise {
            let normalizedRelativePath = this.NormalizeLoadedDirectoryPath relativeDirectoryPath
            let mutable needsWatcherCoverage = false
            let capturedWatcherEpoch = this.WatcherEpoch
            let capturedArcPath = this.path

            do!
                this.EnqueueFileTreeUpdate(fun () -> promise {
                    match capturedArcPath, this.path with
                    | None, _ -> return raise (arcNotOpenError ())
                    | Some _, None -> return raise (arcNotOpenError ())
                    | Some expectedPath, Some currentPath when
                        capturedWatcherEpoch <> this.WatcherEpoch
                        || not (PathHelpers.pathsEqual expectedPath currentPath)
                        ->
                        return raise (ArcLoadCancelledException this.window.id)
                    | Some arcPath, Some _ ->
                        match this.fileTreeDirectoryHasMore.TryGetValue normalizedRelativePath with
                        | true, false -> ()
                        | _ ->
                            let! directoryPath, cursor =
                                this.AcquireFileTreeDirectoryCursor(arcPath, normalizedRelativePath)

                            try
                                let! page = readFileTreeDirectoryCursorPage directoryPath cursor 100

                                if
                                    capturedWatcherEpoch <> this.WatcherEpoch
                                    || this.path |> Option.exists (PathHelpers.pathsEqual arcPath) |> not
                                then
                                    do! this.CloseFileTreeDirectoryCursor normalizedRelativePath
                                    return raise (ArcLoadCancelledException this.window.id)

                                let nextTree = mergeFileTreeDirectoryPage page.Entries this.fileTree
                                this.fileTreeDirectoryHasMore.[normalizedRelativePath] <- page.HasMore
                                this.SetFileTree nextTree

                                if not page.HasMore then
                                    do! this.CloseFileTreeDirectoryCursor normalizedRelativePath
                            with error ->
                                do! this.CloseFileTreeDirectoryCursor normalizedRelativePath
                                return raise error

                        if not (System.String.IsNullOrWhiteSpace normalizedRelativePath) then
                            let controller = this.LoadedDirectoryWatcherController

                            if not (this.IsFileTreeDirectoryLoaded normalizedRelativePath) then
                                controller.AddLoadedDirectory(normalizedRelativePath, false)
                                needsWatcherCoverage <- not controller.Current.MutationActive

                })

            if needsWatcherCoverage then
                this.RequestLoadedDirectoryWatcherCoverage true
                |> Promise.map ignore
                |> Promise.catch (fun error ->
                    swatelogfn this.window.id "Failed to establish loaded-directory watcher coverage: %s" error.Message
                )
                |> Promise.start

            return ()
        }

        member this.ResetFileTreeToRoot() =
            match this.path with
            | None -> promise { return raise (arcNotOpenError ()) }
            | Some requestedArcPath ->
                match this.PendingFileTreeReset with
                | Some(pendingArcPath, pendingReset) when PathHelpers.pathsEqual pendingArcPath requestedArcPath ->
                    pendingReset
                | Some(_, pendingReset) ->
                    pendingReset
                    |> Promise.catch (fun _ -> ())
                    |> Promise.bind (fun () -> this.ResetFileTreeToRoot())
                | None ->
                    this.IncrementWatcherEpoch()
                    let controller = this.LoadedDirectoryWatcherController
                    controller.ClearPendingHandoffs()
                    // Clearing the dirty map immediately prevents an active refresh from scheduling a
                    // follow-up while this reset waits for earlier FileTree work.
                    controller.UpdateRefreshes(fun _ -> Map.empty)

                    let queuedReset =
                        this.EnqueueFileTreeUpdate(fun () -> promise {
                            try
                                do! this.CloseFileTreeDirectoryCursors()
                                this.fileTreeDirectoryHasMore.Clear()

                                match this.path with
                                | Some currentArcPath when PathHelpers.pathsEqual currentArcPath requestedArcPath ->
                                    let! rootPage = this.LoadInitialFileTreeRootPageCore requestedArcPath
                                    do! this.RequestLoadedDirectoryWatcherCoverage false |> Promise.map ignore
                                    controller.ClearLoadedDirectories()
                                    this.fileTreeDirectoryHasMore.[""] <- rootPage.HasMore
                                    this.SetFileTree rootPage.Entries
                                | _ -> return raise (exn "The ARC path changed before the FileTree reset ran.")
                            finally
                                this.PendingFileTreeReset <- None
                        })

                    this.PendingFileTreeReset <- Some(requestedArcPath, queuedReset)
                    queuedReset

        /// Refreshes a directory only when File Explorer already knows its contents.
        /// The root is always visible and is therefore always eligible.
        member this.RefreshFileTreeDirectoryIfLoaded(relativeDirectoryPath: string) = promise {
            if
                System.String.IsNullOrWhiteSpace relativeDirectoryPath
                || this.IsFileTreeDirectoryLoaded relativeDirectoryPath
            then
                do! this.RefreshFileTreeDirectory relativeDirectoryPath
                return true
            else
                return false
        }

        member this.GetRendererFileTreeSnapshot() = promise {
            match this.path with
            | None ->
                return {
                    entries = Dictionary<string, FileEntry>()
                    directoryHasMore = Dictionary<string, bool>()
                }
            | Some arcPath ->
                if this.fileTree.Count = 0 then
                    do! this.EnsureInitialFileTreeRootPage arcPath |> Promise.map ignore

                return {
                    entries = toRendererFileTree arcPath this.fileTree.Values
                    directoryHasMore = Dictionary<string, bool>(this.fileTreeDirectoryHasMore)
                }
        }

        member this.LoadArc() = promise {
            if this.path.IsSome then
                match! ARC.LoadAsyncSwateZeroByteRepair this.path.Value with
                | Error e -> swatefailfn this.window.id "Unable to load ARC: %s" (PathHelpers.formatContractErrors e)
                | Ok _ when this.window.isDestroyed () -> return raise (ArcLoadCancelledException this.window.id)
                | Ok arc ->
                    this.SetArc(arc)
                    this.RefreshHasUnsavedArcChangesFlag()
            else
                swatefailfn this.window.id "No path set for StartFileWatcher."
        }

        member private this.LoadInitialFileTreeRootPageCore(arcPath: string) = promise {
            let normalizedArcPath = normalizeRootPath arcPath
            let! rootEntry = getFileEntry normalizedArcPath

            if not rootEntry.isDirectory then
                return {
                    Entries = createFileEntryTree [| rootEntry |]
                    HasMore = false
                }
            else
                do! this.CloseFileTreeDirectoryCursor ""

                let! directoryPath, cursor = this.AcquireFileTreeDirectoryCursor(normalizedArcPath, "")

                try
                    let! page = readFileTreeDirectoryCursorPage directoryPath cursor 100

                    if not page.HasMore then
                        do! this.CloseFileTreeDirectoryCursor ""

                    return {
                        Entries = createFileEntryTree (Array.append [| rootEntry |] page.Entries)
                        HasMore = page.HasMore
                    }
                with error ->
                    do! this.CloseFileTreeDirectoryCursor ""
                    return raise error
        }

        member internal this.EnsureInitialFileTreeRootPage(arcPath: string) =
            let capturedWatcherEpoch = this.WatcherEpoch

            this.EnqueueFileTreeUpdate(fun () -> promise {
                match this.path with
                | Some currentArcPath when
                    capturedWatcherEpoch = this.WatcherEpoch
                    && PathHelpers.pathsEqual currentArcPath arcPath
                    ->
                    if this.fileTree.Count = 0 then
                        let! rootPage = this.LoadInitialFileTreeRootPageCore arcPath
                        this.fileTreeDirectoryHasMore.[""] <- rootPage.HasMore
                        this.SetFileTree rootPage.Entries

                    return this.fileTree
                | _ -> return raise (ArcLoadCancelledException this.window.id)
            })

        member this.StartFileWatcher(?usePolling: bool) =
            if this.path.IsSome then
                this.LoadedDirectoryWatcherController.ActivateLifecycle()

                match this.watcher with
                | Some _ -> ()
                | None ->
                    let watcher, ready =
                        createFileWatcherWithReady
                            this.path.Value
                            usePolling
                            (fun error -> swatelogfn this.window.id "Permanent ARC watcher error: %s" error.Message)

                    let mutable cancelReadyWait = ignore

                    let cancelled =
                        JS.Constructors.Promise.Create(fun resolve _ ->
                            cancelReadyWait <- fun () -> resolve FileWatcherReadyOutcome.Cancelled
                        )

                    let sendWatcherMessage = WindowSend.sender<IArcFileWatcherApi> this.window

                    let sendMsgApi: IArcFileWatcherApi = {
                        IsLoadingChanges =
                            fun isLoading -> sendWatcherMessage (fun api -> api.IsLoadingChanges isLoading)
                    }

                    watcher.on (Chokidar.Events.All, this._FileEventController sendMsgApi) |> ignore
                    this.watcher <- Some watcher

                    this.FileWatcherReady <-
                        Some(
                            race [|
                                ready
                                |> Promise.map (
                                    function
                                    | Ok() -> FileWatcherReadyOutcome.Ready
                                    | Error error -> FileWatcherReadyOutcome.WatcherError error
                                )
                                cancelled
                            |]
                        )

                    this.CancelFileWatcherReadyWait <- Some cancelReadyWait
            else
                swatefailfn this.window.id "No path set for StartFileWatcher."

        member this.ClearPendingFileWatcherState() =
            this.IncrementWatcherEpoch()
            this.ResetWatcherDeferralCount()
            this.fileWatcherReloadArcTimeout |> Option.iter Fable.Core.JS.clearTimeout
            this.fileWatcherReloadArcTimeout <- None
            this.fileWatcherPendingEvents.Clear()
            this.fileWatcherPendingArcMergeEvents.Clear()
            this.importedFileWatcherPaths.Clear()

        member this.StopFileWatcher() = promise {
            this.ClearPendingFileWatcherState()
            this.CancelFileWatcherReadyWait |> Option.iter (fun cancel -> cancel ())
            this.CancelFileWatcherReadyWait <- None
            // Reject callbacks synchronously. Requesting retirement below also advances the watcher
            // generation before either close is awaited, so callbacks owned by the retiring watcher
            // cannot join a later restarted lifecycle.
            this.LoadedDirectoryWatcherController.StopLifecycle()

            let loadedDirectoryWatcherClose =
                this.RequestLoadedDirectoryWatcherCoverage false |> Promise.map ignore

            match this.watcher with
            | None -> ()
            | Some watcher ->
                try
                    do! watcher.close ()
                with _ ->
                    ()

            this.watcher <- None
            this.FileWatcherReady <- None
            this.SchedulePendingFileWatcherEvents <- None

            do! loadedDirectoryWatcherClose
            // A lifecycle queued ahead of retirement may have touched these collections before it
            // observed cancellation. Reassert the completed-stop invariants after it drains.
            this.LoadedDirectoryWatcherController.ClearLifecycleData()
            this.ClearPendingFileWatcherState()
            do! this.EnqueueFileTreeUpdate(fun () -> this.CloseFileTreeDirectoryCursors())

        }

        member internal this.RestoreEmptyVaultAfterFailedInitialization() = promise {
            do! this.StopFileWatcher()

            this.SetArcInitializationState false
            this.path <- None
            this.fileTree.Clear()
            this.fileTreeDirectoryHasMore.Clear()

            try
                this.ClearArc()
            with error ->
                swatelogfn this.window.id "Failed to reset ARC window presentation: %s" error.Message

            if not (this.window.isDestroyed ()) then
                WindowSend.send<IPathChangeRendererApi> this.window (fun api -> api.pathChange None)
                this.SetFileTree(Dictionary<string, FileEntry>())
        }

        /// Starts permanent observation and ARC loading together. The watcher is attached
        /// synchronously before LoadArc begins, so initialization can safely buffer events without
        /// putting the watcher's initial scan on the model-loading critical path.
        member this.Startup() = promise {
            this.StartFileWatcher()

            let watcherReady = promise {
                match this.FileWatcherReady with
                | Some ready ->
                    match! ready with
                    | FileWatcherReadyOutcome.Ready -> ()
                    | FileWatcherReadyOutcome.WatcherError error -> return raise error
                    | FileWatcherReadyOutcome.Cancelled -> return raise (ArcLoadCancelledException this.window.id)
                | None -> ()
            }

            let! _ = [| watcherReady; this.LoadArc() |] |> Promise.all

            return ()
        }

        /// Publishes the bounded root snapshot while ARC metadata continues loading in the background.
        member internal this.PublishArcInitializationSnapshot(fileTree: Dictionary<string, FileEntry>) =
            match this.window.isDestroyed (), this.path with
            | true, _ -> raise (ArcLoadCancelledException this.window.id)
            | false, None -> swatefailfn this.window.id "Unable to publish ARC initialization without a path."
            | false, Some normalizedPath ->
                this.fileTree <- fileTree

                WindowSend.send<IPathChangeRendererApi> this.window (fun api -> api.pathChange (Some normalizedPath))

                this.SetFileTree fileTree

        /// Finalizes a prepared ARC after every asynchronous initialization step succeeded.
        member internal this.FinalizeArcInitialization() =
            this.SetArcInitializationState false

            if
                this.fileWatcherPendingEvents.Count > 0
                || this.fileWatcherPendingArcMergeEvents.Count > 0
            then
                this.SchedulePendingFileWatcherEvents
                |> Option.iter (fun schedule -> schedule ())

        member this.OpenARC(path: string) = promise {
            match this.path with
            | Some _ -> swatefailfn this.window.id "Unable to open ARC in vault bound to ARC."
            | None ->
                let normalizedPath = PathHelpers.normalizePath path

                swatelogfn this.window.id "path: %s" normalizedPath
                this.SetArcInitializationState true
                this.path <- Some normalizedPath

                try
                    let initialization = this.Startup()
                    this.ArcInitialization <- Some initialization

                    try
                        do! initialization
                    finally
                        this.ArcInitialization <- None
                with error ->
                    do! this.RestoreEmptyVaultAfterFailedInitialization()
                    return raise error
        }

        member this.CreateARC(path: string, identifier: string) = promise {
            match this.path, this.arc with
            | Some _, _ -> swatefailfn this.window.id "Unable to create ARC in vault bound to path."
            | _, Some _ -> swatefailfn this.window.id "Unable to create ARC in vault bound to ARC."
            | None, None ->
                let normalizedPath = PathHelpers.normalizePath path

                try
                    this.SetArcInitializationState true
                    let arc = ARC(identifier)
                    this.path <- Some normalizedPath
                    this.SetArc(arc)
                    this.RefreshHasUnsavedArcChangesFlag()

                    do!
                        this.WithBusyWritingScope(fun () -> promise {
                            match! arc.TryWriteAsyncSwate(normalizedPath) with
                            | Ok _ -> ()
                            | Error errors ->
                                failwithf
                                    "Could not write ARC, failed with the following errors %s"
                                    (PathHelpers.formatContractErrors errors)
                        })

                    try
                        do! this.Startup()
                    with ArcLoadCancelledException targetWindowId ->
                        return raise (ArcCreatedButClosedException targetWindowId)

                with error ->
                    do! this.RestoreEmptyVaultAfterFailedInitialization()
                    return raise error
        }

        member this.RenameOpenArcRoot(newName: string) : Fable.Core.JS.Promise<Result<string, exn>> = promise {
            // A running write, such as a Git LFS download or free, holds absolute paths into the
            // ARC folder. Moving the folder under it would make its next step fail, so the rename is refused until the write ends.
            let busyWritingError () =
                exn
                    "Cannot rename the ARC folder while Swate is still writing to it. Wait until the running operation finishes, then try again."

            match this.path with
            | None -> return Error(arcNotOpenError ())
            | Some _ when this.isBusyWriting -> return Error(busyWritingError ())
            | Some currentPath ->
                let hadWatcher = this.watcher.IsSome

                if hadWatcher then
                    do! this.StopFileWatcher()
                else
                    this.ClearPendingFileWatcherState()

                // Release directory handles before moving the ARC folder. On platforms such as Windows,
                // an open handle anywhere below the root can otherwise prevent the rename.
                do! this.EnqueueFileTreeUpdate(fun () -> this.CloseFileTreeDirectoryCursors())

                // A write can start while the watcher stops.
                let! renameResult =
                    if this.isBusyWriting then
                        promise { return Error(busyWritingError ()) }
                    else
                        renameOpenArcRootDirectoryOnDisk currentPath newName

                match renameResult with
                | Error renameError ->
                    if hadWatcher then
                        this.StartFileWatcher()

                    return Error renameError
                | Ok renamedPath ->
                    this.path <- Some renamedPath

                    match Main.VersionControl.WorkspaceSessionHost.tryCurrent () with
                    | Some host ->
                        let! moved = host.WorkspaceRenamed(currentPath, renamedPath) |> Async.StartAsPromise

                        match moved with
                        | Ok() -> ()
                        | Error message ->
                            swatelogfn
                                this.window.id
                                "ARC folder was renamed to '%s', but its version control binding could not follow: %s"
                                renamedPath
                                message
                    | None -> ()

                    if this.arc.IsNone then
                        try
                            do! this.LoadArc()
                        with loadError ->
                            swatelogfn
                                this.window.id
                                "ARC folder was renamed to '%s', but reload failed: %s"
                                renamedPath
                                loadError.Message

                    if hadWatcher then
                        try
                            this.StartFileWatcher()
                        with watcherError ->
                            swatelogfn
                                this.window.id
                                "ARC folder was renamed to '%s', but file watcher restart failed: %s"
                                renamedPath
                                watcherError.Message

                    try
                        do! this.ResetFileTreeToRoot()
                    with refreshError ->
                        swatelogfn
                            this.window.id
                            "ARC folder was renamed to '%s', but file tree refresh failed: %s"
                            renamedPath
                            refreshError.Message

                    try
                        WindowSend.send<IPathChangeRendererApi>
                            this.window
                            (fun api -> api.pathChange (Some renamedPath))
                    with notifyError ->
                        swatelogfn
                            this.window.id
                            "ARC folder was renamed to '%s', but renderer path notification failed: %s"
                            renamedPath
                            notifyError.Message

                    return Ok renamedPath
        }


/// Describes the outcome of an ARC lifecycle action performed by the controller.
[<RequireQualifiedAccess>]
type ArcOpenDisposition =
    | OpenedInCurrent of path: string
    | OpenedInNewWindow of path: string
    | FocusedExisting of path: string
    | CreatedInCurrent of path: string
    | CreatedInNewWindow of path: string

    member this.CreatedArcPath =
        match this with
        | CreatedInCurrent path
        | CreatedInNewWindow path -> Some path
        | OpenedInCurrent _
        | OpenedInNewWindow _
        | FocusedExisting _ -> None

module ArcOpenDisposition =
    let path =
        function
        | ArcOpenDisposition.OpenedInCurrent p
        | ArcOpenDisposition.OpenedInNewWindow p
        | ArcOpenDisposition.FocusedExisting p
        | ArcOpenDisposition.CreatedInCurrent p
        | ArcOpenDisposition.CreatedInNewWindow p -> p


type ArcVaults() =

    let pathInitializations = Dictionary<string, JS.Promise<unit>>()

    /// Key is window.id
    member val Vaults = Dictionary<int, ArcVault>() with get

    member this.Paths = this.Vaults.Values |> Seq.choose (fun x -> x.path) |> Array.ofSeq

    member this.BroadcastRecentARCs() =
        let recentARCs = RECENT_ARCS.Get()

        this.Vaults.Values
        |> Array.ofSeq
        |> fun arr ->
            if arr.Length > 0 then
                arr
                |> Array.iter (fun vault ->
                    WindowSend.send<IRecentArcsRendererApi> vault.window (fun api -> api.recentARCsUpdate recentARCs)
                )

    /// Centralized side-effect: update recent ARCs store and broadcast to all windows.
    member private this.TrackRecentAndBroadcast(arcPath: string) =
        let normalizedArcPath = PathHelpers.normalizePath arcPath
        RECENT_ARCS.Add(normalizedArcPath) |> ignore
        this.BroadcastRecentARCs()

    member this.DisposeVault(id: int) =
        match this.Vaults.TryGetValue(id) with
        | false, _ -> swatefailfn id "Failed to remove vault."
        | true, vault ->
            vault.StopFileWatcher() |> Promise.start
            this.Vaults.Remove(id) |> ignore

            match Main.VersionControl.WorkspaceSessionHost.tryCurrent (), vault.path with
            | Some host, Some path ->
                if host.IsIdle path then
                    if this.TryGetVaultByPath path |> Option.isNone then
                        host.CloseSession path |> Async.StartAsPromise |> Promise.start
                else
                    let rec closeWhenIdle () = promise {
                        do! host.WhenOperationsComplete(host.RunningOperationIds path)

                        if this.TryGetVaultByPath path |> Option.isSome then ()
                        elif not (host.IsIdle path) then do! closeWhenIdle ()
                        else do! host.CloseSession path |> Async.StartAsPromise
                    }

                    closeWhenIdle () |> Promise.start
            | _ -> ()

            vault.path |> Option.iter (fun p -> RECENT_ARCS.Inactivate(p) |> ignore)
            this.BroadcastRecentARCs()
            printfn $"[Swate] Removed vault '{id}'"

    member this.ResolveCloseRequest(windowId: int, decision: SaveBeforeQuitDecision) = promise {
        match this.TryGetVault(windowId) with
        | None ->
            let message = "Close request ignored. No vault found."
            swatelogfn windowId "%s" message
            return Error(exn message)
        | Some(vault: ArcVault) ->
            vault.isCloseRequestPending <- false

            match decision with
            | SaveBeforeQuitDecision.CancelClose ->
                swatelogfn windowId "Close request cancelled by user."
                return Ok()
            | SaveBeforeQuitDecision.CloseWithoutSaving ->
                swatelogfn windowId "Close request approved by user. Closing without saving."
                vault.RefreshHasUnsavedArcChangesFlag()
                vault.isCloseApproved <- true
                vault.window.close ()
                return Ok()
            | SaveBeforeQuitDecision.SaveAndClose ->
                swatelogfn windowId "Close request approved by user. Closing after main save."

                if vault.hasUnsavedArcChanges then
                    let! persistResult = vault.WriteArc()

                    match persistResult with
                    | Error saveError -> return Error saveError
                    | Ok() ->
                        vault.isCloseApproved <- true
                        vault.window.close ()
                        return Ok()
                else
                    vault.isCloseApproved <- true
                    vault.window.close ()
                    return Ok()
    }

    member this.OnCloseWindow(window: BrowserWindow, vault: ArcVault, id: int) =
        window.onFocus (fun () ->
            if vault.path.IsSome && not vault.isInitializingArc then
                vault.RefreshFileTreeDirectory ""
                |> Promise.catch (fun refreshError ->
                    swatelogfn id "Unable to refresh the ARC root after window focus: %s" refreshError.Message
                )
                |> Promise.start
        )

        window.onClose (fun closeEvent ->
            let operationCloseApproved = vault.isOperationCloseApproved
            vault.isOperationCloseApproved <- false

            let host = Main.VersionControl.WorkspaceSessionHost.tryCurrent ()

            let cancelWindowReads () =
                host
                |> Option.iter (fun currentHost ->
                    let mutationIds = currentHost.RunningMutationIdsForWindow id |> Set.ofArray

                    currentHost.RunningOperationIdsForWindow id
                    |> Array.filter (fun operationId -> not (Set.contains operationId mutationIds))
                    |> Array.iter (fun operationId -> currentHost.Cancel operationId |> ignore)
                )

            if not vault.isCloseApproved then
                if vault.activeFileImport.IsSome then
                    closeEvent.preventDefault ()

                    if not vault.isWaitingForImportCleanup then
                        vault.isWaitingForImportCleanup <- true

                        promise {
                            let! importResult = promise {
                                try
                                    let activeImport = vault.activeFileImport.Value

                                    if activeImport.State.phase = FileImportPhase.Copying then
                                        activeImport.AbortController.abort ()

                                    return! activeImport.Completion
                                with importError ->
                                    return Error importError
                            }

                            vault.isWaitingForImportCleanup <- false

                            match importResult with
                            | Ok _ -> window.close ()
                            | Error importError ->
                                swatelogfn id "Active import failed while closing: %s" importError.Message

                                if not (window.isDestroyed ()) then
                                    dialog.showErrorBox (
                                        "Could not close Swate",
                                        $"The active file import could not be rolled back completely. The window was kept open to avoid hiding a partial import.\n\n{importError.Message}"
                                    )
                        }
                        |> Promise.start
                elif vault.isInitializingArc then
                    swatelogfn id "Closing window directly because ARC initialization is still in progress."
                elif
                    host
                    |> Option.exists (fun currentHost ->
                        (currentHost.RunningMutationIdsForWindow id).Length > 0
                        && not operationCloseApproved
                    )
                then
                    closeEvent.preventDefault ()

                    if not vault.isWaitingForOperationsOnClose then
                        vault.isWaitingForOperationsOnClose <- true

                        promise {
                            try
                                match host with
                                | Some currentHost ->
                                    let! response =
                                        dialog.showMessageBox (
                                            ?window = Some(unbox<BaseWindow> window),
                                            ?options =
                                                Some(
                                                    Dialog.ShowMessageBox.Options(
                                                        message =
                                                            "A version control operation is still running in this window.",
                                                        ``type`` = Enums.Dialog.ShowMessageBox.Options.Type.Question,
                                                        detail =
                                                            $"Closing the window cancels it. The window closes once the operation has stopped, or after {closeWaitTimeoutMilliseconds / 1000} seconds if it does not stop.",
                                                        buttons = [|
                                                            "Cancel operation and close"
                                                            "Keep window open"
                                                        |],
                                                        defaultId = 1,
                                                        cancelId = 1
                                                    )
                                                )
                                        )

                                    if response.response = 0.0 then
                                        let operationIds = currentHost.RunningOperationIdsForWindow id

                                        operationIds
                                        |> Array.iter (fun operationId -> currentHost.Cancel operationId |> ignore)

                                        let waitForOperations = promise {
                                            do! currentHost.WhenOperationsComplete operationIds
                                            return true
                                        }

                                        let waitForTimeout = promise {
                                            do! Promise.sleep closeWaitTimeoutMilliseconds
                                            return false
                                        }

                                        let! operationsCompleted = race [| waitForOperations; waitForTimeout |]

                                        if not operationsCompleted then
                                            let stillRunning = currentHost.RunningOperationIdsForWindow id

                                            if stillRunning.Length > 0 then
                                                swatelogfn
                                                    id
                                                    "Timed out waiting for version control operations before close: %s"
                                                    (String.concat ", " stillRunning)

                                        vault.isOperationCloseApproved <- true
                                        vault.isWaitingForOperationsOnClose <- false

                                        if not (window.isDestroyed ()) then
                                            window.close ()
                                    else
                                        vault.isWaitingForOperationsOnClose <- false
                                | None -> vault.isWaitingForOperationsOnClose <- false
                            with closeError ->
                                vault.isWaitingForOperationsOnClose <- false

                                swatelogfn
                                    id
                                    "Unable to close while a version control operation is running: %s"
                                    closeError.Message
                        }
                        |> Promise.start
                elif vault.hasUnsavedArcChanges then
                    closeEvent.preventDefault ()

                    if not vault.isCloseRequestPending then
                        vault.isCloseRequestPending <- true

                        WindowSend.send<IMainSaveBeforeQuitApi> vault.window (fun api -> api.requestSaveBeforeQuit ())
                else
                    cancelWindowReads ()
                    swatelogfn id "Closing window directly because no unsaved ARC changes are present."
            else
                cancelWindowReads ()
        )

        window.onClosed (fun () ->
            vault.isWaitingForImportCleanup <- false
            vault.isWaitingForOperationsOnClose <- false
            vault.isOperationCloseApproved <- false
            vault.isCloseRequestPending <- false
            vault.isCloseApproved <- false

            if this.Vaults.ContainsKey(id) then
                this.DisposeVault(id)
        )

    member private this.CleanupFailedRegistration(window: BrowserWindow, vault: ArcVault, id: int) = promise {
        do! vault.StopFileWatcher()
        this.Vaults.Remove(id) |> ignore

        if not (window.isDestroyed ()) then
            window.destroy ()
    }

    member private this.RegisterVaultCore
        (initialize: ArcVault -> JS.Promise<unit>, ?onFailureBeforeCleanup: exn -> unit)
        : JS.Promise<ArcVault> =
        promise {
            let window = createWindow ()
            let id = window.id
            let vault = ArcVault(window)
            this.Vaults.Add(id, vault)
            this.OnCloseWindow(window, vault, id)

            try
                do! loadWindow window
                do! initialize vault

                window.focus ()
                swatelogfn id "Register window"

                return vault
            with error ->
                onFailureBeforeCleanup
                |> Option.iter (fun notifyFailure ->
                    try
                        notifyFailure error
                    with notificationError ->
                        swatelogfn id "Failed to report window registration error: %s" notificationError.Message
                )

                let targetWasDestroyed = window.isDestroyed ()
                do! this.CleanupFailedRegistration(window, vault, id)

                match error with
                | ArcLoadCancelledException _
                | ArcCreatedButClosedException _ -> return raise error
                | _ when targetWasDestroyed -> return raise (ArcLoadCancelledException id)
                | _ -> return raise error
        }

    member this.RegisterVault(?onFailureBeforeCleanup: exn -> unit) : JS.Promise<int> = promise {
        let! vault =
            this.RegisterVaultCore((fun _ -> promise { return () }), ?onFailureBeforeCleanup = onFailureBeforeCleanup)

        return vault.window.id
    }

    member private this.RegisterVaultWithValidatedArc(path: string) =
        this.RegisterVaultCore(fun vault -> promise {
            let initialization = vault.OpenARC(path)
            let! fileTree = this.InitializeFileTreeForActiveVault(vault.window.id, vault, path)
            vault.PublishArcInitializationSnapshot fileTree
            do! initialization
            vault.FinalizeArcInitialization()
        })

    member private this.ValidateArcRoot(path: string) = promise {
        let! arcRootExists = ARCtrl.FileSystemHelper.directoryExistsAsync path

        if not arcRootExists then
            return Error(exn $"The ARC cannot be found at location: '{path}'.")
        else
            let investigationPath =
                ARCtrl.ArcPathHelper.combine path ARCtrl.ArcPathHelper.InvestigationFileName

            let! investigationExists = ARCtrl.FileSystemHelper.fileExistsAsync investigationPath

            if investigationExists then
                return Ok()
            else
                return
                    Error(
                        exn
                            $"The folder does not contain the required ARC investigation file '{ARCtrl.ArcPathHelper.InvestigationFileName}'."
                    )
    }

    member this.RegisterVaultWithNewArc(path: string, newIdentifier: string) : JS.Promise<ArcVault> =
        this.RegisterVaultCore(fun vault -> promise {
            do! vault.CreateARC(path, newIdentifier)
            let! fileTree = this.InitializeFileTreeForCreatedVault(vault.window.id, vault, path)

            try
                vault.PublishArcInitializationSnapshot fileTree
                vault.FinalizeArcInitialization()
            with ArcLoadCancelledException targetWindowId ->
                return raise (ArcCreatedButClosedException targetWindowId)
        })

    member this.OpenARCInVault(windowId: int, path: string) = promise {
        let normalizedArcPath = PathHelpers.normalizePath path

        match! this.ValidateArcRoot normalizedArcPath with
        | Error error -> return raise error
        | Ok() ->
            match this.Vaults.TryGetValue windowId with
            | false, _ -> return failwith $"Vault with window-id '{windowId}' not found."
            | true, vault ->
                do!
                    this.InitializeArcInCurrentVault(
                        windowId,
                        vault,
                        normalizedArcPath,
                        false,
                        fun () -> vault.OpenARC(normalizedArcPath)
                    )
    }

    member this.CreateARCInVault(windowId: int, path: string, identifier: string) = promise {
        match this.Vaults.TryGetValue windowId with
        | false, _ -> failwith $"Vault with window-id '{windowId}' not found."
        | true, vault ->
            do!
                this.InitializeArcInCurrentVault(
                    windowId,
                    vault,
                    PathHelpers.normalizePath path,
                    true,
                    fun () -> vault.CreateARC(path, identifier)
                )

        return ()
    }

    member this.TryGetVault(windowId: int) =
        match this.Vaults.TryGetValue windowId with
        | true, vault -> Some vault
        | false, _ -> None

    member this.TryGetVaultByPath(path: string) =
        this.Vaults.Values
        |> Seq.tryFind (fun v -> v.path |> Option.exists (fun vaultPath -> PathHelpers.pathsEqual vaultPath path))

    member private this.EnsureVaultIsStillActive(windowId: int, expectedVault: ArcVault) =
        match this.TryGetVault windowId with
        | Some _ when not (expectedVault.window.isDestroyed ()) -> ()
        | _ -> raise (ArcLoadCancelledException windowId)

    member private this.InitializeFileTreeForActiveVault(windowId: int, expectedVault: ArcVault, arcPath: string) = promise {
        let! rootPageResult = expectedVault.EnsureInitialFileTreeRootPage arcPath |> Promise.result

        this.EnsureVaultIsStillActive(windowId, expectedVault)

        match rootPageResult with
        | Ok fileTree -> return fileTree
        | Error error -> return raise error
    }

    member private this.InitializeFileTreeForCreatedVault(windowId: int, expectedVault: ArcVault, arcPath: string) = promise {
        try
            return! this.InitializeFileTreeForActiveVault(windowId, expectedVault, arcPath)
        with ArcLoadCancelledException targetWindowId ->
            return raise (ArcCreatedButClosedException targetWindowId)
    }

    member private this.InitializeArcInCurrentVault
        (windowId: int, vault: ArcVault, arcPath: string, create: bool, initialize: unit -> JS.Promise<unit>)
        =
        promise {
            try
                do! promise {
                    if create then
                        // Creation must finish writing the scaffold before its root can be read.
                        do! initialize ()
                        let! fileTree = this.InitializeFileTreeForCreatedVault(windowId, vault, arcPath)
                        vault.PublishArcInitializationSnapshot fileTree
                    else
                        // Existing ARC metadata loading and the bounded root read can run together.
                        // Publishing the root first lets large ARCs leave the opening page immediately.
                        let initialization = initialize ()
                        let! fileTree = this.InitializeFileTreeForActiveVault(windowId, vault, arcPath)
                        vault.PublishArcInitializationSnapshot fileTree
                        do! initialization
                }

                try
                    vault.FinalizeArcInitialization()
                with ArcLoadCancelledException targetWindowId when create ->
                    return raise (ArcCreatedButClosedException targetWindowId)
            with error ->
                do! vault.RestoreEmptyVaultAfterFailedInitialization()
                return raise error
        }

    member private this.WithPathInitialization<'T>
        (arcPath: string, operation: unit -> JS.Promise<'T>)
        : JS.Promise<'T> =
        let pathKey = PathHelpers.normalizePathForFsComparison arcPath

        let rec waitForTurn () = promise {
            match pathInitializations.TryGetValue pathKey with
            | true, pending ->
                // A failed/cancelled owner must not poison the path. Once it settles, retry and
                // either use its active vault or become the next initializer.
                let! _ = pending |> Promise.result
                return! waitForTurn ()
            | false, _ ->
                let mutable release = ignore

                let completion =
                    JS.Constructors.Promise.Create(fun resolve _ -> release <- fun () -> resolve ())

                pathInitializations.Add(pathKey, completion)

                try
                    return! operation ()
                finally
                    pathInitializations.Remove(pathKey) |> ignore
                    release ()
        }

        waitForTurn ()

    // ── ARC Lifecycle Controller ──────────────────────────────────────────
    // All open/create/focus decisions are made here.
    // IPC handlers should delegate to these methods.

    /// Open an existing ARC at the given path.
    /// Decision: already-open → focus, calling window empty → open there, else → new window.
    member this.OpenOrFocusArc(callingWindowId: int, arcPath: string) = promise {
        let normalizedArcPath = PathHelpers.normalizePath arcPath

        return!
            this.WithPathInitialization(
                normalizedArcPath,
                fun () -> promise {
                    match this.TryGetVaultByPath normalizedArcPath with
                    | Some vault ->
                        vault.window.focus ()
                        this.TrackRecentAndBroadcast(normalizedArcPath)
                        return ArcOpenDisposition.FocusedExisting normalizedArcPath
                    | None ->
                        match! this.ValidateArcRoot normalizedArcPath with
                        | Error error -> return raise error
                        | Ok() ->
                            match this.TryGetVault callingWindowId with
                            | Some vault when vault.path.IsNone ->
                                do!
                                    this.InitializeArcInCurrentVault(
                                        callingWindowId,
                                        vault,
                                        normalizedArcPath,
                                        false,
                                        fun () -> vault.OpenARC(normalizedArcPath)
                                    )

                                this.TrackRecentAndBroadcast(normalizedArcPath)
                                return ArcOpenDisposition.OpenedInCurrent normalizedArcPath
                            | _ ->
                                let! _ = this.RegisterVaultWithValidatedArc(normalizedArcPath)
                                this.TrackRecentAndBroadcast(normalizedArcPath)
                                return ArcOpenDisposition.OpenedInNewWindow normalizedArcPath
                }
            )
    }

    /// Create a new ARC at the given path with the given identifier.
    /// Decision: path already open → focus, calling window empty → create there, else → new window.
    member this.CreateOrFocusArc(callingWindowId: int, arcPath: string, identifier: string) = promise {
        let normalizedArcPath = PathHelpers.normalizePath arcPath

        return!
            this.WithPathInitialization(
                normalizedArcPath,
                fun () -> promise {
                    match this.TryGetVaultByPath normalizedArcPath with
                    | Some vault ->
                        vault.window.focus ()
                        this.TrackRecentAndBroadcast(normalizedArcPath)
                        return ArcOpenDisposition.FocusedExisting normalizedArcPath
                    | None ->
                        match this.TryGetVault callingWindowId with
                        | Some vault when vault.path.IsNone ->
                            do!
                                this.InitializeArcInCurrentVault(
                                    callingWindowId,
                                    vault,
                                    normalizedArcPath,
                                    true,
                                    fun () -> vault.CreateARC(normalizedArcPath, identifier)
                                )

                            this.TrackRecentAndBroadcast(normalizedArcPath)
                            return ArcOpenDisposition.CreatedInCurrent normalizedArcPath
                        | _ ->
                            let! _ = this.RegisterVaultWithNewArc(normalizedArcPath, identifier)
                            this.TrackRecentAndBroadcast(normalizedArcPath)
                            return ArcOpenDisposition.CreatedInNewWindow normalizedArcPath
                }
            )
    }


let ARC_VAULTS: ArcVaults = ArcVaults()
