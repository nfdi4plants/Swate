namespace Modals

open Feliz
open Feliz.DaisyUI
open ARCtrl
open FileImport
open Swate.Components

type BaseModal =

    [<ReactComponent>]
    static member Main (rmv, ?modalClassInfo: string, ?header: string,  ?modalActivity: ReactElement, ?content: ReactElement seq, ?contentClassInfo: string, ?fooder: ReactElement) =
        let contentClassInfo = defaultArg contentClassInfo "overflow-y-auto space-y-2"
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [ prop.onClick rmv ]
                Daisy.modalBox.div [
                    prop.className "w-4/5 flex flex-col gap-2"
                    if modalClassInfo.IsSome then
                        prop.className modalClassInfo.Value
                    prop.children [
                        //Header
                        Daisy.cardTitle [
                            prop.className "justify-between"
                            prop.children [
                                if header.IsSome then
                                    Html.p header.Value
                                Components.DeleteButton(props=[prop.onClick rmv])
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
                                prop.className contentClassInfo
                                prop.children content.Value
                            ]
                        // Fooder
                        if content.IsSome then
                            Daisy.cardActions [
                                fooder.Value
                            ]
                    ]
                ]
            ]
        ]
