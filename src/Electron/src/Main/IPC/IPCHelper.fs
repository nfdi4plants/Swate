namespace Main.IPC

open Fable.Electron
open Fable.Electron.Main

[<AutoOpen>]
module IPCHelper =
    let windowFromIpcEvent(event: IpcMainEvent) =
        BrowserWindow.fromWebContents(event.sender)

    let windowIdFromIpcEvent(event: IpcMainEvent) =
        BrowserWindow.fromWebContents(event.sender)
        |> Option.map _.id
        |> function | Some id -> id | None -> failwith $"Unable to access window from web-contents-id: '{event.sender.id}'"

