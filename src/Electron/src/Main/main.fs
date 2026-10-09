module Main.Main

open Fable.Core
open Fable.Electron
open Fable.Electron.Remoting.Main
open Main

let private registerRequiredWindow (failureContext: string) =
    ARC_VAULTS.RegisterVault(
        onFailureBeforeCleanup =
            fun error ->
                eprintfn "%s: %s" failureContext error.Message

                dialog.showErrorBox ("Swate could not start", $"{failureContext}\n\n{error.Message}")
    )
    |> Promise.map ignore
    |> Promise.catch (fun _ -> app.quit ())
    |> Promise.start

if SquirrelStartup.started then
    app.quit ()

app
    .whenReady()
    .``then`` (fun () ->
        Main.Settings.AppVersion.initialize ()
        Main.AppMenu.install ()
        Main.Settings.AppVersion.checkForUpdate () |> Promise.start

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

        registerRequiredWindow "The application window could not be loaded."

        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.IVersionControlApi.api
        Remoting.createIpc () |> Remoting.fromValue IPC.IGitLabApi.api
        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.ArcVaultsApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.AuthApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.TemplateApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.ValidationPackageApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.UiSettingsApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.IAppVersionApi.api

        app.onActivate (fun _ ->
            if BrowserWindow.getAllWindows().Length = 0 then
                registerRequiredWindow "The application window could not be reopened."
        )
    )
|> ignore

app.onWindowAllClosed (fun () ->
    Browser.Dom.console.log ("App quit")
    app.quit ()
)

app.onBeforeQuit (fun _ -> Browser.Dom.console.log ("Quitting"))

app.onWillQuit (fun _ ->
    // The before-quit event runs before the windows' close handlers, which may still wait for running operations.
    match Main.VersionControl.WorkspaceSessionHost.tryCurrent () with
    | Some host ->
        host.CloseAll()
        |> Async.StartAsPromise
        |> Promise.catch (fun _ -> ())
        |> Promise.start
    | None -> ()
)
