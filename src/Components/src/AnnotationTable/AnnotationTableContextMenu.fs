namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Feliz

[<AutoOpenAttribute>]
module Helper =

    type PasteCases =
        | AddColumns of {| data: string[][]; columnIndex: int |}
        | PasteColumns of
            {|
                data: string[][]
                columnIndex: int
                rowIndex: int
            |}
        | PasteSinglesAsTerm of
            {|
                data: string[][]
                headers: CompositeHeader[]
                groupedCellCoordinates: CellCoordinate[][]
            |}

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
                        |> Array.map (fun coordinate -> table.GetCellAt(coordinate.x - 1, coordinate.y - 1)))

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

    static member predictPasteBehaviour
        (cellIndex: CellCoordinate, targetTable: ArcTable, selectHandle: SelectHandle, data: string[][])
        =

        let checkForHeaders (row: string[]) =
            let headers = ARCtrl.CompositeHeader.Cases |> Array.map (fun (_, header) -> header)

            let areHeaders =
                headers
                |> Array.collect (fun header -> row |> Array.map (fun cell -> cell.StartsWith(header)))

            Array.contains true areHeaders

        if checkForHeaders data.[0] then
            PasteCases.AddColumns {|
                columnIndex = cellIndex.x
                data = data
            |}
        else if selectHandle.getCount () > 1 then

            //Convert cell coordinates to array
            let cellCoordinates = selectHandle.getSelectedCells () |> Array.ofSeq

            //Group all cells based on their row
            let groupedCellCoordinates =
                cellCoordinates
                |> Array.ofSeq
                |> Array.groupBy (fun item -> item.y)
                |> Array.map (fun (_, row) -> row)

            //Get all required headers for cells
            let headers =
                let columnIndices = cellCoordinates |> Array.distinctBy (fun item -> item.x)

                columnIndices
                |> Array.map (fun index -> targetTable.GetColumn(index.x - 1).Header)

            let termIndices, lengthWithoutTerms = CompositeCell.getHeaderParsingInfo (headers)

            if
                termIndices.Length > 0
                && data.[0].Length >= termIndices.Length + lengthWithoutTerms
            then
                PasteCases.PasteSinglesAsTerm {|
                    data = data
                    headers = headers
                    groupedCellCoordinates = groupedCellCoordinates
                |}
            else
                PasteCases.PasteColumns {|
                    columnIndex = cellIndex.x
                    rowIndex = cellIndex.y
                    data = data
                |}
        else
            PasteCases.PasteColumns {|
                columnIndex = cellIndex.x
                rowIndex = cellIndex.y
                data = data
            |}

    //Recalculates the index, then the amount of selected cells is bigger than the amount of copied cells
    static member getIndex(startIndex, length) =
        let rec loop index length =
            if index < length then
                index
            else
                loop (index - length) length

        loop startIndex length

    static member insertPotentialTermColumns
        (
            table: ArcTable,
            data: string[][],
            headers: CompositeHeader[],
            groupedCellCoordinates: CellCoordinate[][],
            setTable
        ) =

        let parseRow (row: string[][]) (headers: CompositeHeader[]) =
            let rec loop index result =
                if index >= headers.Length then
                    result |> List.rev |> Array.ofList
                else
                    let header = headers.[index]

                    let index =
                        if index >= row.Length then
                            AnnotationTableContextMenuUtil.getIndex (index, row.Length)
                        else
                            index

                    match header with
                    | x when x.IsSingleColumn ->
                        let cell = CompositeCell.fromContentValid (row.[index], header)
                        loop (index + 1) (cell :: result)
                    | x when x.IsDataColumn ->
                        let cell = CompositeCell.fromContentValid (row.[index], header)
                        loop (index + 1) (cell :: result)
                    | x when x.IsTermColumn ->
                        let value =
                            if row.[index].Length = 2 then
                                [| row.[index].[0] |]
                            else
                                row.[index]

                        let cell = CompositeCell.fromContentValid (value, header)
                        loop (index + 1) (cell :: result)

            loop 0 []

        let getHeadersWithLength (headers: CompositeHeader[]) =
            headers
            |> Array.map (fun header ->
                match header with
                | x when x.IsSingleColumn -> x.ToString(), [ 1 ]
                | x when x.IsDataColumn -> x.ToString(), [ 4 ]
                | x when x.IsTermColumn -> x.ToString(), [ 1; 2; 3; 4 ])

        let fitHeaders (strings: string[]) (headers: (string * int list)[]) =
            let rec tryFit index headerList =
                match headerList with
                | [] -> if index = strings.Length then Some [] else None
                | (name, sizes) :: rest ->
                    sizes
                    |> List.tryPick (fun size ->
                        if index + size <= strings.Length then
                            let segment = strings.[index .. index + size - 1]

                            match tryFit (index + size) rest with
                            | Some restResult -> Some((name, segment) :: restResult)
                            | None -> None
                        else
                            None)

            tryFit 0 (Array.toList headers)

        let fittingHeaders = getHeadersWithLength headers

        let fittedRows = data |> Array.map (fun row -> fitHeaders row fittingHeaders)

        let result =
            fittedRows
            |> Array.choose (fun row -> row)
            |> Array.map (fun row -> row |> List.map (fun (_, cells) -> cells) |> Array.ofList)

        let compositeCells = result |> Array.map (fun row -> parseRow row headers)

        //Map over all selected cells
        groupedCellCoordinates
        |> Array.iteri (fun yi row ->
            //Restart row index, when the amount of selected rows is bigger than copied rows
            let yIndex = AnnotationTableContextMenuUtil.getIndex (yi, compositeCells.Length)

            row
            |> Array.iteri (fun xi coordinate ->
                //Restart column index, when the amount of selected columns is bigger than copied columns
                let xIndex = AnnotationTableContextMenuUtil.getIndex (xi, compositeCells.[0].Length)
                table.SetCellAt(coordinate.x - 1, coordinate.y - 1, compositeCells.[yIndex].[xIndex])))

        table.Copy() |> setTable

    static member paste
        ((columnIndex, rowIndex): (int * int), table: ArcTable, data: string[][], selectHandle: SelectHandle, setTable)
        =
        //Check amount of selected cells
        //When multiple cells are selected a different handling is required
        if selectHandle.getCount () > 1 then

            //Convert cell coordinates to array
            let cellCoordinates = selectHandle.getSelectedCells () |> Array.ofSeq

            //Group all cells based on their row
            let groupedCellCoordinates =
                cellCoordinates
                |> Array.ofSeq
                |> Array.groupBy (fun item -> item.y)
                |> Array.map (fun (_, row) -> row)

            //Get all required headers for cells
            let headers =
                let columnIndices = cellCoordinates |> Array.distinctBy (fun item -> item.x)
                columnIndices |> Array.map (fun index -> table.GetColumn(index.x - 1).Header)

            //Converts the cells of each row
            let rowCells =
                data |> Array.map (fun row -> CompositeCell.fromTableStr (row, headers))

            //Map over all selected cells
            groupedCellCoordinates
            |> Array.iteri (fun yi row ->
                //Restart row index, when the amount of selected rows is bigger than copied rows
                let yIndex = AnnotationTableContextMenuUtil.getIndex (yi, rowCells.Length)

                row
                |> Array.iteri (fun xi coordinate ->
                    //Restart column index, when the amount of selected columns is bigger than copied columns
                    let xIndex = AnnotationTableContextMenuUtil.getIndex (xi, rowCells.[0].Length)
                    table.SetCellAt(coordinate.x - 1, coordinate.y - 1, rowCells.[yIndex].[xIndex])))
        else
            let selectedHeader = table.GetColumn(columnIndex).Header
            let newCell = CompositeCell.fromTableStr (data.[0], [| selectedHeader |])
            table.SetCellAt(columnIndex, rowIndex, newCell.[0])

        table.Copy() |> setTable

