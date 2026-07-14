[<AutoOpen>]
module Main.IPC.IPCHelper

open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Main

let windowFromIpcEvent (event: IpcMainInvokeEvent) =
    BrowserWindow.fromWebContents (event.sender)

let dialogParentFromIpcEvent (event: IpcMainInvokeEvent) : BaseWindow option =
    windowFromIpcEvent event |> Option.map (fun window -> unbox<BaseWindow> window)

let windowIdFromIpcEvent (event: IpcMainInvokeEvent) =
    BrowserWindow.fromWebContents (event.sender)
    |> Option.map _.id
    |> function
        | Some id -> id
        | None -> failwith $"Unable to access window from web-contents-id: '{event.sender.id}'"

let tryGetVaultAndArcPath (event: IpcMainInvokeEvent) =
    let windowId = windowIdFromIpcEvent event

    match ARC_VAULTS.TryGetVault(windowId) with
    | None -> Error(exn $"The ARC for window id {windowId} should exist")
    | Some vault ->
        match vault.path with
        | Some arcPath -> Ok(vault, arcPath)
        | None -> Error(exn "ARC is not loaded.")

let withBusyWriting (vault: ArcVault) (operation: unit -> JS.Promise<Result<'T, exn>>) : JS.Promise<Result<'T, exn>> = promise {
    let wasBusyWriting = vault.isBusyWriting
    vault.isBusyWriting <- true

    try
        return! operation ()
    finally
        vault.isBusyWriting <- wasBusyWriting
}
