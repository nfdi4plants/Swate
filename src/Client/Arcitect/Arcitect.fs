module ARCitect.ARCitect

open ARCitect.Interop
open Model
open Model.ARCitect
open Shared
open Messages

let send (msg:ARCitect.Msg) =
    let (data: obj) =
        match msg with
        | Init ->
            "Hello from Swate!"
        | AssayToARCitect assay ->
            ARCtrl.ISA.Json.ArcAssay.toJsonString assay
        | StudyToARCitect study ->
            ARCtrl.ISA.Json.ArcStudy.toJsonString study
        | Error exn ->
            exn
    postMessageToARCitect(msg, data)

let EventHandler (dispatch: Messages.Msg -> unit) : IEventHandler =
    {
        AssayToSwate = fun data ->
            let assay = ARCtrl.ISA.Json.ArcAssay.fromJsonString data.ArcAssayJsonString
            log($"Received Assay {assay.Identifier} from ARCitect!")
            Spreadsheet.UpdateArcFile (ArcFiles.Assay assay) |> SpreadsheetMsg |> dispatch
        StudyToSwate = fun data ->
            let study, assays = ARCtrl.ISA.Json.ArcStudy.fromJsonString data.ArcStudyJsonString
            Spreadsheet.UpdateArcFile (ArcFiles.Study (study,List.ofSeq assays)) |> SpreadsheetMsg |> dispatch
            log($"Received Study {study.Identifier} from ARCitect!")
            Browser.Dom.console.log(study)
        Error = fun exn ->
            Browser.Dom.window.alert(exn)
    }