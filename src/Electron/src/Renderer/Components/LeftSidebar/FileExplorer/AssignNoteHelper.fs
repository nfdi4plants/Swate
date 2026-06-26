module Renderer.Components.LeftSidebar.FileExplorer.FileTreeAssignNoteHelper

open System
open Fable.Core
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper
open Renderer.Components.LeftSidebar.FileExplorer.Types

type AssignNoteConfig = {
    closeDialog: unit -> unit
    setIsAssigning: bool -> unit
    refreshGitStatus: unit -> unit
    copyFileSystemItem: CopyFileSystemItemRequest -> JS.Promise<Result<unit, exn>>
    movePath: MovePathRequest -> JS.Promise<Result<unit, exn>>
    enqueueError: ErrorModalRequest -> unit
}

let enqueueAssignNoteError (enqueueError: ErrorModalRequest -> unit) errorMessage =
    enqueueError (ErrorModalRequest.create (errorMessage, title = "Could not assign note"))

let private assignableTargetKinds = [ NotesTargetKind.Study; NotesTargetKind.Assay ]

let private combineRelativePaths (paths: string[]) =
    ARCtrl.ArcPathHelper.combineMany paths
    |> PathHelpers.normalizeCanonicalRelativePath

let private tryGetTargetKindFromRootFolder rootFolder =
    assignableTargetKinds
    |> List.tryFind (fun kind ->
        let targetFolder, _ = NoteConversion.existingTargetFolders kind
        PathHelpers.pathsEqual rootFolder targetFolder
    )

let private tryCreateNoteAssignmentTarget kind entityName =
    if String.IsNullOrWhiteSpace entityName then
        None
    else
        Some { Name = entityName; Kind = kind }

let tryGetNoteAssignmentTarget (item: FileItem) =
    if not item.IsDirectory then
        None
    else
        item.Path
        |> Option.map PathHelpers.normalizeCanonicalRelativePath
        |> Option.bind (fun path ->
            match getNonEmptyPathParts path with
            | [| root; entityName |] ->
                root
                |> tryGetTargetKindFromRootFolder
                |> Option.bind (fun kind -> tryCreateNoteAssignmentTarget kind entityName)
            | _ -> None
        )

let canAssignNoteToItem item =
    tryGetNoteAssignmentTarget item |> Option.isSome

let private markdownExtension = ".md"

let private withoutMarkdownExtension (fileName: string) =
    if fileName.EndsWith(markdownExtension, StringComparison.OrdinalIgnoreCase) then
        fileName.Substring(0, fileName.Length - markdownExtension.Length)
    else
        fileName

let private tryGetRootNoteFromFilePath (relativePath: string) =
    let normalizedPath = PathHelpers.normalizeCanonicalRelativePath relativePath

    if
        normalizedPath.EndsWith(markdownExtension, StringComparison.OrdinalIgnoreCase)
        |> not
    then
        None
    else
        match getNonEmptyPathParts normalizedPath with
        | [| root; dateFolder; noteFolderName; fileName |] when
            PathHelpers.pathsEqual root NoteConversion.notesRootFolder
            ->
            if PathHelpers.pathsEqual noteFolderName (withoutMarkdownExtension fileName) then
                Some {
                    SourceFolderPath =
                        combineRelativePaths [|
                            NoteConversion.notesRootFolder
                            dateFolder
                            noteFolderName
                        |]
                    NoteFolderName = noteFolderName
                    Label = $"{dateFolder} / {noteFolderName}"
                }
            else
                None
        | _ -> None

let createAssignableNoteOptions (fileEntries: FileEntry seq) =
    fileEntries
    |> Seq.choose (fun entry ->
        if entry.isDirectory then
            None
        else
            tryGetRootNoteFromFilePath entry.path
    )
    |> Seq.distinctBy (fun note -> PathHelpers.normalizeForComparison note.SourceFolderPath)
    |> Seq.sortBy (fun note -> note.Label.ToLowerInvariant())
    |> ResizeArray

let private tryGetRelativePathFromParent parentPath childPath =
    let normalizedParentPath = PathHelpers.normalizeCanonicalRelativePath parentPath
    let normalizedChildPath = PathHelpers.normalizeCanonicalRelativePath childPath

    if
        PathHelpers.isSameOrDescendantPath normalizedChildPath normalizedParentPath
        |> not
    then
        None
    else
        let parentSegments = getNonEmptyPathParts normalizedParentPath
        let childSegments = getNonEmptyPathParts normalizedChildPath

        if childSegments.Length <= parentSegments.Length then
            None
        else
            childSegments |> Array.skip parentSegments.Length |> String.concat "/" |> Some

let createAssignableNoteAssetOptions (fileEntries: FileEntry seq) (note: AssignableNoteRef option) =
    match note with
    | None -> ResizeArray()
    | Some note ->
        let assetFolderPath =
            combineRelativePaths [|
                note.SourceFolderPath
                NoteConversion.noteAssetsFolderName
            |]

        fileEntries
        |> Seq.choose (fun entry ->
            if entry.isDirectory then
                None
            else
                entry.path
                |> tryGetRelativePathFromParent assetFolderPath
                |> Option.map (fun relativeAssetPath -> {
                    SourceRelativePath = entry.path |> PathHelpers.normalizeCanonicalRelativePath
                    RelativeAssetPath = relativeAssetPath
                })
        )
        |> Seq.distinctBy (fun asset -> PathHelpers.normalizeForComparison asset.SourceRelativePath)
        |> Seq.sortBy (fun asset -> asset.RelativeAssetPath.ToLowerInvariant())
        |> ResizeArray

