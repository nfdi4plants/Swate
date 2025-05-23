namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Feliz

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


[<Erase>]
type AnnotationTableContextMenu =
    static member CompositeCellContent (index: CellCoordinate, table: ArcTable, setTable: ArcTable -> unit, selectHandle: SelectHandle) =
        let cellIndex = (index.x - 1, index.y - 1)
        let cell = table.Values[cellIndex]
        [
            ContextMenuItem(
                Html.div "Details",
                icon = ATCMC.Icon "fa-solid fa-magnifying-glass",
                kbdbutton = ATCMC.KbdHint("D")
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
                kbdbutton = ATCMC.KbdHint("C")
            )
            ContextMenuItem(
                Html.div "Cut",
                icon = ATCMC.Icon "fa-solid fa-scissors",
                kbdbutton = ATCMC.KbdHint("X")
            )
            ContextMenuItem(
                Html.div "Paste",
                icon = ATCMC.Icon "fa-solid fa-paste",
                kbdbutton = ATCMC.KbdHint("V")
            )
            ContextMenuItem(isDivider=true)
            ContextMenuItem(
                Html.div "Delete Row",
                icon = ATCMC.Icon "fa-solid fa-delete-left",
                kbdbutton = ATCMC.KbdHint("DelR")
            )
            ContextMenuItem(
                Html.div "Delete Column",
                icon = ATCMC.Icon "fa-solid fa-delete-left fa-rotate-270",
                kbdbutton = ATCMC.KbdHint("DelC")
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