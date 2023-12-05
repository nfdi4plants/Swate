module MainComponents.SpreadsheetView

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Spreadsheet.Cells
open ARCtrl.ISA
open Shared


//let private referenceColumns (state:Set<int>, header:SwateCell, (columnIndex: int, rowIndex:int), model, dispatch) =
//    if header.Header.isTermColumn then
//        [
//            let isExtended = state.Contains(columnIndex)
//            if isExtended then
//                if header.Header.HasUnit then
//                    yield UnitCell((columnIndex,rowIndex), model, dispatch)
//                yield TANCell((columnIndex,rowIndex), model, dispatch)
//        ]
//    else []

let private bodyRow (rowIndex: int) (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    let table = model.SpreadsheetModel.ActiveTable
    Html.tr [
        for columnIndex in 0 .. (table.ColumnCount-1) do
            let c_opt = table.TryGetCellAt(columnIndex,rowIndex)
            match c_opt with
            | Some c ->
                yield
                    Html.td [
                        prop.text (c.GetContent().[0])
                    ]
            | None ->
                yield Html.td Html.none
                //Cell((columnIndex,rowIndex), state, setState, model, dispatch)
            //yield! referenceColumns(state, header, (column,row), model, dispatch)
    ]

let private bodyRows (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    Html.tbody [
        for rowInd in 0 .. model.SpreadsheetModel.ActiveTable.RowCount do
            yield bodyRow rowInd state setState model dispatch 
    ]
    

let private headerRow (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    let table = model.SpreadsheetModel.ActiveTable
    let rowIndex = 0
    Html.tr [
        for columnIndex in 0 .. (table.ColumnCount-1) do
            yield
                //Cell((columnIndex, rowIndex), state, setState, model, dispatch)
                Html.th [
                    prop.text (table.Headers.[columnIndex].ToString())
                ]
            //yield! referenceColumns(state, cell, (column,row), model, dispatch)
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