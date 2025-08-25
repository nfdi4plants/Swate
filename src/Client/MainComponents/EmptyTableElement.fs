namespace MainComponents

open Feliz
open Feliz.DaisyUI
open ARCtrl
open Swate.Components

type EmptyTableElement =

    static member private Button(icons: ReactElement, onclick) =
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
                Html.div [
                    prop.className "swt:card swt:bg-base-300 swt:shadow-xl"
                    prop.children [
                        Html.div [
                            prop.className "swt:card-body"
                            prop.children [
                                Html.h3 [ prop.className "swt:font-bold swt:text-xl"; prop.text "New Table!" ]
                                Html.div [
                                    prop.className "swt:grid swt:grid-cols-[auto_auto] swt:gap-4 swt:items-center"
                                    prop.children [
                                        Html.span "Start from an existing template!"
                                        EmptyTableElement.Button(
                                            React.fragment [
                                                Icons.Templates()
                                            ],
                                            fun _ -> openTemplateWidget ()
                                        )
                                        Html.span "Or start from scratch!"
                                        EmptyTableElement.Button(
                                            Icons.BuildingBlock(),
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