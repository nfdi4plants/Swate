module Main.Main

open Fable.Electron
open Fable.Electron.Remoting.Main

open Swate.Components

open Main

module States =
    let mutable recentARCs: ARCPointer[] = [||]

    let updateRecentARCs maxCount path =
        let updated =
            recentARCs
            |> Array.filter (fun arc -> arc.path <> path)
            |> fun arr -> Array.append arr [| ARCPointer.create(path, path, false) |]
            |> fun arr ->
                if arr.Length > maxCount then
                    arr.[arr.Length - maxCount ..]
                else arr

        recentARCs <- updated
        updated

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