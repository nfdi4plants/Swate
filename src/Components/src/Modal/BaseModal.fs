namespace Swate.Components

open Feliz
open Feliz.DaisyUI
open Swate.Components
open Fable.Core

//Modal that is the base of all big modals
[<Mangle(false); Erase>]
type BaseModal =

    [<ReactComponent(true)>]
    static member BaseModal (
        rmv,
        ?modalClassInfo: string,
        ?header: ReactElement,
        ?modalActivity: ReactElement,
        ?content: ReactElement seq,
        ?contentClassInfo: string,
        ?footer: ReactElement) =
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [ prop.onClick rmv ]
                Daisy.modalBox.div [
                    prop.className [
                        "w-4/5 flex flex-col gap-2"
                        if modalClassInfo.IsSome then
                            modalClassInfo.Value
                    ]
                    prop.children [
                        //Header
                        Daisy.cardTitle [
                            prop.children [
                                if header.IsSome then
                                    header.Value
                                Components.DeleteButton(props=[
                                    prop.className "ml-auto"
                                    prop.onClick rmv
                                ])
                            ]
                        ]
                        // Modal specific action
                        if modalActivity.IsSome then
                            Html.div [
                                modalActivity.Value
                            ]
                        // Scrollable content
                        if content.IsSome then
                            Html.div [
                                prop.testId "modal-content"
                                prop.className [
                                    "overflow-y-auto space-y-2"
                                    if contentClassInfo.IsSome then
                                        contentClassInfo.Value
                                ]
                                prop.children content.Value
                            ]
                        // Footer
                        if footer.IsSome then
                            Daisy.cardActions [
                                footer.Value
                            ]
                    ]
                ]
            ]
        ]
