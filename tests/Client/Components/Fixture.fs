module Fixture

open ARCtrl
open ARCtrl.Spreadsheet
open Browser.Types
open Swate.Components
open Fable.Mocha
open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open ARCtrl
open ARCtrl.Spreadsheet
open Types.AnnotationTableContextMenu
open Types.AnnotationTable

/// <summary>
/// Creates a mock table with some sample data.
///
/// 1. Input [source]
/// 2. Output [sample]
/// 3. Component [instrument model]
/// </summary>
let mkTable () =
    let arcTable = ARCtrl.ArcTable("TestTable", ResizeArray())

    arcTable.AddColumn(
        CompositeHeader.Input IOType.Source,
        [|
            for i in 0..100 do
                CompositeCell.createFreeText $"Source {i}"
        |]
        |> ResizeArray
    )

    arcTable.AddColumn(
        CompositeHeader.Output IOType.Data,
        [|
            for i in 0..100 do
                CompositeCell.createDataFromString (string i, format = "xlsx", selectorFormat = ";")
        |]
        |> ResizeArray
    )

    arcTable.AddColumn(
        CompositeHeader.Component(OntologyAnnotation("instrument model", "MS", "MS:2138970")),
        [|
            for i in 0..100 do
                CompositeCell.createTermFromString ("SCIEX instrument model", "MS", "MS:11111231")
        |]
        |> ResizeArray
    )

    arcTable.AddColumn(
        CompositeHeader.Parameter(OntologyAnnotation("Temperature", "UO", "UO:123435345")),
        [|
            for i in 0..100 do
                CompositeCell.createUnitizedFromString (string i, "Degree Celsius", "UO", "UO:000000001")
        |]
        |> ResizeArray
    )

    arcTable

let hugeRenderingTable () =
    let arcTable = ARCtrl.ArcTable("TestTableHuge", ResizeArray())

    let inputColumn =
        let cells = [|
            for i in 0..100 do
                CompositeCell.createFreeText $"Source {i}"
        |]

        CompositeColumn.create (CompositeHeader.Input IOType.Source, cells |> ResizeArray)

    let outPutColumn =
        let cells = [|
            for i in 0..100 do
                CompositeCell.createFreeText $"Sample {i}"
        |]

        CompositeColumn.create (CompositeHeader.Output IOType.Sample, cells |> ResizeArray)

    let componentColumns =
        let cells = [|
            for i in 0..100 do
                CompositeCell.createTermFromString ("SCIEX instrument model", "MS", "MS:11111231")
        |]

        let componentColumn =
            CompositeColumn.create (
                CompositeHeader.Component(OntologyAnnotation("instrument model", "MS", "MS:2138970")),
                cells |> ResizeArray
            )

        Array.create 50 componentColumn

    let parameterColumns =
        let cells = [|
            for i in 0..100 do
                CompositeCell.createUnitizedFromString (string i, "Degree Celsius", "UO", "UO:000000001")
        |]

        let parameterColumn =
            CompositeColumn.create (
                CompositeHeader.Parameter(OntologyAnnotation("Temperature", "UO", "UO:123435345")),
                cells |> ResizeArray
            )

        Array.create 50 parameterColumn

    let compositeColumns =
        let collection =
            Array.append componentColumns parameterColumns
            |> List.ofArray
            |> (fun list -> list @ [ outPutColumn ])

        inputColumn :: collection |> Array.ofList

    arcTable.AddColumns(compositeColumns)
    arcTable

let BaseCell (rowIndex, columnIndex, data) =
    let content = Html.div (data.ToString())
    TableCell.BaseCell(rowIndex, columnIndex, content, debug = true)

let getRangeOfSelectedCells (selectHande: SelectHandle) =
    selectHande.getSelectedCells ()
    |> Array.ofSeq
    |> Array.ofSeq
    |> Array.groupBy (fun item -> item.y)
    |> Array.map (fun (_, row) -> row)

