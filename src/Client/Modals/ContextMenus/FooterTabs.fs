namespace Modals.ContextMenus

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components.Shared

type FooterTabs =
    static member Table(mouseX, mouseY, tableIndex: int, startEdit: _ -> unit, dispatch: Messages.Msg -> unit) =
        let delete =
            fun _ -> Spreadsheet.RemoveTable tableIndex |> Messages.SpreadsheetMsg |> dispatch

        let rename = fun _ -> startEdit ()

        let children (rmv: unit -> unit) : ReactElement seq = [
            Modals.ContextMenus.Base.Item(Html.span "Delete", delete >> rmv, "fa-solid fa-trash")
            Modals.ContextMenus.Base.Item(Html.span "Rename", rename >> rmv, "fa-solid fa-pen-to-square")
        ]

        let mouseY = mouseY - 30
        Base.Main(mouseX, mouseY, children, dispatch)


    static member Plus(mouseX, mouseY, dispatch: Messages.Msg -> unit) =
        let addTable =
            fun _ ->
                SpreadsheetInterface.CreateAnnotationTable false
                |> Messages.InterfaceMsg
                |> dispatch

        let addDataMap =
            fun _ ->
                SpreadsheetInterface.UpdateDatamap(DataMap.init () |> Some)
                |> Messages.InterfaceMsg
                |> dispatch

        let children (rmv: unit -> unit) : ReactElement seq = [
            Modals.ContextMenus.Base.Item("Add Table", addTable >> rmv, "fa-solid fa-table")
            Modals.ContextMenus.Base.Item("Add Datamap", addDataMap >> rmv, "fa-solid fa-map")
        ]

        let mouseY = mouseY - 30
        Base.Main(mouseX, mouseY, children, dispatch)

    static member DataMap(mouseX, mouseY, dispatch: Messages.Msg -> unit) =
        let delete =
            fun _ -> SpreadsheetInterface.UpdateDatamap None |> Messages.InterfaceMsg |> dispatch

        let children (rmv: unit -> unit) : ReactElement seq = [
            Modals.ContextMenus.Base.Item("Delete", delete >> rmv, "fa-solid fa-trash")
        ]

        let mouseY = mouseY - 10
        Base.Main(mouseX, mouseY, children, dispatch)