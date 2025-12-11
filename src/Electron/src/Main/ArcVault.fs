[<AutoOpen>]
module rec Main.ArcVault

open Fable.Electron
open Fable.Electron.Remoting.Main
open Main
open System.Collections.Generic
open Swate.Electron.Shared.IPCTypes

module ArcVaultHelper =

    open Fable.Core.JsInterop
    open Node.Api

    let Debug = isNullOrUndefined MAIN_WINDOW_VITE_DEV_SERVER_URL

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

/// This depends on the types in this file, but the types on this file must call this to bind IPC calls :/
let arcIOApi (window: BrowserWindow) : IARCIOApi = {
    openARC =
        fun () -> promise {
            let! r =
                dialog.showOpenDialog (
                    unbox window,
                    properties = [|
                        Enums.Dialog.ShowOpenDialog.Options.Properties.OpenDirectory
                    |]
                )

            if r.canceled then
                return Error(exn "Cancelled")
            elif r.filePaths.Length <> 1 then
                return Error(exn "Not exactly one path")
            else
                let arcPath = r.filePaths |> Array.exactlyOne

                do! ARC_VAULTS.InitVault(arcPath)
                return Ok()
        }
}

/// <summary>
///
/// </summary>
/// <param name="path">Can be None if not opened ARC.</param>
type ArcVault private (path: string, window: BrowserWindow, debug: bool) =


    member val path: string = path with get
    member val window: BrowserWindow = window with get

    static member Init(path: string, ?window: BrowserWindow) = promise {
        printfn $"[Swate] Open Arc in {path}"

        let mutable window = window

        if window.IsNone then
            let! newWindow = ArcVaultHelper.createWindow ()
            window <- Some newWindow

        let fileName = path.Replace("\\", "/").Split('/') |> Array.last
        window.Value.setTitle ($"{fileName}")

        Remoting.init |> Remoting.buildHandler (arcIOApi window.Value)

        return ArcVault(path, window.Value, ArcVaultHelper.Debug)
    }

type ArcVaults() =
    member val Vaults = Dictionary<string, ArcVault>() with get

    member this.InitVault(path: string) : Fable.Core.JS.Promise<unit> = promise {
        // check if vault already open
        if this.Vaults.ContainsKey(path) then
            this.Vaults.[path].window.focus ()
        else
            let! newVault = ArcVault.Init(path)

            newVault.window.onClosed (fun () ->
                if this.Vaults.Remove(path) then
                    printfn $"Removed %i{newVault.window.id} from window array"
                else
                    failwith $"Failed to remove %i{newVault.window.id} from window array"
            )

            this.Vaults.Add(path, newVault)
            newVault.window.focus ()

        return ()
    }

    member this.InitVault(path: string, existingWindow: BrowserWindow) : Fable.Core.JS.Promise<unit> = promise {
        // check if vault already open
        if this.Vaults.ContainsKey(path) then
            this.Vaults.[path].window.focus ()
        else
            let! newVault = ArcVault.Init(path, existingWindow)

            newVault.window.onClosed (fun () ->
                if this.Vaults.Remove(path) then
                    printfn $"Removed %i{newVault.window.id} from window array"
                else
                    failwith $"Failed to remove %i{newVault.window.id} from window array"
            )

            this.Vaults.Add(path, newVault)
            newVault.window.focus ()
    }


let ARC_VAULTS: ArcVaults = ArcVaults()