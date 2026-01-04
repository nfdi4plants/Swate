[<AutoOpen>]
module rec Main.ArcVault

open Browser
open System.Collections.Generic

open Fable.Electron
open Fable.Electron.Remoting.Main

open Main

open Swate.Components
open Swate.Electron.Shared.IPCTypes

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

/// <summary>
/// Represents a vault window in the application, optionally associated with a file path.
/// </summary>
/// <param name="path">Can be None if not opened ARC.</param>
type ArcVault(window: BrowserWindow, ?path: string, ?recentARCs: ARCPointer[]) =

    member val path: string option = path with get, private set
    member val window: BrowserWindow = window with get

    member this.SetPath(path: string) =
        match this.path with
        | Some _ -> failwith "Setting path for vaults with existing path is currently not supported."
        | None ->
            let sendMsg =
                Remoting.init
                |> Remoting.withWindow this.window
                |> Remoting.buildClient<Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi>

            sendMsg.pathChange (Some path)
            this.path <- Some path

type ArcVaults() =
    /// Key is window.id
    member val Vaults = Dictionary<int, ArcVault>() with get

    member this.Paths = this.Vaults.Values |> Seq.choose (fun x -> x.path) |> Array.ofSeq

    member this.InitVault(?path: string) : Fable.Core.JS.Promise<int> = promise {
        let! window = ArcVaultHelper.createWindow ()
        let id = window.id
        console.log ($"Register window with id: {id}")
        let vault = ArcVault(window, ?path = path)
        this.Vaults.Add(id, vault)

        window.onClosed (fun () ->
            if this.Vaults.Remove(id) then
                printfn $"[Swate] Removed vault '{window.id}'"
            else
                failwith $"Failed to remove vault '{window.id}'"
        )

        window.focus ()

        return id
    }

    member this.SetPath(windowId: int, path: string) =
        match this.Vaults.TryGetValue windowId with
        | false, _ -> failwith $"Vault with window-id '{windowId}' not found."
        | true, vault -> vault.SetPath path

    member this.TryGetVault(windowId: int) =
        match this.Vaults.TryGetValue windowId with
        | true, vault -> Some vault
        | false, _ -> None

    member this.TryGetVaultByPath(path: string) =
        this.Vaults.Values |> Seq.tryFind (fun v -> v.path = Some path)

let ARC_VAULTS: ArcVaults = ArcVaults()