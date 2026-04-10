module Main.Main

open Fable.Electron
open Swate.Electron.Shared.IPCTypes
open Fable.Electron.Remoting.Main
open Main
open Fable.Core

if SquirrelStartup.started then
    app.quit ()

app
    .whenReady()
    .``then`` (fun () ->
        // Restore persisted auth before any IPC handlers fire
        Main.Auth.AuthService.tryRestoreFromStorage ()

        ARC_VAULTS.RegisterVault() |> ignore

        Remoting.init |> Remoting.buildHandler IPC.IGitApi.api
        Remoting.init |> Remoting.buildHandler IPC.IGitLabApi.api
        Remoting.init |> Remoting.buildHandler IPC.ArcVaultsApi.api
        Remoting.init |> Remoting.buildHandler Main.IPC.AuthApi.api

        app.onActivate (fun _ ->
            if BrowserWindow.getAllWindows().Length = 0 then
                ARC_VAULTS.RegisterVault() |> ignore
        )
    )
|> ignore

app.onWindowAllClosed (fun () ->
    Browser.Dom.console.log ("App quit")
    app.quit ()
)

app.onBeforeQuit (fun _ -> Browser.Dom.console.log ("Quitting"))