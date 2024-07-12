module XlsxFileView

open Feliz
open Feliz.Bulma
open Messages
open Spreadsheet
open Shared
open Model

[<ReactComponentAttribute>]
let Main(model: Model, dispatch: Messages.Msg -> unit, openBuildingBlockWidget, openTemplateWidget) = 
    match model.SpreadsheetModel.ActiveView with
    | ActiveView.Table _ ->
        match model.SpreadsheetModel.ActiveTable.ColumnCount with
        | 0 -> 
            MainComponents.EmptyTableElement.Main(openBuildingBlockWidget, openTemplateWidget)
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
    | ActiveView.DataMap ->
        MainComponents.DataMap.DataMap.Main (model, dispatch)