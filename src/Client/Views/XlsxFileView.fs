module XlsxFileView

open Feliz
open Feliz.Bulma
open Messages
open Spreadsheet
open Shared

[<ReactComponentAttribute>]
let Main(model: Messages.Model, dispatch: Messages.Msg -> unit) = 
    match model.SpreadsheetModel.ActiveView with
    | ActiveView.Table _ ->
        match model.SpreadsheetModel.ActiveTable.ColumnCount with
        | 0 -> 
            MainComponents.EmptyTableElement.Main()
        | _ ->
            MainComponents.SpreadsheetView.Main model dispatch
    | ActiveView.Metadata ->
        Bulma.section [
            Bulma.container [
                prop.className "is-max-desktop"
                prop.children [
                    match model.SpreadsheetModel.ArcFile with
                    | Some (ArcFiles.Assay a) ->
                        MainComponents.Metadata.Assay.Main(a, model, dispatch)
                    | Some (ArcFiles.Study (s,aArr)) ->
                        MainComponents.Metadata.Study.Main(s, aArr, model, dispatch)
                    | Some (ArcFiles.Investigation inv) ->
                        MainComponents.Metadata.Investigation.Main(inv, model, dispatch)
                    | Some (ArcFiles.Template t) ->
                        MainComponents.Metadata.Template.Main(t, model, dispatch)
                    | None ->
                        Html.none
                ]
            ]
        ]