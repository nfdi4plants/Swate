module Swate.Components.Shared.DatamapParentInfoTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Swate.Components.Shared

let tests =
    testList "DatamapParentInfo" [
        testCase "creates parent info from supported folder paths"
        <| fun _ ->
            [
                "assays/Assay A", DataMapParent.Assay, "Assay A"
                "studies/Study A", DataMapParent.Study, "Study A"
                "runs/Run A", DataMapParent.Run, "Run A"
                "workflows/Workflow A", DataMapParent.Workflow, "Workflow A"
            ]
            |> List.iter (fun (path, expectedParent, expectedId) ->
                let actual = DatamapParentInfo.tryFromFolderPath path

                Expect.equal
                    actual
                    (Some(DatamapParentInfo.create expectedId expectedParent))
                    $"Expected {path} to identify its DataMap parent"
            )

        testCase "rejects paths that are not DataMap parent folders"
        <| fun _ ->
            [
                "assays"
                "assays/Assay A/isa.datamap.xlsx"
                "investigation"
                "notes/Note A"
            ]
            |> List.iter (fun path ->
                Expect.isNone
                    (DatamapParentInfo.tryFromFolderPath path)
                    $"Expected {path} not to identify a DataMap parent"
            )
    ]
