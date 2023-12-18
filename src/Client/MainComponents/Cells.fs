module Spreadsheet.Cells

open Feliz
open Feliz.Bulma

open Spreadsheet
open MainComponents
open Messages
open Shared
open ARCtrl.ISA

type private CellState = {
    Active: bool
    /// This value is used to show during input cell editing. After confirming edit it will be used to push update
    Value: string
} with
    static member init() =
        {
            Active      = false
            Value       = ""
        }
    static member init(v: string) =
        {
            Active      = false
            Value       = v
        }


let private cellStyle (specificStyle: IStyleAttribute list) = prop.style [
        style.minWidth 100
        style.height 22
        style.border(length.px 1, borderStyle.solid, "darkgrey")
        yield! specificStyle
    ]

let private cellInnerContainerStyle (specificStyle: IStyleAttribute list) = prop.style [
        style.display.flex;
        style.justifyContent.spaceBetween;
        style.height(length.percent 100);
        style.minHeight(35)
        style.width(length.percent 100)
        style.alignItems.center
        yield! specificStyle
    ]

let private cellInputElement (isHeader: bool, updateMainStateTable: unit -> unit, setState_cell, state_cell, cell_value) =
    Bulma.input.text [
        prop.autoFocus true
        prop.style [
            if isHeader then style.fontWeight.bold
            style.width(length.percent 100)
            style.height.unset
            style.borderRadius(0)
            style.border(0,borderStyle.none,"")
            style.backgroundColor.transparent
            style.margin (0)
            style.padding(length.em 0.5,length.em 0.75)
            //if isHeader then
            //    style.color(NFDIColors.white)
        ]
        // Update main spreadsheet state when leaving focus or...
        prop.onBlur(fun _ ->
            updateMainStateTable()
        )
        // .. when pressing "ENTER". "ESCAPE" will negate changes.
        prop.onKeyDown(fun e ->
            match e.which with
            | 13. -> //enter
                updateMainStateTable()
            | 27. -> //escape
                setState_cell {Active = false; Value = cell_value}
            | _ -> ()
        )
        // Only change cell value while typing to increase performance. 
        prop.onChange(fun e ->
            setState_cell {state_cell with Value = e}
        )
        prop.defaultValue cell_value
    ]

let private basicValueDisplayCell (v: string) =
    Html.span [
        prop.style [
            style.flexGrow 1
            style.padding(length.em 0.5,length.em 0.75)
        ]
        prop.text v
    ]

module private EventPresets =

    let onClickSelect (index: int*int, state_cell, selectedCells: Set<int*int>, dispatch)=
        fun (e: Browser.Types.MouseEvent) ->
            // don't select cell if active(editable)
            if not state_cell.Active then
                match e.ctrlKey with
                | true ->
                    let createSetOfIndex (columnMin:int, columnMax, rowMin:int, rowMax: int) =
                        [
                            for c in columnMin .. columnMax do
                                for r in rowMin .. rowMax do
                                    c, r
                        ] |> Set.ofList
                    let source = selectedCells.MinimumElement
                    let target = index
                    let columnMin, columnMax = System.Math.Min(fst source, fst target), System.Math.Max(fst source, fst target)
                    let rowMin, rowMax = System.Math.Min(snd source, snd target), System.Math.Max(snd source, snd target)
                    let set = createSetOfIndex (columnMin,columnMax,rowMin,rowMax)
                    UpdateSelectedCells set |> SpreadsheetMsg |> dispatch
                | false ->
                    let next = if selectedCells = Set([index]) then Set.empty else Set([index])
                    UpdateSelectedCells next |> SpreadsheetMsg |> dispatch
            else
                ()
            
