module MainComponents.SpreadsheetView

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Spreadsheet.Cells
open ARCtrl
open Shared

let private cellPlaceholder (c_opt: CompositeCell option) =
    let tableCell (children: ReactElement list) = Html.td [
        Html.div [
            prop.style [style.minHeight (length.px 30); style.minWidth (length.px 100)]
            prop.children children
        ]
    ]
    Html.td [
        match c_opt with
        | Some c -> 
            tableCell [
                Html.span (c.GetContent().[0])
            ]
        | None ->
            tableCell [
                Html.span ""
            ]
    ]

/// <summary>
/// rowIndex < 0 equals header
/// </summary>
/// <param name="rowIndex"></param>
let private RowLabel (rowIndex: int) = 
    let t : IReactProperty list -> ReactElement = if rowIndex < 0 then Html.th else Html.td 
    t [
        //prop.style [style.resize.none; style.border(length.px 1, borderStyle.solid, "darkgrey")]
        //prop.children [
        //    Bulma.button.button [
        //        prop.className "px-2 py-1"
        //        prop.style [style.custom ("border", "unset"); style.borderRadius 0]
        //        Bulma.button.isFullWidth
        //        Bulma.button.isStatic
        //        prop.tabIndex -1
        //        prop.text (if rowIndex < 0 then "" else $"{rowIndex+1}")
        //    ]
        //]
        prop.style [style.resize.none; style.border(length.px 1, borderStyle.solid, "darkgrey"); style.height(length.perc 100)]
        prop.children [
            Html.div [
                prop.style [style.height(length.perc 100);]
                prop.className "is-flex is-justify-content-center is-align-items-center px-2 is-unselectable my-grey-out"
                prop.disabled true
                prop.children [
                    Html.b (if rowIndex < 0 then "" else $"{rowIndex+1}")
                ]
            ]
        ]
    ]

let private bodyRow (rowIndex: int) (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
    let table = model.SpreadsheetModel.ActiveTable
    Html.tr [
        RowLabel rowIndex
        for columnIndex in 0 .. (table.ColumnCount-1) do
            let index = columnIndex, rowIndex
            let cell = model.SpreadsheetModel.ActiveTable.Values.[index]
            Cells.Cell.Body (index, cell, model, dispatch)
            let isExtended = state.Contains columnIndex
            if isExtended && (cell.isTerm || cell.isUnitized) then
                if cell.isUnitized then 
                    Cell.BodyUnit(index, cell, model, dispatch)
                else
                    Cell.Empty()
                Cell.BodyTSR(index, cell, model, dispatch)
                Cell.BodyTAN(index, cell, model, dispatch)
    ]

let private bodyRows (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
    Html.tbody [
        for rowInd in 0 .. model.SpreadsheetModel.ActiveTable.RowCount-1 do
            yield bodyRow rowInd state model dispatch 
    ]
    

let private headerRow (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    let table = model.SpreadsheetModel.ActiveTable
    Html.tr [
        if table.ColumnCount > 0 then RowLabel -1
        for columnIndex in 0 .. (table.ColumnCount-1) do
            let header = table.Headers.[columnIndex]
            Cells.Cell.Header(columnIndex, header, state, setState, model, dispatch)
            let isExtended = state.Contains columnIndex
            if isExtended then
                Cell.HeaderUnit(columnIndex, header, state, setState, model, dispatch)
                Cell.HeaderTSR(columnIndex, header, state, setState, model, dispatch)
                Cell.HeaderTAN(columnIndex, header, state, setState, model, dispatch)
    ]

open Fable.Core.JsInterop

[<ReactComponent>]
let Main (model:Model) (dispatch: Msg -> unit) =
    //React.useListener.on("keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    let ref = React.useElementRef()
    //React.useElementListener.on(ref, "keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    /// This state is used to track which columns are expanded
    let state, setState : Set<int> * (Set<int> -> unit) = React.useState(Set.empty)
    Html.div [
        prop.id "SPREADSHEET_MAIN_VIEW"
        prop.tabIndex 0
        prop.style [style.border(1, borderStyle.solid, "grey"); style.width.minContent; style.marginRight(length.vw 10)]
        prop.ref ref
        prop.onKeyDown(fun e -> Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch e)
        prop.children [
            Html.table [
                prop.className "fixed_headers"
                prop.children [
                    Html.thead [
                        headerRow state setState model dispatch
                    ]
                    bodyRows state model dispatch
                ]
            ]
        ]
    ]