module CustomComponents.WarningModal

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared
open CustomComponents

let warningModal (model:Model) dispatch =
    let msg = fun e -> model.WarningModal.Value.NextMsg |> dispatch
    let closeMsg = fun e -> UpdateWarningModal None |> dispatch
    let message = model.WarningModal.Value.ModalMessage
    Modal.modal [ Modal.IsActive true ] [
        Modal.background [
            Props [ OnClick closeMsg ]
        ] [ ]
        Notification.notification [
            Notification.Props [Style [MaxWidth "80%"; MaxHeight "80%"; OverflowX OverflowOptions.Auto ]]
        ] [
            Notification.delete [
                Props [OnClick closeMsg]
            ][]
            Field.div [][
                str message
            ]
            Field.div [][
                Button.a [
                    Button.Color IsWarning
                    Button.Props [Style [Float FloatOptions.Right]]
                    Button.OnClick msg
                ][
                    str "Continue"
                ]
            ]
        ]
    ]