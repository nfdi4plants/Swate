namespace Modals.ContextMenus

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components.Shared

type DataMapCell =
    static member Main(mouseX, mouseY, ci: int, ri: int, model: Model.Model, dispatch: Messages.Msg -> unit) =
        let index = ci, ri
        let isSelectedCell = model.SpreadsheetModel.SelectedCells.Contains index
        let isHeader = Util.isHeader ri
        let fillColumn = Util.fillColumn index dispatch
        let clear = Util.clear isSelectedCell index dispatch
        let copy = Util.copy isSelectedCell index dispatch
        let cut = Util.cut index dispatch
        let paste = Util.paste isSelectedCell index dispatch
        let pasteAll = Util.pasteAll index dispatch
        let deleteRow = Util.deleteRow index model dispatch

        let children (rmv: unit -> unit) : ReactElement seq = [
            if isHeader then
                Modals.ContextMenus.Base.Item(Html.span "No Actions available")
            else
                Modals.ContextMenus.Base.Item(Html.span "Fill Column", fillColumn >> rmv, "fa-solid fa-pen")
                Modals.ContextMenus.Base.Item(Html.span "Clear", clear >> rmv, "fa-solid fa-eraser")
                Modals.ContextMenus.Base.Divider()
                Modals.ContextMenus.Base.Item(Html.span "Copy", copy >> rmv, "fa-solid fa-copy")
                Modals.ContextMenus.Base.Item(Html.span "Cut", cut >> rmv, "fa-solid fa-scissors")
                Modals.ContextMenus.Base.Item(Html.span "Paste", paste >> rmv, "fa-solid fa-paste")
                Modals.ContextMenus.Base.Item(Html.span "Paste All", pasteAll >> rmv, "fa-solid fa-paste")
                Modals.ContextMenus.Base.Divider()
                Modals.ContextMenus.Base.Item(Html.span "Delete Row", deleteRow >> rmv, "fa-solid fa-delete-left")
        ]

        Base.Main(mouseX, mouseY, children, dispatch)