module MainComponents.SpreadsheetView

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Spreadsheet.Cells

let private referenceColumns (state:Set<int>, header:SwateCell, (columnIndex: int, rowIndex:int), model, dispatch) =
    if header.Header.isTermColumn then
        [
            let isExtended = state.Contains(columnIndex)
            if isExtended then
                if header.Header.HasUnit then
                    yield UnitCell((columnIndex,rowIndex), model, dispatch)
                yield TANCell((columnIndex,rowIndex), model, dispatch)
        ]
    else []

let private bodyRow (i:int) (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    let r = model.SpreadsheetModel.ActiveTable |> Map.filter (fun (_,r) _ -> r = i)
    Html.tr [
        for KeyValue ((column,row),cell) in r do
            let header = model.SpreadsheetModel.ActiveTable.[column,0]
            yield
                Cell((column,row), state, setState, model, dispatch)
            yield! referenceColumns(state, header, (column,row), model, dispatch)
    ]

let private headerRow (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    let rowInd = 0
    let r = model.SpreadsheetModel.ActiveTable |> Map.filter (fun (_,r) _ -> r = rowInd)
    Html.tr [
        for KeyValue ((column,row),cell) in r do
            yield
                Cell((column,row), state, setState, model, dispatch)
            yield! referenceColumns(state, cell, (column,row), model, dispatch)
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
                    Html.tbody [
                        let rows = model.SpreadsheetModel.ActiveTable.Keys |> Seq.maxBy snd |> snd
                        for rowInd in 1 .. rows do
                            yield bodyRow rowInd state setState model dispatch 
                    ]
                ]
            ]
        ]
    ]