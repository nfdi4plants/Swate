namespace MainComponents.DataMap

open ARCtrl
open Feliz
open Feliz.Bulma
open Model
open SpreadsheetInterface
open Messages
open Shared

module private Components =
    open Spreadsheet.Cells

    let NameIndex = -1

    let private BodyRow (rowIndex: int) (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
        Html.tr [
            MainComponents.CellStyles.RowLabel rowIndex
            Spreadsheet.Cells.Cell.BodyBase(
                Spreadsheet.ColumnType.Main,
                model.SpreadsheetModel.DataMapOrDefault.DataContexts.[rowIndex].Name |> Option.defaultValue "",
                (fun _ -> ()),
                (NameIndex, rowIndex),
                model,
                dispatch,
                readonly=true,
                tooltip="This field is calculated from `Data File Path` and `Data Selector`"
            )
            for columnIndex in 0 .. (model.SpreadsheetModel.DataMapOrDefault.ColumnCount-1) do
                let index = columnIndex, rowIndex
                let cell = model.SpreadsheetModel.DataMapOrDefault.GetCell(columnIndex, rowIndex)
                Spreadsheet.Cells.Cell.Body (index, cell, model, dispatch)
                let isExtended = state.Contains columnIndex
                let header = Spreadsheet.Controller.Generic.getHeader columnIndex model.SpreadsheetModel
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

    let BodyRows (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
        Html.tbody [
            for rowInd in 0 .. model.SpreadsheetModel.DataMapOrDefault.RowCount-1 do
                yield BodyRow rowInd state model dispatch 
        ]

    let HeaderRow (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
        let headers = [|
            for i in 0 .. DataMap.ColumnCount-1 do DataMap.getHeader i
        |]
        let dtm = model.SpreadsheetModel.DataMapOrDefault
        Html.tr [
            MainComponents.CellStyles.RowLabel -1
            Spreadsheet.Cells.Cell.Header(NameIndex, CompositeHeader.FreeText "Data Name", state, setState, model, dispatch, readonly = true)
            for columnIndex in 0 .. (dtm.ColumnCount-1) do
                let header = headers.[columnIndex]
                Spreadsheet.Cells.Cell.Header(columnIndex, header, state, setState, model, dispatch, readonly = true)
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

type DataMap =

    [<ReactComponent>]
    static member Main(model: Model, dispatch: Msg -> unit) =
        let ref = React.useElementRef()
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
                            Components.HeaderRow state setState model dispatch
                        ]
                        Components.BodyRows state model dispatch
                    ]
                ]
            ]
        ]