/////<summary> Only apply this element to SwateCell if header has term. </summary>
//[<ReactComponent>]
//let TANCell(index: (int*int), model: Model, dispatch) =
//    let columnIndex = fst index
//    let rowIndex = snd index
//    let state = model.SpreadsheetModel
//    let cell = state.ActiveTable.[index]
//    let isHeader = cell.isHeader
//    let cellValue =
//        if isHeader then
//            let tan = cell.Header.Term |> Option.map (fun x -> x.TermAccession) |> Option.defaultValue ""
//            tan
//        elif cell.isUnit then
//            cell.Unit.Unit.TermAccession
//        elif cell.isTerm then
//            cell.Term.Term.TermAccession
//        else ""
//    let state_cell, setState_cell = React.useState(CellState.init(cellValue))
//    let isSelected = state.SelectedCells.Contains index
//    let cell_element : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
//    cell_element [
//        prop.key $"Cell_{state.ActiveTableIndex}-{columnIndex}-{rowIndex}_TAN"
//        cellStyle [
//            if isHeader then
//                style.color(NFDIColors.white)
//                style.backgroundColor(NFDIColors.DarkBlue.Lighter20)
//            if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
//        ]
//        prop.onContextMenu <| ContextMenu.onContextMenu (index, model, dispatch)
//        prop.children [
//            Html.div [
//                cellInnerContainerStyle []
//                prop.onDoubleClick(fun e ->
//                    e.preventDefault()
//                    e.stopPropagation()
//                    UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
//                    if not state_cell.Active then setState_cell {state_cell with Active = true}
//                )
//                prop.onClick <| EventPresets.onClickSelect(index, state_cell, state.SelectedCells, dispatch)
//                prop.children [
//                    if state_cell.Active then
//                        let updateMainStateTable() =
//                            // Only update if changed
//                            if state_cell.Value <> cellValue then
//                                // Updating unit name should remove unit tsr/tan
//                                let nextTerm = 
//                                    match cell with
//                                    | IsHeader header ->
//                                        let nextTerm = header.Term |> Option.map (fun t -> {t with TermAccession = state_cell.Value}) |> Option.defaultValue (TermMinimal.create "" state_cell.Value )
//                                        let nextHeader = {header with Term = Some nextTerm}
//                                        IsHeader nextHeader
//                                    | IsTerm t_cell ->
//                                        let nextTermCell =
//                                            let term = { t_cell.Term with TermAccession = state_cell.Value }
//                                            { t_cell with Term = term } 
//                                        IsTerm nextTermCell
//                                    | IsUnit u_cell ->
//                                        let nextUnitCell =
//                                            let unit = { u_cell.Unit with TermAccession = state_cell.Value }
//                                            { u_cell with Unit = unit } 
//                                        IsUnit nextUnitCell
//                                    | IsFreetext _ ->
//                                        let t_cell = cell.toTermCell().Term
//                                        let term = {t_cell.Term with TermAccession = state_cell.Value}
//                                        let nextCell = {t_cell with Term = term}
//                                        IsTerm nextCell
//                                Msg.UpdateTable (index, nextTerm) |> SpreadsheetMsg |> dispatch
//                            setState_cell {state_cell with Active = false}
//                        cellInputElement(isHeader, updateMainStateTable, setState_cell, state_cell, cellValue)
//                    else
//                        let displayValue =
//                            if isHeader then
//                                $"{ColumnCoreNames.TermAccessionNumber.toString} ({cellValue})"
//                            else
//                                cellValue
//                        Html.span [
//                            prop.style [
//                                style.flexGrow 1
//                            ]
//                            prop.text displayValue
//                        ]
//                ]
//            ]
//        ]
//    ]

//[<ReactComponent>]
//let UnitCell(index: (int*int), model: Model, dispatch) =
//    let columnIndex = fst index
//    let rowIndex = snd index
//    let state = model.SpreadsheetModel
//    let cell = state.ActiveTable.[index]
//    let isHeader = cell.isHeader
//    let cellValue = if isHeader then ColumnCoreNames.Unit.toString elif cell.isUnit then cell.Unit.Unit.Name else "Unknown"
//    let state_cell, setState_cell = React.useState(CellState.init(cellValue))
//    let isSelected = state.SelectedCells.Contains index
//    let cell_element : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
//    cell_element [
//        prop.key $"Cell_{state.ActiveTableIndex}-{columnIndex}-{rowIndex}_Unit"
//        cellStyle [
//            if isHeader then
//                style.color(NFDIColors.white)
//                style.backgroundColor(NFDIColors.DarkBlue.Lighter20)
//            if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
//        ]
//        prop.onContextMenu <| ContextMenu.onContextMenu (index, model, dispatch)
//        prop.children [
//            Html.div [
//                cellInnerContainerStyle []
//                prop.onDoubleClick(fun e ->
//                    e.preventDefault()
//                    e.stopPropagation()
//                    UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
//                    if not state_cell.Active then setState_cell {state_cell with Active = true}
//                )
//                prop.onClick <| EventPresets.onClickSelect(index, state_cell, state.SelectedCells, dispatch)
//                prop.children [
//                    if not isHeader && state_cell.Active then
//                        let updateMainStateTable() =
//                            // Only update if changed
//                            if state_cell.Value <> cellValue then
//                                // This column only exists for unit cells
//                                let nextTerm = {cell.Unit.Unit with Name = state_cell.Value}
//                                let nextBody = IsUnit { cell.Unit with Unit = nextTerm }
//                                Msg.UpdateTable (index, nextBody) |> SpreadsheetMsg |> dispatch
//                            setState_cell {state_cell with Active = false}
//                        cellInputElement(isHeader, updateMainStateTable, setState_cell, state_cell, cellValue)
//                    else
//                        Html.span [
//                            prop.style [
//                                style.flexGrow 1
//                            ]
//                            prop.text cellValue
//                        ]
//                ]
//            ]
//        ]
//    ]

