module Swate.Components.Composite.Tree.Types

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components.Primitive.ContextMenu.Types

/// Distinguishes expandable branch nodes from terminal leaf nodes.
[<StringEnum(CaseRules.LowerFirst)>]
type TreeNodeKind =
    | Branch
    | Leaf

/// Defines whether the tree stores one selected node or a set of selected nodes.
[<StringEnum(CaseRules.LowerFirst)>]
type TreeSelectionMode =
    | Single
    | Multiple

/// Describes the lifecycle state for children loaded through a TreeDataSource.
[<StringEnum(CaseRules.LowerFirst)>]
type TreeLazyLoadStatus =
    | Idle
    | Loading
    | Loaded
    | Error

/// JavaScript-facing tree node model used by static and datasource-backed trees.
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

/// Runtime state passed to custom node renderers for content, leading, and trailing slots.
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

/// A flattened tree row with depth and parent metadata for rendering and navigation.
type TreeVisibleNode<'T> = {
    Node: TreeItem<'T>
    Depth: int
    ParentId: string option
}

/// Cached load result for a node whose children are provided asynchronously.
type TreeLoadState<'T> = {
    Status: TreeLazyLoadStatus
    Children: TreeItem<'T>[] option
    Error: string option
    RequestId: int option
}

/// Lookup tables derived from the currently visible tree rows.
type TreeRowLookup<'T> = {
    Nodes: Map<string, TreeItem<'T>>
    Parents: Map<string, string>
    VisibleNodes: TreeVisibleNode<'T>[]
}

/// Datasource adapter for lazy trees; unknown child counts are represented by negative values.
type TreeDataSource<'T> = {
    GetChildrenCount: TreeItem<'T> option -> int
    GetTreeItems: TreeItem<'T> option -> JS.Promise<TreeItem<'T>[]>
}

/// Imperative cache invalidation API exposed to consumers through apiRef.
type TreeApi = {
    InvalidateNode: string -> unit
    InvalidateAll: unit -> unit
}

/// Allows consumers to extend or replace the generated CSS class list for tree rows.
type TreeStyleFn<'T> = TreeNodeKind option -> TreeItem<'T> option -> string list -> string list

/// Creates context menu items for a node target or for the tree root when no node is targeted.
type TreeContextMenuFn<'T> = TreeItem<'T> option -> ContextMenuItem[]

/// Context value shared by tree subcomponents that need access to tree-level configuration.
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

/// Internal React state container used by the tree hooks and controller.
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

/// Coordinates DOM focus, virtualized scrolling, and visible-row lookup for keyboard navigation.
type TreeFocusController<'T> = {
    Lookup: TreeRowLookup<'T>
    SetFocusedId: string option -> unit
    ScrollToIndex: int -> unit
    FocusDom: string -> unit
}

/// Event handlers produced for tree rows by the controller hook.
type TreeNodeActions<'T> = {
    ExpandNode: TreeItem<'T> -> unit
    SelectNode: TreeItem<'T> -> bool -> unit
    OnNodeKeyDown: TreeItem<'T> -> KeyboardEvent -> unit
}
