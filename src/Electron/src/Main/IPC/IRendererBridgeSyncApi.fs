module Main.IPC.RendererBridgeSyncApi

open System.Collections.Generic
open Fable.Core
open Fable.Electron.Remoting.Main
open Main
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.IPCTypes

let private sendPathChange (vault: ArcVault) =
    Remoting.init
    |> Remoting.withWindow vault.window
    |> Remoting.buildClient<IPathChangeApi>
    |> fun client -> client.pathChange vault.path

let private sendRecentArcs (vault: ArcVault) =
    let recentArcs = RECENT_ARCS.Get()

    Remoting.init
    |> Remoting.withWindow vault.window
    |> Remoting.buildClient<IRecentARCsUpdateApi>
    |> fun client -> client.recentARCsUpdate recentArcs

let private sendFileTree (vault: ArcVault) = promise {
    match vault.path with
    | Some _ -> do! vault.RefreshFileTree()
    | None ->
        Remoting.init
        |> Remoting.withWindow vault.window
        |> Remoting.buildClient<IFileTreeUpdateApi>
        |> fun client -> client.fileTreeUpdate (Dictionary<string, FileEntry>())
}

let api: IRendererBridgeSyncApi = {
    syncRendererBridgeState =
        fun event -> promise {
            let windowId = windowIdFromIpcEvent event

            match ARC_VAULTS.TryGetVault windowId with
            | None -> return Error(exn $"The ARC for window id {windowId} should exist")
            | Some vault ->
                sendPathChange vault
                sendRecentArcs vault
                do! sendFileTree vault
                return Ok()
        }
}
