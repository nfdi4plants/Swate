module CustomComponents.ErrorModal

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

let errorModal (model:Model) dispatch =
    let closeMsg = (fun e -> UpdateLastFullError None |> DevMsg |> dispatch) 
    Modal.modal [ Modal.IsActive true ] [
        Modal.background [
            Props [ OnClick closeMsg ]
        ] [ ]
        Notification.notification [
            Notification.Color IsDanger
            Notification.Props [Style [MaxWidth "80%"; MaxHeight "80%"; OverflowX OverflowOptions.Auto (*CSSProp.Custom ("overflow", "scroll")*)]]
        ] [
            Notification.delete [Props [OnClick closeMsg]][]
            str (model.DevState.LastFullError.Value.GetPropagatedError())
        ]
    ]