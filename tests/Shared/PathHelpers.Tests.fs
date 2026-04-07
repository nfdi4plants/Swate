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
    ]
