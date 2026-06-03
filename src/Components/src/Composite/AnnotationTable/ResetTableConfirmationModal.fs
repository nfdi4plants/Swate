namespace Swate.Components.Composite.AnnotationTable

open Feliz
open Fable.Core
open Swate.Components.Primitive.BaseModal

[<Erase; Mangle(false)>]
type ResetTableConfirmationModal =

    [<ReactComponent>]
    static member ResetTableConfirmationModal
        (
            isOpen: bool,
            setIsOpen: bool -> unit,
            onDelete: unit -> unit
        ) =
        let close () = setIsOpen false

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Attention!",
            children =
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
                ],
            footer =
                React.Fragment [
                    Html.button [
                        prop.className "swt:btn swt:btn-info"
                        prop.text "Back"
                        prop.onClick (fun _ -> close ())
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-error"
                        prop.text "Delete"
                        prop.onClick (fun _ ->
                            onDelete ()
                            close ()
                        )
                    ]
                ]
        )
