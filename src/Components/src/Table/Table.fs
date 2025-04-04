namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI

module Virtual =

    [<Literal>]
    let ImportPath = "@tanstack/react-virtual"

    [<ImportMember(ImportPath)>]
    type Range =
        interface end

    [<ImportMember(ImportPath)>]
    type VirtualItem =
        member this.key: string = jsNative
        member this.index: int = jsNative
        member this.start: int = jsNative
        member this.``end``: int = jsNative
        member this.size: int = jsNative

    [<StringEnum(CaseRules.LowerFirst)>]
    type AlignOption =
        | Auto
        | Start
        | Center
        | End

    [<StringEnum(CaseRules.LowerFirst)>]
    type ScrollBehavior =
        | Auto
        | Smooth

    [<ImportMember(ImportPath)>]
    type Virtualizer<'A,'B> =
        member this.getVirtualItems() : VirtualItem [] = jsNative
        member this.getTotalSize() : int = jsNative
        member this.scrollToIndex (index: int, ?options: {|align: AlignOption option; behavior: ScrollBehavior option|}) : unit = jsNative

type Virtual =

    [<ImportMember(Virtual.ImportPath)>]
    static member defaultRangeExtractor(range: Virtual.Range) : int [] = jsNative

    [<ImportMember(Virtual.ImportPath)>]
    [<NamedParamsAttribute>]
    static member useVirtualizer
        (
            // required
            count: int,
            getScrollElement: unit -> option<Browser.Types.HTMLElement>,
            estimateSize: int -> int,
            // optional
            ?scrollMargin: float,
            ?scrollPaddingStart: float,
            ?scrollPaddingEnd: float,
            ?overscan: int,
            ?rangeExtractor: Virtual.Range -> int [],
            ?debug: bool,
            ?onChange: (Virtual.Virtualizer<_,_> * bool) -> unit,
            ?horizontal: bool,
            ?paddingStart: int,
            ?paddingEnd: int,
            ?gap: int,
            ?lanes: int
        ) : Virtual.Virtualizer<obj, obj> = jsNative

module private TableHelper =

    let (|ActiveTrigger|Default|) (eventCode: string) =
        let lower = eventCode.ToLower()
        if (lower.StartsWith("key") || lower.StartsWith("digit") || lower.StartsWith("numpad")) || lower.StartsWith("backspace") then
            ActiveTrigger
        else
            Default

    let keyDownController (e: Browser.Types.KeyboardEvent) (selectedCells: GridSelect.GridSelectHandle) (activeCellIndex: CellCoordinate option, setActiveCellIndex) =
        if activeCellIndex.IsNone then
            let nav = selectedCells.selectBy e
            if not nav && selectedCells.count > 0 then
                match e.code with
                | ActiveTrigger ->
                    selectedCells.SelectOrigin |> Option.defaultValue selectedCells.selectedCellsReducedSet.MinimumElement
                    |> Some
                    |> setActiveCellIndex
                | Default -> ()



