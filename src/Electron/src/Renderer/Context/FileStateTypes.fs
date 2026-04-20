module Renderer.Context.FileStateTypes

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes

type FileState = {
    FileTree: FileEntry[]
    Selection: ArcSelection
}
with
    static member init() : FileState = {
        FileTree = [||]
        Selection = ArcSelection.empty
    }

type FileStateController = {
    state: FileState
    setSelection: ArcSelection -> unit
    updateSelection: (ArcSelection -> ArcSelection) -> unit
}
