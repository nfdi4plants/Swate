namespace Swate.Components.AnnotationTableContextMenu

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Feliz

open Swate.Components
open Types.AnnotationTableContextMenu

/// AnnotationTableContextMenu Components
type ATCMC =
    static member Icon(className: string) = Html.i [ prop.className className ]

    static member KbdHint(text: string, ?label: string) =
        let label = defaultArg label text

        {|
            element = Html.kbd [ prop.className "swt:ml-auto swt:kbd swt:kbd-sm"; prop.text text ]
            label = label
        |}

type AnnotationTableContextMenuUtil =

    static member fillColumn(index: CellCoordinate, table: ArcTable) : ArcTable =
        let cell = table.GetCellAt(index.x - 1, index.y - 1)
        let nextTable = table.Copy()

        for y in 0 .. table.RowCount - 1 do
            nextTable.SetCellAt(index.x - 1, y, cell.Copy())

        nextTable

    static member clear(cellIndex: CellCoordinate, table: ArcTable, selectHandle: SelectHandle) : ArcTable =
        let nextTable = table.Copy()


        if selectHandle.contains cellIndex then
            nextTable.ClearSelectedCells(selectHandle)
        else
            nextTable.ClearCell(cellIndex)


        nextTable


    static member deleteRow
        (tableIndex: CellCoordinate, rowIndex, table: ArcTable, selectHandle: SelectHandle)
        : ArcTable =
        if selectHandle.contains tableIndex then
            let rowCoordinates =
                selectHandle.getSelectedCells ()
                |> Array.ofSeq
                |> Array.distinctBy (fun coordinate -> coordinate.y)
                |> Array.map (fun coordinate -> coordinate.y - 1)

            table.RemoveRows(rowCoordinates)
        else
            table.RemoveRow(rowIndex - 1)

        table.Copy()

    static member checkForHeader(value: string) =

        let splitCamelCase (input: string) =
            Regex.Matches(input, @"([A-Z][a-z]+|[A-Z]+(?![a-z]))")
            |> Seq.cast<Match>
            |> Seq.map (fun m -> m.Value)
            |> String.concat " "

        let headerNames =
            CompositeHeader.Cases |> Array.map (fun (_, item) -> splitCamelCase item)

        match value with
        | header when ARCtrl.Helper.Regex.tryParseCharacteristicColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseComponentColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseFactorColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseInputColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseOutputColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseParameterColumnHeader(header).IsSome -> true
        | header when
            //header with a short length can lead to a problem here, so they must be at least as long as an expected header
            let shortestHeaderLength = headerNames |> Array.minBy String.length |> String.length
            headerNames
            |> Array.exists (fun case -> not (System.String.IsNullOrEmpty header) && case.StartsWith(header) && header.Length >= shortestHeaderLength)
            ->
            true
        | _ -> false

    static member deleteColumn
        (tableIndex: CellCoordinate, colIndex: int, table: ArcTable, selectHandle: SelectHandle)
        : ArcTable =
        if selectHandle.contains tableIndex then
            let columnCoordinates =
                selectHandle.getSelectedCells ()
                |> Array.ofSeq
                |> Array.distinctBy (fun coordinate -> coordinate.x)
                |> Array.map (fun coordinate -> coordinate.x - 1)

            table.RemoveColumns(columnCoordinates)
        else
            table.RemoveColumn(colIndex - 1)

        table.Copy()

    static member copy(cellIndex: CellCoordinate, table: ArcTable, selectHandle: SelectHandle) =
        let result =
            if selectHandle.getCount () > 1 then

                let cellCoordinates =
                    selectHandle.getSelectedCells ()
                    |> Array.ofSeq
                    |> Array.groupBy (fun item -> item.y)

                let headers, cells =
                    cellCoordinates
                    |> Array.map (fun (_, row) ->
                        row
                        |> Array.partition (fun coordinate -> (coordinate.y - 1) < 0)
                        |> fun (headerCoordinates, bodyCoordinates) ->
                            headerCoordinates
                            |> Array.map (fun coordinate -> table.GetColumn(coordinate.x - 1).Header),
                            bodyCoordinates
                            |> Array.map (fun coordinate -> table.GetCellAt(coordinate.x - 1, coordinate.y - 1))
                    )
                    |> Array.unzip

                let headers = headers |> Array.concat
                
                if headers.Length > 0 then
                    let columns =
                        let body =
                            cells.[1..]
                            |> Array.transpose
                        headers
                        |> Array.mapi (fun index header ->
                            if body.Length - 1 >= index then
                                CompositeColumn.create (header, body.[index] |> ResizeArray)
                            else
                                CompositeColumn.create (header, Array.empty |> ResizeArray)
                        )

                    let tableString =
                        let table = ArcTable.init ("placeholder")
                        table.AddColumns(columns)
                        table.ToStringSeqs()

                    tableString
                    |> Array.map (fun row -> row |> String.concat "\t")
                    |> String.concat System.Environment.NewLine
                else
                    CompositeCell.ToTableTxt(cells)
            else if cellIndex.y - 1 < 0 then
                let column = table.GetColumn(cellIndex.x - 1)
                let table = ArcTable.init ("placeholder")
                table.AddColumn(column.Header)

                table.ToStringSeqs()
                |> Array.map (fun row -> row |> String.concat "\t")
                |> String.concat System.Environment.NewLine
            else
                let cell = table.GetCellAt((cellIndex.x - 1, cellIndex.y - 1))
                cell.ToTabStr()

        navigator.clipboard.writeText result

    static member cut(cellIndex: CellCoordinate, table: ArcTable, setTable, selectHandle: SelectHandle) = promise {
        do! AnnotationTableContextMenuUtil.copy (cellIndex, table, selectHandle)

        let nextTable =
            AnnotationTableContextMenuUtil.clear (cellIndex, table, selectHandle)

        nextTable |> setTable
    }

    //Recalculates the index, then the amount of selected cells is bigger than the amount of copied cells
    static member getIndex(startIndex, length) =
        let rec loop index length =
            if index < length then
                index
            else
                loop (index - length) length

        loop startIndex length

    static member getCopiedCells() = promise {
        let! copiedValue = navigator.clipboard.readText ()

        let rows =
            copiedValue.Split([| System.Environment.NewLine |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun item -> item.Split('\t') |> Array.map _.Trim())

        return rows
    }

    static member getFittedCells(data: string[][], headers: CompositeHeader[]) =

        let fitColumnsToTarget (row: string[][]) (headers: CompositeHeader[]) =
            let rec loop index result =
                if index >= headers.Length then
                    result |> List.rev |> Array.ofList
                else
                    let header = headers.[index]

                    let newIndex =
                        if index >= row.Length then
                            AnnotationTableContextMenuUtil.getIndex (index, row.Length)
                        else
                            index

                    match header with
                    | x when x.IsSingleColumn ->
                        let cell = CompositeCell.fromContentValid (row.[newIndex], header)
                        loop (index + 1) (cell :: result)
                    | x when x.IsDataColumn ->
                        let cell = CompositeCell.fromContentValid (row.[newIndex], header)
                        loop (index + 1) (cell :: result)
                    | x when x.IsTermColumn ->
                        let value =
                            if row.[newIndex].Length = 2 then
                                [| row.[newIndex].[0] |]
                            else
                                row.[newIndex]

                        let cell = CompositeCell.fromContentValid (value, header)
                        loop (index + 1) (cell :: result)
                    | anyElse -> failwith $"Error-fitColumnsToTarget: Encountered unsupported case: {anyElse}"

            loop 0 []

        let getHeadersExpectedLengths (headers: CompositeHeader[]) =
            headers
            |> Array.map (fun header ->
                match header with
                | x when x.IsSingleColumn -> x.ToString(), [ 1 ]
                | x when x.IsDataColumn -> x.ToString(), [ 4 ]
                | x when x.IsTermColumn -> x.ToString(), [ 1; 2; 3; 4 ]
                | anyElse -> failwith $"Error-getHeadersWithLength: Encountered unsupported case: {anyElse}"
            )

        let fitHeaders (strings: string[]) (headersSizes: (string * int list)[]) (maxColumns: int) =
            let rec tryFit (cell: string[]) index (remaining: (string * int list) list) (columnsLeft: int) =

                let index =
                    if index >= strings.Length && columnsLeft > 0 then
                        0
                    else
                        index

                match remaining, columnsLeft with
                | _, 0 -> Some []
                | [], _ -> Some []
                | (name, sizes) :: rest, _ ->
                    sizes
                    |> List.sortDescending
                    |> List.choose (fun size ->

                        let maxSize =
                            let cellsLeft = (cell.Length - index)

                            if cellsLeft > columnsLeft - 1 then
                                cellsLeft - (columnsLeft - 1)
                            else
                                cellsLeft

                        let adjustedSize = min size maxSize

                        let actualSize =
                            if index + adjustedSize <= cell.Length then adjustedSize
                            elif index < cell.Length then cell.Length - index
                            else 0

                        if actualSize > 0 then
                            let segment = cell.[index .. index + actualSize - 1]

                            match tryFit cell (index + actualSize) rest (columnsLeft - 1) with
                            | Some restResult -> Some((name, segment) :: restResult)
                            | None -> None
                        else
                            None
                    )
                    |> List.tryHead

            tryFit strings 0 (Array.toList headersSizes) maxColumns

        let fittingHeaders = getHeadersExpectedLengths headers

        let fittedRows =
            data |> Array.map (fun row -> fitHeaders row fittingHeaders headers.Length)

        let result =
            fittedRows
            |> Array.choose (fun row -> row)
            |> Array.map (fun row -> row |> List.map (fun (_, cells) -> cells) |> Array.ofList)

        result |> Array.map (fun row -> fitColumnsToTarget row headers)

    static member predictPasteBehaviour
        (cellIndex: CellCoordinate, targetTable: ArcTable, selectHandle: SelectHandle, data: string[][])
        =

        //Convert cell coordinates to array
        let cellCoordinates = selectHandle.getSelectedCells () |> Array.ofSeq

        //Get all required headers for cells
        let headers =
            let columnIndices = cellCoordinates |> Array.distinctBy (fun item -> item.x)

            columnIndices
            |> Array.map (fun index -> targetTable.GetColumn(index.x - 1).Header)

        let checkForHeaders (row: string[]) =
            let headers = ARCtrl.CompositeHeader.Cases |> Array.map (fun (_, header) -> header)

            let areHeaders =
                headers
                |> Array.collect (fun _ ->
                    row
                    |> Array.map (fun cell -> AnnotationTableContextMenuUtil.checkForHeader (cell))
                )

            Array.contains true areHeaders

        //Group all cells based on their row
        let groupedCellCoordinates =
            cellCoordinates
            |> Array.ofSeq
            |> Array.groupBy (fun item -> item.y)
            |> Array.map (fun (_, row) -> row)

        if checkForHeaders data.[0] || cellCoordinates.[0].y <= 0 || cellIndex.y <= 0 then

            let headers, body =
                if data.Length > 1 then
                    data.[0], data.[1..]
                else
                    data.[0], [||]

            let columns = Array.append [| headers |] body |> Array.transpose
            let columnsArrays = columns |> Seq.toArray |> Array.map (Seq.toArray)

            let compositeColumns =
                ARCtrl.Spreadsheet.ArcTable.composeColumns columnsArrays |> ResizeArray

            PasteCases.AddColumns {|
                data = compositeColumns
                coordinate = cellIndex
                coordinates = groupedCellCoordinates
            |}
        else
            let fittedCells =
                AnnotationTableContextMenuUtil.getFittedCells (data, headers)
                |> Array.transpose
                |> Array.map (fun column -> column |> ResizeArray)

            let columns =
                Array.map2 (fun header cells -> CompositeColumn.create (header, cells)) headers fittedCells
                |> ResizeArray

            let isEmpty =
                Array.isEmpty fittedCells
                || fittedCells
                   |> Array.map (fun row ->
                       Seq.isEmpty row
                       || Seq.forall (fun (cell: CompositeCell) -> String.IsNullOrWhiteSpace(cell.ToTabStr())) row
                   )
                   |> Array.contains true

            if isEmpty then
                PasteCases.Unknown {| data = data; headers = headers |}
            else
                PasteCases.PasteCells {|
                    data = columns
                    coordinates = groupedCellCoordinates
                |}

    static member getValueOfCompositeHeader (compositeHeader: CompositeHeader) =
        match compositeHeader with
        | CompositeHeader.Component oa -> defaultArg oa.Name (compositeHeader.ToString())
        | CompositeHeader.Characteristic oa -> defaultArg oa.Name (compositeHeader.ToString())
        | CompositeHeader.Factor oa -> defaultArg oa.Name (compositeHeader.ToString())
        | CompositeHeader.Parameter oa -> defaultArg oa.Name (compositeHeader.ToString())
        | CompositeHeader.ProtocolType -> CompositeHeaderDiscriminate.ProtocolType.ToString()
        | CompositeHeader.ProtocolDescription -> CompositeHeaderDiscriminate.ProtocolDescription.ToString()
        | CompositeHeader.ProtocolUri -> CompositeHeaderDiscriminate.ProtocolUri.ToString()
        | CompositeHeader.ProtocolVersion -> CompositeHeaderDiscriminate.ProtocolVersion.ToString()
        | CompositeHeader.ProtocolREF -> CompositeHeaderDiscriminate.ProtocolREF.ToString()
        | CompositeHeader.Performer -> CompositeHeaderDiscriminate.Performer.ToString()
        | CompositeHeader.Date -> CompositeHeaderDiscriminate.Date.ToString()
        | CompositeHeader.Input io -> io.ToString()
        | CompositeHeader.Output io -> io.ToString()
        | CompositeHeader.Comment s -> s
        | CompositeHeader.FreeText s -> s

    static member pasteCells
        (
            pasteColumns:
                {|
                    data: ResizeArray<CompositeColumn>
                    coordinates: CellCoordinate[][]
                |},
            coordinate: CellCoordinate,
            selectHandle: SelectHandle,
            table: ArcTable,
            setTable
        ) =

        let getCorrectTargetForCell
            (currentCell: CompositeCell)
            (table: ArcTable)
            (targetCoordinate: CellCoordinate)
            adaption
            =

            if currentCell.isUnitized then
                let targetCell =
                    table.GetCellAt(targetCoordinate.x - adaption, targetCoordinate.y - adaption)

                let value, unit = currentCell.AsUnitized

                if targetCell.isUnitized && unit.isEmpty () then
                    let _, targetUnit = targetCell.AsUnitized
                    let newTarget = CompositeCell.createUnitized (value, targetUnit)
                    newTarget
                elif targetCell.isTerm && unit.isEmpty () then
                    let targetTerm = targetCell.AsTerm
                    let newTarget = CompositeCell.createUnitized (value, targetTerm)
                    newTarget
                else
                    currentCell
            else
                currentCell

        let getCorrectTargetForHeader
            (currentHeader: CompositeHeader)
            (table: ArcTable)
            (targetCoordinate: CellCoordinate)
            adaption
            =

            let targetCell =
                table.GetCellAt(targetCoordinate.x - adaption, targetCoordinate.y - adaption)

            if targetCell.isUnitized then
                let _, targetUnit = targetCell.AsUnitized
                let value = AnnotationTableContextMenuUtil.getValueOfCompositeHeader(currentHeader)
                let newTarget = CompositeCell.createUnitized (value, targetUnit)
                newTarget
            elif targetCell.isTerm then
                let targetTerm = defaultArg (currentHeader.TryGetTerm()) targetCell.AsTerm
                let newTarget = CompositeCell.createTerm (targetTerm)
                newTarget
            elif targetCell.isData then
                let targetInfo = targetCell.AsData
                let targetData = AnnotationTableContextMenuUtil.getValueOfCompositeHeader(currentHeader)
                let newTarget = CompositeCell.createDataFromString(targetData, ?format = targetInfo.Format, ?selectorFormat = targetInfo.SelectorFormat)
                newTarget
            else
                let value = AnnotationTableContextMenuUtil.getValueOfCompositeHeader(currentHeader)
                CompositeCell.createFreeText(value)

        //Check amount of selected cells
        //When multiple cells are selected a different handling is required
        let headerCoordinates, bodyCoordinates =
            if pasteColumns.coordinates.[0].[0].y - 1 < 0 then
                pasteColumns.coordinates.[0], pasteColumns.coordinates.[1..]
            else
                [||], pasteColumns.coordinates

        let pasteWithCoordiantes (coordinates: CellCoordinate[][]) =
            if selectHandle.getCount () > 1 then
                //Map over all selected cells
                coordinates
                |> Array.iteri (fun yi row ->
                    //Restart row index, when the amount of selected rows is bigger than copied rows
                    let yIndex =
                        if (pasteColumns.data.Item 0).Cells.Count = 0 then
                            1
                        else
                            AnnotationTableContextMenuUtil.getIndex (yi, (pasteColumns.data.Item 0).Cells.Count)
                    row
                    |> Array.iteri (fun xi coordinate ->
                        let xIndex = AnnotationTableContextMenuUtil.getIndex (xi, pasteColumns.data.Count)

                        if coordinate.y - 1 < 0 then
                            table.UpdateHeader(coordinate.x - 1, pasteColumns.data.[xIndex].Header, true)
                        else
                            let newTarget =
                                if pasteColumns.data.[0].Cells.Count = 0 then
                                    let currentHeader = pasteColumns.data.[xIndex].Header
                                    getCorrectTargetForHeader currentHeader table coordinate 1
                                else
                                    let cells = (pasteColumns.data.Item xIndex).Cells
                                    let currentCell = cells.[yIndex]
                                    getCorrectTargetForCell currentCell table coordinate 1
                            table.SetCellAt(coordinate.x - 1, coordinate.y - 1, newTarget)
                    )
                )
            else if coordinate.y - 1 < 0 then
                table.UpdateHeader(coordinate.x - 1, pasteColumns.data.[0].Header, true)
            else
                let newTarget =
                    if pasteColumns.data.[0].Cells.Count = 0 then
                        let currentHeader = pasteColumns.data.[0].Header
                        getCorrectTargetForHeader currentHeader table coordinate 1
                    else
                        let currentCell = pasteColumns.data.[0].Cells.[0]
                        getCorrectTargetForCell currentCell table coordinate 1
                table.SetCellAt(coordinate.x - 1, coordinate.y - 1, newTarget)

        if headerCoordinates.Length > 0 then
            pasteWithCoordiantes [| headerCoordinates |]

        if bodyCoordinates.Length > 0 then
            pasteWithCoordiantes bodyCoordinates

        table.Copy() |> setTable

    static member paste
        (
            pasteCases: PasteCases,
            coordinate: CellCoordinate,
            table: ArcTable,
            selectHandle: SelectHandle,
            setModal: Types.AnnotationTable.ModalTypes option -> unit,
            setTable: ArcTable -> unit
        ) =

        match pasteCases with
        | AddColumns addColumns ->
            setModal (
                AnnotationTable.ModalTypes.PasteCaseUserInput(PasteCases.AddColumns addColumns, selectHandle)
                |> Some
            )
        | PasteCells pasteColumns ->
            AnnotationTableContextMenuUtil.pasteCells (pasteColumns, coordinate, selectHandle, table, setTable)
        | Unknown unknownPasteCase ->
            setModal (
                AnnotationTable.ModalTypes.UnknownPasteCase(PasteCases.Unknown unknownPasteCase)
                |> Some
            )

    static member tryPasteCopiedCells
        (
            cellIndex: CellCoordinate,
            arcTable: ArcTable,
            selectHandle: SelectHandle,
            setModal: AnnotationTable.ModalTypes option -> unit,
            setArcTable: ArcTable -> unit
        ) =
        promise {
            let! data = AnnotationTableContextMenuUtil.getCopiedCells ()

            try
                let prediction =
                    AnnotationTableContextMenuUtil.predictPasteBehaviour (cellIndex, arcTable, selectHandle, data)

                AnnotationTableContextMenuUtil.paste (
                    prediction,
                    cellIndex,
                    arcTable,
                    selectHandle,
                    setModal,
                    setArcTable
                )
            with exn ->
                setModal (AnnotationTable.ModalTypes.Error(exn.Message) |> Some)
        }

[<Erase>]
type AnnotationTableContextMenu =
    static member CompositeCellContent
        (
            index: CellCoordinate,
            arcTable: ArcTable,
            setArcTable: ArcTable -> unit,
            selectHandle: SelectHandle,
            setModal: Types.AnnotationTable.ModalTypes option -> unit
        ) =
        let cellIndex = {| x = index.x; y = index.y |}
        let cell = arcTable.GetCellAt(cellIndex.x - 1, cellIndex.y - 1)
        let header = arcTable.GetColumn(cellIndex.x - 1).Header

        let containsHeaderRow =
            let range = selectHandle.getSelectedCellRange ()
            if range.IsSome then range.Value.yStart = 0 else false

        let transformName =
            match cell with
            | CompositeCell.Term _ -> "Transform to Unit"
            | CompositeCell.Unitized _ -> "Transform to Term"
            | CompositeCell.Data _ -> "Transform to Text"
            | CompositeCell.FreeText _ -> if header.IsDataColumn then "Transform to Data" else ""

        [
            ContextMenuItem(
                Html.div "Details",
                icon = Icons.MagnifyingGlassPlus(),
                kbdbutton = ATCMC.KbdHint("D"),
                onClick = fun _ -> AnnotationTable.ModalTypes.Details index |> Some |> setModal
            )
            ContextMenuItem(
                Html.div "Edit",
                icon = Icons.PenToSquare(),
                kbdbutton = ATCMC.KbdHint("E"),
                onClick = fun _ -> AnnotationTable.ModalTypes.Edit index |> Some |> setModal
            )
            ContextMenuItem(
                Html.div "Fill Column",
                icon = Icons.Pen(),
                kbdbutton = ATCMC.KbdHint("F"),
                onClick = fun _ -> AnnotationTableContextMenuUtil.fillColumn (cellIndex, arcTable) |> setArcTable
            )
            if not (String.IsNullOrWhiteSpace(transformName)) then
                ContextMenuItem(
                    Html.div transformName,
                    icon = Icons.ArrorRightLeft(),
                    kbdbutton = ATCMC.KbdHint("T"),
                    onClick = fun _ -> AnnotationTable.ModalTypes.Transform index |> Some |> setModal
                )
            ContextMenuItem(
                Html.div "Clear",
                icon = Icons.Eraser(),
                kbdbutton = ATCMC.KbdHint("Del"),
                onClick =
                    fun _ ->
                        AnnotationTableContextMenuUtil.clear (cellIndex, arcTable, selectHandle)
                        |> setArcTable
            )
            ContextMenuItem(isDivider = true)
            ContextMenuItem(
                Html.div "Copy",
                icon = Icons.Copy(),
                kbdbutton = ATCMC.KbdHint("C"),
                onClick =
                    fun _ ->
                        AnnotationTableContextMenuUtil.copy (cellIndex, arcTable, selectHandle)
                        |> Promise.start

            )
            if not containsHeaderRow then
                ContextMenuItem(
                    Html.div "Cut",
                    icon = Icons.Scissor(),
                    kbdbutton = ATCMC.KbdHint("X"),
                    onClick =
                        fun _ ->
                            AnnotationTableContextMenuUtil.cut (cellIndex, arcTable, setArcTable, selectHandle)
                            |> Promise.start
                )
            ContextMenuItem(
                Html.div "Paste",
                icon = Icons.Paste(),
                kbdbutton = ATCMC.KbdHint("V"),
                onClick =
                    fun _ ->
                        AnnotationTableContextMenuUtil.tryPasteCopiedCells (
                            cellIndex,
                            arcTable,
                            selectHandle,
                            setModal,
                            setArcTable
                        )
                        |> Promise.start
            )
            ContextMenuItem(isDivider = true)
            ContextMenuItem(
                Html.div "Delete Row",
                icon = Icons.DeleteLeft(),
                kbdbutton = ATCMC.KbdHint("DelR"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteRow (cc, cellIndex.y, arcTable, selectHandle)
                        |> setArcTable
            )
            ContextMenuItem(
                Html.div "Delete Column",
                icon = Icons.DeleteDown(),
                kbdbutton = ATCMC.KbdHint("DelC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteColumn (cc, cellIndex.x, arcTable, selectHandle)
                        |> setArcTable
            )
            ContextMenuItem(
                Html.div "Move Column",
                icon = Icons.ArrorRightLeft(),
                kbdbutton = ATCMC.KbdHint("MC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>
                        setModal (AnnotationTable.ModalTypes.MoveColumn(cc, cellIndex) |> Some)
            )
        ]

    static member CompositeHeaderContent
        (
            columnIndex: int,
            arcTable: ArcTable,
            setArcTable: ArcTable -> unit,
            selectHandle: SelectHandle,
            setModal: Types.AnnotationTable.ModalTypes option -> unit
        ) =
        let cellCoordinate: CellCoordinate = {| y = 0; x = columnIndex |}

        [
            ContextMenuItem(
                Html.div "Details",
                icon = Icons.MagnifyingGlassPlus(),
                kbdbutton = ATCMC.KbdHint("D"),
                onClick = fun _ -> AnnotationTable.ModalTypes.Details cellCoordinate |> Some |> setModal
            )
            ContextMenuItem(
                Html.div "Edit",
                icon = Icons.PenToSquare(),
                kbdbutton = ATCMC.KbdHint("E"),
                onClick = fun _ -> AnnotationTable.ModalTypes.Edit cellCoordinate |> Some |> setModal
            )
            ContextMenuItem(isDivider = true)
            ContextMenuItem(
                Html.div "Copy",
                icon = Icons.Copy(),
                kbdbutton = ATCMC.KbdHint("C"),
                onClick =
                    fun _ ->
                        AnnotationTableContextMenuUtil.copy (cellCoordinate, arcTable, selectHandle)
                        |> Promise.start

            )
            ContextMenuItem(
                Html.div "Paste",
                icon = Icons.Paste(),
                kbdbutton = ATCMC.KbdHint("V"),
                onClick =
                    fun _ ->
                        AnnotationTableContextMenuUtil.tryPasteCopiedCells (
                            cellCoordinate,
                            arcTable,
                            selectHandle,
                            setModal,
                            setArcTable
                        )
                        |> Promise.start
            )
            ContextMenuItem(isDivider = true)
            ContextMenuItem(
                Html.div "Delete Column",
                icon = Icons.DeleteDown(),
                kbdbutton = ATCMC.KbdHint("DelC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteColumn (cc, columnIndex - 1, arcTable, selectHandle)
                        |> setArcTable
            )
            ContextMenuItem(
                Html.div "Move Column",
                icon = Icons.ArrorRightLeft(),
                kbdbutton = ATCMC.KbdHint("MC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        setModal (
                            AnnotationTable.ModalTypes.MoveColumn(cc, {| x = columnIndex - 1; y = 0 |})
                            |> Some
                        )
            )
        ]

    static member IndexColumnContent
        (index: int, table: ArcTable, setTable: ArcTable -> unit, selectHandle: SelectHandle)
        =
        [
            ContextMenuItem(
                Html.div "Delete Row",
                icon = Icons.DeleteLeft(),
                kbdbutton = ATCMC.KbdHint("DelR"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteRow (cc, index - 1, table, selectHandle)
                        |> setTable
            )
        ]