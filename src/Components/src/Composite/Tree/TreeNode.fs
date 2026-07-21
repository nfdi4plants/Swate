namespace Swate.Components.Composite.Tree

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components.Composite.Tree.Types

[<Erase; Mangle(false)>]
type TreeNode =

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member Row<'T>
        (
            row: TreeVisibleNode<'T>,
            isExpanded: bool,
            isSelected: bool,
            isFocused: bool,
            isLoading: bool,
            error: string option,
            canExpand: bool,
            canSelect: bool,
            ?renderNode: TreeRenderProps<'T> -> ReactElement,
            ?leading: TreeRenderProps<'T> -> ReactElement,
            ?trailing: TreeRenderProps<'T> -> ReactElement,
            ?styleFn: TreeStyleFn<'T>,
            ?onToggle: unit -> unit,
            ?onSelect: MouseEvent -> unit,
            ?onFocus: unit -> unit,
            ?onKeyDown: KeyboardEvent -> unit,
            ?debug: bool
        ) =
        let node = row.Node
        let onToggle = defaultArg onToggle ignore
        let onSelect = defaultArg onSelect ignore
        let onFocus = defaultArg onFocus ignore
        let onKeyDown = defaultArg onKeyDown ignore
        let debug = defaultArg debug false

        let renderProps: TreeRenderProps<'T> = {
            Node = node
            Depth = row.Depth
            IsExpanded = isExpanded
            IsSelected = isSelected
            IsFocused = isFocused
            IsLoading = isLoading
            Error = error
            Toggle = onToggle
            Select = onSelect
        }

        let expandButton =
            if canExpand then
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-ghost swt:btn-square swt:btn-xs swt:min-h-0 swt:size-6 swt:shrink-0"
                    prop.tabIndex -1
                    prop.ariaLabel (
                        if isExpanded then
                            $"Collapse {node.label}"
                        else
                            $"Expand {node.label}"
                    )
                    prop.onClick (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                        onToggle ()
                    )
                    prop.children [
                        if isLoading then
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-xs"
                            ]
                        else
                            Html.i [
                                prop.className $"swt:iconify {TreeHelper.chevronIcon isExpanded} swt:size-4"
                            ]
                    ]
                ]
            else
                Html.span [
                    prop.ariaHidden true
                    prop.className "swt:size-6 swt:shrink-0"
                ]

        let leadingContent =
            match leading with
            | Some leading -> leading renderProps
            | None ->
                match node.leading with
                | Some leading -> leading
                | None ->
                    match node.icon with
                    | Some icon -> icon
                    | None ->
                        Html.i [
                            prop.className [
                                $"swt:iconify {TreeHelper.defaultIcon node} swt:size-4 swt:shrink-0"
                            ]
                        ]

        let nodeContent =
            match renderNode with
            | Some renderNode ->
                Html.div [
                    prop.className "swt:min-w-0 swt:flex-1 swt:text-left"
                    prop.children [ renderNode renderProps ]
                ]
            | None ->
                Html.span [
                    prop.className "swt:min-w-0 swt:flex-1 swt:truncate swt:text-left"
                    prop.text node.label
                ]

        let errorContent =
            match error with
            | Some error ->
                Html.span [
                    prop.className "swt:badge swt:badge-error swt:badge-sm swt:shrink-0"
                    prop.title error
                    prop.text "Error"
                ]
            | None -> Html.none

        let trailingContent =
            match trailing with
            | Some trailing -> trailing renderProps
            | None ->
                match node.trailing with
                | Some trailing -> trailing
                | None -> Html.none

        Html.div [
            prop.role "treeitem"
            prop.tabIndex (if isFocused then 0 else -1)
            prop.custom ("aria-selected", isSelected)
            if not canSelect && not canExpand then
                prop.custom ("aria-disabled", true)
            prop.custom ("aria-level", row.Depth + 1)
            if canExpand then
                prop.custom ("aria-expanded", isExpanded)
            prop.custom ("data-tree-node-id", node.id)
            prop.custom ("data-tree-node-kind", string node.kind)
            if debug then
                prop.testId $"tree-node-{node.id}"
            prop.className (TreeHelper.nodeContainerClasses row canSelect canExpand isSelected isFocused styleFn)
            prop.style [ style.paddingLeft (length.rem (float row.Depth * 1.25)) ]
            prop.title (node.tooltip |> Option.defaultValue node.label)
            prop.onClick onSelect
            prop.onFocus (fun _ -> onFocus ())
            prop.onKeyDown onKeyDown
            prop.children [
                Html.div [
                    prop.className [
                        "swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-2"
                        if node.kind = TreeNodeKind.Branch then
                            "swt:pl-4"
                    ]
                    prop.children [ leadingContent; nodeContent ]
                ]

                Html.div [
                    prop.className "swt:ml-auto swt:flex swt:shrink-0 swt:items-center swt:justify-end swt:gap-2"
                    prop.children [ trailingContent; errorContent; expandButton ]
                ]
            ]
        ]
