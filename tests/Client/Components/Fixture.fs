namespace Fixture

open Fable.Mocha
open ARCtrl
open ARCtrl.Spreadsheet
open Swate.Components

type Fixture =

    /// <summary>
    /// Creates a mock table with some sample data.
    ///
    /// 1. Input [source]
    /// 2. Output [sample]
    /// 3. Component [instrument model]
    /// </summary>
    static member mkTable () =
        let arcTable =
            ARCtrl.ArcTable("TestTable", ResizeArray(), System.Collections.Generic.Dictionary())

        arcTable.AddColumn(
            CompositeHeader.Input IOType.Source,
            [|
                for i in 0..100 do
                    CompositeCell.createFreeText $"Source {i}"
            |]
        )

        arcTable.AddColumn(
            CompositeHeader.Output IOType.Sample,
            [|
                for i in 0..100 do
                    CompositeCell.createFreeText $"Sample {i}"
            |]
        )

        arcTable.AddColumn(
            CompositeHeader.Component(OntologyAnnotation("instrument model", "MS", "MS:2138970")),
            [|
                for i in 0..100 do
                    CompositeCell.createTermFromString ("SCIEX instrument model", "MS", "MS:11111231")
            |]
        )

        arcTable.AddColumn(
            CompositeHeader.Parameter(OntologyAnnotation("Temperature", "UO", "UO:123435345")),
            [|
                for i in 0..100 do
                    CompositeCell.createUnitizedFromString(string i, "Degree Celsius", "UO", "UO:000000001")
            |]
        )

        arcTable

    static member getRangeOfSelectedCells (selectHande: SelectHandle) =
        selectHande.getSelectedCells ()
        |> Array.ofSeq
        |> Array.ofSeq
        |> Array.groupBy (fun item -> item.y)
        |> Array.map (fun (_, row) -> row)

    ///Start with 1 and not 0!
    static member mkSelectHandle (yStart, yEnd, xStart, xEnd) =
        let range: CellCoordinateRange = {|
            yStart = yStart
            yEnd = yEnd
            xStart = xStart
            xEnd = xEnd
        |}

        new SelectHandle(
            (fun _ -> failwith "Not implemented"),
            (fun _ -> failwith "Not implemented"),
            (fun _ -> failwith "Not implemented"),
            (fun _ -> Some range),
            (fun _ -> CellCoordinateRange.toArray range),
            (fun _ -> CellCoordinateRange.count range)
        )

    static member Column_Component_InstrumentModel = [|
            [| "Component [instrument model]"; "TSR ()"; "TAN ()" |]
            [| "SCIEX instrument model"; "MS"; "MS:424242" |]
        |]

    static member Body_Component_InstrumentModel_SingleRow = [|
            [| "SCIEX instrument model"; "MS"; "MS:424242" |]
        |]

    static member Body_Component_InstrumentModel_SingleRow_Term = [|
            [| "Test"; "Testi"; "SCIEX instrument model"; "MS"; "MS:424242" |]
        |]

    static member Body_Component_InstrumentModel_SingleRow_Unit = [|
            [| "Test"; "Testi"; "My Mass Spec"; "SCIEX instrument model"; "MS"; "MS:424242" |]
        |]

    static member Body_Component_InstrumentModel_TwoRows = [|
            [| "SCIEX instrument model"; "MS"; "MS:424242" |]
            [| "SCIEX instrument model"; "MS"; "MS:434343" |]
        |]

    static member Body_Integer = [|
            [| "4" |]
        |]

    static member Body_Empty = [|
            [| "" |]
        |]
