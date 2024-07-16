module MainComponents.SpreadsheetView

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Spreadsheet.Cells
open ARCtrl
open Shared
open Model

let private BodyRow (rowIndex: int) (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
    let table = model.SpreadsheetModel.ActiveTable
    Html.tr [
        CellStyles.RowLabel rowIndex
        for columnIndex in 0 .. (table.ColumnCount-1) do
            let index = columnIndex, rowIndex
            let cell = model.SpreadsheetModel.ActiveTable.Values.[index]
            Cells.Cell.Body (index, cell, model, dispatch)
            let isExtended = state.Contains columnIndex
            let header = table.Headers.[columnIndex]
            if (cell.isTerm || cell.isUnitized) && isExtended then
                if cell.isUnitized then 
                    Cell.BodyUnit(index, cell, model, dispatch)
                else
                    Cell.Empty()
                Cell.BodyTSR(index, cell, model, dispatch)
                Cell.BodyTAN(index, cell, model, dispatch)
            elif header.IsDataColumn then
                if cell.isData then
                    Cell.BodyDataSelector(index, cell, model, dispatch)
                    Cell.BodyDataFormat(index, cell, model, dispatch)
                    Cell.BodyDataSelectorFormat(index, cell, model, dispatch)
                else
                    Cell.Empty()
                    Cell.Empty()
                    Cell.Empty()
    ]

let private BodyRows (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
    Html.tbody [
        for rowInd in 0 .. model.SpreadsheetModel.ActiveTable.RowCount-1 do
            yield BodyRow rowInd state model dispatch 
    ]
    

let private HeaderRow (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    let table = model.SpreadsheetModel.ActiveTable
    Html.tr [
        if table.ColumnCount > 0 then CellStyles.RowLabel -1
        for columnIndex in 0 .. (table.ColumnCount-1) do
            let header = table.Headers.[columnIndex]
            Cells.Cell.Header(columnIndex, header, state, setState, model, dispatch)
            if header.IsTermColumn then
                let isExtended = state.Contains columnIndex
                if isExtended then
                    Cell.HeaderUnit(columnIndex, header, state, setState, model, dispatch)
                    Cell.HeaderTSR(columnIndex, header, state, setState, model, dispatch)
                    Cell.HeaderTAN(columnIndex, header, state, setState, model, dispatch)
            elif header.IsDataColumn then
                Cell.HeaderDataSelector(columnIndex, header, state, setState, model, dispatch)
                Cell.HeaderDataFormat(columnIndex, header, state, setState, model, dispatch)
                Cell.HeaderDataSelectorFormat(columnIndex, header, state, setState, model, dispatch)
            else
                ()
                
    ]

open Fable.Core.JsInterop

[<ReactComponent>]
let Main (model:Model) (dispatch: Msg -> unit) =
    //React.useListener.on("keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    let ref = React.useElementRef()
    //React.useElementListener.on(ref, "keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    /// This state is used to track which columns are expanded
    let state, setState : Set<int> * (Set<int> -> unit) = React.useState(Set.empty)
    React.useEffect((fun _ -> setState Set.empty), [|box model.SpreadsheetModel.ActiveView|])
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
                        HeaderRow state setState model dispatch
                    ]
                    BodyRows state model dispatch
                ]
            ]
        ]
    ]