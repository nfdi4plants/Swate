module Main.Main

open Fable.Electron
open Fable.Electron.Remoting.Main
open Main

if SquirrelStartup.started then
    app.quit ()

// Keep the BrowserWindow associated with Swate's installed Windows taskbar entry.
// Without an explicit AppUserModelID, native taskbar progress can be attached to
// Electron's development identity instead of the visible Swate icon.
app.setAppUserModelId "com.squirrel.swate.swate"

app
    .whenReady()
    .``then`` (fun () ->
        // Restore persisted auth before any IPC handlers fire
        Main.Auth.AuthService.tryRestoreFromStorage ()

        ARC_VAULTS.RegisterVault() |> ignore

        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.IGitApi.api
        Remoting.createIpc () |> Remoting.fromValue IPC.IGitLabApi.api
        Remoting.createIpc () |> Remoting.fromIpcMainEvent IPC.ArcVaultsApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.AuthApi.api
        Remoting.createIpc () |> Remoting.fromValue Main.IPC.TemplateApi.api

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
