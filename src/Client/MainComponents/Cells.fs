module Spreadsheet.Cells

open Feliz
open Feliz.Bulma

open Spreadsheet
open MainComponents
open Messages
open Shared
open ARCtrl.ISA
open Components

type private CellMode = 
| Active
| Idle

type private CellState = {
    CellMode: CellMode
    /// This value is used to show during input cell editing. After confirming edit it will be used to push update
    Value: string
} with
    static member init() =
        {
            CellMode    = Idle
            Value       = ""
        }
    static member init(v: string) =
        {
            CellMode    = Idle
            Value       = v
        }

    member this.IsActive = this.CellMode = Active
    member this.IsIdle = this.CellMode = Idle

type private ColumnType =
| Main
| Unit
| TSR
| TAN
with
    member this.IsMainColumn = match this with | Main -> true | _ -> false
    member this.IsRefColumn = match this with | Unit | TSR | TAN -> true | _ -> false

module private CellComponents =


    let cellStyle (specificStyle: IStyleAttribute list) = prop.style [
            style.minWidth 100
            style.height 22
            style.border(length.px 1, borderStyle.solid, "darkgrey")
            yield! specificStyle
        ]

    let cellInnerContainerStyle (specificStyle: IStyleAttribute list) = prop.style [
            style.display.flex;
            style.justifyContent.spaceBetween;
            style.height(length.percent 100);
            style.minHeight(35)
            style.width(length.percent 100)
            style.alignItems.center
            yield! specificStyle
        ]

    [<ReactComponent>]
    let CellInputElement (isHeader: bool, isReadOnly: bool, updateMainStateTable: string -> unit, setState_cell, state_cell, cell_value, columnType) =
        let ref = React.useElementRef()

        React.useLayoutEffectOnce(fun _ -> ClickOutsideHandler.AddListener (ref, fun _ -> updateMainStateTable state_cell.Value))
        let input = 
            Bulma.control.div [
                Bulma.control.isExpanded
                prop.children [
                    Bulma.input.text [
                        prop.readOnly isReadOnly
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
                        // .. when pressing "ENTER". "ESCAPE" will negate changes.
                        prop.onKeyDown(fun e ->
                            match e.which with
                            | 13. -> //enter
                                updateMainStateTable state_cell.Value
                            | 27. -> //escape
                                setState_cell {CellMode = Idle; Value = cell_value}
                            | _ -> ()
                        )
                        // Only change cell value while typing to increase performance. 
                        prop.onChange(fun e ->
                            setState_cell {state_cell with Value = e}
                        )
                        prop.defaultValue cell_value
                    ]
                ]
            ]
        Bulma.field.div [
            Bulma.field.hasAddons
            prop.ref ref
            prop.className "is-flex-grow-1 m-0"
            prop.children [ input ]           
        ]

    let basicValueDisplayCell (v: string) =
        Html.span [
            prop.style [
                style.flexGrow 1
                style.padding(length.em 0.5,length.em 0.75)
            ]
            prop.text v
        ]

    let compositeCellDisplay (cc: CompositeCell) =
        let hasValidOA = match cc with | CompositeCell.Term oa -> oa.TermAccessionShort <> "" | CompositeCell.Unitized (v, oa) -> oa.TermAccessionShort <> "" | CompositeCell.FreeText _ -> false
        let v = cc.ToString()
        Html.div [
            prop.classes ["is-flex"]
            prop.style [
                style.flexGrow 1
                style.padding(length.em 0.5,length.em 0.75)
            ]
            prop.children [
                Html.span [
                    prop.style [
                        style.flexGrow 1
                    ]
                    prop.text v
                ]
                if hasValidOA then 
                    Bulma.icon [Html.i [
                        prop.style [style.custom("marginLeft", "auto")]
                        prop.className ["fa-solid"; "fa-check"]
                    ]]
            ]
        ]

    let extendHeaderButton (state_extend: Set<int>, columnIndex, setState_extend) =
        let isExtended = state_extend.Contains(columnIndex)
        Bulma.icon [
            prop.style [
                style.height (length.perc 100)
                style.minWidth 25
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


module private CellAux =

    let headerTANSetter (columnIndex: int, s: string, header: CompositeHeader, dispatch) =
        let oa = header.TryOA()
        let tan = if s = "" then None else Some s
        oa 
        |> Option.map (fun x -> {x with TermAccessionNumber = tan})
        |> Option.map header.UpdateWithOA
        |> Option.iter (fun nextHeader -> Msg.UpdateHeader (columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch)

    let oasetter (index, cell: CompositeCell, dispatch) = fun (oa:OntologyAnnotation) ->
        let nextCell = 
            if oa.TermSourceREF.IsNone && oa.TermAccessionNumber.IsNone then // update only mainfield, if mainfield is the only field with value
                cell.UpdateMainField oa.NameText 
            else 
                cell.UpdateWithOA oa
        Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        

open CellComponents
open CellAux

module private EventPresets =

    open Shared

    let onClickSelect (index: int*int, state_cell: CellState, selectedCells: Set<int*int>, model:Messages.Model, dispatch)=
        fun (e: Browser.Types.MouseEvent) ->
            // don't select cell if active(editable)
            if state_cell.IsIdle then
                let set = 
                    match e.shiftKey, selectedCells.Count with
                    | true, 0 ->
                        selectedCells
                    | true, _ ->
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
                        set
                    | false, _ ->
                        let next = if selectedCells = Set([index]) then Set.empty else Set([index])
                        next
                UpdateSelectedCells set |> SpreadsheetMsg |> dispatch
                if not set.IsEmpty then
                    let oa = 
                        let columnIndex = set |> Seq.minBy fst |> fst 
                        let column = model.SpreadsheetModel.ActiveTable.GetColumn(columnIndex)
                        if column.Header.IsTermColumn then
                            column.Header.ToTerm() |> Some //ToOA
                        else
                            None
                    TermSearch.UpdateParentTerm oa |> TermSearchMsg |> dispatch

open Shared

type Cell =

    [<ReactComponent>]
    static member private HeaderBase(columnType: ColumnType, setter: string -> unit, cellValue: string, columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let state = model.SpreadsheetModel
        let state_cell, setState_cell = React.useState(CellState.init(cellValue))
        React.useEffect((fun _ -> setState_cell {state_cell with Value = cellValue}), [|box cellValue|])
        let isReadOnly = columnType = Unit
        Html.th [
            if columnType.IsRefColumn then Bulma.color.hasBackgroundGreyLighter
            prop.key $"Header_{state.ActiveView.TableIndex}-{columnIndex}-{columnType}"
            prop.id $"Header_{columnIndex}_{columnType}"
            cellStyle []
            prop.className "main-contrast-bg"
            prop.children [
                Html.div [
                    cellInnerContainerStyle [style.custom("backgroundColor","inherit")]
                    if not isReadOnly then prop.onDoubleClick(fun e ->
                        e.preventDefault()
                        e.stopPropagation()
                        UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
                        if state_cell.IsIdle then setState_cell {state_cell with CellMode = Active}
                    )
                    prop.children [
                        if state_cell.IsActive then
                            /// Update change to mainState and exit active input.
                            let updateMainStateTable = fun (s: string) -> 
                                // Only update if changed
                                if s <> cellValue then
                                    setter s
                                setState_cell {state_cell with CellMode = Idle}
                            CellInputElement(true, isReadOnly, updateMainStateTable, setState_cell, state_cell, cellValue, columnType)
                        else
                            let cellValue = // shadow cell value for tsr and tan to add columnType
                                match columnType with
                                | TSR | TAN -> $"{columnType} ({cellValue})" 
                                | _ -> cellValue
                            basicValueDisplayCell cellValue
                        if columnType = Main && not header.IsSingleColumn then 
                            extendHeaderButton(state_extend, columnIndex, setState_extend)
                    ]
                ]
            ]
        ]

    static member Header(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let cellValue = header.ToString()
        let setter =
            fun (s: string) -> 
                let mutable nextHeader = CompositeHeader.OfHeaderString s
                // update header with ref columns if term column
                if header.IsTermColumn && not header.IsFeaturedColumn then
                    nextHeader <- nextHeader.UpdateDeepWith header
                Msg.UpdateHeader (columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch
        Cell.HeaderBase(Main, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch)

    static member HeaderUnit(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let cellValue = "Unit"
        let setter = fun (s: string) -> ()
        Cell.HeaderBase(Unit, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch)

    static member HeaderTSR(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let cellValue = header.TryOA() |> Option.map (fun oa -> oa.TermAccessionShort) |> Option.defaultValue ""
        let setter = fun (s: string) -> headerTANSetter(columnIndex, s, header, dispatch)
        Cell.HeaderBase(TSR, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch)

    static member HeaderTAN(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let cellValue = header.TryOA() |> Option.map (fun oa -> oa.TermAccessionShort) |> Option.defaultValue ""
        let setter = fun (s: string) -> headerTANSetter(columnIndex, s, header, dispatch)
        Cell.HeaderBase(TAN, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch)

    static member Empty() =
        Html.td [ cellStyle []; prop.readOnly true; prop.children [
            Html.div [
                prop.style [style.height (length.perc 100)]
                prop.className "is-flex is-align-items-center is-justify-content-center"
                prop.children [
                    Html.div "-"
                ]
            ]
        ]]

    [<ReactComponent>]
    static member private BodyBase(columnType: ColumnType, cellValue: string, setter: string -> unit, index: (int*int), cell: CompositeCell, model: Model, dispatch, ?oasetter: OntologyAnnotation -> unit) =
        let columnIndex, rowIndex = index
        let state = model.SpreadsheetModel
        let state_cell, setState_cell = React.useState(CellState.init(cellValue))
        React.useEffect((fun _ -> setState_cell {state_cell with Value = cellValue}), [|box cellValue|])
        let isSelected = state.SelectedCells.Contains index
        let makeIdle() = setState_cell {state_cell with CellMode = Idle}
        Html.td [
            prop.key $"Cell_{state.ActiveView.TableIndex}-{columnIndex}-{rowIndex}"
            cellStyle [
                if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
            ]
            prop.onContextMenu <| ContextMenu.onContextMenu (index, model, dispatch)
            prop.children [
                Html.div [
                    cellInnerContainerStyle []
                    prop.onDoubleClick(fun e ->
                        e.preventDefault()
                        e.stopPropagation() 
                        if state_cell.IsIdle then setState_cell {state_cell with CellMode = Active}
                        UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
                    )
                    if state_cell.IsIdle then prop.onClick <| EventPresets.onClickSelect(index, state_cell, state.SelectedCells, model, dispatch)
                    prop.onMouseDown(fun e -> if state_cell.IsIdle && e.shiftKey then e.preventDefault())
                    prop.children [
                        match state_cell.CellMode with
                        | Active ->
                            // Update change to mainState and exit active input.
                            if oasetter.IsSome then 
                                let oa = cell.ToOA()
                                let onBlur = fun e -> makeIdle()
                                let onEscape = fun e -> makeIdle()
                                let onEnter = fun e -> makeIdle()
                                let headerOA = state.ActiveTable.Headers.[columnIndex].TryOA()
                                let setter = fun (oa: OntologyAnnotation option) -> 
                                    if oa.IsSome then oasetter.Value oa.Value else setter ""
                                Components.TermSearch.Input(setter, input=oa, fullwidth=true, ?parent'=headerOA, displayParent=false, debounceSetter=1000, onBlur=onBlur, onEscape=onEscape, onEnter=onEnter, autofocus=true, borderRadius=0, border="unset", searchableToggle=true)
                            else
                                let updateMainStateTable = fun (s: string) -> 
                                    // Only update if changed
                                    if s <> cellValue then
                                        setter s
                                    makeIdle()
                                CellInputElement(false, false, updateMainStateTable, setState_cell, state_cell, cellValue, columnType)
                        | Idle ->
                            if columnType = Main then
                                compositeCellDisplay cell
                            else
                                basicValueDisplayCell cellValue
                    ]
                ]
            ]
        ]

    static member Body(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.GetContent().[0]
        let setter = fun (s: string) ->
            let nextCell = cell.UpdateMainField s
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        let oasetter = if cell.isTerm then CellAux.oasetter(index, cell, dispatch) |> Some else None
        Cell.BodyBase(Main, cellValue, setter, index, cell, model, dispatch, ?oasetter=oasetter)

    static member BodyUnit(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.GetContent().[1]
        let setter = fun (s: string) ->
            let oa = cell.ToOA() 
            let newName = if s = "" then None else Some s
            let nextOA = {oa with Name = newName }
            let nextCell = cell.UpdateWithOA nextOA
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        let oasetter = if cell.isUnitized then CellAux.oasetter(index, cell, dispatch) |> Some else None
        Cell.BodyBase(Unit, cellValue, setter, index, cell, model, dispatch, ?oasetter=oasetter)

    static member BodyTSR(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let contentIndex = if cell.isUnitized then 2 else 1
        let cellValue = cell.GetContent().[contentIndex]
        let setter = fun (s: string) ->
            let oa = cell.ToOA() 
            let newTSR = if s = "" then None else Some s
            let nextOA = {oa with TermSourceREF = newTSR}
            let nextCell = cell.UpdateWithOA nextOA
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(TSR, cellValue, setter, index, cell, model, dispatch)

    static member BodyTAN(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let contentIndex = if cell.isUnitized then 3 else 2
        let cellValue = cell.GetContent().[contentIndex]
        let setter = fun (s: string) ->
            let oa = cell.ToOA() 
            let newTAN = if s = "" then None else Some s
            let nextOA = {oa with TermAccessionNumber = newTAN}
            let nextCell = cell.UpdateWithOA nextOA
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(TAN, cellValue, setter, index, cell, model, dispatch)
        