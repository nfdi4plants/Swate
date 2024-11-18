module MainComponents.ContextMenu

open Feliz
open Feliz.DaisyUI
open Spreadsheet
open ARCtrl
open Model
open Shared

module private Shared =

    let isUnitOrTermCell (cell: CompositeCell option) =
        cell.IsSome && (cell.Value.isTerm || cell.Value.isUnitized)

    let isHeader (rowIndex: int) = rowIndex < 0

module Table =
    type private ContextFunctions = {
        DeleteRow       : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        DeleteColumn    : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        MoveColumn      : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        Copy            : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        Cut             : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        Paste           : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        PasteAll        : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        FillColumn      : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        Clear           : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        TransformCell   : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        UpdateAllCells  : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        GetTermDetails  : OntologyAnnotation -> (unit -> unit) -> Browser.Types.MouseEvent -> unit
        EditColumn      : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        RowIndex        : int
        ColumnIndex     : int
    }

    let private contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (contextCell: CompositeCell option) (header: CompositeHeader) (rmv: unit -> unit) =
        /// This element will remove the contextmenu when clicking anywhere else
        let isHeader = Shared.isHeader funcs.RowIndex
        let buttonList = [
            Components.BaseContextMenu.Item ("Edit Column", funcs.EditColumn rmv, "fa-solid fa-table-columns")
            if (isHeader && header.IsTermColumn) || Shared.isUnitOrTermCell contextCell then
                let oa = if isHeader then header.ToTerm() else contextCell.Value.ToOA()
                if oa.TermAccessionShort <> "" then
                    Components.BaseContextMenu.Item ("Details", funcs.GetTermDetails oa rmv, "fa-solid fa-magnifying-glass")
            if not isHeader then
                Components.BaseContextMenu.Item ("Fill Column", funcs.FillColumn rmv, "fa-solid fa-pen")
                if Shared.isUnitOrTermCell contextCell then
                    let text = if contextCell.Value.isTerm then "As Unit Cell" else "As Term Cell"
                    Components.BaseContextMenu.Item (text, funcs.TransformCell rmv, "fa-solid fa-arrow-right-arrow-left")
                elif header.IsDataColumn then
                    let text = if contextCell.Value.isFreeText then "As Data Cell" else "As Free Text Cell"
                    Components.BaseContextMenu.Item (text, funcs.TransformCell rmv, "fa-solid fa-arrow-right-arrow-left")
                else
                    Components.BaseContextMenu.Item ("Update Column", funcs.UpdateAllCells rmv, "fa-solid fa-ellipsis-vertical")
                Components.BaseContextMenu.Item ("Clear", funcs.Clear rmv, "fa-solid fa-eraser")
                Components.BaseContextMenu.Divider()
                Components.BaseContextMenu.Item ("Copy", funcs.Copy rmv, "fa-solid fa-copy")
                Components.BaseContextMenu.Item ("Cut", funcs.Cut rmv, "fa-solid fa-scissors")
                Components.BaseContextMenu.Item ("Paste", funcs.Paste rmv, "fa-solid fa-paste")
                Components.BaseContextMenu.Item ("Paste All", funcs.PasteAll rmv, "fa-solid fa-paste")
                Components.BaseContextMenu.Divider()
                Components.BaseContextMenu.Item ("Delete Row", funcs.DeleteRow rmv, "fa-solid fa-delete-left")
            Components.BaseContextMenu.Item ("Delete Column", funcs.DeleteColumn rmv, "fa-solid fa-delete-left fa-rotate-270")
            Components.BaseContextMenu.Item ("Move Column", funcs.MoveColumn rmv, "fa-solid fa-arrow-right-arrow-left")
        ]
        Components.BaseContextMenu.Main(mousex, mousey, rmv, buttonList)

    let onContextMenu (index: int*int, model: Model, dispatch) = fun (e: Browser.Types.MouseEvent) ->
        e.stopPropagation()
        e.preventDefault()
        let ci, ri = index
        let mousePosition = int e.pageX, int e.pageY
        /// if there are selected cells in the same column as the clicked event, delete all selected rows.
        let deleteRowEvent _ =
            let s = Set.toArray model.SpreadsheetModel.SelectedCells
            if Array.isEmpty s |> not && Array.forall (fun (c,r) -> c = ci) s && Array.contains index s then
                let indexArr = s |> Array.map snd |> Array.distinct
                Spreadsheet.DeleteRows indexArr |> Messages.SpreadsheetMsg |> dispatch
            else
                Spreadsheet.DeleteRow (ri) |> Messages.SpreadsheetMsg |> dispatch
        let cell = model.SpreadsheetModel.ActiveTable.TryGetCellAt(ci, ri)
        let header = model.SpreadsheetModel.ActiveTable.Headers.[ci]
        let isSelectedCell = model.SpreadsheetModel.SelectedCells.Contains index
        let editColumnEvent _ = Modals.Controller.renderModal("EditColumn_Modal", Modals.EditColumn.Main (fst index) model dispatch)
        let triggerMoveColumnModal _ = Modals.Controller.renderModal("MoveColumn_Modal", Modals.MoveColumn.Main(ci, model, dispatch))
        let triggerUpdateColumnModal _ =
            let columnIndex = fst index
            let column = model.SpreadsheetModel.ActiveTable.GetColumn columnIndex
            Modals.Controller.renderModal("UpdateColumn_Modal", Modals.UpdateColumn.Main(ci, column, dispatch))
        let triggerTermModal oa _ =
            Modals.Controller.renderModal("TermDetails_Modal", Modals.TermModal.Main(oa, dispatch))
        let funcs = {
            DeleteRow       = fun rmv e -> rmv(); deleteRowEvent e
            DeleteColumn    = fun rmv e -> rmv(); Spreadsheet.DeleteColumn (ci) |> Messages.SpreadsheetMsg |> dispatch
            MoveColumn      = fun rmv e -> rmv(); triggerMoveColumnModal e
            Copy            = fun rmv e ->
                rmv();
                if isSelectedCell then
                    Spreadsheet.CopySelectedCells |> Messages.SpreadsheetMsg |> dispatch
                else
                    Spreadsheet.CopyCell index |> Messages.SpreadsheetMsg |> dispatch
            Cut             = fun rmv e -> rmv (); Spreadsheet.CutCell index |> Messages.SpreadsheetMsg |> dispatch
            Paste           = fun rmv e ->
                rmv();
                if isSelectedCell then
                    Spreadsheet.PasteSelectedCells |> Messages.SpreadsheetMsg |> dispatch
                else
                    Spreadsheet.PasteCell index |> Messages.SpreadsheetMsg |> dispatch
            PasteAll        = fun rmv e ->
                rmv();
                Spreadsheet.PasteCellsExtend index |> Messages.SpreadsheetMsg |> dispatch
            FillColumn      = fun rmv e -> rmv (); Spreadsheet.FillColumnWithTerm index |> Messages.SpreadsheetMsg |> dispatch
            Clear           = fun rmv e -> rmv (); if isSelectedCell then Spreadsheet.ClearSelected |> Messages.SpreadsheetMsg |> dispatch else Spreadsheet.Clear [|index|] |> Messages.SpreadsheetMsg |> dispatch
            TransformCell   = fun rmv e ->
                if cell.IsSome && (cell.Value.isTerm || cell.Value.isUnitized) then
                    let nextCell = if cell.Value.isTerm then cell.Value.ToUnitizedCell() else cell.Value.ToTermCell()
                    rmv(); Spreadsheet.UpdateCell (index, nextCell) |> Messages.SpreadsheetMsg |> dispatch
                elif cell.IsSome && header.IsDataColumn then
                    let nextCell = if cell.Value.isFreeText then cell.Value.ToDataCell() else cell.Value.ToFreeTextCell()
                    rmv(); Spreadsheet.UpdateCell (index, nextCell) |> Messages.SpreadsheetMsg |> dispatch

            UpdateAllCells = fun rmv e -> rmv(); triggerUpdateColumnModal e
            GetTermDetails = fun oa rmv e -> rmv(); triggerTermModal oa e
            EditColumn      = fun rmv e -> rmv(); editColumnEvent e
            RowIndex        = snd index
            ColumnIndex     = fst index
        }
        let child = contextmenu mousePosition funcs cell header
        let name = $"context_{mousePosition}"
        Modals.Controller.renderModal(name, child)

