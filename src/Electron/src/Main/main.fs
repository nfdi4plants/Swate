module Main.Main

open Fable.Core
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

        // The provider catalog needs the settings root, which exists only once the app is
        // ready. A failure here must not take the IPC registrations below with it.
        try
            let runtime = Main.VersionControl.VersionControlRuntime.createProduction ()
            let host = Main.VersionControl.WorkspaceSessionHost.WorkspaceSessionHost(runtime)
            Main.VersionControl.WorkspaceSessionHost.initialize host
        with error ->
            Browser.Dom.console.error ("Version control host initialization failed", error.Message)

        ARC_VAULTS.RegisterVault() |> ignore

        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.IVersionControlApi.api
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

app.onBeforeQuit (fun _ ->
    Browser.Dom.console.log ("Quitting")

    match Main.VersionControl.WorkspaceSessionHost.tryCurrent () with
    | Some host ->
        host.CloseAll()
        |> Async.StartAsPromise
        |> Promise.catch (fun _ -> ())
        |> Promise.start
    | None -> ()
)
