module ElectronCore.RenamePathRulesTests

open Swate.Electron.Shared.RenamePathRules
open Swate.Components.Shared
open Vitest

Vitest.describe("RenamePathRules", fun () ->
    Vitest.test("validateRenameName accepts valid names", fun () ->
        match validateRenameName "  New Assay  " with
        | Ok normalizedName -> Vitest.expect(normalizedName).toBe("New Assay")
        | Error errorMessage -> failwith errorMessage
    )

    Vitest.test("validateRenameName rejects path separators", fun () ->
        let result = validateRenameName "bad/name"
        Vitest.expect(result).toEqual(Error "The new name must not contain path separators or null characters.")
    )

    Vitest.test("buildRenamedSiblingPath keeps parent path and replaces leaf", fun () ->
        let targetPath = buildRenamedSiblingPath "assays/OldAssay" "NewAssay"
        Vitest.expect(targetPath).toBe("assays/NewAssay")
    )

    Vitest.test("tryBuildRenameTargetPath rejects no-op rename", fun () ->
        let result = tryBuildRenameTargetPath "assays/OldAssay" "OldAssay"
        Vitest.expect(result).toEqual(Error "Rename target is identical to the current path.")
    )

    Vitest.test("ArcDeletePathRules.isRenamePathAllowed only allows entity folders", fun () ->
        Vitest.expect(ArcDeletePathRules.isRenamePathAllowed "assays/OldAssay").toBe(true)
        Vitest.expect(ArcDeletePathRules.isRenamePathAllowed "assays/OldAssay/isa.assay.xlsx").toBe(false)
        Vitest.expect(ArcDeletePathRules.isRenamePathAllowed "assays/OldAssay/notes/custom.txt").toBe(false)
    )
)

Vitest.describe("PathHelpers path relation helpers", fun () ->
    Vitest.test("containsPathTraversalSegments detects traversal markers", fun () ->
        Vitest.expect(PathHelpers.containsPathTraversalSegments "../assays/MyAssay").toBe(true)
        Vitest.expect(PathHelpers.containsPathTraversalSegments "assays/./MyAssay").toBe(true)
        Vitest.expect(PathHelpers.containsPathTraversalSegments "assays/MyAssay").toBe(false)
    )

    Vitest.test("isSameOrDescendantPathForFsComparison is case-insensitive and normalized", fun () ->
        Vitest.expect(PathHelpers.isSameOrDescendantPathForFsComparison "C:/Repo/ARC/Assays/A" "c:\\repo\\arc")
            .toBe(true)

        Vitest.expect(PathHelpers.isSameOrDescendantPathForFsComparison "C:/Repo/Other/A" "c:/repo/arc")
            .toBe(false)
    )
)
