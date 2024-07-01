module NotFoundView

open Fable.React
open Messages
open Model

let notFoundComponent (model:Model) (dispatch:Msg -> unit) =
    div [] [
        str "The requested url does not exist in context of this application. Please tell us how you got here so we can fix this together."
    ]