module ARCitect.ARCitect

open ARCitect.Interop
open Model
open Model.ARCitect
open Shared
open Messages
open Elmish

let send (msg:ARCitect.Msg) =
    let (data: obj) =
        match msg with
        | Init ->
            "Hello from Swate!"
        | TriggerSwateClose ->
            null
        | AssayToARCitect assay ->
            let assay = ARCtrl.ISA.Json.ArcAssay.toJsonString assay
            assay
        | StudyToARCitect study ->
            let json = ARCtrl.ISA.Json.ArcStudy.toJsonString study (ResizeArray([]))
            json
        | Error exn ->
            exn
    postMessageToARCitect(msg, data)

let EventHandler (dispatch: Messages.Msg -> unit) : IEventHandler =
    {
        AssayToSwate = fun data ->
            let assay = ARCtrl.ISA.Json.ArcAssay.fromJsonString data.ArcAssayJsonString
            log($"Received Assay {assay.Identifier} from ARCitect!")
            Spreadsheet.InitFromArcFile (ArcFiles.Assay assay) |> SpreadsheetMsg |> dispatch
        StudyToSwate = fun data ->
            let study, assays = ARCtrl.ISA.Json.ArcStudy.fromJsonString data.ArcStudyJsonString
            Spreadsheet.InitFromArcFile (ArcFiles.Study (study,List.ofSeq assays)) |> SpreadsheetMsg |> dispatch
            log($"Received Study {study.Identifier} from ARCitect!")
            Browser.Dom.console.log(study)
        Error = fun exn ->
            GenericError (Cmd.none, exn) |> DevMsg |> dispatch
    }