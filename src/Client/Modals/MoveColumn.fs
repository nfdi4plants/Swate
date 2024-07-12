namespace Modals

open Feliz
open Feliz.Bulma
open Model
open Messages
open Shared

open ARCtrl

type MoveColumn =

    [<ReactComponent>]
    static member InputField(index: int, set, max: int, submit) =
        let input, setInput = React.useState(index)
        Bulma.field.div [
            prop.className "is-grouped is-justify-content-space-between"
            prop.style [style.gap (length.rem 1)]
            prop.children [
                Bulma.field.div [
                    Bulma.label "Preview"
                    Bulma.field.div [
                        Bulma.field.hasAddons
                        prop.children [
                            Bulma.control.div [
                                Bulma.control.isExpanded
                                prop.children [
                                    Bulma.input.number [
                                        prop.onChange(fun i -> setInput i)
                                        prop.defaultValue input
                                        prop.min 0
                                        prop.max max
                                    ]
                                ]
                            ]
                            Bulma.control.div [
                                Bulma.button.button [
                                    prop.onClick(fun _ -> set (index,input))
                                    prop.text "Apply"
                                ]
                            ]
                        ]
                    ]
                ]
                Bulma.field.div [ 
                    Bulma.label "Update Table"
                    Bulma.button.a [
                        Bulma.color.isInfo
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
        Bulma.modal [
            Bulma.modal.isActive
            prop.children [
                Bulma.modalBackground [ prop.onClick rmv ]
                Bulma.modalCard [
                    prop.style [style.maxHeight(length.percent 70); style.overflowY.hidden]
                    prop.children [
                        Bulma.modalCardHead [
                            Bulma.modalCardTitle "Move Column"
                            Bulma.delete [ prop.onClick rmv ]
                        ]
                        Bulma.modalCardBody [
                            MoveColumn.InputField(index, updateIndex, state.Length-1, submit)
                            Bulma.tableContainer [
                                prop.style [style.maxHeight 400; style.overflowY.auto]
                                prop.children [
                                    Bulma.table [
                                        Bulma.table.isFullWidth
                                        prop.children [
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
                                                            Bulma.color.hasBackgroundDanger; 
                                                            prop.className "has-background-danger"
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