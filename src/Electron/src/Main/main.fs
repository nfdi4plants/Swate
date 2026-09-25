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
        // Restore persisted auth before any IPC handlers fire
        Main.Auth.AuthService.tryRestoreFromStorage ()

        registerRequiredWindow "The application window could not be loaded."

        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.IGitApi.api
        Remoting.createIpc () |> Remoting.fromValue IPC.IGitLabApi.api
        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.ArcVaultsApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.AuthApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.TemplateApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.ValidationPackageApi.api

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
