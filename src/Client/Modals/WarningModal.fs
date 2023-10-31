module Modals.WarningModal

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Shared
open Feliz
open Feliz.Bulma


let warningModal (warning:{|NextMsg:Msg; ModalMessage: string|}, model, dispatch) (rmv: _ -> unit) =
    let msg = fun _ -> warning.NextMsg |> dispatch
    let closeMsg = rmv
    let message = warning.ModalMessage

    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [prop.onClick closeMsg]
            Bulma.notification [
                prop.style [style.width(length.percent 80); style.maxHeight (length.percent 80); style.overflowX.auto]
                prop.children [
                    Bulma.delete [prop.onClick closeMsg]
                    Bulma.field.div [
                        prop.text message
                    ]
                    Bulma.field.div [
                        Bulma.button.a [
                            Bulma.color.isWarning
                            prop.style [style.float'.right]
                            prop.onClick (fun e ->
                                closeMsg e
                                msg e
                            )
                            prop.text "Continue"
                        ]
                    ]
                ]
            ]
        ]
    ]

open Feliz
open Feliz.Bulma
open ExcelColors

let warningModalSimple (warning: string) (rmv: _ -> unit) =
    let closeMsg = rmv
    Bulma.modal [
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [ prop.onClick rmv ]
            Bulma.notification [
                prop.style [style.maxWidth (length.percent 80); style.maxHeight (length.percent 80); style.overflowX.auto; ]
                prop.children [
                    Bulma.delete [
                        prop.onClick closeMsg
                    ]
                    Bulma.field.div warning
                    Bulma.field.div [
                        Bulma.button.a [
                            Bulma.color.isWarning
                            prop.style [style.float'.right]
                            prop.onClick (fun e ->
                                closeMsg e
                            )
                            prop.text "Continue"
                        ] 
                    ]
                ]
            ]
        ]
    ]