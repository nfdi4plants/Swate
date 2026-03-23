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
} with

    static member init() = {
        RecentARCs = [||]
        FileTree = []
        SelectedTreeItemPath = None
        TemplateImportType = TableJoinOptions.Headers
    }

let WorkspaceStateCtx =
    React.createContext<StateContext<WorkspaceState>> (StateContext.init (WorkspaceState.init ()))


[<Hook>]
let useWorkspaceStateCtx () = React.useContext WorkspaceStateCtx