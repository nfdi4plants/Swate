module Renderer.Components.LeftSidebar.FileExplorer.FileTreeMaterialization

open Swate.Components.Shared
open Swate.Components.Page.FileExplorer.Types
open Swate.Electron.Shared.FileIOTypes

type MaterializedState = {
    ArcScopeId: string option
    Paths: Set<string>
}

let empty = { ArcScopeId = None; Paths = Set.empty }

let materialize path state = {
    state with
        Paths = state.Paths.Add(PathHelpers.normalizePath path)
}

/// Reconciles expansion state through the canonical FileTree lookup without walking the tree.
let reconcileMaterializedState
    (arcScopeId: string option)
    (selectedTreeItemPath: string option)
    (root: FileTreeNode option)
    (isKnownDirectory: string -> bool)
    (current: MaterializedState)
    =
    match root with
    | None -> {
        ArcScopeId = arcScopeId
        Paths = Set.empty
      }
    | Some root ->
        let isKnownDirectory path = isKnownDirectory (PathHelpers.normalizePath path)

        let rec collectSelectedAncestors path collected =
            let normalizedPath = PathHelpers.normalizePath path

            let nextCollected =
                if isKnownDirectory normalizedPath then
                    Set.add normalizedPath collected
                else
                    collected

            match PathHelpers.tryGetParentPath normalizedPath with
            | Some parentPath -> collectSelectedAncestors parentPath nextCollected
            | None -> nextCollected

        let requiredPaths =
            selectedTreeItemPath
            |> Option.map (fun path -> collectSelectedAncestors path Set.empty)
            |> Option.defaultValue Set.empty
            |> fun paths ->
                if root.isDirectory then
                    paths.Add(PathHelpers.normalizePath root.path)
                else
                    paths

        let persistedPaths =
            if current.ArcScopeId = arcScopeId then
                current.Paths |> Set.filter isKnownDirectory
            else
                Set.empty

        {
            ArcScopeId = arcScopeId
            Paths = Set.union persistedPaths requiredPaths
        }

let rec toMaterializedFileItemTree
    (createItem: FileTreeNode -> FileItem)
    (materializedDirectoryPaths: Set<string>)
    (parent: FileTreeNode)
    =
    if parent.isDirectory then
        let normalizedParentPath = PathHelpers.normalizePath parent.path

        let isDirectoryMaterialized =
            materializedDirectoryPaths.Contains normalizedParentPath

        let children =
            if isDirectoryMaterialized then
                parent.children.Values
                |> Seq.map (toMaterializedFileItemTree createItem materializedDirectoryPaths)
                |> List.ofSeq
                |> Some
            elif parent.children.Count = 0 then
                Some []
            else
                None

        {
            createItem parent with
                Children = children
        }
    else
        createItem parent
