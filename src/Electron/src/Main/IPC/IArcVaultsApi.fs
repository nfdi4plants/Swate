module Main.IPC.IArcVaultsApi

open Swate.Electron.Shared.IPCTypes
open Fable.Electron.Main
open Main

open Swate.Components

/// This depends on the types in this file, but the types on this file must call this to bind IPC calls :/
let api: IArcVaultsApi = {
    openARC =
        fun event -> promise {
            let! r =
                dialog.showOpenDialog (
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
                let windowId = windowIdFromIpcEvent event
                let selectedARC = ARCPointer.create(arcPath, arcPath, true)
                ARC_VAULTS.SetPath(windowId, arcPath)
                return Ok arcPath
        }
    openARCInNewWindow =
        fun _ -> promise {
            let! r =
                dialog.showOpenDialog (
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

                match ARC_VAULTS.TryGetVaultByPath arcPath with
                | None ->
                    let! _ = ARC_VAULTS.InitVault(arcPath)
                    return Ok()
                | Some vault ->
                    vault.window.focus()
                    return Ok()
        }
    getOpenPath =
        fun event -> promise {
            return
                ARC_VAULTS.TryGetVault (windowIdFromIpcEvent event)
                |> Option.bind (fun v -> v.path)
        }
    getRecentARCs =
        fun _ -> promise {
            let test =
                ARC_VAULTS.Vaults.Values
                |> Array.ofSeq
                |> Array.map (fun arc ->
                    if arc.path.IsSome then
                        Some (ARCPointer.create(arc.path.Value, arc.path.Value, false))
                    else
                        None
                )
                |> Array.choose (fun item -> item)
            return test
        }
}