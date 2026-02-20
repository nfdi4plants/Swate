module Electron.Tests.DataAnnotatorWidget

open Fable.Mocha
open ARCtrl
open Swate.Components.Shared
open Renderer.components.Widgets

let private mkCells (count: int) =
    Array.init count (fun _ -> CompositeCell.FreeText "")
    |> ResizeArray

let private mkAnnotationInput selectors targetColumn : AnnotationInput = {
    Selectors = selectors
    FileName = "raw-data.tsv"
    FileType = "text/tab-separated-values"
    TargetColumn = targetColumn
}

let private expectOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: expected Ok, received Error: {err}"

let private expectError label result =
    match result with
    | Ok _ -> failwith $"{label}: expected Error, received Ok."
    | Error err -> err

let private assertDataCellFields (fileName: string) (fileType: string) (selector: string) (cell: CompositeCell) =
    match cell with
    | CompositeCell.Data data ->
        Expect.equal data.FilePath (Some fileName) "FilePath should be set on data cell"
        Expect.equal data.Format (Some fileType) "Format should be set on data cell"
        Expect.equal data.Selector (Some selector) "Selector should be set on data cell"
        Expect.equal data.SelectorFormat (Some URLs.Data.SelectorFormat.csv) "SelectorFormat should point to RFC7111"
    | _ ->
        failwith "Expected CompositeCell.Data"

let Main =
    testList "Electron Data Annotator" [
        testCase "sort mixed data targets numerically" <| fun _ ->
            let targets = set [
                DataAnnotator.DataTarget.Cell(1, 1)
                DataAnnotator.DataTarget.Row 10
                DataAnnotator.DataTarget.Column 11
                DataAnnotator.DataTarget.Cell(2, 0)
                DataAnnotator.DataTarget.Row 2
                DataAnnotator.DataTarget.Column 1
            ]

            let selectors = DataAnnotatorDataSource.SelectorsFromTargets true targets

            Expect.equal
                selectors
                [|
                    "cell=2,3"
                    "cell=3,2"
                    "col=2"
                    "col=12"
                    "row=4"
                    "row=12"
                |]
                "Selectors should be deterministic and sorted by numeric coordinates."

        testCase "autodetect chooses output when no input/output exists" <| fun _ ->
            let table = ArcTable.init "T"
            let header = DataAnnotatorDataSource.TryGetTargetHeader table DataAnnotator.TargetColumn.Autodetect |> expectOk "autodetect"
            Expect.equal header (CompositeHeader.Output IOType.Data) "Autodetect should default to Output on empty table"

        testCase "autodetect chooses output when only input exists" <| fun _ ->
            let table = ArcTable.init "T"
            table.AddColumn(CompositeHeader.Input IOType.Data, mkCells 1, forceReplace = true)
            let header = DataAnnotatorDataSource.TryGetTargetHeader table DataAnnotator.TargetColumn.Autodetect |> expectOk "autodetect"
            Expect.equal header (CompositeHeader.Output IOType.Data) "Autodetect should choose Output when only Input exists"

        testCase "autodetect chooses input when only output exists" <| fun _ ->
            let table = ArcTable.init "T"
            table.AddColumn(CompositeHeader.Output IOType.Data, mkCells 1, forceReplace = true)
            let header = DataAnnotatorDataSource.TryGetTargetHeader table DataAnnotator.TargetColumn.Autodetect |> expectOk "autodetect"
            Expect.equal header (CompositeHeader.Input IOType.Data) "Autodetect should choose Input when only Output exists"

        testCase "autodetect fails when input and output both exist" <| fun _ ->
            let table = ArcTable.init "T"
            table.AddColumn(CompositeHeader.Input IOType.Data, mkCells 1, forceReplace = true)
            table.AddColumn(CompositeHeader.Output IOType.Data, mkCells 1, forceReplace = true)
            let err = DataAnnotatorDataSource.TryGetTargetHeader table DataAnnotator.TargetColumn.Autodetect |> expectError "autodetect"
            Expect.equal
                err
                "Both Input and Output columns already exist. Select Input or Output explicitly."
                "Autodetect should fail when both IO columns already exist"

        testCase "applyToTable adds data column with populated data cells" <| fun _ ->
            let table = ArcTable.init "T"
            let selectors = [| "row=2"; "cell=3,2" |]
            let input = mkAnnotationInput selectors DataAnnotator.TargetColumn.Autodetect
            let count = DataAnnotatorDataSource.ApplyToTable table input |> expectOk "applyToTable"

            Expect.equal count selectors.Length "applyToTable should return selector count"

            let outputColumn = table.TryGetOutputColumn()
            Expect.equal outputColumn.IsSome true "Output data column should be available after apply"
            let outputColumn = outputColumn.Value

            Expect.equal outputColumn.Cells.Count selectors.Length "Output column should match selector count"
            assertDataCellFields input.FileName input.FileType selectors.[0] outputColumn.Cells.[0]
            assertDataCellFields input.FileName input.FileType selectors.[1] outputColumn.Cells.[1]

        testCase "applyToDataMap extends contexts and sets selector metadata" <| fun _ ->
            let dataMap = DataMap.init ()
            let selectors = [| "row=2"; "row=3"; "cell=4,2" |]
            let input = mkAnnotationInput selectors DataAnnotator.TargetColumn.Autodetect
            let count = DataAnnotatorDataSource.ApplyToDataMap dataMap input |> expectOk "applyToDataMap"

            Expect.equal count selectors.Length "applyToDataMap should return selector count"
            Expect.equal dataMap.DataContexts.Count selectors.Length "DataMap should be expanded to selector count"

            let ctx = dataMap.DataContexts.[0]
            Expect.equal ctx.FilePath (Some input.FileName) "DataMap context file path should be set"
            Expect.equal ctx.Format (Some input.FileType) "DataMap context format should be set"
            Expect.equal ctx.Selector (Some selectors.[0]) "DataMap context selector should be set"
            Expect.equal ctx.SelectorFormat (Some URLs.Data.SelectorFormat.csv) "DataMap selector format should point to RFC7111"

        testCase "parse uses default separator and still validates empty file data" <| fun _ ->
            let file = DataAnnotator.DataFile.create ("empty.csv", "text/csv", "", 0.0)

            let dataError = DataAnnotatorDataSource.TryParseDataFile " " file |> expectError "empty data validation"
            Expect.equal dataError "Parsed file does not contain any data rows." "Empty parsed file should be rejected"
    ]
