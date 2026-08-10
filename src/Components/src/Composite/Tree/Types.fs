module Swate.Components.Composite.Tree.Types

open Browser.Types
open Fable.Core
open Feliz

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
/// StringEnum emits this value as a native JavaScript string rather than an object.
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
    member val icon: ReactElement option = icon with get, set
    member val tooltip: string option = tooltip with get, set
    member val leading: ReactElement option = leading with get, set
    member val trailing: ReactElement option = trailing with get, set
    member val className: string option = className with get, set

/// Runtime state passed to custom node renderers for content, leading, and trailing slots.
[<JS.Pojo>]
type TreeRenderProps<'T>
    (
        node: TreeItem<'T>,
        depth: int,
        isExpanded: bool,
        isSelected: bool,
        isFocused: bool,
        isLoading: bool,
        error: string option,
        toggle: unit -> unit,
        select: MouseEvent -> unit
    ) =
    member val node = node with get, set
    member val depth = depth with get, set
    member val isExpanded = isExpanded with get, set
    member val isSelected = isSelected with get, set
    member val isFocused = isFocused with get, set
    member val isLoading = isLoading with get, set
    member val error = error with get, set
    member val toggle = toggle with get, set
    member val select = select with get, set

/// A flattened tree row with depth and parent metadata for rendering and navigation.
type TreeVisibleNode<'T> = {
    node: TreeItem<'T>
    depth: int
    parentId: string option
    posInSet: int
    setSize: int
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
[<JS.Pojo>]
type TreeDataSource<'T>
    (getChildrenCount: TreeItem<'T> option -> int, getTreeItems: TreeItem<'T> option -> JS.Promise<TreeItem<'T>[]>) =
    member val getChildrenCount = getChildrenCount with get, set
    member val getTreeItems = getTreeItems with get, set

/// Imperative cache invalidation API exposed to consumers through apiRef.
[<JS.Pojo>]
type TreeApi(invalidateNode: string -> unit, invalidateAll: unit -> unit) =
    member val invalidateNode = invalidateNode with get, set
    member val invalidateAll = invalidateAll with get, set

/// Allows consumers to extend or replace the generated CSS class list for tree rows.
type TreeStyleFn<'T> = TreeItem<'T> option -> string[] -> string[]

/// Exposes the browser context-menu event together with its node target, or None for the tree root.
type TreeContextMenuEvent<'T> = delegate of MouseEvent * TreeItem<'T> option -> unit

/// Context value shared by tree subcomponents that need access to tree-level configuration.
type TreeContextValue<'T> = {
    DataSource: TreeDataSource<'T> option
    SelectionMode: TreeSelectionMode
    SelectedIds: string[] option
    DefaultSelectedIds: string[] option
    DefaultExpandedIds: string[] option
    OnSelectionChange: (string[] -> unit) option
    SelectionDisabled: bool
    IsNodeSelectable: TreeItem<'T> -> bool
    EnableLazyLoading: bool
    EnableVirtualization: bool
    EstimateNodeHeight: int
    OnContextMenu: TreeContextMenuEvent<'T> option
    RenderNode: (TreeRenderProps<'T> -> ReactElement) option
    Leading: (TreeRenderProps<'T> -> ReactElement) option
    Trailing: (TreeRenderProps<'T> -> ReactElement) option
    StyleFn: TreeStyleFn<'T> option
    OnError: exn -> unit
    ApiRef: IRefValue<TreeApi option> option
    AriaLabel: string
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
