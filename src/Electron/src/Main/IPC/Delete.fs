module Main.IPC.Delete

open Fable.Core
open ARCtrl
open ARCtrl.Contract
open Main.ArcMerge
open Swate.Components.Shared
open ARC

[<RequireQualifiedAccess>]
module ArcDeleteHelper =

    let arcFileTypeForZone =
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

    let private removeFromDiskAsync fileType identifier arcPath (arc: ARC) =
        match fileType with
        | ArcFilesDiscriminate.Assay -> arc.TryRemoveAssayAsync(arcPath, identifier)
        | ArcFilesDiscriminate.Study -> arc.TryRemoveStudyAsync(arcPath, identifier)
        | ArcFilesDiscriminate.Workflow -> arc.TryRemoveWorkflowAsync(arcPath, identifier)
        | ArcFilesDiscriminate.Run -> arc.TryRemoveRunAsync(arcPath, identifier)
        | _ -> promise { return Error [| $"Deleting {fileType} is not supported." |] }

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

    let tryEnsureArcEntityResolved fileType identifier relativePath (arc: ARC) =
        match arc.TryArcFileByPath(relativePath) with
        | Some arcFile when arcFileMatchesEntity fileType identifier arcFile -> Ok()
        | _ ->
            Error(
                exn
                    $"ARC does not contain {entityKindForFileType fileType} with identifier '{identifier}'."
            )

    let canonicalEntityFilePath zone identifier =
        ArcDeletePathRules.buildCanonicalEntityPaths zone identifier
        |> List.head

    let private mergeDeletedEntityFromDisk arcPath canonicalFilePath (arcLocal: ARC) = promise {
        match! ARC.tryLoadAsync arcPath with
        | Error errors ->
            return
                Error(
                    exn
                        $"Deleted ARC entity, but could not reload the ARC from disk: {PathHelpers.formatContractErrors errors}"
                )
        | Ok diskArc ->
            // The IPC delete path suppresses watcher ARC merges, so apply the same unlink event explicitly.
            return
                Ok(
                    ARC.merge
                        arcLocal
                        diskArc
                        [
                            {
                                EventName = EventName.Unlink
                                Path = canonicalFilePath
                            }
                        ]
                )
    }

    let private tryGetEntityDeleteTarget relativePath =
        match ArcDeletePathRules.classifyDeleteTarget relativePath with
        | ArcDeletePathRules.DeletePathClassification.EntityFolderTarget(zone, identifier, normalizedRelativePath) ->
            Ok(arcFileTypeForZone zone, identifier, normalizedRelativePath, canonicalEntityFilePath zone identifier)
        | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
            ArcDeletePathRules.CanonicalArcFileTarget.EntityFile(zone, identifier),
            normalizedRelativePath
          ) ->
            Ok(arcFileTypeForZone zone, identifier, normalizedRelativePath, normalizedRelativePath)
        | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
            ArcDeletePathRules.CanonicalArcFileTarget.DataMapFile _,
            _
          )
        | ArcDeletePathRules.DeletePathClassification.AddZoneDescendantTarget _
        | ArcDeletePathRules.DeletePathClassification.GenericTarget _ ->
            Error(exn "Generic filesystem delete paths do not use ARC entity delete contracts.")
        | ArcDeletePathRules.DeletePathClassification.CanonicalFileTarget(
            ArcDeletePathRules.CanonicalArcFileTarget.InvestigationFile,
            _
          ) ->
            Error(exn "Deleting the investigation file is not supported.")
        | ArcDeletePathRules.DeletePathClassification.ProtectedTarget _ ->
            Error(exn "Deleting protected files (for example .gitkeep or readme.md) is not allowed.")
        | ArcDeletePathRules.DeletePathClassification.DisallowedTarget _ ->
            Error(exn "Deletion is not allowed for this path.")

    /// Deletes ARC entities through ARCtrl contracts while preserving unrelated dirty in-memory edits.
    let deleteArcEntityAsync (arcPath: string) (relativePath: string) (arc: ARC) : JS.Promise<Result<ARC, exn>> =
        promise {
            match tryGetEntityDeleteTarget relativePath with
            | Error validationError -> return Error validationError
            | Ok(fileType, identifier, normalizedRelativePath, canonicalFilePath) ->
                try
                    match! ARC.tryLoadAsync arcPath with
                    | Error errors ->
                        return
                            Error(
                                exn
                                    $"Could not load ARC from disk before deleting '{normalizedRelativePath}': {PathHelpers.formatContractErrors errors}"
                            )
                    | Ok diskArc ->
                        match tryEnsureArcEntityResolved fileType identifier canonicalFilePath diskArc with
                        | Error resolutionError -> return Error resolutionError
                        | Ok() ->
                            match! removeFromDiskAsync fileType identifier arcPath diskArc with
                            | Error errors ->
                                return
                                    Error(
                                        exn
                                            $"Could not delete ARC entity at '{normalizedRelativePath}': {PathHelpers.formatContractErrors errors}"
                                    )
                            | Ok _ ->
                                return! mergeDeletedEntityFromDisk arcPath canonicalFilePath arc
                with deleteError ->
                    return
                        Error(
                            exn
                                $"Could not delete ARC entity at '{normalizedRelativePath}': {deleteError.Message}"
                        )
        }
