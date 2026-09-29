module Swate.Components.Composite.Tree.Types

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components.Primitive.ContextMenu.Types

/// Defines whether the tree stores one selected node or a set of selected nodes.
[<RequireQualifiedAccess; StringEnum(CaseRules.LowerFirst)>]
type TreeSelectionMode =
    | Single
    | Multiple

/// Describes the lifecycle state for children loaded through a TreeDataSource.
/// StringEnum emits this value as a native JavaScript string rather than an object.
[<RequireQualifiedAccess; StringEnum(CaseRules.LowerFirst)>]
type internal TreeLazyLoadStatus =
    | Idle
    | Loading
    | Loaded
    | Error

/// JavaScript-facing properties shared by leaf and branch tree items.
[<AllowNullLiteral; JS.Pojo>]
type TreeItemProps<'T>
    (
        id: string,
        label: string,
        ?data: 'T,
        ?icon: ReactElement,
        ?tooltip: string,
        ?leading: ReactElement,
        ?trailing: ReactElement,
        ?className: string
    ) =
    member val id = id with get, set
    member val label = label with get, set
    member val data: 'T option = data with get, set
    member val icon: ReactElement option = icon with get, set
    member val tooltip: string option = tooltip with get, set
    member val leading: ReactElement option = leading with get, set
    member val trailing: ReactElement option = trailing with get, set
    member val className: string option = className with get, set

/// JavaScript-facing tree node model that prevents leaves from carrying children.
[<TypeScriptTaggedUnion("type")>]
type TreeItem<'T> =
    | Leaf of props: TreeItemProps<'T>
    | Branch of props: TreeItemProps<'T> * children: TreeItem<'T>[] option

[<RequireQualifiedAccess>]
module internal TreeItem =

    let props item =
        match item with
        | Leaf props
        | Branch(props, _) -> props

    let getId item = (props item).id

    let isBranch item =
        match item with
        | Branch _ -> true
        | Leaf _ -> false

    let tryGetChildren item =
        match item with
        | Leaf _ -> None
        | Branch(_, children) -> children

/// Runtime state passed to custom node renderers for content, leading, and trailing slots.
[<JS.Pojo>]
type TreeRenderProps<'T>
    (
        node: TreeItem<'T>,
        depth: int,
        isExpanded: bool,
        isSelected: bool,
        isActive: bool,
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
    member val isActive = isActive with get, set
    member val isFocused = isFocused with get, set
    member val isLoading = isLoading with get, set
    member val error = error with get, set
    member val toggle = toggle with get, set
    member val select = select with get, set

/// A flattened tree row with depth and parent metadata for rendering and navigation.
type internal TreeVisibleNode<'T> = {
    node: TreeItem<'T>
    comparisonNode: TreeItem<'T>
    depth: int
    parentId: string option
    posInSet: int
    setSize: int
}

/// Cached load result for a node whose children are provided asynchronously.
type internal TreeLoadState<'T> = {
    status: TreeLazyLoadStatus
    children: TreeItem<'T>[] option
    error: string option
}

/// Lookup tables derived from the currently visible tree rows.
type internal TreeRowLookup<'T> = {
    nodes: Map<string, TreeItem<'T>>
    parents: Map<string, string>
    indices: Map<string, int>
    visibleNodes: TreeVisibleNode<'T>[]
}

/// Datasource adapter for lazy trees.
[<JS.Pojo>]
type TreeDataSource<'T>(getTreeItems: TreeItem<'T> option -> JS.Promise<TreeItem<'T>[]>) =
    member val getTreeItems = getTreeItems with get, set

/// Imperative cache invalidation API exposed to consumers through apiRef.
[<JS.Pojo>]
type TreeApi(invalidateNode: string -> unit, invalidateAll: unit -> unit) =
    member val invalidateNode = invalidateNode with get, set
    member val invalidateAll = invalidateAll with get, set

/// Allows consumers to extend or replace the generated CSS class list for tree rows.
type TreeStyleFn<'T> = TreeItem<'T> option -> string[] -> string[]

/// Builds context-menu entries for a tree node target, or for the tree root when no node is targeted.
type TreeContextMenuEvent<'T> = delegate of MouseEvent * TreeItem<'T> option -> ContextMenuItem[]

/// Internal React state container used by the tree hooks and controller.
type internal TreeState<'T> = {
    expandedIds: Set<string>
    setExpandedIds: (Set<string> -> Set<string>) -> unit
    selectedIds: string[]
    setSelectedIds: (string[] -> string[]) -> unit
    activeId: string option
    setActiveId: string option -> unit
    focusedId: string option
    setFocusedId: string option -> unit
    selectionAnchorId: string option
    setSelectionAnchorId: string option -> unit
    loadedChildren: Map<string, TreeLoadState<'T>>
    setLoadedChildren: (Map<string, TreeLoadState<'T>> -> Map<string, TreeLoadState<'T>>) -> unit
}

/// Coordinates DOM focus, virtualized scrolling, and visible-row lookup for keyboard navigation.
type internal TreeFocusController<'T> = {
    lookup: TreeRowLookup<'T>
    setActiveId: string option -> unit
    setFocusedId: string option -> unit
    scrollToIndex: int -> unit
    focusDom: string -> unit
}

/// Tracks the current request per cache entry so stale async results are ignored.
type internal TreeLoadTracker = {
    mutable nextRequestId: int
    mutable activeRequestIds: Map<string, int>
}

/// Values required to load one datasource-backed cache entry.
type internal TreeLoadController<'T> = {
    dataSource: TreeDataSource<'T> option
    trackerRef: IRefValue<TreeLoadTracker>
    treeState: TreeState<'T>
    onError: exn -> unit
}

/// Latest state and callbacks read by stable row event handlers.
type internal TreeNodeActionState<'T> = {
    items: TreeItem<'T>[]
    dataSource: TreeDataSource<'T> option
    isSelectionDisabled: bool
    isNodeSelectable: TreeItem<'T> -> bool
    lookup: TreeRowLookup<'T>
    focusedId: string option
    selectionMode: TreeSelectionMode
    effectiveSelectedIds: string[]
    scrollToIndex: int -> unit
    setSelection: string[] -> unit
    treeState: TreeState<'T>
    onError: exn -> unit
}

/// Current consumer-provided node renderers read by memoized rows.
type internal TreePresentation<'T> = {
    renderNode: (TreeRenderProps<'T> -> ReactElement) option
    leading: (TreeRenderProps<'T> -> ReactElement) option
    trailing: (TreeRenderProps<'T> -> ReactElement) option
}

/// Values required to apply one user selection intent.
type internal TreeSelectionController<'T> = {
    selectionMode: TreeSelectionMode
    isSelectionDisabled: bool
    isNodeSelectable: TreeItem<'T> -> bool
    lookup: TreeRowLookup<'T>
    effectiveSelectedIds: string[]
    setSelection: string[] -> unit
    treeState: TreeState<'T>
}

/// Describes how a user interaction changes the current selection.
type internal TreeSelectionIntent =
    | Replace
    | Toggle
    | Range

/// Event handlers produced for tree rows by the controller hook.
type internal TreeNodeActions<'T> = {
    expandNode: string -> unit
    selectNode: string -> TreeSelectionIntent -> unit
    onNodeKeyDown: string -> KeyboardEvent -> unit
}
