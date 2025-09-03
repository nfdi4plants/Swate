namespace Modals.ContextMenus

// open ARCtrl
// open Feliz
// open Feliz.DaisyUI
// open Swate.Components
// open Swate.Components.Shared

// type DataMapCell =
//     static member Main(mouseX, mouseY, ci: int, ri: int, model: Model.Model, dispatch: Messages.Msg -> unit) =
//         let index: CellCoordinate =
//             {|
//                 x = ci
//                 y = ri
//             |}
//         let isSelectedCell = CellCoordinateRange.contains model.SpreadsheetModel.SelectedCells index
//         let isHeader = Util.isHeader ri
//         let fillColumn = Util.fillColumn index dispatch
//         let clear = Util.clear isSelectedCell index dispatch
//         let copy = Util.copy isSelectedCell index dispatch
//         let cut = Util.cut index dispatch
//         let paste = Util.paste isSelectedCell index dispatch
//         let pasteAll = Util.pasteAll index dispatch
//         let deleteRow = Util.deleteRow index model dispatch

//         let children (rmv: unit -> unit) : ReactElement seq = [
//             if isHeader then
//                 Modals.ContextMenus.Base.Item(Html.span "No Actions available")
//             else
//                 Modals.ContextMenus.Base.Item(Html.span "Fill Column", fillColumn >> rmv, Icons.Pen())
//                 Modals.ContextMenus.Base.Item(Html.span "Clear", clear >> rmv, Icons.Eraser())
//                 Modals.ContextMenus.Base.Divider()
//                 Modals.ContextMenus.Base.Item(Html.span "Copy", copy >> rmv, Icons.Copy())
//                 Modals.ContextMenus.Base.Item(Html.span "Cut", cut >> rmv, Icons.Scissor())
//                 Modals.ContextMenus.Base.Item(Html.span "Paste", paste >> rmv, Icons.Paste())
//                 Modals.ContextMenus.Base.Item(Html.span "Paste All", pasteAll >> rmv, Icons.Paste())
//                 Modals.ContextMenus.Base.Divider()
//                 Modals.ContextMenus.Base.Item(Html.span "Delete Row", deleteRow >> rmv, Icons.DeleteLeft())
//         ]

//         Base.Main(mouseX, mouseY, children, dispatch)