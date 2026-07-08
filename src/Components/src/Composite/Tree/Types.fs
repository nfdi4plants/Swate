module Swate.Components.Composite.Tree.Types

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components.Primitive.ContextMenu.Types

[<StringEnum(CaseRules.LowerFirst)>]
type TreeNodeKind =
    | Branch
    | Leaf

[<StringEnum(CaseRules.LowerFirst)>]
type TreeSelectionMode =
    | Single
    | Multiple

[<StringEnum(CaseRules.LowerFirst)>]
type TreeLazyLoadStatus =
    | Idle
    | Loading
    | Loaded
    | Error

[<AllowNullLiteral; JS.Pojo>]
type TreeItem<'T>
    (
        id: string,
        label: string,
        kind: TreeNodeKind,
        ?data: 'T,
        ?children: TreeItem<'T>[],
        ?childrenCount: int,
        ?icon: ReactElement,
        ?tooltip: string,
        ?leading: ReactElement,
        ?trailing: ReactElement,
        ?className: string
    ) =
    member val id = id with get, set
    member val label = label with get, set
    member val kind = kind with get, set
    member val data: 'T option = data with get, set
    member val children: TreeItem<'T>[] option = children with get, set
    member val childrenCount: int option = childrenCount with get, set
    member val icon: ReactElement option = icon with get, set
    member val tooltip: string option = tooltip with get, set
    member val leading: ReactElement option = leading with get, set
    member val trailing: ReactElement option = trailing with get, set
    member val className: string option = className with get, set

type TreeRenderProps<'T> = {
    Node: TreeItem<'T>
    Depth: int
    IsExpanded: bool
    IsSelected: bool
    IsFocused: bool
    IsLoading: bool
    Error: string option
    Toggle: unit -> unit
    Select: MouseEvent -> unit
}

type TreeVisibleNode<'T> = {
    Node: TreeItem<'T>
    Depth: int
    ParentId: string option
}

type TreeLoadState<'T> = {
    Status: TreeLazyLoadStatus
    Children: TreeItem<'T>[] option
    Error: string option
}

type TreeRowLookup<'T> = {
    Nodes: Map<string, TreeItem<'T>>
    Parents: Map<string, string>
    VisibleNodes: TreeVisibleNode<'T>[]
}

type TreeDataSource<'T> = {
    GetChildrenCount: TreeItem<'T> option -> int
    GetTreeItems: TreeItem<'T> option -> JS.Promise<TreeItem<'T>[]>
}

type TreeApi = {
    InvalidateNode: string -> unit
    InvalidateAll: unit -> unit
}

type TreeStyleFn<'T> = TreeNodeKind option -> TreeItem<'T> option -> string list -> string list

type TreeContextMenuFn<'T> = TreeItem<'T> option -> ContextMenuItem[]

type TreeContextValue<'T> = {
    DataSource: TreeDataSource<'T> option
    SelectionMode: TreeSelectionMode
    SelectionDisabled: bool
    IsNodeSelectable: TreeItem<'T> -> bool
    RenderNode: (TreeRenderProps<'T> -> ReactElement) option
    Leading: (TreeRenderProps<'T> -> ReactElement) option
    Trailing: (TreeRenderProps<'T> -> ReactElement) option
    StyleFn: TreeStyleFn<'T> option
    ContextMenuItems: TreeContextMenuFn<'T> option
    OnError: exn -> unit
    Debug: bool
}

type TreeState<'T> = {
    ExpandedIds: Set<string>
    SetExpandedIds: (Set<string> -> Set<string>) -> unit
    SelectedIds: Set<string>
    SetSelectedIds: (Set<string> -> Set<string>) -> unit
    FocusedId: string option
    SetFocusedId: string option -> unit
    LoadedChildren: Map<string, TreeLoadState<'T>>
    SetLoadedChildren: (Map<string, TreeLoadState<'T>> -> Map<string, TreeLoadState<'T>>) -> unit
}

type TreeNodeActions<'T> = {
    ExpandNode: TreeItem<'T> -> unit
    SelectNode: TreeItem<'T> -> bool -> unit
    FocusById: string -> unit
    OnNodeKeyDown: TreeItem<'T> -> KeyboardEvent -> unit
}

type TreeNodeProps<'T> = {
    Row: TreeVisibleNode<'T>
    IsExpanded: bool
    IsSelected: bool
    IsFocused: bool
    IsLoading: bool
    Error: string option
    CanExpand: bool
    CanSelect: bool
    RenderNode: (TreeRenderProps<'T> -> ReactElement) option
    Leading: (TreeRenderProps<'T> -> ReactElement) option
    Trailing: (TreeRenderProps<'T> -> ReactElement) option
    StyleFn: TreeStyleFn<'T> option
    OnToggle: unit -> unit
    OnSelect: MouseEvent -> unit
    OnFocus: unit -> unit
    OnKeyDown: KeyboardEvent -> unit
    Debug: bool
}
