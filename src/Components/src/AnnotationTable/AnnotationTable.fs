namespace Swate.Components

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

[<Mangle(false); Erase>]
type AnnotationTable =

    static member private InactiveTextRender(text: string, tcc: TableCellController, ?icon: ReactElement, ?debug) =
        TableCell.BaseCell(
            tcc.Index.y,
            tcc.Index.x,
            Html.div [
                prop.className [
                    if not tcc.IsSelected && tcc.Index.y = 0 then
                        "swt:bg-base-300"
                    "swt:flex swt:flex-row swt:gap-2 swt:items-center swt:h-full swt:max-w-full swt:px-2 swt:py-2 swt:w-full"
                ]
                prop.children [
                    if icon.IsSome then
                        icon.Value
                    Html.div [ prop.className "swt:truncate"; prop.text text ]
                ]
            ],
            props = [ prop.title text; prop.onClick (fun e -> tcc.onClick e) ],
            className = "swt:w-full swt:h-full",
            ?debug = debug
        )

    static member private ContextMenu(arcTable, setArcTable, tableRef: IRefValue<TableHandle>, containerRef, setModal, ?debug: bool) =

        ContextMenu.ContextMenu(
            (fun data ->
                let index = data |> unbox<CellCoordinate>
                if index.x = 0 && index.y > 0 then // index col
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
                ),
            ?debug = debug
        )

    static member private ModalController
        (arcTable: ArcTable, setArcTable, modal: AnnotationTable.ModalTypes, setModal, tableRef: IRefValue<TableHandle>, ?debug: bool)
        =

        let rmv =
            fun _ ->
                tableRef.current.focus ()
                setModal ModalTypes.None

        React.fragment [
            match modal with
            | ModalTypes.None -> Html.none
            | ModalTypes.Details cc ->
                if cc.x = 0 then // no details modal for index col
                    Html.none
                elif cc.y = 0 then // headers
                    let header = arcTable.Headers.[cc.x - 1]

                    let setHeader =
                        fun (newHeader: CompositeHeader) ->
                            try
                                arcTable.UpdateHeader(cc.x - 1, newHeader)
                                setArcTable arcTable
                            with exn ->
                                let exnMessage =
                                    if exn.Message.StartsWith("Tried setting header for column with invalid type of cells.") then
                                        "Your change does not work with Details. Use \"Edit\" instead."
                                    else
                                        exn.Message
                                setModal (ModalTypes.Error exnMessage)
                                failwith exn.Message

                    CompositeCellModal.CompositeHeaderModal(
                        header,
                        setHeader,
                        rmv
                    )

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
                        rmv,
                        header,
                        ?debug = debug
                    )
            | ModalTypes.Transform cc ->
                if cc.x = 0 then // no details modal for index col
                    Html.none
                elif cc.y = 0 then // headers
                    let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)

                    let setCell =
                        fun (cell: CompositeCell) ->
                            arcTable.SetCellAt(cc.x - 1, cc.y - 1, cell)
                            setArcTable arcTable

                    let header = arcTable.Headers.[cc.x - 1]

                    CompositeCellEditModal.CompositeCellTransformModal(
                        cell,
                        header,
                        setCell,
                        rmv
                    )
                else
                    let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)

                    let setCell =
                        fun (cell: CompositeCell) ->
                            arcTable.SetCellAt(cc.x - 1, cc.y - 1, cell)
                            setArcTable arcTable

                    let header = arcTable.Headers.[cc.x - 1]

                    CompositeCellEditModal.CompositeCellTransformModal(
                        cell,
                        header,
                        setCell,
                        rmv
                    )
            | ModalTypes.Edit cc ->
                if cc.x = 0 then // no details modal for index col
                    Html.none
                elif cc.y = 0 then // headers
                    EditConfig.CompositeCellEditModal(cc.x-1, arcTable, setArcTable, rmv, ?debug = debug)
                else
                    EditConfig.CompositeCellEditModal(cc.x-1, arcTable, setArcTable, rmv, ?debug = debug)
            | ModalTypes.MoveColumn(uiTableIndex, arcTableIndex) ->
                ContextMenuModals.MoveColumnModal(
                    arcTable,
                    setArcTable,
                    arcTableIndex,
                    uiTableIndex,
                    setModal,
                    tableRef,
                    ?debug = debug
                )

            | ModalTypes.PasteCaseUserInput(AddColumns addColumns) ->
                ContextMenuModals.PasteFullColumnsModal(arcTable, setArcTable, addColumns, setModal, tableRef)
            | ModalTypes.Error(exn) ->
                ContextMenuModals.ErrorModal(exn, setModal, tableRef)
            | ModalTypes.UnknownPasteCase(Unknown unknownPasteCase) ->
                ContextMenuModals.UnknownPasteCase(unknownPasteCase.data, unknownPasteCase.headers, setModal, tableRef)
            | anyElse ->
                console.warn ("Unknown modal type", anyElse)
                Html.none
        ]


    [<ReactComponent(true)>]
    static member AnnotationTable(arcTable: ArcTable, setArcTable: ArcTable -> unit, ?height: int, ?debug: bool) =
        let containerRef = React.useElementRef ()
        let tableRef = React.useRef<TableHandle> (null)
        let (modal: ModalTypes), setModal = React.useState ModalTypes.None
        let debug = defaultArg debug false
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
                                "swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-center swt:cursor-not-allowed swt:w-full swt:h-full swt:bg-base-200",
                            debug = debug
                        )
                    | Some(U2.Case2 header) ->
                        let text = header.ToString()
                        AnnotationTable.InactiveTextRender(text, tcc, debug = debug)
                    | Some(U2.Case1 cell) ->
                        let text = cell.ToString()

                        let termAccession =
                            match cell with
                            | term when term.isTerm -> cell.AsTerm.TermAccessionShort
                            | unit when unit.isUnitized -> (snd cell.AsUnitized).TermAccessionShort
                            | _ -> ""

                        let icon =
                            if System.String.IsNullOrWhiteSpace termAccession |> not then
                                Html.i [
                                    prop.className "swt:text-primary"
                                    prop.title termAccession
                                    prop.children [ Icons.Check() ]
                                ]
                                |> Some
                            else
                                None

                        AnnotationTable.InactiveTextRender(text, tcc, ?icon = icon, debug = debug)
                ),
                withKey =
                    fun (tcc: TableCellController, compositeCell: U2<CompositeCell, CompositeHeader> option) ->
                        $"cellRender-{tcc.Index.x}-{tcc.Index.y}"
            )
        let renderActiveCell =
            React.memo (
                (fun (tcc: TableCellController) ->
                    match tcc with
                    | _ when tcc.Index.x > 0 && tcc.Index.y > 0 ->
                        let setCell =
                            fun (cell: CellCoordinate) (cc: CompositeCell) ->
                                arcTable.SetCellAt(cell.x - 1, cell.y - 1, cc)

                        let cell = arcTable.GetCellAt(tcc.Index.x - 1, tcc.Index.y - 1)
                        TableCell.CompositeCellActiveRender(tcc, cell, setCell tcc.Index, debug = debug, displayIndicators = false)
                    | _ when tcc.Index.x > 0 && tcc.Index.y = 0 ->
                        let setHeader =
                            fun (index: int) (ch: CompositeHeader) ->
                                arcTable.UpdateHeader(index - 1, ch)
                        let header = arcTable.GetColumn(tcc.Index.x - 1).Header
                        TableCell.CompositeHeaderActiveRender(tcc, header, setHeader (tcc.Index.x), debug = debug, displayIndicators = false)
                    | _ -> Html.div "Unknown cell type"
                )
            )

        Html.div [
            prop.ref containerRef
            if debug then
                prop.testId "annotation_table"
                prop.custom("data-columnCount", arcTable.ColumnCount)
                prop.custom("data-rowCount", arcTable.RowCount)
            prop.children [
                ReactDOM.createPortal ( // Modals
                    AnnotationTable.ModalController(arcTable, setArcTable, modal, setModal, tableRef, debug = debug),
                    Browser.Dom.document.body
                )
                AnnotationTable.ContextMenu(arcTable, setArcTable, tableRef, containerRef, setModal, debug)
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

                            cellRender (tcc, cell)
                        ),
                    renderActiveCell = renderActiveCell,
                    ref = tableRef,
                    ?height = height,
                    onKeydown =
                        (fun (e, selectedCells, activeCell) ->
                            if
                                (e.ctrlKey || e.metaKey)
                                && e.code = kbdEventCode.enter
                                && activeCell.IsNone
                                && selectedCells.count > 0
                            then
                                let cell = selectedCells.selectedCellsReducedSet.MinimumElement
                                setModal (ModalTypes.Details cell)
                            elif e.code = kbdEventCode.delete && selectedCells.count > 0 then
                                arcTable.ClearSelectedCells(tableRef.current.SelectHandle)
                                arcTable.Copy() |> setArcTable
                        ),
                    enableColumnHeaderSelect = true,
                    debug = debug
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

        arcTable.AddColumn(
            CompositeHeader.Output IOType.Data,
            [|
                for i in 0..100 do
                    let newData = Data.create(string i, $"Sample {i}", DataFile.RawDataFile, "Some Format", $"Selector Format {i}", ResizeArray[Comment.create("Test", string i)])
                    CompositeCell.createData(newData)
            |]
        )

        let table, setTable = React.useState (arcTable)

        AnnotationTable.AnnotationTable(table, setTable, height = 600)