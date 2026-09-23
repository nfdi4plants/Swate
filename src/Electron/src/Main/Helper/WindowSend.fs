module Main.WindowSend

open Fable.Electron.Main
open Fable.Electron.Remoting.Main

/// Whether the window and its web contents can still receive messages. Sending to either
/// after it is destroyed throws "Object has been destroyed" in the main process.
let isAlive (window: BrowserWindow) =
    not (window.isDestroyed ()) && not (window.webContents.isDestroyed ())

/// Sends through a renderer proxy of 'Api, or drops the message when the window is gone.
let inline send<'Api> (window: BrowserWindow) (deliver: 'Api -> unit) =
    if isAlive window then
        Remoting.createIpc ()
        |> Remoting.withWindow window
        |> Remoting.buildProxySender<'Api>
        |> deliver
