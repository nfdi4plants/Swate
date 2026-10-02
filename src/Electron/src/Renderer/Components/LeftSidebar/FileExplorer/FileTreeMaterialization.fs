module Renderer.Components.LeftSidebar.FileExplorer.FileTreeMaterialization

open System.Collections.Generic
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

let rec private collectDirectoryPaths (node: FileTreeNode) (directoryPaths: Set<string>) =
    if node.isDirectory then
        node.children.Values
        |> Seq.fold
            (fun state child -> collectDirectoryPaths child state)
            (Set.add (PathHelpers.normalizePath node.path) directoryPaths)
    else
        directoryPaths

let reconcileMaterializedState
    (arcScopeId: string option)
    (selectedTreeItemPath: string option)
    (root: FileTreeNode option)
    (current: MaterializedState)
    =
    match root with
    | None -> {
        ArcScopeId = arcScopeId
        Paths = Set.empty
      }
    | Some root ->
        let validDirectoryPaths = collectDirectoryPaths root Set.empty

        let requiredPaths =
            selectedTreeItemPath
            |> Option.map (fun selectedPath ->
                validDirectoryPaths
                |> Set.filter (fun directoryPath -> PathHelpers.isSameOrDescendantPath selectedPath directoryPath)
            )
            |> Option.defaultValue Set.empty
            |> fun paths ->
                if root.isDirectory then
                    paths.Add(PathHelpers.normalizePath root.path)
                else
                    paths

        let persistedPaths =
            if current.ArcScopeId = arcScopeId then
                Set.intersect current.Paths validDirectoryPaths
            else
                Set.empty

        {
            ArcScopeId = arcScopeId
            Paths = Set.union persistedPaths requiredPaths
        }

/// Reconciles expansion state from indexed directory membership without walking the complete FileTree.
let reconcileMaterializedStateWithKnownDirectories
    (arcScopeId: string option)
    (selectedTreeItemPath: string option)
    (root: FileTreeNode option)
    (knownDirectoryPaths: HashSet<string>)
    (current: MaterializedState)
    =
    match root with
    | None -> {
        ArcScopeId = arcScopeId
        Paths = Set.empty
      }
    | Some root ->
        let isKnownDirectory path =
            knownDirectoryPaths.Contains(PathHelpers.normalizePath path)

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
