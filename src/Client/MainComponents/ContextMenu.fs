module MainComponents.ContextMenu

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages


type private ContextFunctions = {
    DeleteRow       : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    DeleteColumn    : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Copy            : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Cut             : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Paste           : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    FillColumn      : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    EditColumn      : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    RowIndex        : int
    ColumnIndex     : int
}

let private contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (rmv: _ -> unit) =
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
            prop.style [style.borderRadius 0; style.justifyContent.spaceBetween]
            prop.onClick msg
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
        Html.div [ prop.style [style.border(2, borderStyle.solid, NFDIColors.DarkBlue.Base); style.margin(2,0)] ]
    ]
    let isHeaderRow = funcs.RowIndex = 0
    let buttonList = [
        button ("Edit Column", "fa-solid fa-table-columns", funcs.EditColumn rmv, [])
        button ("Fill Column", "fa-solid fa-file-signature", funcs.FillColumn rmv, [prop.disabled isHeaderRow])
        divider
        button ("Copy", "fa-solid fa-copy", funcs.Copy rmv, [])
        button ("Cut", "fa-solid fa-scissors", funcs.Cut rmv, [])
        button ("Paste", "fa-solid fa-paste",  funcs.Paste rmv, [prop.disabled Spreadsheet.Table.Controller.clipboardCell.IsNone])
        divider
        button ("Delete Row", "fa-solid fa-delete-left", funcs.DeleteRow rmv, [prop.disabled isHeaderRow])
        button ("Delete Column", "fa-solid fa-delete-left fa-rotate-270", funcs.DeleteColumn rmv, [])
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
    let editColumnEvent _ = Modals.Controller.renderModal("EditColumn_Modal", Modals.EditColumn.Main (fst index) model dispatch)
    let funcs = {
        DeleteRow       = fun rmv e -> rmv e; deleteRowEvent e
        DeleteColumn    = fun rmv e -> rmv e; Spreadsheet.DeleteColumn (fst index) |> Messages.SpreadsheetMsg |> dispatch
        Copy            = fun rmv e -> rmv e; Spreadsheet.CopyCell index |> Messages.SpreadsheetMsg |> dispatch
        Cut             = fun rmv e -> rmv e; Spreadsheet.CutCell index |> Messages.SpreadsheetMsg |> dispatch
        Paste           = fun rmv e -> rmv e; Spreadsheet.PasteCell index |> Messages.SpreadsheetMsg |> dispatch
        FillColumn      = fun rmv e -> rmv e; Spreadsheet.FillColumnWithTerm index |> Messages.SpreadsheetMsg |> dispatch
        EditColumn      = fun rmv e -> rmv e; editColumnEvent e
        RowIndex        = snd index
        ColumnIndex     = fst index
    }
    let child = contextmenu mousePosition funcs
    let name = $"context_{mousePosition}"
    Modals.Controller.renderModal(name, child)