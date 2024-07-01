module ARCitect.ARCitect

open ARCitect.Interop
open Model
open Model.ARCitect
open Shared
open Messages
open Elmish
open ARCtrl
open ARCtrl.Json

let send (msg:ARCitect.Msg) =
    let (data: obj) =
        match msg with
        | Init ->
            "Hello from Swate!"
        | AssayToARCitect assay ->
            let assay = ArcAssay.toJsonString 0 assay
            assay
        | StudyToARCitect study ->
            let json = ArcStudy.toJsonString 0 study
            json
        | InvestigationToARCitect inv ->
            let json = ArcInvestigation.toJsonString 0 inv
            json
        | RequestPaths selectDirectories ->
            selectDirectories
        | Error exn ->
            exn
    postMessageToARCitect(msg, data)

let EventHandler (dispatch: Messages.Msg -> unit) : IEventHandler =
    {
        AssayToSwate = fun data ->
            let assay = ArcAssay.fromJsonString data.ArcAssayJsonString
            log($"Received Assay {assay.Identifier} from ARCitect!")
            Spreadsheet.InitFromArcFile (ArcFiles.Assay assay) |> SpreadsheetMsg |> dispatch
        StudyToSwate = fun data ->
            let study = ArcStudy.fromJsonString data.ArcStudyJsonString
            Spreadsheet.InitFromArcFile (ArcFiles.Study (study, [])) |> SpreadsheetMsg |> dispatch
            log($"Received Study {study.Identifier} from ARCitect!")
        InvestigationToSwate = fun data ->
            let inv = ArcInvestigation.fromJsonString data.ArcInvestigationJsonString
            Spreadsheet.InitFromArcFile (ArcFiles.Investigation inv) |> SpreadsheetMsg |> dispatch
            log($"Received Investigation {inv.Title} from ARCitect!")
        PathsToSwate = fun paths ->
            log $"Received {paths.paths.Length} paths from ARCitect!"
            FilePicker.LoadNewFiles (List.ofArray paths.paths) |> FilePickerMsg |> dispatch
        Error = fun exn ->
            GenericError (Cmd.none, exn) |> DevMsg |> dispatch
    }