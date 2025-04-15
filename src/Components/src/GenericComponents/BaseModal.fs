namespace Swate.Components

open Feliz
open Feliz.DaisyUI
open Fable.Core

//Modal that is the base of all big modals
[<Mangle(false); Erase>]
type BaseModal =

    [<ReactComponent(true)>]
    static member BaseModal (
        rmv,
        ?modalClassInfo: string,
        ?header: ReactElement,
        ?modalActions: ReactElement,
        ?content: ReactElement,
        ?contentClassInfo: string,
        ?footer: ReactElement,
        ?debug: string) =

        Daisy.modal.div [
            if debug.IsSome then
                prop.testId ("modal_" + debug.Value)
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
                        if modalActions.IsSome then
                            Html.div [
                                modalActions.Value
                            ]
                        // Scrollable content
                        if content.IsSome then
                            Html.div [
                                if debug.IsSome then
                                    prop.testId ("modal_content_" + debug.Value)
                                prop.className [
                                    if contentClassInfo.IsSome then
                                        contentClassInfo.Value
                                    else
                                        "overflow-y-auto space-y-2"
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
