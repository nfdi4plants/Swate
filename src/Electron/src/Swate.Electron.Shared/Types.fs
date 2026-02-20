namespace Swate.Electron.Shared

open ARCtrl
open ARCtrl.Json
open Swate.Components
open Swate.Electron.Shared.IPCTypes

[<RequireQualifiedAccess>]
type AppState =
    | Init
    | ARC of path: string

[<RequireQualifiedAccess>]
module ArcFileSaveMapping =

    let tryCreateSaveRequest (arcFile: ArcFiles) : SaveArcFileRequest option =
        match arcFile with
        | ArcFiles.Investigation investigation ->
            Some {
                FileType = ArcFileType.Investigation
                Json = ArcInvestigation.toJsonString 0 investigation
            }
        | ArcFiles.Study(study, _) ->
            Some {
                FileType = ArcFileType.Study
                Json = ArcStudy.toJsonString 0 study
            }
        | ArcFiles.Assay assay ->
            Some {
                FileType = ArcFileType.Assay
                Json = ArcAssay.toJsonString 0 assay
            }
        | ArcFiles.Run run ->
            Some {
                FileType = ArcFileType.Run
                Json = ArcRun.toJsonString 0 run
            }
        | ArcFiles.Workflow workflow ->
            Some {
                FileType = ArcFileType.Workflow
                Json = ArcWorkflow.toJsonString 0 workflow
            }
        | ArcFiles.DataMap _
        | ArcFiles.Template _ ->
            None

    let tryParseArcFile (fileType: ArcFileType) (json: string) : Result<ArcFiles, exn> =
        try
            match fileType with
            | ArcFileType.Investigation ->
                ArcInvestigation.fromJsonString json
                |> ArcFiles.Investigation
                |> Ok
            | ArcFileType.Study ->
                ArcStudy.fromJsonString json
                |> fun study -> ArcFiles.Study(study, [])
                |> Ok
            | ArcFileType.Assay ->
                ArcAssay.fromJsonString json
                |> ArcFiles.Assay
                |> Ok
            | ArcFileType.Run ->
                ArcRun.fromJsonString json
                |> ArcFiles.Run
                |> Ok
            | ArcFileType.Workflow ->
                ArcWorkflow.fromJsonString json
                |> ArcFiles.Workflow
                |> Ok
            | ArcFileType.DataMap ->
                DataMap.fromJsonString json
                |> fun datamap -> ArcFiles.DataMap(None, datamap)
                |> Ok
        with e ->
            Error e

    let tryParseSaveRequest (request: SaveArcFileRequest) : Result<ArcFiles, exn> =
        tryParseArcFile request.FileType request.Json

    let tryCreatePreviewData (arcFile: ArcFiles) : PreviewData option =
        match arcFile with
        | ArcFiles.Investigation investigation ->
            Some(ArcFileData(ArcFileType.Investigation, ArcInvestigation.toJsonString 0 investigation))
        | ArcFiles.Study(study, _) ->
            Some(ArcFileData(ArcFileType.Study, ArcStudy.toJsonString 0 study))
        | ArcFiles.Assay assay ->
            Some(ArcFileData(ArcFileType.Assay, ArcAssay.toJsonString 0 assay))
        | ArcFiles.Run run ->
            Some(ArcFileData(ArcFileType.Run, ArcRun.toJsonString 0 run))
        | ArcFiles.Workflow workflow ->
            Some(ArcFileData(ArcFileType.Workflow, ArcWorkflow.toJsonString 0 workflow))
        | ArcFiles.DataMap _
        | ArcFiles.Template _ ->
            None
