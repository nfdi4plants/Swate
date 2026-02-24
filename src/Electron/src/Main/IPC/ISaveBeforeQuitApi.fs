module Main.IPC.ISaveBeforeQuitApi

open Fable.Core
open Fable.Electron
open Swate.Electron.Shared.IPCTypes
open Main

let api: ISaveBeforeQuitApi = {
    resolveCloseRequest =
        fun (event: IpcMainEvent) (decision: SaveBeforeQuitDecision) -> promise {
            try
                let windowId = windowIdFromIpcEvent event
                ARC_VAULTS.ResolveCloseRequest(windowId, decision)
                return Ok()
            with e ->
                return Error e
        }
}