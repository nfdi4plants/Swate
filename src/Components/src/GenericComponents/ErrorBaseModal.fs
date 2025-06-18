namespace Swate.Components

open Feliz
open Feliz.DaisyUI
open Fable.Core
open Fable.React
open Feliz
open Feliz.DaisyUI
open Swate.Components

//Modal that is the base of all error modals
[<Mangle(false); Erase>]
type ErrorBaseModal =

    ///<summary>This modal is used to display errors from for example api communication</summary>
    static member ErrorBaseModal
        (
            rmv,
            error: string,
            ?modalClassInfo: string,
            ?header: ReactElement,
            ?modalActions: ReactElement,
            ?footer: ReactElement,
            ?debug: string
        ) =

        Html.div [
            if debug.IsSome then
                prop.testId ("modal_" + debug.Value)
            prop.className "swt:modal swt:modal-open"
            prop.children [
                Html.div [
                    prop.className "swt:modal-backdrop"
                    prop.onClick rmv
                ]
                Html.div [
                    prop.className [
                        "swt:modal-box swt:!p-0"
                        if modalClassInfo.IsSome then
                            modalClassInfo.Value
                    ]
                    prop.style [
                        style.width (length.percent 90)
                        style.maxHeight (length.percent 80)
                        style.overflow.auto
                    ]
                    prop.children [
                        Html.div [
                            prop.className "swt:card-title"
                            prop.children [
                                if header.IsSome then
                                    header.Value
                            ]
                        ]

                        if modalActions.IsSome then
                            Html.div [ prop.children modalActions.Value ]

                        Html.div [
                            prop.className "swt:alert swt:alert-error swt:size-full"
                            prop.children [
                                Html.div [
                                    Components.CircularExitButton(props = [ prop.onClick rmv ])
                                ]
                                Html.span error

                                if footer.IsSome then
                                    Html.div [ prop.children footer.Value ]
                            ]
                        ]
                    ]
                ]
            ]
        ]