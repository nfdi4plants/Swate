module Main

open Fable.Electron
open Swate.Electron.Shared.IPCTypes
open Fable.Electron.Remoting.Main
open Main

if SquirrelStartup.started then
    app.quit ()

let startUpAPI (window: BrowserWindow) : IStartUpApi = {
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

                do! ARC_VAULTS.InitVault(arcPath, window)
                return Ok arcPath
        }
}

let createStartUpWindow () = promise {
    let! window = ArcVaultHelper.createWindow ()
    Remoting.init |> Remoting.buildHandler (startUpAPI window)
    return ()
}

app
    .whenReady()
    .``then`` (fun () ->

        createStartUpWindow () |> ignore

        app.onActivate (fun _ ->
            if BrowserWindow.getAllWindows().Length = 0 then
                createStartUpWindow () |> ignore
        )
    )
|> ignore

app.onWindowAllClosed (fun () -> app.quit ())
app.onBeforeQuit (fun _ -> Browser.Dom.console.log ("Quitting"))