namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI
open ARCtrl
open ARCtrl.Spreadsheet

[<Mangle(false); Erase>]
type AnnotationTable =

    static member InactiveTextRender(text: string, tcc: TableCellController, ?icon: ReactElement) =
        TableCell.BaseCell(
            tcc.Index.y,
            tcc.Index.x,
            Html.div [
                prop.className [
                    if not tcc.IsSelected && tcc.Index.y = 0 then
                        "swt:bg-base-300"
                    "swt:flex swt:flex-row swt:gap-2 swt:items-center swt:h-full swt:max-w-full swt:px-2 swt:py-1 swt:w-full"
                ]
                prop.children [
                    if icon.IsSome then
                        icon.Value
                    Html.div [ prop.className "swt:truncate"; prop.text text ]
                ]
            ],
            props = [ prop.title text; prop.onClick (fun e -> tcc.onClick e) ],
            className = "swt:w-full swt:h-full"
        )

    [<ReactComponent(true)>]
    static member AnnotationTable(arcTable: ArcTable, setArcTable: ArcTable -> unit, ?debug: bool) =
        let containerRef = React.useElementRef ()
        let tableRef = React.useRef<TableHandle> (null)
        let (detailsModal: CellCoordinate option), setDetailsModal = React.useState None
        let (pastCases: PasteCases option), setPastCases = React.useState None

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
                                "swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-center swt:cursor-not-allowed swt:w-full swt:h-full swt:bg-base-200"
                        )
                    | Some(U2.Case2 header) ->
                        let text = header.ToString()
                        AnnotationTable.InactiveTextRender(text, tcc)
                    | Some(U2.Case1 cell) ->
                        let text = cell.ToString()

                        let termAccession =
                            match cell with
                            | term when term.isTerm -> cell.AsTerm.TermAccessionShort
                            | unit when unit.isUnitized -> (snd cell.AsUnitized).TermAccessionShort
                            | _ -> ""

                        let icon =
                            if System.String.IsNullOrWhiteSpace termAccession |> not
                            then
                                Html.i [
                                    prop.className "fa-solid fa-check swt:text-primary"
                                    prop.title termAccession
                                ]
                                |> Some
                            else
                                None
                        AnnotationTable.InactiveTextRender(text, tcc, ?icon = icon)),
                withKey =
                    fun (tcc: TableCellController, compositeCell: U2<CompositeCell, CompositeHeader> option) ->
                        $"{tcc.Index.x}-{tcc.Index.y}"
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
                        prop.className "swt:btn swt:btn-primary"
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
                ReactDOM.createPortal (
                    React.fragment [
                        match pastCases with
                        | Some (PasteCases.AddColumns addColumns) ->
                            let rmv =
                                fun _ ->
                                    tableRef.current.focus ()
                                    setPastCases None

                            let addColumnsBtn compositeColumns columnIndex =
                                Html.button [
                                    prop.className "swt:btn swt:btn-outline swt:btn-primary"
                                    prop.text "Confirm"
                                    prop.onClick (fun _ ->
                                        arcTable.AddColumns(compositeColumns, columnIndex, false, false)
                                        arcTable.Copy() |> setArcTable
                                        rmv ())
                                ]

                            let headers = addColumns.data.[0]
                            let body = addColumns.data.[1..]
                            let columns = Seq.append [ headers ] body |> Seq.transpose
                            let columnsList = columns |> Seq.toArray |> Array.map (Seq.toArray)
                            let compositeColumns = ArcTable.composeColumns columnsList

                            BaseModal.BaseModal(
                                (fun _ -> rmv ()),
                                header = Html.div "Headers have been detected",
                                content =
                                    React.fragment [
                                        Html.div [
                                            prop.className "swt:overflow-x-auto"
                                            prop.children [
                                                Html.text "Preview"
                                                Html.table [
                                                    prop.className "swt:table swt:table-xs"
                                                    prop.children (
                                                        Html.thead [
                                                            Html.tr (
                                                                compositeColumns
                                                                |> Array.map (fun compositeColumn ->
                                                                    Html.th (compositeColumn.Header.ToString()))
                                                            )
                                                        ]
                                                    )
                                                ]
                                            ]
                                        ]
                                    ],
                                footer = React.fragment [ FooterButtons.Cancel(rmv); addColumnsBtn compositeColumns (addColumns.columnIndex + 1)],
                                contentClassInfo = CompositeCellModal.BaseModalContentClassOverride
                            )
                        | Some (PasteColumns pasteColumns)->
                            AnnotationTableContextMenuUtil.paste((pasteColumns.columnIndex, pasteColumns.rowIndex), arcTable, pasteColumns.data, tableRef.current.SelectHandle, setArcTable)
                            setPastCases None
                        | _ -> Html.none
                    ],
                    Browser.Dom.document.body
                )
                ContextMenu.ContextMenu(
                    (fun data ->
                        let index = data |> unbox<CellCoordinate>

                        if index.x = 0 then // index col
                            AnnotationTableContextMenu.IndexColumnContent(index.y, arcTable, setArcTable)
                        elif index.y = 0 then // header Row
                            AnnotationTableContextMenu.CompositeHeaderContent(index.x, arcTable, setArcTable)
                        else // standard cell
                            AnnotationTableContextMenu.CompositeCellContent(
                                {| x = index.x; y = index.y |},
                                arcTable,
                                setArcTable,
                                tableRef.current.SelectHandle,
                                setDetailsModal,
                                setPastCases
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
                    renderCell =
                        (fun (tcc: TableCellController) ->
                            let cell =
                                if tcc.Index.x = 0 then
                                    None
                                elif tcc.Index.y = 0 then
                                    Some(arcTable.Headers.[tcc.Index.x - 1] |> U2.Case2)
                                else
                                    Some(arcTable.GetCellAt(tcc.Index.x - 1, tcc.Index.y - 1) |> U2.Case1)

                            cellRender (tcc, cell)),
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
                                arcTable.Copy() |> setArcTable),
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