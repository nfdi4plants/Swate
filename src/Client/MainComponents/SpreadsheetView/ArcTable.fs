module MainComponents.SpreadsheetView.ArcTable

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Spreadsheet.Cells
open ARCtrl
open Shared
open Model

let private CreateBodyCells (columnIndex, rowIndex, state:Set<int>, model:Model, dispatch: Msg -> unit) =
    let index = columnIndex, rowIndex
    let table = model.SpreadsheetModel.ActiveTable
    let cell = model.SpreadsheetModel.ActiveTable.Values.[index]
    let isExtended = state.Contains columnIndex
    [
        Cells.Cell.Body (index, cell, model, dispatch)
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

let private CreateHeaderCells(columnIndex, state, setState, model, dispatch) =
    let table = model.SpreadsheetModel.ActiveTable
    let header = table.Headers.[columnIndex]
    [
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
    

let private BodyRow (rowIndex: int) (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
    [
        for columnIndex in 0 .. (model.SpreadsheetModel.ActiveTable.ColumnCount-1) do
            (
                columnIndex,
                rowIndex,
                state,
                model,
                dispatch
            )
    ]

let private BodyRows (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
    [|
        for rowInd in 0 .. model.SpreadsheetModel.ActiveTable.RowCount-1 do
            yield BodyRow rowInd state model dispatch 
    |]
    

let private HeaderRow (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
    [
        for columnIndex in 0 .. (model.SpreadsheetModel.ActiveTable.ColumnCount-1) do
            (
                columnIndex,
                state,
                setState,
                model,
                dispatch
            )           
    ]

open Fable.Core.JsInterop

[<ReactComponent>]
let Main (model:Model, dispatch: Msg -> unit) =
    ////React.useListener.on("keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    //let ref = React.useElementRef()
    ////React.useElementListener.on(ref, "keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    ///// This state is used to track which columns are expanded
    //let state, setState : Set<int> * (Set<int> -> unit) = React.useState(Set.empty)
    //React.useEffect((fun _ -> setState Set.empty), [|box model.SpreadsheetModel.ActiveView|])
    //let createRowLabel (rowIndex: int) = MainComponents.CellStyles.RowLabel rowIndex
    //Html.div [
    //    prop.id "SPREADSHEET_MAIN_VIEW"
    //    prop.key $"SPREADSHEET_MAIN_VIEW_{model.SpreadsheetModel.ActiveView.TableIndex}"
    //    prop.tabIndex 0
    //    prop.className "flex grow overflow-y-hidden"
    //    prop.style [style.border(1, borderStyle.solid, "grey")]
    //    prop.ref ref
    //    prop.onKeyDown(fun e -> Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch e)
    //    prop.children [
    //        Components.LazyLoadTable.Main(
    //            "SpreadsheetViewTable",
    //            BodyRows state model dispatch,
    //            CreateBodyCells,
    //            {|data=HeaderRow state setState model dispatch; createCell=CreateHeaderCells|},
    //            35,
    //            tableClasses=[|"fixed_headers"|],
    //            containerClasses=[|"pr-[10vw]"|],
    //            rowLabel={|styling=Some createRowLabel|}
    //        )
    //    ]
    //]
    Generic.Main(
        (fun s -> BodyRows s model dispatch),
        CreateBodyCells,
        (fun s ss -> HeaderRow s ss model dispatch),
        CreateHeaderCells,
        model,
        dispatch
    )