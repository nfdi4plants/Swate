[<AutoOpen>]
module Main.ArcVault

open System.Collections.Generic
open Fable.Electron
open Fable.Electron.Remoting.Main
open Main
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
    member val fileTree: Dictionary<string, FileEntry> = Dictionary<string, FileEntry>() with get, set
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
                                    let pathUpdater (path: string) =
                                        $"{this.path.Value}\{path}".Replace(ArcPathHelper.PathSeperatorWindows, ArcPathHelper.PathSeperator)
                                    match eventName.ToLower() with
                                    | name when name = Chokidar.Events.Add.ToString() ->
                                        if this.path.IsSome then
                                            let newPath = pathUpdater path
                                            let! addedFile = getFileEntry(newPath)
                                            let newFileTree = this.fileTree
                                            newFileTree.Add(addedFile.path, addedFile)
                                            this.SetFileTree(newFileTree)
                                    | name when name = Chokidar.Events.AddDir.ToString() ->
                                        if this.path.IsSome then
                                            let newPath = pathUpdater path
                                            let! addedFile = getFileEntry(newPath)
                                            let newFileTree = this.fileTree
                                            newFileTree.Add(addedFile.path, addedFile)
                                            this.SetFileTree(newFileTree)
                                    | name when name = Chokidar.Events.Unlink.ToString() ->
                                        let newPath = pathUpdater path
                                        if this.path.IsSome && this.fileTree.ContainsKey(newPath) then
                                            let newFileTree = this.fileTree
                                            newFileTree.Remove(newPath) |> ignore
                                            this.SetFileTree(newFileTree)
                                    | name when name = Chokidar.Events.UnlinkDir.ToString() ->
                                        let newPath = pathUpdater path
                                        if this.path.IsSome && this.fileTree.ContainsKey(newPath) then
                                            let newFileTree = this.fileTree
                                            let affectedPaths =
                                                this.fileTree.Keys
                                                |> Array.ofSeq
                                                |> Array.filter (fun path -> path.Contains(newPath))
                                            newFileTree.Remove(newPath) |> ignore
                                            affectedPaths
                                            |> Array.iter (fun path -> newFileTree.Remove(path) |> ignore)
                                            this.SetFileTree(newFileTree)
                                    | _ ->
                                        if this.path.IsSome then
                                            let fileTree = getFileEntries this.path.Value |> createFileEntryTree
                                            this.SetFileTree(fileTree)
                                }
                                |> Promise.start
                            )
                            500
                            
                    this.fileWatcherReloadArcTimeout <- Some timeoutId

        member this.SetFileTree(fileTree: Dictionary<string, FileEntry>) =
            this.fileTree <-  fileTree

            let sendMsg =
                Remoting.init
                |> Remoting.withWindow this.window
                |> Remoting.buildClient<IMainUpdateRendererApi>
            sendMsg.fileTreeUpdate fileTree

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
                this.isBusyWriting <- true

                try
                    do! arc.WriteAsync(path)
                finally
                    this.isBusyWriting <- false

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

        member this.OpenAssay(identifier: string) =
            let assay = this.arc.Value.TryGetAssay(identifier)

            match assay with
            | Some assay -> Some assay
            | None -> None

        member this.OpenStudy(identifier: string) =
            let study = this.arc.Value.TryGetStudy(identifier)

            match study with
            | Some study -> Some study
            | None -> None

        member this.OpenWorkflow(identifier: string) =
            let workflow = this.arc.Value.TryGetWorkflow(identifier)

            match workflow with
            | Some workflow -> Some workflow
            | None -> None

        member this.OpenRun(identifier: string) =
            let run = this.arc.Value.TryGetRun(identifier)

            match run with
            | Some run -> Some run
            | None -> None

        member this.GetContracts() =
            match this.arc with
            | Some arc -> arc.GetUpdateContracts()
            | None -> failwith "No arc available"

        member this.UpdateAsync() =
            match this.arc with
            | Some arc ->
                console.log($"this.path.Value: {this.path.Value}")

                let contracts = arc.GetUpdateContracts()

                console.log($"contracts: {contracts.Length}")

                contracts
                |> Array.iter (fun contract ->
                    console.log($"contract.Path: {contract.Path}"))

                arc.UpdateAsync(this.path.Value)
                |> Promise.start
                arc.WriteAsync(this.path.Value)
                |> Promise.start
            | None -> failwith "No arc available"
            

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
            this.Vaults.Remove(id) |> ignore
            swatelogfn id $"Removed vault."

    member this.OnCloseWindow(window: BrowserWindow, vault: ArcVault, id: int) =

        window.onClose (fun _ ->
            let recentARCs = vault.OnClose()
            this.BroadcastRecentARCs recentARCs
        )

        window.onClosed (fun () ->
            this.DisposeVault(id)
        )

    member this.RegisterVault() : Fable.Core.JS.Promise<int> = promise {
        let! window = ArcVaultHelper.createWindow ()
        let id = window.id
        let vault = ArcVault(window)
        this.Vaults.Add(id, vault)

        this.OnCloseWindow(window, vault, id)

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

        this.OnCloseWindow(window, vault, id)

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

    member this.SetFileTree(windowId: int, fileTree: Dictionary<string, FileEntry>) =
        let potVault = this.TryGetVault(windowId)
        match potVault with
        | Some (vault: ArcVault) -> vault.SetFileTree(fileTree)
        | None -> failwith $"Vault with window-id '{windowId}' not found."

    member this.TryGetVault(windowId: int) =
        match this.Vaults.TryGetValue windowId with
        | true, vault -> Some vault
        | false, _ -> None

    member this.TryGetVaultByPath(path: string) =
        this.Vaults.Values |> Seq.tryFind (fun v -> v.path = Some path)


let ARC_VAULTS: ArcVaults = ArcVaults()
