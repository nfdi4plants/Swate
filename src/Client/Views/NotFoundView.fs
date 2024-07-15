module NotFoundView

open Feliz
open Messages
open Model

let notFoundComponent (model:Model) (dispatch:Msg -> unit) =
    Html.div "The requested url does not exist in context of this application. Please tell us how you got here so we can fix this together."