module Renderer.Components.LeftSidebar.FileExplorer.FileTreeMaterialization

open System
open Swate.Components.Shared
open Swate.Components.Page.FileExplorer.Types
open Swate.Electron.Shared.FileIOTypes

type MaterializedState = {
    ArcScopeId: string option
    Paths: Set<string>
    ExpandedPaths: Set<string>
}

let empty = {
    ArcScopeId = None
    Paths = Set.empty
    ExpandedPaths = Set.empty
}

let setExpandedPaths paths state =
    let normalizedPaths = paths |> Set.map PathHelpers.normalizePath

    {
        state with
            Paths = Set.union state.Paths normalizedPaths
            ExpandedPaths = normalizedPaths
    }

let private isSameOrDescendantLogicalPath path ancestorPath =
    let normalizedPath = PathHelpers.normalizePath path
    let normalizedAncestorPath = PathHelpers.normalizePath ancestorPath

    normalizedPath = normalizedAncestorPath
    || normalizedPath.StartsWith(normalizedAncestorPath + "/", StringComparison.Ordinal)

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
        ExpandedPaths = Set.empty
      }
    | Some root ->
        let validDirectoryPaths = collectDirectoryPaths root Set.empty

        let requiredPaths =
            selectedTreeItemPath
            |> Option.map (fun selectedPath ->
                validDirectoryPaths
                |> Set.filter (fun directoryPath -> isSameOrDescendantLogicalPath selectedPath directoryPath)
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

        let persistedExpandedPaths =
            if current.ArcScopeId = arcScopeId then
                Set.intersect current.ExpandedPaths validDirectoryPaths
            else
                Set.empty

        let expandedPaths = Set.union persistedExpandedPaths requiredPaths

        {
            ArcScopeId = arcScopeId
            Paths = Set.union persistedPaths expandedPaths
            ExpandedPaths = expandedPaths
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
            else
                None

        {
            createItem parent with
                Children = children
        }
    else
        createItem parent
