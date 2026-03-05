module Renderer.context.WorkspaceStateCtx

open Feliz
open Swate.Components
open Swate.Electron.Shared.IPCTypes

type WorkspaceState = {
    RecentARCs: SelectorTypes.ARCPointer []
    FileTree: FileEntry list
    SelectedTreeItemPath: string option
} with

    static member init () = {
        RecentARCs = [||]
        FileTree = []
        SelectedTreeItemPath = None
    }

let WorkspaceStateCtx =
    React.createContext<StateContext<WorkspaceState>> (StateContext.init (WorkspaceState.init ()))
