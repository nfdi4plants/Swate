module Swate.Components.Composite.Workspace.Context

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.Workspace.Types
open Swate.Components.JsBindings

type WorkspaceCtxValue = {
    layout: Pane
    setLayout: Pane -> unit
    panes: Map<string, WorkspacePaneState>
    setPanes: Map<string, WorkspacePaneState> -> unit
    contentMap: Map<string, ReactElement>
    activeTabId: string option
    setActiveTabId: string option -> unit
    handleDragEnd: DndKit.IDndKitEvent -> unit
    onDragStart: DndKit.IDndKitEvent -> unit
    debug: bool
    closeTab: string -> unit
    closeOthers: string -> unit
    closeAll: unit -> unit
    closeAllInPane: string -> unit
}

let WorkspaceCtx =
    React.createContext<WorkspaceCtxValue> (
        Unchecked.defaultof<WorkspaceCtxValue>
    )

[<Hook>]
let useWorkspaceCtx () = React.useContext WorkspaceCtx

type PaneCtxValue = {
    paneId: string
    tabs: WorkspaceTab array
    tabOrder: string array
    activateTab: string -> unit
    closeTab: string -> unit
}

let PaneCtx =
    React.createContext<PaneCtxValue> (
        Unchecked.defaultof<PaneCtxValue>
    )

[<Hook>]
let usePaneCtx () = React.useContext PaneCtx
