module ElectronRenderer.FileTreeMaterializationTests

open System.Collections.Generic
open Browser.Dom
open Fable.Core.JsInterop
open Feliz
open Renderer.Components.LeftSidebar.FileExplorer.FileTreeMaterialization
open Swate.Components.Page.FileExplorer.Types
open Swate.Electron.Shared.FileIOTypes
open Vitest

Vitest.vi.mock (
    "./src/Electron/src/Renderer/Api.js",
    box (fun () ->
        createObj [
            "ipcGitLabApi" ==> createObj []
            "ipcVersionControlApi" ==> createObj []
            "ipcArcVaultApi" ==> createObj []
            "ipcAuthApi" ==> createObj []
            "ipcTemplateApi" ==> createObj []
            "ipcValidationPackageApi" ==> createObj []
        ]
    )
)
|> ignore

let private fileNode (name: string) (path: string) =
    FileTreeNode.create (name, false, path, Dictionary())

let private directoryNode (name: string) (path: string) (children: FileTreeNode list) =
    let childrenByName = Dictionary<string, FileTreeNode>()

    children |> List.iter (fun child -> childrenByName.[child.name] <- child)

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

let private folderItem id children = {
    FileTree.createFolder id (Some id) FileItemIcon.Folder with
        Id = id
        Children = Some children
}

let private fileItem id = {
    FileTree.createFile id (Some id) FileItemIcon.Document with
        Id = id
}

[<ReactComponent>]
let private ExpansionCleanupProbe (report: string[] -> unit) =
    Html.div [
        prop.ref (
            Renderer.Components.LeftSidebar.FileExplorer.FileTree.CreateClearActiveExpandedDirectoriesRef(fun () ->
                report [||]
            )
        )
    ]

Vitest.describe (
    "Electron file-tree materialization",
    fun () ->
        Vitest.test (
            "updating expanded paths materializes them and preserves the ARC scope",
            fun () ->
                let state = {
                    empty with
                        ArcScopeId = Some "C:/arc"
                }

                let materialized = setExpandedPaths (Set.singleton "arc\\notes") state

                Vitest.expect(materialized.ArcScopeId).toEqual (Some "C:/arc")
                Vitest.expect(materialized.Paths |> Set.toList).toEqual ([ "arc/notes" ])
                Vitest.expect(materialized.ExpandedPaths |> Set.toList).toEqual ([ "arc/notes" ])
        )

        Vitest.test (
            "maps an unmaterialized non-empty directory without children or expansion state",
            fun () ->
                let directory =
                    directoryNode "notes" "arc/notes" [ fileNode "note.md" "arc/notes/note.md" ]

                let item = toFileItemTree Set.empty directory

                Vitest.expect(item.Children.IsNone).toBe (true)
                Vitest.expect(item.IsExpanded).toBe (false)
        )

        Vitest.test (
            "maps children for a materialized directory without setting expansion state",
            fun () ->
                let directory =
                    directoryNode "notes" "arc/notes" [ fileNode "note.md" "arc/notes/note.md" ]

                let item = toFileItemTree (Set.singleton "arc/notes") directory

                Vitest.expect(item.Children.IsSome).toBe (true)
                Vitest.expect(item.Children.Value.Length).toBe (1)
                Vitest.expect(item.Children.Value.Head.Name).toBe ("note.md")
                Vitest.expect(item.IsExpanded).toBe (false)
        )

        Vitest.test (
            "snapshot reconciliation preserves surviving paths, prunes removed paths, and materializes selection",
            fun () ->
                let kept =
                    directoryNode "kept" "arc/kept" [ fileNode "kept.txt" "arc/kept/kept.txt" ]

                let selected =
                    directoryNode "selected" "arc/selected" [ fileNode "selected.txt" "arc/selected/selected.txt" ]

                let root = directoryNode "arc" "arc" [ kept; selected ]

                let current = {
                    ArcScopeId = Some "C:/arc"
                    Paths = Set.ofList [ "arc"; "arc/kept"; "arc/removed" ]
                    ExpandedPaths = Set.ofList [ "arc/kept"; "arc/removed" ]
                }

                let reconciled =
                    reconcileMaterializedState (Some "C:/arc") (Some "arc/selected/selected.txt") (Some root) current

                Vitest.expect(reconciled.Paths |> Set.toList).toEqual ([ "arc"; "arc/kept"; "arc/selected" ])
                Vitest.expect(reconciled.ExpandedPaths |> Set.toList).toEqual ([ "arc"; "arc/kept"; "arc/selected" ])
        )

        Vitest.test (
            "changing ARC scope resets materialized paths to the required root and selection chain",
            fun () ->
                let kept =
                    directoryNode "kept" "arc/kept" [ fileNode "kept.txt" "arc/kept/kept.txt" ]

                let root = directoryNode "arc" "arc" [ kept ]

                let current = {
                    ArcScopeId = Some "C:/old-arc"
                    Paths = Set.ofList [ "arc"; "arc/kept" ]
                    ExpandedPaths = Set.ofList [ "arc"; "arc/kept" ]
                }

                let reconciled =
                    reconcileMaterializedState (Some "C:/new-arc") None (Some root) current

                Vitest.expect(reconciled.ArcScopeId).toEqual (Some "C:/new-arc")
                Vitest.expect(reconciled.Paths |> Set.toList).toEqual ([ "arc" ])
                Vitest.expect(reconciled.ExpandedPaths |> Set.toList).toEqual ([ "arc" ])
        )
)

Vitest.describe (
    "active expanded directory reconciliation",
    fun () ->
        let child = folderItem "parent/child" []
        let sibling = folderItem "sibling" []
        let file = fileItem "file.txt"
        let items = [ folderItem "parent" [ child; file ]; sibling ]

        let activeIds expandedIds =
            FileExplorerLogic.collectActiveExpandedDirectories expandedIds items
            |> List.map _.Id
            |> Set.ofList

        Vitest.test (
            "keeps expanded parents, children, and unrelated siblings active",
            fun () ->
                let active = activeIds (Set.ofList [ "parent"; "parent/child"; "sibling" ])

                Vitest.expect(active |> Set.toList).toEqual ([ "parent"; "parent/child"; "sibling" ])
        )

        Vitest.test (
            "does not report a stale expanded child below a collapsed parent",
            fun () ->
                let active = activeIds (Set.ofList [ "parent/child"; "sibling" ])

                Vitest.expect(active |> Set.toList).toEqual ([ "sibling" ])
        )

        Vitest.test (
            "never reports files as active expanded directories",
            fun () ->
                let active = activeIds (Set.ofList [ "parent"; "file.txt" ])

                Vitest.expect(active.Contains "file.txt").toBe (false)
                Vitest.expect(active |> Set.toList).toEqual ([ "parent" ])
        )
)

Vitest.describe (
    "file explorer expansion cleanup",
    fun () ->
        Vitest.test (
            "unmount reports an empty active-directory request after an expanded scope",
            fun () -> promise {
                let requests = ResizeArray<string[]>()
                let container = document.createElement "div"
                document.body.appendChild container |> ignore
                let root = ReactDOM.createRoot container

                try
                    requests.Add [| "studies/S1/dataset" |]
                    root.render (ExpansionCleanupProbe requests.Add)
                    Vitest.expect(requests.[0]).toEqual ([| "studies/S1/dataset" |])
                    root.unmount ()

                    while requests.Count < 2 do
                        do! Promise.sleep 0

                    Vitest.expect(requests.[requests.Count - 1]).toEqual ([||])
                finally
                    container.remove ()
            }
        )
)
