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
            prop.className "is-grouped is-justify-content-space-between"
            prop.style [style.gap (length.rem 1)]
            prop.children [
                Html.div [
                    Html.p "Preview"
                    Html.div [
                        Daisy.input [
                            prop.type'.number
                            prop.onChange(fun i -> setInput i)
                            prop.defaultValue input
                            prop.min 0
                            prop.max max
                        ]
                        Daisy.button.button [
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
                Daisy.card [
                    prop.style [style.maxHeight(length.percent 70); style.overflowY.hidden]
                    prop.children [
                        Daisy.cardBody [
                            Daisy.cardActions [
                                prop.className "justify-end"
                                prop.children [
                                    Components.DeleteButton(props=[prop.onClick rmv])
                                ]
                            ]
                            Daisy.cardTitle "Move Column"
                            MoveColumn.InputField(index, updateIndex, state.Length-1, submit)
                            Html.div [
                                prop.className "overflow-y-auto max-w-md"
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
                                                        prop.className "bg-error"
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