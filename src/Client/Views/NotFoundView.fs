module NotFoundView

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

let notFoundComponent (model:Model) (dispatch:Msg -> unit) =
    div [] [
        str "The requested url does not exist in context of this application. Please tell us how you got here so we can fix this together."
    ]