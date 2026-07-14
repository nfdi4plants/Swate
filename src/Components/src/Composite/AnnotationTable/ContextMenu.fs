namespace Swate.Components.Composite.AnnotationTable

open System
open Fable.Core
open ARCtrl
open Feliz

open Swate.Components
open Swate.Components.Primitive
open Swate.Components.Primitive.ContextMenu.Types
open Swate.Components.Composite.Table
open Swate.Components.Composite.Table.Types
open Swate.Components.Composite.AnnotationTable.Types

/// AnnotationTableContextMenu Components
type ATCMC =
    static member Icon(className: string) = Html.i [ prop.className className ]

    static member KbdHint(text: string, ?label: string) =
        let label = defaultArg label text

        {|
            element =
                Html.kbd [
                    prop.className "swt:ml-auto swt:kbd swt:kbd-sm"
                    prop.text text
                ]
            label = label
        |}

type AnnotationTableContextMenuUtil =

    static member fillColumn(index: CellCoordinate, table: ArcTable) : ArcTable =
        let cell = table.GetCellAt(index.x - 1, index.y - 1)
        let nextTable = table.Copy()

        for y in 0 .. table.RowCount - 1 do
            nextTable.SetCellAt(index.x - 1, y, cell.Copy())

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
            table.RemoveColumn(colIndex - 1)

        table.Copy()

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
                        AnnotationTableClipboard.clear (cellIndex, arcTable, selectHandle)
                        |> setArcTable
            )
            ContextMenuItem(isDivider = true)
            ContextMenuItem(
                Html.div "Copy",
                icon = Icons.Copy(),
                kbdbutton = ATCMC.KbdHint("C"),
                onClick =
                    fun _ ->
                        AnnotationTableClipboard.copy (cellIndex, arcTable, selectHandle)
                        |> Promise.start

            )
            if not containsHeaderRow then
                ContextMenuItem(
                    Html.div "Cut",
                    icon = Icons.Scissor(),
                    kbdbutton = ATCMC.KbdHint("X"),
                    onClick =
                        fun _ ->
                            AnnotationTableClipboard.cut (cellIndex, arcTable, setArcTable, selectHandle)
                            |> Promise.start
                )
            ContextMenuItem(
                Html.div "Paste",
                icon = Icons.Paste(),
                kbdbutton = ATCMC.KbdHint("V"),
                onClick =
                    fun _ ->
                        AnnotationTableClipboard.tryPasteCopiedCells (
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
                        AnnotationTableClipboard.copy (cellCoordinate, arcTable, selectHandle)
                        |> Promise.start

            )
            ContextMenuItem(
                Html.div "Paste",
                icon = Icons.Paste(),
                kbdbutton = ATCMC.KbdHint("V"),
                onClick =
                    fun _ ->
                        AnnotationTableClipboard.tryPasteCopiedCells (
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

                        AnnotationTableContextMenuUtil.deleteColumn (cc, columnIndex, arcTable, selectHandle)
                        |> setArcTable
            )
            ContextMenuItem(
                Html.div "Move Column",
                icon = Icons.ArrorRightLeft(),
                kbdbutton = ATCMC.KbdHint("MC"),
                onClick =
                    fun c ->
                        let cc = c.spawnData |> unbox<CellCoordinate>

                        setModal (AnnotationTable.ModalTypes.MoveColumn(cc, {| x = columnIndex; y = 0 |}) |> Some)
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
