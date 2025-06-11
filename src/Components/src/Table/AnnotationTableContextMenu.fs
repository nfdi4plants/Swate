namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Feliz

[<AutoOpenAttribute>]
module Helper =

    type PasteCases =
    | AddColumns of {| data: string[][]; columnIndex: int |}
    | PasteColumns of {| data: string[][]; columnIndex: int; rowIndex: int |}

    type Clipboard =
        abstract member writeText: string -> JS.Promise<unit>
        abstract member readText: unit -> JS.Promise<string>

    type Navigator =
        abstract member clipboard: Clipboard

    [<Emit("navigator")>]
    let navigator: Navigator = jsNative

/// AnnotationTableContextMenu Components
type ATCMC =
    static member Icon (className: string) =
        Html.i [ prop.className className ]

    static member KbdHint (text: string, ?label: string) =
        let label = defaultArg label text
        {| element = Html.kbd [ prop.className "swt:ml-auto swt:kbd swt:kbd-sm"; prop.text text ]; label = label|}

type AnnotationTableContextMenuUtil =

    static member clear (tableIndex: CellCoordinate, cellIndex: (int * int), table: ArcTable, selectHandle: SelectHandle) : ArcTable =
        if selectHandle.contains tableIndex then
            table.ClearSelectedCells(selectHandle)
        else
            table.ClearCell(cellIndex)
        table.Copy()

    static member deleteRow (tableIndex: CellCoordinate, cellIndex: (int * int), table: ArcTable, selectHandle: SelectHandle) : ArcTable =
        if selectHandle.contains tableIndex then
            let rowCoordinates =
                selectHandle.getSelectedCells()
                |> Array.ofSeq
                |> Array.distinctBy (fun coordinate -> coordinate.y)
                |> Array.map (fun coordinate -> coordinate.y - 1)

            table.RemoveRows(rowCoordinates)
        else
            table.RemoveRow(snd cellIndex)
        table.Copy()

    static member deleteColumn (tableIndex: CellCoordinate, cellIndex: (int * int), table: ArcTable, selectHandle: SelectHandle) : ArcTable =
        if selectHandle.contains tableIndex then
            let columnCoordinates =
                selectHandle.getSelectedCells()
                |> Array.ofSeq
                |> Array.distinctBy (fun coordinate -> coordinate.x)
                |> Array.map (fun coordinate -> coordinate.x - 1)

            table.RemoveColumns(columnCoordinates)
        else
            table.RemoveColumn(fst cellIndex)
        table.Copy()

    static member copy (cellIndex: (int * int), table: ArcTable, selectHandle: SelectHandle) =

        let result =
            if selectHandle.getCount() > 1 then

                let cellCoordinates =
                    selectHandle.getSelectedCells()
                    |> Array.ofSeq
                    |> Array.groupBy (fun item -> item.y)

                let cells =
                    cellCoordinates
                    |> Array.map (fun (_, row) ->
                        row
                        |> Array.map (fun coordinate ->
                            table.GetCellAt(coordinate.x - 1, coordinate.y - 1)
                        )
                    )

                CompositeCell.ToTableTxt(cells)
            else
                let cell = table.GetCellAt(cellIndex)
                cell.ToTabStr()

        navigator.clipboard.writeText result

    static member getCopiedCells() =
        promise {
            let! copiedValue = navigator.clipboard.readText()

            let rows =
                copiedValue.Split([|System.Environment.NewLine|], System.StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (fun item ->
                    item.Split('\t')
                    |> Array.map _.Trim())
            return rows
        }

    static member predictPasteBehaviour((columnIndex, rowIndex): (int * int), targetTable: ArcTable, selectHandle: SelectHandle, data: string [][]) =

        let checkForHeaders (row: string []) =
            let headers = ARCtrl.CompositeHeader.Cases |> Array.map (fun (_, header) -> header)
            let areHeaders =
                headers
                |> Array.collect (fun header ->
                    row
                    |> Array.map (fun cell -> cell.StartsWith(header)))
            Array.contains true areHeaders

        if checkForHeaders data.[0] then
            PasteCases.AddColumns {|columnIndex = columnIndex; data = data|}
        else
            PasteCases.PasteColumns {|columnIndex = columnIndex; rowIndex = rowIndex; data = data|}

    static member paste ((columnIndex, rowIndex): (int * int), table: ArcTable, data:string[][], selectHandle: SelectHandle, setTable) =
        //Check amount of selected cells
        //When multiple cells are selected a different handling is required
        if selectHandle.getCount() > 1 then

            //Convert cell coordinates to array
            let cellCoordinates =
                selectHandle.getSelectedCells()
                |> Array.ofSeq

            //Get all required headers for cells
            let headers =
                let columnIndices =
                    cellCoordinates
                    |> Array.distinctBy (fun item -> item.x)
                columnIndices
                |> Array.map (fun index -> table.GetColumn(index.x - 1).Header)

            //Recalculates the index, then the amount of selected cells is bigger than the amount of copied cells
            let getIndex startIndex length =
                let rec loop index length =
                    if index < length then
                        index
                    else
                        loop (index - length) length
                loop startIndex length

            //Converts the cells of each row
            let rowCells =
                data
                |> Array.map (fun row ->
                    CompositeCell.fromTableStr(row, headers))

            //Group all cells based on their row
            let groupedCellCoordinates =
                cellCoordinates
                |> Array.ofSeq
                |> Array.groupBy (fun item -> item.y)

            //Map over all selected cells
            groupedCellCoordinates
            |> Array.iteri (fun yi (_, row) ->
                //Restart row index, when the amount of selected rows is bigger than copied rows
                let yIndex = getIndex yi rowCells.Length
                row
                |> Array.iteri (fun xi coordinate ->
                    //Restart column index, when the amount of selected columns is bigger than copied columns
                    let xIndex = getIndex xi rowCells.[0].Length
                    table.SetCellAt(coordinate.x - 1, coordinate.y - 1, rowCells.[yIndex].[xIndex])
                )
            )
        else
            let selectedHeader = table.GetColumn(columnIndex).Header
            let newCell = CompositeCell.fromTableStr(data.[0], [|selectedHeader|])
            table.SetCellAt(columnIndex, rowIndex, newCell.[0])
        table.Copy()
        |> setTable

[<Erase>]
type AnnotationTableContextMenu =
    static member CompositeCellContent (index: CellCoordinate, table: ArcTable, setTable: ArcTable -> unit, selectHandle: SelectHandle, setDetailsModal: CellCoordinate option -> unit, setPastCases) =
        let cellIndex = (index.x - 1, index.y - 1)
        [
            ContextMenuItem(
                Html.div "Details",
                icon = ATCMC.Icon "fa-solid fa-magnifying-glass",
                kbdbutton = ATCMC.KbdHint("D"),
                onClick = fun c ->
                    let cc = c.spawnData |> unbox<CellCoordinate> |> Some
                    setDetailsModal cc
            )
            ContextMenuItem(
                Html.div "Fill Column",
                icon = ATCMC.Icon "fa-solid fa-pen",
                kbdbutton = ATCMC.KbdHint("F")
            )
            ContextMenuItem(
                Html.div "Edit",
                icon = ATCMC.Icon "fa-solid fa-pen-to-square",
                kbdbutton = ATCMC.KbdHint("E")
            )
            ContextMenuItem(
                Html.div "Clear",
                icon = ATCMC.Icon "fa-solid fa-eraser",
                kbdbutton = ATCMC.KbdHint("Del"),
                onClick = fun c ->
                    let cc = c.spawnData |> unbox<CellCoordinate>
                    AnnotationTableContextMenuUtil.clear(cc, cellIndex, table, selectHandle)
                    |> setTable
            )
            ContextMenuItem(isDivider=true)
            ContextMenuItem(
                Html.div "Copy",
                icon = ATCMC.Icon "fa-solid fa-copy",
                kbdbutton = ATCMC.KbdHint("C"),
                onClick = fun c ->
                    let cc = c.spawnData |> unbox<CellCoordinate>
                    AnnotationTableContextMenuUtil.copy(cellIndex, table, selectHandle)
                    |> Promise.start

            )
            ContextMenuItem(
                Html.div "Cut",
                icon = ATCMC.Icon "fa-solid fa-scissors",
                kbdbutton = ATCMC.KbdHint("X"),
                onClick = fun c ->
                    let cc = c.spawnData |> unbox<CellCoordinate>
                    AnnotationTableContextMenuUtil.copy(cellIndex, table, selectHandle) |> ignore
                    AnnotationTableContextMenuUtil.clear(cc, cellIndex, table, selectHandle)
                    |> setTable
            )
            ContextMenuItem(
                Html.div "Paste",
                icon = ATCMC.Icon "fa-solid fa-paste",
                kbdbutton = ATCMC.KbdHint("V"),
                onClick = fun c ->
                    promise {
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        let! data = AnnotationTableContextMenuUtil.getCopiedCells()

                        Some (AnnotationTableContextMenuUtil.predictPasteBehaviour(cellIndex, table, selectHandle, data))
                        |> setPastCases
                    }
                    |> Promise.start
            )
            ContextMenuItem(isDivider=true)
            ContextMenuItem(
                Html.div "Delete Row",
                icon = ATCMC.Icon "fa-solid fa-delete-left",
                kbdbutton = ATCMC.KbdHint("DelR"),
                onClick = fun c ->
                    let cc = c.spawnData |> unbox<CellCoordinate>
                    AnnotationTableContextMenuUtil.deleteRow(cc, cellIndex, table, selectHandle)
                    |> setTable
            )
            ContextMenuItem(
                Html.div "Delete Column",
                icon = ATCMC.Icon "fa-solid fa-delete-left fa-rotate-270",
                kbdbutton = ATCMC.KbdHint("DelC"),
                onClick = fun c ->
                    let cc = c.spawnData |> unbox<CellCoordinate>
                    AnnotationTableContextMenuUtil.deleteColumn(cc, cellIndex, table, selectHandle)
                    |> setTable
            )
            ContextMenuItem(
                Html.div "Move Column",
                icon = ATCMC.Icon "fa-solid fa-arrow-right-arrow-left",
                kbdbutton = ATCMC.KbdHint("MC")
            )
        ]

    static member CompositeHeaderContent (index: int, table: ArcTable, setTable: ArcTable -> unit) =
        let header = table.Headers.[index]
        [

        ]

    static member IndexColumnContent (index: int, table: ArcTable, setTable: ArcTable -> unit) =
        [

        ]