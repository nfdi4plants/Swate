module ActivityLogView

open Fulma
open Fable.React
open Model

//TO-DO: Save log as tab seperated file

let activityLogComponent (model:Model) =
    Table.table [
        Table.IsFullWidth
        Table.Props [ExcelColors.colorBackground model.SiteStyleState.ColorMode]
    ] [
        tbody [] (
            model.DevState.Log
            |> List.map LogItem.toTableRow
        )
    ]
    