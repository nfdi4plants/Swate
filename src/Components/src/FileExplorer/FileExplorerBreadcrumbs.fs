namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz

open Swate.Components.FileExplorer.FileTreeDataStructures

// ============================================================================
// BREADCRUMBS COMPONENT
// ============================================================================
[<Mangle(false); Erase>]
type Breadcrumbs =
    [<ReactComponent>]
    static member Breadcrumbs(path: FileItem list, onNavigate: string -> unit) =
        Html.div [
            prop.className "swt:breadcrumbs swt:text-sm swt:mb-4 swt:px-2"
            prop.children [
                Html.ul [
                    prop.className "swt:flex swt:items-center swt:gap-2"
                    prop.children (
                        [
                            Html.li [
                                prop.className "swt:flex swt:items-center"
                                prop.children [
                                    Html.a [
                                        prop.className "swt:link swt:link-hover swt:flex swt:items-center swt:gap-1"
                                        prop.onClick (fun _ -> onNavigate "")
                                        prop.children [
                                            // Home icon
                                            Svg.svg [
                                                svg.xmlns "http://www.w3.org/2000/svg"
                                                svg.fill "none"
                                                svg.viewBox (0, 0, 24, 24)
                                                svg.stroke "currentColor"
                                                svg.strokeWidth 1.5
                                                svg.className "swt:h-4 swt:w-4"
                                                svg.children [
                                                    Svg.path [
                                                        svg.custom ("strokeLinecap", "round")
                                                        svg.custom ("strokeLinejoin", "round")
                                                        svg.d
                                                            "M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25"
                                                    ]
                                                ]
                                            ]
                                            Html.span "Root"
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        @ (path
                           |> List.map (fun item ->
                               Html.li [
                                   prop.className "swt:flex swt:items-center"
                                   prop.children [
                                       // Separator
                                       Html.span [ prop.className "swt:mx-2 swt:text-gray-400"; prop.text "/" ]
                                       Html.a [
                                           prop.className "swt:link swt:link-hover"
                                           prop.text item.Name
                                           prop.onClick (fun _ -> onNavigate item.Id)
                                       ]
                                   ]
                               ]
                           ))
                    )
                ]
            ]
        ]

    [<ReactComponent>]
    static member CompactBreadcrumbs(path: FileItem list, onNavigate: string -> unit, ?maxItems: int) =
        let maxItems = defaultArg maxItems 3

        let displayPath =
            if path.Length <= maxItems then
                path
            else
                // Show first item, ellipsis, and last (maxItems - 1) items
                let ellipsisItem = {
                    Id = "ellipsis"
                    Name = "..."
                    IconPath = "more_horiz"
                    IsExpanded = false
                    Children = None
                    IdRel = None
                    IsDirectory = false
                    IsLFS = None
                    IsLFSPointer = None
                    Checkout = None
                    Downloaded = None
                    Size = None
                    SizeFormatted = None
                    ItemType = "ellipsis"
                    Label = Some "..."
                    Selectable = false
                }

                [ path.[0] ]
                @ [ ellipsisItem ]
                @ (path |> List.skip (path.Length - maxItems + 1))

        Html.div [
            prop.className "swt:breadcrumbs swt:text-sm swt:mb-4 swt:px-2"
            prop.children [
                Html.ul [
                    prop.className "swt:flex swt:items-center swt:gap-1"
                    prop.children (
                        Html.li [
                            Html.a [
                                prop.className "swt:link swt:link-hover swt:text-xs"
                                prop.text "Root"
                                prop.onClick (fun _ -> onNavigate "")
                            ]
                        ]
                        :: (displayPath
                            |> List.map (fun item ->
                                Html.li [
                                    prop.className "swt:flex swt:items-center"
                                    prop.children [
                                        Html.span [ prop.className "swt:mx-1 swt:text-gray-400"; prop.text "/" ]
                                        if item.ItemType = "ellipsis" then
                                            Html.span [ prop.className "swt:text-gray-500"; prop.text "..." ]
                                        else
                                            Html.a [
                                                prop.className "swt:link swt:link-hover swt:text-xs"
                                                prop.text item.Name
                                                prop.onClick (fun _ -> onNavigate item.Id)
                                            ]
                                    ]
                                ]
                            ))
                    )
                ]
            ]
        ]