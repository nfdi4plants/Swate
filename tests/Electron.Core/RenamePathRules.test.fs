module ElectronCore.RenamePathRulesTests

open Swate.Electron.Shared.RenamePathRules
open Swate.Components.Shared
open Vitest

Vitest.describe (
    "RenamePathRules",
    fun () ->
        Vitest.test (
            "validateRenameName accepts valid names",
            fun () ->
                match validateRenameName "  New Assay  " with
                | Ok normalizedName -> Vitest.expect(normalizedName).toBe ("New Assay")
                | Error errorMessage -> failwith errorMessage
        )

        Vitest.test (
            "validateRenameName rejects path separators",
            fun () ->
                let result = validateRenameName "bad/name"

                Vitest
                    .expect(result)
                    .toEqual (Error "The new name must not contain path separators or null characters.")
        )

        Vitest.test (
            "buildRenamedSiblingPath keeps parent path and replaces leaf",
            fun () ->
                let targetPath = buildRenamedSiblingPath "assays/OldAssay" "NewAssay"
                Vitest.expect(targetPath).toBe ("assays/NewAssay")
        )

        Vitest.test (
            "tryBuildRenameTargetPath rejects no-op rename",
            fun () ->
                let result = tryBuildRenameTargetPath "assays/OldAssay" "OldAssay"
                Vitest.expect(result).toEqual (Error "Rename target is identical to the current path.")
        )

        Vitest.test (
            "ArcEntityPathRules.isRenamePathAllowed allows entity folders and safe generic descendants",
            fun () ->
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "assays/OldAssay").toBe (true)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "assays/OldAssay/isa.assay.xlsx").toBe (false)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "assays/OldAssay/notes/custom.txt").toBe (true)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "assays/OldAssay/dataset").toBe (false)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "assays/OldAssay/protocols").toBe (false)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "studies/StudyA/resources").toBe (false)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "assays/OldAssay/dataset/raw.txt").toBe (true)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "notes").toBe (false)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "notes/2026-06-15/foo/foo.md").toBe (true)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "test.fsx").toBe (true)
        )

        Vitest.test (
            "native structural child matching is case-insensitive",
            fun () ->
                Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "assays/OldAssay/DataSet").toBe (false)
                Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "studies/StudyA/Protocols").toBe (false)
                Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "studies/StudyA/Resources").toBe (false)
        )

        Vitest.test (
            "generic filesystem targets are limited to safe non-canonical paths",
            fun () ->
                let allowedTargets = [
                    "assays/AssayA/protocols/protocol.md"
                    "assays/AssayA/dataset/raw.txt"
                    "assays/AssayA/protocol"
                    "workflows/WorkflowA/scripts/workflow.cwl"
                    "runs/RunA/data.txt"
                    "test.fsx"
                    "notes/custom.md"
                ]

                allowedTargets
                |> List.iter (fun path ->
                    Vitest.expect(ArcEntityPathRules.isGenericFileSystemTargetAllowed path).toBe (true)
                )

                let rejectedTargets = [
                    ""
                    "assays"
                    "assays/AssayA"
                    "assays/AssayA/dataset"
                    "assays/AssayA/protocols"
                    "studies/StudyA/protocols"
                    "studies/StudyA/resources"
                    "assays/AssayA/isa.assay.xlsx"
                    "assays/AssayA/isa.datamap.xlsx"
                    "assays/AssayA/.gitattributes"
                    "assays/AssayA/readme.md"
                    "assays/AssayA/.git/config"
                    "../assays/AssayA/custom.txt"
                    "isa.investigation.xlsx"
                ]

                rejectedTargets
                |> List.iter (fun path ->
                    Vitest.expect(ArcEntityPathRules.isGenericFileSystemTargetAllowed path).toBe (false)
                )
        )

        Vitest.test (
            "protected entity child folders can still be used as generic filesystem parents",
            fun () ->
                Vitest.expect(ArcEntityPathRules.isGenericFileSystemParentAllowed "assays/AssayA/dataset").toBe (true)

                Vitest
                    .expect(ArcEntityPathRules.isGenericFileSystemParentAllowed "studies/StudyA/protocols")
                    .toBe (true)
        )

        Vitest.test (
            "root-level generic filesystem child paths are allowed for safe targets only",
            fun () ->
                match tryBuildGenericFileSystemChildPath false "" "docs" with
                | Ok targetPath -> Vitest.expect(targetPath).toBe ("docs")
                | Error errorMessage -> failwith errorMessage

                match tryBuildGenericFileSystemChildPath true "" "notes.txt" with
                | Ok targetPath -> Vitest.expect(targetPath).toBe ("notes.txt")
                | Error errorMessage -> failwith errorMessage

                let rejectedRootTargets = [
                    "studies"
                    "assays"
                    "workflows"
                    "runs"
                    "isa.investigation.xlsx"
                    ".git"
                    ".gitattributes"
                    ".gitkeep"
                    "../escape"
                ]

                rejectedRootTargets
                |> List.iter (fun name ->
                    [ true; false ]
                    |> List.iter (fun isFile ->
                        match tryBuildGenericFileSystemChildPath isFile "" name with
                        | Ok targetPath -> failwith $"Expected root target '{targetPath}' to be rejected."
                        | Error _ -> ()
                    )
                )
        )

        Vitest.test (
            "README files can be created anywhere generic files are allowed",
            fun () ->
                let parents = [
                    ""
                    "test"
                    "assays/AssayA"
                    "assays/AssayA/dataset"
                    "studies"
                ]

                parents
                |> List.iter (fun parentPath ->
                    [ "README.md"; "readme.md" ]
                    |> List.iter (fun name ->
                        match tryBuildGenericFileSystemChildPath true parentPath name with
                        | Ok _ -> ()
                        | Error errorMessage ->
                            failwith $"Expected '{parentPath}/{name}' to be creatable: {errorMessage}"
                    )
                )

                match tryBuildGenericFileSystemChildPath false "" "README.md" with
                | Ok targetPath -> failwith $"Expected README folder '{targetPath}' to be rejected."
                | Error _ -> ()

                Vitest.expect(ArcEntityPathRules.isDeletePathAllowed "README.md").toBe (false)
                Vitest.expect(ArcEntityPathRules.isRenamePathAllowed "assays/AssayA/README.md").toBe (false)
        )

        Vitest.test (
            "add zone roots accept new files but not new folders",
            fun () ->
                [ "studies"; "assays"; "workflows"; "runs" ]
                |> List.iter (fun zone ->
                    Vitest.expect(ArcEntityPathRules.isGenericFileSystemCreateParentAllowed true zone).toBe (true)
                    Vitest.expect(ArcEntityPathRules.isGenericFileSystemCreateParentAllowed false zone).toBe (false)

                    match tryBuildGenericFileSystemChildPath true zone "overview.md" with
                    | Ok targetPath -> Vitest.expect(targetPath).toBe ($"{zone}/overview.md")
                    | Error errorMessage -> failwith errorMessage

                    match tryBuildGenericFileSystemChildPath false zone "NewEntity" with
                    | Ok targetPath -> failwith $"Expected entity-shaped folder '{targetPath}' to be rejected."
                    | Error _ -> ()
                )

                match tryBuildGenericFileSystemChildPath true "studies" ".gitkeep" with
                | Ok targetPath -> failwith $"Expected protected file '{targetPath}' to be rejected."
                | Error _ -> ()
        )

        Vitest.test (
            "item-aware delete and rename checks treat files directly inside add zones as generic files",
            fun () ->
                [
                    "studies/overview.md", true
                    "studies/isa.study.xlsx", false
                    "studies/readme.md", false
                ]
                |> List.iter (fun (path, expected) ->
                    Vitest.expect(ArcEntityPathRules.isItemDeletePathAllowed true path).toBe (expected)
                    Vitest.expect(ArcEntityPathRules.isItemRenamePathAllowed true path).toBe (expected)
                )

                Vitest.expect(ArcEntityPathRules.isItemDeletePathAllowed false "studies/StudyA").toBe (true)
                Vitest.expect(ArcEntityPathRules.isItemRenamePathAllowed false "studies/StudyA").toBe (true)
                Vitest.expect(ArcEntityPathRules.isItemDeletePathAllowed true "assays/AssayA/protocol.md").toBe (true)
        )

        Vitest.test (
            "files directly inside add zones are generic rename targets",
            fun () ->
                match tryBuildGenericFileSystemRenameTargetPath true "studies/overview.md" "summary.md" with
                | Ok targetPath -> Vitest.expect(targetPath).toBe ("studies/summary.md")
                | Error errorMessage -> failwith errorMessage

                match tryBuildGenericFileSystemRenameTargetPath false "studies/StudyA" "StudyB" with
                | Ok targetPath -> failwith $"Expected entity folder rename '{targetPath}' to be rejected."
                | Error _ -> ()

                match tryBuildGenericFileSystemRenameTargetPath true "studies/overview.md" "README.md" with
                | Ok targetPath -> failwith $"Expected rename to protected '{targetPath}' to be rejected."
                | Error _ -> ()
        )
)

Vitest.describe (
    "PathHelpers path relation helpers",
    fun () ->
        Vitest.test (
            "containsPathTraversalSegments detects traversal markers",
            fun () ->
                Vitest.expect(PathHelpers.containsPathTraversalSegments "../assays/MyAssay").toBe (true)
                Vitest.expect(PathHelpers.containsPathTraversalSegments "assays/./MyAssay").toBe (true)
                Vitest.expect(PathHelpers.containsPathTraversalSegments "assays/MyAssay").toBe (false)
        )

        Vitest.test (
            "isSameOrDescendantPathForFsComparison is case-insensitive and normalized",
            fun () ->
                Vitest
                    .expect(PathHelpers.isSameOrDescendantPathForFsComparison "C:/Repo/ARC/Assays/A" "c:\\repo\\arc")
                    .toBe (true)

                Vitest
                    .expect(PathHelpers.isSameOrDescendantPathForFsComparison "C:/Repo/Other/A" "c:/repo/arc")
                    .toBe (false)
        )
)
