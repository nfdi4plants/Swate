module FilePickerView

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Thoth.Json
open Thoth.Elmish
open ExcelColors
open Api
open Model
open Messages
open Update
open Shared

let filePickerComponent (model:Model) (dispatch:Msg -> unit) =
    div [] [
        str "TEST"
    ]