namespace Pages

open Model

open Feliz
open Feliz.DaisyUI

type ActivityLog =

    static member Main(model: Model) =
        Html.div [
            Daisy.table [
                prop.className "table-xs"
                prop.children [ Html.tbody (model.DevState.Log |> List.map LogItem.toTableRow) ]
            ]
        ]