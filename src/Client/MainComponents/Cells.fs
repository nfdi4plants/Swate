module Spreadsheet.Cells

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages

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

type private ContextFunctions = {
    DeleteRow       : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    DeleteColumn    : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Copy            : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Cut             : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Paste           : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    FillColumn      : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
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
            style.zIndex 20
            style.width 150
            style.zIndex 31 // to overlap navbar
            style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base)
        ]
        prop.children [
            rmv_element
            Html.ul buttonList
        ]
    ]

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
        style.padding(length.em 0.5,length.em 0.75)
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
            if isHeader then
                style.color(NFDIColors.white)
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
                setState_cell {state_cell with Active = false; Value = cell_value}
            | _ -> ()
        )
        // Only change cell value while typing to increase performance. 
        prop.onChange(fun e ->
            setState_cell {state_cell with Value = e}
        )
        prop.defaultValue cell_value
    ]

open Shared.TermTypes
open Shared.OfficeInteropTypes

module private EventPresets =

    let onContextMenu (index: int*int, dispatch) = fun (e: Browser.Types.MouseEvent) ->
        e.stopPropagation()
        e.preventDefault()
        let mousePosition = int e.pageX, int e.pageY
        let funcs = {
            DeleteRow       = fun rmv e -> rmv e; Spreadsheet.DeleteRow (snd index) |> Messages.SpreadsheetMsg |> dispatch
            DeleteColumn    = fun rmv e -> rmv e; Spreadsheet.DeleteColumn (fst index) |> Messages.SpreadsheetMsg |> dispatch
            Copy            = fun rmv e -> rmv e; Spreadsheet.CopyCell index |> Messages.SpreadsheetMsg |> dispatch
            Cut             = fun rmv e -> rmv e; Spreadsheet.CutCell index |> Messages.SpreadsheetMsg |> dispatch
            Paste           = fun rmv e -> rmv e; Spreadsheet.PasteCell index |> Messages.SpreadsheetMsg |> dispatch
            FillColumn      = fun rmv e -> rmv e; Spreadsheet.FillColumnWithTerm index |> Messages.SpreadsheetMsg |> dispatch
            RowIndex        = snd index
            ColumnIndex     = fst index
        }
        let child = contextmenu mousePosition funcs
        let name = $"context_{mousePosition}"
        Modals.Controller.renderModal(name, child)

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
            
///<summary> Only apply this element to SwateCell if header has term. </summary>
[<ReactComponent>]
let TANCell(index: (int*int), isHeader:bool, model: Model, dispatch) =
    let cell_element : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
    let columnIndex = fst index
    let rowIndex = snd index
    let state = model.SpreadsheetModel
    let cell = state.ActiveTable.[index]
    let cellValue =
        if isHeader then
            let tan = cell.Header.Term |> Option.map (fun x -> x.TermAccession) |> Option.defaultValue ""
            tan
        elif cell.isBody && cell.Body.Unit.IsSome then
            cell.Body.Unit.Value.TermAccession
        elif cell.isBody then
            cell.Body.Term.TermAccession
        else FreeTextInput
    let state_cell, setState_cell = React.useState(CellState.init(cellValue))
    let isSelected = state.SelectedCells.Contains index
    cell_element [
        prop.key $"Cell_{columnIndex}-{rowIndex}_TAN"
        cellStyle [
            if isHeader then
                style.color(NFDIColors.white)
                style.backgroundColor(NFDIColors.DarkBlue.Lighter20)
            if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
        ]
        prop.onContextMenu <| EventPresets.onContextMenu (index, dispatch)
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
                        let updateMainStateTable() =
                            // Only update if changed
                            if state_cell.Value <> cellValue then
                                // Updating unit name should remove unit tsr/tan
                                let nextTerm = 
                                    match cell with
                                    | IsHeader header ->
                                        let nextTerm = header.Term |> Option.map (fun t -> {t with TermAccession = state_cell.Value}) |> Option.defaultValue (TermMinimal.create "" state_cell.Value )
                                        let nextHeader = {header with Term = Some nextTerm}
                                        IsHeader nextHeader
                                    | IsBody body ->
                                        /// if cell has unit, update unit, not term
                                        let nextTerm =
                                            let t = if body.Unit.IsSome then body.Unit.Value else body.Term
                                            { t with TermAccession = state_cell.Value} 
                                        let nextBody = if body.Unit.IsSome then {body with Unit = Some nextTerm} else {body with Term = nextTerm} 
                                        IsBody nextBody
                                Msg.UpdateTable (index, nextTerm) |> SpreadsheetMsg |> dispatch
                            setState_cell {state_cell with Active = false}
                        cellInputElement(isHeader, updateMainStateTable, setState_cell, state_cell, cellValue)
                    else
                        let displayValue =
                            if isHeader then
                                $"{ColumnCoreNames.TermAccessionNumber.toString} ({cellValue})"
                            else
                                cellValue
                        Html.span [
                            prop.style [
                                style.flexGrow 1
                            ]
                            prop.text displayValue
                        ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let UnitCell(index: (int*int), isHeader:bool, model: Model, dispatch) =
    let cell_element : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
    let columnIndex = fst index
    let rowIndex = snd index
    let state = model.SpreadsheetModel
    let cell = state.ActiveTable.[index]
    let cellValue = if isHeader then ColumnCoreNames.Unit.toString elif cell.isBody && cell.Body.Unit.IsSome then cell.Body.Unit.Value.Name else "Unknown"
    let state_cell, setState_cell = React.useState(CellState.init(cellValue))
    let isSelected = state.SelectedCells.Contains index
    cell_element [
        prop.key $"Cell_{columnIndex}-{rowIndex}_Unit"
        cellStyle [
            if isHeader then
                style.color(NFDIColors.white)
                style.backgroundColor(NFDIColors.DarkBlue.Lighter20)
            if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
        ]
        prop.onContextMenu <| EventPresets.onContextMenu (index, dispatch)
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
                    if not isHeader && state_cell.Active then
                        let updateMainStateTable() =
                            // Only update if changed
                            if state_cell.Value <> cellValue then
                                // Updating unit name should remove unit tsr/tan
                                let nextTerm = {cell.Body.Unit.Value with Name = state_cell.Value}
                                let nextBody = IsBody { cell.Body with Unit = nextTerm |> Some }
                                Msg.UpdateTable (index, nextBody) |> SpreadsheetMsg |> dispatch
                            setState_cell {state_cell with Active = false}
                        cellInputElement(isHeader, updateMainStateTable, setState_cell, state_cell, cellValue)
                    else
                        Html.span [
                            prop.style [
                                style.flexGrow 1
                            ]
                            prop.text cellValue
                        ]
                ]
            ]
        ]
    ]

