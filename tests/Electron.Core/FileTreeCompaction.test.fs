module ElectronCore.FileTreeCompactionTests

open System.Collections.Generic
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Vitest

let private fileNode (name: string) (path: string) =
    FileTreeNode.create (name, false, path, Dictionary())

let private directoryNode (name: string) (path: string) (children: FileTreeNode list) =
    let childrenByName = Dictionary<string, FileTreeNode>()

    children
    |> List.iter (fun child -> childrenByName.[child.name] <- child)

    FileTreeNode.create (name, true, path, childrenByName)

let private onlyChild (node: FileTreeNode) =
    if node.children.Count <> 1 then
        failwith $"Expected exactly one child on '{node.path}', but found {node.children.Count}."

    node.children.Values |> Seq.exactlyOne

Vitest.describe("FileIOHelper.collapseSingleChildSameNameDirectories", fun () ->
    Vitest.test("collapses A/A single-child same-name directories", fun () ->
        let leaf = fileNode "leaf.txt" "arc/A/A/leaf.txt"
        let innerA = directoryNode "A" "arc/A/A" [ leaf ]
        let outerA = directoryNode "A" "arc/A" [ innerA ]
        let root = directoryNode "arc" "arc" [ outerA ]

        let collapsed = collapseSingleChildSameNameDirectories root
        let mergedA = onlyChild collapsed
        let mergedLeaf = onlyChild mergedA

        Vitest.expect(mergedA.path).toBe("arc/A/A")
        Vitest.expect(mergedA.name).toBe("A")
        Vitest.expect(mergedLeaf.path).toBe("arc/A/A/leaf.txt"))

    Vitest.test("collapses repeated A/A/A chains recursively", fun () ->
        let leaf = fileNode "leaf.txt" "arc/A/A/A/leaf.txt"
        let level3 = directoryNode "A" "arc/A/A/A" [ leaf ]
        let level2 = directoryNode "A" "arc/A/A" [ level3 ]
        let level1 = directoryNode "A" "arc/A" [ level2 ]
        let root = directoryNode "arc" "arc" [ level1 ]

        let collapsed = collapseSingleChildSameNameDirectories root
        let mergedA = onlyChild collapsed

        Vitest.expect(mergedA.path).toBe("arc/A/A/A")
        Vitest.expect(mergedA.children.Count).toBe(1))

    Vitest.test("does not collapse when same-name child folder has siblings/files", fun () ->
        let sameNameChild = directoryNode "A" "arc/A/A" []
        let siblingFile = fileNode "notes.txt" "arc/A/notes.txt"
        let outerA = directoryNode "A" "arc/A" [ sameNameChild; siblingFile ]
        let root = directoryNode "arc" "arc" [ outerA ]

        let collapsed = collapseSingleChildSameNameDirectories root
        let topA = onlyChild collapsed
        let childPaths =
            topA.children.Values
            |> Seq.map _.path
            |> Seq.sort
            |> Seq.toList

        Vitest.expect(topA.path).toBe("arc/A")
        Vitest.expect(childPaths).toEqual([ "arc/A/A"; "arc/A/notes.txt" ]))

    Vitest.test("does not collapse when only child has a different name", fun () ->
        let innerB = directoryNode "B" "arc/A/B" []
        let outerA = directoryNode "A" "arc/A" [ innerB ]
        let root = directoryNode "arc" "arc" [ outerA ]

        let collapsed = collapseSingleChildSameNameDirectories root
        let topA = onlyChild collapsed
        let childB = onlyChild topA

        Vitest.expect(topA.path).toBe("arc/A")
        Vitest.expect(topA.name).toBe("A")
        Vitest.expect(childB.path).toBe("arc/A/B")
        Vitest.expect(childB.name).toBe("B"))

    Vitest.test("preserves deepest path/id for interactions and compares names case-insensitively", fun () ->
        let leaf = fileNode "leaf.txt" "arc/Data/data/leaf.txt"
        let innerData = directoryNode "data" "arc/Data/data" [ leaf ]
        let outerData = directoryNode "Data" "arc/Data" [ innerData ]
        let root = directoryNode "arc" "arc" [ outerData ]

        let collapsed = collapseSingleChildSameNameDirectories root
        let mergedData = onlyChild collapsed

        Vitest.expect(mergedData.path).toBe("arc/Data/data")
        Vitest.expect(mergedData.path = "arc/Data").toBe(false))
)
