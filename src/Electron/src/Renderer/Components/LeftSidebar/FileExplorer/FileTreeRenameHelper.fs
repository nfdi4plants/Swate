module Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper

open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Renderer.Components.LeftSidebar.FileExplorer.Types

let tryBuildRenameDraft (item: FileItem) : Result<ArcRenameDraft, string> =
    match item.Path |> Option.map PathHelpers.normalizeCanonicalRelativePath with
    | None -> Error "Could not resolve the selected item path for rename."
    | Some sourcePath ->
        if ArcEntityPathRules.isRenamePathAllowed sourcePath |> not then
            Error "Renaming this item is not allowed."
        else
            Ok {
                Item = item
                SourcePath = sourcePath
                InitialName = PathHelpers.getNameFromPath sourcePath
            }

let tryRemapSelectionPath (sourcePath: string) (targetPath: string) (selectedPath: string option) =
    selectedPath
    |> Option.bind (PathHelpers.tryRemapPathPrefix sourcePath targetPath)
