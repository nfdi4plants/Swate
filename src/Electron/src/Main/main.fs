module Main.Main

open Fable.Electron
open Fable.Electron.Remoting.Main
open Main

if SquirrelStartup.started then
    app.quit ()

app
    .whenReady()
    .``then`` (fun () ->
        // Restore persisted auth before any IPC handlers fire
        Main.Auth.AuthService.tryRestoreFromStorage ()

        // The provider catalog needs the settings root and the restored accounts.
        Main.VersionControl.VersionControlRuntime.createProduction ()
        |> Main.VersionControl.VersionControlRuntime.initialize

        ARC_VAULTS.RegisterVault() |> ignore

        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.IGitApi.api
        Remoting.createIpc () |> Remoting.fromValue IPC.IGitLabApi.api
        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.ArcVaultsApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.AuthApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.TemplateApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.ValidationPackageApi.api

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
