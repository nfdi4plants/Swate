module Swate.Electron.Shared.IPCTypes

open Fable.Core.JS // Promise type
open Fable.Electron

open Swate.Components

/// Two Way Bridge: Renderer <-> Main
type IArcVaultsApi = {
    /// Will open ARC in same window
    openARC: IpcMainEvent -> Promise<Result<string, exn>>
    createARC: IpcMainEvent -> string -> Promise<Result<string, exn>>
    focusExistingARCWindow: string -> Promise<Result<unit, exn>>
    /// Will open ARC in a new window
    openARCInNewWindow: unit -> Promise<Result<unit, exn>>
    createARCInNewWindow: string -> Promise<Result<unit, exn>>
    closeARC: IpcMainEvent -> Promise<Result<unit, exn>>
    getOpenPath: IpcMainEvent -> Promise<string option>
    getRecentARCs: unit -> Promise<Swate.Components.Types.SelectorTypes.ARCPointer []>
    checkForARC: string -> Promise<bool>
}

type FileEntry =
    {name: string; path: string; isDirectory: bool; children: FileEntry []}

[<AutoOpen>]
module FileEntryExtensions =

    type FileEntry with
        member this.updateChildren (children: FileEntry []) =
            {this with children = children}

        static member create (name: string, path: string, isDirectory: bool, ?children) =
            {
                name = name
                path = path
                isDirectory = isDirectory
                children = defaultArg children [||]
            }

/// One Way Bridge: Main -> Renderer
type IMainUpdateRendererApi = {
    pathChange: string option -> unit
    recentARCsUpdate: ARCPointer [] -> unit
    fileTreeUpdate: FileEntry option -> unit
}

// Todo: What should filewatcher do when detecting changes?
/// One Way Bridge: Main -> Renderer
type IArcFileWatcherApi = {
    /// This function is called when ARC is reloaded due to local file changes.
    IsLoadingChanges: bool -> unit
}