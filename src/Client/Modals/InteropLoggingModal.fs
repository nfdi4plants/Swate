module Modals.InteropLoggingModal

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared

let interopLoggingModal(model:DevState, dispatch) (rmv: _ -> unit) =
    let closeMsg =
        fun e ->
            rmv e
            UpdateDisplayLogList [] |> DevMsg |> dispatch
    let logs = model.DisplayLogList
    Modal.modal [ Modal.IsActive true] [
        Modal.background [
            Props [ OnClick closeMsg ]
        ] [ ]
        Notification.notification [
            Notification.Props [Style [
                MaxWidth "80%"; MaxHeight "80%"; OverflowX OverflowOptions.Auto;
                //yield! colorControlInArray model.SiteStyleState.ColorMode
            ]]
        ] [
            Notification.delete [
                Props [OnClick closeMsg]
            ] []
            Field.div [] [
                Table.table [
                Table.IsFullWidth
                //Table.Props [ExcelColors.colorBackground model.SiteStyleState.ColorMode]
            ] [
                tbody [] (
                    logs
                    |> List.map LogItem.toTableRow
                )
            ]
            ]
            Field.div [] [
                Button.a [
                    Button.Color IsWarning
                    Button.Props [Style [Float FloatOptions.Right]]
                    Button.OnClick closeMsg
                ] [
                    str "Continue"
                ]
            ]
        ]
    ]