[<AutoOpen>]
module Main.ArcVault

open System.Collections.Generic
open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Fable.Electron.Remoting.Main
open Main
open Main.ARCtrlExtensions
open Main.Bindings
open Main.ArcMerge
open Main.ArcVaultHelper
open Main.FileImportCoordinator
open Swate.Components.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Swate.Electron.Shared.FileIOTypes
open ARCtrl

exception ArcLoadCancelledException of targetWindowId: int

let private startFileWatcherOwnWriteArcMergeSuppression suppressionMs currentTimeout onElapsed =
    currentTimeout |> Option.iter Fable.Core.JS.clearTimeout
    Fable.Core.JS.setTimeout onElapsed suppressionMs

/// <summary>
/// Represents a vault window in the application, optionally associated with a file path.
/// </summary>
/// <param name="path">Can be None if not opened ARC.</param>
type ArcVault(window: BrowserWindow) =

    let fileWatcherOwnWriteArcMergeSuppressionMs = 500
    let arcMergeQueue = ArcMergeQueue(window.id)

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
    member val watcher: Chokidar.IWatcher option = None with get, set
    member val fileWatcherReloadArcTimeout: int option = None with get, set
    member val fileWatcherPendingEvents: ResizeArray<ArcVaultFileSystemEvent> = ResizeArray() with get
    member val fileWatcherPendingArcMergeEvents: ResizeArray<ArcVaultFileSystemEvent> = ResizeArray() with get
    /// Imported paths awaiting their delayed Chokidar events. These events update the tree but must not re-merge the import.
    member val importedFileWatcherPaths: HashSet<string> = HashSet() with get
    member val private isBusyWritingValue: bool = false with get, set
    member val private fileWatcherOwnWriteArcMergeSuppressionTimeout: int option = None with get, set
    member val activeFileImport: ActiveFileImport option = None with get, set
    member val isWaitingForImportCleanup = false with get, set

    /// Runs ARC merges sequentially so every operation observes the result of the preceding merge.
    member this.EnqueueArcMerge<'T>(operation: unit -> Fable.Core.JS.Promise<'T>) : Fable.Core.JS.Promise<'T> =
        arcMergeQueue.Enqueue operation

    /// Indicates whether the vault is currently busy writing changes to disk.
    /// When a write finishes, watcher ARC merges stay suppressed briefly to cover delayed own-write events.
    member this.isBusyWriting
        with get () = this.isBusyWritingValue
        and set value =
            let wasBusyWriting = this.isBusyWritingValue
            this.isBusyWritingValue <- value

            if wasBusyWriting && not value then
                this.fileWatcherOwnWriteArcMergeSuppressionTimeout <-
                    startFileWatcherOwnWriteArcMergeSuppression
                        fileWatcherOwnWriteArcMergeSuppressionMs
                        this.fileWatcherOwnWriteArcMergeSuppressionTimeout
                        (fun () -> this.fileWatcherOwnWriteArcMergeSuppressionTimeout <- None)
                    |> Some

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
        let hadUnsavedArcChanges = this.hasUnsavedArcChanges
        this.arc <- None
        this.hasUnsavedArcChanges <- false

        if not (this.window.isDestroyed ()) then
            try
                this.window.title <- Swate.Electron.Shared.ApplicationVersion.windowTitle None
            with error ->
                swatelogfn this.window.id "Failed to reset ARC window title: %s" error.Message

            if hadUnsavedArcChanges then
                try
                    sendArcHasUnsavedChangesUpdate false this.window
                with error ->
                    swatelogfn this.window.id "Failed to reset ARC dirty state in renderer: %s" error.Message

    /// Sets the dirty marker for unsaved in-memory ARC mutations.
    member this.RefreshHasUnsavedArcChangesFlag() =
        // Use this value to only send updates to the renderer when the dirty state actually changes. This avoids redundant updates.
        if this.arc.IsSome then
            let hasNewChanges = this.arc.Value.hasInMemoryChanges ()
            let valueIsChanging = this.hasUnsavedArcChanges <> hasNewChanges
            this.hasUnsavedArcChanges <- hasNewChanges

            if valueIsChanging then
                sendArcHasUnsavedChangesUpdate hasNewChanges this.window

