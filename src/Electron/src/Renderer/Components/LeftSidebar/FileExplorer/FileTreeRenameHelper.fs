module Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper

open ARCitISA.Components.Page.FileExplorer.Types
open ARCitISA.Components.Shared
open Renderer.Components.LeftSidebar.FileExplorer.Types
open Renderer.Components.LeftSidebar.FileExplorer.Helper

let tryBuildRenameDraft (item: FileItem) : Result<ArcRenameDraft, string> =
    match item.Path |> Option.map PathHelpers.normalizeCanonicalRelativePath with
    | None -> Error "Could not resolve the selected item path for rename."
    | Some sourcePath ->
        if canRenameFileItem item |> not then
            Error "Renaming this item is not allowed."
        else
            Ok {
                Item = item
                SourcePath = sourcePath
                InitialName = PathHelpers.getNameFromPath sourcePath
            }
