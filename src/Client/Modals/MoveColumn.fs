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
            prop.className "flex gap-4 justify-between"
            prop.children [
                Html.div [
                    Html.p "Preview"
                    Daisy.join [
                        Daisy.input [
                            join.item
                            prop.className "input-bordered"
                            prop.type'.number
                            prop.onChange (fun i -> setInput i)
                            prop.defaultValue input
                            prop.min 0
                            prop.max max
                        ]
                        Daisy.button.button [
                            join.item
                            prop.onClick (fun _ -> set (index, input))
                            prop.text "Preview"
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

        let content = [
            Daisy.table [
                Html.thead [ Html.tr [ Html.th "Index"; Html.th "Column" ] ]
                Html.tbody [
                    for i in 0 .. state.Length - 1 do
                        Html.tr [
                            if i = index then
                                prop.className "bg-error text-error-content"
                            prop.children [ Html.td i; Html.td (state.[i].ToString()) ]
                        ]
                ]
            ]
        ]

        let fooder submit input rmv =
            Html.div [
                prop.className "justify-end flex gap-2"
                prop.style [ style.marginLeft length.auto ]
                prop.children [
                    Daisy.button.button [ prop.onClick rmv; button.outline; prop.text "Cancel" ]
                    //Html.p "Update Table"
                    Daisy.button.a [ button.primary; prop.onClick (submit input); prop.text "Submit" ]
                ]
            ]

        Swate.Components.BaseModal.BaseModal(
            rmv,
            header = Html.p "Move Column",
            modalActivity = modalActivity,
            contentClassInfo = "overflow-y-auto max-w-[700px]",
            content = content,
            footer = fooder submit input rmv
        )

//Daisy.modal.div [
//    modal.active
//    prop.children [
//        Daisy.modalBackdrop [ prop.onClick rmv ]
//        Daisy.modalBox.div [
//            Daisy.card [
//                prop.style [style.maxHeight(length.percent 70); style.overflowY.hidden]
//                prop.children [
//                    Daisy.cardBody [
//                        Daisy.cardTitle [
//                            prop.className "flex flex-row justify-between"
//                            prop.children [
//                                Html.span "Move Column"
//                                Components.DeleteButton(props=[prop.onClick rmv])
//                            ]
//                        ]
//                        MoveColumn.InputField(index, updateIndex, state.Length-1, submit)
//                        Html.div [
//                            prop.className "overflow-y-auto max-w-[700px]"
//                            prop.children [
//                                Daisy.table [
//                                    Html.thead [
//                                        Html.tr [
//                                            Html.th "Index"
//                                            Html.th "Column"
//                                        ]
//                                    ]
//                                    Html.tbody [
//                                        for i in 0 .. state.Length-1 do
//                                            Html.tr [
//                                                if i = index then
//                                                    prop.className "bg-error text-error-content"
//                                                prop.children [
//                                                    Html.td i
//                                                    Html.td (state.[i].ToString())
//                                                ]
//                                            ]
//                                    ]
//                                ]
//                            ]
//                        ]
//                    ]
//                ]
//            ]
//        ]
//    ]
//]