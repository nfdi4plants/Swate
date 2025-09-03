namespace Modals.ContextMenus

open ARCtrl
open Feliz
open Feliz.DaisyUI

open Swate.Components
open Swate.Components.Shared

// type TableCell =
//     static member Main(mouseX, mouseY, ci: int, ri: int, model: Model.Model, dispatch: Messages.Msg -> unit) =
//         let index: CellCoordinate = {| x = ci; y = ri |}
//         let cell = model.SpreadsheetModel.ActiveTable.TryGetCellAt(ci, ri)
//         let header = model.SpreadsheetModel.ActiveTable.Headers.[ci]
//         let isSelectedCell = CellCoordinateRange.contains model.SpreadsheetModel.SelectedCells index
//         let isHeader = Util.isHeader ri
//         let isUnitOrTermCell = Util.isUnitOrTermCell cell
//         let deleteRow = Util.deleteRow index model dispatch

//         let editColumnModal =
//             fun _ ->
//                 Model.ModalState.TableModals.EditColumn(index.x)
//                 |> Model.ModalState.ModalTypes.TableModal
//                 |> Some
//                 |> Messages.UpdateModal
//                 |> dispatch

//         let triggerMoveColumnModal =
//             fun _ ->
//                 Model.ModalState.TableModals.MoveColumn(index.x)
//                 |> Model.ModalState.ModalTypes.TableModal
//                 |> Some
//                 |> Messages.UpdateModal
//                 |> dispatch

//         let triggerUpdateColumnModal =
//             fun _ ->
//                 let columnIndex = index.x
//                 let column = model.SpreadsheetModel.ActiveTable.GetColumn columnIndex

//                 Model.ModalState.TableModals.BatchUpdateColumnValues(columnIndex, column)
//                 |> Model.ModalState.ModalTypes.TableModal
//                 |> Some
//                 |> Messages.UpdateModal
//                 |> dispatch

//         let triggerTermModal oa _ =
//             Model.ModalState.TableModals.TermDetails(oa)
//             |> Model.ModalState.ModalTypes.TableModal
//             |> Some
//             |> Messages.UpdateModal
//             |> dispatch

//         let transFormCell =
//             fun _ ->
//                 if cell.IsSome && (cell.Value.isTerm || cell.Value.isUnitized) then
//                     let nextCell =
//                         if cell.Value.isTerm then
//                             cell.Value.ToUnitizedCell()
//                         else
//                             cell.Value.ToTermCell()

//                     Spreadsheet.UpdateCell(index, nextCell) |> Messages.SpreadsheetMsg |> dispatch
//                 elif cell.IsSome && header.IsDataColumn then
//                     let nextCell =
//                         if cell.Value.isFreeText then
//                             cell.Value.ToDataCell()
//                         else
//                             cell.Value.ToFreeTextCell()

//                     Spreadsheet.UpdateCell(index, nextCell) |> Messages.SpreadsheetMsg |> dispatch

//         let clear = Util.clear isSelectedCell index dispatch
//         let copy = Util.copy isSelectedCell index dispatch
//         let cut = Util.cut index dispatch
//         let paste = Util.paste isSelectedCell index dispatch
//         let pasteAll = Util.pasteAll index dispatch
//         let fillColumn = Util.fillColumn index dispatch

//         let deleteColumn =
//             fun _ -> Spreadsheet.DeleteColumn(ci) |> Messages.SpreadsheetMsg |> dispatch

//         let children (rmv: unit -> unit) : ReactElement seq = [
//             Modals.ContextMenus.Base.Item("Edit Column", editColumnModal, Icons.TableColumn())
//             if (isHeader && header.IsTermColumn) || isUnitOrTermCell then
//                 let oa = if isHeader then header.ToTerm() else cell.Value.ToOA()

//                 if oa.TermAccessionShort <> "" then
//                     Modals.ContextMenus.Base.Item("Details", triggerTermModal oa, Icons.MagnifyingClass())
//             if not isHeader then
//                 Modals.ContextMenus.Base.Item("Fill Column", fillColumn >> rmv, Icons.Pen())

//                 if isUnitOrTermCell then
//                     let text = if cell.Value.isTerm then "As Unit Cell" else "As Term Cell"
//                     Modals.ContextMenus.Base.Item(text, transFormCell >> rmv, Icons.ArrorRightLeft())
//                 elif header.IsDataColumn then
//                     let text =
//                         if cell.Value.isFreeText then
//                             "As Data Cell"
//                         else
//                             "As Free Text Cell"

//                     Modals.ContextMenus.Base.Item(text, transFormCell >> rmv, Icons.ArrorRightLeft())

//                 if not isUnitOrTermCell then
//                     Modals.ContextMenus.Base.Item(
//                         "Update Column",
//                         triggerUpdateColumnModal,
//                         Icons.EllipsisVertical()
//                     )

//                 Modals.ContextMenus.Base.Item("Clear", clear >> rmv, Icons.Eraser())
//                 Modals.ContextMenus.Base.Divider()
//                 Modals.ContextMenus.Base.Item("Copy", copy >> rmv, Icons.Copy())
//                 Modals.ContextMenus.Base.Item("Cut", cut >> rmv, Icons.Scissor())
//                 Modals.ContextMenus.Base.Item("Paste", paste >> rmv, Icons.Paste())
//                 Modals.ContextMenus.Base.Item("Paste All", pasteAll >> rmv, Icons.Paste())
//                 Modals.ContextMenus.Base.Divider()
//                 Modals.ContextMenus.Base.Item("Delete Row", deleteRow >> rmv, Icons.DeleteLeft())
//             Modals.ContextMenus.Base.Item("Delete Column", deleteColumn >> rmv, Icons.DeleteDown())
//             Modals.ContextMenus.Base.Item("Move Column", triggerMoveColumnModal, Icons.ArrorRightLeft())
//         ]

//         Base.Main(mouseX, mouseY, children, dispatch)