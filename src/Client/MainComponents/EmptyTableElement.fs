namespace MainComponents

open Feliz
open Feliz.DaisyUI
open EmptyTableModals
open Swate.Components
open Model
open ARCtrl

module private EmptyTableElementHelpers =
    [<RequireQualifiedAccess>]
    type Modals =
        | BuildingBlock
        | Templates
        | PreviousTableSelect

open EmptyTableElementHelpers

type EmptyTableElement =

    static member private TableButton
        (icon: ReactElement, header: string, description: string, onclick, ?disabled: bool)
        =
        let disabled = defaultArg disabled false

        Html.div [
            prop.className [
                "swt:btn swt:text-left
                swt:w-full swt:min-h-[120px]
                swt:min-w-[120px] swt:max-w-[230px] swt:flex-col
                swt:gap-2 swt:border swt:border-base-content"
                if disabled then
                    "swt:btn-disabled swt:opacity-50"
            ]
            prop.role.button
            prop.tabIndex 0
            prop.onClick (fun _ ->
                if not disabled then
                    onclick ()
            )
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-row swt:gap-2 swt:font-bold swt:items-center swt:w-full"
                    prop.children [
                        Html.div [
                            prop.className
                                "swt:p-1 swt:border swt:border-primary swt:rounded swt:bg-primary/50 swt:text-primary-content/80 swt:h-fit"
                            prop.children [ icon ]
                        ]
                        Html.span header
                    ]
                ]
                Html.div [
                    prop.className "swt:text-xs swt:text-base-content/50 swt:text-left"
                    prop.text description
                ]
            ]
        ]

    static member CreateMinimalTable(model, dispatch) =
        let tables = model.SpreadsheetModel.ArcFile.Value.Tables().Tables |> Array.ofSeq

        let activeTableIndex =
            let activeTableName = model.SpreadsheetModel.ActiveTable.Name
            tables |> Array.findIndex (fun table -> table.Name = activeTableName)

        let activeTable = tables.[activeTableIndex]

        let newColumns =
            let inputColumn =
                let header = CompositeHeader.Input IOType.Sample
                CompositeColumn.create (header)

            let parameterColumn =
                let header = CompositeHeader.ProtocolREF
                CompositeColumn.create (header)

            let outPutColumn =
                let header = CompositeHeader.Output IOType.Data
                CompositeColumn.create (header)

            [| inputColumn; parameterColumn; outPutColumn |]

        activeTable.AddColumns(newColumns)
        activeTable.AddRowsEmpty(3)

        Spreadsheet.UpdateTable(activeTable) |> Messages.SpreadsheetMsg |> dispatch

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =

        let modal, setModal = React.useState (None: Modals option)

        React.fragment [
            EmptyTableModals.BuildingBlock(
                model,
                isOpen = (modal = Some Modals.BuildingBlock),
                setIsOpen =
                    (function
                    | true -> setModal (Some Modals.BuildingBlock)
                    | false -> setModal None),
                dispatch = dispatch
            )
            EmptyTableModals.Templates(
                model,
                isOpen = (modal = Some Modals.Templates),
                setIsOpen =
                    (function
                    | true -> setModal (Some Modals.Templates)
                    | false -> setModal None),
                dispatch = dispatch
            )
            EmptyTableModals.PreviousTableSelect(
                model,
                isOpen = (modal = Some Modals.PreviousTableSelect),
                setIsOpen =
                    (function
                    | true -> setModal (Some Modals.PreviousTableSelect)
                    | false -> setModal None),
                dispatch = dispatch
            )
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
                                        prop.className "swt:flex swt:font-bold swt:text-xl swt:mb-6"
                                        prop.text "New Table!"
                                    ]
                                    Html.div [
                                        prop.className
                                            "swt:grid swt:grid-cols-2 swt:grid-rows-2 swt:gap-6 swt:h-full swt:place-items-center"
                                        prop.children [
                                            EmptyTableElement.TableButton(
                                                Icons.Templates(),
                                                "Start with template!",
                                                "Select a full template as a starting point.",
                                                fun _ -> Modals.Templates |> Some |> setModal
                                            )
                                            EmptyTableElement.TableButton(
                                                Icons.BuildingBlock(),
                                                "Start from scratch!",
                                                "Select a building block as a starting point.",
                                                fun _ -> Modals.BuildingBlock |> Some |> setModal
                                            )
                                            EmptyTableElement.TableButton(
                                                Icons.BasicTable(),
                                                "Create basic table!",
                                                "Create a table with columns: Input, Protocol, Output.",
                                                fun _ -> EmptyTableElement.CreateMinimalTable(model, dispatch)
                                            )
                                            EmptyTableElement.TableButton(
                                                Icons.OutputColumn(),
                                                "Utilize prior output!",
                                                "Select an output column of one table as new input column.",
                                                (fun _ -> Modals.PreviousTableSelect |> Some |> setModal),
                                                (model.SpreadsheetModel.Tables.TableCount <= 1)
                                            )
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]