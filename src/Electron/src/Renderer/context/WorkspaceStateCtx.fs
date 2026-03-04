module Renderer.context.WorkspaceStateCtx

open System.Collections.Generic
open Feliz
open Swate.Components
open Swate.Electron.Shared.IPCTypes

type WorkspaceState = {
    RecentARCs: SelectorTypes.ARCPointer []
    FileTree: Dictionary<string, FileEntry>
    SelectedTreeItemPath: string option
} with

    static member init () = {
        RecentARCs = [||]
        FileTree = Dictionary<string, FileEntry>()
        SelectedTreeItemPath = None
    }

let WorkspaceStateCtx =
    React.createContext<StateContext<WorkspaceState>> (StateContext.init (WorkspaceState.init ()))
