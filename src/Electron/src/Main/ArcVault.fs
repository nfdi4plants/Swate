[<AutoOpen>]
module rec Main.ArcVault

open Browser
open Fable.Electron
open Fable.Electron.Remoting.Main
open Main
open System.Collections.Generic
open Main.Bindings
open Swate.Electron.Shared.IPCTypes
open ARCtrl

module ArcVaultHelper =

    open Fable.Core.JsInterop
    open Node.Api

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
        let watcher =
            Chokidar.Chokidar.watch (path, Chokidar.WatchOptions(cwd = path, awaitWriteFinish = true))

        watcher

/// <summary>
/// Represents a vault window in the application, optionally associated with a file path.
/// </summary>
/// <param name="path">Can be None if not opened ARC.</param>
type ArcVault(window: BrowserWindow, ?path: string, ?arc, ?watcher) =

    member val path: string option = path with get, private set
    member val window: BrowserWindow = window with get
    member val arc: ARC option = arc with get, set
    member val watcher: Chokidar.IWatcher option = watcher with get, set

    member this.LoadArc() = promise {
        if this.path.IsSome then
            match! ARC.tryLoadAsync (this.path.Value) with
            | Error e -> failwith $"[Swate-{this.window.id}] Unable to load ARC: {e}"
            | Ok arc -> this.arc <- Some arc
        else
            failwith $"[Swate-{this.window.id}] No path set for StartFileWatcher."
    }

    member this.StartFileWatcher() =
        if this.path.IsSome then
            let watcher = ArcVaultHelper.createFileWatcher this.path.Value

            let sendMsg =
                Remoting.init
                |> Remoting.withWindow this.window
                |> Remoting.buildClient<IArcFileWatcherApi>

            watcher.on (
                Chokidar.Events.Raw,
                fun e ->
                    console.log ($"[Swate-{window.id}] Chokidar: ", e)

                    promise {
                        sendMsg.IsLoadingChanges true
                        do! this.LoadArc()
                        sendMsg.IsLoadingChanges false
                    }
                    |> Promise.start
            )
            |> ignore
        else
            failwith $"[Swate-{this.window.id}] No path set for StartFileWatcher."

    /// This functions should be called once, when an vault is first started with a path
    member this.Startup() = promise {
        printfn "StartFileWatcher"
        this.StartFileWatcher()
        printfn "LoadArc"
        do! this.LoadArc()
        printfn "Set Window Title"
        this.window.title <- this.arc.Value.Identifier
    }

    member this.OpenARC(path: string) = promise {
        match this.path with
        | Some _ -> failwith "Unable to open ARC in vault bound to ARC."
        | None ->
            let sendMsg =
                Remoting.init
                |> Remoting.withWindow this.window
                |> Remoting.buildClient<IMainUpdateRendererApi>

            console.log ($"[Swate-{this.window.id}] path: {path}")
            this.path <- Some path
            do! this.Startup()
            sendMsg.pathChange (Some path)
    }

    member this.CreateARC(path: string, identifier: string) = promise {
        match this.path, this.arc with
        | Some _, _ -> failwith "Unable to create ARC in vault bound to path."
        | _, Some _ -> failwith "Unable to create ARC in vault bound to ARC."
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

type ArcVaults() =
    /// Key is window.id
    member val Vaults = Dictionary<int, ArcVault>() with get

    member this.Paths = this.Vaults.Values |> Seq.choose (fun x -> x.path) |> Array.ofSeq

    member this.DisposeVault(id: int) =
        match this.Vaults.TryGetValue(id) with
        | false, _ -> failwith $"[Swate-{id}] Failed to remove vault."
        | true, vault ->
            vault.watcher |> Option.iter (fun watcher -> watcher.close () |> Promise.start)
            this.Vaults.Remove(id) |> ignore
            printfn $"[Swate-{id}] Removed vault."

    member this.InitVault(?path: string, ?newIdentifier: string) : Fable.Core.JS.Promise<int> = promise {
        let! window = ArcVaultHelper.createWindow ()
        let id = window.id
        console.log ($"Register window with id: {id}")
        let vault = ArcVault(window, ?path = path)
        this.Vaults.Add(id, vault)

        match path, newIdentifier with
        | Some p, None -> do! vault.OpenARC(p)
        | Some p, Some i -> do! vault.CreateARC(p, i)
        | _, _ -> ()

        window.onClosed (fun () -> this.DisposeVault(id))

        window.focus ()

        return id
    }

    member this.OpenARC(windowId: int, path: string) = promise {
        match this.Vaults.TryGetValue windowId with
        | false, _ -> failwith $"Vault with window-id '{windowId}' not found."
        | true, vault -> do! vault.OpenARC path

        return ()
    }

    member this.CreateARC(windowId: int, path: string, identifier: string) = promise {
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