module Main

open Fable.Electron
open Fable.Core.JsInterop
open Node.Api
open Fable.Electron.Main

if SquirrelStartup.started then
    app.quit ()


type ArcVault(path: string) =
    member val path = path with get



let windows = ResizeArray<BrowserWindow>()

let createWindow () =
    let screenSize = screen.getPrimaryDisplay().workAreaSize

    let mainWindowOptions =
        BrowserWindowConstructorOptions(
            width = int screenSize.width,
            height = int screenSize.height,
            webPreferences = WebPreferences(preload = path.join (__dirname, "preload.fs.js"))
        )

    let mainWindow = BrowserWindow(mainWindowOptions)

    if isNullOrUndefined MAIN_WINDOW_VITE_DEV_SERVER_URL then
        mainWindow.loadFile (path.join (__dirname, $"../renderer/{MAIN_WINDOW_VITE_NAME}/index.html"))
    else
        mainWindow.loadURL MAIN_WINDOW_VITE_DEV_SERVER_URL
    |> ignore

    mainWindow.webContents.openDevTools Enums.WebContents.OpenDevTools.Options.Mode.Right

    mainWindow.onClosed (fun () ->
        if windows.Remove(mainWindow) then
            printfn $"Removed %i{mainWindow.id} from window array"
        else
            failwith $"Failed to remove %i{mainWindow.id} from window array"
    )

    windows.Add mainWindow


app
    .whenReady()
    .``then`` (fun () ->
        createWindow ()

        app.onActivate (fun _ ->
            if BrowserWindow.getAllWindows().Length = 0 then
                createWindow ()
        )
    )
|> ignore

app.onWindowAllClosed (fun () -> app.quit ())
app.onBeforeQuit (fun _ -> Browser.Dom.console.log ("Quitting"))