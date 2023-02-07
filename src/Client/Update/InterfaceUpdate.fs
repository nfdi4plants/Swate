namespace Update

open Elmish
open SpreadsheetInterface
open Messages
open Model
open Shared
open OfficeInteropTypes
open OfficeInterop

module private Helper =
    open ExcelJS.Fable.GlobalBindings

    let initializeAddIn () = Office.onReady()

module Interface =

    let update (model: Messages.Model) (msg: SpreadsheetInterface.Msg) : Messages.Model * Cmd<Messages.Msg> =
        match msg with
        | Initialize ->
            let initExcel() = promise {
                let! tryExcel = Helper.initializeAddIn()
                Browser.Dom.console.log("[LOG] ",tryExcel)
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
                            (AnnotationTableExists >> OfficeInteropMsg)
                            (curry GenericError Cmd.none >> DevMsg)
                    | _ -> ()
                ]
            nextModel, cmd