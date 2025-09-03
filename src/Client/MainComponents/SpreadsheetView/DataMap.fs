module MainComponents.SpreadsheetView.DataMap

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Model
open SpreadsheetInterface
open Messages
open Swate.Components.Shared

open Spreadsheet.Cells

// let NameIndex = -1

// let private CreateBodyCells (columnIndex, rowIndex, state: Set<int>, model, dispatch) =
//     let index = columnIndex, rowIndex
//     let cell = model.SpreadsheetModel.DataMapOrDefault.GetCell(columnIndex, rowIndex)

//     [
//         Spreadsheet.Cells.Cell.Body(index, cell, model, dispatch)
//         let isExtended = state.Contains columnIndex

//         let header =
//             Spreadsheet.Controller.Generic.getHeader columnIndex model.SpreadsheetModel

//         if (cell.isTerm || cell.isUnitized) && isExtended then
//             if cell.isUnitized then
//                 Cell.BodyUnit(index, cell, model, dispatch)
//             else
//                 Cell.Empty()

//             Cell.BodyTSR(index, cell, model, dispatch)
//             Cell.BodyTAN(index, cell, model, dispatch)
//         elif header.IsDataColumn && isExtended then
//             if cell.isData then
//                 Cell.BodyDataSelector(index, cell, model, dispatch)
//                 Cell.BodyDataFormat(index, cell, model, dispatch)
//                 Cell.BodyDataSelectorFormat(index, cell, model, dispatch)
//             else
//                 Cell.Empty()
//                 Cell.Empty()
//                 Cell.Empty()
//     ]

// let private CreateHeaderCells (columnIndex, state, setState, model, dispatch) =
//     let header = DataMap.getHeader columnIndex

//     [
//         Cell.Header(columnIndex, header, state, setState, model, dispatch, readonly = true)
//         if header.IsTermColumn then
//             let isExtended = state.Contains columnIndex

//             if isExtended then
//                 Cell.HeaderUnit(columnIndex, header, state, setState, model, dispatch)
//                 Cell.HeaderTSR(columnIndex, header, state, setState, model, dispatch)
//                 Cell.HeaderTAN(columnIndex, header, state, setState, model, dispatch)
//         elif header.IsDataColumn then
//             Cell.HeaderDataSelector(columnIndex, header, state, setState, model, dispatch)
//             Cell.HeaderDataFormat(columnIndex, header, state, setState, model, dispatch)
//             Cell.HeaderDataSelectorFormat(columnIndex, header, state, setState, model, dispatch)
//         else
//             ()
//     ]

// let private BodyRow (rowIndex: int) (state: Set<int>) (model: Model) (dispatch: Msg -> unit) = [
//     //MainComponents.CellStyles.RowLabel rowIndex
//     //Spreadsheet.Cells.Cell.BodyBase(
//     //    Spreadsheet.ColumnType.Main,
//     //    model.SpreadsheetModel.DataMapOrDefault.DataContexts.[rowIndex].Name |> Option.defaultValue "",
//     //    (fun _ -> ()),
//     //    (NameIndex, rowIndex),
//     //    model,
//     //    dispatch,
//     //    readonly=true,
//     //    tooltip="This field is calculated from `Data File Path` and `Data Selector`"
//     //)
//     for columnIndex in 0 .. (model.SpreadsheetModel.DataMapOrDefault.ColumnCount - 1) do
//         (columnIndex, rowIndex, state, model, dispatch)
// ]

// let private BodyRows (state: Set<int>) (model: Model) (dispatch: Msg -> unit) = [|
//     for rowInd in 0 .. model.SpreadsheetModel.DataMapOrDefault.RowCount - 1 do
//         yield BodyRow rowInd state model dispatch
// |]

// let private HeaderRow (state: Set<int>) setState (model: Model) (dispatch: Msg -> unit) = [
//     //MainComponents.CellStyles.RowLabel -1
//     //Spreadsheet.Cells.Cell.Header(NameIndex, CompositeHeader.FreeText "Data Name", state, setState, model, dispatch, readonly = true)
//     for columnIndex in 0 .. (model.SpreadsheetModel.DataMapOrDefault.ColumnCount - 1) do
//         (columnIndex, state, setState, model, dispatch)
// ]

open Swate.Components

let Main (model: Model, dispatch: Msg -> unit) =
    //let state, setState : Set<int> * (Set<int> -> unit) = React.useState(Set.empty)
    //Html.div [
    //    prop.id "SPREADSHEET_MAIN_VIEW"
    //    prop.tabIndex 0
    //    prop.className "flex grow overflow-y-hidden"
    //    prop.style [style.border(1, borderStyle.solid, "grey")]
    //    prop.ref ref
    //    prop.onKeyDown(fun e -> Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch e)
    //    prop.children [
    //        Components.LazyLoadTable.Main("SpreadsheetViewTable", Components.BodyRows state model dispatch, Components.HeaderRow state setState model dispatch, 35, tableClasses=[|"fixed_headers"|])
    //    ]
    //]
    // Generic.Main(
    //     (fun s -> BodyRows s model dispatch),
    //     CreateBodyCells,
    //     (fun s ss -> HeaderRow s ss model dispatch),
    //     CreateHeaderCells,
    //     model,
    //     dispatch
    // )

    let datamap = model.SpreadsheetModel.DataMapOrDefault

    let setDatamap =
        fun (dm: DataMap) -> Spreadsheet.UpdateDatamap(Some dm) |> SpreadsheetMsg |> dispatch

    DataMapTable.DataMapTable(datamap, setDatamap)