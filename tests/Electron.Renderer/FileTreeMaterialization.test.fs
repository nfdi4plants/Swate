module ElectronRenderer.FileTreeMaterializationTests

open System.Collections.Generic
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeMaterialization
open Swate.Components.Page.FileExplorer.Types
open Swate.Electron.Shared.FileIOTypes
open Vitest

let private fileNode (name: string) (path: string) =
    FileTreeNode.create (name, false, path, Dictionary())

let private directoryNode (name: string) (path: string) (children: FileTreeNode list) =
    let childrenByName = Dictionary<string, FileTreeNode>()

    children
    |> List.iter (fun child -> childrenByName.[child.name] <- child)

    FileTreeNode.create (name, true, path, childrenByName)

let private toFileItemTree materializedDirectoryPaths node =
    toMaterializedFileItemTree
        (fun node ->
            let item =
                if node.isDirectory then
                    FileTree.createFolder node.name (Some node.path) FileItemIcon.Folder
                else
                    FileTree.createFile node.name (Some node.path) FileItemIcon.Document

            { item with Id = node.path }
        )
        materializedDirectoryPaths
        node

Vitest.describe("Electron file-tree materialization", fun () ->
    Vitest.test("materializing a directory normalizes its path and preserves its ARC scope", fun () ->
        let state = { empty with ArcScopeId = Some "C:/arc" }
        let materialized = materialize "arc\\notes" state

        Vitest.expect(materialized.ArcScopeId).toEqual(Some "C:/arc")
        Vitest.expect(materialized.Paths |> Set.toList).toEqual([ "arc/notes" ])
    )

    Vitest.test("maps an unmaterialized non-empty directory without children or expansion state", fun () ->
        let directory = directoryNode "notes" "arc/notes" [ fileNode "note.md" "arc/notes/note.md" ]
        let item = toFileItemTree Set.empty directory

        Vitest.expect(item.Children.IsNone).toBe(true)
        Vitest.expect(item.IsExpanded).toBe(false)
    )

    Vitest.test("maps children for a materialized directory without setting expansion state", fun () ->
        let directory = directoryNode "notes" "arc/notes" [ fileNode "note.md" "arc/notes/note.md" ]
        let item = toFileItemTree (Set.singleton "arc/notes") directory

        Vitest.expect(item.Children.IsSome).toBe(true)
        Vitest.expect(item.Children.Value.Length).toBe(1)
        Vitest.expect(item.Children.Value.Head.Name).toBe("note.md")
        Vitest.expect(item.IsExpanded).toBe(false)
    )

    Vitest.test("snapshot reconciliation preserves surviving paths, prunes removed paths, and materializes selection", fun () ->
        let kept = directoryNode "kept" "arc/kept" [ fileNode "kept.txt" "arc/kept/kept.txt" ]
        let selected = directoryNode "selected" "arc/selected" [ fileNode "selected.txt" "arc/selected/selected.txt" ]
        let root = directoryNode "arc" "arc" [ kept; selected ]

        let current = {
            ArcScopeId = Some "C:/arc"
            Paths = Set.ofList [ "arc"; "arc/kept"; "arc/removed" ]
        }

        let reconciled =
            reconcileMaterializedState
                (Some "C:/arc")
                (Some "arc/selected/selected.txt")
                (Some root)
                current

        Vitest.expect(reconciled.Paths |> Set.toList).toEqual([ "arc"; "arc/kept"; "arc/selected" ])
    )

    Vitest.test("changing ARC scope resets materialized paths to the required root and selection chain", fun () ->
        let kept = directoryNode "kept" "arc/kept" [ fileNode "kept.txt" "arc/kept/kept.txt" ]
        let root = directoryNode "arc" "arc" [ kept ]

        let current = {
            ArcScopeId = Some "C:/old-arc"
            Paths = Set.ofList [ "arc"; "arc/kept" ]
        }

        let reconciled =
            reconcileMaterializedState (Some "C:/new-arc") None (Some root) current

        Vitest.expect(reconciled.ArcScopeId).toEqual(Some "C:/new-arc")
        Vitest.expect(reconciled.Paths |> Set.toList).toEqual([ "arc" ])
    )
)
