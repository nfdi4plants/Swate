namespace Swate.Components.Template

open Fable.Core
open Feliz
open Swate.Components

[<Erase; Mangle(false)>]
type TemplateToolbar =

    [<ReactComponent>]
    static member TemplateToolbar
        (selectedCount: int, isRefreshing: bool, onRefresh: unit -> unit, onImport: unit -> unit)
        =

        let canImport = selectedCount > 0

        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:items-end"
            prop.children [
                Html.h3 [
                    prop.className "swt:text-xl swt:font-bold"
                    prop.text "Add Template"
                ]
                Html.div [
                    prop.className "swt:text-xs swt:opacity-70 swt:ml-auto"
                    prop.textf "%d selected" selectedCount
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-sm swt:btn-square"
                    prop.title "Refresh templates"
                    prop.disabled isRefreshing
                    prop.onClick (fun _ -> onRefresh ())
                    prop.children [ Icons.ArrowsRotate() ]
                ]
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm swt:w-full"
                        if canImport then "swt:btn-primary" else "swt:btn-disabled"
                    ]
                    prop.disabled (not canImport)
                    prop.onClick (fun _ -> onImport ())
                    prop.text "Import"
                ]
            ]
        ]