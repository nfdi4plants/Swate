module Modals.WarningModal

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared

let warningModal (warning:{|NextMsg:Msg; ModalMessage: string|}, model, dispatch) (rmv: _ -> unit) =
    let msg = fun _ -> warning.NextMsg |> dispatch
    let closeMsg = rmv
    let message = warning.ModalMessage
    Modal.modal [ Modal.IsActive true] [
        Modal.background [
            Props [ OnClick closeMsg ]
        ] [ ]
        Notification.notification [
            Notification.Props [Style [MaxWidth "80%"; MaxHeight "80%"; OverflowX OverflowOptions.Auto; yield! colorControlInArray model.SiteStyleState.ColorMode ]]
        ] [
            Notification.delete [
                Props [OnClick closeMsg]
            ] []
            Field.div [] [
                str message
            ]
            Field.div [] [
                Button.a [
                    Button.Color IsWarning
                    Button.Props [Style [Float FloatOptions.Right]]
                    Button.OnClick (fun e ->
                        closeMsg e
                        msg e
                    )
                ] [
                    str "Continue"
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