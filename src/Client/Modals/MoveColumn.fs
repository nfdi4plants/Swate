namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Swate.Components.Shared

open ARCtrl
open Swate.Components

type MoveColumn =

    [<ReactComponent>]
    static member InputField(index: int, set, max: int, input: int, setInput: int -> unit) =
        Html.div [
            prop.className "swt:flex gswt:ap-4 swt:justify-between"
            prop.children [
                Html.div [
                    Html.p "Preview"
                    Html.div [
                        prop.className "swt:join"
                        prop.children [
                            Html.input [
                                prop.className "swt:input swt:join-item"
                                prop.type'.number
                                prop.onChange (fun i -> setInput i)
                                prop.defaultValue input
                                prop.min 0
                                prop.max max
                            ]
                            Html.button [
                                prop.className "swt:btn swt:join-item"
                                prop.text "Preview"
                                prop.onClick (fun _ -> set (index, input))
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(columnIndex: int, model: Model, dispatch) =
        let table = model.SpreadsheetModel.ActiveTable
        let state, setState = React.useState (Array.ofSeq table.Headers)
        let index, setIndex = React.useState (columnIndex)
        let input, setInput = React.useState (index)
        let isOpen, setIsOpen = React.useState (true)

        let rmv = Util.RMV_MODAL dispatch

        let updateIndex (current, next) =
            setIndex next
            let nextState = ResizeArray(state)
            Helper.arrayMoveColumn current next nextState
            setState (Array.ofSeq nextState)

        let submit =
            fun i e ->
                Spreadsheet.MoveColumn(columnIndex, i) |> SpreadsheetMsg |> dispatch
                rmv e

        let modalActivity =
            Html.div [
                prop.children [ MoveColumn.InputField(index, updateIndex, state.Length - 1, input, setInput) ]
            ]

        let content =
            React.fragment [
                Html.table [
                    prop.className "swt:table"
                    prop.children [
                        Html.thead [ Html.tr [ Html.th "Index"; Html.th "Column" ] ]
                        Html.tbody [
                            for i in 0 .. state.Length - 1 do
                                Html.tr [
                                    if i = index then
                                        prop.className "swt:bg-error swt:text-error-content"
                                    prop.children [ Html.td i; Html.td (state.[i].ToString()) ]
                                ]
                        ]
                    ]
                ]
            ]

        let fooder submit input rmv =
            Html.div [
                prop.className "swt:justify-end swt:flex swt:gap-2"
                prop.style [ style.marginLeft length.auto ]
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-outline"
                        prop.text "Cancel"
                        prop.onClick rmv
                    ]
                    Html.a [
                        prop.className "swt:btn swt:btn-primary"
                        prop.text "Submit"
                        prop.onClick (submit input)
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.p "Move Column",
            content,
            modalActions = modalActivity,
            footer = fooder submit input rmv
        )
