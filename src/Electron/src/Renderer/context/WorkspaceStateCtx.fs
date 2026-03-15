module Renderer.Context.WorkspaceStateCtx

open Feliz
open ARCtrl
open Swate.Components
open Swate.Electron.Shared.FileIOTypes

type WorkspaceState = {
    RecentARCs: SelectorTypes.ARCPointer[]
    FileTree: FileEntry list
    SelectedTreeItemPath: string option
    TemplateImportType: TableJoinOptions
    RequestedWidgetToOpen: WidgetType option
} with

    static member init() = {
        RecentARCs = [||]
        FileTree = []
        SelectedTreeItemPath = None
        TemplateImportType = TableJoinOptions.Headers
        RequestedWidgetToOpen = None
    }

let WorkspaceStateCtx =
    React.createContext<StateContext<WorkspaceState>> (StateContext.init (WorkspaceState.init ()))
