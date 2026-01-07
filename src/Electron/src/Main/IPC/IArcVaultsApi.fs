module Main.IPC.IArcVaultsApi

open Fable.Electron
open Swate.Electron.Shared.IPCTypes
open Fable.Electron.Main
open Main

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

                do! ARC_VAULTS.OpenARCInVault(windowId, arcPath)

                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs
                ARC_VAULTS.BroadcastRecentARCs(recentARCs)

                return Ok arcPath
        }
    createARC =
        fun (event: IpcMainEvent) (identifier: string) -> promise {

            let! r =
                dialog.showOpenDialog (
                    properties = [|
                        Enums.Dialog.ShowOpenDialog.Options.Properties.OpenDirectory
                    |]
                )

            Browser.Dom.console.log ("[Main] identifier:", identifier)

            if r.canceled then
                return Error(exn "Cancelled")
            elif r.filePaths.Length <> 1 then
                return Error(exn "Not exactly one path")
            else
                let arcPath = r.filePaths |> Array.exactlyOne
                let windowId = windowIdFromIpcEvent event

                do! ARC_VAULTS.CreateARCInVault(windowId, arcPath, identifier)

                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs
                ARC_VAULTS.BroadcastRecentARCs(recentARCs)

                return Ok arcPath
        }
    createARCInNewWindow =
        fun identifier -> promise {
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

                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs

                match ARC_VAULTS.TryGetVaultByPath arcPath with
                | None ->
                    let! _ = ARC_VAULTS.RegisterVaultWithNewArc(arcPath, identifier)
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                    return Ok()
                | Some vault ->
                    vault.window.focus ()
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                    return Ok()
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
                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs

                match ARC_VAULTS.TryGetVaultByPath arcPath with
                | None ->
                    let! _ = ARC_VAULTS.RegisterVaultWithArc(arcPath)
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                    return Ok()
                | Some vault ->
                    vault.window.focus ()
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                    return Ok()
        }
    closeARC =
        fun event -> promise {
            try
                let windowId = windowIdFromIpcEvent event
                let vault = ARC_VAULTS.TryGetVault(windowId)

                if vault.IsSome && vault.Value.path.IsSome then
                    let recentARCs = ARCHolder.updateRecentARCs vault.Value.path.Value maxNumberRecentARCs
                    ARC_VAULTS.BroadcastRecentARCs(recentARCs)

                ARC_VAULTS.DisposeVault(windowId)
                return Ok()
            with e ->
                return Error e
        }
    focusExistingARCWindow =
        fun arcPath -> promise {
            match ARC_VAULTS.TryGetVaultByPath arcPath with
            | None ->
                return Error(exn $"The ARC for path {arcPath} should exist")
            | Some vault ->
                let recentARCs = ARCHolder.updateRecentARCs arcPath maxNumberRecentARCs
                vault.window.focus()
                ARC_VAULTS.BroadcastRecentARCs(recentARCs)
                return Ok()
        }
    getOpenPath =
        fun event -> promise {
            return
                ARC_VAULTS.TryGetVault(windowIdFromIpcEvent event)
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