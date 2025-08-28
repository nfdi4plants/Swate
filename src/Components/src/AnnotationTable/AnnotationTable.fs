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

    static member private CompositeHeaderActiveRender
        (tableCellController: TableCellController, header: CompositeHeader, setHeader: CompositeHeader -> unit, ?debug)
        =

        match header with
        | CompositeHeader.Input io ->
            TableCell.StringActiveCell(
                tableCellController,
                io.ToString(),
                (fun input -> IOType.ofString input |> CompositeHeader.Input |> setHeader),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Output io ->
            TableCell.StringActiveCell(
                tableCellController,
                io.ToString(),
                (fun input -> IOType.ofString input |> CompositeHeader.Output |> setHeader),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Comment txt ->
            TableCell.StringActiveCell(
                tableCellController,
                txt,
                (fun input -> setHeader (CompositeHeader.Comment input)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.FreeText txt ->
            TableCell.StringActiveCell(
                tableCellController,
                txt,
                (fun input -> setHeader (CompositeHeader.FreeText input)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Parameter oa ->
            TableCell.OntologyAnnotationActiveCell(
                tableCellController,
                oa,
                (fun t -> setHeader (CompositeHeader.Parameter t)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Characteristic oa ->
            TableCell.OntologyAnnotationActiveCell(
                tableCellController,
                oa,
                (fun t -> setHeader (CompositeHeader.Characteristic t)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Component oa ->
            TableCell.OntologyAnnotationActiveCell(
                tableCellController,
                oa,
                (fun t -> setHeader (CompositeHeader.Component t)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Factor oa ->
            TableCell.OntologyAnnotationActiveCell(
                tableCellController,
                oa,
                (fun t -> setHeader (CompositeHeader.Factor t)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Performer
        | CompositeHeader.ProtocolDescription
        | CompositeHeader.ProtocolREF
        | CompositeHeader.ProtocolType
        | CompositeHeader.ProtocolUri
        | CompositeHeader.ProtocolVersion
        | CompositeHeader.Date ->
            TableCell.StringActiveCell(
                tableCellController,
                header.ToString(),
                (fun input ->
                    let nextHeader = CompositeHeader.OfHeaderString input
                    setHeader nextHeader
                ),
                isStickyHeader = true,
                ?debug = debug
            )

    static member CompositeCellActiveRender
        (tableCellController: TableCellController, cell: CompositeCell, setCell: CompositeCell -> unit, ?debug)
        =

        match cell with
        | CompositeCell.Term oa ->
            TableCell.OntologyAnnotationActiveCell(
                tableCellController,
                oa,
                (fun t -> setCell (CompositeCell.Term t)),
                ?debug = debug
            )
        | CompositeCell.FreeText txt ->
            TableCell.StringActiveCell(
                tableCellController,
                txt,
                (fun t -> setCell (CompositeCell.FreeText t)),
                ?debug = debug
            )
        | CompositeCell.Unitized(v, oa) ->
            TableCell.StringActiveCell(
                tableCellController,
                v,
                (fun _ -> setCell (CompositeCell.Unitized(v, oa))),
                ?debug = debug
            )
        | CompositeCell.Data d ->
            TableCell.StringActiveCell(
                tableCellController,
                Option.defaultValue "" d.Name,
                (fun t ->
                    d.Name <- t |> Option.whereNot System.String.IsNullOrWhiteSpace
                    setCell (CompositeCell.Data d)
                ),
                ?debug = debug
            )

    [<ReactComponentAttribute>]
    static member private ActiveCellRender
        (tcc: TableCellController, arcTable: ArcTable, setTable: ArcTable -> unit, ?debug: bool)
        =
        match tcc with
        | _ when tcc.Index.x > 0 && tcc.Index.y > 0 ->
            let setCell =
                fun (cell: CellCoordinate) (cc: CompositeCell) ->
                    let nextTable = arcTable |> ArcTable.setCellAt (cell.x - 1, cell.y - 1, cc)
                    setTable nextTable

            let cell = arcTable.GetCellAt(tcc.Index.x - 1, tcc.Index.y - 1)
            AnnotationTable.CompositeCellActiveRender(tcc, cell, setCell tcc.Index, ?debug = debug)
        | _ when tcc.Index.x > 0 && tcc.Index.y = 0 ->
            let setHeader =
                fun (index: int) (ch: CompositeHeader) ->
                    let nextTable = arcTable |> ArcTable.updateHeader (index - 1, ch)
                    setTable nextTable

            let header = arcTable.GetColumn(tcc.Index.x - 1).Header
            AnnotationTable.CompositeHeaderActiveRender(tcc, header, setHeader (tcc.Index.x), ?debug = debug)
        | _ -> Html.div "Unknown cell type"


    static member private ContextMenu
        (arcTable: ArcTable, setArcTable, tableRef: IRefValue<TableHandle>, containerRef, setModal, ?debug: bool)
        =

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
        (
            arcTable: ArcTable,
            setArcTable,
            modal: AnnotationTable.ModalTypes,
            setModal,
            tableRef: IRefValue<TableHandle>,
            ?debug: bool
        ) =

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
                                    if
                                        exn.Message.StartsWith(
                                            "Tried setting header for column with invalid type of cells."
                                        )
                                    then
                                        "Your change does not work with Details. Use \"Edit\" instead."
                                    else
                                        exn.Message

                                setModal (ModalTypes.Error exnMessage)
                                failwith exn.Message

                    CompositeCellModal.CompositeHeaderModal(header, setHeader, rmv)

                else
                    let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)

                    let setCell =
                        fun (cell: CompositeCell) ->
                            arcTable.SetCellAt(cc.x - 1, cc.y - 1, cell)
                            setArcTable arcTable

                    let header = arcTable.Headers.[cc.x - 1]

                    CompositeCellModal.CompositeCellModal(cell, setCell, rmv, header, ?debug = debug)
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

                    CompositeCellEditModal.CompositeCellTransformModal(cell, header, setCell, rmv)
                else
                    let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)

                    let setCell =
                        fun (cell: CompositeCell) ->
                            arcTable.SetCellAt(cc.x - 1, cc.y - 1, cell)
                            setArcTable arcTable

                    let header = arcTable.Headers.[cc.x - 1]

                    CompositeCellEditModal.CompositeCellTransformModal(cell, header, setCell, rmv)
            | ModalTypes.Edit cc ->
                if cc.x = 0 then // no details modal for index col
                    Html.none
                elif cc.y = 0 then // headers
                    EditConfig.CompositeCellEditModal(cc.x - 1, arcTable, setArcTable, rmv, ?debug = debug)
                else
                    EditConfig.CompositeCellEditModal(cc.x - 1, arcTable, setArcTable, rmv, ?debug = debug)
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
            | ModalTypes.Error(exn) -> ContextMenuModals.ErrorModal(exn, setModal, tableRef)
            | ModalTypes.UnknownPasteCase(Unknown unknownPasteCase) ->
                ContextMenuModals.UnknownPasteCase(unknownPasteCase.data, unknownPasteCase.headers, setModal, tableRef)
            | anyElse ->
                console.warn ("Unknown modal type", anyElse)
                Html.none
        ]


    [<ReactComponent(true)>]
    static member AnnotationTable
        (arcTable: ArcTable, setArcTable: ArcTable -> unit, ?onSelect: GridSelect.OnSelect, ?height: int, ?debug: bool)
        =
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
                                "swt:rounded-0 swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-center swt:cursor-not-allowed swt:w-full swt:h-full swt:bg-base-200",
                            debug = debug
                        )
                    | Some(U2.Case2 header) ->
                        let text = header.ToString()
                        TableCell.StringInactiveCell(tcc, text, debug = debug)
                    | Some(U2.Case1 cell) ->
                        let text = cell.ToString()

                        let termAccession =
                            match cell with
                            | term when term.isTerm -> cell.AsTerm.TermAccessionShort
                            | unit when unit.isUnitized -> (snd cell.AsUnitized).TermAccessionShort
                            | _ -> ""

                        let oa = cell.ToOA()

                        TableCell.InactiveCell(
                            tcc,
                            Html.div [
                                prop.className "swt:flex swt:w-full swt:justify-between"
                                prop.children [
                                    Html.text text
                                    if oa.TermAccessionShort |> System.String.IsNullOrWhiteSpace |> not then
                                        Html.i [
                                            prop.className "swt:text-primary"
                                            prop.title termAccession
                                            prop.children [ Icons.Check() ]
                                        ]
                                ]
                            ],
                            debug = debug
                        )
                ),
                withKey =
                    fun (tcc: TableCellController, compositeCell: U2<CompositeCell, CompositeHeader> option) ->
                        $"cellRender-{tcc.Index.x}-{tcc.Index.y}-{compositeCell |> Option.map _.GetHashCode()}"
            )

        let renderActiveCell =
            (fun (tcc: TableCellController) ->
                AnnotationTable.ActiveCellRender(tcc, arcTable, setArcTable, debug = debug)
            )

        Html.div [
            prop.ref containerRef
            if debug then
                prop.testId "annotation_table"
                prop.custom ("data-columncount", arcTable.ColumnCount)
                prop.custom ("data-rowcount", arcTable.RowCount)
            prop.className "swt:overflow-auto swt:flex swt:flex-col swt:h-full"
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
                    ?onSelect = onSelect,
                    onKeydown =
                        (fun (e, selectedCells, activeCell) ->
                            if e.code = kbdEventCode.f2 || (
                                (e.ctrlKey || e.metaKey)
                                && e.code = kbdEventCode.enter
                                && activeCell.IsNone
                                && selectedCells.count > 0)
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
                    CompositeCell.createUnitizedFromString (string i, "Degree Celsius", "UO", "UO:000000001")
            |]
        )

        arcTable.AddColumn(
            CompositeHeader.Output IOType.Data,
            [|
                for i in 0..100 do
                    let newData =
                        Data.create (
                            string i,
                            $"Sample {i}",
                            DataFile.RawDataFile,
                            "Some Format",
                            $"Selector Format {i}",
                            ResizeArray[Comment.create ("Test", string i)]
                        )

                    CompositeCell.createData (newData)
            |]
        )

        let table, setTable = React.useState (arcTable)

        AnnotationTable.AnnotationTable(table, setTable, height = 600)