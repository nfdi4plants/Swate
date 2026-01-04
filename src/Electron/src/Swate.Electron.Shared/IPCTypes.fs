module Swate.Electron.Shared.IPCTypes

open Fable.Core.JS // Promise type
open Fable.Electron

type IArcVaultsApi = {
    /// Will open ARC in same window
    openARC: IpcMainEvent -> Promise<Result<string, exn>>
    /// Will open ARC in a new window
    openARCInNewWindow: unit -> Promise<Result<unit, exn>>
    getOpenPath: IpcMainEvent -> Promise<string option>
    getRecentARCs: unit -> Promise<Swate.Components.Types.SelectorTypes.ARCPointer []>
}

type IMainUpdateRendererApi = {
    pathChange: string option -> unit
}
