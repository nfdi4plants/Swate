namespace Main.IPC

open Fable.Core
open Fable.Electron
open Fable.Electron.Main
open Main
open Main.ArcVaultHelper

[<AutoOpen>]
module IPCHelper =
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
            | None -> Error(arcNotOpenError ())

    /// Nested busy scopes per vault window. The vault flag is a plain boolean, so the
    /// depth is counted here and the flag only drops when the outermost scope ends,
    /// whichever API opened it.
    let private busyDepth = System.Collections.Generic.Dictionary<int, int>()

    /// Marks the vault busy for the duration of the operation. The bookkeeping runs
    /// before the promise builder starts, so the flag is set before the caller gets
    /// the promise back.
    let withBusyWritingScope (vault: ArcVault) (operation: unit -> JS.Promise<'T>) : JS.Promise<'T> =
        let key = vault.window.id

        let depth =
            match busyDepth.TryGetValue key with
            | true, current -> current
            | _ -> 0

        busyDepth[key] <- depth + 1

        if depth = 0 then
            vault.isBusyWriting <- true

        let release () =
            let remaining =
                match busyDepth.TryGetValue key with
                | true, current -> current - 1
                | _ -> 0

            if remaining <= 0 then
                busyDepth.Remove key |> ignore
                vault.isBusyWriting <- false
            else
                busyDepth[key] <- remaining

        promise {
            try
                return! operation ()
            finally
                release ()
        }

    let withBusyWriting
        (vault: ArcVault)
        (operation: unit -> JS.Promise<Result<'T, exn>>)
        : JS.Promise<Result<'T, exn>> =
        withBusyWritingScope vault operation

    let withExclusiveBusyWriting
        (vault: ArcVault)
        (operation: unit -> JS.Promise<Result<'T, exn>>)
        : JS.Promise<Result<'T, exn>> =
        if vault.isBusyWriting then
            JS.Constructors.Promise.resolve (
                Error(exn "Swate is still saving another change. Please wait a moment and try again.")
            )
        else
            withBusyWriting vault operation
