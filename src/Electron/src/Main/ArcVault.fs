[<AutoOpen>]
module Main.ArcVault

open Fable.Electron
open Fable.Electron.Remoting.Main
open Main
open System.Collections.Generic
open Main.Bindings
open Swate.Components
open Swate.Electron.Shared.IPCTypes
open ARCtrl

module ArcVaultHelper =

    open Fable.Core.JsInterop
    open Node.Api

    let swatelogfn id fmt =
        Printf.kprintf (fun s -> Browser.Dom.console.log ("[Swate-" + string id + "] " + s)) fmt

    let swatefailfn id fmt =
        Printf.kprintf (fun s -> failwith ("[Swate-" + string id + "] " + s)) fmt

    let createWindow () = promise {
        printfn "[Swate] Creating new window"
        let screenSize = screen.getPrimaryDisplay().workAreaSize

        let mainWindowOptions =
            BrowserWindowConstructorOptions(
                width = int screenSize.width,
                height = int screenSize.height,
                webPreferences = WebPreferences(preload = path.join (__dirname, "preload.fs.js"))
            )

        let window = BrowserWindow(mainWindowOptions)

        if isNullOrUndefined MAIN_WINDOW_VITE_DEV_SERVER_URL then
            do! window.loadFile (path.join (__dirname, $"../renderer/{MAIN_WINDOW_VITE_NAME}/index.html"))
        else
            window.webContents.openDevTools Enums.WebContents.OpenDevTools.Options.Mode.Right
            do! window.loadURL MAIN_WINDOW_VITE_DEV_SERVER_URL

        return window
    }

    let createFileWatcher (path: string) =

        let ignoreFn =
            fun (path: string) ->
                let normalizedPath = path.Replace("\\", "/")

                let tempXlsxPattern = """\.~\$.*\.xlsx$"""
                let dotFolders = """(^|\/)\.[^/]+\/.+"""

                // skip temporary Excel files (created when editing an xlsx file)
                if System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, tempXlsxPattern) then
                    true
                // skip dot-folders (e.g., .git/, .vscode/, .idea/, etc.). This will also match if a subfolder is a dot-folder
                elif System.Text.RegularExpressions.Regex.IsMatch(normalizedPath, dotFolders) then
                    true
                else
                    false

        let watcher =
            Chokidar.Chokidar.watch (
                path,
                Chokidar.WatchOptions(cwd = path, awaitWriteFinish = true, ignored = !^ignoreFn, ignoreInitial = true)
            )

        watcher

open ArcVaultHelper

/// <summary>
/// Represents a vault window in the application, optionally associated with a file path.
/// </summary>
/// <param name="path">Can be None if not opened ARC.</param>
type ArcVault(window: BrowserWindow) =

    member val window: BrowserWindow = window with get
    member val path: string option = None with get, set
    member val arc: ARC option = None with get, set
    member val watcher: Chokidar.IWatcher option = None with get, set
    member val fileWatcherReloadArcTimeout: int option = None with get, set
    /// Indicates whether the vault is currently busy writing changes to disk.
    /// This should disable reloads from the file watcher.
    member val isBusyWriting: bool = false with get, set

[<AutoOpen>]
module ArcVaultExtensions =
    type ArcVault with

        member private this._ScheduleReloadArc(sendMsgApi: IArcFileWatcherApi) =

            fun (eventName: string) (path: string) ->
                // Clear any existing timeout
                match this.fileWatcherReloadArcTimeout with
                | Some timeoutId -> Fable.Core.JS.clearTimeout timeoutId
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
                                }
                                |> Promise.start
                            )
                            500

                    this.fileWatcherReloadArcTimeout <- Some timeoutId

        member this.LoadArc() = promise {
            if this.path.IsSome then
                match! ARC.tryLoadAsync (this.path.Value) with
                | Error e -> swatefailfn this.window.id $"Unable to load ARC: {e}"
                | Ok arc -> this.arc <- Some arc
            else
                swatefailfn this.window.id $"No path set for StartFileWatcher."
        }

        member this.StartFileWatcher() =
            if this.path.IsSome then
                let watcher = createFileWatcher this.path.Value

                let sendMsgApi =
                    Remoting.init
                    |> Remoting.withWindow this.window
                    |> Remoting.buildClient<IArcFileWatcherApi>

                watcher.on (Chokidar.Events.All, this._ScheduleReloadArc sendMsgApi) |> ignore
            else
                swatefailfn this.window.id $"No path set for StartFileWatcher."

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
                    Remoting.init
                    |> Remoting.withWindow this.window
                    |> Remoting.buildClient<IMainUpdateRendererApi>

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
                    Remoting.init
                    |> Remoting.withWindow this.window
                    |> Remoting.buildClient<IMainUpdateRendererApi>

                let arc = ARC(identifier)
                this.path <- Some path
                this.arc <- Some arc
                do! arc.WriteAsync(path)
                do! this.Startup()
                sendMsg.pathChange (Some path)
        }

        member this.OnClose() =
            match this.path with
            | Some path ->
                let arcs =
                    recentARCs
                    |> Array.filter (fun arc -> arc.path <> path)
                setRecentARCs arcs
                printfn $"[Swate] Removed vault '{this.window.id}'"
                arcs
            | None -> [||]

type ArcVaults() =
    /// Key is window.id
    member val Vaults = Dictionary<int, ArcVault>() with get

    member this.Paths = this.Vaults.Values |> Seq.choose (fun x -> x.path) |> Array.ofSeq

    member this.BroadcastRecentARCs(recentARCs: ARCPointer[]) =
        this.Vaults.Values
        |> Array.ofSeq
        |> Array.iter (fun vault ->
            Remoting.init
            |> Remoting.withWindow vault.window
            |> Remoting.buildClient<Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi>
            |> fun client -> client.recentARCsUpdate recentARCs
        )

    member this.DisposeVault(id: int) =
        match this.Vaults.TryGetValue(id) with
        | false, _ -> swatefailfn id $"Failed to remove vault."
        | true, vault ->
            vault.watcher |> Option.iter (fun watcher -> watcher.close () |> Promise.start)
            let recentARCs = vault.OnClose()
            this.BroadcastRecentARCs recentARCs
            this.Vaults.Remove(id) |> ignore
            swatelogfn id $"Removed vault."

    member this.RegisterVault() : Fable.Core.JS.Promise<int> = promise {
        let! window = ArcVaultHelper.createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)

        window.onClosed (fun () -> this.DisposeVault(id))
        window.focus ()
        swatelogfn id $"Register window"

        return id
    }

    member this.RegisterVaultWithArc(path: string) = promise {
        let! window = ArcVaultHelper.createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)
        do! vault.OpenARC(path)

        window.onClosed (fun () -> this.DisposeVault(id))
        window.focus ()
        swatelogfn id $"Register window"

        return id
    }

    member this.RegisterVaultWithNewArc(path: string, newIdentifier: string) : Fable.Core.JS.Promise<int> = promise {
        let! window = ArcVaultHelper.createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)

        do! vault.CreateARC(path, newIdentifier)

        window.onClosed (fun () -> this.DisposeVault(id))
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


let ARC_VAULTS: ArcVaults = ArcVaults()