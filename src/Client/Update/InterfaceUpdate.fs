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
open OfficeInteropTypes


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