[<Mangle(false); Erase>]
type Table =

    [<ReactComponent(true)>]
    static member Table(
        rowCount: int,
        columnCount: int,
        renderCell: CellCoordinate -> ReactElement,
        renderActiveCell: TableCellController -> ReactElement,
        ref: IRefValue<TableHandle>,
        ?onSelect: GridSelect.OnSelect,
        ?onKeyDown: {|event: Browser.Types.KeyboardEvent; |} -> unit,
        ?defaultStyleSelect: bool,
        ?debug: bool
    ) =
        let debug = defaultArg debug false
        let defaultStyleSelect = defaultArg defaultStyleSelect true

        let scrollContainerRef = React.useElementRef()

        let rowVirtualizer = Virtual.useVirtualizer(
            count = rowCount,
            getScrollElement = (fun () -> scrollContainerRef.current),
            estimateSize = (fun _ -> Constants.Table.DefaultRowHeight),
            overscan = 2,
            scrollMargin = -Constants.Table.DefaultRowHeight,
            scrollPaddingEnd = 1.5 * float Constants.Table.DefaultRowHeight,
            rangeExtractor = (fun range ->
                let next = set [
                    0
                    yield! Virtual.defaultRangeExtractor range
                ]
                Set.toArray next
            )
        )

        let columnVirtualizer = Virtual.useVirtualizer(
            count = columnCount,
            getScrollElement = (fun () -> scrollContainerRef.current),
            estimateSize = (fun _ -> Constants.Table.DefaultColumnWidth),
            overscan = 2,
            scrollMargin = -Constants.Table.DefaultColumnWidth,
            scrollPaddingEnd = 1.5 * float Constants.Table.DefaultColumnWidth,
            horizontal = true,
            rangeExtractor = (fun range ->
                let next = set [
                    0
                    yield! Virtual.defaultRangeExtractor range
                ]
                Set.toArray next
            )
        )

        let scrollTo = fun (coordinate: CellCoordinate) ->
            rowVirtualizer.scrollToIndex(coordinate.y)
            columnVirtualizer.scrollToIndex(coordinate.x)

        let onSelect =
            fun cell newCellRange ->
                onSelect |> Option.iter(fun onSelect ->
                    onSelect cell newCellRange
                )
                scrollTo(cell)

        let activeCellIndex, setActiveCellIndex = React.useState(None: CellCoordinate option)

        let GridSelect = React.useGridSelect(
            rowCount = rowCount,
            columnCount = columnCount,
            minRow = 1,
            minCol = 1,
            onSelect = onSelect
        )

        React.useImperativeHandle(
            ref,
            (fun () ->
                TableHandle(
                    scrollTo = scrollTo,
                    select = SelectHandle(
                        contains = GridSelect.contains,
                        selectAt = GridSelect.selectAt,
                        clear = GridSelect.clear
                    )
                )
            ),
            [| GridSelect.selectedCells |]
        )

        React.fragment [
            Html.div [
                prop.className "border border-primary p-2 flex flex-row gap-2"
                prop.children [
                    Html.div [
                        let txt = Swate.Components.GridSelect.SelectedCellRange.toString GridSelect.selectedCells
                        prop.text txt
                    ]
                ]
            ]
            Html.div [
                prop.ref scrollContainerRef
                prop.onKeyDown (fun e ->
                    TableHelper.keyDownController e GridSelect (activeCellIndex, setActiveCellIndex)
                )
                prop.tabIndex 0
                prop.className "overflow-auto h-96 w-full border border-primary rounded snap-both snap-proximity"
                if debug then
                    prop.testId "virtualized-table"
                prop.children [
                    Html.div [
                        prop.style [
                            style.marginTop Constants.Table.DefaultRowHeight
                            style.marginLeft Constants.Table.DefaultColumnWidth
                            style.height (rowVirtualizer.getTotalSize())
                            style.width (columnVirtualizer.getTotalSize())
                            style.position.relative
                        ]
                        prop.className ""
                        prop.children [
                            for virtualRow in rowVirtualizer.getVirtualItems() do
                                React.keyedFragment(virtualRow.index, [
                                    for virtualColumn in columnVirtualizer.getVirtualItems() do
                                        let isSelected = GridSelect.contains ({| x = virtualColumn.index; y = virtualRow.index |})
                                        let isOrigin = GridSelect.SelectOrigin |> Option.exists(fun origin -> origin = {| x = virtualColumn.index; y = virtualRow.index |})
                                        let isActive = activeCellIndex = Some {| x = virtualColumn.index; y = virtualRow.index |}
                                        Html.div [
                                            prop.key virtualColumn.key
                                            prop.style [
                                                style.top 0
                                                style.left 0
                                                style.position.absolute
                                                style.custom("transform", $"translateX({virtualColumn.start}px) translateY({virtualRow.start}px)" )
                                                if virtualColumn.index = 0 then
                                                    style.height virtualRow.size
                                                    style.width (length.perc 100)
                                                    style.zIndex 2
                                                    style.pointerEvents.none
                                                elif virtualRow.index = 0 then
                                                    style.height (length.perc 100)
                                                    style.width virtualColumn.size
                                                    style.zIndex 1
                                                    style.pointerEvents.none
                                                else
                                                    style.height virtualRow.size
                                                    style.width virtualColumn.size
                                            ]
                                            prop.onClick(fun e ->
                                                if not isActive then
                                                    if e.detail >= 2 then
                                                        console.log("set active")
                                                        setActiveCellIndex(Some {| x = virtualColumn.index; y = virtualRow.index |})
                                                    else
                                                        console.log("set select")
                                                        let nextSet = Set.singleton ({|x = virtualColumn.index; y = virtualRow.index|})
                                                        if GridSelect.selectedCellsReducedSet = nextSet then
                                                            GridSelect.clear()
                                                        else
                                                            GridSelect.selectAt({|x = virtualColumn.index; y = virtualRow.index|}, e.shiftKey)
                                            )
                                            prop.className [
                                                "cursor-pointer"
                                                if defaultStyleSelect then
                                                    "swt-table-cell"
                                            ]
                                            if isActive then
                                                prop.custom("data-active", isActive)
                                            if isSelected then
                                                prop.custom("data-selected", isSelected)
                                            if isOrigin then
                                                prop.custom("data-is-append-origin", isOrigin)
                                            prop.children (
                                                let index = {|x = virtualColumn.index; y = virtualRow.index|}
                                                if isActive then
                                                    let controller =
                                                        TableCellController.init(
                                                            index,
                                                            onKeyDown = (fun (e: Browser.Types.KeyboardEvent) ->
                                                                match e.code with
                                                                | kbdEventCode.enter ->
                                                                    if GridSelect.contains({|index with y = index.y + 1|}) then
                                                                        setActiveCellIndex(Some({|index with y = index.y + 1|}))
                                                                    elif GridSelect.contains({|x = index.x + 1; y = GridSelect.selectedCells.Value.yStart|}) then
                                                                        setActiveCellIndex(Some {|x = index.x + 1; y = GridSelect.selectedCells.Value.yStart|})
                                                                    else
                                                                        // setActiveCellIndex(Some({|x = GridSelect.selectedCells.Value.xStart; y = GridSelect.selectedCells.Value.yStart|}))
                                                                        setActiveCellIndex(None)
                                                                        scrollContainerRef.current.Value.focus()
                                                                        GridSelect.selectAt(index, false)
                                                                | kbdEventCode.escape ->
                                                                    setActiveCellIndex(None)
                                                                    scrollContainerRef.current.Value.focus()
                                                                    GridSelect.clear()
                                                                | _ ->
                                                                    ()
                                                            ),
                                                            onBlur = (fun _ ->
                                                                setActiveCellIndex(None)
                                                            )
                                                        )
                                                    renderActiveCell controller
                                                else
                                                    renderCell index
                                            )
                                        ]
                                ])
                        ]
                    ]
                ]
            ]
    ]

    [<ReactComponent>]
    static member MinimalTableCell(ts: TableCellController, data: string, setData) =
        let tempData, setTempData = React.useState(data)
        React.useEffect((fun _ ->
            setTempData data
        ), [| box data |])
        TableCell.BaseCell(
            ts.Index.y,
            ts.Index.x,
            Html.input [
                prop.autoFocus true
                prop.className "rounded-none w-full h-full bg-base-100 text-base-content px-2 py-1"
                prop.defaultValue tempData
                prop.onChange (fun (e: string) ->
                    setTempData e
                )
                prop.onKeyDown (fun e ->
                    ts.onKeyDown e
                    match e.code with
                    | kbdEventCode.enter ->
                        setData tempData
                    | _ -> ()
                )
                prop.onBlur (fun e ->
                    ts.onBlur e
                    setData tempData
                )
            ]
        )

    [<ReactComponent>]
    static member Entry() =
        let TableHandler = React.useRef<TableHandle>(null)
        let rowCount = 1_000
        let columnCount = 1_000
        let data, setData = React.useState([|
            for i in 0 .. columnCount - 1 do
                [|
                    for j in 0 .. rowCount - 1 do
                        yield $"Row {j}, Column {i}"
                |]
        |])
        let render =
            React.memo (
                (fun (ts: CellCoordinate) ->
                    if ts.x = 0 then
                        TableCell.StickyIndexColumn(ts.y, false)
                    elif ts.y = 0 then
                        TableCell.StickyHeader(ts.x, false)
                    else
                        TableCell.BaseCell(
                            ts.y,
                            ts.x,
                            Html.div [
                                prop.className "truncate"
                                prop.text data.[ts.y].[ts.x]
                            ]
                        )
                ),
                withKey = (fun (ts: CellCoordinate) -> $"{ts.x}-{ts.y}")
            )
        let renderActiveCell =
            React.memo (
                (fun (ts: TableCellController) ->
                    Table.MinimalTableCell(
                        ts,
                        data.[ts.Index.y].[ts.Index.x],
                        (fun newData ->
                            data.[ts.Index.y].[ts.Index.x] <- newData
                            setData data
                        )
                    )
                ),
                withKey = (fun (ts: TableCellController) -> $"{ts.Index.x}-{ts.Index.y}")
            )

        Html.div [
            prop.className "flex flex-col gap-4"
            prop.children [
                Html.button [
                    prop.className "btn btn-primary"
                    prop.text "scroll to 500, 500"
                    prop.onClick (fun _ ->
                        TableHandler.current.select.selectAt({| x = 500; y = 500 |}, false)
                        TableHandler.current.scrollTo({| x = 500; y = 500 |})
                    )
                ]
                Table.Table(
                    rowCount = 100_000,
                    columnCount = 100_000,
                    renderCell = render,
                    renderActiveCell = renderActiveCell,
                    ref = TableHandler
                )
            ]
        ]
