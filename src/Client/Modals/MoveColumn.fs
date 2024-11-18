namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared

open ARCtrl
open Components

type MoveColumn =

    [<ReactComponent>]
    static member InputField(index: int, set, max: int, submit) =
        let input, setInput = React.useState(index)
        Html.div [
            prop.className "flex gap-4 justify-between"
            prop.children [
                Html.div [
                    Html.p "Preview"
                    Daisy.join [
                        Daisy.input [
                            join.item
                            prop.className "input-bordered"
                            prop.type'.number
                            prop.onChange(fun i -> setInput i)
                            prop.defaultValue input
                            prop.min 0
                            prop.max max
                        ]
                        Daisy.button.button [
                            join.item
                            prop.onClick(fun _ -> set (index,input))
                            prop.text "Apply"
                        ]
                    ]
                ]
                Html.div [
                    Html.p "Update Table"
                    Daisy.button.a [
                        button.info
                        prop.onClick (submit input)
                        prop.text "Submit"
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main (columnIndex: int, model: Model, dispatch) (rmv: _ -> unit) =
        let table = model.SpreadsheetModel.ActiveTable
        let state, setState = React.useState(Array.ofSeq table.Headers)
        let index, setIndex = React.useState(columnIndex)
        let updateIndex(current, next) =
            setIndex next
            let nextState = ResizeArray(state)
            Helper.arrayMoveColumn current next nextState
            setState (Array.ofSeq nextState)
        let submit = fun i e ->
            Spreadsheet.MoveColumn(columnIndex, i) |> SpreadsheetMsg |> dispatch
            rmv e
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [ prop.onClick rmv ]
                Daisy.modalBox.div [
                    Daisy.card [
                        prop.style [style.maxHeight(length.percent 70); style.overflowY.hidden]
                        prop.children [
                            Daisy.cardBody [
                                Daisy.cardTitle [
                                    prop.className "flex flex-row justify-between"
                                    prop.children [
                                        Html.h2 "Move Column"
                                        Components.DeleteButton(props=[prop.onClick rmv])
                                    ]
                                ]
                                MoveColumn.InputField(index, updateIndex, state.Length-1, submit)
                                Html.div [
                                    prop.className "overflow-y-auto max-w-[700px]"
                                    prop.children [
                                        Daisy.table [
                                            Html.thead [
                                                Html.tr [
                                                    Html.th "Index"
                                                    Html.th "Column"
                                                ]
                                            ]
                                            Html.tbody [
                                                for i in 0 .. state.Length-1 do
                                                    Html.tr [
                                                        if i = index then
                                                            prop.className "bg-error text-error-content"
                                                        prop.children [
                                                            Html.td i
                                                            Html.td (state.[i].ToString())
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
            ]
        ]