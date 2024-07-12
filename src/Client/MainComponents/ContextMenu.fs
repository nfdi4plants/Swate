module MainComponents.ContextMenu

open Feliz
open Feliz.Bulma
open Spreadsheet
open ARCtrl
open Model

module Table =
    type private ContextFunctions = {
        DeleteRow       : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        DeleteColumn    : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        MoveColumn      : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        Copy            : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        Cut             : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        Paste           : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        PasteAll        : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        FillColumn      : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        Clear           : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        TransformCell   : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        UpdateAllCells  : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        //EditColumn      : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        RowIndex        : int
        ColumnIndex     : int
    }

    let private isUnitOrTermCell (cell: CompositeCell option) =
        cell.IsSome && not cell.Value.isFreeText

    let private isHeader (rowIndex: int) = rowIndex < 0

    let private contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (contextCell: CompositeCell option) (rmv: _ -> unit) =
        /// This element will remove the contextmenu when clicking anywhere else
        let isHeader = isHeader funcs.RowIndex
        let rmv_element = Html.div [
            prop.onClick rmv
            prop.onContextMenu(fun e -> e.preventDefault(); rmv e)
            prop.style [
                style.position.fixedRelativeToWindow
                style.backgroundColor.transparent
                style.left 0
                style.top 0
                style.right 0
                style.bottom 0
                style.display.block
            ]
        ]
        let button (name:string, icon: string, msg, props) = Html.li [
            Bulma.button.button [
                prop.style [style.borderRadius 0; style.justifyContent.spaceBetween; style.fontSize (length.rem 0.9)]
                prop.onClick msg
                prop.className "py-1"
                Bulma.button.isFullWidth
                //Bulma.button.isSmall
                Bulma.color.isBlack
                Bulma.button.isInverted
                yield! props
                prop.children [
                    Bulma.icon [Html.i [prop.className icon]]
                    Html.span name
                ]
            ]
        ]
        let divider = Html.li [
            Html.div [ prop.style [style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base); style.margin(2,0); style.width (length.perc 75); style.marginLeft length.auto] ]
        ]
        let buttonList = [
            //button ("Edit Column", "fa-solid fa-table-columns", funcs.EditColumn rmv, [])
            if not isHeader then
                button ("Fill Column", "fa-solid fa-pen", funcs.FillColumn rmv, [])
                if isUnitOrTermCell contextCell then
                    let text = if contextCell.Value.isTerm then "As Unit Cell" else "As Term Cell"
                    button (text, "fa-solid fa-arrow-right-arrow-left", funcs.TransformCell rmv, [])
                else
                    button ("Update Column", "fa-solid fa-ellipsis-vertical", funcs.UpdateAllCells rmv, [])
                button ("Clear", "fa-solid fa-eraser", funcs.Clear rmv, [])
                divider
                button ("Copy", "fa-solid fa-copy", funcs.Copy rmv, [])
                button ("Cut", "fa-solid fa-scissors", funcs.Cut rmv, [])
                button ("Paste", "fa-solid fa-paste",  funcs.Paste rmv, [])
                button ("Paste All", "fa-solid fa-paste",  funcs.PasteAll rmv, [])
                divider
                button ("Delete Row", "fa-solid fa-delete-left", funcs.DeleteRow rmv, [])
            button ("Delete Column", "fa-solid fa-delete-left fa-rotate-270", funcs.DeleteColumn rmv, [])
            button ("Move Column", "fa-solid fa-arrow-right-arrow-left", funcs.MoveColumn rmv, [])
        ]
        Html.div [
            prop.style [
                style.backgroundColor "white"
                style.position.absolute
                style.left mousex
                style.top (mousey - 40)
                style.width 150
                style.zIndex 40 // to overlap navbar
                style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base)
            ]
            prop.children [
                rmv_element
                Html.ul buttonList
            ]
        ]

    open Shared

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
        let isSelectedCell = model.SpreadsheetModel.SelectedCells.Contains index
        //let editColumnEvent _ = Modals.Controller.renderModal("EditColumn_Modal", Modals.EditColumn.Main (fst index) model dispatch)
        let triggerMoveColumnModal _ = Modals.Controller.renderModal("MoveColumn_Modal", Modals.MoveColumn.Main(ci, model, dispatch))
        let triggerUpdateColumnModal _ = 
            let columnIndex = fst index
            let column = model.SpreadsheetModel.ActiveTable.GetColumn columnIndex
            Modals.Controller.renderModal("UpdateColumn_Modal", Modals.UpdateColumn.Main(ci, column, dispatch))
        let funcs = {
            DeleteRow       = fun rmv e -> rmv e; deleteRowEvent e
            DeleteColumn    = fun rmv e -> rmv e; Spreadsheet.DeleteColumn (ci) |> Messages.SpreadsheetMsg |> dispatch
            MoveColumn      = fun rmv e -> rmv e; triggerMoveColumnModal e
            Copy            = fun rmv e -> 
                rmv e; 
                if isSelectedCell then
                    Spreadsheet.CopySelectedCells |> Messages.SpreadsheetMsg |> dispatch
                else
                    Spreadsheet.CopyCell index |> Messages.SpreadsheetMsg |> dispatch
            Cut             = fun rmv e -> rmv e; Spreadsheet.CutCell index |> Messages.SpreadsheetMsg |> dispatch
            Paste           = fun rmv e -> 
                rmv e; 
                if isSelectedCell then
                    Spreadsheet.PasteSelectedCells |> Messages.SpreadsheetMsg |> dispatch
                else
                    Spreadsheet.PasteCell index |> Messages.SpreadsheetMsg |> dispatch
            PasteAll        = fun rmv e ->
                rmv e;
                Spreadsheet.PasteCellsExtend index |> Messages.SpreadsheetMsg |> dispatch
            FillColumn      = fun rmv e -> rmv e; Spreadsheet.FillColumnWithTerm index |> Messages.SpreadsheetMsg |> dispatch
            Clear           = fun rmv e -> rmv e; if isSelectedCell then Spreadsheet.ClearSelected |> Messages.SpreadsheetMsg |> dispatch else Spreadsheet.Clear [|index|] |> Messages.SpreadsheetMsg |> dispatch
            TransformCell   = fun rmv e -> 
                if cell.IsSome && (cell.Value.isTerm || cell.Value.isUnitized) then
                    let nextCell = if cell.Value.isTerm then cell.Value.ToUnitizedCell() else cell.Value.ToTermCell()
                    rmv e; Spreadsheet.UpdateCell (index, nextCell) |> Messages.SpreadsheetMsg |> dispatch
            UpdateAllCells = fun rmv e -> rmv e; triggerUpdateColumnModal e
            //EditColumn      = fun rmv e -> rmv e; editColumnEvent e
            RowIndex        = snd index
            ColumnIndex     = fst index
        }
        let child = contextmenu mousePosition funcs cell
        let name = $"context_{mousePosition}"
        Modals.Controller.renderModal(name, child)

