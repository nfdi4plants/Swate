namespace Pages

open Model

open Feliz
open Feliz.DaisyUI

[<AutoOpen>]
module rec PersistentStorage =

    type ActivityLog =

        static member Main (model: Model) =
            Html.div [
                Html.span [ Html.text "Autosave"]
                Daisy.table [
                    prop.className "table-xs"
                    prop.children [
                        Html.tbody (
                            model.DevState.Log
                            |> List.map LogItem.toTableRow
                        )
                    ]
                ]
            ]
