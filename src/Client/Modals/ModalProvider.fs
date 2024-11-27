namespace Modals

open Feliz
open Feliz.DaisyUI

open Model.ModalState

type ModalProvider =

    static member ExcelModal(modal: ExcelModals, model: Model.Model, dispatch) =
        match modal with
        | ExcelModals.InteropLogging ->
            Modals.InteropLogging.Main (model.DevState, dispatch)

    static member TableModal (modal: TableModals, model, dispatch) =
        match modal with
        | TableModals.EditColumn columnIndex ->
            Modals.EditColumn.Main columnIndex model dispatch
        | TableModals.MoveColumn columnIndex ->
            Modals.MoveColumn.Main (columnIndex, model, dispatch)
        | TableModals.ResetTable ->
            Modals.ResetTable.Main dispatch
        | TableModals.TermDetails term ->
            Modals.TermModal.Main (term, dispatch)
        | TableModals.SelectiveFileImport arcfile ->
            Modals.SelectiveImportModal.Main (arcfile, dispatch)
        | TableModals.BatchUpdateColumnValues (columnIndex, column) ->
            Modals.UpdateColumn.Main (columnIndex, column, dispatch)
        | TableModals.TableCellContext (mouseX, mouseY, ci, ri) ->
            Modals.ContextMenus.TableCell.Main(mouseX, mouseY, ci, ri, model, dispatch)
        | TableModals.DataMapCellContext (mouseX, mouseY, ci, ri) ->
            Modals.ContextMenus.DataMapCell.Main(mouseX, mouseY, ci, ri, model, dispatch)


    static member GeneralModal (modal: GeneralModals, model, dispatch) =
        match modal with
        | GeneralModals.Error exn ->
            Modals.Error.Main (exn, dispatch)
        | GeneralModals.Warning msg ->
            Modals.Warning.Main (msg, dispatch)
        | GeneralModals.Loading ->
            Modals.Loading.Modal (dispatch)


    [<ReactComponent>]
    static member Main (model: Model.Model, dispatch) =
        Html.div [
            prop.id "modal-provider"
            prop.children [
                match model.ModalState with
                | {ActiveModal = None } ->
                    Html.none
                | {ActiveModal = Some modal } ->
                    match modal with
                    | GeneralModal m ->
                        ModalProvider.GeneralModal(m, model, dispatch)
                    | TableModal m ->
                        ModalProvider.TableModal(m, model, dispatch)
                    | ExcelModal m ->
                        ModalProvider.ExcelModal(m, model, dispatch)
                    | Force ele ->
                        ele
            ]
        ]