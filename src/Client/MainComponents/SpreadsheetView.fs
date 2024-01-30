module MainComponents.SpreadsheetView

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Spreadsheet.Cells
open ARCtrl.ISA
open Shared


let private referenceColumns (columnIndex, header: CompositeHeader, state: Set<int>, setState, model, dispatch) =
    if header.IsTermColumn then
        [
            let isExtended = state.Contains columnIndex
            if isExtended then
                Cell.HeaderUnit(columnIndex, header, state, setState, model, dispatch)
                Cell.HeaderTSR(columnIndex, header, state, setState, model, dispatch)
                Cell.HeaderTAN(columnIndex, header, state, setState, model, dispatch)
        ]
    else []

let cellPlaceholder (c_opt: CompositeCell option) =
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

let private bodyRow (rowIndex: int) (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    let table = model.SpreadsheetModel.ActiveTable
    Html.tr [
        for columnIndex in 0 .. (table.ColumnCount-1) do
            let index = columnIndex, rowIndex
            let state = model.SpreadsheetModel
            let cell = state.ActiveTable.Values.[index]
            Cells.Cell.Body (index, cell, model, dispatch)
                //Cell((columnIndex,rowIndex), state, setState, model, dispatch)
            //yield! referenceColumns(state, header, (column,row), model, dispatch)
    ]

let private bodyRows (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    Html.tbody [
        for rowInd in 0 .. model.SpreadsheetModel.ActiveTable.RowCount-1 do
            yield bodyRow rowInd state setState model dispatch 
    ]
    

let private headerRow (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    let table = model.SpreadsheetModel.ActiveTable
    Html.tr [
        for columnIndex in 0 .. (table.ColumnCount-1) do
            let header = table.Headers.[columnIndex]
            yield
                Cells.Cell.Header(columnIndex, header, state, setState, model, dispatch)
            yield! referenceColumns(columnIndex, header, state, setState, model, dispatch)
    ]

[<ReactComponent>]
let Main (model:Model) (dispatch: Msg -> unit) =
    /// This state is used to track which columns are expanded
    let state, setState : Set<int> * (Set<int> -> unit) = React.useState(Set.empty)
    Html.div [
        prop.style [style.border(1, borderStyle.solid, "grey"); style.width.minContent; style.marginRight(length.vw 10)]
        prop.children [
            Html.table [
                prop.className "fixed_headers"
                prop.children [
                    Html.thead [
                        headerRow state setState model dispatch
                    ]
                    bodyRows state setState model dispatch
                ]
            ]
        ]
    ]