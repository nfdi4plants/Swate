namespace MainComponents

open Feliz
open Feliz.DaisyUI
open ARCtrl

type EmptyTableElement =

    static member private Button(icons: ReactElement, onclick) =
        Daisy.button.button [
            button.primary
            button.square
            prop.className "place-self-end gap-0"
            prop.onClick (fun _ -> onclick())
            prop.children icons
        ]

    static member Main(openBuildingBlockWidget: unit -> unit, openTemplateWidget: unit -> unit) =
        Html.div [
            prop.className "flex justify-center items-center"
            prop.style [style.height (length.perc 100)]
            prop.children [
                Daisy.card [
                    prop.className "bg-base-300 shadow-xl"
                    prop.children [
                        Daisy.cardBody [
                            Html.h3 [
                                prop.className "font-bold text-xl"
                                prop.text "New Table!"
                            ]
                            Html.div [
                                prop.className "grid grid-cols-[1fr,auto] gap-4 items-center"
                                prop.children [
                                    Html.text "Start from an existing template!"
                                    EmptyTableElement.Button(
                                        React.fragment [
                                            Html.i [prop.className "fa-solid fa-circle-plus" ]
                                            Html.i [prop.className "fa-solid fa-table" ]
                                        ],
                                        fun _ -> openTemplateWidget()
                                    )
                                    Html.text "Or start from scratch!"
                                    EmptyTableElement.Button(
                                        React.fragment [
                                            Html.i [prop.className "fa-solid fa-circle-plus" ]
                                            Html.i [prop.className "fa-solid fa-table-columns" ]
                                        ],
                                        fun _ -> openBuildingBlockWidget()
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]