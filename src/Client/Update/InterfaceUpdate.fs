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
open Shared.ARCtrlHelper

/// This seems like such a hack :(
module private ExcelHelper =

    open Fable.Core
    open ExcelJS.Fable.GlobalBindings

    let initializeAddIn () = Office.onReady().``then``(fun _ -> ()) |> Async.AwaitPromise

    /// Office-js will kill iframe loading in ARCitect, therefore we must load it conditionally
    let addOfficeJsScript(callback: unit -> unit) =
        let cdn = @"https://appsforoffice.microsoft.com/lib/1/hosted/office.js"
        let _type = "text/javascript"
        let s = Browser.Dom.document.createElement("script")
        s?``type`` <- _type
        s?src <- cdn
        Browser.Dom.document.head.appendChild s |> ignore
        s.onload <- fun _ -> callback()
        ()

    /// Make a function that loops short sleep sequences until a mutable variable is set to true
    /// do mutabel dotnet ref for variable
    let myAwaitLoadedThenInit(loaded: ref<bool>) =
        let rec loop() =
            async {
            if loaded.Value then
                do! initializeAddIn()
            else
                do! Async.Sleep 100
                do! loop()
            }
        loop()

    let officeload() =
        let loaded = ref false
        async {
            addOfficeJsScript(fun _ -> loaded.Value <- true)
            do! myAwaitLoadedThenInit loaded
        }

//open Fable.Core.JS

module Interface =

    let update (model: Model) (msg: SpreadsheetInterface.Msg) : Model * Cmd<Messages.Msg> =
        let host = model.PersistentStorageState.Host

        let innerUpdate (model: Model) (msg: SpreadsheetInterface.Msg) =
            match msg with
            // This is very bloated, might be good to reduce
            | Initialize host ->
                let cmd =
                    Cmd.batch [
                        Cmd.ofMsg (Ontologies.GetOntologies |> OntologyMsg)
                        match host with
                        | Swatehost.Excel ->
                            Cmd.OfAsync.either
                                ExcelHelper.officeload
                                ()
                                (fun _ -> TryFindAnnotationTable |> OfficeInteropMsg)
                                (curry GenericError Cmd.none >> DevMsg)
                        | Swatehost.Browser ->
                            Cmd.none
                        | Swatehost.ARCitect ->
                            Cmd.ofEffect (fun _ -> 
                                LocalHistory.Model.ResetHistoryWebStorage()
                                ARCitect.ARCitect.send ARCitect.Init
                            )
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
            | UpdateDatamap datamapOption ->
                match host with
                | Some Swatehost.Excel ->
                    failwith "UpdateDatamap not implemented for Excel"
                    model, Cmd.none
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.UpdateDatamap datamapOption |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | UpdateDataMapDataContextAt (index,dc) ->
                match host with
                | Some Swatehost.Excel ->
                    //let cmd = OfficeInterop.UpdateDataContextAt (dc, index) |> OfficeInteropMsg |> Cmd.ofMsg
                    failwith "UpdateDataContextAt not implemented for Excel"
                    model, Cmd.none
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.UpdateDataMapDataContextAt (index, dc) |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | AddTable table ->
                match host with
                | Some Swatehost.Excel ->
                    //let cmd = OfficeInterop.AddTable table |> OfficeInteropMsg |> Cmd.ofMsg
                    failwith "AddTable not implemented for Excel"
                    model, Cmd.none
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.AddTable table |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | AddAnnotationBlock minBuildingBlockInfo ->
                match host with
                | Some Swatehost.Excel ->
                    let cmd = OfficeInterop.AddAnnotationBlock minBuildingBlockInfo |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.AddAnnotationBlock minBuildingBlockInfo |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | ValidateBuildingBlock ->
                match host with
                | Some Swatehost.Excel ->
                    let cmd = OfficeInterop.ValidateBuildingBlock |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | AddAnnotationBlocks compositeColumns ->
                match host with
                | Some Swatehost.Excel ->
                    let cmd = OfficeInterop.AddAnnotationBlocks compositeColumns |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.AddAnnotationBlocks compositeColumns |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"

            | AddTemplate table ->
                match host with
                | Some Swatehost.Excel ->
                    let cmd = OfficeInterop.AddTemplate table |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.AddTemplate table |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | JoinTable (table, index, options) ->
                match host with
                | Some Swatehost.Excel ->
                    let cmd = OfficeInterop.JoinTable (table, index, options) |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.JoinTable (table, index, options) |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | UpdateArcFile tables ->
                match host with
                | Some Swatehost.Excel ->
                    //let cmd = OfficeInterop.ImportFile tables |> OfficeInteropMsg |> Cmd.ofMsg
                    Browser.Dom.window.alert "Not implemented"
                    model, Cmd.none
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.UpdateArcFile tables |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | ImportXlsx bytes ->
                match host with
                | Some Swatehost.Excel ->
                    Browser.Dom.window.alert "ImportXlsx Not implemented"
                    model, Cmd.none
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.ImportXlsx bytes |> SpreadsheetMsg |> Cmd.ofMsg
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
                | Some Swatehost.Excel ->
                    let cmd = OfficeInterop.InsertFileNames fileNames |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    if model.SpreadsheetModel.SelectedCells.IsEmpty then
                        model, Cmd.ofMsg (DevMsg.GenericError (Cmd.none, exn("No cell(s) selected.")) |> DevMsg)
                    else
                        let columnIndex, rowIndex = model.SpreadsheetModel.SelectedCells.MinimumElement
                        let mutable rowIndex = rowIndex
                        let cells = [|
                            for name in fileNames do
                                match model.SpreadsheetModel.ActiveTable.TryGetCellAt(columnIndex,rowIndex) with
                                | Some c ->
                                    let cell = c.UpdateMainField name
                                    (columnIndex, rowIndex), cell
                                    rowIndex <- rowIndex + 1
                                | None -> ()
                        |]
                        let cmd = Spreadsheet.UpdateCells cells |> SpreadsheetMsg |> Cmd.ofMsg
                        model, cmd
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
            | AddDataAnnotation data ->
                match host with
                | Some Swatehost.Excel ->
                    //let cmd = OfficeInterop.AddDataAnnotation data |> OfficeInteropMsg |> Cmd.ofMsg
                    failwith "AddDataAnnotation is not implemented" 
                    model, Cmd.none
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.AddDataAnnotation data |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | ExportJson (arcfile, jef) ->
                match host with
                | Some Swatehost.Excel ->
                    failwith "ExportJson not implemented for Excel"
                    model, Cmd.none
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = SpreadsheetMsg (Spreadsheet.ExportJson (arcfile, jef)) |> Cmd.ofMsg
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
            | UpdateUnitForCells ->
                match host with
                | Some Swatehost.Excel ->
                    let cmd = OfficeInterop.UpdateUnitForCells |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | RectifyTermColumns ->
                match host with
                | Some Swatehost.Excel ->
                    let cmd = OfficeInterop.RectifyTermColumns |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.RectifyTermColumns |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
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

        try
            innerUpdate model msg
        with
            | e -> 
                let cmd = GenericError (Cmd.none, e) |> DevMsg |> Cmd.ofMsg
                model, cmd
                