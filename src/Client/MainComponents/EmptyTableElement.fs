namespace MainComponents

open Feliz
open Feliz.Bulma
open ARCtrl

type EmptyTableElement =
    static member Main(openBuildingBlockWidget: unit -> unit, openTemplateWidget: unit -> unit) =
        Html.div [
            prop.className "is-flex is-justify-content-center is-align-items-center"
            prop.style [style.height (length.perc 100)]
            prop.children [
                Bulma.box [
                    Bulma.content [
                        prop.children [
                            Html.h3 [
                                prop.className "title"
                                prop.text "New Table!"
                            ]
                            Bulma.field.div [
                                prop.className "is-flex is-justify-content-space-between is-align-items-center gap-3"
                                prop.children [
                                    Html.text "Start from an existing template!"
                                    Bulma.button.span [
                                        prop.onClick (fun _ -> openTemplateWidget())
                                        prop.children [
                                            Bulma.icon [ 
                                                Html.i [prop.className "fa-solid fa-circle-plus" ]
                                                Html.i [prop.className "fa-solid fa-table" ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                            Bulma.field.div [
                                prop.className "is-flex is-justify-content-space-between is-align-items-center gap-3"
                                prop.children [
                                    Html.text "Or start from scratch!"
                                    Bulma.button.span [
                                        prop.onClick (fun _ -> openBuildingBlockWidget())
                                        prop.children [
                                            Bulma.icon [ 
                                                Html.i [prop.className "fa-solid fa-circle-plus" ]
                                                Html.i [prop.className "fa-solid fa-table-columns" ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]