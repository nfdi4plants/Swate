module ARCitect.ARCitect

open ARCitect.Interop
open Model
open Model.ARCitect
open Shared
open Messages
open Elmish
open ARCtrl.ISA
open ARCtrl.ISA.Json

let send (msg:ARCitect.Msg) =
    let (data: obj) =
        match msg with
        | Init ->
            "Hello from Swate!"
        | TriggerSwateClose ->
            null
        | AssayToARCitect assay ->
            let assay = ArcAssay.toArcJsonString assay
            assay
        | StudyToARCitect study ->
            let json = ArcStudy.toArcJsonString study
            json
        | Error exn ->
            exn
    postMessageToARCitect(msg, data)

let EventHandler (dispatch: Messages.Msg -> unit) : IEventHandler =
    {
        AssayToSwate = fun data ->
            let assay = ArcAssay.fromArcJsonString data.ArcAssayJsonString
            log($"Received Assay {assay.Identifier} from ARCitect!")
            Spreadsheet.InitFromArcFile (ArcFiles.Assay assay) |> SpreadsheetMsg |> dispatch
        StudyToSwate = fun data ->
            let study = ArcStudy.fromArcJsonString data.ArcStudyJsonString
            Spreadsheet.InitFromArcFile (ArcFiles.Study (study, [])) |> SpreadsheetMsg |> dispatch
            log($"Received Study {study.Identifier} from ARCitect!")
            Browser.Dom.console.log(study)
        Error = fun exn ->
            GenericError (Cmd.none, exn) |> DevMsg |> dispatch
    }