module DataMap =
    type private ContextFunctions = {
        FillColumn      : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        Clear           : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        //EditColumn      : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        //
        Copy            : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        Cut             : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        Paste           : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        PasteAll        : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        //
        DeleteRow       : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        RowIndex        : int
        ColumnIndex     : int
    }

    let private contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (contextCell: CompositeCell option) (rmv: unit -> unit) =
        /// This element will remove the contextmenu when clicking anywhere else
        let isHeader = Shared.isHeader funcs.RowIndex
        let buttonList = [
            if not isHeader then
                Components.BaseContextMenu.Item (Html.span "Fill Column", funcs.FillColumn rmv, "fa-solid fa-pen")
                Components.BaseContextMenu.Item (Html.span "Clear", funcs.Clear rmv, "fa-solid fa-eraser")
                Components.BaseContextMenu.Divider()
                Components.BaseContextMenu.Item (Html.span "Copy", funcs.Copy rmv, "fa-solid fa-copy")
                Components.BaseContextMenu.Item (Html.span "Cut", funcs.Cut rmv, "fa-solid fa-scissors")
                Components.BaseContextMenu.Item (Html.span "Paste", funcs.Paste rmv, "fa-solid fa-paste")
                Components.BaseContextMenu.Item (Html.span "Paste All", funcs.PasteAll rmv, "fa-solid fa-paste")
                Components.BaseContextMenu.Divider()
                Components.BaseContextMenu.Item (Html.span "Delete Row", funcs.DeleteRow rmv, "fa-solid fa-delete-left")
        ]
        Components.BaseContextMenu.Main(mousex, mousey, rmv, buttonList)

    open Shared

    let onContextMenu (index: int*int, model: Model, dispatch) = fun (e: Browser.Types.MouseEvent) ->
        e.stopPropagation()
        e.preventDefault()
        let mousePosition = int e.pageX, int e.pageY
        /// if there are selected cells in the same column as the clicked event, delete all selected rows.
        let deleteRowEvent _ =
            let s = Set.toArray model.SpreadsheetModel.SelectedCells
            if Array.isEmpty s |> not && Array.forall (fun (c,r) -> c = fst index) s && Array.contains index s then
                let indexArr = s |> Array.map snd |> Array.distinct
                Spreadsheet.DeleteRows indexArr |> Messages.SpreadsheetMsg |> dispatch
            else
                Spreadsheet.DeleteRow (snd index) |> Messages.SpreadsheetMsg |> dispatch
        let cell = model.SpreadsheetModel.ActiveTable.TryGetCellAt(fst index, snd index)
        let isSelectedCell = model.SpreadsheetModel.SelectedCells.Contains index
        //let editColumnEvent _ = Modals.Controller.renderModal("EditColumn_Modal", Modals.EditColumn.Main (fst index) model dispatch)
        let triggerMoveColumnModal _ = Modals.Controller.renderModal("MoveColumn_Modal", Modals.MoveColumn.Main(fst index, model, dispatch))
        let triggerUpdateColumnModal _ =
            let columnIndex = fst index
            let column = model.SpreadsheetModel.ActiveTable.GetColumn columnIndex
            Modals.Controller.renderModal("UpdateColumn_Modal", Modals.UpdateColumn.Main(fst index, column, dispatch))
        let funcs = {
            FillColumn      = fun rmv e -> rmv (); Spreadsheet.FillColumnWithTerm index |> Messages.SpreadsheetMsg |> dispatch
            DeleteRow       = fun rmv e -> rmv (); deleteRowEvent e
            Clear           = fun rmv e -> rmv (); if isSelectedCell then Spreadsheet.ClearSelected |> Messages.SpreadsheetMsg |> dispatch else Spreadsheet.Clear [|index|] |> Messages.SpreadsheetMsg |> dispatch
            Copy            = fun rmv e ->
                rmv ();
                if isSelectedCell then
                    Spreadsheet.CopySelectedCells |> Messages.SpreadsheetMsg |> dispatch
                else
                    Spreadsheet.CopyCell index |> Messages.SpreadsheetMsg |> dispatch
            Cut             = fun rmv e -> rmv(); Spreadsheet.CutCell index |> Messages.SpreadsheetMsg |> dispatch
            Paste           = fun rmv e ->
                rmv ();
                if isSelectedCell then
                    Spreadsheet.PasteSelectedCells |> Messages.SpreadsheetMsg |> dispatch
                else
                    Spreadsheet.PasteCell index |> Messages.SpreadsheetMsg |> dispatch
            PasteAll        = fun rmv e ->
                rmv ();
                Spreadsheet.PasteCellsExtend index |> Messages.SpreadsheetMsg |> dispatch
            //EditColumn      = fun rmv e -> rmv e; editColumnEvent e
            RowIndex        = snd index
            ColumnIndex     = fst index
        }
        let isHeader = Shared.isHeader funcs.RowIndex
        if not isHeader then
            let child = contextmenu mousePosition funcs cell
            let name = $"context_{mousePosition}"
            Modals.Controller.renderModal(name, child)