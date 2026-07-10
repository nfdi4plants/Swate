module Swate.Components.Composite.Workspace.Context

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.Workspace.Types
open Swate.Components.JsBindings

// -- DnD context (unchanged from current) --

type WorkspaceDndContext = {
    onDragStart: DndKit.IDndKitEvent -> unit
    handleDragEnd: DndKit.IDndKitEvent -> unit
    isDragging: bool
}

let WorkspaceDndCtx =
    React.createContext<WorkspaceDndContext>(Unchecked.defaultof<WorkspaceDndContext>)

[<Hook>]
let useWorkspaceDndCtx () = React.useContext WorkspaceDndCtx

// -- Dispatch context (stable reference, never changes) --

type WorkspaceDispatchContext = {
    dispatch: obj -> unit
}

let WorkspaceDispatchCtx =
    React.createContext<WorkspaceDispatchContext>(Unchecked.defaultof<WorkspaceDispatchContext>)

[<Hook>]
let useWorkspaceDispatchCtx () = React.useContext WorkspaceDispatchCtx

// -- Layout context (changes on split/close pane) --

type WorkspaceLayoutContext = {
    layout: Layout
}

let WorkspaceLayoutCtx =
    React.createContext<WorkspaceLayoutContext>(Unchecked.defaultof<WorkspaceLayoutContext>)

[<Hook>]
let useWorkspaceLayoutCtx () = React.useContext WorkspaceLayoutCtx

// -- Pane state context (changes on tab operations) --

type WorkspacePaneStateContext = {
    panesMap: Map<PaneId, Pane<obj>>
    focusedPane: PaneId
    renderTabContent: obj -> ReactElement
    renderTab: obj -> ReactElement
    debug: bool
}

let WorkspacePaneStateCtx =
    React.createContext<WorkspacePaneStateContext>(Unchecked.defaultof<WorkspacePaneStateContext>)

[<Hook>]
let useWorkspacePaneStateCtx () = React.useContext WorkspacePaneStateCtx

// -- Per-pane context (created fresh per LeafNode) --

type PaneContext = {
    paneId: PaneId
    tabs: Tab<obj> array
    focusedTab: TabId option
    isFocusedPane: bool
}

let PaneCtx =
    React.createContext<PaneContext>(Unchecked.defaultof<PaneContext>)

[<Hook>]
let usePaneCtx () = React.useContext PaneCtx

// -- Sortable active context (controls reorder preview visibility) --

type SortableActiveContext = {
    isActiveRef: IRefValue<bool>
}

let SortableActiveCtx =
    React.createContext<SortableActiveContext>(Unchecked.defaultof<SortableActiveContext>)

[<Hook>]
let useSortableActiveCtx () = React.useContext SortableActiveCtx