let private targetAddZone =
    function
    | NotesTargetKind.Study -> ArcEntityPathRules.AddZone.Studies
    | NotesTargetKind.Assay -> ArcEntityPathRules.AddZone.Assays

let private targetChildFolderName targetKind destination =
    match destination with
    | AssignNoteAssetDestination.Protocol ->
        let _, protocolsFolder = NoteConversion.existingTargetFolders targetKind
        protocolsFolder
    | AssignNoteAssetDestination.Dataset -> ARCtrl.ArcPathHelper.AssayDatasetFolderName
    | AssignNoteAssetDestination.Resource -> ARCtrl.ArcPathHelper.StudiesResourcesFolderName

let assetDestinationExistsForTarget (target: ExistingTargetRef) destination =
    let childFolderName = targetChildFolderName target.Kind destination

    ArcEntityPathRules.nativeEntityChildFolderNames (targetAddZone target.Kind)
    |> List.exists (fun folderName -> PathHelpers.pathsEqual folderName childFolderName)

let assignableAssetDestinationsForTarget target =
    [
        AssignNoteAssetDestination.Protocol
        AssignNoteAssetDestination.Dataset
        AssignNoteAssetDestination.Resource
    ]
    |> List.filter (assetDestinationExistsForTarget target)

let createDefaultAssetDestinations
    (availableDestinations: AssignNoteAssetDestination list)
    (assets: seq<AssignableNoteAssetRef>)
    =
    match availableDestinations |> List.tryHead with
    | None -> Map.empty
    | Some defaultDestination ->
        assets
        |> Seq.map (fun asset -> asset.SourceRelativePath, defaultDestination)
        |> Map.ofSeq

let private targetRootPath (target: ExistingTargetRef) =
    let targetFolder, _ = NoteConversion.existingTargetFolders target.Kind
    combineRelativePaths [| targetFolder; target.Name |]

let buildAssignedNoteFolderPath (target: ExistingTargetRef) (noteFolderName: string) =
    NoteConversion.mkExistingTargetRelativePath target noteFolderName
    |> Option.bind NoteConversion.tryGetNoteFolderRelativePath
    |> Option.defaultWith (fun () ->
        let _, protocolsFolder = NoteConversion.existingTargetFolders target.Kind

        combineRelativePaths [| targetRootPath target; protocolsFolder; noteFolderName |]
    )
    |> PathHelpers.normalizeCanonicalRelativePath

let buildAssignedAssetTargetPath
    (target: ExistingTargetRef)
    (note: AssignableNoteRef)
    (asset: AssignableNoteAssetRef)
    destination
    =
    let assetFolderPath =
        match destination with
        | AssignNoteAssetDestination.Protocol -> buildAssignedNoteFolderPath target note.NoteFolderName
        | AssignNoteAssetDestination.Dataset ->
            combineRelativePaths [|
                targetRootPath target
                ARCtrl.ArcPathHelper.AssayDatasetFolderName
                note.NoteFolderName
            |]
        | AssignNoteAssetDestination.Resource ->
            combineRelativePaths [|
                targetRootPath target
                ARCtrl.ArcPathHelper.StudiesResourcesFolderName
                note.NoteFolderName
            |]

    combineRelativePaths [|
        assetFolderPath
        NoteConversion.noteAssetsFolderName
        asset.RelativeAssetPath
    |]

let private moveAssignedAssets config target note targetFolderPath assets assetDestinations =
    let rec moveNext assets = promise {
        match assets with
        | [] -> return Ok()
        | asset :: remainingAssets ->
            match assetDestinations |> Map.tryFind asset.SourceRelativePath with
            | None
            | Some AssignNoteAssetDestination.Protocol -> return! moveNext remainingAssets
            | Some destination when assetDestinationExistsForTarget target destination |> not ->
                return! moveNext remainingAssets
            | Some destination ->
                let sourcePath =
                    combineRelativePaths [|
                        targetFolderPath
                        NoteConversion.noteAssetsFolderName
                        asset.RelativeAssetPath
                    |]

                let targetPath = buildAssignedAssetTargetPath target note asset destination

                let! moveResult =
                    config.movePath {
                        sourceRelativePath = sourcePath
                        targetRelativePath = targetPath
                        overwrite = false
                    }

                match moveResult with
                | Ok() -> return! moveNext remainingAssets
                | Error moveError -> return Error moveError
    }

    moveNext assets

let assignNoteToTarget
    (config: AssignNoteConfig)
    (target: ExistingTargetRef)
    (note: AssignableNoteRef)
    (assets: AssignableNoteAssetRef list)
    (assetDestinations: Map<string, AssignNoteAssetDestination>)
    =
    let targetFolderPath = buildAssignedNoteFolderPath target note.NoteFolderName

    if PathHelpers.pathsEqual note.SourceFolderPath targetFolderPath then
        config.closeDialog ()
    else
        config.setIsAssigning true

        promise {
            let! copyResult =
                config.copyFileSystemItem {
                    sourceRelativePath = note.SourceFolderPath
                    targetRelativePath = targetFolderPath
                    overwrite = false
                }

            match copyResult with
            | Error copyError -> enqueueAssignNoteError config.enqueueError copyError.Message
            | Ok() ->
                match! moveAssignedAssets config target note targetFolderPath assets assetDestinations with
                | Error assetMoveError -> enqueueAssignNoteError config.enqueueError assetMoveError.Message
                | Ok() ->
                    config.refreshGitStatus ()
                    config.closeDialog ()
        }
        |> Promise.catch (fun error -> enqueueAssignNoteError config.enqueueError error.Message)
        |> Promise.map (fun _ -> config.setIsAssigning false)
        |> Promise.start
