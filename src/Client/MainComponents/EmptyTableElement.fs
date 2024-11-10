namespace MainComponents

open Feliz
open Feliz.DaisyUI
open ARCtrl

type EmptyTableElement =
    static member Main(openBuildingBlockWidget: unit -> unit, openTemplateWidget: unit -> unit) =
        Html.div [
            prop.className "is-flex is-justify-content-center is-align-items-center"
            prop.style [style.height (length.perc 100)]
            prop.children [
                Html.div [
                    prop.className "border border-base-100 rounded p-5 shadow prose"
                    prop.children [
                        Html.h3 [
                            prop.className "title"
                            prop.text "New Table!"
                        ]
                        Html.div [
                            prop.className "is-flex is-justify-content-space-between is-align-items-center gap-3"
                            prop.children [
                                Html.text "Start from an existing template!"
                                Daisy.button.button [
                                    prop.onClick (fun _ -> openTemplateWidget())
                                    prop.children [
                                        Html.i [prop.className "fa-solid fa-circle-plus" ]
                                        Html.i [prop.className "fa-solid fa-table" ]
                                    ]
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "is-flex is-justify-content-space-between is-align-items-center gap-3"
                            prop.children [
                                Html.text "Or start from scratch!"
                                Daisy.button.button [
                                    prop.onClick (fun _ -> openBuildingBlockWidget())
                                    prop.children [
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