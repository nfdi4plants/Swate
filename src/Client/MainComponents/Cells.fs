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

    let headerTSRSetter (columnIndex: int, s: string, header: CompositeHeader, dispatch) =
        log (s, header)
        match header.TryOA(), s with
        | Some oa, "" -> oa.TermSourceREF <- None;  Some oa
        | Some oa, s1 ->  oa.TermSourceREF <- Some s1;  Some oa
        | None, _ -> None
        |> fun s -> log s; s
        |> Option.map header.UpdateWithOA
        |> fun s -> log s; s
        |> Option.iter (fun nextHeader -> Msg.UpdateHeader (columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch)

    let headerTANSetter (columnIndex: int, s: string, header: CompositeHeader, dispatch) =
        match header.TryOA(), s with
        | Some oa, "" -> oa.TermAccessionNumber <- None;  Some oa
        | Some oa, s1 ->  oa.TermAccessionNumber <- Some s1;  Some oa
        | None, _ -> None
        |> Option.map header.UpdateWithOA
        |> Option.iter (fun nextHeader -> Msg.UpdateHeader (columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch)

    let oasetter (index, nextCell: CompositeCell, dispatch) = Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch

    let contextMenuController index model dispatch = if model.SpreadsheetModel.TableViewIsActive() then ContextMenu.Table.onContextMenu (index, model, dispatch) else ContextMenu.DataMap.onContextMenu (index, model, dispatch)

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
                            debounceStorage.current.ClearAndRun()
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
    static member HeaderBase(columnType: ColumnType, setter: string -> unit, cellValue: string, columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch, ?readonly: bool) =
        let readonly = defaultArg readonly false
        let state = model.SpreadsheetModel
        let isReadOnly = (columnType = Unit || readonly)
        let makeIdle() = UpdateActiveCell None |> SpreadsheetMsg |> dispatch
        let makeActive() = UpdateActiveCell (Some (!^columnIndex, columnType)) |> SpreadsheetMsg |> dispatch
        let isIdle = state.CellIsIdle (!^columnIndex, columnType)
        let isActive = not isIdle
        Html.th [
            prop.key $"Header_{state.ActiveView.ViewIndex}-{columnIndex}-{columnType}"
            prop.id $"Header_{columnIndex}_{columnType}"
            prop.readOnly readonly
            cellStyle []
            prop.onContextMenu (CellAux.contextMenuController (columnIndex, -1) model dispatch)
            prop.className [
                "w-[300px]" // horizontal resize property sets width, but cannot override style.width. Therefore we set width as class, which makes it overridable by resize property.
                if columnType.IsRefColumn then
                    "bg-gray-300 dark:bg-gray-700"
                else
                    "bg-white dark:bg-black-800"
            ]
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
                            basicValueDisplayCell cellValue true
                        if columnType = Main && not header.IsSingleColumn then 
                            extendHeaderButton(state_extend, columnIndex, setState_extend)
                    ]
                ]
            ]
        ]

    static member Header(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch, ?readonly: bool) =
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
        Cell.HeaderBase(Main, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch, ?readonly=readonly)

    static member HeaderUnit(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch, ?readonly) =
        let cellValue = "Unit"
        let setter = fun (s: string) -> ()
        Cell.HeaderBase(Unit, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch, ?readonly=readonly)

    static member HeaderTSR(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch, ?readonly) =
        let cellValue = header.TryOA() |> Option.map (fun oa -> oa.TermSourceREF) |> Option.flatten |> Option.defaultValue ""
        let setter = fun (s: string) -> headerTSRSetter(columnIndex, s, header, dispatch)
        Cell.HeaderBase(TSR, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch, ?readonly=readonly)

    static member HeaderTAN(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch, ?readonly) =
        let cellValue = header.TryOA() |> Option.map (fun oa -> oa.TermAccessionShort) |> Option.defaultValue ""
        let setter = fun (s: string) -> headerTANSetter(columnIndex, s, header, dispatch)
        Cell.HeaderBase(TAN, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch, ?readonly=readonly)

    static member HeaderDataSelector(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let ct = ColumnType.DataSelector
        let cellValue = ct.ToColumnHeader()
        let setter = fun _ -> ()
        Cell.HeaderBase(ct, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch, true)

    static member HeaderDataFormat(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let ct = ColumnType.DataFormat
        let cellValue = ct.ToColumnHeader()
        let setter = fun _ -> ()
        Cell.HeaderBase(ct, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch, true)

    static member HeaderDataSelectorFormat(columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch) =
        let ct = ColumnType.DataSelectorFormat
        let cellValue = ct.ToColumnHeader()
        let setter = fun _ -> ()
        Cell.HeaderBase(ct, setter, cellValue, columnIndex, header, state_extend, setState_extend, model, dispatch, true)
  
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
    static member BodyBase(columnType: ColumnType, cellValue: string, setter: string -> unit, index: (int*int), model: Model, dispatch, ?oasetter: {|oa: OntologyAnnotation; setter: OntologyAnnotation -> unit|}, ?displayValue, ?readonly: bool, ?tooltip: string) =
        let readonly = defaultArg readonly false
        let columnIndex, rowIndex = index
        let state = model.SpreadsheetModel
        let isSelected = state.SelectedCells.Contains index
        let isIdle = state.CellIsIdle (!^index, columnType)
        let isActive = not isIdle
        let displayValue = defaultArg displayValue cellValue
        let makeIdle() = 
            UpdateActiveCell None |> SpreadsheetMsg |> dispatch
            let ele = Browser.Dom.document.getElementById("SPREADSHEET_MAIN_VIEW")
            ele.focus()
        let makeActive() = UpdateActiveCell (Some (!^index, columnType)) |> SpreadsheetMsg |> dispatch
        let cellId = Controller.Cells.mkCellId columnIndex rowIndex state
        let ref = React.useElementRef()
        // ---
        // Not sure if we can delete this comment,
        // i am not 100% happy with the scroll logic.
        // Previously the code in this comment made the browser scroll zu the cell whenever it was updated to selected with arrow key navigation.

        // I had some issues (and still have some issues) with this logic and the lazy load table.1
        // But for now i updated the logic to use focus.
        //
        // For now i would leave the comment to reenable the logic when working for a permanent clean solution
        // ---
        //React.useEffect((fun _ ->
        //    if ref.current.IsSome && CellStyles.ScrollToCellId = Some cellId then
        //        //let options = createEmpty<Browser.Types.ScrollIntoViewOptions>
        //        //options.behavior <- Browser.Types.ScrollBehavior.Auto
        //        //options.``inline`` <- Browser.Types.ScrollAlignment.Nearest
        //        //options.block <- Browser.Types.ScrollAlignment.Nearest
        //        ref.current.Value.focus()
        //), [|box CellStyles.ScrollToCellId|])
        Html.td [
            if tooltip.IsSome then prop.title tooltip.Value
            prop.key cellId
            prop.id cellId // This is used for scrollintoview on keyboard navigation
            prop.ref ref
            prop.tabIndex 0 
            cellStyle [
                if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
            ]
            prop.readOnly readonly
            //prop.ref ref
            prop.onContextMenu (CellAux.contextMenuController index model dispatch)
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
                                Components.TermSearch.Input(setter, input=oa, fullwidth=true, ?parent=headerOA, displayParent=false, onBlur=onBlur, onEscape=onEscape, onEnter=onEnter, autofocus=true, borderRadius=0, border="unset", searchableToggle=true, minWidth=length.px 400)
                            else
                                Cell.CellInputElement(cellValue, false, false, setter, makeIdle)
                        else
                            if columnType = Main && oasetter.IsSome then
                                CellStyles.compositeCellDisplay oasetter.Value.oa displayValue
                            else
                                basicValueDisplayCell displayValue false
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
            prop.onContextMenu (CellAux.contextMenuController index model dispatch)
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
        let cellValue = cell.GetContentSwate().[0]
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
        let cellValue = cell.ToOA().NameText
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
        let cellValue = cell.ToOA().TermSourceREF |> Option.defaultValue ""
        let setter = fun (s: string) ->
            let oa = cell.ToOA() 
            let newTSR = if s = "" then None else Some s
            oa.TermSourceREF <- newTSR
            let nextCell = cell.UpdateWithOA oa
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(TSR, cellValue, setter, index, model, dispatch)

    static member BodyTAN(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.ToOA().TermAccessionNumber |> Option.defaultValue ""
        let setter = fun (s: string) ->
            let oa = cell.ToOA() 
            let newTAN = if s = "" then None else Some s
            oa.TermAccessionNumber <- newTAN
            let nextCell = cell.UpdateWithOA oa
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(TAN, cellValue, setter, index, model, dispatch)

    static member BodyDataSelector(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let data = cell.AsData
        let cellValue = data.Selector |> Option.defaultValue ""
        let setter = fun (s: string) ->
            let next = if s = "" then None else Some s
            data.Selector <- next
            let nextCell = cell.UpdateWithData data
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(ColumnType.DataSelector, cellValue, setter, index, model, dispatch)

    static member BodyDataFormat(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let data = cell.AsData
        let cellValue = data.Format |> Option.defaultValue ""
        let setter = fun (s: string) ->
            let next = if s = "" then None else Some s
            data.Format <- next
            let nextCell = cell.UpdateWithData data
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(ColumnType.DataFormat, cellValue, setter, index, model, dispatch)

    static member BodyDataSelectorFormat(index: (int*int), cell: CompositeCell, model: Model, dispatch) =
        let data = cell.AsData
        let cellValue = data.SelectorFormat |> Option.defaultValue ""
        let setter = fun (s: string) ->
            let next = if s = "" then None else Some s
            data.SelectorFormat <- next
            let nextCell = cell.UpdateWithData data
            Msg.UpdateCell (index, nextCell) |> SpreadsheetMsg |> dispatch
        Cell.BodyBase(ColumnType.DataSelectorFormat, cellValue, setter, index, model, dispatch)
        