//let private extendHeaderButton (state_extend: Set<int>, columnIndex, setState_extend) =
//    let isExtended = state_extend.Contains(columnIndex)
//    Bulma.icon [
//        prop.style [
//            style.cursor.pointer
//        ]
//        prop.onDoubleClick(fun e ->
//            e.stopPropagation()
//            e.preventDefault()
//            ()
//        )
//        prop.onClick(fun e ->
//            e.stopPropagation()
//            e.preventDefault()
//            let nextState = if isExtended then state_extend.Remove(columnIndex) else state_extend.Add(columnIndex)
//            setState_extend nextState
//        )
//        prop.children [Html.i [prop.classes ["fa-sharp"; "fa-solid"; "fa-angles-up"; if isExtended then "fa-rotate-270" else "fa-rotate-90"]; prop.style [style.fontSize(length.em 1)]]]
//    ]

[<ReactComponent>]
let HeaderCell(columnIndex: int, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
    let state = model.SpreadsheetModel
    let header = state.ActiveTable.Headers.[columnIndex]
    let cellValue = header.ToString()
    let state_cell, setState_cell = React.useState(CellState.init(cellValue))
    Html.th [
        prop.key $"Header_{state.ActiveView.TableIndex}-{columnIndex}"
        cellStyle [
            //if isHeader && cell.Header.isInputColumn then
            //    style.color(NFDIColors.white)
            //    style.backgroundColor(NFDIColors.LightBlue.Base)
            //elif isHeader && cell.Header.isOutputColumn then
            //    style.color(NFDIColors.white)
            //    style.backgroundColor(NFDIColors.Red.Lighter30)
            //elif isHeader then
            //    style.color(NFDIColors.white)
            //    style.backgroundColor(NFDIColors.DarkBlue.Base)
            //if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
        ]
        //prop.onContextMenu <| ContextMenu.onContextMenu (index, model, dispatch)
        prop.children [
            Html.div [
                cellInnerContainerStyle []
                prop.onDoubleClick(fun e ->
                    e.preventDefault()
                    e.stopPropagation()
                    UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
                    if not state_cell.Active then setState_cell {state_cell with Active = true}
                )
                //prop.onClick <| EventPresets.onClickSelect(index, state_cell, state.SelectedCells, dispatch)
                prop.children [
                    if state_cell.Active then
                        /// Update change to mainState and exit active input.
                        let updateMainStateTable() =
                            // Only update if changed
                            if state_cell.Value <> cellValue then
                                let nextHeader = CompositeHeader.OfHeaderString state_cell.Value
                                Msg.UpdateHeader (columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch
                            setState_cell {state_cell with Active = false}
                        cellInputElement(true, updateMainStateTable, setState_cell, state_cell, cellValue)
                    else
                        basicValueDisplayCell cellValue
                    //if isHeader && cell.Header.isTermColumn then
                    //    extendHeaderButton(state_extend, columnIndex, setState_extend)
                ]
            ]
        ]
    ]

[<ReactComponent>]
let BodyCell(index: (int*int), state_extend: Set<int>, setState_extend, model: Model, dispatch) =
    let columnIndex, rowIndex = index
    let state = model.SpreadsheetModel
    let cell = state.ActiveTable.TryGetCellAt index |> _.Value
    let cellValue = cell.GetContent().[0]
    let state_cell, setState_cell = React.useState(CellState.init(cellValue))
    let isSelected = state.SelectedCells.Contains index
    Html.td [
        prop.key $"Cell_{state.ActiveView.TableIndex}-{columnIndex}-{rowIndex}"
        cellStyle [
            //if isHeader && cell.Header.isInputColumn then
            //    style.color(NFDIColors.white)
            //    style.backgroundColor(NFDIColors.LightBlue.Base)
            //elif isHeader && cell.Header.isOutputColumn then
            //    style.color(NFDIColors.white)
            //    style.backgroundColor(NFDIColors.Red.Lighter30)
            //elif isHeader then
            //    style.color(NFDIColors.white)
            //    style.backgroundColor(NFDIColors.DarkBlue.Base)
            if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
        ]
        prop.onContextMenu <| ContextMenu.onContextMenu (index, model, dispatch)
        prop.children [
            Html.div [
                cellInnerContainerStyle []
                prop.onDoubleClick(fun e ->
                    e.preventDefault()
                    e.stopPropagation()
                    UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
                    if not state_cell.Active then setState_cell {state_cell with Active = true}
                )
                prop.onClick <| EventPresets.onClickSelect(index, state_cell, state.SelectedCells, dispatch)
                prop.children [
                    if state_cell.Active then
                        /// Update change to mainState and exit active input.
                        let updateMainStateTable() =
                            // Only update if changed
                            if state_cell.Value <> cellValue then
                                let nextCell = cell.UpdateMainField state_cell.Value
                                Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
                            setState_cell {state_cell with Active = false}
                        cellInputElement(false, updateMainStateTable, setState_cell, state_cell, cellValue)
                    else
                        let displayName =
                            if cell.isUnitized then
                                let oaName = cell.ToTerm().NameText
                                let unitizedValue = if cellValue = "" then "" else cellValue + " " + oaName
                                unitizedValue
                            else
                                cellValue
                        basicValueDisplayCell displayName
                    //if isHeader && cell.Header.isTermColumn then
                    //    extendHeaderButton(state_extend, columnIndex, setState_extend)
                ]
            ]
        ]
    ]