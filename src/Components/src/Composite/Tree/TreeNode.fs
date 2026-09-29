namespace Swate.Components.Composite.Tree

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components.Composite.Tree.Dom
open Swate.Components.Composite.Tree.Types

[<Erase; Mangle(false)>]
type internal TreeNode =

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member private Surface<'T>
        (
            node: TreeItem<'T>,
            depth: int,
            isExpanded: bool,
            isSelected: bool,
            isActive: bool,
            isFocused: bool,
            isLoading: bool,
            error: string option,
            canExpand: bool,
            presentationRef: IRefValue<TreePresentation<'T>>,
            onToggle: unit -> unit,
            onSelect: MouseEvent -> unit
        ) =
        let presentation = presentationRef.current
        let nodeProps = TreeItem.props node

        let renderProps =
            TreeRenderProps(
                node,
                depth,
                isExpanded,
                isSelected,
                isActive,
                isFocused,
                isLoading,
                error,
                onToggle,
                onSelect
            )

        let expandButton =
            if canExpand then
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-ghost swt:btn-square swt:btn-xs swt:min-h-0 swt:size-6 swt:shrink-0"
                    prop.tabIndex -1
                    prop.ariaLabel (
                        if isExpanded then
                            $"Collapse {nodeProps.label}"
                        else
                            $"Expand {nodeProps.label}"
                    )
                    // Keep the treeitem as the DOM focus owner when its chevron is pressed.
                    prop.onMouseDown (fun event -> event.preventDefault ())
                    prop.onClick (fun event ->
                        event.preventDefault ()
                        event.stopPropagation ()
                        focusTreeItemFromEvent event
                        onToggle ()
                    )
                    prop.children [
                        if isLoading then
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-xs"
                            ]
                        else
                            Html.i [
                                prop.className $"swt:iconify {Helper.chevronIcon isExpanded} swt:size-4"
                            ]
                    ]
                ]
            else
                Html.span [
                    prop.ariaHidden true
                    prop.className "swt:size-6 swt:shrink-0"
                ]

        let leadingContent =
            match presentation.leading with
            | Some leading -> leading renderProps
            | None ->
                match nodeProps.leading with
                | Some leading -> leading
                | None ->
                    match nodeProps.icon with
                    | Some icon -> icon
                    | None ->
                        Html.i [
                            prop.className [
                                $"swt:iconify {Helper.defaultIcon node} swt:size-4 swt:shrink-0"
                            ]
                        ]

        let nodeContent =
            match presentation.renderNode with
            | Some renderNode ->
                Html.div [
                    prop.className "swt:min-w-0 swt:flex-1 swt:text-left"
                    prop.children [ renderNode renderProps ]
                ]
            | None ->
                Html.span [
                    prop.className "swt:min-w-0 swt:flex-1 swt:truncate swt:text-left"
                    prop.text nodeProps.label
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
            match presentation.trailing with
            | Some trailing -> trailing renderProps
            | None -> nodeProps.trailing |> Option.defaultValue Html.none

        React.Fragment [
            Html.div [
                prop.className [
                    "swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-2"
                    if TreeItem.isBranch node then
                        "swt:pl-4"
                ]
                prop.children [ leadingContent; nodeContent ]
            ]

            Html.div [
                prop.className "swt:ml-auto swt:flex swt:shrink-0 swt:items-center swt:justify-end swt:gap-2"
                prop.children [ trailingContent; errorContent; expandButton ]
            ]
        ]

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member TreeNode<'T>
        (
            nodeId: string,
            comparisonNode: TreeItem<'T>,
            depth: int,
            posInSet: int,
            setSize: int,
            isExpanded: bool,
            isSelected: bool,
            isActive: bool,
            isTabStop: bool,
            isFocused: bool,
            isLoading: bool,
            error: string option,
            canSelect: bool,
            canExpand: bool,
            className: string[],
            debug: bool,
            nodesRef: IRefValue<Map<string, TreeItem<'T>>>,
            presentationRef: IRefValue<TreePresentation<'T>>,
            ?onToggle: unit -> unit,
            ?onSelect: MouseEvent -> unit,
            ?onFocus: unit -> unit,
            ?onKeyDown: KeyboardEvent -> unit
        ) =
        let node =
            nodesRef.current |> Map.tryFind nodeId |> Option.defaultValue comparisonNode

        let nodeProps = TreeItem.props node
        let onToggle = defaultArg onToggle ignore
        let onSelect = defaultArg onSelect ignore
        let onFocus = defaultArg onFocus ignore
        let onKeyDown = defaultArg onKeyDown ignore

        Html.div [
            prop.role "treeitem"
            prop.tabIndex (if isTabStop then 0 else -1)
            if canSelect then
                prop.custom ("aria-selected", isSelected)
            if not canSelect && not canExpand then
                prop.custom ("aria-disabled", true)
            prop.custom ("aria-level", depth + 1)
            prop.custom ("aria-posinset", posInSet)
            prop.custom ("aria-setsize", setSize)
            if canExpand then
                prop.custom ("aria-expanded", isExpanded)
            prop.custom ("data-tree-node-id", nodeId)
            prop.custom ("data-tree-node-kind", if TreeItem.isBranch node then "branch" else "leaf")
            prop.custom ("data-tree-active", isActive)
            prop.custom ("data-tree-focused", isFocused)
            if debug then
                prop.testId $"tree-node-{nodeId}"
            prop.className className
            prop.style [ style.paddingLeft (length.rem (float depth * 1.25)) ]
            prop.title (nodeProps.tooltip |> Option.defaultValue nodeProps.label)
            prop.onClick (fun event ->
                if not (originatesFromInteractiveDescendant event) then
                    onSelect event
            )
            prop.onFocus (fun _ -> onFocus ())
            prop.onKeyDown onKeyDown
            prop.children [
                TreeNode.Surface(
                    node,
                    depth,
                    isExpanded,
                    isSelected,
                    isActive,
                    isFocused,
                    isLoading,
                    error,
                    canExpand,
                    presentationRef,
                    onToggle,
                    onSelect
                )
            ]
        ]
