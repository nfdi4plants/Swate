module internal Swate.Components.Page.FileExplorer.TypesTests

open Swate.Components.Page.FileExplorer.Types
open Vitest

let private file (path: string) = {
    FileTree.createFile (path.Split('/') |> Array.last) (Some path) FileItemIcon.Document with
        Id = path
}

let private folder (path: string) (children: FileItem list) = {
    FileTree.createFolder (path.Split('/') |> Array.last) (Some path) FileItemIcon.Folder with
        Id = path
        Children = Some children
}

let private items = [
    folder "studies" [
        folder "studies/s1" [
            file "studies/s1/isa.study.xlsx"
            folder "studies/s1/resources" []
        ]
    ]
    folder "assays" [ file "assays/notes.md" ]
]

let private updateItems revealItemId model =
    FileExplorerLogic.update (FileExplorerLogic.UpdateItems(items, None, revealItemId)) model

Vitest.describe (
    "FileExplorer reveal",
    fun () ->
        Vitest.test (
            "revealing a file opens its parent folders and leaves the selection alone",
            fun () ->
                let model =
                    FileExplorerLogic.init items
                    |> FileExplorerLogic.update (FileExplorerLogic.SelectItem "assays/notes.md")
                    |> updateItems (Some "studies/s1/isa.study.xlsx")

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([ "studies"; "studies/s1" ])
                Vitest.expect(model.SelectedId).toEqual (Some "assays/notes.md")
        )

        Vitest.test (
            "revealing a folder opens the folder too",
            fun () ->
                let model = FileExplorerLogic.init items |> updateItems (Some "studies/s1")

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([ "studies"; "studies/s1" ])
        )

        Vitest.test (
            "revealing an item that is not in the tree opens nothing",
            fun () ->
                let model =
                    FileExplorerLogic.init items |> updateItems (Some "runs/r1/isa.run.xlsx")

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([])
        )

        Vitest.test (
            "a folder closed after the reveal stays closed on later updates without a reveal",
            fun () ->
                let model =
                    FileExplorerLogic.init items
                    |> updateItems (Some "studies/s1/isa.study.xlsx")
                    |> FileExplorerLogic.update (FileExplorerLogic.SetExpanded("studies", false))
                    |> updateItems None

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([ "studies/s1" ])
        )
)
