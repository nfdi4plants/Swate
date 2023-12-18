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


module private Helper =
    open ExcelJS.Fable.GlobalBindings

    let initializeAddIn () = Office.onReady()

module Interface =

    let update (model: Messages.Model) (msg: SpreadsheetInterface.Msg) : Messages.Model * Cmd<Messages.Msg> =
        let host = model.PersistentStorageState.Host
        match msg with
        | Initialize ->
            let initExcel() = promise {
                let! tryExcel = Helper.initializeAddIn()
                let host =
                    if (isNull >> not) tryExcel.host then
                        Swatehost.Excel (tryExcel.host.ToString(), tryExcel.platform.ToString())
                    else
                        Swatehost.Browser
                return host
            }
            let cmd =
                Cmd.OfPromise.perform
                    initExcel
                    ()
                    InitializeResponse
            model, Cmd.map InterfaceMsg cmd
        // This is very bloated, might be good to reduce
        | InitializeResponse host ->
            let nextState = {model.PersistentStorageState with Host = host}
            let nextModel = {model with PersistentStorageState = nextState}
            let cmd =
                Cmd.batch [
                    Cmd.ofMsg (GetAppVersion |> Request |> Api)
                    Cmd.ofMsg (FetchAllOntologies |> Request |> Api)
                    match host with
                    | Swatehost.Excel (h,p) ->
                        let welcomeMsg = sprintf "Ready to go in %s running on %s" h p
                        Cmd.ofMsg (curry GenericLog Cmd.none ("Info",welcomeMsg) |> DevMsg)
                        Cmd.OfPromise.either
                            OfficeInterop.Core.tryFindActiveAnnotationTable
                            ()
                            (OfficeInterop.AnnotationTableExists >> OfficeInteropMsg)
                            (curry GenericError Cmd.none >> DevMsg)
                    | Swatehost.Browser | Swatehost.Electron ->
                        Cmd.batch [
                            Cmd.ofEffect (fun dispatch -> Spreadsheet.KeyboardShortcuts.addOnKeydownEvent dispatch)
                        ]
                    | _ -> ()
                ]
            nextModel, cmd
        // These messages are guarded against host = Swatehost.None
        // Swatehost.None should only ever be used during init and is not checked for elsewhere. 
        | msg ->
            if host = Swatehost.None then failwith "Host initialisation not finished. Reload Page or contact maintainer."
            match msg with
            | Initialize | InitializeResponse _ -> failwith "This is caught before"
            | CreateAnnotationTable usePrevOutput ->
                match host with
                | Swatehost.Excel _ ->
                    let cmd = OfficeInterop.CreateAnnotationTable usePrevOutput |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Swatehost.Browser ->
                    let cmd = Spreadsheet.CreateAnnotationTable usePrevOutput |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | AddAnnotationBlock minBuildingBlockInfo ->
                match host with
                //| Swatehost.Excel _ ->
                //    let cmd = OfficeInterop.AddAnnotationBlock minBuildingBlockInfo |> OfficeInteropMsg |> Cmd.ofMsg
                //    model, cmd
                | Swatehost.Browser ->
                    let cmd = Spreadsheet.AddAnnotationBlock minBuildingBlockInfo |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | AddAnnotationBlocks minBuildingBlockInfos ->
                match host with
                //| Swatehost.Excel _ ->
                //    let cmd = OfficeInterop.AddAnnotationBlocks minBuildingBlockInfos |> OfficeInteropMsg |> Cmd.ofMsg
                //    model, cmd
                | Swatehost.Browser ->
                    let cmd = Spreadsheet.AddAnnotationBlocks minBuildingBlockInfos |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | ImportFile tables ->
                match host with
                | Swatehost.Excel _ ->
                    //let cmd = OfficeInterop.ImportFile tables |> OfficeInteropMsg |> Cmd.ofMsg
                    Browser.Dom.window.alert "Not implemented"
                    model, Cmd.none
                | Swatehost.Browser ->
                    let cmd = Spreadsheet.UpdateArcFile tables |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | InsertOntologyTerm termMinimal ->
                match host with
                //| Swatehost.Excel _ ->
                //    let cmd = OfficeInterop.InsertOntologyTerm termMinimal |> OfficeInteropMsg |> Cmd.ofMsg
                //    model, cmd
                | Swatehost.Browser ->
                    let cmd = Spreadsheet.InsertOntologyTerm termMinimal |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | InsertFileNames fileNames ->
                match host with
                | Swatehost.Excel _ ->
                    let cmd = OfficeInterop.InsertFileNames fileNames |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                //| Swatehost.Browser ->
                //    let arr = fileNames |> List.toArray |> Array.map (fun x -> TermTypes.TermMinimal.create x "")
                //    let cmd = Spreadsheet.InsertOntologyTerms arr |> SpreadsheetMsg |> Cmd.ofMsg
                //    model, cmd
                | _ -> failwith "not implemented"
            | RemoveBuildingBlock ->
                match host with
                | Swatehost.Excel _ ->
                    let cmd = OfficeInterop.RemoveBuildingBlock |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Swatehost.Browser ->
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
                | Swatehost.Excel _ ->
                    let cmd = JsonExporterMsg JsonExporter.State.ParseTableOfficeInteropRequest |> Cmd.ofMsg
                    model, cmd
                | Swatehost.Browser ->
                    let cmd = SpreadsheetMsg Spreadsheet.ExportJsonTable |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | ExportJsonTables ->
                match host with
                | Swatehost.Excel _ ->
                    let cmd = JsonExporterMsg JsonExporter.State.ParseTablesOfficeInteropRequest |> Cmd.ofMsg
                    model, cmd
                | Swatehost.Browser ->
                    let cmd = SpreadsheetMsg Spreadsheet.ExportJsonTables |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | ParseTablesToDag ->
                match host with
                | Swatehost.Excel _ ->
                    let cmd = DagMsg Dag.ParseTablesOfficeInteropRequest |> Cmd.ofMsg
                    model, cmd
                | Swatehost.Browser ->
                    let cmd = SpreadsheetMsg Spreadsheet.ParseTablesToDag |> Cmd.ofMsg
                    model, cmd
                | _ -> failwith "not implemented"
            | EditBuildingBlock ->
                match host with
                | Swatehost.Excel _ ->
                    let cmd = OfficeInterop.GetSelectedBuildingBlockTerms |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                //| Swatehost.Browser ->
                //    let selectedIndex = model.SpreadsheetModel.SelectedCells |> Set.toArray |> Array.minBy fst |> fst
                //    let cmd = Cmd.ofEffect (fun dispatch -> Modals.Controller.renderModal("EditColumn_Modal", Modals.EditColumn.Main selectedIndex model dispatch))
                //    model, cmd
                | _ -> failwith "not implemented"
            | UpdateTermColumns ->
                match host with
                | Swatehost.Excel _ ->
                    let cmd = OfficeInterop.FillHiddenColsRequest |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Swatehost.Browser ->
                    let cmd = Spreadsheet.UpdateTermColumns |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ ->
                    failwith "not implemented"
            | UpdateTermColumnsResponse terms ->
                match host with
                | Swatehost.Excel _ ->
                    let cmd = OfficeInterop.FillHiddenColumns terms |> OfficeInteropMsg |> Cmd.ofMsg
                    model, cmd
                | Swatehost.Browser ->
                    let cmd = Spreadsheet.UpdateTermColumnsResponse terms |> SpreadsheetMsg |> Cmd.ofMsg
                    model, cmd
                | _ ->
                    failwith "not implemented"
                