[<AutoOpen>]
module Main.ArcVault

open System.Collections.Generic
open Fable.Electron
open Fable.Electron.Remoting.Main
open Main
open Main.Bindings
open Main.ArcVaultHelper
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc
open Swate.Electron.Shared.FileIOTypes
open ARCtrl

/// <summary>
/// Represents a vault window in the application, optionally associated with a file path.
/// </summary>
/// <param name="path">Can be None if not opened ARC.</param>
type ArcVault(window: BrowserWindow) =

    member val window: BrowserWindow = window with get
    member val path: string option = None with get, set
    member val arc: ARC option = None with get, set
    /// Dirty marker for unsaved in-memory ARC mutations.
    /// This flag is intentionally coarse and can remain true even if later edits restore the previous logical state.
    ///Good workaround, for a missing member 👍 Might even be more performant, than calculating a isDirty flag. On the other hand, it is unable to detect if changes are removed again. For example:
    ///Add Assay 1 -> Set hasUnsavedArcChanges <- true
    ///Remove Assay 1 -> Still true, even tough changes were removed again and it is in its original state.
    ///I am not sure if performance of calculating changes or precision should be more important. Lets see in the future. Maybe you can add this comment as /// comment on this member?
    member val hasUnsavedArcChanges: bool = false with get, set
    member val fileTree: Dictionary<string, FileEntry> = Dictionary<string, FileEntry>() with get, set
    member val watcher: Chokidar.IWatcher option = None with get, set
    member val fileWatcherReloadArcTimeout: int option = None with get, set
    /// Indicates whether the vault is currently busy writing changes to disk.
    /// This should disable reloads from the file watcher.
    member val isBusyWriting: bool = false with get, set
    /// Indicates whether a close confirmation dialog is currently open.
    member val isCloseRequestPending: bool = false with get, set
    /// Allows a confirmed close to pass through the onClose handler exactly once.
    member val isCloseApproved: bool = false with get, set

