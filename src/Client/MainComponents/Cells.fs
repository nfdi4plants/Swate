module Spreadsheet.Cells

open Feliz
open Feliz.DaisyUI
open Fable.Core
open Spreadsheet
open MainComponents
open Messages
open Swate.Components.Shared
open ARCtrl
open Components
open Model
open Modals

module private CellAux =
    let headerTSRSetter (columnIndex: int, s: string, header: CompositeHeader, dispatch) =
        log (s, header)

        match header.TryOA(), s with
        | Some oa, "" ->
            oa.TermSourceREF <- None
            Some oa
        | Some oa, s1 ->
            oa.TermSourceREF <- Some s1
            Some oa
        | None, _ -> None
        |> fun s ->
            log s
            s
        |> Option.map header.UpdateWithOA
        |> fun s ->
            log s
            s
        |> Option.iter (fun nextHeader -> Msg.UpdateHeader(columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch)

    let headerTANSetter (columnIndex: int, s: string, header: CompositeHeader, dispatch) =
        match header.TryOA(), s with
        | Some oa, "" ->
            oa.TermAccessionNumber <- None
            Some oa
        | Some oa, s1 ->
            oa.TermAccessionNumber <- Some s1
            Some oa
        | None, _ -> None
        |> Option.map header.UpdateWithOA
        |> Option.iter (fun nextHeader -> Msg.UpdateHeader(columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch)

    let oasetter (index, nextCell: CompositeCell, dispatch) =
        Msg.UpdateCell(index, nextCell) |> SpreadsheetMsg |> dispatch

    let contextMenuController index model dispatch =
        if model.SpreadsheetModel.TableViewIsActive() then
            ContextMenu.Table.onContextMenu (index, dispatch)
        else
            ContextMenu.DataMap.onContextMenu (index, dispatch)

    let buildingBlockModalController index dispatch =
        CompositeCollumnModal.onKeyDown (index, dispatch)

open CellAux

module private EventPresets =
    open Swate.Components.Shared

    let onClickSelect (index: int * int, isIdle: bool, selectedCells: Set<int * int>, model: Model, dispatch) =
        fun (e: Browser.Types.MouseEvent) ->
            // don't select cell if active(editable)
            if isIdle then
                let set =
                    match e.shiftKey, selectedCells.Count with
                    | true, 0 -> selectedCells
                    | true, _ ->
                        let createSetOfIndex (columnMin: int, columnMax, rowMin: int, rowMax: int) =
                            [
                                for c in columnMin..columnMax do
                                    for r in rowMin..rowMax do
                                        c, r
                            ]
                            |> Set.ofList

                        let source = selectedCells.MinimumElement
                        let target = index

                        let columnMin, columnMax =
                            System.Math.Min(fst source, fst target), System.Math.Max(fst source, fst target)

                        let rowMin, rowMax =
                            System.Math.Min(snd source, snd target), System.Math.Max(snd source, snd target)

                        let set = createSetOfIndex (columnMin, columnMax, rowMin, rowMax)
                        set
                    | false, _ ->
                        let next =
                            if selectedCells = Set([ index ]) then
                                Set.empty
                            else
                                Set([ index ])

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

open Swate.Components.Shared
open Fable.Core.JsInterop

type Cell =

    [<ReactComponent>]
    static member CellInputElement
        (input: string, isHeader: bool, isReadOnly: bool, setter: string -> unit, makeIdle, index, dispatch)
        =
        let state, setState = React.useState (input)
        React.useEffect ((fun () -> setState input), [| box input |])
        let debounceStorage = React.useRef (newDebounceStorage ())
        let loading, setLoading = React.useState (false)

        let dsetter (inp) =
            debouncel debounceStorage.current "TextChange" 1000 setLoading setter inp

        Html.label [
            prop.className
                "input flex flex-row items-center gap-2 grow m-0 w-full h-full rounded-none bg-transparent border-0"
            prop.children [
                Html.input [
                    prop.defaultValue input
                    prop.readOnly isReadOnly
                    prop.autoFocus true
                    prop.className "bg-transparent w-full h-full"
                    prop.onBlur (fun _ ->
                        if isHeader then
                            setter state

                        debounceStorage.current.ClearAndRun()
                        makeIdle ())
                    prop.onKeyDown (fun e ->
                        e.stopPropagation ()

                        match e.code with
                        | Swate.Components.kbdEventCode.enter ->
                            if isHeader then
                                setter state
                                debounceStorage.current.ClearAndRun()

                                ModalState.TableModals.EditColumn(fst (index))
                                |> Model.ModalState.ModalTypes.TableModal
                                |> Some
                                |> Messages.UpdateModal
                                |> dispatch

                                makeIdle ()
                            else
                                debounceStorage.current.ClearAndRun()
                                makeIdle ()
                                CellAux.buildingBlockModalController index dispatch

                        | Swate.Components.kbdEventCode.escape -> //escape
                            debounceStorage.current.Clear()
                            makeIdle ()
                        | _ -> ())
                    // Only change cell value while typing to increase performance.
                    prop.onChange (fun e -> if isHeader then setState e else dsetter e)
                ]
                if loading then
                    Daisy.loading []
            ]
        ]

    [<ReactComponent>]
    static member HeaderBase
        (
            columnType: ColumnType,
            setter: string -> unit,
            cellValue: string,
            columnIndex: int,
            header: CompositeHeader,
            state_extend: Set<int>,
            setState_extend,
            model: Model,
            dispatch,
            ?readonly: bool
        ) =
        let readonly = defaultArg readonly false
        let state = model.SpreadsheetModel
        let isReadOnly = (columnType = Unit || readonly)

        let makeIdle () =
            UpdateActiveCell None |> SpreadsheetMsg |> dispatch

        let makeActive () =
            UpdateActiveCell(Some(!^columnIndex, columnType)) |> SpreadsheetMsg |> dispatch

        let isIdle = state.CellIsIdle(!^columnIndex, columnType)
        let isActive = not isIdle

        Html.th [
            prop.key $"Header_{state.ActiveView.ViewIndex}-{columnIndex}-{columnType}"
            prop.id $"Header_{columnIndex}_{columnType}"
            prop.readOnly readonly
            CellStyles.cellStyle [
                "resize-x w-[300px] truncate" // horizontal resize property sets width, but cannot override style.width. Therefore we set width as class, which makes it overridable by resize property.
                if columnType.IsRefColumn then
                    "bg-base-200"
            ]
            prop.onContextMenu (CellAux.contextMenuController (columnIndex, -1) model dispatch)
            prop.children [
                Html.div [
                    if not isReadOnly then
                        prop.onClick (fun e ->
                            e.preventDefault ()
                            e.stopPropagation ()
                            UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch

                            if isIdle then
                                makeActive ())
                    prop.children [
                        if isActive then
                            Cell.CellInputElement(
                                cellValue,
                                true,
                                isReadOnly,
                                setter,
                                makeIdle,
                                (columnIndex, 0),
                                dispatch
                            )
                        else
                            let cellValue = // shadow cell value for tsr and tan to add columnType
                                match columnType with
                                | TSR
                                | TAN -> $"{columnType} ({cellValue})"
                                | _ -> cellValue

                            let extendableButtonOpt =
                                if
                                    (columnType = Main && not header.IsSingleColumn)
                                    || (columnType = Main && header.IsDataColumn)
                                then
                                    CellStyles.ExtendHeaderButton(state_extend, columnIndex, setState_extend)
                                    |> Some
                                else
                                    None

                            CellStyles.BasicValueDisplayCell(cellValue, extendableButtonOpt)
                    ]
                ]
            ]
        ]

    static member Header
        (
            columnIndex: int,
            header: CompositeHeader,
            state_extend: Set<int>,
            setState_extend,
            model: Model,
            dispatch,
            ?readonly: bool
        ) =
        let cellValue = header.ToString()

        let setter =
            fun (s: string) ->
                let mutable nextHeader = CompositeHeader.OfHeaderString s
                // update header with ref columns if term column
                if header.IsTermColumn && not header.IsFeaturedColumn then
                    let updatedOA =
                        match nextHeader.TryOA(), header.TryOA() with
                        | Some oa1, Some oa2 ->
                            oa1.TermAccessionNumber <- oa2.TermAccessionNumber
                            oa1.TermSourceREF <- oa2.TermSourceREF
                            oa1
                        | _ -> failwith "this should never happen"

                    nextHeader <- nextHeader.UpdateWithOA updatedOA

                Msg.UpdateHeader(columnIndex, nextHeader) |> SpreadsheetMsg |> dispatch

        Cell.HeaderBase(
            Main,
            setter,
            cellValue,
            columnIndex,
            header,
            state_extend,
            setState_extend,
            model,
            dispatch,
            ?readonly = readonly
        )

    static member HeaderUnit
        (
            columnIndex: int,
            header: CompositeHeader,
            state_extend: Set<int>,
            setState_extend,
            model: Model,
            dispatch,
            ?readonly
        ) =
        let cellValue = "Unit"
        let setter = fun (s: string) -> ()

        Cell.HeaderBase(
            Unit,
            setter,
            cellValue,
            columnIndex,
            header,
            state_extend,
            setState_extend,
            model,
            dispatch,
            ?readonly = readonly
        )

    static member HeaderTSR
        (
            columnIndex: int,
            header: CompositeHeader,
            state_extend: Set<int>,
            setState_extend,
            model: Model,
            dispatch,
            ?readonly
        ) =
        let cellValue =
            header.TryOA()
            |> Option.map (fun oa -> oa.TermSourceREF)
            |> Option.flatten
            |> Option.defaultValue ""

        let setter = fun (s: string) -> headerTSRSetter (columnIndex, s, header, dispatch)

        Cell.HeaderBase(
            TSR,
            setter,
            cellValue,
            columnIndex,
            header,
            state_extend,
            setState_extend,
            model,
            dispatch,
            ?readonly = readonly
        )

    static member HeaderTAN
        (
            columnIndex: int,
            header: CompositeHeader,
            state_extend: Set<int>,
            setState_extend,
            model: Model,
            dispatch,
            ?readonly
        ) =
        let cellValue =
            header.TryOA()
            |> Option.map (fun oa -> oa.TermAccessionShort)
            |> Option.defaultValue ""

        let setter = fun (s: string) -> headerTANSetter (columnIndex, s, header, dispatch)

        Cell.HeaderBase(
            TAN,
            setter,
            cellValue,
            columnIndex,
            header,
            state_extend,
            setState_extend,
            model,
            dispatch,
            ?readonly = readonly
        )

    static member HeaderDataSelector
        (columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch)
        =
        let ct = ColumnType.DataSelector
        let cellValue = ct.ToColumnHeader()
        let setter = fun _ -> ()

        Cell.HeaderBase(
            ct,
            setter,
            cellValue,
            columnIndex,
            header,
            state_extend,
            setState_extend,
            model,
            dispatch,
            true
        )

    static member HeaderDataFormat
        (columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch)
        =
        let ct = ColumnType.DataFormat
        let cellValue = ct.ToColumnHeader()
        let setter = fun _ -> ()

        Cell.HeaderBase(
            ct,
            setter,
            cellValue,
            columnIndex,
            header,
            state_extend,
            setState_extend,
            model,
            dispatch,
            true
        )

    static member HeaderDataSelectorFormat
        (columnIndex: int, header: CompositeHeader, state_extend: Set<int>, setState_extend, model: Model, dispatch)
        =
        let ct = ColumnType.DataSelectorFormat
        let cellValue = ct.ToColumnHeader()
        let setter = fun _ -> ()

        Cell.HeaderBase(
            ct,
            setter,
            cellValue,
            columnIndex,
            header,
            state_extend,
            setState_extend,
            model,
            dispatch,
            true
        )

    static member Empty() =
        Html.td [
            CellStyles.cellStyle []
            prop.readOnly true
            prop.children [
                Html.div [
                    prop.style [
                        style.height (length.perc 100)
                        style.cursor.notAllowed
                        style.userSelect.none
                    ]
                    prop.className "flex grow items-center justify-center bg-base-300 opacity-60"
                    prop.children [ Html.div "-" ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member BodyBase
        (
            columnType: ColumnType,
            cellValue: string,
            setter: string -> unit,
            index: (int * int),
            model: Model,
            dispatch,
            ?oasetter:
                {|
                    oa: OntologyAnnotation
                    setter: OntologyAnnotation -> unit
                |},
            ?displayValue,
            ?readonly: bool,
            ?tooltip: string
        ) =
        let readonly = defaultArg readonly false
        let columnIndex, rowIndex = index
        let state = model.SpreadsheetModel
        let isSelected = state.SelectedCells.Contains index
        let isIdle = state.CellIsIdle(!^index, columnType)
        let isActive = not isIdle
        let displayValue = defaultArg displayValue cellValue

        let makeIdle () =
            UpdateActiveCell None |> SpreadsheetMsg |> dispatch
            let ele = Browser.Dom.document.getElementById ("SPREADSHEET_MAIN_VIEW")
            ele.focus ()

        let makeActive () =
            UpdateActiveCell(Some(!^index, columnType)) |> SpreadsheetMsg |> dispatch

        let cellId = Controller.Cells.mkCellId columnIndex rowIndex state
        // let ref = React.useElementRef()
        Html.td [
            if tooltip.IsSome then
                prop.title tooltip.Value
            prop.id cellId // This is used for scrollintoview on keyboard navigation
            // prop.ref ref
            prop.tabIndex 0
            CellStyles.cellStyle [
                if isSelected then
                    "!bg-base-300"
            ]
            prop.readOnly readonly
            prop.onContextMenu (CellAux.contextMenuController index model dispatch)
            prop.onClick (fun e ->
                e.preventDefault ()
                e.stopPropagation ()

                if not readonly && isIdle then
                    makeActive ()

                if isIdle then
                    EventPresets.onClickSelect (index, isIdle, state.SelectedCells, model, dispatch) e)
            prop.onDoubleClick (fun e ->
                e.preventDefault ()
                e.stopPropagation ()

                if not readonly then
                    if isIdle then
                        makeActive ()

                    UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch)
            prop.onKeyDown (fun e ->
                if e.code = Swate.Components.kbdEventCode.enter then
                    buildingBlockModalController index dispatch)
            //if isIdle then prop.onClick <| EventPresets.onClickSelect(index, isIdle, state.SelectedCells, model, dispatch)
            prop.onMouseDown (fun e ->
                if isIdle then
                    e.preventDefault ())
            prop.children [
                if isActive then
                    // Update change to mainState and exit active input.
                    if oasetter.IsSome then
                        let input = oasetter.Value.oa.ToTerm() |> Some
                        let onBlur = fun e -> promise { makeIdle () }

                        let onKeyDown =
                            fun (e: Browser.Types.KeyboardEvent) -> promise {
                                match e.code with
                                | Swate.Components.kbdEventCode.enter //enter
                                | Swate.Components.kbdEventCode.escape -> //escape
                                    makeIdle ()
                                | _ -> ()
                            }

                        let setter =
                            fun (termOpt: Swate.Components.Term option) ->
                                let oa =
                                    termOpt
                                    |> Option.map OntologyAnnotation.fromTerm
                                    |> Option.defaultWith OntologyAnnotation

                                oasetter.Value.setter oa

                        let headerOA =
                            if state.TableViewIsActive() then
                                state.ActiveTable.Headers.[columnIndex].TryOA()
                                |> Option.bind (fun x ->
                                    x.TermAccessionShort |> Option.whereNot System.String.IsNullOrWhiteSpace)
                            else
                                None
                        // Components.TermSearch.Input(
                        //     setter, input=oa, fullwidth=true, ?parent=headerOA, displayParent=false,
                        //     onBlur=onBlur, onEscape=onEscape, onEnter=onEnter, autofocus=true,
                        //     classes="h-[35px] !rounded-none w-full !border-0"
                        // )
                        Swate.Components.TermSearch.TermSearch(
                            setter,
                            term = input,
                            ?parentId = headerOA,
                            autoFocus = true,
                            onBlur = onBlur,
                            onKeyDown = onKeyDown,
                            classNames = (Swate.Components.TermSearchStyle(!^"h-[35px] !rounded-none w-full !border-0")),
                            disableDefaultSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            disableDefaultAllChildrenSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            disableDefaultParentSearch = model.PersistentStorageState.IsDisabledSwateDefaultSearch,
                            termSearchQueries = model.PersistentStorageState.TIBQueries.TermSearch,
                            parentSearchQueries = model.PersistentStorageState.TIBQueries.ParentSearch,
                            allChildrenSearchQueries = model.PersistentStorageState.TIBQueries.AllChildrenSearch
                        )
                    else
                        Cell.CellInputElement(cellValue, false, false, setter, makeIdle, index, dispatch)
                else if columnType = Main && oasetter.IsSome then
                    CellStyles.CompositeCellDisplay(oasetter.Value.oa, displayValue)
                else
                    CellStyles.BasicValueDisplayCell(displayValue, None)
            ]
        ]

    [<ReactComponent>]
    static member BodySelect
        (value: string, setter: string -> unit, values: #seq<string>, index: (int * int), model: Model.Model, dispatch)
        =
        let columnIndex, rowIndex = index

        Html.td [
            prop.key $"Cell_Select_{columnIndex}_{rowIndex}"
            CellStyles.cellStyle []
            prop.onContextMenu (CellAux.contextMenuController index model dispatch)
            prop.children [
                Html.div [
                    prop.children [
                        Html.div [
                            prop.className "select w-full"
                            prop.children [
                                Html.select [
                                    prop.className "!rounded-none w-full"
                                    prop.value value
                                    prop.onChange (fun (e: string) -> setter e)
                                    prop.children [
                                        for v in values do
                                            Html.option [ prop.value v; prop.text v ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    static member Body(index: (int * int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.GetContentSwate().[0]

        let setter =
            fun (s: string) ->
                let nextCell = cell.UpdateMainField s
                Msg.UpdateCell(index, nextCell) |> SpreadsheetMsg |> dispatch

        let oasetter =
            if cell.isTerm then
                {|
                    oa = cell.ToOA()
                    setter =
                        fun (oa: OntologyAnnotation) ->
                            let nextCell = cell.UpdateWithOA oa
                            CellAux.oasetter (index, nextCell, dispatch)
                |}
                |> Some
            else
                None

        let displayValue = cell.ToString()

        Cell.BodyBase(
            Main,
            cellValue,
            setter,
            index,
            model,
            dispatch,
            ?oasetter = oasetter,
            displayValue = displayValue
        )

    static member BodyUnit(index: (int * int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.ToOA().NameText

        let setter =
            fun (s: string) ->
                let oa = cell.ToOA()
                let newName = if s = "" then None else Some s
                oa.Name <- newName
                let nextCell = cell.UpdateWithOA oa
                Msg.UpdateCell(index, nextCell) |> SpreadsheetMsg |> dispatch

        let oasetter =
            if cell.isUnitized then
                {|
                    oa = cell.ToOA()
                    setter =
                        fun (oa: OntologyAnnotation) ->
                            let nextCell = cell.UpdateWithOA oa
                            CellAux.oasetter (index, nextCell, dispatch)
                |}
                |> Some
            else
                None

        let displayValue = cell.ToString()

        Cell.BodyBase(
            Unit,
            cellValue,
            setter,
            index,
            model,
            dispatch,
            ?oasetter = oasetter,
            displayValue = displayValue
        )

    static member BodyTSR(index: (int * int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.ToOA().TermSourceREF |> Option.defaultValue ""

        let setter =
            fun (s: string) ->
                let oa = cell.ToOA()
                let newTSR = if s = "" then None else Some s
                oa.TermSourceREF <- newTSR
                let nextCell = cell.UpdateWithOA oa
                Msg.UpdateCell(index, nextCell) |> SpreadsheetMsg |> dispatch

        Cell.BodyBase(TSR, cellValue, setter, index, model, dispatch)

    static member BodyTAN(index: (int * int), cell: CompositeCell, model: Model, dispatch) =
        let cellValue = cell.ToOA().TermAccessionNumber |> Option.defaultValue ""

        let setter =
            fun (s: string) ->
                let oa = cell.ToOA()
                let newTAN = if s = "" then None else Some s
                oa.TermAccessionNumber <- newTAN
                let nextCell = cell.UpdateWithOA oa
                Msg.UpdateCell(index, nextCell) |> SpreadsheetMsg |> dispatch

        Cell.BodyBase(TAN, cellValue, setter, index, model, dispatch)

    static member BodyDataSelector(index: (int * int), cell: CompositeCell, model: Model, dispatch) =
        let data = cell.AsData
        let cellValue = data.Selector |> Option.defaultValue ""

        let setter =
            fun (s: string) ->
                let next = if s = "" then None else Some s
                data.Selector <- next
                let nextCell = cell.UpdateWithData data
                Msg.UpdateCell(index, nextCell) |> SpreadsheetMsg |> dispatch

        Cell.BodyBase(ColumnType.DataSelector, cellValue, setter, index, model, dispatch)

    static member BodyDataFormat(index: (int * int), cell: CompositeCell, model: Model, dispatch) =
        let data = cell.AsData
        let cellValue = data.Format |> Option.defaultValue ""

        let setter =
            fun (s: string) ->
                let next = if s = "" then None else Some s
                data.Format <- next
                let nextCell = cell.UpdateWithData data
                Msg.UpdateCell(index, nextCell) |> SpreadsheetMsg |> dispatch

        Cell.BodyBase(ColumnType.DataFormat, cellValue, setter, index, model, dispatch)

    static member BodyDataSelectorFormat(index: (int * int), cell: CompositeCell, model: Model, dispatch) =
        let data = cell.AsData
        let cellValue = data.SelectorFormat |> Option.defaultValue ""

        let setter =
            fun (s: string) ->
                let next = if s = "" then None else Some s
                data.SelectorFormat <- next
                let nextCell = cell.UpdateWithData data
                Msg.UpdateCell(index, nextCell) |> SpreadsheetMsg |> dispatch

        Cell.BodyBase(ColumnType.DataSelectorFormat, cellValue, setter, index, model, dispatch)