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

    static member checkForHeader (value: string) =

        let splitCamelCase (input: string) =
            Regex.Matches(input, @"([A-Z][a-z]+|[A-Z]+(?![a-z]))")
            |> Seq.cast<Match>
            |> Seq.map (fun m -> m.Value)
            |> String.concat " "

        let headerNames =
            CompositeHeader.Cases
            |> Array.map (fun (_, item) -> splitCamelCase item)

        match value with
        | header when ARCtrl.Helper.Regex.tryParseCharacteristicColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseComponentColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseFactorColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseInputColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseOutputColumnHeader(header).IsSome -> true
        | header when ARCtrl.Helper.Regex.tryParseParameterColumnHeader(header).IsSome -> true
        | header when headerNames |> Array.exists (fun case ->
            not (System.String.IsNullOrEmpty header) &&
            case.StartsWith(header)) -> true
        | _ -> false

    static member fillColumn(index: CellCoordinate, table: ArcTable, setTable) =
        let cell = table.GetCellAt(index.x, index.y)
        let nextTable = table.Copy()

        for y in 0 .. table.RowCount - 1 do
            nextTable.SetCellAt(index.x, y, cell.Copy())

        setTable nextTable

    static member clear(cellIndex: CellCoordinate, table: ArcTable, selectHandle: SelectHandle) : ArcTable =
        if selectHandle.contains cellIndex then
            table.ClearSelectedCells(selectHandle)
        else
            table.ClearCell(cellIndex)

        table.Copy()

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
            table.RemoveRow(rowIndex)

        table.Copy()

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
            table.RemoveColumn(colIndex)

        table.Copy()

    static member copy(cellIndex: CellCoordinate, table: ArcTable, selectHandle: SelectHandle) =
        let result =
            if selectHandle.getCount () > 1 then

                let cellCoordinates =
                    selectHandle.getSelectedCells ()
                    |> Array.ofSeq
                    |> Array.groupBy (fun item -> item.y)

                let columns, cells =
                    cellCoordinates
                    |> Array.map (fun (_, row) ->
                        row
                        |> Array.partition (fun coordinate -> (coordinate.y - 1) < 0)
                        |> fun (headerCoordinates, bodyCoordinates) ->
                            headerCoordinates
                            |> Array.map (fun coordinate ->
                                table.GetColumn(coordinate.x - 1)),
                            bodyCoordinates
                            |> Array.map (fun coordinate ->
                                table.GetCellAt(coordinate.x - 1, coordinate.y - 1))
                    )
                    |> Array.unzip

                let headerString = CompositeHeader.ToTableTxt(columns |> Array.collect id)
                let bodyString = CompositeCell.ToTableTxt(cells)

                if String.IsNullOrEmpty headerString then
                    bodyString
                else
                    headerString + bodyString
            else
                if cellIndex.y - 1 < 0 then
                    let column = table.GetColumn(cellIndex.x - 1)
                    column.Header.ToTabStr()
                else
                    let cell = table.GetCellAt((cellIndex.x - 1, cellIndex.y - 1))
                    cell.ToTabStr()

        navigator.clipboard.writeText result

    static member cut(cellIndex: CellCoordinate, table: ArcTable, setTable, selectHandle: SelectHandle) = promise {
        console.log (cellIndex)
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
                        //if index >= row.Length then
                        //    AnnotationTableContextMenuUtil.getIndex (index, row.Length)
                        //else
                        //    index
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

        let getHeadersExpectedLength (headers: CompositeHeader[]) =
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
                //let index = if index >= strings.Length then 0 else index

                match remaining, columnsLeft with
                | _, 0 -> Some []
                | [], _ -> Some []
                | (name, sizes) :: rest, _ ->
                    sizes
                    |> List.sortDescending
                    |> List.choose (fun size ->
                        let maxSize = (cell.Length - index) - (columnsLeft - 1)
                        let adjustedSize = min size maxSize

                        let actualSize =
                            if index + adjustedSize <= cell.Length then adjustedSize
                            elif index < cell.Length then cell.Length - index
                            else 0

                        if actualSize > 0 then
                            let segment = cell.[index..index + actualSize - 1]

                            match tryFit cell (index + actualSize) rest (columnsLeft - 1) with
                            | Some restResult -> Some((name, segment) :: restResult)
                            | None -> None
                        else
                            match tryFit cell (index + actualSize) rest (columnsLeft - 1) with
                            | Some restResult -> Some(restResult)
                            | None -> None
                    )
                    |> List.tryHead

            tryFit strings 0 (Array.toList headersSizes) maxColumns

        let fittingHeaders = getHeadersExpectedLength headers

        let fittedRows = data |> Array.map (fun row -> fitHeaders row fittingHeaders headers.Length)
        let result =
            fittedRows
            |> Array.choose (fun row -> row)
            |> Array.map (fun row ->
                row
                |> List.map (fun (_, cells) -> cells)
                |> Array.ofList)
        result |> Array.map (fun row -> fitColumnsToTarget row headers)

    static member getFittedHeaders(data: string[], targetCollumnCount: int) =

        let getIOLength(io: IOType) =
            match io with
            | IOType.Data -> [ 1; 2; 3; 4; 5 ]
            | _ -> [ 1 ]

        let getHeadersExpectedLength (headers: string[]) =
            headers
            |> Array.map (fun header ->
                let potHeader = CompositeHeader.OfHeaderString header
                match potHeader with
                | CompositeHeader.Component _ -> CompositeHeader.Component.ToString(), [ 1; 2; 3; 4 ]
                | CompositeHeader.Characteristic _ -> CompositeHeader.Characteristic.ToString(), [ 1; 2; 3; 4 ]
                | CompositeHeader.Factor _ -> CompositeHeader.Factor.ToString(), [ 1; 2; 3; 4 ]
                | CompositeHeader.Parameter _ -> CompositeHeader.Parameter.ToString(), [ 1; 2; 3; 4 ]
                | CompositeHeader.ProtocolType -> CompositeHeader.ProtocolType.ToString(), [ 1 ]
                | CompositeHeader.ProtocolDescription -> CompositeHeader.ProtocolDescription.ToString(), [ 1 ]
                | CompositeHeader.ProtocolUri -> CompositeHeader.ProtocolUri.ToString(), [ 1 ]
                | CompositeHeader.ProtocolVersion -> CompositeHeader.ProtocolVersion.ToString(), [ 1 ]
                | CompositeHeader.ProtocolREF -> CompositeHeader.ProtocolREF.ToString(), [ 1 ]
                | CompositeHeader.Performer -> CompositeHeader.Performer.ToString(), [ 1 ]
                | CompositeHeader.Date -> CompositeHeader.Date.ToString(), [ 1 ]
                | CompositeHeader.Input io -> CompositeHeader.Input.ToString(), getIOLength(io)
                | CompositeHeader.Output io -> CompositeHeader.Output.ToString(), getIOLength(io)
                | CompositeHeader.Comment _ -> CompositeHeader.Comment.ToString(), [ 1 ]
                | CompositeHeader.FreeText _ -> CompositeHeader.FreeText.ToString(), [ 1 ]
            )

        let containsMultipleHeaders (segment: string []) =
            if segment.Length > 1 then
                let result =
                    segment.[1..]
                    |> Array.map (fun item ->
                        AnnotationTableContextMenuUtil.checkForHeader(item))
                    |> Array.contains true
                result
            else
                false

        let fitHeaders (strings: string[]) (headersSizes: (string * int list)[]) (maxColumns: int) =

            let rec tryFit (header: string[]) index (remaining: (string * int list) list) (columnsLeft: int) =

                match remaining, columnsLeft with
                | _, 0 -> Some []
                | [], _ -> Some []
                | (name, sizes) :: rest, _ ->
                    sizes
                    |> List.sortDescending
                    |> List.choose (fun size ->
                        let actualSize =
                            if index + size <= header.Length then size
                            elif index < header.Length then header.Length - index
                            else 0

                        if actualSize > 0 then

                            let segment = header.[index..index + actualSize - 1]

                            match tryFit header (index + actualSize) rest (columnsLeft - 1) with
                            | Some restResult ->
                                if containsMultipleHeaders segment then
                                    None
                                else
                                    Some((name, segment) :: restResult)
                            | None -> None
                        else
                            match tryFit header (index + actualSize) rest (columnsLeft - 1) with
                            | Some restResult -> Some(restResult)
                            | None -> None
                    )
                    |> List.tryHead

            tryFit strings 0 (Array.toList headersSizes) maxColumns

        let expectedHeadersLength = getHeadersExpectedLength data

        let fittedHeaders = fitHeaders data expectedHeadersLength targetCollumnCount

        let result =
            let row =
                if fittedHeaders.IsSome then
                    fittedHeaders.Value
                else
                    []
            row
            |> List.map (fun (_, cells) -> cells) |> Array.ofList

        result
        |> Array.map (fun header -> CompositeHeader.fromContentValid(header))

    static member predictPasteBehaviour
        (cellIndex: CellCoordinate, targetTable: ArcTable, selectHandle: SelectHandle, data: string[][])
        =

        let checkForHeaders (row: string[]) =
            printfn $"row: {row.Length}"
            row
            |> Array.map (fun cell -> AnnotationTableContextMenuUtil.checkForHeader(cell) || cellIndex.y < 0)
            |> Array.contains true

        let headerData, bodyData =
            if data.Length > 0 then
                if checkForHeaders(data.[0]) then
                    data.[0], data.[1..]
                else
                    [||], data
            else
                [||], data

        //Convert cell coordinates to array
        let cellCoordinates : CellCoordinate [] =
            selectHandle.getSelectedCells ()
            |> Array.ofSeq

        //Get all required headers for cells
        let compositeColumns =
            let columnIndices = cellCoordinates |> Array.distinctBy (fun item -> item.x)
            columnIndices
            |> Array.map (fun index -> targetTable.GetColumn(index.x - 1))

        //Group all cells based on their row
        let groupedCellCoordinates =
            cellCoordinates
            |> Array.groupBy (fun item -> item.y)
            |> Array.map (fun (_, row) -> row)

        let fittedCells = AnnotationTableContextMenuUtil.getFittedCells (bodyData, compositeColumns |> Array.map (fun column -> column.Header))

        let isEmpty =
            Array.isEmpty fittedCells
            || fittedCells
                |> Array.map (fun row ->
                    Array.isEmpty row
                    || Array.forall (fun (cell: CompositeCell) -> String.IsNullOrWhiteSpace(cell.ToTabStr())) row
                )
                |> Array.contains true

        if headerData.Length > 0 then
            let columnHeaders = AnnotationTableContextMenuUtil.getFittedHeaders(headerData, compositeColumns.Length)

            let compositeColumns =
                let columns = fittedCells |> Array.transpose
                if columns.Length > 0 then
                    columns
                    |> Array.mapi (fun index column -> CompositeColumn.create(columnHeaders.[index], column))
                    |> ResizeArray
                else
                    columnHeaders
                    |> Array.map (fun header -> CompositeColumn.create(header))
                    |> ResizeArray

            PasteCases.AddColumns {|
                data = compositeColumns
                columnIndex = cellIndex.x
                headers = headerData
                pasteData = fittedCells
                coordinates = groupedCellCoordinates
            |}
        else
            if isEmpty then
                PasteCases.PasteColumns {|
                    data = fittedCells
                    coordinates = groupedCellCoordinates
                |}
                //PasteCases.Unknown {| data = bodyData; headers = compositeColumns |> Array.map (fun column -> column.Header) |}
            else
                PasteCases.PasteColumns {|
                    data = fittedCells
                    coordinates = groupedCellCoordinates
                |}

    static member pasteHeaders(headers: string[], coordinates: CellCoordinate[][], table: ArcTable, setTable, ?forceConvert) =

        let forceConvert = defaultArg forceConvert true

        let columnCoordinates =
            coordinates
            |> Array.collect (fun coordinate ->
                coordinate
                |> Array.where (fun item -> item.y - 1 < 0))

        let columnHeaders = AnnotationTableContextMenuUtil.getFittedHeaders(headers, columnCoordinates.Length)

        columnCoordinates
        |> Array.iteri (fun index coordinate ->
            table.UpdateHeader(coordinate.x - 1, columnHeaders.[index], forceConvert))

        table.Copy() |> setTable

    static member pasteBody
        (
            pasteColumns:
                {|
                    data: CompositeCell[][]
                    coordinates: CellCoordinate[][]
                |},
            coordinate: CellCoordinate,
            table: ArcTable,
            selectHandle: SelectHandle,
            setTable
        ) =

        let getCorrectTarget
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

        //Check amount of selected cells
        //When multiple cells are selected a different handling is required
        if selectHandle.getCount () > 1 then
            //Map over all selected cells
            pasteColumns.coordinates
            |> Array.iteri (fun yi row ->
                //Restart row index, when the amount of selected rows is bigger than copied rows
                let yIndex = AnnotationTableContextMenuUtil.getIndex (yi, pasteColumns.data.Length)
                row
                |> Array.iteri (fun xi rowCoordinate ->
                    //Restart column index, when the amount of selected columns is bigger than copied columns
                    //let xIndex = AnnotationTableContextMenuUtil.getIndex (xi, pasteColumns.data.[0].Length)

                    let currentCell = pasteColumns.data.[yIndex].[xi]
                    let newCoordinate = {| x = rowCoordinate.x - 1; y = rowCoordinate.y - 1 |}
                    let newTarget = getCorrectTarget currentCell table newCoordinate 0
                    table.SetCellAt(rowCoordinate.x - 1, rowCoordinate.y - 1, newTarget)
                )
            )
        else
            if pasteColumns.data.Length = 0 then
                table.ClearCell(coordinate)
            else
                let currentCell = pasteColumns.data.[0].[0]
                let newTarget = getCorrectTarget currentCell table coordinate 0
                table.SetCellAt(coordinate.x, coordinate.y, newTarget)

        table.Copy() |> setTable

    static member paste
        (
            pasteCases: PasteCases,
            coordinate: CellCoordinate,
            table: ArcTable,
            setModal: Types.AnnotationTable.ModalTypes option -> unit,
            selectHandle: SelectHandle,
            setTable: ArcTable -> unit
        ) =

        match pasteCases with
        | AddColumns addColumns ->
            setModal (
                AnnotationTable.ModalTypes.PasteCaseUserInput(PasteCases.AddColumns addColumns, selectHandle)
                |> Some
            )
        | PasteColumns pasteColumns ->
            AnnotationTableContextMenuUtil.pasteBody (pasteColumns, coordinate, table, selectHandle, setTable)
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
                    setModal,
                    selectHandle,
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
        let cellIndex = {| x = index.x - 1; y = index.y - 1 |}
        let cell = arcTable.GetCellAt(cellIndex.x, cellIndex.y)
        let header = arcTable.GetColumn(cellIndex.x).Header

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
                onClick = fun _ -> AnnotationTableContextMenuUtil.fillColumn (cellIndex, arcTable, setArcTable)
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
            table: ArcTable,
            setTable: ArcTable -> unit,
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
                Html.div "Delete Column",
                icon = Icons.DeleteDown(),
                kbdbutton = ATCMC.KbdHint("DelC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteColumn (cc, columnIndex - 1, table, selectHandle)
                        |> setTable
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