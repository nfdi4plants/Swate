namespace Fixture

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
open Feliz.DaisyUI
open ARCtrl
open ARCtrl.Spreadsheet

open Types.AnnotationTableContextMenu
open Types.AnnotationTable

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

    static member hugeRenderingTable () =
        let arcTable =
            ARCtrl.ArcTable("TestTableHuge", ResizeArray(), System.Collections.Generic.Dictionary())

        let inputColumn =
            let cells =
                [|
                    for i in 0..100 do
                        CompositeCell.createFreeText $"Source {i}"
                |]
            CompositeColumn.create(CompositeHeader.Input IOType.Source, cells)
        let outPutColumn =
            let cells =
                [|
                    for i in 0..100 do
                        CompositeCell.createFreeText $"Sample {i}"
                |]
            CompositeColumn.create(CompositeHeader.Output IOType.Sample, cells)
        let componentColumns =
            let cells = 
                [|
                    for i in 0..100 do
                        CompositeCell.createTermFromString ("SCIEX instrument model", "MS", "MS:11111231")
                |]
            let componentColumn = CompositeColumn.create(CompositeHeader.Component(OntologyAnnotation("instrument model", "MS", "MS:2138970")), cells)
            Array.create 50 componentColumn
        let parameterColumns =
            let cells = 
                [|
                    for i in 0..100 do
                        CompositeCell.createUnitizedFromString(string i, "Degree Celsius", "UO", "UO:000000001")
                |]
            let parameterColumn = CompositeColumn.create(CompositeHeader.Parameter(OntologyAnnotation("Temperature", "UO", "UO:123435345")), cells)
            Array.create 50 parameterColumn

        let compositeColumns =
            let collection =
                Array.append componentColumns parameterColumns
                |> List.ofArray
                |> (fun list -> list @ [outPutColumn])
            inputColumn::collection
            |> Array.ofList
        arcTable.AddColumns(compositeColumns)
        arcTable

    static member BaseCell(rowIndex, columnIndex, data) =
        let content = Html.div (data.ToString())
        TableCell.BaseCell(rowIndex, columnIndex, content, debug = true)

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

    static member AnnotationTable(arcTable, setArcTable) =
        AnnotationTable.AnnotationTable(arcTable, setArcTable, height = 600, debug = true)

    static member ContextMenu(arcTable, setArcTable, tableRef: IRefValue<TableHandle>, containerRef, setModal) =
        ContextMenu.ContextMenu(
            (fun data ->
                let index = data |> unbox<CellCoordinate>

                if index.x = 0 then // index col
                    AnnotationTableContextMenu.IndexColumnContent(
                        index.y,
                        arcTable,
                        setArcTable,
                        tableRef.current.SelectHandle
                    )
                elif index.y = 0 then // header Row
                    AnnotationTableContextMenu.CompositeHeaderContent(
                        index.x,
                        arcTable,
                        setArcTable,
                        tableRef.current.SelectHandle,
                        setModal
                    )
                else // standard cell
                    AnnotationTableContextMenu.CompositeCellContent(
                        {| x = index.x; y = index.y |},
                        arcTable,
                        setArcTable,
                        tableRef.current.SelectHandle,
                        setModal
                    )
            ),
            ref = containerRef,
            onSpawn =
                (fun e ->
                    let target = e.target :?> Browser.Types.HTMLElement

                    match target.closest ("[data-row][data-column]"), containerRef.current with
                    | Some cell, Some container when container.contains (cell) ->
                        let cell = cell :?> Browser.Types.HTMLElement
                        let row = int cell?dataset?row
                        let col = int cell?dataset?column
                        let indices: CellCoordinate = {| y = row; x = col |}
                        console.log (indices)
                        Some indices
                    | _ ->
                        console.log ("No table cell found")
                        None
                )
        )