///Start with 1 and not 0!
let mkSelectHandle (yStart, yEnd, xStart, xEnd) =
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

let Component_Term_InstrumentModel_String = [|
    [|
        "Characteristic [plant structure development stage]"
        "Term Source REF (PO:0009012)"
        "Term Accession Number (PO:0009012)"
    |]
    [| "SCIEX instrument model"; "MS"; "MS:424242" |]
|]

let Component_InstrumentModel_Term_Column =
    let header =
        CompositeHeader.Characteristic(
            OntologyAnnotation.create ("plant structure development stage", "PO", "PO:0009012")
        )

    let body =
        CompositeCell.Term(OntologyAnnotation.create ("SCIEX instrument model", "MS", "MS:424242"))

    CompositeColumn.create (header, [| body |] |> ResizeArray)

let Component_InstrumentModel_Term_Body =
    let header =
        CompositeHeader.Component(OntologyAnnotation.create ("instrument model", "MS", "MS:2138970"))

    let body =
        CompositeCell.Term(OntologyAnnotation.create ("SCIEX instrument model", "MS", "MS:424242"))

    CompositeColumn.create (header, [| body |] |> ResizeArray)

let Component_Unit_InstrumentModel_String = [|
    [|
        "Characteristic [plant structure development stage]"
        "Unit"
        "Term Source REF (PO:0009012)"
        "Term Accession Number (PO:0009012)"
    |]
    [|
        "My mass spec"
        "SCIEX instrument model"
        "MS"
        "MS:424242"
    |]
|]

let Component_InstrumentModel_Unit_Column =
    let header =
        CompositeHeader.Characteristic(
            OntologyAnnotation.create ("plant structure development stage", "PO", "PO:0009012")
        )

    let body =
        CompositeCell.Unitized("My mass spec", OntologyAnnotation.create ("SCIEX instrument model", "MS", "MS:424242"))

    CompositeColumn.create (header, [| body |] |> ResizeArray)

let Component_Unit_InstrumentModel_Unit_Term_String = [|
    [|
        "Characteristic [plant structure development stage]"
        "Unit"
        "Term Source REF (PO:0009012)"
        "Term Accession Number (PO:0009012)"
        "Characteristic [plant structure development stage]"
        "Term Source REF (PO:0009012)"
        "Term Accession Number (PO:0009012)"
    |]
    [|
        "My mass spec"
        "SCIEX instrument model"
        "MS"
        "MS:424242"
        "SCIEX instrument model"
        "MS"
        "MS:424242"
    |]
|]

let Component_Unit_InstrumentModel_Unit_Term_Columns = [|
    Component_InstrumentModel_Unit_Column
    Component_InstrumentModel_Term_Column
|]

let Body_Component_InstrumentModel_SingleRow_Term_String = [| [| "SCIEX instrument model"; "MS"; "MS:424242" |] |]

let Body_Component_InstrumentModel_Pseudo_SingleRow_String = [|
    [| "Component_ [instrument model]"; "TSR ()"; "TAN ()" |]
|]

let Body_Component_InstrumentModel_Pseudo_SingleRow_Column =
    let header = CompositeHeader.Input IOType.Source
    let body = [| CompositeCell.FreeText "Component_ [instrument model]" |]
    CompositeColumn.create (header, body |> ResizeArray)

let Body_Component_InstrumentModel_SingleRow_1Freetext_1_Term_Strings = [|
    [| "Test"; "SCIEX instrument model"; "MS"; "MS:424242" |]
|]

let Body_Component_InstrumentModel_SingleRow_1Freetext_1_Term_Columns =
    let header1 = CompositeHeader.Output IOType.Data
    let body1 = CompositeCell.FreeText "Test"
    let column1 = CompositeColumn.create (header1, [| body1 |] |> ResizeArray)

    let header2 =
        CompositeHeader.Component(OntologyAnnotation.create ("instrument model", "MS", "MS:2138970"))

    let body2 =
        CompositeCell.Term(OntologyAnnotation.create ("SCIEX instrument model", "MS", "MS:424242"))

    let column2 = CompositeColumn.create (header2, [| body2 |] |> ResizeArray)
    [| column1; column2 |]

