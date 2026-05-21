module Main.IPC.Rename

open Fable.Core
open ARCtrl
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules
open ARC

[<RequireQualifiedAccess>]
module ArcRenameHelper =

    type IdentifierRenameSyncPlan = {
        FileType: ArcFilesDiscriminate
        OldIdentifier: string
        NewIdentifier: string
    }

    type RenamePlan = {
        SourcePath: string
        TargetPath: string
        SyncPlan: IdentifierRenameSyncPlan
    }

    let private normalizeRelativePathForComparison (path: string) =
        path
        |> PathHelpers.normalizeCanonicalRelativePath

    let private arcFileTypeForZone =
        function
        | ArcDeletePathRules.AddZone.Assays -> ArcFilesDiscriminate.Assay
        | ArcDeletePathRules.AddZone.Studies -> ArcFilesDiscriminate.Study
        | ArcDeletePathRules.AddZone.Workflows -> ArcFilesDiscriminate.Workflow
        | ArcDeletePathRules.AddZone.Runs -> ArcFilesDiscriminate.Run

    let private entityKindForFileType =
        function
        | ArcFilesDiscriminate.Assay -> "assay"
        | ArcFilesDiscriminate.Study -> "study"
        | ArcFilesDiscriminate.Workflow -> "workflow"
        | ArcFilesDiscriminate.Run -> "run"
        | fileType -> string fileType

    let private arcFileMatchesEntity fileType identifier =
        function
        | ArcFiles.Assay assay ->
            fileType = ArcFilesDiscriminate.Assay
            && PathHelpers.pathsEqual assay.Identifier identifier
        | ArcFiles.Study(study, _) ->
            fileType = ArcFilesDiscriminate.Study
            && PathHelpers.pathsEqual study.Identifier identifier
        | ArcFiles.Workflow workflow ->
            fileType = ArcFilesDiscriminate.Workflow
            && PathHelpers.pathsEqual workflow.Identifier identifier
        | ArcFiles.Run run ->
            fileType = ArcFilesDiscriminate.Run
            && PathHelpers.pathsEqual run.Identifier identifier
        | ArcFiles.Investigation _
        | ArcFiles.DataMap _
        | ArcFiles.Template _ -> false

    let private tryEnsureArcEntityResolved fileType identifier relativePath (arc: ARC) =
        match arc.TryArcFileByPath(relativePath) with
        | Some arcFile when arcFileMatchesEntity fileType identifier arcFile -> Ok()
        | _ ->
            Error(
                exn
                    $"ARC does not contain {entityKindForFileType fileType} with identifier '{identifier}'."
            )

    let private canonicalEntityFilePath zone identifier =
        ArcDeletePathRules.buildCanonicalEntityPaths zone identifier
        |> List.head

    /// Performs the ARCtrl entity rename contract matching the plan's ARC file type.
    let renameArcEntityAsync (arcPath: string) (renamePlan: RenamePlan) (arc: ARC) : JS.Promise<Result<ARC, exn>> =
        let syncPlan = renamePlan.SyncPlan

        let renameAsync =
            match syncPlan.FileType with
            | ArcFilesDiscriminate.Assay ->
                arc.RenameAssayAsync(arcPath, syncPlan.OldIdentifier, syncPlan.NewIdentifier)
            | ArcFilesDiscriminate.Study ->
                arc.RenameStudyAsync(arcPath, syncPlan.OldIdentifier, syncPlan.NewIdentifier)
            | ArcFilesDiscriminate.Workflow ->
                arc.RenameWorkflowAsync(arcPath, syncPlan.OldIdentifier, syncPlan.NewIdentifier)
            | ArcFilesDiscriminate.Run ->
                arc.RenameRunAsync(arcPath, syncPlan.OldIdentifier, syncPlan.NewIdentifier)
            | fileType -> promise { return failwith $"Renaming {fileType} is not supported." }

        promise {
            try
                do! renameAsync
                return Ok arc
            with renameError ->
                return
                    Error(
                        exn
                            $"Could not rename ARC entity from '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}': {renameError.Message}"
                    )
        }

    let private validateEntityRenameSourceClassification (classification: ArcDeletePathRules.RenamePathClassification) =
        match classification with
        | ArcDeletePathRules.RenamePathClassification.EntityFolderTarget(zone, identifier, normalizedRelativePath) ->
            Ok(zone, identifier, normalizedRelativePath)
        | ArcDeletePathRules.RenamePathClassification.RootTarget -> Error(exn "Renaming the ARC root is not allowed.")
        | ArcDeletePathRules.RenamePathClassification.DisallowedTarget _ ->
            Error(exn "Rename path must not contain path traversal segments.")
        | ArcDeletePathRules.RenamePathClassification.ProtectedTarget _ ->
            Error(exn "Renaming protected files (for example .gitkeep or readme.md) is not allowed.")
        | ArcDeletePathRules.RenamePathClassification.InvestigationFileTarget _ ->
            Error(exn "Renaming the investigation file is not supported.")
        | ArcDeletePathRules.RenamePathClassification.AddZoneRootTarget _ ->
            Error(exn "Renaming add-zone root folders (studies/, assays/, workflows/, runs/) is not allowed.")
        | ArcDeletePathRules.RenamePathClassification.CanonicalEntityFileTarget _
        | ArcDeletePathRules.RenamePathClassification.CanonicalDataMapFileTarget _ ->
            Error(exn "Renaming canonical ARC files is not supported. Rename the containing ARC entity folder instead.")
        | ArcDeletePathRules.RenamePathClassification.GenericTarget _ ->
            Error(exn "Renaming generic files or folders uses the generic filesystem rename path.")

    let tryBuildRenamePlan (arc: ARC) (request: RenamePathRequest) : Result<RenamePlan, exn> =
        let requestedRelativePath = normalizeRelativePathForComparison request.relativePath
        let sourceClassification = ArcDeletePathRules.classifyRenameTarget requestedRelativePath

        match validateEntityRenameSourceClassification sourceClassification with
        | Error validationError -> Error validationError
        | Ok(sourceZone, sourceIdentifier, sourcePath) ->
            let sourceFileType = arcFileTypeForZone sourceZone

            match tryEnsureArcEntityResolved sourceFileType sourceIdentifier (canonicalEntityFilePath sourceZone sourceIdentifier) arc with
            | Error resolutionError -> Error resolutionError
            | Ok() ->
                match tryBuildRenameTargetPath sourcePath request.newName with
                | Error targetPathError -> Error(exn targetPathError)
                | Ok targetPath ->
                    let targetIdentifier = PathHelpers.getNameFromPath targetPath

                    Ok {
                        SourcePath = sourcePath
                        TargetPath = targetPath
                        SyncPlan = {
                            FileType = sourceFileType
                            OldIdentifier = sourceIdentifier
                            NewIdentifier = targetIdentifier
                        }
                    }
