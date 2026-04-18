namespace Swate.Components.Template

open Fable.Core
open Feliz
open Swate.Components

[<Erase; Mangle(false)>]
type TemplateToolbar =

    [<ReactComponent>]
    static member TemplateToolbar
        (selectedCount: int, canImport: bool, isRefreshing: bool, onRefresh: unit -> unit, onImport: unit -> unit)
        =
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
                    prop.children [
                        if isRefreshing then
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-xs"
                            ]
                        else
                            Icons.ArrowsRotate()
                    ]
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