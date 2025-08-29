namespace MainComponents

open Feliz
open Feliz.DaisyUI
open EmptyTableModals
open Swate.Components
open Model
open ARCtrl

type EmptyTableElement =

    static member private TableButton(text: string, onclick) =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:items-center swt:gap-2"
            prop.children [
                Html.div [
                    prop.className "swt:btn swt:btn-neutral swt:btn-outline swt:place-self-end swt:gap-0 swt:w-full swt:min-h-[100px] swt:min-w-[100px] swt:max-w-[150px] swt:text-secondary"
                    prop.role "button"
                    prop.onClick (fun _ -> onclick ())
                    prop.text text
                ]
            ]
        ]

    static member CreateMinimalTable(model, dispatch) =
        let tables = model.SpreadsheetModel.ArcFile.Value.Tables().Tables |> Array.ofSeq
        let activeTableIndex =
            let activeTableName = model.SpreadsheetModel.ActiveTable.Name
            tables
            |> Array.findIndex (fun table -> table.Name = activeTableName)

        let activeTable = tables.[activeTableIndex]

        let newColumns =
            let inputColumn =
                let header = CompositeHeader.Input IOType.Sample
                CompositeColumn.create(header)
            let parameterColumn =
                let header = CompositeHeader.ProtocolREF
                CompositeColumn.create(header)
            let outPutColumn =
                let header = CompositeHeader.Output IOType.Data
                CompositeColumn.create(header)

            [| inputColumn; parameterColumn; outPutColumn |]

        activeTable.AddColumns(newColumns)

        Spreadsheet.UpdateTable(activeTable)
        |> Messages.SpreadsheetMsg
        |> dispatch

    [<ReactComponent>]
    static member Main(model:Model, dispatch) =

        let selectBuildingBlockIsOpen, setSelectBuildingBlockIsOpen = React.useState (false)
        let selectTemplatesIsOpen, setSelectTemplatesIsOpen = React.useState (false)
        let selectPreviousTableIsOpen, setSelectPreviousTableIsOpen = React.useState (false)

        Html.div [
            prop.className "swt:flex swt:justify-center swt:h-full swt:items-center"
            prop.children [
                Html.div [
                    prop.className "swt:card swt:bg-base-300 swt:shadow-xl swt:min-h-[400px] swt:min-w-[400px]"
                    prop.children [
                        EmptyTableModals.BuildingBlock(model, selectBuildingBlockIsOpen, setSelectBuildingBlockIsOpen, dispatch)
                        EmptyTableModals.Templates(model, selectTemplatesIsOpen, setSelectTemplatesIsOpen, dispatch)
                        EmptyTableModals.PreviousTableSelect(model, selectPreviousTableIsOpen, setSelectPreviousTableIsOpen, dispatch)
                        Html.div [
                            prop.className "swt:card-body swt:flex-1 swt:flex swt:flex-col swt:justify-center"
                            prop.children [
                                Html.h3 [
                                    prop.className "swt:flex swt:font-bold swt:text-xl swt:mb-6";
                                    prop.text "New Table!"
                                ]
                                Html.div [
                                    prop.className "swt:grid swt:grid-cols-2 swt:grid-rows-2 swt:gap-6 swt:h-full swt:place-items-center"
                                    prop.children [
                                        EmptyTableElement.TableButton(
                                            "Start from existing template!",
                                            fun _ -> setSelectTemplatesIsOpen true
                                        )
                                        EmptyTableElement.TableButton(
                                            "Start from scratch!",
                                            fun _ -> setSelectBuildingBlockIsOpen true
                                        )
                                        EmptyTableElement.TableButton(
                                            "Copy output from previous table!",
                                            fun _ -> setSelectPreviousTableIsOpen true
                                        )
                                        EmptyTableElement.TableButton(
                                            "Start with a minimal table!",
                                            fun _ -> EmptyTableElement.CreateMinimalTable(model, dispatch)
                                        )
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]