let private extendHeaderButton (state_extend: Set<int>, columnIndex, setState_extend) =
    let isExtended = state_extend.Contains(columnIndex)
    Bulma.icon [
        prop.style [
            style.cursor.pointer
        ]
        prop.onDoubleClick(fun e ->
            e.stopPropagation()
            e.preventDefault()
            ()
        )
        prop.onClick(fun e ->
            e.stopPropagation()
            e.preventDefault()
            let nextState = if isExtended then state_extend.Remove(columnIndex) else state_extend.Add(columnIndex)
            setState_extend nextState
        )
        prop.children [Html.i [prop.classes ["fa-sharp"; "fa-solid"; "fa-angles-up"; if isExtended then "fa-rotate-270" else "fa-rotate-90"]; prop.style [style.fontSize(length.em 1)]]]
    ]

[<ReactComponent>]
let Cell(index: (int*int), isHeader:bool, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
    let columnIndex = fst index
    let rowIndex = snd index
    let state = model.SpreadsheetModel
    let cell = state.ActiveTable.[index]
    let cellValue = if isHeader then cell.Header.SwateColumnHeader else cell.Body.Term.Name
    let state_cell, setState_cell = React.useState(CellState.init(cellValue))
    let isSelected = state.SelectedCells.Contains index
    let cell_element : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
    cell_element [
        prop.key $"Cell_{columnIndex}-{rowIndex}"
        cellStyle [
            if isHeader then
                style.color(NFDIColors.white)
                style.backgroundColor(NFDIColors.DarkBlue.Base)
            if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
        ]
        prop.onContextMenu <| EventPresets.onContextMenu (index, dispatch)
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
                        /// TODO! Try get from db?
                        /// Update change to mainState and exit active input.
                        let updateMainStateTable() =
                            // Only update if changed
                            if state_cell.Value <> cellValue then
                                let nextTerm = cell.updateDisplayValue state_cell.Value
                                Msg.UpdateTable (index, nextTerm) |> SpreadsheetMsg |> dispatch
                            setState_cell {state_cell with Active = false}
                        cellInputElement(isHeader, updateMainStateTable, setState_cell, state_cell, cellValue)
                    else
                        let displayName =
                            if cell.isBody && cell.Body.Unit.IsSome then
                                let name = if cellValue = "" then None else Some cellValue
                                name |> Option.map (fun x -> x + " " + cell.Body.Unit.Value.Name) |> Option.defaultValue ""
                            else
                                cellValue
                        Html.span [
                            prop.style [
                                style.flexGrow 1
                            ]
                            prop.text displayName
                        ]
                    if isHeader && (cell.Header.isTermColumn || cell.Header.isFeaturedCol) then
                        extendHeaderButton(state_extend, columnIndex, setState_extend)
                ]
            ]
        ]
    ]