module ElectronCore.FileTreeCompactionTests

open System.Collections.Generic
module FileTreeCreator = Main.FileTreeCreator
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

        let collapsed = collapseSingleChildSameName root
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

        let collapsed = collapseSingleChildSameName root
        let mergedA = onlyChild collapsed

        Vitest.expect(mergedA.path).toBe("arc/A/A/A")
        Vitest.expect(mergedA.children.Count).toBe(1))

    Vitest.test("does not collapse when same-name child folder has siblings/files", fun () ->
        let sameNameChild = directoryNode "A" "arc/A/A" []
        let siblingFile = fileNode "notes.txt" "arc/A/notes.txt"
        let outerA = directoryNode "A" "arc/A" [ sameNameChild; siblingFile ]
        let root = directoryNode "arc" "arc" [ outerA ]

        let collapsed = collapseSingleChildSameName root
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

        let collapsed = collapseSingleChildSameName root
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

        let collapsed = collapseSingleChildSameName root
        let mergedData = onlyChild collapsed

        Vitest.expect(mergedData.path).toBe("arc/Data/data")
        Vitest.expect(mergedData.path = "arc/Data").toBe(false))
)

Vitest.describe("FileIOHelper.toFileTreeNode LFS metadata", fun () ->
    Vitest.test("preserves Git LFS ls-files metadata from FileEntry to root FileTreeNode", fun () ->
        let lfsInfo: GitLfsLsFileInfo = {
            name = "arc/sample.bin"
            size = 2048.0
            checkout = false
            downloaded = false
            ``oid_type`` = "sha256"
            oid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            version = "https://git-lfs.github.com/spec/v1"
        }

        let rootEntry: FileEntry = {
            name = "arc"
            isDirectory = true
            path = "C:/arc"
            lfs = Some lfsInfo
        }

        let rootNode = toFileTreeNode [| rootEntry |]

        Vitest.expect(rootNode.lfs).toEqual(Some lfsInfo))

    Vitest.test("keeps root-level generic files and folders as root children", fun () ->
        let entries = [|
            FileEntry.create("arc", "", true)
            FileEntry.create("studies", "studies", true)
            FileEntry.create("docs", "docs", true)
            FileEntry.create("notes.txt", "notes.txt", false)
        |]

        let rootNode = toFileTreeNode entries

        Vitest.expect(rootNode.children.ContainsKey("studies")).toBe(true)
        Vitest.expect(rootNode.children.ContainsKey("docs")).toBe(true)
        Vitest.expect(rootNode.children.ContainsKey("notes.txt")).toBe(true)
        Vitest.expect(rootNode.children.["docs"].path).toBe("docs")
        Vitest.expect(rootNode.children.["notes.txt"].path).toBe("notes.txt"))
)

Vitest.describe("FileTreeCreator.removePathAndDescendants", fun () ->
    let createFileEntry path isDirectory = {
        name =
            path
            |> Swate.Components.Shared.PathHelpers.normalizePath
            |> Swate.Components.Shared.PathHelpers.getFileName
        isDirectory = isDirectory
        path = path
        lfs = None
    }

    Vitest.test("removes only the target path and descendants", fun () ->
        let tree = Dictionary<string, FileEntry>()
        tree.Add("C:/arc", createFileEntry "C:/arc" true)
        tree.Add("C:/arc/assays", createFileEntry "C:/arc/assays" true)
        tree.Add("C:/arc/assays/A", createFileEntry "C:/arc/assays/A" true)
        tree.Add("C:/arc/assays/A/isa.assay.xlsx", createFileEntry "C:/arc/assays/A/isa.assay.xlsx" false)
        tree.Add("C:/arc/assays/AB", createFileEntry "C:/arc/assays/AB" true)
        tree.Add("C:/arc/assays/AB/isa.assay.xlsx", createFileEntry "C:/arc/assays/AB/isa.assay.xlsx" false)

        let updatedTree = FileTreeCreator.removePathAndDescendants "C:/arc/assays/A" tree

        Vitest.expect(updatedTree.ContainsKey("C:/arc/assays/A")).toBe(false)
        Vitest.expect(updatedTree.ContainsKey("C:/arc/assays/A/isa.assay.xlsx")).toBe(false)
        Vitest.expect(updatedTree.ContainsKey("C:/arc/assays/AB")).toBe(true)
        Vitest.expect(updatedTree.ContainsKey("C:/arc/assays/AB/isa.assay.xlsx")).toBe(true)
    )
)

Vitest.describe("FileTreeCreator.upsertFileEntry", fun () ->
    let createFileEntry path isDirectory lfs = {
        name =
            path
            |> Swate.Components.Shared.PathHelpers.normalizePath
            |> Swate.Components.Shared.PathHelpers.getFileName
        isDirectory = isDirectory
        path = path
        lfs = lfs
    }

    let pointerInfo: GitLfsLsFileInfo = {
        name = "data.bin"
        size = 128.0
        checkout = false
        downloaded = false
        ``oid_type`` = "sha256"
        oid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        version = "https://git-lfs.github.com/spec/v1"
    }

    Vitest.test("replaces an existing file entry without throwing", fun () ->
        let tree = Dictionary<string, FileEntry>()
        tree.Add("C:/arc/data.bin", createFileEntry "C:/arc/data.bin" false None)
        tree.Add("C:/arc/other.bin", createFileEntry "C:/arc/other.bin" false None)

        let updatedTree =
            FileTreeCreator.upsertFileEntry
                (createFileEntry "C:/arc/data.bin" false (Some pointerInfo))
                tree

        Vitest.expect(updatedTree.Count).toBe(2)
        Vitest.expect(updatedTree.["C:/arc/data.bin"].lfs).toEqual(Some pointerInfo)
        Vitest.expect(updatedTree.ContainsKey("C:/arc/other.bin")).toBe(true)
    )

    Vitest.test("returns a new dictionary instead of mutating the current file tree", fun () ->
        let tree = Dictionary<string, FileEntry>()
        tree.Add("C:/arc/data.bin", createFileEntry "C:/arc/data.bin" false None)

        let updatedTree =
            FileTreeCreator.upsertFileEntry
                (createFileEntry "C:/arc/data.bin" false (Some pointerInfo))
                tree

        Vitest.expect(tree.["C:/arc/data.bin"].lfs).toEqual(None)
        Vitest.expect(updatedTree.["C:/arc/data.bin"].lfs).toEqual(Some pointerInfo)
    )
)
