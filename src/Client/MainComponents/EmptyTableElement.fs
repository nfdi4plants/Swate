namespace MainComponents

open Feliz
open Feliz.DaisyUI
open ARCtrl
open Swate.Components

type EmptyTableElement =

    static member private TableButton(icons: ReactElement, text: string, onclick) =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:items-center swt:gap-2"
            prop.children [
                Html.div [
                    prop.className "swt:btn swt:btn-primary swt:btn-square swt:place-self-end swt:gap-0 swt:min-h-[100px] swt:min-w-[100px]"
                    prop.onClick (fun _ -> onclick ())
                    prop.text text
                    prop.children icons
                ]
            ]
        ]

    static member Main(openBuildingBlockWidget: unit -> unit, openTemplateWidget: unit -> unit, openSelectedTableWidget: bool -> unit) =
        Html.div [
            prop.className "swt:flex swt:justify-center swt:h-full swt:items-center"
            prop.children [
                Html.div [
                    prop.className "swt:card swt:bg-base-300 swt:shadow-xl swt:min-h-[400px] swt:min-w-[400px]"
                    prop.children [
                        Html.div [
                            prop.className "swt:card-body swt:flex-1 swt:flex swt:flex-col swt:justify-center"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:flex swt:justify-center swt:font-bold swt:text-xl swt:mb-6";
                                    prop.text "New Table!"
                                ]
                                Html.div [
                                    prop.className "swt:grid swt:grid-cols-2 swt:grid-rows-2 swt:gap-6 swt:h-full swt:place-items-center"
                                    prop.children [
                                        EmptyTableElement.TableButton(
                                            React.fragment [
                                                Icons.Templates()
                                            ],
                                            "Start from existing template!",
                                            fun _ -> openTemplateWidget ()
                                        )
                                        EmptyTableElement.TableButton(
                                            Icons.BuildingBlock(),
                                            "Start from scratch!",
                                            fun _ -> openBuildingBlockWidget ()
                                        )
                                        EmptyTableElement.TableButton(
                                            Icons.BuildingBlock(),
                                            "Copy output from previous table!",
                                            fun _ -> openSelectedTableWidget true
                                        )
                                        EmptyTableElement.TableButton(
                                            Icons.BuildingBlock(),
                                            "Copy output from previous table!",
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