[<AutoOpen>]
module ArcVaultExtensions =
    type ArcVault with

        member private this._ScheduleReloadArc(sendMsgApi: IArcFileWatcherApi) =

            fun (eventName: string) (path: string) ->
                // Clear any existing timeout
                match this.fileWatcherReloadArcTimeout with
                | Some timeoutId ->
                    Fable.Core.JS.clearTimeout timeoutId
                    this.fileWatcherReloadArcTimeout <- None
                | None ->
                    if this.isBusyWriting then
                        sendMsgApi.IsLoadingChanges true

                // If busy writing, skip reload
                if this.isBusyWriting then
                    swatelogfn this.window.id "Skipping ARC reload due to busy writing."
                    sendMsgApi.IsLoadingChanges false
                else
                    swatelogfn this.window.id "File change detected: %s on %s" eventName path
                    // Schedule a new reload after 500ms
                    let timeoutId =
                        Fable.Core.JS.setTimeout
                            (fun () ->
                                promise {
                                    swatelogfn this.window.id "Scheduled ARC reload triggered by file watcher."
                                    do! this.LoadArc()
                                    sendMsgApi.IsLoadingChanges false

                                    match this.path with
                                    | None -> ()
                                    | Some arcPath ->
                                        let eventPath =
                                            $"{arcPath}\{path}"
                                                .Replace(ArcPathHelper.PathSeperatorWindows, ArcPathHelper.PathSeperator)

                                        match eventName.ToLowerInvariant() with
                                        | name when name = Chokidar.Events.Add.ToString().ToLowerInvariant()
                                                    || name = Chokidar.Events.Change.ToString().ToLowerInvariant() ->
                                            let! changedFile = getFileEntryWithLfsMetadata arcPath eventPath
                                            let nextFileTree = upsertFileEntry changedFile this.fileTree
                                            this.SetFileTree(nextFileTree)
                                        | name when name = Chokidar.Events.AddDir.ToString().ToLowerInvariant() ->
                                            let! addedDirectory = getFileEntry eventPath
                                            let nextFileTree = upsertFileEntry addedDirectory this.fileTree
                                            this.SetFileTree(nextFileTree)
                                        | name when name = Chokidar.Events.Unlink.ToString().ToLowerInvariant()
                                                    || name = Chokidar.Events.UnlinkDir.ToString().ToLowerInvariant() ->
                                            let nextFileTree = removePathAndDescendants eventPath this.fileTree
                                            this.SetFileTree(nextFileTree)
                                        | _ -> ()

                                    this.fileWatcherReloadArcTimeout <- None
                                }
                                |> Promise.catch (fun ex ->
                                    swatelogfn this.window.id "Scheduled ARC reload failed: %s" ex.Message
                                    sendMsgApi.IsLoadingChanges false
                                    this.fileWatcherReloadArcTimeout <- None)
                                |> Promise.start
                            )
                            500

                    this.fileWatcherReloadArcTimeout <- Some timeoutId

        /// This function mutably sets the active ARC in memory without persisting to disk.
        member this.SetArc(arc: ARC) =
            this.arc <- Some arc
            this.window.title <- arc.Identifier

        /// Applies an ARC content DTO to the in-memory ARC and marks the vault dirty.
        member this.UpdateArcBy(request: FileContentDTO) : Result<unit, exn> =
            match this.arc with
            | None -> Error(exn "ARC is not loaded.")
            | Some arc ->
                let normalizedRequest = Swate.Electron.Shared.FileIOHelper.FileContentDTO.normalizeArcFileRequestPath request

                match updateARCByFileContentDTO arc normalizedRequest with
                | Error saveError -> Error saveError
                | Ok newArc ->
                    this.SetArc(newArc)
                    this.hasUnsavedArcChanges <- true
                    Ok()

        /// Writes the active in-memory ARC scaffold to disk using ARCtrl UpdateAsync.
        member this.WriteArc() : Fable.Core.JS.Promise<Result<unit, exn>> = promise {
            match this.path, this.arc with
            | Some arcPath, Some arc ->
                this.isBusyWriting <- true

                try
                    try
                        do! arc.UpdateAsync(arcPath)
                        this.hasUnsavedArcChanges <- false
                        return Ok()
                    with e ->
                        return Error(exn $"Failed to persist ARC to disk: {e.Message}")
                finally
                    this.isBusyWriting <- false
            | _ -> return Error(exn "ARC is not loaded.")
        }

        /// Applies an ARC file request on a copy and persists it.
        /// The in-memory ARC is only replaced after successful persistence.
        member this.ApplyArcFileAndSave(request: FileContentDTO) : Fable.Core.JS.Promise<Result<unit, exn>> = promise {
            match this.path, this.arc with
            | Some arcPath, Some arc ->
                let normalizedRequest =
                    Swate.Electron.Shared.FileIOHelper.FileContentDTO.normalizeArcFileRequestPath request

                let workingArc = arc.Copy()

                match updateARCByFileContentDTO workingArc normalizedRequest with
                | Error updateError -> return Error updateError
                | Ok updatedArc ->
                    this.isBusyWriting <- true

                    try
                        try
                            do! updatedArc.UpdateAsync(arcPath)
                            this.SetArc(updatedArc)
                            this.hasUnsavedArcChanges <- false
                            return Ok()
                        with e ->
                            return Error(exn $"Failed to persist ARC to disk: {e.Message}")
                    finally
                        this.isBusyWriting <- false
            | _ -> return Error(exn "ARC is not loaded.")
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
                    let! fileEntries = getFileEntries arcPath
                    this.fileTree <- createFileEntryTree fileEntries

                return toRendererFileTree arcPath this.fileTree.Values
        }

        member this.LoadArc() = promise {
            if this.path.IsSome then
                match! ARC.tryLoadAsync (this.path.Value) with
                | Error e -> swatefailfn this.window.id $"Unable to load ARC: {e}"
                | Ok arc ->
                    this.arc <- Some arc
                    this.hasUnsavedArcChanges <- false
            else
                swatefailfn this.window.id $"No path set for StartFileWatcher."
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

                    watcher.on (Chokidar.Events.All, this._ScheduleReloadArc sendMsgApi) |> ignore
                    this.watcher <- Some watcher
            else
                swatefailfn this.window.id $"No path set for StartFileWatcher."

        member this.StopFileWatcher() = promise {
            match this.watcher with
            | None -> return ()
            | Some watcher ->
                try
                    do! watcher.close ()
                with _ ->
                    ()

                this.watcher <- None
        }

        /// This functions should be called once, when an vault is first started with a path
        member this.Startup() = promise {
            this.StartFileWatcher()
            do! this.LoadArc()
            this.window.title <- this.arc.Value.Identifier
        }

        member this.OpenARC(path: string) = promise {
            match this.path with
            | Some _ -> swatefailfn this.window.id "Unable to open ARC in vault bound to ARC."
            | None ->
                let sendMsg =
                    Remoting.createIpc ()
                    |> Remoting.withWindow this.window
                    |> Remoting.buildProxySender<IPathChangeRendererApi>

                swatelogfn this.window.id "path: %s" path
                this.path <- Some path
                do! this.Startup()
                sendMsg.pathChange (Some path)
        }

        member this.CreateARC(path: string, identifier: string) = promise {
            match this.path, this.arc with
            | Some _, _ -> swatefailfn this.window.id "Unable to create ARC in vault bound to path."
            | _, Some _ -> swatefailfn this.window.id "Unable to create ARC in vault bound to ARC."
            | None, None ->
                let sendMsg =
                    Remoting.createIpc ()
                    |> Remoting.withWindow this.window
                    |> Remoting.buildProxySender<IPathChangeRendererApi>

                let arc = ARC(identifier)
                this.path <- Some path
                this.arc <- Some arc
                this.hasUnsavedArcChanges <- false
                this.isBusyWriting <- true

                try
                    do! arc.WriteAsync(path)
                finally
                    this.isBusyWriting <- false

                do! this.Startup()
                sendMsg.pathChange (Some path)
        }

        /// Load file entries from disk and push the file tree to the renderer.
        member this.RefreshFileTree() = promise {
            match this.path with
            | Some arcPath ->
                let! fileEntries = getFileEntries arcPath
                let fileTree = createFileEntryTree fileEntries
                this.SetFileTree(fileTree)
            | None -> ()
        }


/// Describes the outcome of an ARC lifecycle action performed by the controller.
[<RequireQualifiedAccess>]
type ArcOpenDisposition =
    | OpenedInCurrent of path: string
    | OpenedInNewWindow of path: string
    | FocusedExisting of path: string
    | CreatedInCurrent of path: string
    | CreatedInNewWindow of path: string

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
                    Remoting.createIpc ()
                    |> Remoting.withWindow vault.window
                    |> Remoting.buildProxySender<IRecentArcsRendererApi>
                    |> fun client -> client.recentARCsUpdate recentARCs
                )

    /// Centralized side-effect: update recent ARCs store and broadcast to all windows.
    member private this.TrackRecentAndBroadcast(arcPath: string) =
        RECENT_ARCS.Add(arcPath) |> ignore
        this.BroadcastRecentARCs()

    member this.DisposeVault(id: int) =
        match this.Vaults.TryGetValue(id) with
        | false, _ -> swatefailfn id $"Failed to remove vault."
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
                vault.hasUnsavedArcChanges <- false
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
                if vault.hasUnsavedArcChanges then
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
            vault.isCloseRequestPending <- false
            vault.isCloseApproved <- false
            this.DisposeVault(id)
        )

    member this.RegisterVault() : Fable.Core.JS.Promise<int> = promise {
        let! window = createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)

        this.OnCloseWindow(window, vault, id)

        window.focus ()
        swatelogfn id $"Register window"

        return id
    }

    member this.RegisterVaultWithArc(path: string) = promise {
        let! window = createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)
        do! vault.OpenARC(path)

        this.OnCloseWindow(window, vault, id)

        window.focus ()
        swatelogfn id $"Register window"

        return id
    }

    member this.RegisterVaultWithNewArc(path: string, newIdentifier: string) : Fable.Core.JS.Promise<int> = promise {
        let! window = createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)

        do! vault.CreateARC(path, newIdentifier)

        this.OnCloseWindow(window, vault, id)

        window.focus ()
        swatelogfn id $"Register window"

        return id
    }

    member this.OpenARCInVault(windowId: int, path: string) = promise {
        match this.Vaults.TryGetValue windowId with
        | false, _ -> failwith $"Vault with window-id '{windowId}' not found."
        | true, vault -> do! vault.OpenARC path

        return ()
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
        this.Vaults.Values |> Seq.tryFind (fun v -> v.path = Some path)

    // ── ARC Lifecycle Controller ──────────────────────────────────────────
    // All open/create/focus decisions are made here.
    // IPC handlers should delegate to these methods.

    /// Open an existing ARC at the given path.
    /// Decision: already-open → focus, calling window empty → open there, else → new window.
    member this.OpenOrFocusArc(callingWindowId: int, arcPath: string) = promise {
        match this.TryGetVaultByPath arcPath with
        | Some vault ->
            vault.window.focus ()
            this.TrackRecentAndBroadcast(arcPath)
            return ArcOpenDisposition.FocusedExisting arcPath
        | None ->
            match this.TryGetVault callingWindowId with
            | Some vault when vault.path.IsNone ->
                do! vault.OpenARC(arcPath)
                do! vault.RefreshFileTree()
                this.TrackRecentAndBroadcast(arcPath)
                return ArcOpenDisposition.OpenedInCurrent arcPath
            | _ ->
                let! newWindowId = this.RegisterVaultWithArc(arcPath)

                match this.TryGetVault newWindowId with
                | Some newVault -> do! newVault.RefreshFileTree()
                | None -> ()

                this.TrackRecentAndBroadcast(arcPath)
                return ArcOpenDisposition.OpenedInNewWindow arcPath
    }

    /// Create a new ARC at the given path with the given identifier.
    /// Decision: path already open → focus, calling window empty → create there, else → new window.
    member this.CreateOrFocusArc(callingWindowId: int, arcPath: string, identifier: string) = promise {
        match this.TryGetVaultByPath arcPath with
        | Some vault ->
            vault.window.focus ()
            this.TrackRecentAndBroadcast(arcPath)
            return ArcOpenDisposition.FocusedExisting arcPath
        | None ->
            match this.TryGetVault callingWindowId with
            | Some vault when vault.path.IsNone ->
                do! vault.CreateARC(arcPath, identifier)
                do! vault.RefreshFileTree()
                this.TrackRecentAndBroadcast(arcPath)
                return ArcOpenDisposition.CreatedInCurrent arcPath
            | _ ->
                let! newWindowId = this.RegisterVaultWithNewArc(arcPath, identifier)

                match this.TryGetVault newWindowId with
                | Some newVault -> do! newVault.RefreshFileTree()
                | None -> ()

                this.TrackRecentAndBroadcast(arcPath)
                return ArcOpenDisposition.CreatedInNewWindow arcPath
    }


let ARC_VAULTS: ArcVaults = ArcVaults()
