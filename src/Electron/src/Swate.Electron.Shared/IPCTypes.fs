module Swate.Electron.Shared.IPCTypes

open Fable.Core.JS // Promise type
open Fable.Electron

type IArcVaultsApi = {
    /// Will open ARC in same window
    openARC: IpcMainEvent -> Promise<Result<string, exn>>
    createARC: IpcMainEvent -> string -> Promise<Result<string, exn>>
    /// Will open ARC in a new window
    openARCInNewWindow: unit -> Promise<Result<unit, exn>>
    createARCInNewWindow: string -> Promise<Result<unit, exn>>
    closeARC: IpcMainEvent -> Promise<Result<unit, exn>>
    getOpenPath: IpcMainEvent -> Promise<string option>
}

type IMainUpdateRendererApi = { pathChange: string option -> unit }

// Todo: What should filewatcher do when detecting changes?
type IArcFileWatcherApi = {
    /// This function is called when ARC is reloaded due to local file changes.
    IsLoadingChanges: bool -> unit
}