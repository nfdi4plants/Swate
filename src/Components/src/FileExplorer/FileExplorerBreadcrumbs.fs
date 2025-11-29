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
            prop.className "swt:breadcrumbs swt:text-sm swt:mb-4"
            prop.children [
                Html.ul [
                    prop.children (
                        [
                            Html.li [
                                Html.a [
                                    prop.className "swt:link swt:link-hover"
                                    prop.text "Root"
                                    prop.onClick (fun _ -> onNavigate "")
                                ]
                            ]
                        ]
                        @ (path
                           |> List.map (fun item ->
                               Html.li [
                                   Html.a [
                                       prop.className "swt:link swt:link-hover"
                                       prop.text item.Name
                                       prop.onClick (fun _ -> onNavigate item.Id)
                                   ]
                               ]
                           ))
                    )
                ]
            ]
        ]