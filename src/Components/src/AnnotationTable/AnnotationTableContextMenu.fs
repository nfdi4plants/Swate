namespace Swate.Components

open System
open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Feliz

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

    static member clear
        (tableIndex: CellCoordinate, cellIndex: CellCoordinate, table: ArcTable, selectHandle: SelectHandle)
        : ArcTable =
        if selectHandle.contains tableIndex then
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

                let cells =
                    cellCoordinates
                    |> Array.map (fun (_, row) ->
                        row
                        |> Array.map (fun coordinate -> table.GetCellAt(coordinate.x - 1, coordinate.y - 1))
                    )

                CompositeCell.ToTableTxt(cells)
            else
                let cell = table.GetCellAt((cellIndex.x, cellIndex.y))
                cell.ToTabStr()

        navigator.clipboard.writeText result

    static member getCopiedCells() = promise {
        let! copiedValue = navigator.clipboard.readText ()

        let rows =
            copiedValue.Split([| System.Environment.NewLine |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun item -> item.Split('\t') |> Array.map _.Trim())

        return rows
    }

    static member getFittedCells
        (
            data: string[][],
            headers: CompositeHeader[]
        ) =

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

            loop 0 []

        let getHeadersWithLength (headers: CompositeHeader[]) =
            headers
            |> Array.map (fun header ->
                match header with
                | x when x.IsSingleColumn -> x.ToString(), [ 1 ]
                | x when x.IsDataColumn -> x.ToString(), [ 4 ]
                | x when x.IsTermColumn -> x.ToString(), [ 1; 2; 3; 4 ]
            )

        let fitHeaders (strings: string[]) (headersSizes: (string * int list)[]) =
            let rec tryFit (cell: string[]) index (headerSizesList: (string * int list) list) =
                let index =
                    if index >= strings.Length then
                        0
                    else
                        index
                match headerSizesList with
                | [] -> Some []
                | (name, sizes) :: rest ->
                    sizes
                    |> List.sortDescending
                    |> List.choose (fun size ->
                        let actualSize =
                            if index + size <= cell.Length then size
                            elif index < cell.Length then cell.Length - index
                            else 0
                        if actualSize > 0 then
                            let segment = cell.[index .. index + actualSize - 1]
                            match tryFit cell (index + actualSize) rest with
                            | Some restResult ->
                                Some((name, segment) :: restResult)
                            | None -> None
                        else
                            None
                    )
                    |> List.tryHead
            tryFit strings 0 (Array.toList headersSizes)

        let fittingHeaders = getHeadersWithLength headers

        let fittedRows = data |> Array.map (fun row -> fitHeaders row fittingHeaders)

        let result =
            fittedRows
            |> Array.choose (fun row -> row)
            |> Array.map (fun row -> row |> List.map (fun (_, cells) -> cells) |> Array.ofList)

        result
        |> Array.map (fun row -> fitColumnsToTarget row headers)

    static member predictPasteBehaviour
        (cellIndex: CellCoordinate, targetTable: ArcTable, selectHandle: SelectHandle, data: string[][])
        =

        //Convert cell coordinates to array
        let cellCoordinates =
            selectHandle.getSelectedCells() |> Array.ofSeq

        //Get all required headers for cells
        let headers =
            let columnIndices = cellCoordinates |> Array.distinctBy (fun item -> item.x)

            columnIndices
            |> Array.map (fun index -> targetTable.GetColumn(index.x - 1).Header)

        let checkForHeaders (row: string[]) =
            let headers = ARCtrl.CompositeHeader.Cases |> Array.map (fun (_, header) -> header)

            let areHeaders =
                headers
                |> Array.collect (fun header -> row |> Array.map (fun cell -> cell.StartsWith(header)))

            Array.contains true areHeaders

        if checkForHeaders data.[0] then

            let body =
                let rest = data.[1..]
                if rest.Length > 0 then rest
                else [||]
            let columns = Array.append [| data.[0] |] body |> Array.transpose
            let columnsList = columns |> Seq.toArray |> Array.map (Seq.toArray)
            let compositeColumns = ARCtrl.Spreadsheet.ArcTable.composeColumns columnsList |> ResizeArray

            PasteCases.AddColumns {|
                data = compositeColumns
                columnIndex = cellIndex.x
            |}
        else

            //Group all cells based on their row
            let groupedCellCoordinates =
                cellCoordinates
                |> Array.ofSeq
                |> Array.groupBy (fun item -> item.y)
                |> Array.map (fun (_, row) -> row)

            let fittedCells =
                AnnotationTableContextMenuUtil.getFittedCells(
                    data,
                    headers
                )

            let isEmpty =
                Array.isEmpty fittedCells ||
                fittedCells
                |> Array.map (fun row ->
                    Array.isEmpty row ||
                    Array.forall (fun (cell: CompositeCell) -> String.IsNullOrWhiteSpace (cell.ToTabStr())) row
                )
                |> Array.contains true

            if isEmpty then
                PasteCases.Unknown {|
                    data = data
                    headers = headers
                |}
            else
                PasteCases.PasteColumns {|
                    data = fittedCells
                    coordinates = groupedCellCoordinates
                |}

    //Recalculates the index, then the amount of selected cells is bigger than the amount of copied cells
    static member getIndex(startIndex, length) =
        let rec loop index length =
            if index < length then
                index
            else
                loop (index - length) length

        loop startIndex length

    static member pasteDefault
        (pasteColumns: {| data: CompositeCell [][]; coordinates: CellCoordinate [][] |}, coordinate: CellCoordinate, table: ArcTable, selectHandle: SelectHandle, setTable)
        =

        let getCorrectTarget (currentCell: CompositeCell) (table: ArcTable) (targetCoordinate: CellCoordinate) adaption =
            if currentCell.isUnitized then
                let targetCell = table.GetCellAt(targetCoordinate.x - adaption, targetCoordinate.y - adaption)
                let value, unit = currentCell.AsUnitized
                if targetCell.isUnitized && unit.isEmpty() then
                    let _, targetUnit = targetCell.AsUnitized
                    let newTarget = CompositeCell.createUnitized(value, targetUnit)
                    newTarget
                elif targetCell.isTerm && unit.isEmpty() then
                    let targetTerm = targetCell.AsTerm
                    let newTarget = CompositeCell.createUnitized(value, targetTerm)
                    newTarget
                else
                    currentCell
            else
                currentCell

        //Check amount of selected cells
        //When multiple cells are selected a different handling is required
        if selectHandle.getCount() > 1 then

            //Map over all selected cells
            pasteColumns.coordinates
            |> Array.iteri (fun yi row ->
                //Restart row index, when the amount of selected rows is bigger than copied rows
                let yIndex = AnnotationTableContextMenuUtil.getIndex (yi, pasteColumns.data.Length)
                row
                |> Array.iteri (fun xi coordinate ->
                    //Restart column index, when the amount of selected columns is bigger than copied columns
                    let xIndex = AnnotationTableContextMenuUtil.getIndex (xi, pasteColumns.data.[0].Length)
                    let currentCell = pasteColumns.data.[yIndex].[xIndex]
                    let newTarget = getCorrectTarget currentCell table coordinate 1
                    table.SetCellAt(coordinate.x - 1, coordinate.y - 1, newTarget)
                )
            )
        else
            let currentCell = pasteColumns.data.[0].[0]
            let newTarget = getCorrectTarget currentCell table coordinate 0
            table.SetCellAt(coordinate.x, coordinate.y, newTarget)

        table.Copy() |> setTable

    static member paste
        (pasteCases: PasteCases, coordinate:CellCoordinate, table: ArcTable, setModal, selectHandle: SelectHandle, setTable)
        =

        match pasteCases with
        | AddColumns addColumns ->
            setModal (
                AnnotationTable.ModalTypes.PasteCaseUserInput(PasteCases.AddColumns addColumns)
            )
        | PasteColumns pasteColumns ->
            AnnotationTableContextMenuUtil.pasteDefault (
                pasteColumns,
                coordinate,
                table,
                selectHandle,
                setTable
            )
        | Unknown unknownPasteCase ->
            setModal (
                AnnotationTable.ModalTypes.UnknownPasteCase(PasteCases.Unknown unknownPasteCase)
            )

[<Erase>]
type AnnotationTableContextMenu =
    static member CompositeCellContent
        (
            index: CellCoordinate,
            arcTable: ArcTable,
            setArcTable: ArcTable -> unit,
            selectHandle: SelectHandle,
            setModal: Types.AnnotationTable.ModalTypes -> unit
        ) =
        let cellIndex = {| x = index.x - 1; y = index.y - 1 |}
        let cell = arcTable.GetCellAt(cellIndex.x, cellIndex.y)
        let header = arcTable.GetColumn(cellIndex.x).Header

        let transformName =
            match cell with
            | CompositeCell.Term _ -> "Transform to Unit"
            | CompositeCell.Unitized _ -> "Transform to Term"
            | CompositeCell.Data _ -> "Transform to Text"
            | CompositeCell.FreeText _ ->
                if header.IsDataColumn then "Transform to Data"
                else ""
        [
            ContextMenuItem(
                Html.div "Details",
                icon = Icons.MagnifyingClassPlus(),
                kbdbutton = ATCMC.KbdHint("D"),
                onClick = fun _ -> AnnotationTable.ModalTypes.Details index |> setModal
            )
            ContextMenuItem(
                Html.div "Edit",
                icon = Icons.PenToSquare(),
                kbdbutton = ATCMC.KbdHint("E"),
                onClick = fun _ -> AnnotationTable.ModalTypes.Edit index |> setModal
            )
            ContextMenuItem(
                Html.div "Fill Column",
                icon = Icons.Pen(),
                kbdbutton = ATCMC.KbdHint("F")
            )
            if not (String.IsNullOrWhiteSpace(transformName)) then
                ContextMenuItem(
                    Html.div transformName,
                    icon = Icons.ArrorRightLeft(),
                    kbdbutton = ATCMC.KbdHint("T"),
                    onClick = fun _ -> AnnotationTable.ModalTypes.Transform index |> setModal
                )
            ContextMenuItem(
                Html.div "Clear",
                icon = Icons.Eraser(),
                kbdbutton = ATCMC.KbdHint("Del"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.clear (cc, cellIndex, arcTable, selectHandle)
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
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.copy (cellIndex, arcTable, selectHandle)
                        |> ignore

                        AnnotationTableContextMenuUtil.clear (cc, cellIndex, arcTable, selectHandle)
                        |> setArcTable
            )
            ContextMenuItem(
                Html.div "Paste",
                icon = Icons.Paste(),
                kbdbutton = ATCMC.KbdHint("V"),
                onClick =
                    fun _ ->
                        promise {
                            let! data = AnnotationTableContextMenuUtil.getCopiedCells ()

                            try
                                let prediction =
                                    AnnotationTableContextMenuUtil.predictPasteBehaviour (
                                        cellIndex,
                                        arcTable,
                                        selectHandle,
                                        data
                                    )

                                AnnotationTableContextMenuUtil.paste(
                                    prediction,
                                    cellIndex,
                                    arcTable,
                                    setModal,
                                    selectHandle,
                                    setArcTable
                                )
                            with exn ->
                                setModal (
                                    AnnotationTable.ModalTypes.Error(exn.Message)
                                )
                        }
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
                        setModal (AnnotationTable.ModalTypes.MoveColumn(cc, cellIndex))
            )
        ]

    static member CompositeHeaderContent
        (columnIndex: int, table: ArcTable, setTable: ArcTable -> unit, selectHandle: SelectHandle, setModal: Types.AnnotationTable.ModalTypes -> unit)
        =
        let cellCoordinate : CellCoordinate = {| y = 0; x = columnIndex |}
        [
            ContextMenuItem(
                Html.div "Details",
                icon = Icons.MagnifyingClassPlus(),
                kbdbutton = ATCMC.KbdHint("D"),
                onClick = fun _ -> AnnotationTable.ModalTypes.Details cellCoordinate |> setModal
            )
            ContextMenuItem(
                Html.div "Edit",
                icon = Icons.PenToSquare(),
                kbdbutton = ATCMC.KbdHint("E"),
                onClick = fun _ -> AnnotationTable.ModalTypes.Edit cellCoordinate |> setModal
            )
            ContextMenuItem(isDivider = true)
            ContextMenuItem(
                Html.div "Delete Column",
                icon = Icons.DeleteDown(),
                kbdbutton = ATCMC.KbdHint("DelC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteColumn (cc, columnIndex, table, selectHandle)
                        |> setTable
            )
            ContextMenuItem(
                Html.div "Move Column",
                icon = Icons.ArrorRightLeft(),
                kbdbutton = ATCMC.KbdHint("MC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>
                        setModal (AnnotationTable.ModalTypes.MoveColumn(cc, cc))
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