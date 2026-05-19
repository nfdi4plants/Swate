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

    Vitest.test("ArcDeletePathRules.isRenamePathAllowed allows entity folders and safe generic descendants", fun () ->
        Vitest.expect(ArcDeletePathRules.isRenamePathAllowed "assays/OldAssay").toBe(true)
        Vitest.expect(ArcDeletePathRules.isRenamePathAllowed "assays/OldAssay/isa.assay.xlsx").toBe(false)
        Vitest.expect(ArcDeletePathRules.isRenamePathAllowed "assays/OldAssay/notes/custom.txt").toBe(true)
    )

    Vitest.test("generic filesystem targets are limited to safe non-canonical entity descendants", fun () ->
        let allowedTargets = [
            "assays/AssayA/protocols/protocol.md"
            "studies/StudyA/resources"
            "workflows/WorkflowA/scripts/workflow.cwl"
            "runs/RunA/data.txt"
        ]

        allowedTargets
        |> List.iter (fun path -> Vitest.expect(ArcDeletePathRules.isGenericFileSystemTargetAllowed path).toBe(true))

        let rejectedTargets = [
            ""
            "assays"
            "assays/AssayA"
            "assays/AssayA/isa.assay.xlsx"
            "assays/AssayA/isa.datamap.xlsx"
            "assays/AssayA/readme.md"
            "assays/AssayA/.git/config"
            "../assays/AssayA/custom.txt"
            "notes/custom.md"
        ]

        rejectedTargets
        |> List.iter (fun path -> Vitest.expect(ArcDeletePathRules.isGenericFileSystemTargetAllowed path).toBe(false))
    )

    Vitest.test("generic filesystem parents include entity folders and safe generic folders", fun () ->
        Vitest.expect(ArcDeletePathRules.isGenericFileSystemParentAllowed "assays/AssayA").toBe(true)
        Vitest.expect(ArcDeletePathRules.isGenericFileSystemParentAllowed "assays/AssayA/protocols").toBe(true)
        Vitest.expect(ArcDeletePathRules.isGenericFileSystemParentAllowed "assays").toBe(false)
        Vitest.expect(ArcDeletePathRules.isGenericFileSystemParentAllowed "notes").toBe(false)
    )

    Vitest.test("tryBuildGenericFileSystemChildPath validates parent and name", fun () ->
        match tryBuildGenericFileSystemChildPath "assays/AssayA" " protocol.md " with
        | Ok targetPath -> Vitest.expect(targetPath).toBe("assays/AssayA/protocol.md")
        | Error errorMessage -> failwith errorMessage

        Vitest.expect(tryBuildGenericFileSystemChildPath "assays" "protocol.md")
            .toEqual(Error "Generic file and folder creation is only allowed inside ARC entity folders.")

        Vitest.expect(tryBuildGenericFileSystemChildPath "assays/AssayA" "isa.assay.xlsx")
            .toEqual(Error "Generic file and folder targets must be non-canonical descendants inside ARC entity folders.")
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