[<AutoOpen>]
module ArcVaultExtensions =

    type ArcVault with

        member private this.ApplyWatcherArcMerge(events: FileEvent list) : Fable.Core.JS.Promise<Result<unit, exn>> = promise {
            match this.path, this.arc with
            | Some _, None ->
                try
                    do! this.LoadArc()
                    return Ok()
                with loadError ->
                    return Error loadError
            | Some arcPath, Some arcLocal ->
                match! ARC.LoadAsyncSwate arcPath with
                | Error loadError -> return Error(exn (PathHelpers.formatContractErrors loadError))
                | Ok reloadedArc ->
                    // The file watcher is our source of truth for changes made on disk. Establish the reloaded
                    // ARC as a clean hash baseline before merging it; without this, unchanged entities have
                    // missing/stale hashes and the next save can unnecessarily overwrite multiple XLSX files.
                    baselineArcStaticHashes reloadedArc

                    let! mergeResult = promise {
                        try
                            return Ok(ARC.merge arcLocal reloadedArc events)
                        with mergeError ->
                            return Error mergeError
                    }

                    match mergeResult with
                    | Error mergeError -> return Error mergeError
                    | Ok mergedArc ->
                        // ARC.merge may copy or reconstruct entities without retaining the hashes established
                        // above, so transfer the disk baseline to the merged ARC. Watcher-applied values then
                        // stay clean, while values that still differ because of local edits remain unsaved.
                        syncArcStaticHashes reloadedArc mergedArc
                        this.SetArc mergedArc
                        this.RefreshHasUnsavedArcChangesFlag()
                        return Ok()
            | _ -> return Error(arcNotOpenError ())
        }

        member private this.ApplyWatcherFileTreeEvents(events: ArcVaultFileSystemEvent list) = promise {
            match this.path with
            | None -> ()
            | Some arcPath ->
                let mutable nextFileTree = this.fileTree
                let mutable hasFileTreeChanges = false

                for event in events do
                    try
                        if
                            WatcherHelpers.eventNameEquals Chokidar.Events.Add event.EventName
                            || WatcherHelpers.eventNameEquals Chokidar.Events.Change event.EventName
                        then
                            let! changedFile = getFileEntryWithLfsMetadata arcPath event.AbsolutePath
                            nextFileTree <- upsertFileEntry changedFile nextFileTree
                            hasFileTreeChanges <- true
                        elif WatcherHelpers.eventNameEquals Chokidar.Events.AddDir event.EventName then
                            let! addedDirectory = getFileEntry event.AbsolutePath
                            nextFileTree <- upsertFileEntry addedDirectory nextFileTree
                            hasFileTreeChanges <- true
                        elif
                            WatcherHelpers.eventNameEquals Chokidar.Events.Unlink event.EventName
                            || WatcherHelpers.eventNameEquals Chokidar.Events.UnlinkDir event.EventName
                        then
                            nextFileTree <- removePathAndDescendants event.AbsolutePath nextFileTree
                            hasFileTreeChanges <- true
                    with fileTreeError ->
                        swatelogfn
                            this.window.id
                            "Unable to update file tree for watcher event '%s' on '%s': %s"
                            event.EventName
                            event.RelativePath
                            fileTreeError.Message

                if hasFileTreeChanges then
                    this.SetFileTree(nextFileTree)
        }

        member this.TryTriggerArcInMemoryMergeOnFileWatcherEvents(events: ArcVaultFileSystemEvent list) = promise {
            let arcEvents = WatcherHelpers.toArcMergeEvents events
            return! this.EnqueueArcMerge(fun () -> this.ApplyWatcherArcMerge arcEvents)
        }

        member this.TriggerArcInMemoryMergeOnFileWatcherEvents(events: ArcVaultFileSystemEvent list) = promise {
            match! this.TryTriggerArcInMemoryMergeOnFileWatcherEvents events with
            | Ok() -> ()
            | Error mergeError ->
                swatelogfn this.window.id "Unable to merge ARC after file watcher event: %s" mergeError.Message
        }

        member private this._FileEventController(sendMsgApi: IArcFileWatcherApi) =
            let rec scheduleReload () =
                let timeoutId =
                    Fable.Core.JS.setTimeout
                        (fun () ->
                            promise {
                                swatelogfn this.window.id "Scheduled ARC reload triggered by file watcher."

                                if this.isBusyWriting && this.activeFileImport.IsSome then
                                    // Imports are long-running. Retain their queued tree and unrelated ARC events
                                    // until the import's explicit synchronization releases the writer.
                                    scheduleReload ()
                                else
                                    let pendingEvents = this.fileWatcherPendingEvents |> Seq.toList
                                    let pendingArcMergeEvents = this.fileWatcherPendingArcMergeEvents |> Seq.toList
                                    this.fileWatcherPendingEvents.Clear()
                                    this.fileWatcherPendingArcMergeEvents.Clear()

                                    // FileTree updates are renderer-visible and can trigger an immediate openFile call.
                                    // Merge first so that call reads the same ARC state represented by the published tree.
                                    if not pendingArcMergeEvents.IsEmpty && not this.isBusyWriting then
                                        do! this.TriggerArcInMemoryMergeOnFileWatcherEvents pendingArcMergeEvents

                                    do! this.ApplyWatcherFileTreeEvents pendingEvents

                                    this.fileWatcherReloadArcTimeout <- None
                                    sendMsgApi.IsLoadingChanges false
                            }
                            |> Promise.catch (fun ex ->
                                swatelogfn this.window.id "Scheduled ARC reload failed: %s" ex.Message
                                sendMsgApi.IsLoadingChanges false
                                this.fileWatcherReloadArcTimeout <- None
                            )
                            |> Promise.start
                        )
                        500

                this.fileWatcherReloadArcTimeout <- Some timeoutId

            fun (eventName: string) (path: string) ->

                swatelogfn this.window.id "File change detected: %s on %s" eventName path

                WatcherHelpers.queueFileWatcherEvent
                    (fun event ->
                        let isImportedPath =
                            this.importedFileWatcherPaths.Remove(
                                PathHelpers.normalizePath (event.AbsolutePath.ToLowerInvariant())
                            )

                        not isImportedPath
                        && (this.activeFileImport.IsSome || this.IsFileWatcherArcMergeEligible)
                    )
                    this.path
                    this.fileWatcherPendingEvents
                    this.fileWatcherPendingArcMergeEvents
                    eventName
                    path

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
                this.isBusyWriting <- true

                try
                    try
                        match! arc.TryUpdateAsyncSwate(arcPath) with
                        | Error errors -> return Error(exn (PathHelpers.formatContractErrors errors))
                        | Ok _ ->
                            this.RefreshHasUnsavedArcChangesFlag()
                            return Ok()
                    with e ->
                        return Error(exn $"Failed to persist ARC to disk: {e.Message}")
                finally
                    this.isBusyWriting <- false
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
                    let wasBusyWriting = this.isBusyWriting
                    this.isBusyWriting <- true

                    try
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
                                    let! refreshedFileTree =
                                        refreshFileTreeEntry arcPath (DatamapParentInfo.toPath parentInfo) this.fileTree

                                    this.SetFileTree refreshedFileTree
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
                    finally
                        this.isBusyWriting <- wasBusyWriting
            | _ -> return Error(arcNotOpenError ())
        }

        member this.SetFileTree(fileTree: Dictionary<string, FileEntry>) =
            this.fileTree <- fileTree

            let sendMsg =
                Remoting.createIpc ()
                |> Remoting.withWindow this.window
                |> Remoting.buildProxySender<IFileTreeRendererApi>

            let rendererFileTree =
                match this.path with
                | Some arcPath -> toRendererFileTree arcPath fileTree.Values
                | None -> Dictionary<string, FileEntry>()

            sendMsg.fileTreeUpdate rendererFileTree

        member this.GetRendererFileTreeSnapshot() = promise {
            match this.path with
            | None -> return Dictionary<string, FileEntry>()
            | Some arcPath ->
                if this.fileTree.Count = 0 then
                    let! fileTree = getFileTree arcPath
                    this.fileTree <- fileTree

                return toRendererFileTree arcPath this.fileTree.Values
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

        member this.StartFileWatcher(?usePolling: bool) =
            if this.path.IsSome then
                match this.watcher with
                | Some _ -> ()
                | None ->
                    let watcher = createFileWatcher this.path.Value usePolling

                    let sendMsgApi =
                        Remoting.createIpc ()
                        |> Remoting.withWindow this.window
                        |> Remoting.buildProxySender<IArcFileWatcherApi>

                    watcher.on (Chokidar.Events.All, this._FileEventController sendMsgApi) |> ignore
                    this.watcher <- Some watcher
            else
                swatefailfn this.window.id "No path set for StartFileWatcher."

        member this.ClearPendingFileWatcherState() =
            this.fileWatcherReloadArcTimeout |> Option.iter Fable.Core.JS.clearTimeout
            this.fileWatcherReloadArcTimeout <- None
            this.fileWatcherPendingEvents.Clear()
            this.fileWatcherPendingArcMergeEvents.Clear()
            this.importedFileWatcherPaths.Clear()

        member this.StopFileWatcher() = promise {
            match this.watcher with
            | None -> ()
            | Some watcher ->
                try
                    do! watcher.close ()
                with _ ->
                    ()

            this.watcher <- None
            this.ClearPendingFileWatcherState()
        }

        member private this.RestoreEmptyVaultAfterFailedInitialization() = promise {
            do! this.StopFileWatcher()
            this.path <- None
            this.fileTree.Clear()
            this.ClearArc()
        }

        /// This functions should be called once, when an vault is first started with a path
        member this.Startup() = promise {
            do! this.LoadArc()

            if this.window.isDestroyed () then
                return raise (ArcLoadCancelledException this.window.id)
            else
                this.StartFileWatcher()

                this.window.title <-
                    Swate.Electron.Shared.ApplicationVersion.windowTitle (Some this.arc.Value.Identifier)
        }

        member this.OpenARC(path: string) = promise {
            match this.path with
            | Some _ -> swatefailfn this.window.id "Unable to open ARC in vault bound to ARC."
            | None ->
                let normalizedPath = PathHelpers.normalizePath path

                let sendMsg =
                    Remoting.createIpc ()
                    |> Remoting.withWindow this.window
                    |> Remoting.buildProxySender<IPathChangeRendererApi>

                swatelogfn this.window.id "path: %s" normalizedPath
                this.path <- Some normalizedPath

                try
                    do! this.Startup()
                    sendMsg.pathChange (Some normalizedPath)
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

                let sendMsg =
                    Remoting.createIpc ()
                    |> Remoting.withWindow this.window
                    |> Remoting.buildProxySender<IPathChangeRendererApi>

                try
                    let arc = ARC(identifier)
                    this.path <- Some normalizedPath
                    this.SetArc(arc)
                    this.RefreshHasUnsavedArcChangesFlag()
                    this.isBusyWriting <- true

                    try
                        match! arc.TryWriteAsyncSwate(normalizedPath) with
                        | Ok _ -> ()
                        | Error errors ->
                            failwithf
                                "Could not write ARC, failed with the following errors %s"
                                (PathHelpers.formatContractErrors errors)
                    finally
                        this.isBusyWriting <- false

                    do! this.Startup()
                    sendMsg.pathChange (Some normalizedPath)
                with error ->
                    do! this.RestoreEmptyVaultAfterFailedInitialization()
                    return raise error
        }

        member this.RenameOpenArcRoot(newName: string) : Fable.Core.JS.Promise<Result<string, exn>> = promise {
            match this.path with
            | None -> return Error(arcNotOpenError ())
            | Some currentPath ->
                let hadWatcher = this.watcher.IsSome

                if hadWatcher then
                    do! this.StopFileWatcher()
                else
                    this.ClearPendingFileWatcherState()

                let! renameResult = renameOpenArcRootDirectoryOnDisk currentPath newName

                match renameResult with
                | Error renameError ->
                    if hadWatcher then
                        this.StartFileWatcher()

                    return Error renameError
                | Ok renamedPath ->
                    this.path <- Some renamedPath

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
                        let! fileTree = getFileTree renamedPath
                        this.SetFileTree fileTree
                    with refreshError ->
                        swatelogfn
                            this.window.id
                            "ARC folder was renamed to '%s', but file tree refresh failed: %s"
                            renamedPath
                            refreshError.Message

                    try
                        let sendMsg =
                            Remoting.createIpc ()
                            |> Remoting.withWindow this.window
                            |> Remoting.buildProxySender<IPathChangeRendererApi>

                        sendMsg.pathChange (Some renamedPath)
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
                    if not (vault.window.isDestroyed ()) then
                        Remoting.createIpc ()
                        |> Remoting.withWindow vault.window
                        |> Remoting.buildProxySender<IRecentArcsRendererApi>
                        |> fun client -> client.recentARCsUpdate recentARCs
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
        window.onClose (fun closeEvent ->
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
                elif vault.hasUnsavedArcChanges then
                    closeEvent.preventDefault ()

                    if not vault.isCloseRequestPending then
                        vault.isCloseRequestPending <- true

                        let saveBeforeQuitClient =
                            Remoting.createIpc ()
                            |> Remoting.withWindow vault.window
                            |> Remoting.buildProxySender<IMainSaveBeforeQuitApi>

                        saveBeforeQuitClient.requestSaveBeforeQuit ()
                else
                    swatelogfn id "Closing window directly because no unsaved ARC changes are present."
        )

        window.onClosed (fun () ->
            vault.isWaitingForImportCleanup <- false
            vault.isCloseRequestPending <- false
            vault.isCloseApproved <- false

            match this.TryGetVault(id) with
            | Some registeredVault when obj.ReferenceEquals(registeredVault, vault) -> this.DisposeVault(id)
            | _ -> ()
        )

    member private this.CleanupFailedRegistration(window: BrowserWindow, vault: ArcVault, id: int) = promise {
        do! vault.StopFileWatcher()
        this.Vaults.Remove(id) |> ignore

        if not (window.isDestroyed ()) then
            window.destroy ()
    }

    member this.RegisterVault(?onFailureBeforeCleanup: exn -> unit) : Fable.Core.JS.Promise<int> = promise {
        let window = createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)
        this.OnCloseWindow(window, vault, id)

        try
            do! loadWindow window

            window.focus ()
            swatelogfn id "Register window"

            return id
        with error ->
            onFailureBeforeCleanup
            |> Option.iter (fun notifyFailure ->
                try
                    notifyFailure error
                with notificationError ->
                    swatelogfn id "Failed to report window registration error: %s" notificationError.Message
            )

            do! this.CleanupFailedRegistration(window, vault, id)
            return raise error
    }

    member private this.RegisterVaultWithValidatedArc(path: string) = promise {
        let window = createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)
        this.OnCloseWindow(window, vault, id)

        try
            do! loadWindow window
            do! vault.OpenARC(path)

            window.focus ()
            swatelogfn id "Register window"

            return id
        with error ->
            do! this.CleanupFailedRegistration(window, vault, id)
            return raise error
    }

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

    member this.RegisterVaultWithNewArc(path: string, newIdentifier: string) : Fable.Core.JS.Promise<int> = promise {
        let window = createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)
        this.OnCloseWindow(window, vault, id)

        try
            do! loadWindow window
            do! vault.CreateARC(path, newIdentifier)

            window.focus ()
            swatelogfn id "Register window"

            return id
        with error ->
            do! this.CleanupFailedRegistration(window, vault, id)
            return raise error
    }

    member this.OpenARCInVault(windowId: int, path: string) = promise {
        let normalizedArcPath = PathHelpers.normalizePath path

        match! this.ValidateArcRoot normalizedArcPath with
        | Error error -> return raise error
        | Ok() ->
            match this.Vaults.TryGetValue windowId with
            | false, _ -> return failwith $"Vault with window-id '{windowId}' not found."
            | true, vault -> do! vault.OpenARC normalizedArcPath
    }

    member this.CreateARCInVault(windowId: int, path: string, identifier: string) = promise {
        match this.Vaults.TryGetValue windowId with
        | false, _ -> failwith $"Vault with window-id '{windowId}' not found."
        | true, vault -> do! vault.CreateARC(path, identifier)

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
        | Some registeredVault when
            obj.ReferenceEquals(registeredVault, expectedVault)
            && not (expectedVault.window.isDestroyed ())
            ->
            ()
        | _ -> raise (ArcLoadCancelledException windowId)

    member private this.InitializeFileTreeForActiveVault(windowId: int, expectedVault: ArcVault, arcPath: string) = promise {
        let! fileTreeResult = promise {
            try
                let! fileTree = getFileTree arcPath
                return Ok fileTree
            with error ->
                return Error error
        }

        this.EnsureVaultIsStillActive(windowId, expectedVault)

        match fileTreeResult with
        | Ok fileTree -> expectedVault.SetFileTree fileTree
        | Error error -> return raise error
    }

    // ── ARC Lifecycle Controller ──────────────────────────────────────────
    // All open/create/focus decisions are made here.
    // IPC handlers should delegate to these methods.

    /// Open an existing ARC at the given path.
    /// Decision: already-open → focus, calling window empty → open there, else → new window.
    member this.OpenOrFocusArc(callingWindowId: int, arcPath: string) = promise {
        let normalizedArcPath = PathHelpers.normalizePath arcPath

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
                    do! vault.OpenARC(normalizedArcPath)
                    do! this.InitializeFileTreeForActiveVault(callingWindowId, vault, normalizedArcPath)
                    this.TrackRecentAndBroadcast(normalizedArcPath)
                    return ArcOpenDisposition.OpenedInCurrent normalizedArcPath
                | _ ->
                    let! newWindowId = this.RegisterVaultWithValidatedArc(normalizedArcPath)

                    let newVault =
                        match this.TryGetVault newWindowId with
                        | Some vault -> vault
                        | None -> raise (ArcLoadCancelledException newWindowId)

                    do! this.InitializeFileTreeForActiveVault(newWindowId, newVault, normalizedArcPath)
                    this.TrackRecentAndBroadcast(normalizedArcPath)
                    return ArcOpenDisposition.OpenedInNewWindow normalizedArcPath
    }

    /// Create a new ARC at the given path with the given identifier.
    /// Decision: path already open → focus, calling window empty → create there, else → new window.
    member this.CreateOrFocusArc(callingWindowId: int, arcPath: string, identifier: string) = promise {
        let normalizedArcPath = PathHelpers.normalizePath arcPath

        match this.TryGetVaultByPath normalizedArcPath with
        | Some vault ->
            vault.window.focus ()
            this.TrackRecentAndBroadcast(normalizedArcPath)
            return ArcOpenDisposition.FocusedExisting normalizedArcPath
        | None ->
            match this.TryGetVault callingWindowId with
            | Some vault when vault.path.IsNone ->
                do! vault.CreateARC(normalizedArcPath, identifier)
                do! this.InitializeFileTreeForActiveVault(callingWindowId, vault, normalizedArcPath)
                this.TrackRecentAndBroadcast(normalizedArcPath)
                return ArcOpenDisposition.CreatedInCurrent normalizedArcPath
            | _ ->
                let! newWindowId = this.RegisterVaultWithNewArc(normalizedArcPath, identifier)

                let newVault =
                    match this.TryGetVault newWindowId with
                    | Some vault -> vault
                    | None -> raise (ArcLoadCancelledException newWindowId)

                do! this.InitializeFileTreeForActiveVault(newWindowId, newVault, normalizedArcPath)
                this.TrackRecentAndBroadcast(normalizedArcPath)
                return ArcOpenDisposition.CreatedInNewWindow normalizedArcPath
    }


let ARC_VAULTS: ArcVaults = ArcVaults()
