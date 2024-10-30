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

module private JsonImportHelper =

    open ARCtrl
    open JsonImport

    let updateWithMetadata (uploadedFile: ArcFiles) (state: SelectiveImportModalState) =
        if not state.ImportMetadata then failwith "Metadata must be imported"
        /// This updates the existing tables based on import config (joinOptions)
        let createUpdatedTables (arcTables: ResizeArray<ArcTable>) =
            [
                for it in state.ImportTables do
                    let sourceTable = arcTables.[it.Index]
                    let appliedTable = ArcTable.init(sourceTable.Name)
                    appliedTable.Join(sourceTable, joinOptions=state.ImportType)
                    appliedTable
            ]
            |> ResizeArray
        let arcFile =
            match uploadedFile with
            | Assay a as arcFile->
                let tables = createUpdatedTables a.Tables
                a.Tables <- tables
                arcFile
            | Study (s,_) as arcFile ->
                let tables = createUpdatedTables s.Tables
                s.Tables <- tables
                arcFile
            | Template t as arcFile ->
                let table = createUpdatedTables (ResizeArray[t.Table])
                t.Table <- table.[0]
                arcFile
            | Investigation _ as arcFile ->
                arcFile
        arcFile

    /// <summary>
    /// 
    /// </summary>
    /// <param name="import"></param>
    /// <param name="importState"></param>
    /// <param name="activeTableIndex">Required to append imported tables to the active table.</param>
    /// <param name="existing"></param>
    let updateTables (import: ArcFiles) (importState: SelectiveImportModalState) (activeTableIndex: int option) (existingOpt: ArcFiles option) =
        match existingOpt with
        | Some existing ->
            let importTables =
                match import with
                | Assay a -> a.Tables
                | Study (s,_) -> s.Tables
                | Template t -> ResizeArray([t.Table])
                | Investigation _ -> ResizeArray()
            let existingTables =
                match existing with
                | Assay a -> a.Tables
                | Study (s,_) -> s.Tables
                | Template t -> ResizeArray([t.Table])
                | Investigation _ ->  ResizeArray()
            let appendTables =
                // only append if the active table exists (this is to handle join call on investigations)
                match activeTableIndex with
                | Some i when i >= 0 && i < existingTables.Count ->
                    let activeTable = existingTables.[i]
                    let tables = importState.ImportTables |> Seq.filter (fun x -> not x.FullImport) |> Seq.map (fun x -> importTables.[x.Index])
                    /// Everything will be appended against this table, which in the end will be appended to the main table
                    let tempTable = activeTable.Copy()
                    for table in tables do
                        let preparedTemplate = Table.distinctByHeader tempTable table
                        tempTable.Join(preparedTemplate, joinOptions=importState.ImportType)
                    existingTables.[i] <- tempTable
                | _ -> ()
            let addTables =
                importState.ImportTables
                |> Seq.filter (fun x -> x.FullImport)
                |> Seq.map (fun x -> importTables.[x.Index])
                |> Seq.map (fun table -> // update tables based on joinOptions
                    let nTable = ArcTable.init(table.Name)
                    nTable.Join(table, joinOptions=importState.ImportType)
                    nTable
                )
                |> Seq.iter (fun table -> existingTables.Add table)
            existing
        | None -> //failsafe, if no file exists, we fully insert the import
            import


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
                    let cmd = OfficeInterop.JoinTable (table, options) |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.JoinTable (table, index, options) |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | UpdateArcFile arcFiles ->
                match host with
                | Some Swatehost.Excel ->
                    //let cmd = OfficeInterop.ImportFile tables |> OfficeInteropMsg |> Cmd.ofMsg
                    Browser.Dom.window.alert "Not implemented"
                    model, Cmd.none
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd = Spreadsheet.UpdateArcFile arcFiles |> SpreadsheetMsg |> Cmd.ofMsg
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
            | ImportJson data ->
                match host with
                | Some Swatehost.Excel ->
                    /// In Excel we must get the current information from worksheets and update them with the imported information
                    let updateExistingInfoWithImportedInfo() = 
                        promise {
                            match data.importState.ImportMetadata with
                            | true -> // full import, does not require additional information
                                return JsonImportHelper.updateWithMetadata data.importedFile data.importState
                            | false -> // partial import, requires additional information
                                let! arcfile = OfficeInterop.Core.OfficeInterop.tryParseToArcFile()
                                let arcfileOpt = arcfile |> Result.toOption
                                let! activeTable = ExcelJS.Fable.GlobalBindings.Excel.run(fun context ->
                                    OfficeInterop.Core.tryGetActiveArcTable context
                                )
                                let activeTableIndex =
                                    match arcfileOpt, activeTable with
                                    | Some arcfile, Some activeTable -> arcfile.Tables() |> Seq.tryFindIndex (fun x -> x = activeTable)
                                    | _ -> None
                                return JsonImportHelper.updateTables data.importedFile data.importState activeTableIndex arcfileOpt
                        }
                    let updateArcFile (arcFile: ArcFiles) =
                        log ("next arcfile", arcFile)
                        SpreadsheetInterface.UpdateArcFile arcFile |> InterfaceMsg
                    let cmd =
                        Cmd.OfPromise.perform
                            updateExistingInfoWithImportedInfo
                            ()
                            updateArcFile
                    model, cmd
                | Some Swatehost.Browser | Some Swatehost.ARCitect ->
                    let cmd =
                        match data.importState.ImportMetadata with
                        | true -> JsonImportHelper.updateWithMetadata data.importedFile data.importState
                        | false -> JsonImportHelper.updateTables data.importedFile data.importState model.SpreadsheetModel.ActiveView.TryTableIndex model.SpreadsheetModel.ArcFile
                        |> SpreadsheetInterface.UpdateArcFile |> InterfaceMsg |> Cmd.ofMsg
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
                    let cmd = SpreadsheetMsg (Spreadsheet.ExportJson (arcfile, jef)) |> Cmd.ofMsg
                    model, cmd
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
                