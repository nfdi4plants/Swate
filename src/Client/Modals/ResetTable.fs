namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Swate.Components.Shared
open Swate.Components

type ResetTable =

    [<ReactComponent>]
    static member Main(isOpen, setIsOpen, dispatch) =

        let rmv = fun _ -> setIsOpen false

        let reset =
            fun e ->
                Spreadsheet.Reset |> SpreadsheetMsg |> dispatch
                rmv e

        Swate.Components.BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Attention!",
            children =
                (React.fragment [
                    Html.div [
                        prop.className "swt:prose"
                        prop.children [
                            Html.p [
                                prop.innerHtml
                                    "Careful, this will delete <b>all</b> tables and <b>all</b> table history!"
                            ]
                            Html.p [
                                prop.innerHtml "There is no option to recover any information deleted in this way."
                            ]
                            Html.p [
                                prop.innerHtml
                                    "If you only want to delete one sheet, right-click the sheet at the bottom and select `delete`"
                            ]
                        ]
                    ]
                ]),
            footer =
                React.fragment [
                    Html.button [ prop.className "swt:btn swt:btn-info"; prop.text "Back"; prop.onClick rmv ]
                    //Daisy.button.a [ prop.onClick reset; button.error; prop.text "Delete" ]
                    Html.button [
                        prop.className "swt:btn swt:btn-error"
                        prop.text "Delete"
                        prop.onClick reset
                    ]
                ]
        )

// //Daisy.modal.div [
// Html.div [
//     prop.className "swt:modal swt:modal-open"
//     prop.children [
//         //Daisy.modalBackdrop [ prop.onClick rmv ]
//         Html.div [ prop.className "swt:modal-backdrop"; prop.onClick rmv ]
//         //Daisy.modalBox.div [
//         Html.div [
//             prop.className "swt:modal-box swt:card"
//             prop.children [
//                 //Daisy.cardTitle [
//                 Html.div [
//                     prop.className "swt:card-title swt:flex swt:flex-row swt:justify-between"
//                     prop.children [
//                         Html.span [ prop.className "swt:text-xl"; prop.text "Attention!" ]
//                         Components.DeleteButton(props = [ prop.onClick rmv ])
//                     ]
//                 ]
//                 Html.div [
//                     Html.p [
//                         prop.innerHtml
//                             "Careful, this will delete <b>all</b> tables and <b>all</b> table history!"
//                     ]
//                     Html.p [
//                         prop.innerHtml "There is no option to recover any information deleted in this way."
//                     ]
//                     Html.p [
//                         prop.innerHtml
//                             "If you only want to delete one sheet, right-click the sheet at the bottom and select `delete`"
//                     ]
//                 ]
//                 //Daisy.cardActions [
//                 Html.div [
//                     prop.className "swt:card-actions swt:justify-end"
//                     prop.children [
//                         //Daisy.button.a [ prop.onClick rmv; button.info; prop.text "Back" ]
//                         Html.button [
//                             prop.className "swt:btn swt:btn-info"
//                             prop.text "Back"
//                             prop.onClick rmv
//                         ]
//                         //Daisy.button.a [ prop.onClick reset; button.error; prop.text "Delete" ]
//                         Html.button [
//                             prop.className "swt:btn swt:btn-error"
//                             prop.text "Delete"
//                             prop.onClick reset
//                         ]
//                     ]
//                 ]
//             ]
//         ]
//     ]
// ]