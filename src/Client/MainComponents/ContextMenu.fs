module MainComponents.ContextMenu

open Feliz
open Feliz.DaisyUI
open Spreadsheet
open ARCtrl
open Model
open Swate.Components.Shared

module Table =

    let onContextMenu (index: int * int, dispatch) =
        fun (e: Browser.Types.MouseEvent) ->
            e.stopPropagation ()
            e.preventDefault ()
            let ci, ri = index
            let mouseX, mouseY = int e.pageX, int e.pageY

            Model.ModalState.TableModals.TableCellContext(mouseX, mouseY, ci, ri)
            |> Model.ModalState.ModalTypes.TableModal
            |> Some
            |> Messages.UpdateModal
            |> dispatch

module DataMap =

    let onContextMenu (index: int * int, dispatch) =
        fun (e: Browser.Types.MouseEvent) ->
            e.stopPropagation ()
            e.preventDefault ()
            let ci, ri = index
            let mouseX, mouseY = int e.pageX, int e.pageY
            let isHeader = Modals.ContextMenus.Util.isHeader ri

            if not isHeader then
                Model.ModalState.TableModals.DataMapCellContext(mouseX, mouseY, ci, ri)
                |> Model.ModalState.ModalTypes.TableModal
                |> Some
                |> Messages.UpdateModal
                |> dispatch