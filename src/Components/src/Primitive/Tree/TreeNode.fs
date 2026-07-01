namespace Swate.Components.Primitive.Tree

open Fable.Core
open Feliz
open Swate.Components.Primitive.Tree.Helper
open Swate.Components.Primitive.Tree.Types

[<Erase; Mangle(false)>]
type TreeNode =

    [<ReactComponent>]
    static member Row<'T>(props: TreeNodeProps<'T>) =
        let node = props.Row.Node

        let renderProps: TreeRenderProps<'T> = {
            Node = node
            Depth = props.Row.Depth
            IsExpanded = props.IsExpanded
            IsSelected = props.IsSelected
            IsFocused = props.IsFocused
            IsLoading = props.IsLoading
            Error = props.Error
            Toggle = props.OnToggle
            Select = props.OnSelect
        }

        Html.div [
            prop.role "treeitem"
            prop.tabIndex (if props.IsFocused then 0 else -1)
            prop.custom ("aria-selected", props.IsSelected)
            prop.custom ("aria-disabled", not props.CanSelect)
            prop.custom ("aria-level", props.Row.Depth + 1)
            if node.kind = TreeNodeKind.Branch then
                prop.custom ("aria-expanded", props.IsExpanded)
            prop.custom ("data-tree-node-id", node.id)
            prop.custom ("data-tree-node-kind", string node.kind)
            if props.Debug then
                prop.testId $"tree-node-{node.id}"
            prop.className (NodeHelper.nodeContainerClasses props)
            prop.style [
                style.paddingLeft (length.rem (float props.Row.Depth * 1.25))
            ]
            prop.title (node.tooltip |> Option.defaultValue node.label)
            prop.onClick props.OnSelect
            prop.onFocus (fun _ -> props.OnFocus())
            prop.onKeyDown props.OnKeyDown
            prop.children [
                Html.button [
                    prop.type'.button
                    prop.className [
                        "swt:btn swt:btn-ghost swt:btn-square swt:btn-xs swt:min-h-0 swt:size-6 swt:shrink-0"
                        if not props.CanExpand then
                            "swt:invisible"
                    ]
                    prop.tabIndex -1
                    prop.ariaLabel (
                        if props.IsExpanded then
                            $"Collapse {node.label}"
                        else
                            $"Expand {node.label}"
                    )
                    prop.onClick (fun e ->
                        e.preventDefault ()
                        e.stopPropagation ()
                        props.OnToggle()
                    )
                    prop.children [
                        if props.IsLoading then
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-xs"
                            ]
                        else
                            Html.i [
                                prop.className $"swt:iconify {NodeHelper.chevronIcon props.IsExpanded} swt:size-4"
                            ]
                    ]
                ]

                match props.Leading with
                | Some leading -> leading renderProps
                | None ->
                    match node.leading with
                    | Some leading -> leading
                    | None ->
                        match node.icon with
                        | Some icon -> icon
                        | None ->
                            Html.i [
                                prop.className $"swt:iconify {NodeHelper.defaultIcon node} swt:size-4 swt:shrink-0"
                            ]

                match props.RenderNode with
                | Some renderNode -> renderNode renderProps
                | None ->
                    Html.span [
                        prop.className "swt:min-w-0 swt:flex-1 swt:truncate swt:text-left"
                        prop.text node.label
                    ]

                match props.Error with
                | Some error ->
                    Html.span [
                        prop.className "swt:badge swt:badge-error swt:badge-sm swt:shrink-0"
                        prop.title error
                        prop.text "Error"
                    ]
                | None -> ()

                match props.Trailing with
                | Some trailing -> trailing renderProps
                | None ->
                    match node.trailing with
                    | Some trailing -> trailing
                    | None -> Html.none
            ]
        ]
