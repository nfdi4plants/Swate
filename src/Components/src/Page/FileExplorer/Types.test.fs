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

let private changedItems = [
    folder "studies" [
        folder "studies/s1" [
            file "studies/s1/isa.study.xlsx"
            folder "studies/s1/resources" []
        ]
        file "studies/notes.txt"
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

        Vitest.test (
            "keeping the reveal id set does not reopen a folder the user closed",
            fun () ->
                let target = Some "studies/s1/isa.study.xlsx"

                let model =
                    FileExplorerLogic.init items
                    |> updateItems target
                    |> FileExplorerLogic.update (FileExplorerLogic.SetExpanded("studies", false))
                    |> FileExplorerLogic.update (FileExplorerLogic.UpdateItems(changedItems, None, target))

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([ "studies/s1" ])
        )

        Vitest.test (
            "revealing another id in between lets the first id reveal again",
            fun () ->
                let study = Some "studies/s1/isa.study.xlsx"

                let model =
                    FileExplorerLogic.init items
                    |> updateItems study
                    |> FileExplorerLogic.update (FileExplorerLogic.SetExpanded("studies", false))
                    |> updateItems (Some "assays/notes.md")
                    |> updateItems study

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([ "assays"; "studies"; "studies/s1" ])
        )

        Vitest.test (
            "revealing a folder whose children are not loaded opens its parents but not the folder",
            fun () ->
                let unloaded = [
                    folder "studies" [
                        {
                            folder "studies/s2" [] with
                                Children = None
                        }
                    ]
                ]

                let model =
                    FileExplorerLogic.update
                        (FileExplorerLogic.UpdateItems(unloaded, None, Some "studies/s2"))
                        (FileExplorerLogic.init unloaded)

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([ "studies" ])
        )

        Vitest.test (
            "revealing the same id again after it was cleared opens the folders again",
            fun () ->
                let target = Some "studies/s1/isa.study.xlsx"

                let model =
                    FileExplorerLogic.init items
                    |> updateItems target
                    |> FileExplorerLogic.update (FileExplorerLogic.SetExpanded("studies", false))
                    |> updateItems None
                    |> updateItems target

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([ "studies"; "studies/s1" ])
        )

        Vitest.test (
            "a reveal id for an item that arrives later opens its folders once the item is there",
            fun () ->
                let target = Some "studies/s1/isa.study.xlsx"

                let before =
                    FileExplorerLogic.update
                        (FileExplorerLogic.UpdateItems([ folder "assays" [ file "assays/notes.md" ] ], None, target))
                        (FileExplorerLogic.init [])

                Vitest.expect(before.ExpandedIds |> Set.toList).toEqual ([])

                let model = updateItems target before

                Vitest.expect(model.ExpandedIds |> Set.toList).toEqual ([ "studies"; "studies/s1" ])
        )
)
