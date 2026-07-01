module Swate.Components.Composite.Workspace.Types

open Feliz
open Fable.Core
open Browser.Types

type WorkspaceTab = {
    Id: string
    Label: string
    Icon: string option
}

[<StringEnum>]
type SplitDirection =
    | Horizontal
    | Vertical

[<RequireQualifiedAccess>]
type Pane =
    | Leaf of paneId: string
    | Split of direction: SplitDirection * first: Pane * second: Pane * ratio: float

type WorkspacePaneState = {
    tabs: WorkspaceTab array
    tabOrder: string array
    activeTabId: string option
} with

    static member Empty = {
        tabs = [||]
        tabOrder = [||]
        activeTabId = None
    }

type IWorkspaceHandle =
    abstract activateTab: string -> unit
    abstract closeTab: string -> unit
    abstract openTab: WorkspaceTab -> unit

type ContextMenuSpawnData = {
    tabId: string
    paneId: string
}

type WorkspaceProps = {
    tabs: WorkspaceTab list
    contentMap: Map<string, ReactElement>
    onTabsChange: WorkspaceTab list -> unit
    onActiveTabChange: string option -> unit
    activeTabId: string option
    ref: IRefValue<IWorkspaceHandle option>
    className: string option
    debug: bool option
}
