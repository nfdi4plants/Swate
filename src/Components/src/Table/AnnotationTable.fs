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

    static member InactiveTextRender(text: string, tcc: TableCellController, ?icon: ReactElement) =
        TableCell.BaseCell(
            tcc.Index.y,
            tcc.Index.x,
            Html.div [
                prop.className [
                    if not tcc.IsSelected && tcc.Index.y = 0 then
                        "bg-base-300"
                    "flex flex-row gap-2 items-center h-full max-w-full px-2 py-1 w-full"
                ]
                prop.children [
                    if icon.IsSome then
                        icon.Value
                    Html.div [ prop.className "truncate"; prop.text text ]
                ]
            ],
            props = [ prop.title text; prop.onClick (fun e -> tcc.onClick e) ],
            className = "w-full h-full"
        )

    [<ReactComponent(true)>]
    static member AnnotationTable(arcTable: ArcTable, setArcTable: ArcTable -> unit, ?debug: bool) =
        let containerRef = React.useElementRef ()
        let tableRef = React.useRef<TableHandle> (null)
        let (detailsModal: CellCoordinate option), setDetailsModal = React.useState (None)

        let cellRender =
            React.memo (
                (fun (tcc: TableCellController, compositeCell: U2<CompositeCell, CompositeHeader> option) ->
                    match compositeCell with
                    | None ->
                        TableCell.BaseCell(
                            tcc.Index.y,
                            tcc.Index.x,
                            Html.text tcc.Index.y,
                            className =
                                "px-2 py-1 flex items-center justify-center cursor-not-allowed w-full h-full bg-base-200"
                        )
                    | Some (U2.Case2 header) ->
                        let text = header.ToString()
                        AnnotationTable.InactiveTextRender(text, tcc)
                    | Some (U2.Case1 cell) ->
                        let text = cell.ToString()

                        let icon =
                            if
                                (cell.isTerm || cell.isUnitized)
                                && System.String.IsNullOrWhiteSpace cell.AsTerm.TermAccessionShort |> not
                            then
                                Html.i [
                                    prop.className "fa-solid fa-check text-primary"
                                    prop.title cell.AsTerm.TermAccessionShort
                                ]
                                |> Some
                            else
                                None

                        AnnotationTable.InactiveTextRender(text, tcc, ?icon = icon)
                ),
                withKey = fun (tcc: TableCellController, compositeCell: U2<CompositeCell, CompositeHeader> option) -> $"{tcc.Index.x}-{tcc.Index.y}"
            )

        let renderActiveCell =
            React.memo (
                (fun (tcc: TableCellController) ->
                    match tcc with
                    | cell when tcc.Index.x > 0 && tcc.Index.y > 0 ->
                        let setCell =
                            fun (cell: CellCoordinate) (cc: CompositeCell) ->
                                arcTable.SetCellAt(cell.x - 1, cell.y - 1, cc)

                        let cell = arcTable.GetCellAt(tcc.Index.x - 1, tcc.Index.y - 1)
                        TableCell.CompositeCellActiveRender(tcc, cell, setCell tcc.Index)
                    | _ -> Html.div "Unknown cell type")

            )
        Html.div [
            prop.ref containerRef
            prop.children [
                Html.div [
                    Html.button [
                        prop.className "btn btn-primary"
                        prop.onClick (fun _ ->
                            let iscontained = tableRef.current.SelectHandle.contains {| x = 2; y = 2 |}
                            console.log ("iscontained", iscontained))
                        prop.text "Verify 2,2"
                    ]
                ]
                ReactDOM.createPortal (
                    React.fragment [
                        match detailsModal with
                        | None -> Html.none
                        | Some cc ->
                            if cc.x = 0 then // no details modal for index col
                                Html.none
                            elif cc.y = 0 then // headers
                                let header = arcTable.Headers.[cc.x - 1]
                                Html.none
                            else
                                let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)

                                let setCell =
                                    fun (cell: CompositeCell) ->
                                        arcTable.SetCellAt(cc.x - 1, cc.y - 1, cell)
                                        setArcTable arcTable

                                let header = arcTable.Headers.[cc.x - 1]

                                CompositeCellModal.CompositeCellModal(
                                    cell,
                                    setCell,
                                    (fun _ ->
                                        tableRef.current.focus ()
                                        setDetailsModal None),
                                    header
                                )

                    ],
                    Browser.Dom.document.body
                )
                ContextMenu.ContextMenu(
                    (fun data ->
                        let index = data |> unbox<CellCoordinate>
                        if index.x = 0 then // index col
                            AnnotationTableContextMenu.IndexColumnContent(
                                index.y,
                                arcTable,
                                setArcTable
                            )
                        elif index.y = 0 then // header Row
                            AnnotationTableContextMenu.CompositeHeaderContent(
                                index.x,
                                arcTable,
                                setArcTable
                            )
                        else // standard cell
                            AnnotationTableContextMenu.CompositeCellContent(
                                {| x = index.x; y = index.y |},
                                arcTable,
                                setArcTable,
                                tableRef.current.SelectHandle
                            )

                        // [
                        //     for i in 0..5 do
                        //         ContextMenuItem(
                        //             text = Html.span $"Item {i}",
                        //             ?icon =
                        //                 (if i = 4 then
                        //                     Html.i [ prop.className "fa-solid fa-check" ] |> Some
                        //                 else
                        //                     None),
                        //             ?kbdbutton = (if i = 3 then {| element = Html.kbd [ prop.className "ml-auto kbd kbd-sm"; prop.text "Back" ]; label = "Back"|} |> Some else None),
                        //             onClick =
                        //                 (fun e ->
                        //                     e.buttonEvent.stopPropagation ()
                        //                     let index = e.spawnData |> unbox<CellCoordinate>
                        //                     console.log (sprintf "Item clicked: %i" i, index))
                        //         )
                        // ]
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
                                None)
                )
                Table.Table(
                    rowCount = arcTable.RowCount + 1,
                    columnCount = arcTable.ColumnCount + 1,
                    renderCell = (fun (tcc: TableCellController) ->
                        let cell =
                            if tcc.Index.x = 0 then
                                None
                            elif tcc.Index.y = 0 then
                                Some (arcTable.Headers.[tcc.Index.x - 1] |> U2.Case2)
                            else
                                Some (arcTable.GetCellAt(tcc.Index.x - 1, tcc.Index.y - 1) |> U2.Case1)
                        cellRender (tcc, cell)
                    ),
                    renderActiveCell = renderActiveCell,
                    ref = tableRef,
                    onKeydown =
                        (fun (e, selectedCells, activeCell) ->
                            if
                                (e.ctrlKey || e.metaKey)
                                && e.code = kbdEventCode.enter
                                && activeCell.IsNone
                                && selectedCells.count > 0
                            then
                                let cell = selectedCells.selectedCellsReducedSet.MinimumElement
                                console.log ("set details modal for:", cell)
                                setDetailsModal (Some cell)
                            elif e.code = kbdEventCode.delete && selectedCells.count > 0 then
                                arcTable.ClearSelectedCells(tableRef.current.SelectHandle)
                                arcTable.Copy()
                                |> setArcTable
                            ),
                    enableColumnHeaderSelect = true
                )
            ]
        ]

    static member Entry() =
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

        let table, setTable = React.useState (arcTable)

        AnnotationTable.AnnotationTable(table, setTable)