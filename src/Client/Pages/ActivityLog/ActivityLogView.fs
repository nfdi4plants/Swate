namespace Pages

open Fable
open Fable.Core.JsInterop

open Model
open Messages
open Browser.Types

open Feliz
open Feliz.DaisyUI

type ActivityLog =

    static member Main (model:Model) =
        Html.div [
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

