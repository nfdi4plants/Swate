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
                console.log ($"Register window with path: {arcPath}")
                ARC_VAULTS.SetPath(windowId, arcPath)

                let recentARCs = ARCHolder.updateRecentARCs arcPath 5
                ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                return Ok arcPath
        }
    openARCInNewWindow =
        fun _ ->
            promise {
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
                    let recentARCs = ARCHolder.updateRecentARCs arcPath 5

                    match ARC_VAULTS.TryGetVaultByPath arcPath with
                    | None ->
                        let! _ = ARC_VAULTS.InitVault(arcPath)
                        ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                        return Ok()
                    | Some vault ->
                        vault.window.focus()
                        ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                        return Ok()
        }
    focusExistingARCWindow =
        fun arcPath -> promise {
            match ARC_VAULTS.TryGetVaultByPath arcPath with
            | None ->
                return Error(exn $"The ARC for path {arcPath} should exist")
            | Some vault ->
                let recentARCs = ARCHolder.updateRecentARCs arcPath 5
                vault.window.focus()
                ARC_VAULTS.BroadcastRecentARCs(recentARCs)
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
            return recentARCs
        }
    checkForARC =
        fun path -> promise {
            return ARC_VAULTS.TryGetVaultByPath(path).IsSome
        }
}