module Spreadsheet.Cells

open Feliz
open Feliz.Bulma
open Fable.Core

open Spreadsheet
open MainComponents
open Messages
open Shared
open ARCtrl
open Components
open Model

module private CellComponents =

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
        match header.TryOA(), s with
        | Some oa, "" -> oa.TermAccessionNumber <- None;  Some oa
        | Some oa, s1 ->  oa.TermAccessionNumber <- Some s1;  Some oa
        | None, _ -> None
        |> Option.map header.UpdateWithOA
        |> Option.iter (fun nextHeader -> Msg.UpdateHeader (columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch)

    let oasetter (index, nextCell: CompositeCell, dispatch) = Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        

open CellComponents
open CellAux

module private EventPresets =

    open Shared

    let onClickSelect (index: int*int, isIdle:bool, selectedCells: Set<int*int>, model:Model, dispatch)=
        fun (e: Browser.Types.MouseEvent) ->
            // don't select cell if active(editable)
            if isIdle then
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
                if not set.IsEmpty && model.SpreadsheetModel.TableViewIsActive() then
                    let oa = 
                        let columnIndex = set |> Seq.minBy fst |> fst 
                        let column = model.SpreadsheetModel.ActiveTable.GetColumn(columnIndex)
                        if column.Header.IsTermColumn then
                            column.Header.ToTerm() |> Some //ToOA
                        else
                            None
                    TermSearch.UpdateParentTerm oa |> TermSearchMsg |> dispatch

open Shared
open Fable.Core.JsInterop
open CellStyles

type Cell =

    [<ReactComponent>]
    static member CellInputElement (input: string, isHeader: bool, isReadOnly: bool, setter: string -> unit, makeIdle) =
        let state, setState = React.useState(input)
        React.useEffect((fun () -> setState input), [|box input|])
        let debounceStorage = React.useRef(newDebounceStorage())
        let loading, setLoading = React.useState(false)
        let dsetter (inp) = debouncel debounceStorage.current "TextChange" 1000 setLoading setter inp
        let input = 
            Bulma.control.div [
                Bulma.control.isExpanded
                if loading then Bulma.control.isLoading
                prop.children [
                    Bulma.input.text [
                        prop.defaultValue input
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
                        ]
                        prop.onBlur(fun _ -> 
                            if isHeader then setter state; 
                            makeIdle()
                        )
                        prop.onKeyDown(fun e ->
                            e.stopPropagation()
                            match e.which with
                            | 13. -> //enter
                                if isHeader then setter state
                                debounceStorage.current.ClearAndRun()
                                makeIdle()
                            | 27. -> //escape
                                debounceStorage.current.Clear()
                                makeIdle()
                            | _ -> ()
                        )
                        // Only change cell value while typing to increase performance. 
                        prop.onChange(fun e -> 
                            if isHeader then setState e else dsetter e
                        )
                    ]
                ]
            ]
        Bulma.field.div [
            Bulma.field.hasAddons
            prop.className "is-flex-grow-1 m-0"
            prop.children [ input ]           
        ]

    [<ReactComponent>]
    static member HeaderBase(columnType: ColumnType, setter: string -> unit, cellValue: string, columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let state = model.SpreadsheetModel
        let isReadOnly = columnType = Unit
        let makeIdle() = UpdateActiveCell None |> SpreadsheetMsg |> dispatch
        let makeActive() = UpdateActiveCell (Some (!^columnIndex, columnType)) |> SpreadsheetMsg |> dispatch
        let isIdle = state.CellIsIdle (!^columnIndex, columnType)
        let isActive = not isIdle
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
                        if isIdle then makeActive()
                    )
                    prop.children [
                        if isActive then
                            Cell.CellInputElement(cellValue, true, isReadOnly, setter, makeIdle)
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
                    let updatedOA =
                        match nextHeader.TryOA() ,header.TryOA() with
                        | Some oa1, Some oa2 -> oa1.TermAccessionNumber <- oa2.TermAccessionNumber; oa1.TermSourceREF <- oa2.TermSourceREF; oa1
                        | _ -> failwith "this should never happen"
                    nextHeader <- nextHeader.UpdateWithOA updatedOA
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
                prop.style [style.height (length.perc 100); style.cursor.notAllowed; style.userSelect.none]
                prop.className "is-flex is-align-items-center is-justify-content-center has-background-grey-lighter"
                prop.children [
                    Html.div "-"
                ]
            ]
        ]]

    [<ReactComponent>]
    static member BodyBase(columnType: ColumnType, cellValue: string, setter: string -> unit, index: (int*int), model: Model, dispatch, ?oasetter: {|oa: OntologyAnnotation; setter: OntologyAnnotation -> unit|}, ?displayValue, ?readonly: bool) =
        let readonly = defaultArg readonly false
        let columnIndex, rowIndex = index
        let state = model.SpreadsheetModel
        let isSelected = state.SelectedCells.Contains index
        let isIdle = state.CellIsIdle (!^index, columnType)
        let isActive = not isIdle
        let ref = React.useElementRef()
        let displayValue = defaultArg displayValue cellValue
        let makeIdle() = 
            UpdateActiveCell None |> SpreadsheetMsg |> dispatch
            let ele = Browser.Dom.document.getElementById("SPREADSHEET_MAIN_VIEW")
            ele.focus()
        let makeActive() = UpdateActiveCell (Some (!^index, columnType)) |> SpreadsheetMsg |> dispatch
        React.useEffect((fun () -> 
            if isSelected then
                let options = createEmpty<Browser.Types.ScrollIntoViewOptions>
                options.behavior <- Browser.Types.ScrollBehavior.Auto
                options.``inline`` <- Browser.Types.ScrollAlignment.Nearest
                options.block <- Browser.Types.ScrollAlignment.Nearest
                if ref.current.IsSome then ref.current.Value.scrollIntoView(options)), 
                [|box isSelected|]
        )
        Html.td [
            prop.key $"Cell_{state.ActiveView.TableIndex}-{columnIndex}-{rowIndex}"
            cellStyle [
                if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
            ]
            prop.ref ref
            prop.onContextMenu <| ContextMenu.onContextMenu (index, model, dispatch)
            prop.children [
                Html.div [
                    cellInnerContainerStyle []
                    if not readonly then
                        prop.onDoubleClick(fun e ->
                            e.preventDefault()
                            e.stopPropagation()
                            if isIdle then makeActive()
                            UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
                        )
                        if isIdle then prop.onClick <| EventPresets.onClickSelect(index, isIdle, state.SelectedCells, model, dispatch)
                    prop.onMouseDown(fun e -> if isIdle && e.shiftKey then e.preventDefault())
                    prop.children [
                        if isActive then
                            // Update change to mainState and exit active input.
                            if oasetter.IsSome then 
                                let oa = oasetter.Value.oa
                                let onBlur = fun e -> makeIdle();
                                let onEscape = fun e -> makeIdle();
                                let onEnter = fun e -> makeIdle(); 
                                let setter = fun (oa: OntologyAnnotation option) ->
                                    let oa = oa |> Option.defaultValue (OntologyAnnotation())
                                    oasetter.Value.setter oa
                                let headerOA = if state.TableViewIsActive() then state.ActiveTable.Headers.[columnIndex].TryOA() else None
                                Components.TermSearch.Input(setter, input=oa, fullwidth=true, ?parent=headerOA, displayParent=false, debounceSetter=1000, onBlur=onBlur, onEscape=onEscape, onEnter=onEnter, autofocus=true, borderRadius=0, border="unset", searchableToggle=true, minWidth=length.px 400)
                            else
                                Cell.CellInputElement(cellValue, false, false, setter, makeIdle)
                        else
                            if columnType = Main && oasetter.IsSome then
                                CellStyles.compositeCellDisplay oasetter.Value.oa displayValue
                            else
                                basicValueDisplayCell displayValue
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member BodySelect(value: string, setter: string -> unit, values: #seq<string>, index: (int*int), model: Model.Model, dispatch) =
        let columnIndex, rowIndex = index
        let state = model.SpreadsheetModel
        let ref = React.useElementRef()
        Html.td [
            prop.key $"Cell_Select_{columnIndex}_{rowIndex}"
            cellStyle []
            prop.ref ref
            prop.onContextMenu <| ContextMenu.onContextMenu (index, model, dispatch)
            prop.children [
                Html.div [
                    cellInnerContainerStyle []
                    prop.children [
                        Html.div [
                            prop.className "select w-full"
                            prop.children [
                                Html.select [
                                    prop.className "!rounded-none w-full"
                                    prop.value value
                                    prop.onChange(fun (e: string) -> setter e)
                                    prop.children [
                                        for v in values do
                                            Html.option [
                                                prop.value v
                                                prop.text v
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    static member Body(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.GetContent().[0]
        let setter = fun (s: string) ->
            let nextCell = cell.UpdateMainField s
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        let oasetter =
            if cell.isTerm then
                {|
                    oa = cell.ToOA()
                    setter =
                        fun (oa:OntologyAnnotation) ->
                            let nextCell = cell.UpdateWithOA oa
                            CellAux.oasetter(index, nextCell, dispatch)
                |}
                |> Some
            else
                None
        let displayValue = cell.ToString()
        Cell.BodyBase(Main, cellValue, setter, index, model, dispatch, ?oasetter=oasetter, displayValue=displayValue)

    static member BodyUnit(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.GetContent().[1]
        let setter = fun (s: string) ->
            let oa = cell.ToOA()
            let newName = if s = "" then None else Some s
            oa.Name <- newName
            let nextCell = cell.UpdateWithOA oa
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        let oasetter =
            if cell.isUnitized then
                {|
                    oa = cell.ToOA()
                    setter =
                        fun (oa:OntologyAnnotation) ->
                            let nextCell = cell.UpdateWithOA oa
                            CellAux.oasetter(index, nextCell, dispatch)
                |}
                |> Some
            else
                None
        let displayValue = cell.ToString()
        Cell.BodyBase(Unit, cellValue, setter, index, model, dispatch, ?oasetter=oasetter, displayValue=displayValue)

    static member BodyTSR(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let contentIndex = if cell.isUnitized then 2 else 1
        let cellValue = cell.GetContent().[contentIndex]
        let setter = fun (s: string) ->
            let oa = cell.ToOA() 
            let newTSR = if s = "" then None else Some s
            oa.TermSourceREF <- newTSR
            let nextCell = cell.UpdateWithOA oa
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(TSR, cellValue, setter, index, model, dispatch)

    static member BodyTAN(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let contentIndex = if cell.isUnitized then 3 else 2
        let cellValue = cell.GetContent().[contentIndex]
        let setter = fun (s: string) ->
            let oa = cell.ToOA() 
            let newTAN = if s = "" then None else Some s
            oa.TermAccessionNumber <- newTAN
            let nextCell = cell.UpdateWithOA oa
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(TAN, cellValue, setter, index, model, dispatch)
        