module Main.Main

open Fable.Electron
open Fable.Electron.Remoting.Main

open Main

if SquirrelStartup.started then
    app.quit ()

app
    .whenReady()
    .``then`` (fun () ->
        ARC_VAULTS.InitVault() |> ignore

        Remoting.init |> Remoting.buildHandler IPC.IArcVaultsApi.api

        app.onActivate (fun _ ->
            if BrowserWindow.getAllWindows().Length = 0 then
                ARC_VAULTS.InitVault() |> ignore
        )
    )
|> ignore

app.onWindowAllClosed (fun () ->
    Browser.Dom.console.log ("App quit")
    app.quit ()
)

app.onBeforeQuit (fun _ -> Browser.Dom.console.log ("Quitting"))