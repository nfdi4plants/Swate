namespace Modals

open Feliz
open Feliz.Bulma
open Model
open Messages
open Shared

open ARCtrl

type SelectiveImportModal =

    [<ReactComponent>]
    static member Main (import: ArcFiles) (rmv: _ -> unit) =
        Bulma.modal [
            Bulma.modal.isActive
            prop.children [
                Bulma.modalBackground [ prop.onClick rmv ]
                Bulma.modalCard [
                    prop.style [style.maxHeight(length.percent 70); style.overflowY.hidden]
                    prop.children [
                        Bulma.modalCardHead [
                            Bulma.modalCardTitle "Import"
                            Bulma.delete [ prop.onClick rmv ]
                        ]
                        Bulma.modalCardBody [ 
                            Html.div "Placeholder1"
                        ]    
                        Bulma.modalCardFoot [
                            Bulma.button.button [
                                color.isInfo
                                prop.style [style.marginLeft length.auto]
                                prop.text "Submit"
                                prop.onClick(fun e ->
                                    rmv e
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]
