module ElectronCore.FileTreeCompactionTests

open System.Collections.Generic
module FileTreeCreator = Main.FileTreeCreator

open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.VersionControlTypes
open Swate.Components.Shared
open Vitest

let private createFileEntry path isDirectory largeObject = {
    name = path |> PathHelpers.normalizePath |> PathHelpers.getFileName
    isDirectory = isDirectory
    path = path
    largeObject = largeObject
}

Vitest.describe (
    "FileTreeCreator.removePathAndDescendants",
    fun () ->
        Vitest.test (
            "returns a copy without the target path and descendants",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc", createFileEntry "C:/arc" true None)
                tree.Add("C:/arc/assays", createFileEntry "C:/arc/assays" true None)
                tree.Add("C:/arc/assays/A", createFileEntry "C:/arc/assays/A" true None)

                tree.Add("C:/arc/assays/A/isa.assay.xlsx", createFileEntry "C:/arc/assays/A/isa.assay.xlsx" false None)

                tree.Add("C:/arc/assays/AB", createFileEntry "C:/arc/assays/AB" true None)

                tree.Add(
                    "C:/arc/assays/AB/isa.assay.xlsx",
                    createFileEntry "C:/arc/assays/AB/isa.assay.xlsx" false None
                )

                let updatedTree = FileTreeCreator.removePathAndDescendants "C:/arc/assays/A" tree

                Vitest.expect(updatedTree.ContainsKey("C:/arc/assays/A")).toBe (false)
                Vitest.expect(updatedTree.ContainsKey("C:/arc/assays/A/isa.assay.xlsx")).toBe (false)
                Vitest.expect(updatedTree.ContainsKey("C:/arc/assays/AB")).toBe (true)
                Vitest.expect(updatedTree.ContainsKey("C:/arc/assays/AB/isa.assay.xlsx")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/A")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/A/isa.assay.xlsx")).toBe (true)
        )
)

Vitest.describe (
    "FileTreeCreator.removePathsAndDescendantsInPlace",
    fun () ->
        Vitest.test (
            "removes multiple target directories while preserving unrelated and case-distinct siblings",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc/assays/A", createFileEntry "C:/arc/assays/A" true None)
                tree.Add("C:/arc/assays/A/file.txt", createFileEntry "C:/arc/assays/A/file.txt" false None)
                tree.Add("C:/arc/studies/S", createFileEntry "C:/arc/studies/S" true None)
                tree.Add("C:/arc/studies/S/file.txt", createFileEntry "C:/arc/studies/S/file.txt" false None)
                tree.Add("C:/arc/assays/AB", createFileEntry "C:/arc/assays/AB" true None)
                tree.Add("C:/arc/assays/a", createFileEntry "C:/arc/assays/a" true None)
                tree.Add("C:/arc/assays/a/file.txt", createFileEntry "C:/arc/assays/a/file.txt" false None)

                FileTreeCreator.removePathsAndDescendantsInPlace [ "C:/arc/assays/A"; "C:/arc/studies/S" ] tree

                Vitest.expect(tree.ContainsKey("C:/arc/assays/A")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/A/file.txt")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc/studies/S")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc/studies/S/file.txt")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/AB")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/a")).toBe (true)
                Vitest.expect(tree.ContainsKey("C:/arc/assays/a/file.txt")).toBe (true)
        )

        Vitest.test (
            "handles ancestor and descendant delete targets without removing siblings",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc/studies/S", createFileEntry "C:/arc/studies/S" true None)
                tree.Add("C:/arc/studies/S/data", createFileEntry "C:/arc/studies/S/data" true None)

                tree.Add("C:/arc/studies/S/data/file.txt", createFileEntry "C:/arc/studies/S/data/file.txt" false None)

                tree.Add("C:/arc/studies/Sibling", createFileEntry "C:/arc/studies/Sibling" true None)

                FileTreeCreator.removePathsAndDescendantsInPlace [ "C:/arc/studies/S/data"; "C:/arc/studies/S" ] tree

                Vitest.expect(tree.ContainsKey("C:/arc/studies/S")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc/studies/S/data")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc/studies/S/data/file.txt")).toBe (false)
                Vitest.expect(tree.ContainsKey("C:/arc/studies/Sibling")).toBe (true)
        )
)

Vitest.describe (
    "FileTreeCreator.upsertFileEntry",
    fun () ->
        let pointerInfo: ObjectStateDto = {
            Path = "data.bin"
            SizeBytes = Some 128.0
            IsMaterialized = false
            IsLocallyAvailable = false
            ObjectId = Some "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        }

        Vitest.test (
            "returns a copy with an existing file entry replaced",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc/data.bin", createFileEntry "C:/arc/data.bin" false None)
                tree.Add("C:/arc/other.bin", createFileEntry "C:/arc/other.bin" false None)

                let fileEntry = createFileEntry "C:/arc/data.bin" false (Some pointerInfo)
                let updatedTree = FileTreeCreator.upsertFileEntry fileEntry tree

                Vitest.expect(updatedTree.Count).toBe (2)
                Vitest.expect(updatedTree.["C:/arc/data.bin"].largeObject).toEqual (Some pointerInfo)
                Vitest.expect(updatedTree.ContainsKey("C:/arc/other.bin")).toBe (true)
                Vitest.expect(tree.["C:/arc/data.bin"].largeObject).toEqual (None)
        )

        Vitest.test (
            "returns a copy with a new file entry added",
            fun () ->
                let tree = Dictionary<string, FileEntry>()
                tree.Add("C:/arc/other.bin", createFileEntry "C:/arc/other.bin" false None)

                let fileEntry = createFileEntry "C:/arc/data.bin" false (Some pointerInfo)
                let updatedTree = FileTreeCreator.upsertFileEntry fileEntry tree

                Vitest.expect(updatedTree.ContainsKey("C:/arc/other.bin")).toBe (true)
                Vitest.expect(updatedTree.["C:/arc/data.bin"].largeObject).toEqual (Some pointerInfo)
                Vitest.expect(tree.ContainsKey("C:/arc/data.bin")).toBe (false)
        )
)
