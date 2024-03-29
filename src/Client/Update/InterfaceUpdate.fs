namespace Update

// <-->
// This order is required to correctly inferre the correct "Msg"s below.
// do not touch and remove 
open Messages
open OfficeInterop
open SpreadsheetInterface
// </-->

open Elmish
open Model
open Shared
open Fable.Core.JsInterop

module private Helper =
    open ExcelJS.Fable.GlobalBindings

    let initializeAddIn () = Office.onReady()

//open Fable.Core.JS

module Interface =

    let update (model: Messages.Model) (msg: SpreadsheetInterface.Msg) : Messages.Model * Cmd<Messages.Msg> =
        let host = model.PersistentStorageState.Host
        match msg with
        // This is very bloated, might be good to reduce
        | Initialize host ->
            let cmd =
                Cmd.batch [
                    Cmd.ofMsg (GetAppVersion |> Request |> Api)
                    Cmd.ofMsg (FetchAllOntologies |> Request |> Api)
                    match host with
                    | Swatehost.Excel ->
                        Cmd.OfPromise.either
                            OfficeInterop.Core.tryFindActiveAnnotationTable
                            ()
                            (OfficeInterop.AnnotationTableExists >> OfficeInteropMsg)
                            (curry GenericError Cmd.none >> DevMsg)
                    | Swatehost.Browser ->
                        Cmd.batch [
                            Cmd.ofEffect (fun dispatch -> Spreadsheet.KeyboardShortcuts.addOnKeydownEvent dispatch)
                        ]
                    | Swatehost.ARCitect ->
                        Cmd.batch [
                            Cmd.ofEffect (fun dispatch -> Spreadsheet.KeyboardShortcuts.addOnKeydownEvent dispatch)
                            Cmd.ofEffect (fun _ -> ARCitect.ARCitect.send ARCitect.Init)
                        ]
                ]
            model, cmd
        | CreateAnnotationTable usePrevOutput ->
            match host with
            | Some Swatehost.Excel ->
                let cmd = OfficeInterop.CreateAnnotationTable usePrevOutput |> OfficeInteropMsg |> Cmd.ofMsg
                model, cmd
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                let cmd = Spreadsheet.CreateAnnotationTable usePrevOutput |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | AddAnnotationBlock minBuildingBlockInfo ->
            match host with
            //| Swatehost.Excel _ ->
            //    let cmd = OfficeInterop.AddAnnotationBlock minBuildingBlockInfo |> OfficeInteropMsg |> Cmd.ofMsg
            //    model, cmd
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                let cmd = Spreadsheet.AddAnnotationBlock minBuildingBlockInfo |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | AddAnnotationBlocks minBuildingBlockInfos ->
            match host with
            //| Swatehost.Excel _ ->
            //    let cmd = OfficeInterop.AddAnnotationBlocks minBuildingBlockInfos |> OfficeInteropMsg |> Cmd.ofMsg
            //    model, cmd
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                let cmd = Spreadsheet.AddAnnotationBlocks minBuildingBlockInfos |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | JoinTable (table, index, options) ->
            match host with
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                let cmd = Spreadsheet.JoinTable (table, index, options) |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | ImportFile tables ->
            match host with
            | Some Swatehost.Excel ->
                //let cmd = OfficeInterop.ImportFile tables |> OfficeInteropMsg |> Cmd.ofMsg
                Browser.Dom.window.alert "Not implemented"
                model, Cmd.none
            | Some Swatehost.Browser ->
                let cmd = Spreadsheet.UpdateArcFile tables |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | InsertOntologyAnnotation termMinimal ->
            match host with
            //| Swatehost.Excel _ ->
            //    let cmd = OfficeInterop.InsertOntologyTerm termMinimal |> OfficeInteropMsg |> Cmd.ofMsg
            //    model, cmd
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                let cmd = Spreadsheet.InsertOntologyAnnotation termMinimal |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | InsertFileNames fileNames ->
            match host with
            | Some Swatehost.Excel | Some Swatehost.ARCitect ->
                let cmd = OfficeInterop.InsertFileNames fileNames |> OfficeInteropMsg |> Cmd.ofMsg
                model, cmd
            //| Swatehost.Browser ->
            //    let arr = fileNames |> List.toArray |> Array.map (fun x -> TermTypes.TermMinimal.create x "")
            //    let cmd = Spreadsheet.InsertOntologyTerms arr |> SpreadsheetMsg |> Cmd.ofMsg
            //    model, cmd
            | _ -> failwith "not implemented"
        | RemoveBuildingBlock ->
            match host with
            | Some Swatehost.Excel ->
                let cmd = OfficeInterop.RemoveBuildingBlock |> OfficeInteropMsg |> Cmd.ofMsg
                model, cmd
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                if Set.isEmpty model.SpreadsheetModel.SelectedCells then failwith "No column selected"
                let selectedColumns, _ = model.SpreadsheetModel.SelectedCells |> Set.toArray |> Array.unzip
                let distinct = selectedColumns |> Array.distinct
                let cmd =
                    if distinct.Length <> 1 then
                        let msg = Failure("Please select one column only if you want to use `Remove Building Block`.")
                        GenericError (Cmd.none,msg) |> DevMsg |> Cmd.ofMsg
                    else
                        Spreadsheet.DeleteColumn (distinct.[0]) |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | ExportJsonTable ->
            match host with
            | Some Swatehost.Excel ->
                let cmd = JsonExporterMsg JsonExporter.ParseTableOfficeInteropRequest |> Cmd.ofMsg
                model, cmd
            | Some Swatehost.Browser ->
                let cmd = SpreadsheetMsg Spreadsheet.ExportJsonTable |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | ExportJsonTables ->
            match host with
            | Some Swatehost.Excel ->
                let cmd = JsonExporterMsg JsonExporter.ParseTablesOfficeInteropRequest |> Cmd.ofMsg
                model, cmd
            | Some Swatehost.Browser ->
                let cmd = SpreadsheetMsg Spreadsheet.ExportJsonTables |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | ParseTablesToDag ->
            match host with
            | Some Swatehost.Excel ->
                let cmd = DagMsg Dag.ParseTablesOfficeInteropRequest |> Cmd.ofMsg
                model, cmd
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                let cmd = SpreadsheetMsg Spreadsheet.ParseTablesToDag |> Cmd.ofMsg
                model, cmd
            | _ -> failwith "not implemented"
        | EditBuildingBlock ->
            match host with
            | Some Swatehost.Excel ->
                let cmd = OfficeInterop.GetSelectedBuildingBlockTerms |> OfficeInteropMsg |> Cmd.ofMsg
                model, cmd
            //| Swatehost.Browser ->
            //    let selectedIndex = model.SpreadsheetModel.SelectedCells |> Set.toArray |> Array.minBy fst |> fst
            //    let cmd = Cmd.ofEffect (fun dispatch -> Modals.Controller.renderModal("EditColumn_Modal", Modals.EditColumn.Main selectedIndex model dispatch))
            //    model, cmd
            | _ -> failwith "not implemented"
        | UpdateTermColumns ->
            match host with
            | Some Swatehost.Excel ->
                let cmd = OfficeInterop.FillHiddenColsRequest |> OfficeInteropMsg |> Cmd.ofMsg
                model, cmd
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                let cmd = Spreadsheet.UpdateTermColumns |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ ->
                failwith "not implemented"
        | UpdateTermColumnsResponse terms ->
            match host with
            | Some Swatehost.Excel ->
                let cmd = OfficeInterop.FillHiddenColumns terms |> OfficeInteropMsg |> Cmd.ofMsg
                model, cmd
            | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                let cmd = Spreadsheet.UpdateTermColumnsResponse terms |> SpreadsheetMsg |> Cmd.ofMsg
                model, cmd
            | _ ->
                failwith "not implemented"
                