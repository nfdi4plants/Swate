module Renderer.Components.MainContent.NoteMoveHelper

open System
open Swate.Components.Shared
open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.FileIOHelper

type ExistingTargetNoteMovePlan = {
    SourcePath: string
    TargetPath: string
    Request: FileContentDTO
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

let tryBuildMoveToExistingTargetPlan
    (selectedPath: string option)
    (markdown: string)
    (targetRef: ExistingTargetRef)
    (existingPaths: string seq)
    =
    match selectedPath |> Option.map PathHelpers.normalizePath with
    | None -> Error "No note file is selected."
    | Some sourcePath when not (isAssignableMarkdownPath sourcePath) ->
        Error "Only markdown note or protocol files inside notes, studies, or assays can be added to an existing Study or Assay."
    | Some sourcePath ->
        match tryResolveTargetPath markdown targetRef with
        | Error errorMessage -> Error errorMessage
        | Ok preferredTargetPath when PathHelpers.pathsEqual sourcePath preferredTargetPath ->
            Error "The selected note is already in this Study or Assay."
        | Ok preferredTargetPath ->
            let plan = {
                SourcePath = sourcePath
                TargetPath = preferredTargetPath
                Request = FileContentDTO.create FileContentType.Markdown markdown preferredTargetPath
            }

            if PathHelpers.pathExistsInSnapshot existingPaths preferredTargetPath then
                Ok(ExistingTargetNoteMovePlanResult.TargetConflict plan)
            else
                Ok(ExistingTargetNoteMovePlanResult.Ready plan)
