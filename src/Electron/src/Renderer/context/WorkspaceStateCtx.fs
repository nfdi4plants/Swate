module Renderer.context.WorkspaceStateCtx

open System.Collections.Generic
open Feliz
open Swate.Components
open Swate.Electron.Shared.IPCTypes

type WorkspaceStateContext = {
    RecentARCs: SelectorTypes.ARCPointer []
    SetRecentARCs: SelectorTypes.ARCPointer [] -> unit
    FileTree: Dictionary<string, FileEntry>
    SetFileTree: Dictionary<string, FileEntry> -> unit
    SelectedTreeItemPath: string option
    SetSelectedTreeItemPath: string option -> unit
} with

    static member init () = {
        RecentARCs = [||]
        SetRecentARCs = ignore
        FileTree = Dictionary<string, FileEntry>()
        SetFileTree = ignore
        SelectedTreeItemPath = None
        SetSelectedTreeItemPath = ignore
    }

let WorkspaceStateCtx =
    React.createContext<WorkspaceStateContext> (WorkspaceStateContext.init ())
