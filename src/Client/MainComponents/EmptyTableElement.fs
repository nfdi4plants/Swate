namespace MainComponents

open Feliz
open Feliz.DaisyUI
open ARCtrl

type EmptyTableElement =

    static member private Button(icons: ReactElement, onclick) =
        //Daisy.button.button [
        Html.div [
            prop.className "swt:btn swt:btn-primary swt:btn-square swt:place-self-end swt:gap-0"
            prop.onClick (fun _ -> onclick ())
            prop.children icons
        ]

    static member Main(openBuildingBlockWidget: unit -> unit, openTemplateWidget: unit -> unit) =
        Html.div [
            prop.className "swt:flex swt:justify-center swt:items-center"
            prop.style [ style.height (length.perc 100) ]
            prop.children [
                //Daisy.card [
                Html.div [
                    prop.className "swt:card swt:bg-base-300 swt:shadow-xl"
                    prop.children [
                        //Daisy.cardBody [
                        Html.div [
                            prop.className "swt:card-body"
                            prop.children [
                                Html.h3 [ prop.className "swt:font-bold swt:text-xl"; prop.text "New Table!" ]
                                Html.div [
                                    prop.className "swt:grid swt:grid-cols-[1fr,auto] swt:gap-4 swt:items-center"
                                    prop.children [
                                        Html.text "Start from an existing template!"
                                        EmptyTableElement.Button(
                                            React.fragment [
                                                Html.i [ prop.className "fa-solid fa-circle-plus" ]
                                                Html.i [ prop.className "fa-solid fa-table" ]
                                            ],
                                            fun _ -> openTemplateWidget ()
                                        )
                                        Html.text "Or start from scratch!"
                                        EmptyTableElement.Button(
                                            React.fragment [
                                                Html.i [ prop.className "fa-solid fa-circle-plus" ]
                                                Html.i [ prop.className "fa-solid fa-table-columns" ]
                                            ],
                                            fun _ -> openBuildingBlockWidget ()
                                        )
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]