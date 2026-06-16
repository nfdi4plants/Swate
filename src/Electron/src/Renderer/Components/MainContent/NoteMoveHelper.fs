module Renderer.Components.MainContent.NoteMoveHelper

open System
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor


type ExistingTargetNoteFolderMove = {
    SourceFolderPath: string
    TargetFolderPath: string
}

type ExistingTargetNoteMovePlan = {
    SourcePath: string
    TargetPath: string
    FolderMove: ExistingTargetNoteFolderMove option
}

[<RequireQualifiedAccess>]
type ExistingTargetNoteMovePlanResult =
    | Ready of ExistingTargetNoteMovePlan
    | TargetConflict of ExistingTargetNoteMovePlan

let private isAssignableMarkdownPath (path: string) =
    let normalizedPath = PathHelpers.normalizePath path

    normalizedPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
    && (normalizedPath.StartsWith("notes/", StringComparison.OrdinalIgnoreCase)
        || normalizedPath.StartsWith("studies/", StringComparison.OrdinalIgnoreCase)
        || normalizedPath.StartsWith("assays/", StringComparison.OrdinalIgnoreCase))

let private tryResolveTargetPath (markdown: string) (targetRef: ExistingTargetRef) =
    match NoteConversion.tryDecodeMarkdownFrontmatter markdown with
    | None -> Error "Add to existing Assay/Study requires note frontmatter with title and date."
    | Some(frontmatter, _) ->
        match Validation.sanitizeProtocolName frontmatter.Title with
        | None -> Error "Title is invalid for protocol naming. Choose a different title."
        | Some protocolName ->
            match NoteConversion.mkExistingTargetRelativePath targetRef protocolName with
            | None -> Error "Could not resolve a safe target path."
            | Some targetPath -> Ok targetPath

let private tryBuildFolderMove sourcePath targetPath =
    let expectedMarkdownFileName folderPath =
        $"{PathHelpers.getFileName folderPath}.md"

    match
        NoteConversion.tryGetNoteFolderRelativePath sourcePath, NoteConversion.tryGetNoteFolderRelativePath targetPath
    with
    | Some sourceFolderPath, Some targetFolderPath when
        PathHelpers.pathsEqual (PathHelpers.getFileName sourcePath) (expectedMarkdownFileName sourceFolderPath)
        && PathHelpers.pathsEqual (PathHelpers.getFileName targetPath) (expectedMarkdownFileName targetFolderPath)
        ->
        Some {
            SourceFolderPath = sourceFolderPath
            TargetFolderPath = targetFolderPath
        }
    | _ -> None

let movePlanConflictPath plan =
    plan.FolderMove
    |> Option.map _.TargetFolderPath
    |> Option.defaultValue plan.TargetPath

let private planTargetExistsInSnapshot existingPaths plan =
    PathHelpers.pathExistsInSnapshot existingPaths plan.TargetPath
    || PathHelpers.pathExistsInSnapshot existingPaths (movePlanConflictPath plan)

let tryBuildMoveToExistingTargetPlan
    (selectedPath: string option)
    (markdown: string)
    (targetRef: ExistingTargetRef)
    (existingPaths: string seq)
    =
    match selectedPath |> Option.map PathHelpers.normalizePath with
    | None -> Error "No note file is selected."
    | Some sourcePath when not (isAssignableMarkdownPath sourcePath) ->
        Error
            "Only markdown note or protocol files inside notes, studies, or assays can be added to an existing Study or Assay."
    | Some sourcePath ->
        match tryResolveTargetPath markdown targetRef with
        | Error errorMessage -> Error errorMessage
        | Ok preferredTargetPath when PathHelpers.pathsEqual sourcePath preferredTargetPath ->
            Error "The selected note is already in this Study or Assay."
        | Ok preferredTargetPath ->
            let plan = {
                SourcePath = sourcePath
                TargetPath = preferredTargetPath
                FolderMove = tryBuildFolderMove sourcePath preferredTargetPath
            }

            if planTargetExistsInSnapshot existingPaths plan then
                Ok(ExistingTargetNoteMovePlanResult.TargetConflict plan)
            else
                Ok(ExistingTargetNoteMovePlanResult.Ready plan)
