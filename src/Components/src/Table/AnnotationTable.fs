namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI
open ARCtrl

[<Mangle(false); Erase>]
type AnnotationTable =

    [<ReactComponent(true)>]
    static member AnnotationTable(arcTable: ArcTable, ?debug: bool) =
        let tableRef = React.useRef<TableHandle>(null)
        let activeCellIndex, setActiveCellIndex = React.useState(None: CellCoordinate option)
        let setCell = fun (cell: CellCoordinate) (cc: CompositeCell) ->
            arcTable.SetCellAt(cell.x - 1, cell.y - 1, cc)

        let cellRender =
            React.memo (
                (fun (cc: CellCoordinate, isSelected, isOrigin) ->
                    match cc with
                    | isIndexCol when cc.x = 0 ->
                        TableCell.StickyIndexColumn(cc.y, ?debug = debug)
                    | isIndexRow when cc.y = 0 ->
                        TableCell.StickyHeader(cc.x, ?debug = debug)
                    | cell when cc.x > 0 && cc.y > 0 ->
                        let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)
                        let isActive = activeCellIndex = Some cc
                        let setCellAt = fun (cell: CompositeCell) -> setCell cc cell
                        let cellState = TableCellState.init(isSelected, isOrigin, isActive)
                        TableCell.CompositeCell(cc.y, cc.x, cell, cellState, setCellAt, setActiveCellIndex, ?debug = debug)
                    | _ ->
                        Html.div "Unknown cell type"
                ),
                withKey = fun (cc: CellCoordinate, isSelected, isOrigin) -> $"{cc.x}-{cc.y}-{isSelected}-{isOrigin}"
            )
        React.fragment [
            Html.div [
                Html.button [
                    prop.className "btn btn-primary"
                    prop.onClick (fun _ ->
                        let iscontained = tableRef.current.select.contains {|x = 2; y = 2|}
                        console.log("iscontained", iscontained)
                    )
                    prop.text "Verify 2,2"
                ]
            ]
            Table.Table(
                rowCount = arcTable.RowCount + 1,
                columnCount = arcTable.ColumnCount + 1,
                renderCell = (fun cc is io -> cellRender(cc, is, io)),
                ref = tableRef,
                defaultStyleSelect = false
            )
        ]

    static member Entry() =
        let arcTable = ARCtrl.ArcTable("TestTable", ResizeArray(), System.Collections.Generic.Dictionary())
        arcTable.AddColumn(CompositeHeader.Input IOType.Source, [|for i in 0 .. 100 do CompositeCell.createFreeText $"Source {i}"|])
        arcTable.AddColumn(CompositeHeader.Output IOType.Sample, [|for i in 0 .. 100 do CompositeCell.createFreeText $"Sample {i}"|])
        arcTable.AddColumn(CompositeHeader.Component (OntologyAnnotation ("instrument model", "MS", "MS:2138970")), [|for i in 0 .. 100 do CompositeCell.createTermFromString("SCIEX instrument model", "MS", "MS:11111231")|])
        AnnotationTable.AnnotationTable(arcTable = arcTable)
