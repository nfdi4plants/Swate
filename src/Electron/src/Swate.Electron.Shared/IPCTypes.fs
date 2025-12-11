module Swate.Electron.Shared.IPCTypes

open Fable.Core.JS // Promise type

/// This IPC API is used to handle cases, in which no ARC is opened in the window yet
type IStartUpApi = {
    /// Will open ARC in same window
    openARC: unit -> Promise<Result<string, exn>>
}

type IARCIOApi = {
    /// Will open ARC in new window
    openARC: unit -> Promise<Result<unit, exn>>
}