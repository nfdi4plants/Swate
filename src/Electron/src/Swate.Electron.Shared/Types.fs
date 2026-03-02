namespace Swate.Electron.Shared

open ARCtrl
open ARCtrl.Json
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
                FileType = ArcFilesDiscriminate.Investigation
                Json = ArcInvestigation.toJsonString 0 investigation
            }
        | ArcFiles.Study(study, _) ->
            Some {
                FileType = ArcFilesDiscriminate.Study
                Json = ArcStudy.toJsonString 0 study
            }
        | ArcFiles.Assay assay ->
            Some {
                FileType = ArcFilesDiscriminate.Assay
                Json = ArcAssay.toJsonString 0 assay
            }
        | ArcFiles.Run run ->
            Some {
                FileType = ArcFilesDiscriminate.Run
                Json = ArcRun.toJsonString 0 run
            }
        | ArcFiles.Workflow workflow ->
            Some {
                FileType = ArcFilesDiscriminate.Workflow
                Json = ArcWorkflow.toJsonString 0 workflow
            }
        | ArcFiles.DataMap _
        | ArcFiles.Template _ ->
            None

    let tryParseArcFile (fileType: ArcFilesDiscriminate) (json: string) : Result<ArcFiles, exn> =
        try
            match fileType with
            | ArcFilesDiscriminate.Investigation ->
                ArcInvestigation.fromJsonString json
                |> ArcFiles.Investigation
                |> Ok
            | ArcFilesDiscriminate.Study ->
                ArcStudy.fromJsonString json
                |> fun study -> ArcFiles.Study(study, [])
                |> Ok
            | ArcFilesDiscriminate.Assay ->
                ArcAssay.fromJsonString json
                |> ArcFiles.Assay
                |> Ok
            | ArcFilesDiscriminate.Run ->
                ArcRun.fromJsonString json
                |> ArcFiles.Run
                |> Ok
            | ArcFilesDiscriminate.Workflow ->
                ArcWorkflow.fromJsonString json
                |> ArcFiles.Workflow
                |> Ok
            | ArcFilesDiscriminate.DataMap ->
                DataMap.fromJsonString json
                |> fun datamap -> ArcFiles.DataMap(None, datamap)
                |> Ok
            | ArcFilesDiscriminate.Template ->
                Template.fromJsonString json
                |> ArcFiles.Template
                |> Ok
        with e ->
            Error e

    let tryParseSaveRequest (request: SaveArcFileRequest) : Result<ArcFiles, exn> =
        tryParseArcFile request.FileType request.Json

    let tryCreatePreviewData (arcFile: ArcFiles) : PreviewData option =
        match arcFile with
        | ArcFiles.Investigation investigation ->
            Some(PreviewData.ArcFileData(ArcFilesDiscriminate.Investigation, ArcInvestigation.toJsonString 0 investigation))
        | ArcFiles.Study(study, _) ->
            Some(PreviewData.ArcFileData(ArcFilesDiscriminate.Study, ArcStudy.toJsonString 0 study))
        | ArcFiles.Assay assay ->
            Some(PreviewData.ArcFileData(ArcFilesDiscriminate.Assay, ArcAssay.toJsonString 0 assay))
        | ArcFiles.Run run ->
            Some(PreviewData.ArcFileData(ArcFilesDiscriminate.Run, ArcRun.toJsonString 0 run))
        | ArcFiles.Workflow workflow ->
            Some(PreviewData.ArcFileData(ArcFilesDiscriminate.Workflow, ArcWorkflow.toJsonString 0 workflow))
        | ArcFiles.DataMap _
        | ArcFiles.Template _ ->
            None
