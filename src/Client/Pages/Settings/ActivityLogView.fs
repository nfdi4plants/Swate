namespace Pages

open Model

open Feliz
open Feliz.DaisyUI

type ActivityLog =

    static member Main(model: Model) =
        Html.div [
            //Daisy.table [
            //    prop.className "swt:table-xs"
            //    prop.children [ Html.tbody (model.DevState.Log |> List.map LogItem.toTableRow) ]
            //]
            Html.table [
                prop.className "swt:table-xs"
                prop.children [
                    Html.tbody [
                        for logEntry in model.DevState.Log do
                            yield LogItem.toTableRow logEntry
                    ]
                ]
            ]
        ]