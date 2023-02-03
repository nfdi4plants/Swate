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