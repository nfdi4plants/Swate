module ActivityLogView

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Model
open Messages

//TO-DO: Save log as tab seperated file

let activityLogComponent (model:Model) dispatch =
    div [][
        Button.button [
            Button.Color Color.IsDanger
            Button.IsFullWidth
            Button.OnClick (fun e -> UpdatePageState (Some Routing.Route.TermSearch) |> dispatch)
            Button.Props [Style [MarginBottom "1rem"]]
        ][
            str "Back to Term Search"
        ]
        //Button.button [
        //    Button.Color Color.IsInfo
        //    Button.IsFullWidth
        //    Button.OnClick (fun e ->
        //        (fun tableName -> TryExcel (tableName, model.FilePickerState.FileNames))|> PipeActiveAnnotationTable |> ExcelInterop |> dispatch )
        //    Button.Props [Style [MarginBottom "1rem"]]
        //] [
        //    str "Try Excel"
        //]
        //Button.button [
        //    Button.Color Color.IsInfo
        //    Button.IsFullWidth
        //    Button.OnClick (fun e -> TryExcel2 |> ExcelInterop |> dispatch )
        //    Button.Props [Style [MarginBottom "1rem"]]
        //] [
        //    str "Try Excel2"
        //]
        Help.help [][str "This page is used for development/debugging."]
        Table.table [
            Table.IsFullWidth
            Table.Props [ExcelColors.colorBackground model.SiteStyleState.ColorMode]
        ] [
            tbody [] (
                model.DevState.Log
                |> List.map LogItem.toTableRow
            )
        ]
    ]
    