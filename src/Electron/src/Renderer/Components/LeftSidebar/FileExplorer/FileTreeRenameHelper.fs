module Renderer.Components.LeftSidebar.FileExplorer.FileTreeRenameHelper

open Swate.Components.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.RenamePathRules
open Renderer.Components.LeftSidebar.FileExplorer.Helper
open Renderer.Components.LeftSidebar.FileExplorer.Types

let private normalizeRelativePath (path: string) =
    path
    |> PathHelpers.normalizeRelativePath
    |> PathHelpers.normalizePath

let normalizeRenameName (newName: string) =
    validateRenameName newName

let buildRenamedPath (sourcePath: string) (newName: string) =
    buildRenamedSiblingPath sourcePath newName

let private isRenameContextMenuTarget (relativePath: string) =
    match ArcDeletePathRules.classifyRenameTarget relativePath with
    | ArcDeletePathRules.RenamePathClassification.EntityFolderTarget _
    | ArcDeletePathRules.RenamePathClassification.CanonicalEntityFileTarget _
    | ArcDeletePathRules.RenamePathClassification.CanonicalDataMapFileTarget _ -> true
    | _ -> false

let canRenameItem (item: FileItem) =
    tryGetItemRelativePath item
    |> Option.exists isRenameContextMenuTarget

let tryBuildRenameDraft (item: FileItem) : Result<ArcRenameDraft, string> =
    match tryGetItemRelativePath item with
    | None -> Error "Could not resolve the selected item path for rename."
    | Some relativePath ->
        let normalizedRelativePath = normalizeRelativePath relativePath

        if ArcDeletePathRules.isRenamePathAllowed normalizedRelativePath |> not then
            Error "Renaming this item is not allowed."
        else
            let sourcePath =
                normalizedRelativePath
                |> ArcDeletePathRules.resolveRenameSourcePath
                |> normalizeRelativePath

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
