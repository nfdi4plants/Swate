module PathHelpersTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Swate.Components.Shared

let tests =
    testList "PathHelpers" [
        testCase "normalizeSeparators converts backslashes to slashes" <| fun _ ->
            Expect.equal
                (PathHelpers.normalizeSeparators @" C:\repo\arc\file.txt ")
                " C:/repo/arc/file.txt "
                "Backslashes should be normalized without trimming."

        testCase "normalizePath trims whitespace and trailing slashes" <| fun _ ->
            Expect.equal
                (PathHelpers.normalizePath "  studies\\study_01/  ")
                "studies/study_01"
                "Whitespace and trailing slashes should be removed after separator normalization."

        testCase "normalizeRelativePath trims leading and trailing slashes" <| fun _ ->
            Expect.equal
                (PathHelpers.normalizeRelativePath "/assays\\assay_01/")
                "assays/assay_01"
                "Relative paths should not keep leading or trailing separators."

        testCase "pathsEqual stays case-insensitive for shared callers" <| fun _ ->
            Expect.isTrue
                (PathHelpers.pathsEqual @"C:\Repo\Assays\A" "c:/repo/assays/a/")
                "Comparison should ignore slash direction, trailing slash, and casing."

        testCase "resolveArcPreviewPath redirects assay datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "/assays/assay_01/isa.datamap.xlsx")
                "/assays/assay_01/isa.assay.xlsx"
                "Assay datamaps should preview the assay workbook."

        testCase "resolveArcPreviewPath redirects relative assay datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "assays/assay_01/isa.datamap.xlsx")
                "assays/assay_01/isa.assay.xlsx"
                "Relative assay datamaps should preview the assay workbook."

        testCase "resolveArcPreviewPath redirects study datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "/studies/study_01/isa.datamap.xlsx")
                "/studies/study_01/isa.study.xlsx"
                "Study datamaps should preview the study workbook."

        testCase "resolveArcPreviewPath redirects relative study datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "studies/study_01/isa.datamap.xlsx")
                "studies/study_01/isa.study.xlsx"
                "Relative study datamaps should preview the study workbook."

        testCase "resolveArcPreviewPath redirects workflow datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "/workflows/workflow_01/isa.datamap.xlsx")
                "/workflows/workflow_01/isa.workflow.xlsx"
                "Workflow datamaps should preview the workflow workbook."

        testCase "resolveArcPreviewPath redirects relative workflow datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "workflows/workflow_01/isa.datamap.xlsx")
                "workflows/workflow_01/isa.workflow.xlsx"
                "Relative workflow datamaps should preview the workflow workbook."

        testCase "resolveArcPreviewPath redirects run datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "/runs/run_01/isa.datamap.xlsx")
                "/runs/run_01/isa.run.xlsx"
                "Run datamaps should preview the run workbook."

        testCase "resolveArcPreviewPath redirects relative run datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "runs/run_01/isa.datamap.xlsx")
                "runs/run_01/isa.run.xlsx"
                "Relative run datamaps should preview the run workbook."

        testCase "resolveArcPreviewPath leaves non-datamap paths normalized" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "  workflows\\workflow_01\\notes.md  ")
                "workflows/workflow_01/notes.md"
                "Non-datamap paths should only be normalized."

        testCase "resolveArcPreviewPath does not redirect nested assay datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "assays/archive/old/isa.datamap.xlsx")
                "assays/archive/old/isa.datamap.xlsx"
                "Only canonical ARC datamap paths should be redirected."

        testCase "resolveArcPreviewPath does not redirect prefixed assay datamaps" <| fun _ ->
            Expect.equal
                (PathHelpers.resolveArcViewPath "backup/assays/archive/isa.datamap.xlsx")
                "backup/assays/archive/isa.datamap.xlsx"
                "Folder-name matches outside the canonical ARC root should not be redirected."

        testCase "classifyDeleteTarget identifies canonical entity files" <| fun _ ->
            let classification =
                ArcEntityPathRules.classifyDeleteTarget "assays/MyAssay/isa.assay.xlsx"

            match classification with
            | ArcEntityPathRules.DeletePathClassification.CanonicalFileTarget(
                ArcEntityPathRules.CanonicalArcFileTarget.EntityFile(ArcEntityPathRules.AddZone.Assays, "MyAssay"),
                _
              ) -> ()
            | _ -> failwith "Expected canonical assay entity file classification."

        testCase "classifyDeleteTarget identifies entity folders" <| fun _ ->
            let classification =
                ArcEntityPathRules.classifyDeleteTarget "studies/MyStudy"

            match classification with
            | ArcEntityPathRules.DeletePathClassification.EntityFolderTarget(
                ArcEntityPathRules.AddZone.Studies,
                "MyStudy",
                _
              ) -> ()
            | _ -> failwith "Expected study entity folder classification."

        testCase "isDeletePathAllowed keeps broad add-zone descendants" <| fun _ ->
            Expect.isTrue
                (ArcEntityPathRules.isDeletePathAllowed "assays/MyAssay/notes/custom.txt")
                "Any descendant under add zones should remain deletable."

            Expect.isTrue
                (ArcEntityPathRules.isDeletePathAllowed "test.fsx")
                "Safe root-level generic files should be deletable."

        testCase "isDeletePathAllowed rejects protected targets" <| fun _ ->
            Expect.isFalse
                (ArcEntityPathRules.isDeletePathAllowed "workflows/MyWorkflow/readme.md")
                "Protected files should remain non-deletable."

        testCase "buildFallbackUnlinkPaths maps entity folder to canonical files" <| fun _ ->
            let fallbackPaths =
                ArcEntityPathRules.buildFallbackUnlinkPaths "runs/MyRun"

            Expect.equal
                fallbackPaths
                [
                    "runs/MyRun/isa.run.xlsx"
                    "runs/MyRun/isa.datamap.xlsx"
                ]
                "Entity-folder fallback should synthesize canonical entity + datamap unlink paths."

        testCase "buildFallbackUnlinkPaths keeps canonical file targets as-is" <| fun _ ->
            let fallbackPaths =
                ArcEntityPathRules.buildFallbackUnlinkPaths "workflows/MyFlow/isa.datamap.xlsx"

            Expect.equal
                fallbackPaths
                [ "workflows/MyFlow/isa.datamap.xlsx" ]
                "Canonical file fallback should return the normalized target path."

        testCase "classifyRenameTarget maps canonical ARC file paths to dedicated rename variants" <| fun _ ->
            let classification =
                ArcEntityPathRules.classifyRenameTarget "assays/MyAssay/isa.assay.xlsx"

            match classification with
            | ArcEntityPathRules.RenamePathClassification.CanonicalEntityFileTarget(
                ArcEntityPathRules.AddZone.Assays,
                "MyAssay",
                _
              ) -> ()
            | _ -> failwith "Expected canonical assay file rename classification."

        testCase "resolveRenameSourcePath redirects canonical ARC files to their entity folder" <| fun _ ->
            Expect.equal
                (ArcEntityPathRules.resolveRenameSourcePath "runs/MyRun/isa.run.xlsx")
                "runs/MyRun"
                "Renaming a canonical ARC file should rename the parent entity folder."

        testCase "isRenamePathAllowed allows entity folders and safe generic descendants" <| fun _ ->
            Expect.isFalse
                (ArcEntityPathRules.isRenamePathAllowed "")
                "ARC root must not be renameable."

            Expect.isFalse
                (ArcEntityPathRules.isRenamePathAllowed "studies")
                "Add-zone roots must stay protected."

            Expect.isFalse
                (ArcEntityPathRules.isRenamePathAllowed "studies/MyStudy/isa.study.xlsx")
                "Canonical ARC files should not be renameable."

            Expect.isTrue
                (ArcEntityPathRules.isRenamePathAllowed "studies/MyStudy")
                "Entity folders should be renameable."

            Expect.isTrue
                (ArcEntityPathRules.isRenamePathAllowed "studies/MyStudy/notes/custom.txt")
                "Safe generic descendants should be renameable."

            Expect.isTrue
                (ArcEntityPathRules.isRenamePathAllowed "test.fsx")
                "Safe root-level generic files should be renameable."

        testCase "buildCanonicalEntityPaths returns entity and datamap canonical files" <| fun _ ->
            let paths =
                ArcEntityPathRules.buildCanonicalEntityPaths ArcEntityPathRules.AddZone.Workflows "MyWorkflow"

            Expect.equal
                paths
                [
                    "workflows/MyWorkflow/isa.workflow.xlsx"
                    "workflows/MyWorkflow/isa.datamap.xlsx"
                ]
                "Canonical rename merge paths should include entity and datamap files."
    ]
