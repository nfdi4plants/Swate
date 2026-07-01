module Swate.Components.Primitive.Tree.Hooks

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components.Primitive.Tree.Dom
open Swate.Components.Primitive.Tree.State
open Swate.Components.Primitive.Tree.Types

let private toSet (values: string[] option) =
    values |> Option.map Set.ofArray |> Option.defaultValue Set.empty

[<Hook>]
let useTreeState (defaultExpandedIds: string[] option) (defaultSelectedIds: string[] option) =
    let expandedIds, setExpandedIds =
        React.useStateWithUpdater (toSet defaultExpandedIds)

    let selectedIds, setSelectedIds =
        React.useStateWithUpdater (toSet defaultSelectedIds)

    let focusedId, setFocusedId = React.useState<string option> None

    let loadedChildren, setLoadedChildren =
        React.useStateWithUpdater<Map<string, TreeLoadState<'T>>> (Map.empty)

    {
        ExpandedIds = expandedIds
        SetExpandedIds = setExpandedIds
        SelectedIds = selectedIds
        SetSelectedIds = setSelectedIds
        FocusedId = focusedId
        SetFocusedId = setFocusedId
        LoadedChildren = loadedChildren
        SetLoadedChildren = setLoadedChildren
    }

[<Hook>]
let useControlledSelection
    (selectedIds: string[] option)
    (onSelectionChange: (string[] -> unit) option)
    (treeState: TreeState<'T>)
    =
    let effectiveSelectedIds =
        selectedIds
        |> Option.map Set.ofArray
        |> Option.defaultValue treeState.SelectedIds

    let setSelection nextSelectedIds =
        match selectedIds with
        | Some _ -> ()
        | None -> treeState.SetSelectedIds(fun _ -> nextSelectedIds)

        onSelectionChange
        |> Option.iter (fun handler -> handler (nextSelectedIds |> Set.toArray))

    effectiveSelectedIds, setSelection

let private animatedVisibleNode row anchorId isExiting = {
    Row = row
    IsExiting = isExiting
    AnchorId = anchorId
}

[<Hook>]
let useAnimatedVisibleRows (rows: TreeVisibleNode<'T>[]) enableAnimation =
    let exitingRows, setExitingRows =
        React.useStateWithUpdater<TreeAnimatedVisibleNode<'T>[]> [||]

    let previousRowsRef = React.useRef rows
    let exitingRowsRef = React.useRef exitingRows

    React.useEffect ((fun () -> exitingRowsRef.current <- exitingRows), [| box exitingRows |])

    React.useEffect (
        (fun () ->
            let previousRows = previousRowsRef.current
            previousRowsRef.current <- rows

            if not enableAnimation then
                if exitingRowsRef.current.Length > 0 then
                    setExitingRows (fun _ -> [||])

                fun () -> ()
            else
                let nextIds = rows |> Array.map (fun row -> row.Node.id) |> Set.ofArray

                let previousRowsById =
                    previousRows |> Array.map (fun row -> row.Node.id, row) |> Map.ofArray

                let rec nearestRetainedAncestor (row: TreeVisibleNode<'T>) =
                    match row.ParentId with
                    | Some parentId when nextIds.Contains parentId -> Some parentId
                    | Some parentId -> previousRowsById |> Map.tryFind parentId |> Option.bind nearestRetainedAncestor
                    | None -> None

                let removedRows =
                    previousRows
                    |> Array.choose (fun row ->
                        if nextIds.Contains row.Node.id then
                            None
                        else
                            Some(animatedVisibleNode row (nearestRetainedAncestor row) true)
                    )

                if removedRows.Length > 0 then
                    setExitingRows (fun current ->
                        Array.append current removedRows
                        |> Array.distinctBy (fun item -> item.Row.Node.id)
                    )

                    let timeoutId =
                        Fable.Core.JS.setTimeout
                            (fun () ->
                                setExitingRows (fun current ->
                                    current |> Array.filter (fun item -> not item.IsExiting)
                                )
                            )
                            180

                    fun () -> Fable.Core.JS.clearTimeout timeoutId
                else
                    let currentVisibleExitingRows =
                        exitingRowsRef.current
                        |> Array.filter (fun item -> not (nextIds.Contains item.Row.Node.id))

                    if currentVisibleExitingRows.Length <> exitingRowsRef.current.Length then
                        setExitingRows (fun _ -> currentVisibleExitingRows)

                    fun () -> ()
        ),
        [| box rows; box enableAnimation |]
    )

    let nextIds = rows |> Array.map (fun row -> row.Node.id) |> Set.ofArray

    let activeRows = rows |> Array.map (fun row -> animatedVisibleNode row None false)

    let visibleExitingRows =
        exitingRows
        |> Array.filter (fun item -> not (nextIds.Contains item.Row.Node.id))

    let exitingByAnchor = visibleExitingRows |> Array.groupBy _.AnchorId |> Map.ofArray

    let mergedRows = ResizeArray<TreeAnimatedVisibleNode<'T>>()

    let addExitingRows anchor =
        exitingByAnchor
        |> Map.tryFind anchor
        |> Option.iter (fun rows -> rows |> Array.iter (fun row -> mergedRows.Add row))

    activeRows
    |> Array.iter (fun item ->
        mergedRows.Add item
        addExitingRows (Some item.Row.Node.id)
    )

    addExitingRows None
    mergedRows.ToArray()

[<Hook>]
let useTreeApi (apiRef: IRefValue<TreeApi option> option) setLoadedChildren setExpandedIds =
    React.useEffect (
        (fun () ->
            apiRef
            |> Option.iter (fun ref ->
                ref.current <-
                    Some {
                        InvalidateNode =
                            fun nodeId ->
                                setLoadedChildren (NodeState.invalidateNode nodeId)
                                setExpandedIds (fun current -> current |> Set.remove nodeId)
                        InvalidateAll =
                            fun () ->
                                setLoadedChildren (fun _ -> Map.empty)
                                setExpandedIds (fun _ -> Set.empty)
                    }
            )

            fun () -> apiRef |> Option.iter (fun ref -> ref.current <- None)
        ),
        [| box apiRef |]
    )

[<Hook>]
let useTreeNodeActions
    (treeRef: IRefValue<HTMLElement option>)
    (dataSource: TreeDataSource<'T> option)
    selectionMode
    isSelectionDisabled
    isNodeSelectable
    enableLazyLoading
    (treeState: TreeState<'T>)
    (lookup: TreeRowLookup<'T>)
    focusedId
    effectiveSelectedIds
    setSelection
    onError
    =
    let focusDom = TreeDom.focusNode treeRef

    let focusById nodeId =
        TreeController.tryFocusById lookup treeState.SetFocusedId focusDom nodeId

    let expandNode (node: TreeItem<'T>) =
        TreeController.expandNode
            dataSource
            enableLazyLoading
            treeState.LoadedChildren
            treeState.ExpandedIds
            treeState.SetExpandedIds
            treeState.SetLoadedChildren
            onError
            node

    let selectNode (node: TreeItem<'T>) =
        TreeController.focusNode treeState.SetFocusedId focusDom node.id

        TreeController.selectNode
            selectionMode
            isSelectionDisabled
            isNodeSelectable
            effectiveSelectedIds
            setSelection
            node

    let onNodeKeyDown (node: TreeItem<'T>) (event: KeyboardEvent) =
        match event.key with
        | "ArrowDown" ->
            event.preventDefault ()
            TreeController.focusByDelta lookup focusedId treeState.SetFocusedId focusDom 1
        | "ArrowUp" ->
            event.preventDefault ()
            TreeController.focusByDelta lookup focusedId treeState.SetFocusedId focusDom -1
        | "Home" ->
            event.preventDefault ()
            TreeController.focusFirst lookup treeState.SetFocusedId focusDom
        | "End" ->
            event.preventDefault ()
            TreeController.focusLast lookup treeState.SetFocusedId focusDom
        | "ArrowRight" ->
            event.preventDefault ()

            if
                NodeState.canExpand dataSource node
                && not (treeState.ExpandedIds.Contains node.id)
            then
                expandNode node
        | "ArrowLeft" ->
            event.preventDefault ()

            TreeController.collapseOrFocusParent
                lookup
                treeState.ExpandedIds
                treeState.SetExpandedIds
                treeState.SetFocusedId
                focusDom
                node.id
        | "Enter"
        | " " ->
            event.preventDefault ()
            selectNode node
        | _ -> ()

    {
        ExpandNode = expandNode
        SelectNode = selectNode
        FocusById = focusById
        OnNodeKeyDown = onNodeKeyDown
    }
