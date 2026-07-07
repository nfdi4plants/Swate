module Swate.Components.Composite.Workspace.Context

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.Workspace.Types
open Swate.Components.JsBindings

type WorkspaceDndContext = {
    onDragStart: DndKit.IDndKitEvent -> unit
    handleDragEnd: DndKit.IDndKitEvent -> unit
    isDragging: bool
}

let WorkspaceDndCtx =
    React.createContext<WorkspaceDndContext> (
        Unchecked.defaultof<WorkspaceDndContext>
    )

[<Hook>]
let useWorkspaceDndCtx () = React.useContext WorkspaceDndCtx

type WorkspaceContext = {
    layout: Pane
    setLayout: Pane -> unit
    panes: Map<string, WorkspacePaneState>
    setPanes: Map<string, WorkspacePaneState> -> unit
    contentMap: Map<string, ReactElement>
    activeTabId: string option
    setActiveTabId: string option -> unit
    debug: bool
    closeTab: string -> unit
    closeOthers: string -> unit
    closeAll: unit -> unit
    closeAllInPane: string -> unit
}

let WorkspaceCtx =
    React.createContext<WorkspaceContext> (
        Unchecked.defaultof<WorkspaceContext>
    )

[<Hook>]
let useWorkspaceCtx () = React.useContext WorkspaceCtx

type PaneContext = {
    paneId: string
    tabs: WorkspaceTab array
    tabOrder: string array
    activateTab: string -> unit
    closeTab: string -> unit
}

let PaneCtx =
    React.createContext<PaneContext> (
        Unchecked.defaultof<PaneContext>
    )

[<Hook>]
let usePaneCtx () = React.useContext PaneCtx
