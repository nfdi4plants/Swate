module Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper

open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Renderer.Components.LeftSidebar.FileExplorer.Types

let private normalizeRelativePath (path: string) =
    path
    |> PathHelpers.normalizeCanonicalRelativePath

let canRenameItem (item: FileItem) =
    item.Path
    |> Option.map PathHelpers.normalizeCanonicalRelativePath
    |> Option.exists ArcDeletePathRules.isRenamePathAllowed

let tryBuildRenameDraft (item: FileItem) : Result<ArcRenameDraft, string> =

    let tryGetRelativePath (item: FileItem) : string option =
        item.Path
        |> Option.map PathHelpers.normalizeCanonicalRelativePath

    match tryGetRelativePath item with
    | None -> Error "Could not resolve the selected item path for rename."
    | Some relativePath ->
        let normalizedRelativePath = normalizeRelativePath relativePath

        if ArcDeletePathRules.isRenamePathAllowed normalizedRelativePath |> not then
            Error "Renaming this item is not allowed."
        else
            let sourcePath = normalizedRelativePath

            let initialName = PathHelpers.getNameFromPath sourcePath

            Ok {
                Item = item
                SourcePath = sourcePath
                InitialName = initialName
            }

let tryRemapSelectionPath (sourcePath: string) (targetPath: string) (selectedPath: string option) =
    let normalizedSourcePath = normalizeRelativePath sourcePath
    let normalizedTargetPath = normalizeRelativePath targetPath

    selectedPath
    |> Option.map normalizeRelativePath
    |> Option.bind (fun normalizedSelectedPath ->
        if PathHelpers.isSameOrDescendantPath normalizedSelectedPath normalizedSourcePath |> not then
            None
        else
            let selectedSegments = getNonEmptyPathParts normalizedSelectedPath
            let sourceSegments = getNonEmptyPathParts normalizedSourcePath
            let targetSegments = getNonEmptyPathParts normalizedTargetPath

            if selectedSegments.Length < sourceSegments.Length then
                None
            else
                let samePrefix =
                    sourceSegments
                    |> Array.mapi (fun index segment ->
                        PathHelpers.pathsEqual segment selectedSegments.[index]
                    )
                    |> Array.forall id

                if samePrefix |> not then
                    None
                else
                    let suffixSegments =
                        selectedSegments
                        |> Array.skip sourceSegments.Length

                    Array.append targetSegments suffixSegments
                    |> String.concat "/"
                    |> Some
    )
