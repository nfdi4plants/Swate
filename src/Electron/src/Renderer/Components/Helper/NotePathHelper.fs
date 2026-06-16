module Renderer.Components.Helper.NotePathHelper

open Swate.Components.Composite.Notes.Editor
open Swate.Components.Shared

let noteTargetConflictPath markdownPath =
    let normalizedPath = PathHelpers.normalizePath markdownPath

    match NoteConversion.tryGetNoteFolderRelativePath normalizedPath with
    | Some noteFolderPath when
        PathHelpers.pathsEqual
            (PathHelpers.getFileName normalizedPath)
            $"{PathHelpers.getFileName noteFolderPath}.md"
        ->
        PathHelpers.normalizePath noteFolderPath
    | None -> normalizedPath
    | Some _ -> normalizedPath
