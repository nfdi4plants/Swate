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

    [<ReactComponent>]
    static member private CompositeHeaderActiveRender
        (index: CellCoordinate, header: CompositeHeader, setHeader: CompositeHeader -> unit, ?debug, ?key: string)
        =

        match header with
        | CompositeHeader.Input io ->
            TableCell.StringActiveCell(
                index,
                io.ToString(),
                (fun input -> IOType.ofString input |> CompositeHeader.Input |> setHeader),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Output io ->
            TableCell.StringActiveCell(
                index,
                io.ToString(),
                (fun input -> IOType.ofString input |> CompositeHeader.Output |> setHeader),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Comment txt ->
            TableCell.StringActiveCell(
                index,
                txt,
                (fun input -> setHeader (CompositeHeader.Comment input)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.FreeText txt ->
            TableCell.StringActiveCell(
                index,
                txt,
                (fun input -> setHeader (CompositeHeader.FreeText input)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Parameter oa ->
            TableCell.OntologyAnnotationActiveCell(
                index,
                oa,
                (fun t -> setHeader (CompositeHeader.Parameter t)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Characteristic oa ->
            TableCell.OntologyAnnotationActiveCell(
                index,
                oa,
                (fun t -> setHeader (CompositeHeader.Characteristic t)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Component oa ->
            TableCell.OntologyAnnotationActiveCell(
                index,
                oa,
                (fun t -> setHeader (CompositeHeader.Component t)),
                isStickyHeader = true,
                ?debug = debug
            )
        | CompositeHeader.Factor oa ->
            TableCell.OntologyAnnotationActiveCell(
                index,
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
                index,
                header.ToString(),
                (fun input ->
                    let nextHeader = CompositeHeader.OfHeaderString input
                    setHeader nextHeader
                ),
                isStickyHeader = true,
                ?debug = debug
            )

    static member private ContextMenu
        (
            arcTable: ArcTable,
            setArcTable,
            tableRef: IRefValue<TableHandle>,
            containerRef,
            setModal: ModalTypes option -> unit,
            ?debug: bool
        ) =

        ContextMenu.ContextMenu(
            (fun data ->
                let index = data |> unbox<CellCoordinate>

                if index.x = 0 && index.y > 0 then // index col
                    AnnotationTableContextMenu.AnnotationTableContextMenu.IndexColumnContent(
                        index.y,
                        arcTable,
                        setArcTable,
                        tableRef.current.SelectHandle
                    )
                elif index.y = 0 then // header Row
                    AnnotationTableContextMenu.AnnotationTableContextMenu.CompositeHeaderContent(
                        index.x,
                        arcTable,
                        setArcTable,
                        tableRef.current.SelectHandle,
                        setModal
                    )
                else // standard cell
                    AnnotationTableContextMenu.AnnotationTableContextMenu.CompositeCellContent(
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
            modal: AnnotationTable.ModalTypes option,
            setModal,
            tableRef: IRefValue<TableHandle>,
            ?debug: bool
        ) =

        let rmv =
            fun _ ->
                tableRef.current.focus ()
                setModal None

        React.fragment [
            match modal with
            | None -> Html.none
            | Some(ModalTypes.Details cc) ->
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

                                setModal (Some(ModalTypes.Error exnMessage))
                                failwith exn.Message

                    AnnotationTableModals.CompositeCellModal.CompositeHeaderModal(header, setHeader, rmv)

                else
                    let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)

                    let setCell =
                        fun (cell: CompositeCell) ->
                            arcTable.SetCellAt(cc.x - 1, cc.y - 1, cell)
                            setArcTable arcTable

                    let header = arcTable.Headers.[cc.x - 1]

                    AnnotationTableModals.CompositeCellModal.CompositeCellModal(
                        cell,
                        setCell,
                        rmv,
                        header,
                        ?debug = debug
                    )
            | Some(ModalTypes.Transform cc) ->
                if cc.x = 0 then // no details modal for index col
                    Html.none
                elif cc.y = 0 then // headers
                    let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)

                    let setCell =
                        fun (cell: CompositeCell) ->
                            arcTable.SetCellAt(cc.x - 1, cc.y - 1, cell)
                            setArcTable arcTable

                    let header = arcTable.Headers.[cc.x - 1]

                    AnnotationTableModals.CompositeCellEditModal.CompositeCellTransformModal(cell, header, setCell, rmv)
                else
                    let cell = arcTable.GetCellAt(cc.x - 1, cc.y - 1)

                    let setCell =
                        fun (cell: CompositeCell) ->
                            arcTable.SetCellAt(cc.x - 1, cc.y - 1, cell)
                            setArcTable arcTable

                    let header = arcTable.Headers.[cc.x - 1]

                    AnnotationTableModals.CompositeCellEditModal.CompositeCellTransformModal(cell, header, setCell, rmv)
            | Some(ModalTypes.Edit cc) ->
                if cc.x = 0 then // no details modal for index col
                    Html.none
                elif cc.y = 0 then // headers
                    AnnotationTableModals.EditConfig.CompositeCellEditModal(
                        cc.x - 1,
                        arcTable,
                        setArcTable,
                        rmv,
                        ?debug = debug
                    )
                else
                    AnnotationTableModals.EditConfig.CompositeCellEditModal(
                        cc.x - 1,
                        arcTable,
                        setArcTable,
                        rmv,
                        ?debug = debug
                    )
            | Some(ModalTypes.MoveColumn(uiTableIndex, arcTableIndex)) ->
                AnnotationTableModals.ContextMenuModals.MoveColumnModal(
                    arcTable,
                    setArcTable,
                    arcTableIndex,
                    uiTableIndex,
                    setModal,
                    tableRef,
                    ?debug = debug
                )

            | Some(ModalTypes.PasteCaseUserInput(PasteCases.AddColumns addColumns)) ->
                AnnotationTableModals.ContextMenuModals.PasteFullColumnsModal(
                    arcTable,
                    setArcTable,
                    addColumns,
                    setModal,
                    tableRef
                )
            | Some(ModalTypes.Error(exn)) -> AnnotationTableModals.ContextMenuModals.ErrorModal(exn, setModal, tableRef)
            | Some(ModalTypes.UnknownPasteCase(PasteCases.Unknown unknownPasteCase)) ->
                AnnotationTableModals.ContextMenuModals.UnknownPasteCase(
                    unknownPasteCase.data,
                    unknownPasteCase.headers,
                    setModal,
                    tableRef
                )
            | anyElse ->
                console.warn ("Unknown modal type", anyElse)
                Html.none
        ]


    [<ReactComponent(true)>]
    static member AnnotationTable
        (
            arcTable: ArcTable,
            setArcTable: ArcTable -> unit,
            ?height: int,
            ?debug: bool,
            ?onCellSelect: GridSelect.OnSelect,
            ?tableRef: IRefValue<TableHandle>,
            ?key: string
        ) =
        let containerRef = React.useElementRef ()
        let tableRefInner = React.useRef<TableHandle> (null)
        let (modal: ModalTypes option), setModal = React.useState None
        let ctx = React.useContext (Contexts.AnnotationTable.AnnotationTableStateCtx)

        let hasCtx = isNullOrUndefined ctx |> not

        let tableRef = if tableRef.IsSome then tableRef.Value else tableRefInner

        // TODO: Add table to ctx on effect, and remove on unmount
        // Does currently not work, as the disposable is not correctly setup in feliz
        React.useEffectOnce (fun _ ->
            { new System.IDisposable with
                member _.Dispose() =
                    console.log ("unmounted table", arcTable.Name)
            }
        )

        let onSelect: GridSelect.OnSelect =
            fun latest range ->

                if hasCtx then
                    let nextTable: Contexts.AnnotationTable.AnnotationTableContext = { SelectedCells = range }
                    let nextData = ctx.data.Add(arcTable.Name, nextTable)
                    ctx.setData nextData

                onCellSelect |> Option.iter (fun f -> f latest range)

        let cellRender =
            React.memo (
                (fun (index: CellCoordinate, compositeCell: U2<CompositeCell, CompositeHeader> option) ->
                    match compositeCell with
                    | None ->
                        TableCell.BaseCell(
                            index.y,
                            index.x,
                            Html.text index.y,
                            className =
                                "swt:rounded-0 swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-center swt:cursor-not-allowed swt:w-full swt:h-full swt:bg-base-200",
                            ?debug = debug
                        )
                    | Some(U2.Case2 header) ->
                        let text = header.ToString()
                        TableCell.StringInactiveCell(index, text, ?debug = debug)
                    | Some(U2.Case1 cell) -> TableCell.CompositeCellInactiveCell(index, cell, ?debug = debug)
                ),
                withKey =
                    fun (index: CellCoordinate, compositeCell: U2<CompositeCell, CompositeHeader> option) ->
                        $"cellRender-{index.x}-{index.y}-{compositeCell |> Option.map _.GetHashCode()}"
            )

        let activeCellRender =
            React.memo (
                (fun (index: CellCoordinate) ->
                    match index with
                    | _ when index.x > 0 && index.y > 0 ->
                        let setCell =
                            fun (cell: CellCoordinate) (cc: CompositeCell) ->
                                let nextTable = arcTable |> ArcTable.setCellAt (cell.x - 1, cell.y - 1, cc)
                                setArcTable nextTable


                        let cell = arcTable.GetCellAt(index.x - 1, index.y - 1)

                        let header =
                            arcTable.Headers[index.x - 1].ToTerm()
                            |> _.TermAccessionShort
                            |> Option.whereNot System.String.IsNullOrWhiteSpace

                        TableCell.CompositeCellActiveCell(
                            index,
                            cell,
                            setCell index,
                            ?parentId = header,
                            ?debug = debug,
                            key = $"cell-{index.x}-{index.y}"
                        )
                    | _ when index.x > 0 && index.y = 0 ->
                        let x = index.x - 1

                        let setHeader =
                            fun (ch: CompositeHeader) ->
                                let nextTable = arcTable |> ArcTable.updateHeader (x, ch)
                                setArcTable nextTable

                        let header = arcTable.Headers[x]

                        AnnotationTable.CompositeHeaderActiveRender(
                            index,
                            header,
                            setHeader,
                            ?debug = debug,
                            key = $"header-{x}"
                        )
                    | _ -> Html.div "Unknown cell type"
                )
            )

        Html.div [
            prop.ref containerRef
            if debug.IsSome && debug.Value then
                prop.testId "annotation_table"
                prop.custom ("data-columncount", arcTable.ColumnCount)
                prop.custom ("data-rowcount", arcTable.RowCount)
            prop.className "swt:overflow-auto swt:flex swt:flex-col swt:h-full"
            prop.children [
                ReactDOM.createPortal ( // Modals
                    AnnotationTable.ModalController(arcTable, setArcTable, modal, setModal, tableRef, ?debug = debug),
                    Browser.Dom.document.body
                )
                AnnotationTable.ContextMenu(arcTable, setArcTable, tableRef, containerRef, setModal, ?debug = debug)
                Table.Table(
                    rowCount = arcTable.RowCount + 1,
                    columnCount = arcTable.ColumnCount + 1,
                    renderCell =
                        (fun (index: CellCoordinate) ->
                            let cell =
                                if index.x = 0 then
                                    None
                                elif index.y = 0 then
                                    Some(arcTable.Headers.[index.x - 1] |> U2.Case2)
                                else
                                    Some(arcTable.GetCellAt(index.x - 1, index.y - 1) |> U2.Case1)

                            cellRender (index, cell)
                        ),
                    renderActiveCell = activeCellRender,
                    ref = tableRef,
                    ?height = height,
                    onKeydown =
                        (fun (e, selectedCells, activeCell) ->
                            if
                                e.code = kbdEventCode.f2
                                || ((e.ctrlKey || e.metaKey)
                                    && e.code = kbdEventCode.enter
                                    && activeCell.IsNone
                                    && selectedCells.count > 0)
                            then
                                let cell = selectedCells.selectedCellsReducedSet.MinimumElement
                                setModal (Some(ModalTypes.Details cell))
                            elif e.code = kbdEventCode.delete && selectedCells.count > 0 then
                                arcTable.ClearSelectedCells(tableRef.current.SelectHandle)
                                arcTable.Copy() |> setArcTable
                        ),
                    enableColumnHeaderSelect = true,
                    onSelect = onSelect,
                    ?debug = debug
                )
            ]
        ]

    static member Entry() =
        let arcTable =
            ARCtrl.ArcTable("TestTable", ResizeArray(), System.Collections.Generic.Dictionary())

        let ctx = React.useContext (Contexts.AnnotationTable.AnnotationTableStateCtx)

        arcTable.AddColumn(
            CompositeHeader.Input IOType.Source,
            [|
                for i in 0..100 do
                    CompositeCell.createFreeText $"Source {i}"
            |]
        )

        arcTable.AddColumn(
            CompositeHeader.Component(OntologyAnnotation("instrument model", "MS", "MS:1000031")),
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

        React.fragment [
            AnnotationTable.AnnotationTable(table, setTable, height = 600)
            Html.div [
                prop.textf
                    "%A"
                    (ctx.data
                     |> Map.tryFind table.Name
                     |> Option.bind _.SelectedCells
                     |> Option.map (fun x -> sprintf "%i - %i, %i - %i" x.xStart x.xEnd x.yStart x.yEnd))
            ]
        ]