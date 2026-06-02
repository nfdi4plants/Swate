module Main.IPC.Rename

open System
open Fable.Core
open ARCtrl
open Main.ArcVaultHelper
open Main.IPC.Delete
open Main.IPC.FileSystemIO
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules

[<RequireQualifiedAccess>]
module ArcRenameHelper =

    let private tryRenameEntityOnDiskAsync
        arcPath
        fileType
        oldIdentifier
        newIdentifier
        (arc: ARC)
        =
        match fileType with
        | ArcFilesDiscriminate.Assay ->
            arc.TryRenameAssayAsync(arcPath, oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Study ->
            arc.TryRenameStudyAsync(arcPath, oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Workflow ->
            arc.TryRenameWorkflowAsync(arcPath, oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Run ->
            arc.TryRenameRunAsync(arcPath, oldIdentifier, newIdentifier)
        | fileType -> promise { return Error [| $"Renaming {fileType} is not supported." |] }

    let private applyInMemoryRename fileType oldIdentifier newIdentifier (arc: ARC) =
        match fileType with
        | ArcFilesDiscriminate.Assay -> arc.RenameAssay(oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Study -> arc.RenameStudy(oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Workflow -> arc.RenameWorkflow(oldIdentifier, newIdentifier)
        | ArcFilesDiscriminate.Run -> arc.RenameRun(oldIdentifier, newIdentifier)
        | fileType -> failwith $"Renaming {fileType} is not supported."

    let private remapArcFileSystemPaths sourcePath targetPath (arc: ARC) =
        let normalizedSourcePath = sourcePath |> PathHelpers.normalizeCanonicalRelativePath
        let normalizedTargetPath = targetPath |> PathHelpers.normalizeCanonicalRelativePath
        let sourcePrefix = normalizedSourcePath + "/"

        let remappedPaths =
            arc.FileSystem.Tree.ToFilePaths()
            |> Array.map (fun path ->
                let normalizedPath = path |> PathHelpers.normalizeCanonicalRelativePath

                if PathHelpers.pathsEqual normalizedPath normalizedSourcePath then
                    normalizedTargetPath
                elif normalizedPath.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase) then
                    normalizedTargetPath + normalizedPath.Substring(normalizedSourcePath.Length)
                else
                    normalizedPath
            )

        arc.SetFilePaths(remappedPaths)

    let private mergeRenamedEntityFromDisk
        arcPath
        sourcePath
        targetPath
        fileType
        oldIdentifier
        newIdentifier
        (arcLocal: ARC)
        =
        promise {
            match! ARC.tryLoadAsync arcPath with
            | Error errors ->
                return
                    Error(
                        exn
                            $"Renamed ARC entity, but could not reload the ARC from disk: {PathHelpers.formatContractErrors errors}"
                    )
            | Ok persistedArc ->
                baselineArcStaticHashes persistedArc
                let renamedArc = copyArcPreservingStaticHashes arcLocal
                applyInMemoryRename fileType oldIdentifier newIdentifier renamedArc
                remapArcFileSystemPaths sourcePath targetPath renamedArc
                syncArcStaticHashes persistedArc renamedArc
                return Ok renamedArc
        }

    let private renameResolvedArcEntityAsync
        (arcPath: string)
        sourcePath
        targetPath
        canonicalSourcePath
        fileType
        oldIdentifier
        newIdentifier
        (arcLocal: ARC)
        : JS.Promise<Result<ARC, exn>> =
        promise {
            try
                match tryResolveArcRelativePath arcPath targetPath with
                | Error pathError -> return Error pathError
                | Ok targetAbsolutePath ->
                    let! targetExists = pathExistsAsync targetAbsolutePath

                    if targetExists then
                        return
                            Error(
                                exn
                                    $"Cannot rename '{sourcePath}' to '{targetPath}' because the destination already exists."
                            )
                    else
                        match! ARC.tryLoadAsync arcPath with
                        | Error errors ->
                            return
                                Error(
                                    exn
                                        $"Could not load ARC from disk before renaming '{sourcePath}': {PathHelpers.formatContractErrors errors}"
                                )
                        | Ok diskArc ->
                            match ArcDeleteHelper.tryEnsureArcEntityResolved fileType oldIdentifier canonicalSourcePath diskArc with
                            | Error resolutionError -> return Error resolutionError
                            | Ok() ->
                                match!
                                    tryRenameEntityOnDiskAsync
                                        arcPath
                                        fileType
                                        oldIdentifier
                                        newIdentifier
                                        diskArc
                                with
                                | Error errors ->
                                    return
                                        Error(
                                            exn
                                                $"Could not rename ARC entity from '{sourcePath}' to '{targetPath}': {PathHelpers.formatContractErrors errors}"
                                        )
                                | Ok _ ->
                                    return!
                                        mergeRenamedEntityFromDisk
                                            arcPath
                                            sourcePath
                                            targetPath
                                            fileType
                                            oldIdentifier
                                            newIdentifier
                                            arcLocal
            with renameError ->
                let mappedError =
                    mapRenameDiskError sourcePath targetPath renameError

                return
                    Error(
                        exn
                            $"Could not rename ARC entity from '{sourcePath}' to '{targetPath}': {mappedError.Message}"
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

    /// Performs the ARCtrl entity rename contract for an entity-folder rename request.
    let renameArcEntityAsync (arcPath: string) (request: RenamePathRequest) (arcLocal: ARC) : JS.Promise<Result<ARC, exn>> =
        promise {
            let requestedRelativePath = request.relativePath |> PathHelpers.normalizeCanonicalRelativePath
            let sourceClassification = ArcDeletePathRules.classifyRenameTarget requestedRelativePath

            match validateEntityRenameSourceClassification sourceClassification with
            | Error validationError -> return Error validationError
            | Ok(sourceZone, sourceIdentifier, sourcePath) ->
                let sourceFileType = ArcDeleteHelper.arcFileTypeForZone sourceZone
                let canonicalSourcePath = ArcDeleteHelper.canonicalEntityFilePath sourceZone sourceIdentifier

                match ArcDeleteHelper.tryEnsureArcEntityResolved sourceFileType sourceIdentifier canonicalSourcePath arcLocal with
                | Error resolutionError -> return Error resolutionError
                | Ok() ->
                    match tryBuildRenameTargetPath sourcePath request.newName with
                    | Error targetPathError -> return Error(exn targetPathError)
                    | Ok targetPath ->
                        let targetIdentifier = PathHelpers.getNameFromPath targetPath

                        return!
                            renameResolvedArcEntityAsync
                                arcPath
                                sourcePath
                                targetPath
                                canonicalSourcePath
                                sourceFileType
                                sourceIdentifier
                                targetIdentifier
                                arcLocal
        }
