module Swate.Components.Composite.Tree.TreeHelper

open Swate.Components.Composite.Tree.Types

let rootClasses styleFn =
    let baseClasses = [
        "swt:menu swt:w-full swt:min-w-0 swt:rounded-box swt:bg-base-100 swt:p-1"
    ]

    styleFn
    |> Option.map (fun fn -> fn None None baseClasses)
    |> Option.defaultValue baseClasses

let shouldUseVirtualization enableVirtualization visibleCount =
    enableVirtualization && visibleCount > 0

let selectedIdsArray selectedIds = selectedIds |> Set.toArray

let nodeContainerClasses (row: TreeVisibleNode<'T>) canSelect canExpand isSelected isFocused styleFn =
    let baseClasses = [
        "swt:group swt:flex swt:min-h-8 swt:w-full swt:min-w-0 swt:items-center swt:gap-1 swt:rounded-md swt:px-1 swt:py-0.5 swt:text-sm swt:outline-none"
        if canSelect || canExpand then
            "swt:cursor-pointer swt:hover:bg-base-200"
        else
            "swt:cursor-default swt:opacity-80"
        if isSelected then
            "swt:bg-primary swt:text-primary-content swt:hover:bg-primary"
        elif isFocused then
            "swt:bg-base-200"
        yield! row.Node.className |> Option.map List.singleton |> Option.defaultValue []
    ]

    styleFn
    |> Option.map (fun styleFn -> styleFn (Some row.Node.kind) (Some row.Node) baseClasses)
    |> Option.defaultValue baseClasses

let chevronIcon isExpanded =
    if isExpanded then
        "swt:fluent--chevron-down-20-regular"
    else
        "swt:fluent--chevron-right-20-regular"

let defaultIcon (node: TreeItem<'T>) =
    match node.kind with
    | TreeNodeKind.Branch -> "swt:fluent--folder-24-regular"
    | TreeNodeKind.Leaf -> "swt:fluent--document-24-regular"
