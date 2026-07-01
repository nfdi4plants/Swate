module Swate.Components.Primitive.Tree.ContextMenuAdapter

open Browser.Types
open Feliz
open Swate.Components.Primitive.ContextMenu
open Swate.Components.Primitive.ContextMenu.Types
open Swate.Components.Primitive.Tree.Dom
open Swate.Components.Primitive.Tree.State
open Swate.Components.Primitive.Tree.Types

let tryGetTarget (lookup: TreeRowLookup<'T>) (event: MouseEvent) =
    match TreeDom.tryGetNodeId event with
    | Some nodeId -> lookup.Nodes |> Map.tryFind nodeId |> Option.map Some
    | None -> Some None

let render
    (contextMenuItems: TreeContextMenuFn<'T> option)
    (treeRef: IRefValue<HTMLElement option>)
    (lookup: TreeRowLookup<'T>)
    debug
    =
    contextMenuItems
    |> Option.map (fun menuItems ->
        ContextMenu.ContextMenu(
            (fun data -> menuItems (unbox<TreeItem<'T> option> data) |> Array.toList),
            ref = treeRef,
            onSpawn = (fun event -> tryGetTarget lookup event |> Option.map box),
            debug = debug
        )
    )
    |> Option.defaultValue Html.none