[<Erase>]
type AnnotationTableContextMenu =
    static member CompositeCellContent
        (
            index: CellCoordinate,
            table: ArcTable,
            setTable: ArcTable -> unit,
            selectHandle: SelectHandle,
            setDetailsModal: CellCoordinate option -> unit,
            setPastCases
        ) =
        let cellIndex = {| x = index.x - 1; y = index.y - 1 |}

        [
            ContextMenuItem(
                Html.div "Details",
                icon = ATCMC.Icon "fa-solid fa-magnifying-glass",
                kbdbutton = ATCMC.KbdHint("D"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate> |> Some
                        setDetailsModal cc
            )
            ContextMenuItem(Html.div "Fill Column", icon = ATCMC.Icon "fa-solid fa-pen", kbdbutton = ATCMC.KbdHint("F"))
            ContextMenuItem(
                Html.div "Edit",
                icon = ATCMC.Icon "fa-solid fa-pen-to-square",
                kbdbutton = ATCMC.KbdHint("E")
            )
            ContextMenuItem(
                Html.div "Clear",
                icon = ATCMC.Icon "fa-solid fa-eraser",
                kbdbutton = ATCMC.KbdHint("Del"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.clear (cc, cellIndex, table, selectHandle)
                        |> setTable
            )
            ContextMenuItem(isDivider = true)
            ContextMenuItem(
                Html.div "Copy",
                icon = ATCMC.Icon "fa-solid fa-copy",
                kbdbutton = ATCMC.KbdHint("C"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.copy (cellIndex, table, selectHandle)
                        |> Promise.start

            )
            ContextMenuItem(
                Html.div "Cut",
                icon = ATCMC.Icon "fa-solid fa-scissors",
                kbdbutton = ATCMC.KbdHint("X"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>
                        AnnotationTableContextMenuUtil.copy (cellIndex, table, selectHandle) |> ignore

                        AnnotationTableContextMenuUtil.clear (cc, cellIndex, table, selectHandle)
                        |> setTable
            )
            ContextMenuItem(
                Html.div "Paste",
                icon = ATCMC.Icon "fa-solid fa-paste",
                kbdbutton = ATCMC.KbdHint("V"),
                onClick =
                    fun c ->
                        promise {
                            let cc = c.spawnData |> unbox<CellCoordinate>

                            let! data = AnnotationTableContextMenuUtil.getCopiedCells ()

                            Some(
                                AnnotationTableContextMenuUtil.predictPasteBehaviour (
                                    cellIndex,
                                    table,
                                    selectHandle,
                                    data
                                )
                            )
                            |> setPastCases
                        }
                        |> Promise.start
            )
            ContextMenuItem(isDivider = true)
            ContextMenuItem(
                Html.div "Delete Row",
                icon = ATCMC.Icon "fa-solid fa-delete-left",
                kbdbutton = ATCMC.KbdHint("DelR"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteRow (cc, cellIndex.y, table, selectHandle)
                        |> setTable
            )
            ContextMenuItem(
                Html.div "Delete Column",
                icon = ATCMC.Icon "fa-solid fa-delete-left fa-rotate-270",
                kbdbutton = ATCMC.KbdHint("DelC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteColumn (cc, cellIndex.x, table, selectHandle)
                        |> setTable
            )
            ContextMenuItem(
                Html.div "Move Column",
                icon = ATCMC.Icon "fa-solid fa-arrow-right-arrow-left",
                kbdbutton = ATCMC.KbdHint("MC")
            )
        ]

    static member CompositeHeaderContent
        (index: int, table: ArcTable, setTable: ArcTable -> unit, selectHandle: SelectHandle)
        =
        let header = table.Headers.[index]

        [
            ContextMenuItem(
                Html.div "Delete Column",
                icon = ATCMC.Icon "fa-solid fa-delete-left fa-rotate-270",
                kbdbutton = ATCMC.KbdHint("DelC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteColumn (cc, index, table, selectHandle)
                        |> setTable
            )
        ]

    static member IndexColumnContent
        (index: int, table: ArcTable, setTable: ArcTable -> unit, selectHandle: SelectHandle)
        =
        [
            ContextMenuItem(
                Html.div "Delete Row",
                icon = ATCMC.Icon "fa-solid fa-delete-left",
                kbdbutton = ATCMC.KbdHint("DelR"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        AnnotationTableContextMenuUtil.deleteRow (cc, index, table, selectHandle)
                        |> setTable
            )
        ]