module DataMap =
    type private ContextFunctions = {
        DeleteRow       : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        //EditColumn      : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        RowIndex        : int
        ColumnIndex     : int
    }

    let private contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (contextCell: CompositeCell option) (rmv: _ -> unit) =
        /// This element will remove the contextmenu when clicking anywhere else
        let rmv_element = Html.div [
            prop.onClick rmv
            prop.onContextMenu(fun e -> e.preventDefault(); rmv e)
            prop.style [
                style.position.fixedRelativeToWindow
                style.backgroundColor.transparent
                style.left 0
                style.top 0
                style.right 0
                style.bottom 0
                style.display.block
            ]
        ]
        let button (name:string, icon: string, msg, props) = Html.li [
            Bulma.button.button [
                prop.style [style.borderRadius 0; style.justifyContent.spaceBetween; style.fontSize (length.rem 0.9)]
                prop.onClick msg
                prop.className "py-1"
                Bulma.button.isFullWidth
                //Bulma.button.isSmall
                Bulma.color.isBlack
                Bulma.button.isInverted
                yield! props
                prop.children [
                    Bulma.icon [Html.i [prop.className icon]]
                    Html.span name
                ]
            ]
        ]
        let divider = Html.li [
            Html.div [ prop.style [style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base); style.margin(2,0); style.width (length.perc 75); style.marginLeft length.auto] ]
        ]
        let buttonList = [
            button ("Delete Row", "fa-solid fa-delete-left", funcs.DeleteRow rmv, [])
        ]
        Html.div [
            prop.style [
                style.backgroundColor "white"
                style.position.absolute
                style.left mousex
                style.top (mousey - 40)
                style.width 150
                style.zIndex 40 // to overlap navbar
                style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base)
            ]
            prop.children [
                rmv_element
                Html.ul buttonList
            ]
        ]

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
            DeleteRow       = fun rmv e -> rmv e; deleteRowEvent e
            //EditColumn      = fun rmv e -> rmv e; editColumnEvent e
            RowIndex        = snd index
            ColumnIndex     = fst index
        }
        let child = contextmenu mousePosition funcs cell
        let name = $"context_{mousePosition}"
        Modals.Controller.renderModal(name, child)