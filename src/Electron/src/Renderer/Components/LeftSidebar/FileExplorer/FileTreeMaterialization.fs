module Renderer.Components.LeftSidebar.FileExplorer.FileTreeMaterialization

open Swate.Components.Shared
open Swate.Components.Page.FileExplorer.Types
open Swate.Electron.Shared.FileIOTypes

type MaterializedState = {
    ArcScopeId: string option
    Paths: Set<string>
}

let empty = {
    ArcScopeId = None
    Paths = Set.empty
}

let materialize path state =
    let normalizedPath = PathHelpers.normalizePath path

    if state.Paths.Contains normalizedPath then
        state
    else
        {
            state with
                Paths = state.Paths.Add normalizedPath
        }

let rec private collectDirectoryPaths (node: FileTreeNode) (directoryPaths: Set<string>) =
    if node.isDirectory then
        node.children.Values
        |> Seq.fold
            (fun state child -> collectDirectoryPaths child state)
            (Set.add (PathHelpers.normalizePath node.path) directoryPaths)
    else
        directoryPaths

let private requiredMaterializedDirectoryPaths
    (selectedTreeItemPath: string option)
    (root: FileTreeNode)
    (validDirectoryPaths: Set<string>)
    =
    let selectedPathChain =
        selectedTreeItemPath
        |> Option.map (fun selectedPath ->
            validDirectoryPaths
            |> Set.filter (fun directoryPath -> PathHelpers.isSameOrDescendantPath selectedPath directoryPath))
        |> Option.defaultValue Set.empty

    if root.isDirectory then
        selectedPathChain.Add(PathHelpers.normalizePath root.path)
    else
        selectedPathChain

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
        let requiredPaths = requiredMaterializedDirectoryPaths selectedTreeItemPath root validDirectoryPaths

        let persistedPaths =
            if current.ArcScopeId = arcScopeId then
                Set.intersect current.Paths validDirectoryPaths
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
        let isDirectoryMaterialized = materializedDirectoryPaths.Contains normalizedParentPath
        let hasSourceChildren = parent.children.Count > 0

        let mappedChildren =
            if isDirectoryMaterialized then
                parent.children.Values
                |> Seq.map (
                    toMaterializedFileItemTree
                        createItem
                        materializedDirectoryPaths
                )
                |> List.ofSeq
            else
                []

        let children =
            if isDirectoryMaterialized then Some mappedChildren
            elif hasSourceChildren then None
            else Some []

        {
            createItem parent with
                Children = children
        }
    else
        createItem parent
