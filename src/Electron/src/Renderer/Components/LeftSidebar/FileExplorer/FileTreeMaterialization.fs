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
        let isKnownDirectory path =
            isKnownDirectory (PathHelpers.normalizePath path)

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

let private rootItemSortKey (node: FileTreeNode) =
    match node.name.ToLowerInvariant() with
    | "notes" -> 0, 0, ""
    | "readme.md" -> 0, 1, ""
    | "isa.investigation.xlsx" -> 0, 2, ""
    | "studies" -> 0, 3, ""
    | "assays" -> 0, 4, ""
    | "workflows" -> 0, 5, ""
    | "runs" -> 0, 6, ""
    | _ -> 1, System.Int32.MaxValue, node.name.ToLowerInvariant()

let rec private toMaterializedFileItemTreeCore
    (createItem: FileTreeNode -> FileItem)
    (materializedDirectoryPaths: Set<string>)
    (parent: FileTreeNode)
    (isRoot: bool)
    =
    if parent.isDirectory then
        let normalizedParentPath = PathHelpers.normalizePath parent.path

        let isDirectoryMaterialized =
            materializedDirectoryPaths.Contains normalizedParentPath

        let children =
            if isDirectoryMaterialized then
                let childNodes =
                    if isRoot then
                        parent.children.Values |> Seq.sortBy rootItemSortKey
                    else
                        parent.children.Values :> seq<FileTreeNode>

                childNodes
                |> Seq.map (fun parent ->
                    toMaterializedFileItemTreeCore createItem materializedDirectoryPaths parent false
                )
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

let toMaterializedFileItemTree
    (createItem: FileTreeNode -> FileItem)
    (materializedDirectoryPaths: Set<string>)
    (root: FileTreeNode)
    =
    toMaterializedFileItemTreeCore createItem materializedDirectoryPaths root true
