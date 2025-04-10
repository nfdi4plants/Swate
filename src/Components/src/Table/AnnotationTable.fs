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

    static member x = 0

    [<ReactComponent(true)>]
    static member AnnotationTable(arcTable: ArcTable, ?debug: bool) =
        let tableRef = React.useRef<TableHandle>(null)
        let setCell = fun (cell: CellCoordinate) (cc: CompositeCell) ->
            arcTable.SetCellAt(cell.x - 1, cell.y - 1, cc)

        let cellRender =
            React.memo (
                (fun (ts: CellCoordinate) ->
                    match ts with
                    | isIndexCol when ts.x = 0 ->
                        TableCell.StickyIndexColumn(ts.y, ?debug = debug)
                    | isIndexRow when ts.y = 0 ->
                        let header = arcTable.Headers.[(ts.x - 1)]
                        let render =
                            Html.div [
                                prop.className "truncate text-lg"
                                prop.text (header.ToString())
                            ]
                        TableCell.StickyHeader(ts.x, render, ?debug = debug)
                    | cell when ts.x > 0 && ts.y > 0 ->
                        let cell = arcTable.GetCellAt(ts.x - 1, ts.y - 1)
                        let text = cell.ToString()
                        TableCell.BaseCell(ts.y, ts.x,
                            Html.div [
                                prop.title text
                                prop.className "truncate flex flex-row gap-2 items-center"
                                prop.children [
                                    if (cell.isTerm || cell.isUnitized)
                                        && System.String.IsNullOrWhiteSpace cell.AsTerm.TermAccessionShort |> not then
                                        Html.i [
                                            prop.className "fa-solid fa-check text-primary "
                                        ]
                                    Html.text text
                                ]
                            ],
                            className = "px-2 py-1 flex items-center"
                        )
                    | _ ->
                        Html.div "Unknown cell type"
                ),
                withKey = fun (ts: CellCoordinate) -> $"{ts.x}-{ts.y}"
            )
        let renderActiveCell =
            React.memo(
                (fun (tcc: TableCellController) ->
                    match tcc with
                    | isIndexCol when tcc.Index.x = 0 ->
                        TableCell.StickyIndexColumn(tcc.Index.y, ?debug = debug)
                    | isIndexRow when tcc.Index.y = 0 ->
                        TableCell.StickyHeader(tcc.Index.x, ?debug = debug)
                    | cell when tcc.Index.x > 0 && tcc.Index.y > 0 ->
                        let cell = arcTable.GetCellAt(tcc.Index.x - 1, tcc.Index.y - 1)
                        TableCell.CompositeCellActiveRender(
                            tcc,
                            cell,
                            setCell tcc.Index
                        )
                    | _ ->
                        Html.div "Unknown cell type"

                )

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
                renderCell = cellRender,
                renderActiveCell = renderActiveCell,
                ref = tableRef,
                enableColumnHeaderSelect = true
            )
        ]

    static member Entry() =
        let arcTable = ARCtrl.ArcTable("TestTable", ResizeArray(), System.Collections.Generic.Dictionary())
        arcTable.AddColumn(CompositeHeader.Input IOType.Source, [|for i in 0 .. 100 do CompositeCell.createFreeText $"Source {i}"|])
        arcTable.AddColumn(CompositeHeader.Output IOType.Sample, [|for i in 0 .. 100 do CompositeCell.createFreeText $"Sample {i}"|])
        arcTable.AddColumn(CompositeHeader.Component (OntologyAnnotation ("instrument model", "MS", "MS:2138970")), [|for i in 0 .. 100 do CompositeCell.createTermFromString("SCIEX instrument model", "MS", "MS:11111231")|])
        AnnotationTable.AnnotationTable(arcTable = arcTable)
