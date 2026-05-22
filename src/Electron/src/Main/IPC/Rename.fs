module Main.IPC.Rename

open Fable.Core
open ARCtrl
open Main.ArcVaultHelper
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules
open ARC

[<RequireQualifiedAccess>]
module ArcRenameHelper =

    let private formatContractErrors (errors: string[]) =
        errors |> Array.map string |> String.concat "\n"

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

    let private canonicalEntityFileNameForFileType =
        function
        | ArcFilesDiscriminate.Assay -> ARCtrl.ArcPathHelper.AssayFileName
        | ArcFilesDiscriminate.Study -> ARCtrl.ArcPathHelper.StudyFileName
        | ArcFilesDiscriminate.Workflow -> ARCtrl.ArcPathHelper.WorkflowFileName
        | ArcFilesDiscriminate.Run -> ARCtrl.ArcPathHelper.RunFileName
        | fileType -> failwith $"Renaming {fileType} is not supported."

    let private canonicalEntityFilePathForRenamePlan (renamePlan: RenamePlan) =
        let fileName = canonicalEntityFileNameForFileType renamePlan.SyncPlan.FileType
        $"{renamePlan.SourcePath}/{fileName}"

    let private renameOnDiskAsync fileType oldIdentifier newIdentifier arcPath (arc: ARC) =
        match fileType with
        | ArcFilesDiscriminate.Assay -> arc.TryRenameAssayAsync(arcPath, oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Study -> arc.TryRenameStudyAsync(arcPath, oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Workflow -> arc.TryRenameWorkflowAsync(arcPath, oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Run -> arc.TryRenameRunAsync(arcPath, oldIdentifier, newIdentifier)
        | fileType -> promise { return Error [| $"Renaming {fileType} is not supported." |] }

    let private applyIdentifierRename (syncPlan: IdentifierRenameSyncPlan) (arc: ARC) =
        match syncPlan.FileType with
        | ArcFilesDiscriminate.Assay -> arc.RenameAssay(syncPlan.OldIdentifier, syncPlan.NewIdentifier)
        | ArcFilesDiscriminate.Study -> arc.RenameStudy(syncPlan.OldIdentifier, syncPlan.NewIdentifier)
        | ArcFilesDiscriminate.Workflow -> arc.RenameWorkflow(syncPlan.OldIdentifier, syncPlan.NewIdentifier)
        | ArcFilesDiscriminate.Run -> arc.RenameRun(syncPlan.OldIdentifier, syncPlan.NewIdentifier)
        | fileType -> failwith $"Renaming {fileType} is not supported."

    let private remapArcFileSystemPaths (sourcePath: string) (targetPath: string) (arc: ARC) =
        let normalizedSourcePath = normalizeRelativePathForComparison sourcePath
        let normalizedTargetPath = normalizeRelativePathForComparison targetPath

        let remapPath (path: string) =
            let normalizedPath = normalizeRelativePathForComparison path

            if PathHelpers.pathsEqual normalizedPath normalizedSourcePath then
                normalizedTargetPath
            elif PathHelpers.isSameOrDescendantPath normalizedPath normalizedSourcePath then
                let suffix = normalizedPath.Substring(normalizedSourcePath.Length)
                $"{normalizedTargetPath}{suffix}"
            else
                normalizedPath

        arc.FileSystem.Tree.ToFilePaths()
        |> Array.map remapPath
        |> Array.distinctBy PathHelpers.normalizeForComparison
        |> arc.SetFilePaths

    let private applyRenameToInMemoryArc (renamePlan: RenamePlan) (persistedArc: ARC) (arcLocal: ARC) =
        let renamedLocalArc = copyArcPreservingStaticHashes arcLocal
        applyIdentifierRename renamePlan.SyncPlan renamedLocalArc
        remapArcFileSystemPaths renamePlan.SourcePath renamePlan.TargetPath renamedLocalArc
        syncArcStaticHashes persistedArc renamedLocalArc
        renamedLocalArc

    let private mergeRenamedEntityFromDisk arcPath renamePlan arcLocal = promise {
        match! ARC.tryLoadAsync arcPath with
        | Error loadErrors ->
            return
                Error(
                    exn
                        $"Renamed ARC entity from '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}', but could not reload the persisted ARC: {formatContractErrors loadErrors}"
                )
        | Ok persistedArc ->
            baselineArcStaticHashes persistedArc
            return Ok(applyRenameToInMemoryArc renamePlan persistedArc arcLocal)
    }

    /// Performs the ARCtrl entity rename against a clean disk ARC, then applies the rename to the local ARC.
    /// This keeps unrelated dirty in-memory edits from being flushed as part of the rename contracts.
    let renameArcEntityAsync (arcPath: string) (renamePlan: RenamePlan) (arcLocal: ARC) : JS.Promise<Result<ARC, exn>> =
        let syncPlan = renamePlan.SyncPlan
        let canonicalFilePath = canonicalEntityFilePathForRenamePlan renamePlan

        promise {
            try
                match! ARC.tryLoadAsync arcPath with
                | Error loadErrors ->
                    return
                        Error(
                            exn
                                $"Could not load ARC from disk before renaming '{renamePlan.SourcePath}': {formatContractErrors loadErrors}"
                        )
                | Ok diskArc ->
                    baselineArcStaticHashes diskArc

                    match tryEnsureArcEntityResolved syncPlan.FileType syncPlan.OldIdentifier canonicalFilePath diskArc with
                    | Error resolutionError -> return Error resolutionError
                    | Ok() ->
                        match!
                            renameOnDiskAsync
                                syncPlan.FileType
                                syncPlan.OldIdentifier
                                syncPlan.NewIdentifier
                                arcPath
                                diskArc
                        with
                        | Error resolutionError ->
                            return
                                Error(
                                    exn
                                        $"Could not rename ARC entity from '{renamePlan.SourcePath}' to '{renamePlan.TargetPath}': {formatContractErrors resolutionError}"
                                )
                        | Ok _ -> return! mergeRenamedEntityFromDisk arcPath renamePlan arcLocal
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