let Body_Component_InstrumentModel_SingleRow_Unit_Strings = [|
    [|
        "Test"
        "Testi"
        "My Mass Spec"
        "SCIEX instrument model"
        "MS"
        "MS:424242"
    |]
|]

let Body_Component_InstrumentModel_TwoRows_Term_Strings = [|
    [| "SCIEX instrument model"; "MS"; "MS:424242" |]
    [| "New SCIEX instrument model"; "MS"; "MS:434343" |]
|]

let Body_Component_InstrumentModel_TwoRows_Term_Column =
    let header = CompositeHeader.Input IOType.Source
    let body1 = CompositeCell.FreeText "SCIEX instrument model"
    let body2 = CompositeCell.FreeText "New SCIEX instrument model"
    CompositeColumn.create (header, [| body1; body2 |] |> ResizeArray)

let Body_Component_InstrumentModel_ThreeRows_Term_Strings = [|
    [| "SCIEX instrument model"; "MS"; "MS:424242" |]
    [| "New SCIEX instrument model"; "MS"; "MS:434343" |]
    [| "Super New SCIEX instrument model"; "MS"; "MS:444444" |]
|]

let Body_Component_InstrumentModel_ThreeRows_Term_Column =
    let header = CompositeHeader.Input IOType.Source
    let body1 = CompositeCell.FreeText "SCIEX instrument model"
    let body2 = CompositeCell.FreeText "New SCIEX instrument model"
    let body3 = CompositeCell.FreeText "Super New SCIEX instrument model"
    CompositeColumn.create (header, [| body1; body2; body3 |] |> ResizeArray)

let Body_Component_InstrumentModel_TwoColumns_Term_Strings = [|
    [|
        "SCIEX instrument model"
        "MS"
        "MS:424242"
        "New SCIEX instrument model"
        "MS"
        "MS:434343"
    |]
|]

let Body_Component_InstrumentModel_TwoColumns_Term_Columns =
    let header1 = CompositeHeader.Input IOType.Source
    let body1 = CompositeCell.FreeText "SCIEX instrument model"
    let column1 = CompositeColumn.create (header1, [| body1 |] |> ResizeArray)
    let header2 = CompositeHeader.Output IOType.Data
    let body2 = CompositeCell.FreeText "MS"
    let column2 = CompositeColumn.create (header2, [| body2 |] |> ResizeArray)
    [| column1; column2 |]

let Body_Component_InstrumentModel_TwoRowsColumns_Term_Strings = [|
    [|
        "SCIEX instrument model"
        "MS"
        "MS:424242"
        "New SCIEX instrument model"
        "MS"
        "MS:434343"
    |]
    [|
        "SCIEX instrument model"
        "MS"
        "MS:424242"
        "New SCIEX instrument model"
        "MS"
        "MS:434343"
    |]
|]

let Body_Component_InstrumentModel_TwoRowsColumns_Term_Columns =
    let header1 = CompositeHeader.Input IOType.Source
    let body1 = CompositeCell.FreeText "SCIEX instrument model"
    let column1 = CompositeColumn.create (header1, [| body1; body1 |] |> ResizeArray)
    let header2 = CompositeHeader.Output IOType.Data
    let body2 = CompositeCell.FreeText "MS"
    let column2 = CompositeColumn.create (header2, [| body2; body2 |] |> ResizeArray)
    [| column1; column2 |]

let Body_Integer = [| [| "4" |] |]

let Body_Integer_Column =
    let header =
        CompositeHeader.Component(OntologyAnnotation("instrument model", "MS", "MS:2138970"))

    let body = CompositeCell.createUnitizedFromString ("4")
    CompositeColumn.create (header, [| body |] |> ResizeArray)